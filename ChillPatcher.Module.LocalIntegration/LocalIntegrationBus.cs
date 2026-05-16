using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using BepInEx.Logging;

namespace ChillPatcher.Module.LocalIntegration
{
    public sealed class LocalIntegrationBus : IDisposable
    {
        private readonly ManualLogSource _logger;
        private readonly string _host;
        private readonly int _port;
        private readonly string _token;
        private readonly Dictionary<string, ILocalIntegrationHandler> _handlers = new Dictionary<string, ILocalIntegrationHandler>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentQueue<QueuedRequest> _requests = new ConcurrentQueue<QueuedRequest>();

        private TcpListener _listener;
        private Thread _thread;
        private volatile bool _running;

        public LocalIntegrationBus(ManualLogSource logger, string host, int port, string token)
        {
            _logger = logger;
            _host = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host.Trim();
            _port = port;
            _token = token ?? string.Empty;
        }

        internal void Register(ILocalIntegrationHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _handlers[RouteKey(handler.Method, handler.Path)] = handler;
            _logger?.LogInfo($"[本地集成总线] 已注册 {handler.Method} {handler.Path}");
        }

        public void Start()
        {
            if (_running) return;

            _listener = new TcpListener(IPAddress.Parse(_host), _port);
            _listener.Start();
            _running = true;
            _thread = new Thread(Run) { IsBackground = true, Name = "ChillPatcherLocalIntegrationBus" };
            _thread.Start();
            _logger?.LogInfo($"[本地集成总线] 正在监听 http://{_host}:{_port}");
        }

        public void Tick()
        {
            while (_requests.TryDequeue(out var request))
            {
                try
                {
                    request.Handler.Execute(request.Body, _logger);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"[本地集成总线] 处理器执行失败（{request.Handler.Method} {request.Handler.Path}）：{ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            _running = false;
            try { _listener?.Stop(); } catch { }
        }

        private void Run()
        {
            while (_running)
            {
                try
                {
                    var client = _listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(_ => Handle(client));
                }
                catch (SocketException)
                {
                    if (_running) _logger?.LogWarning("[本地集成总线] 监听器意外停止。");
                }
                catch (Exception ex)
                {
                    if (_running) _logger?.LogWarning($"[本地集成总线] 监听器错误：{ex.Message}");
                }
            }
        }

        private void Handle(TcpClient client)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                try
                {
                    var headers = ReadHeaders(stream);
                    var firstLine = headers.Split(new[] { "\r\n" }, StringSplitOptions.None)[0];

                    if (firstLine.StartsWith("GET /health ", StringComparison.OrdinalIgnoreCase))
                    {
                        WriteJson(stream, 200, "{\"ok\":true}");
                        return;
                    }

                    var route = ParseRoute(firstLine);
                    if (route == null || !_handlers.TryGetValue(RouteKey(route.Method, route.Path), out var handler))
                    {
                        WriteJson(stream, 404, "{\"ok\":false,\"error\":\"not found\"}");
                        return;
                    }

                    if (!IsAuthorized(headers))
                    {
                        WriteJson(stream, 401, "{\"ok\":false,\"error\":\"unauthorized\"}");
                        return;
                    }

                    var body = ReadBody(stream, GetContentLength(headers));
                    if (!handler.TryValidate(body, out var error))
                    {
                        WriteJson(stream, 400, "{\"ok\":false,\"error\":\"" + EscapeJson(error) + "\"}");
                        return;
                    }

                    _requests.Enqueue(new QueuedRequest(handler, body));
                    WriteJson(stream, 202, "{\"ok\":true,\"queued\":true}");
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"[本地集成总线] 请求处理失败：{ex.Message}");
                    WriteJson(stream, 500, "{\"ok\":false,\"error\":\"internal error\"}");
                }
            }
        }

        private static string ReadHeaders(NetworkStream stream)
        {
            var buffer = new MemoryStream();
            var one = new byte[1];
            while (stream.Read(one, 0, 1) == 1)
            {
                buffer.WriteByte(one[0]);
                var data = buffer.ToArray();
                var n = data.Length;
                if (n >= 4 && data[n - 4] == '\r' && data[n - 3] == '\n' && data[n - 2] == '\r' && data[n - 1] == '\n')
                    break;
            }
            return Encoding.UTF8.GetString(buffer.ToArray());
        }

        private static int GetContentLength(string headers)
        {
            var match = Regex.Match(headers, @"Content-Length:\s*(\d+)", RegexOptions.IgnoreCase);
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        private static Route ParseRoute(string firstLine)
        {
            var parts = (firstLine ?? "").Split(' ');
            if (parts.Length < 2) return null;
            return new Route(parts[0], parts[1]);
        }

        private static string RouteKey(string method, string path)
        {
            return $"{(method ?? "").Trim().ToUpperInvariant()} {(path ?? "").Trim()}";
        }

        private bool IsAuthorized(string headers)
        {
            if (string.IsNullOrWhiteSpace(_token)) return true;

            var auth = Regex.Match(headers, @"Authorization:\s*Bearer\s+([^\r\n]+)", RegexOptions.IgnoreCase);
            if (auth.Success && string.Equals(auth.Groups[1].Value.Trim(), _token, StringComparison.Ordinal))
                return true;

            var header = Regex.Match(headers, @"X-ChillPatcher-Token:\s*([^\r\n]+)", RegexOptions.IgnoreCase);
            return header.Success && string.Equals(header.Groups[1].Value.Trim(), _token, StringComparison.Ordinal);
        }

        private static string ReadBody(NetworkStream stream, int length)
        {
            if (length <= 0) return "";

            var bytes = new byte[length];
            var offset = 0;
            while (offset < length)
            {
                var read = stream.Read(bytes, offset, length - offset);
                if (read <= 0) break;
                offset += read;
            }

            return Encoding.UTF8.GetString(bytes, 0, offset);
        }

        private static void WriteJson(NetworkStream stream, int status, string body)
        {
            var payload = Encoding.UTF8.GetBytes(body);
            var reason = status == 200 ? "OK" :
                status == 202 ? "Accepted" :
                status == 400 ? "Bad Request" :
                status == 401 ? "Unauthorized" :
                status == 404 ? "Not Found" :
                "Internal Server Error";
            var header = $"HTTP/1.1 {status} {reason}\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: {payload.Length}\r\nConnection: close\r\n\r\n";
            var headerBytes = Encoding.ASCII.GetBytes(header);
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(payload, 0, payload.Length);
        }

        private static string EscapeJson(string value)
        {
            return (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private sealed class Route
        {
            public Route(string method, string path)
            {
                Method = method;
                Path = path;
            }

            public string Method { get; }
            public string Path { get; }
        }

        private sealed class QueuedRequest
        {
            public QueuedRequest(ILocalIntegrationHandler handler, string body)
            {
                Handler = handler;
                Body = body;
            }

            public ILocalIntegrationHandler Handler { get; }
            public string Body { get; }
        }
    }
}
