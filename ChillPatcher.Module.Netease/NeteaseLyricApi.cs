using System.Collections.Generic;
using BepInEx.Logging;
using ChillPatcher.SDK.Interfaces;

namespace ChillPatcher.Module.Netease
{
    /// <summary>
    /// 网易云歌词 API：通过 chill.custom.get("lyric_netease") 访问
    /// 前端传入歌曲 UUID，内部通过 songInfoMap 查找 songId，再调用桥接获取歌词
    /// </summary>
    public class NeteaseLyricApi : ICustomJSApi
    {
        public string Name => "lyric_netease";

        private readonly ManualLogSource _logger;
        private readonly NeteaseBridge _bridge;
        private readonly Dictionary<string, NeteaseBridge.SongInfo> _songInfoMap;

        public NeteaseLyricApi(
            NeteaseBridge bridge,
            Dictionary<string, NeteaseBridge.SongInfo> songInfoMap,
            ManualLogSource logger)
        {
            _logger = logger;
            _bridge = bridge;
            _songInfoMap = songInfoMap;
        }

        /// <summary>
        /// 获取歌词（返回原始 LRC 文本，非 base64）
        /// </summary>
        /// <param name="uuid">歌曲 UUID</param>
        public string getSongLyric(string uuid)
        {
            if (_bridge == null)
            {
                _logger?.LogWarning("[LyricNeteaseApi] Bridge not available");
                return null;
            }

            if (_songInfoMap == null)
            {
                _logger?.LogWarning("[LyricNeteaseApi] SongInfoMap not available");
                return null;
            }

            try
            {
                if (!_songInfoMap.TryGetValue(uuid, out var songInfo))
                {
                    _logger?.LogWarning($"[LyricNeteaseApi] UUID not found: {uuid} (map size={_songInfoMap.Count})");
                    return null;
                }

                var songId = songInfo.Id;
                _logger?.LogInfo($"[LyricNeteaseApi] Getting lyric for songId={songId} (uuid={uuid})");

                var result = _bridge.GetSongLyric(songId);

                if (result == null)
                {
                    _logger?.LogWarning($"[LyricNeteaseApi] GetSongLyric returned null for songId={songId}");
                    return null;
                }

                _logger?.LogInfo($"[LyricNeteaseApi] Got lyric for songId={songId}, length={result.Length}");
                return result;
            }
            catch (System.Exception ex)
            {
                _logger?.LogError($"[LyricNeteaseApi] getSongLyric error: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }
    }
}
