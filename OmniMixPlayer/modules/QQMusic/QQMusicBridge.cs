using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OmniMixPlayer.Module.QQMusic
{
    /// <summary>
    /// P/Invoke bridge to the native QQ Music Go DLL
    /// </summary>
    public class QQMusicBridge
    {
        private const string DLL_NAME = "ChillQQMusic";
        private readonly ILogger _logger;
        private bool _initialized;

        #region Native P/Invoke Declarations

        // Initialization
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int QQMusicInit(string dataDir);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int QQMusicIsLoggedIn();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr QQMusicGetUserInfo();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int QQMusicSetCookie(string cookie);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int QQMusicRefreshLogin();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int QQMusicLogout();

        // Songs & Playlists
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr QQMusicGetLikeSongs(int getAll);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr QQMusicGetUserPlaylists();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr QQMusicGetPlaylistSongs(long playlistId);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr QQMusicGetSongURL(string songMid, string quality);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr QQMusicGetSongInfo(string songMid);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int QQMusicLikeSong(string songMid, int like);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr QQMusicSearchSongs(string keyword, int page, int pageSize);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr QQMusicGetSongLyric(string songMid);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr QQMusicGetRecommendSongs();

        // QR Login
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr QQMusicQRGetImage(string loginType);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr QQMusicQRCheckStatus();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void QQMusicQRCancelLogin();

        // Utility
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void QQMusicFreeString(IntPtr ptr);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void QQMusicFreeBytes(IntPtr ptr);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr QQMusicGetLastError();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int QQMusicClearCache();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr QQMusicGetCacheDir();

        #endregion

        #region Data Types

        public class UserInfo
        {
            [JsonProperty("uin")] public long UIN { get; set; }
            [JsonProperty("nickname")] public string Nickname { get; set; }
            [JsonProperty("avatarUrl")] public string AvatarUrl { get; set; }
            [JsonProperty("vipType")] public int VipType { get; set; }
        }

        public class QRLoginState
        {
            [JsonProperty("code")] public int Code { get; set; }
            [JsonProperty("msg")] public string Msg { get; set; }
            [JsonProperty("nickname")] public string Nickname { get; set; }

            public bool IsWaitingScan => Code == 66;
            public bool IsWaitingConfirm => Code == 67;
            public bool IsSuccess => Code == 0;
            public bool IsExpired => Code == 65;
        }

        public class SongInfo
        {
            [JsonProperty("mid")] public string Mid { get; set; }
            [JsonProperty("id")] public long Id { get; set; }
            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("duration")] public double Duration { get; set; }
            [JsonProperty("artists")] public List<string> Artists { get; set; }
            [JsonProperty("album")] public string Album { get; set; }
            [JsonProperty("albumMid")] public string AlbumMid { get; set; }
            [JsonProperty("coverUrl")] public string CoverUrl { get; set; }
            [JsonProperty("file")] public SongFile File { get; set; }

            public string ArtistString => Artists != null ? string.Join(", ", Artists) : "";
        }

        public class SongFile
        {
            [JsonProperty("mediaMid")] public string MediaMid { get; set; }
            [JsonProperty("size128")] public long Size128 { get; set; }
            [JsonProperty("size320")] public long Size320 { get; set; }
            [JsonProperty("sizeFlac")] public long SizeFlac { get; set; }
            [JsonProperty("sizeHRes")] public long SizeHRes { get; set; }
        }

        public class PlaylistInfo
        {
            [JsonProperty("dissId")] public long DissID { get; set; }
            [JsonProperty("dissName")] public string DissName { get; set; }
            [JsonProperty("songCount")] public int SongCount { get; set; }
            [JsonProperty("coverUrl")] public string CoverUrl { get; set; }
            [JsonProperty("creator")] public string Creator { get; set; }
            [JsonProperty("description")] public string Description { get; set; }
        }

        public class PlaylistDetail
        {
            [JsonProperty("dissId")] public long DissID { get; set; }
            [JsonProperty("dissName")] public string DissName { get; set; }
            [JsonProperty("songCount")] public int SongCount { get; set; }
            [JsonProperty("coverUrl")] public string CoverUrl { get; set; }
            [JsonProperty("songs")] public List<SongInfo> Songs { get; set; }
        }

        public class SongURL
        {
            [JsonProperty("mid")] public string Mid { get; set; }
            [JsonProperty("url")] public string URL { get; set; }
            [JsonProperty("quality")] public string Quality { get; set; }
            [JsonProperty("format")] public string Format { get; set; }
            [JsonProperty("size")] public long Size { get; set; }
        }

        public class UserPlaylists
        {
            [JsonProperty("created")] public List<PlaylistInfo> Created { get; set; }
            [JsonProperty("collected")] public List<PlaylistInfo> Collected { get; set; }
        }

        public class SearchResult
        {
            [JsonProperty("songs")] public List<SongInfo> Songs { get; set; }
            [JsonProperty("total")] public int Total { get; set; }
        }

        public enum AudioQuality
        {
            Standard, // 128kbps
            HQ,       // 320kbps
            SQ,       // FLAC
            HiRes     // Hi-Res
        }

        #endregion

        public QQMusicBridge(ILogger logger)
        {
            _logger = logger;
        }

        #region Helper Methods

        private string ReadAndFreeString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return null;
            try
            {
                return Marshal.PtrToStringUTF8(ptr);
            }
            finally
            {
                QQMusicFreeString(ptr);
            }
        }

        private T ParseJson<T>(IntPtr ptr) where T : class
        {
            var json = ReadAndFreeString(ptr);
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"JSON parse error: {ex.Message}");
                return null;
            }
        }

        private string GetQualityString(AudioQuality quality)
        {
            return quality switch
            {
                AudioQuality.Standard => "128",
                AudioQuality.HQ => "320",
                AudioQuality.SQ => "flac",
                AudioQuality.HiRes => "hires",
                _ => "320"
            };
        }

        public string GetLastError()
        {
            return ReadAndFreeString(QQMusicGetLastError());
        }

        #endregion

        #region QR Login API

        /// <summary>
        /// 获取QR码图片（base64编码的PNG）
        /// </summary>
        /// <param name="loginType">登录类型: "qq" 或 "wx"</param>
        public string GetQRImage(string loginType = "qq")
        {
            try
            {
                return ReadAndFreeString(QQMusicQRGetImage(loginType));
            }
            catch (Exception ex)
            {
                _logger?.LogError($"GetQRImage error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 检查QR扫码状态
        /// </summary>
        public QRLoginState CheckQRStatus()
        {
            try
            {
                var json = ReadAndFreeString(QQMusicQRCheckStatus());
                if (string.IsNullOrEmpty(json))
                {
                    _logger?.LogWarning($"CheckQRStatus returned null, lastError: {GetLastError()}");
                    return null;
                }
                return JsonConvert.DeserializeObject<QRLoginState>(json);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"CheckQRStatus error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 取消QR登录
        /// </summary>
        public void CancelQRLogin()
        {
            try
            {
                QQMusicQRCancelLogin();
            }
            catch (Exception ex)
            {
                _logger?.LogError($"CancelQRLogin error: {ex.Message}");
            }
        }

        #endregion

        #region Public API

        public bool Initialize(string dataDir)
        {
            try
            {
                var result = QQMusicInit(dataDir);
                _initialized = result == 0;
                if (!_initialized)
                {
                    _logger?.LogError($"QQMusicInit failed: {GetLastError()}");
                }
                return _initialized;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Initialize error: {ex.Message}");
                return false;
            }
        }

        public bool IsLoggedIn => _initialized && QQMusicIsLoggedIn() != 0;

        public UserInfo GetUserInfo()
        {
            return ParseJson<UserInfo>(QQMusicGetUserInfo());
        }

        public bool SetCookie(string cookie)
        {
            return QQMusicSetCookie(cookie) == 0;
        }

        public bool RefreshLogin()
        {
            return QQMusicRefreshLogin() == 0;
        }

        public void Logout()
        {
            QQMusicLogout();
        }

        // Songs & Playlists
        public List<SongInfo> GetLikeSongs(bool getAll = true)
        {
            return ParseJson<List<SongInfo>>(QQMusicGetLikeSongs(getAll ? 1 : 0));
        }

        public UserPlaylists GetUserPlaylists()
        {
            return ParseJson<UserPlaylists>(QQMusicGetUserPlaylists());
        }

        public PlaylistDetail GetPlaylistSongs(long playlistId)
        {
            return ParseJson<PlaylistDetail>(QQMusicGetPlaylistSongs(playlistId));
        }

        public SongURL GetSongURL(string songMid, AudioQuality quality = AudioQuality.HQ)
        {
            return ParseJson<SongURL>(QQMusicGetSongURL(songMid, GetQualityString(quality)));
        }

        public SongInfo GetSongInfo(string songMid)
        {
            return ParseJson<SongInfo>(QQMusicGetSongInfo(songMid));
        }

        public bool LikeSong(string songMid, bool like)
        {
            return QQMusicLikeSong(songMid, like ? 1 : 0) == 0;
        }

        public SearchResult SearchSongs(string keyword, int page = 1, int pageSize = 30)
        {
            return ParseJson<SearchResult>(QQMusicSearchSongs(keyword, page, pageSize));
        }

        public string GetSongLyric(string songMid)
        {
            return ReadAndFreeString(QQMusicGetSongLyric(songMid));
        }

        public List<SongInfo> GetRecommendSongs()
        {
            return ParseJson<List<SongInfo>>(QQMusicGetRecommendSongs());
        }

        // Utility
        public bool ClearCache()
        {
            return QQMusicClearCache() == 0;
        }

        public string GetCacheDir()
        {
            return ReadAndFreeString(QQMusicGetCacheDir());
        }

        #endregion
    }
}
