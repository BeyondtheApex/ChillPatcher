using BepInEx.Logging;
using ChillPatcher.SDK.Interfaces;

namespace ChillPatcher.Module.QQMusic
{
    /// <summary>
    /// QQ 音乐歌词 API：通过 chill.custom.get("lyric") 访问
    /// 提供 QQ 音乐歌词获取功能
    /// </summary>
    public class QQMusicLyricApi : ICustomJSApi
    {
        public string Name => "lyric";

        private readonly ManualLogSource _logger;
        private readonly QQMusicBridge _bridge;

        public QQMusicLyricApi(QQMusicBridge bridge, ManualLogSource logger)
        {
            _logger = logger;
            _bridge = bridge;
        }

        /// <summary>
        /// 获取歌词（返回 base64 编码的 LRC 歌词字符串）
        /// </summary>
        public string getSongLyric(string songMid)
        {
            if (_bridge == null)
            {
                _logger?.LogWarning("[LyricApi] Bridge not available");
                return null;
            }

            try
            {
                return _bridge.GetSongLyric(songMid);
            }
            catch (System.Exception ex)
            {
                _logger?.LogError($"[LyricApi] GetSongLyric error: {ex.Message}");
                return null;
            }
        }
    }
}
