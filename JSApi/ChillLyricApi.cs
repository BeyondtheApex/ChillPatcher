using BepInEx.Logging;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 歌词 API：通过 chill.custom.get("lyric") 访问
    /// 提供 QQ 音乐歌词获取功能
    /// </summary>
    public class ChillLyricApi : ICustomJSApi
    {
        public string Name => "lyric";

        private readonly ManualLogSource _logger;
        private readonly object _bridge;
        private readonly System.Reflection.MethodInfo _getSongLyricMethod;

        public ChillLyricApi(object bridge, ManualLogSource logger)
        {
            _logger = logger;
            _bridge = bridge;

            // Use reflection to call GetSongLyric on the bridge object
            // This avoids a direct dependency on the QQMusic module
            var bridgeType = bridge.GetType();
            _getSongLyricMethod = bridgeType.GetMethod("GetSongLyric");
        }

        /// <summary>
        /// 获取歌词（返回 base64 编码的 LRC 歌词字符串）
        /// </summary>
        public string getSongLyric(string songMid)
        {
            if (_bridge == null || _getSongLyricMethod == null)
            {
                _logger?.LogWarning("[LyricApi] Bridge or method not available");
                return null;
            }

            try
            {
                var result = _getSongLyricMethod.Invoke(_bridge, new object[] { songMid });
                return result as string;
            }
            catch (System.Exception ex)
            {
                _logger?.LogError($"[LyricApi] GetSongLyric error: {ex.Message}");
                return null;
            }
        }
    }
}
