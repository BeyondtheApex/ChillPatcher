using BepInEx.Logging;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 网易云歌词 API：通过 chill.custom.get("lyric_netease") 访问
    /// 前端传入歌曲 UUID，内部通过 songInfoMap 查找 songId，再调用桥接获取歌词
    /// </summary>
    public class ChillLyricNeteaseApi : ICustomJSApi
    {
        public string Name => "lyric_netease";

        private readonly ManualLogSource _logger;
        private readonly object _bridge;
        private readonly System.Reflection.MethodInfo _getSongLyricMethod;
        private readonly System.Collections.IDictionary _songInfoMap;
        private readonly System.Reflection.PropertyInfo _songIdProperty;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="bridge">NeteaseBridge 实例（有 GetSongLyric(long) 方法）</param>
        /// <param name="songInfoMap">UUID → SongInfo 映射（Dictionary&lt;string, SongInfo&gt;）</param>
        /// <param name="logger">日志</param>
        public ChillLyricNeteaseApi(object bridge, object songInfoMap, ManualLogSource logger)
        {
            _logger = logger;
            _bridge = bridge;
            _songInfoMap = songInfoMap as System.Collections.IDictionary;

            // GetSongLyric(long songId) on the bridge
            var bridgeType = bridge.GetType();
            _getSongLyricMethod = bridgeType.GetMethod("GetSongLyric");

            // Cache the Id property from SongInfo type
            // Find it from the first value in the dictionary, or from the dictionary's generic type argument
            if (_songInfoMap != null)
            {
                var dictType = songInfoMap.GetType();
                var genericArgs = dictType.GetGenericArguments();
                if (genericArgs.Length == 2)
                {
                    _songIdProperty = genericArgs[1].GetProperty("Id");
                }
            }
        }

        /// <summary>
        /// 获取歌词（返回原始 LRC 文本，非 base64）
        /// </summary>
        /// <param name="uuid">歌曲 UUID</param>
        public string getSongLyric(string uuid)
        {
            if (_bridge == null || _getSongLyricMethod == null)
            {
                _logger?.LogWarning("[LyricNeteaseApi] Bridge or method not available");
                return null;
            }

            if (_songInfoMap == null || _songIdProperty == null)
            {
                _logger?.LogWarning("[LyricNeteaseApi] SongInfoMap not available");
                return null;
            }

            try
            {
                // Look up UUID in songInfoMap to get SongInfo
                if (!_songInfoMap.Contains(uuid))
                {
                    _logger?.LogDebug($"[LyricNeteaseApi] UUID not found in songInfoMap: {uuid}");
                    return null;
                }

                var songInfo = _songInfoMap[uuid];
                var songId = (long)_songIdProperty.GetValue(songInfo);

                _logger?.LogDebug($"[LyricNeteaseApi] Getting lyric for songId={songId} (uuid={uuid})");

                var result = _getSongLyricMethod.Invoke(_bridge, new object[] { songId });
                return result as string;
            }
            catch (System.Exception ex)
            {
                _logger?.LogError($"[LyricNeteaseApi] getSongLyric error: {ex.Message}");
                return null;
            }
        }
    }
}
