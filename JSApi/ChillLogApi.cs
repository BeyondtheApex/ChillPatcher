using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using BepInEx.Logging;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 日志 API：
    /// 1. JS 端日志输出到 ui/ 目录下的独立文件
    /// 2. 获取游戏运行时日志流（实时推送）
    /// </summary>
    public class ChillLogApi : IDisposable
    {
        private readonly ManualLogSource _logger;
        private readonly string _jsLogPath;
        private StreamWriter _jsLogWriter;
        private readonly object _writeLock = new object();

        // 游戏日志监听
        private bool _gameLogListening;
        private Action<string> _gameLogCallback;
        private readonly ConcurrentQueue<string> _gameLogBuffer = new ConcurrentQueue<string>();
        private const int MaxBufferSize = 500;

        public ChillLogApi(ManualLogSource logger, string uiDir)
        {
            _logger = logger;
            _jsLogPath = Path.Combine(uiDir, "oneJS.log");

            try
            {
                _jsLogWriter = new StreamWriter(_jsLogPath, false, Encoding.UTF8) { AutoFlush = true };
                _jsLogWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] OneJS Log Started");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[JSApi.Log] Failed to create JS log file: {ex.Message}");
            }

            // 订阅 BepInEx 日志
            BepInEx.Logging.Logger.Listeners.Add(new GameLogListener(this));
        }

        #region JS 端日志（输出到文件）

        public void log(string message)
        {
            WriteJSLog("LOG", message);
        }

        public void info(string message)
        {
            WriteJSLog("INFO", message);
        }

        public void warn(string message)
        {
            WriteJSLog("WARN", message);
        }

        public void error(string message)
        {
            WriteJSLog("ERROR", message);
        }

        public void debug(string message)
        {
            WriteJSLog("DEBUG", message);
        }

        private void WriteJSLog(string level, string message)
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}";
            lock (_writeLock)
            {
                try
                {
                    _jsLogWriter?.WriteLine(line);
                }
                catch { /* 静默失败 */ }
            }
        }

        #endregion

        #region 游戏日志流式获取

        /// <summary>
        /// 开始监听游戏日志（实时推送到 JS 回调）
        /// </summary>
        public void startListening(Action<string> callback)
        {
            _gameLogCallback = callback;
            _gameLogListening = true;

            // 推送缓冲中的历史日志
            while (_gameLogBuffer.TryDequeue(out var log))
            {
                try { callback(log); } catch { }
            }
        }

        /// <summary>
        /// 停止监听游戏日志
        /// </summary>
        public void stopListening()
        {
            _gameLogListening = false;
            _gameLogCallback = null;
        }

        /// <summary>
        /// 获取缓冲的最近日志
        /// </summary>
        public string getRecentLogs(int count)
        {
            var arr = _gameLogBuffer.ToArray();
            var start = Math.Max(0, arr.Length - count);
            var result = new string[Math.Min(count, arr.Length)];
            Array.Copy(arr, start, result, 0, result.Length);
            return JSApiHelper.ToJson(result);
        }

        internal void OnGameLog(string logLine)
        {
            // 缓冲
            _gameLogBuffer.Enqueue(logLine);
            while (_gameLogBuffer.Count > MaxBufferSize)
                _gameLogBuffer.TryDequeue(out _);

            // 推送
            if (_gameLogListening && _gameLogCallback != null)
            {
                try { _gameLogCallback(logLine); } catch { }
            }
        }

        #endregion

        public void Dispose()
        {
            _gameLogListening = false;
            _gameLogCallback = null;
            lock (_writeLock)
            {
                _jsLogWriter?.Dispose();
                _jsLogWriter = null;
            }
        }

        /// <summary>
        /// BepInEx 日志监听器，将游戏日志转发到 ChillLogApi
        /// </summary>
        private class GameLogListener : ILogListener
        {
            private readonly ChillLogApi _api;
            public GameLogListener(ChillLogApi api) => _api = api;

            public void LogEvent(object sender, LogEventArgs eventArgs)
            {
                var line = $"[{eventArgs.Level}:{eventArgs.Source.SourceName}] {eventArgs.Data}";
                _api.OnGameLog(line);
            }

            public void Dispose() { }
        }
    }
}
