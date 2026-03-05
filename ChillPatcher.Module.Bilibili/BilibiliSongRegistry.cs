using System.Collections.Generic;
using ChillPatcher.SDK.Interfaces;
using ChillPatcher.SDK.Models;

namespace ChillPatcher.Module.Bilibili
{
    public class BilibiliSongRegistry
    {
        private readonly IModuleContext _context;
        private readonly string _moduleId;

        public const string TAG_LOGIN = "bili_login_tag";
        public const string ALBUM_LOGIN = "bili_login_album";
        public const string UUID_LOGIN = "bili_login_action";

        public BilibiliSongRegistry(IModuleContext context, string moduleId)
        {
            _context = context;
            _moduleId = moduleId;
        }

        public void RegisterLoginSong(string statusText)
        {
            var music = new MusicInfo
            {
                UUID = UUID_LOGIN,
                Title = "点击此处扫码登录",
                Artist = statusText,
                AlbumId = ALBUM_LOGIN,
                TagId = TAG_LOGIN,
                SourceType = MusicSourceType.Stream,
                SourcePath = "login_trigger",
                Duration = 60,
                ModuleId = _moduleId,
                IsFavorite = false
            };

            _context.TagRegistry.RegisterTag(TAG_LOGIN, "Bilibili 登录", _moduleId);
            _context.AlbumRegistry.RegisterAlbum(new AlbumInfo
            {
                AlbumId = ALBUM_LOGIN,
                DisplayName = "登录入口",
                TagIds = new List<string> { TAG_LOGIN },
                ModuleId = _moduleId
            }, _moduleId);

            _context.MusicRegistry.RegisterMusic(music, _moduleId);
        }

        public void UpdateLoginSongTitle(string newStatus)
        {
            var music = _context.MusicRegistry.GetMusic(UUID_LOGIN);
            if (music != null)
            {
                music.Artist = newStatus;
                try
                {
                    var method = _context.MusicRegistry.GetType().GetMethod("UpdateMusic");
                    if (method != null) method.Invoke(_context.MusicRegistry, new object[] { music });
                    else RegisterLoginSong(newStatus);
                }
                catch { RegisterLoginSong(newStatus); }
            }
            else RegisterLoginSong(newStatus);
        }

        public void RegisterFolder(BiliFolder folder, List<BiliVideoInfo> videos)
        {
            string tagId = $"bili_fav_{folder.Id}";
            string albumId = $"bili_album_{folder.Id}";

            _context.TagRegistry.RegisterTag(tagId, folder.Title, _moduleId);

            var album = new AlbumInfo
            {
                AlbumId = albumId,
                DisplayName = folder.Title,
                Artist = "Bilibili",
                TagIds = new List<string> { tagId },
                ModuleId = _moduleId,
                SongCount = videos.Count
            };
            _context.AlbumRegistry.RegisterAlbum(album, _moduleId);

            foreach (var v in videos)
            {
                var music = new MusicInfo
                {
                    UUID = MusicInfo.GenerateUUID("bili_" + v.Bvid),
                    Title = v.Title,
                    Artist = v.Artist,
                    AlbumId = albumId,
                    TagId = tagId,
                    SourceType = MusicSourceType.Stream,
                    SourcePath = v.Bvid,
                    Duration = v.Duration,
                    ModuleId = _moduleId,
                    ExtendedData = v.CoverUrl
                };
                _context.MusicRegistry.RegisterMusic(music, _moduleId);
            }
        }
    }
}