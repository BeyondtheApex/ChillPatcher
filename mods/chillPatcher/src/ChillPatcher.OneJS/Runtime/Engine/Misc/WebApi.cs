using System;
using System.Collections;
using System.Collections.Generic;
using Puerts;
using UnityEngine;
using UnityEngine.Networking;

namespace OneJS {
    /// <summary>
    /// Caches images and coalesces multiple requests for the same image.
    /// Supports custom headers and an optional force-refresh mode.
    /// </summary>
    public class WebApi {
        Dictionary<string, Texture2D> _imageCache = new();
        Dictionary<string, List<Action<Texture2D>>> _ongoingRequests = new Dictionary<string, List<Action<Texture2D>>>();

        public Coroutine getText(string uri, Action<string> callback, string headersJson = null) {
            Dictionary<string, string> headers = null;
            if (headersJson != null) {
                headers = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(headersJson);
            }
            return StaticCoroutine.Start(GetTextCo(uri, callback, headers));
        }

        IEnumerator GetTextCo(string uri, Action<string> callback, Dictionary<string, string> headers) {
            using (UnityWebRequest request = UnityWebRequest.Get(uri)) {
                if (headers != null) {
                    foreach (var kv in headers) {
                        request.SetRequestHeader(kv.Key, kv.Value);
                    }
                }
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError)
                    callback(request.error);
                else
                    callback(request.downloadHandler.text);
            }
        }

        public Coroutine getImage(string url, Action<Texture2D> callback, string headersJson = null, bool forceRefresh = false) {
            Dictionary<string, string> headers = null;
            if (headersJson != null) {
                headers = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(headersJson);
            }
            if (!forceRefresh) {
                if (_imageCache.TryGetValue(url, out var value)) {
                    callback(value);
                    return null;
                }
                if (_ongoingRequests.ContainsKey(url)) {
                    _ongoingRequests[url].Add(callback);
                    return null;
                }
                _ongoingRequests[url] = new List<Action<Texture2D>> { callback };

                return StaticCoroutine.Start(GetImageCo(url, headers));
            } else {
                return StaticCoroutine.Start(GetImageCoForced(url, headers, callback));
            }
        }

        IEnumerator GetImageCo(string url, Dictionary<string, string> headers) {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url)) {
                if (headers != null) {
                    foreach (var kv in headers) {
                        request.SetRequestHeader(kv.Key, kv.Value);
                    }
                }
                yield return request.SendWebRequest();
                Texture2D texture = null;
                if (request.result == UnityWebRequest.Result.Success) {
                    texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                    _imageCache[url] = texture;
                } else {
                    Debug.LogError(request.result);
                }

                if (_ongoingRequests.TryGetValue(url, out var callbacks)) {
                    _ongoingRequests.Remove(url);
                    foreach (var cb in callbacks)
                        cb(texture);
                }
            }
        }

        IEnumerator GetImageCoForced(string url, Dictionary<string, string> headers, Action<Texture2D> callback) {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url)) {
                if (headers != null) {
                    foreach (var kv in headers) {
                        request.SetRequestHeader(kv.Key, kv.Value);
                    }
                }
                yield return request.SendWebRequest();
                Texture2D texture = null;
                if (request.result == UnityWebRequest.Result.Success) {
                    texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                    // Update the cache so future calls get the fresh image.
                    _imageCache[url] = texture;
                } else {
                    Debug.LogError(request.result);
                }
                callback(texture);
            }
        }
    }
}