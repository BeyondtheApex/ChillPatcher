using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using BepInEx.Logging;
using Newtonsoft.Json;
using OneJS;
using UnityEngine;
using UnityEngine.Networking;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 网络请求 API
    /// 
    /// JS 端用法：
    ///   // 回调风格
    ///   chill.net.get("https://api.example.com/data", (res) => {
    ///       const data = JSON.parse(res);
    ///       if (data.ok) console.log(data.body);
    ///       else console.log(data.error);
    ///   });
    ///
    ///   // 带自定义头
    ///   chill.net.get(url, callback, '{"Authorization":"Bearer xxx"}');
    ///
    ///   // POST JSON
    ///   chill.net.postJson(url, '{"key":"value"}', callback);
    ///
    ///   // 通用 fetch
    ///   chill.net.fetch(url, callback, '{"method":"POST","headers":{"X-Key":"val"},"body":"...","contentType":"application/json"}');
    /// </summary>
    public class ChillNetApi
    {
        private readonly ManualLogSource _logger;

        public ChillNetApi(ManualLogSource logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// GET 请求，callback 收到 JSON: { ok, status, body, error }
        /// </summary>
        public Coroutine get(string url, Action<string> callback, string headersJson = null)
        {
            return StaticCoroutine.Start(RequestCo("GET", url, null, null, ParseHeaders(headersJson), callback));
        }

        /// <summary>
        /// POST 请求（自定义 content-type 和 body）
        /// </summary>
        public Coroutine post(string url, string body, string contentType, Action<string> callback, string headersJson = null)
        {
            return StaticCoroutine.Start(RequestCo("POST", url, body, contentType, ParseHeaders(headersJson), callback));
        }

        /// <summary>
        /// POST JSON 快捷方法
        /// </summary>
        public Coroutine postJson(string url, string jsonBody, Action<string> callback, string headersJson = null)
        {
            return StaticCoroutine.Start(RequestCo("POST", url, jsonBody, "application/json", ParseHeaders(headersJson), callback));
        }

        /// <summary>
        /// PUT 请求
        /// </summary>
        public Coroutine put(string url, string body, string contentType, Action<string> callback, string headersJson = null)
        {
            return StaticCoroutine.Start(RequestCo("PUT", url, body, contentType, ParseHeaders(headersJson), callback));
        }

        /// <summary>
        /// DELETE 请求
        /// </summary>
        public Coroutine delete(string url, Action<string> callback, string headersJson = null)
        {
            return StaticCoroutine.Start(RequestCo("DELETE", url, null, null, ParseHeaders(headersJson), callback));
        }

        /// <summary>
        /// 通用 fetch 方法，通过 optionsJson 控制请求参数。
        /// optionsJson: { "method": "POST", "headers": {"key":"val"}, "body": "...", "contentType": "application/json" }
        /// callback 收到 JSON: { "ok": true/false, "status": 200, "body": "...", "error": "..." }
        /// </summary>
        public Coroutine fetch(string url, Action<string> callback, string optionsJson = null)
        {
            var method = "GET";
            string body = null;
            string contentType = null;
            Dictionary<string, string> headers = null;

            if (!string.IsNullOrEmpty(optionsJson))
            {
                try
                {
                    var options = JsonConvert.DeserializeObject<Dictionary<string, object>>(optionsJson);
                    if (options.TryGetValue("method", out var m))
                        method = m?.ToString()?.ToUpperInvariant() ?? "GET";
                    if (options.TryGetValue("body", out var b))
                        body = b?.ToString();
                    if (options.TryGetValue("contentType", out var ct))
                        contentType = ct?.ToString();
                    if (options.TryGetValue("headers", out var h) && h != null)
                        headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(h.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[NetApi] Failed to parse fetch options: {ex.Message}");
                }
            }

            return StaticCoroutine.Start(RequestCo(method, url, body, contentType, headers, callback));
        }

        private IEnumerator RequestCo(
            string method,
            string url,
            string body,
            string contentType,
            Dictionary<string, string> headers,
            Action<string> callback)
        {
            using (var request = new UnityWebRequest(url, method))
            {
                // 设置请求体
                if (body != null)
                {
                    var bodyBytes = Encoding.UTF8.GetBytes(body);
                    request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                    if (!string.IsNullOrEmpty(contentType))
                        request.SetRequestHeader("Content-Type", contentType);
                }

                request.downloadHandler = new DownloadHandlerBuffer();

                // 设置自定义头
                if (headers != null)
                {
                    foreach (var kv in headers)
                        request.SetRequestHeader(kv.Key, kv.Value);
                }

                yield return request.SendWebRequest();

                var result = new NetResponse
                {
                    ok = request.result == UnityWebRequest.Result.Success,
                    status = (int)request.responseCode,
                    body = request.downloadHandler?.text,
                    error = request.result != UnityWebRequest.Result.Success ? request.error : null
                };

                callback?.Invoke(JsonConvert.SerializeObject(result));
            }
        }

        private Dictionary<string, string> ParseHeaders(string headersJson)
        {
            if (string.IsNullOrEmpty(headersJson)) return null;
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(headersJson);
            }
            catch
            {
                return null;
            }
        }

        [Serializable]
        private class NetResponse
        {
            public bool ok;
            public int status;
            public string body;
            public string error;
        }
    }
}
