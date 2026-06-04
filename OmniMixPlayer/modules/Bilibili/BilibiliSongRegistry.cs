using System.Collections.Generic;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Protos.Models;

namespace OmniMixPlayer.Module.Bilibili
{
    public class BilibiliSongRegistry
    {
        private readonly IModuleContext _context;
        private readonly string _moduleId;

        public const string PLAYLIST_LOGIN = "bili_login";
        public const string UUID_LOGIN = "bili_login_action";

        public BilibiliSongRegistry(IModuleContext context, string moduleId)
        {
            _context = context;
            _moduleId = moduleId;
        }

        public void RegisterLoginSong(string statusText)
        {
            _context.Library.UpsertPlaylist(new Playlist
            {
                Id = PLAYLIST_LOGIN,
                Name = "Bilibili 登录",
                ModuleId = _moduleId,
                Kind = PlaylistKind.System
            });

            var track = new Track
            {
                Uuid = UUID_LOGIN,
                Title = "B站扫码登录",
                Artist = statusText,
                SourceType = SourceType.Stream,
                SourcePath = "login_trigger",
                Duration = 120,
                ModuleId = _moduleId,
                IsFavorite = false
            };
            _context.Library.UpsertTrack(track);

            _context.Library.ReplacePlaylistEntries(PLAYLIST_LOGIN,
                new[] { new PlaylistEntrySpec { TrackUuid = UUID_LOGIN, Position = 0 } });
        }

        public void UpdateLoginSongTitle(string newStatus)
        {
            var track = _context.Library.GetTrack(UUID_LOGIN);
            if (track != null)
            {
                track.Artist = newStatus;
                _context.Library.UpsertTrack(track);
            }
            else
            {
                RegisterLoginSong(newStatus);
            }
        }

        public void RegisterFolder(BiliFolder folder, List<BiliVideoInfo> videos)
        {
            string playlistId = $"bili_playlist_{folder.Id}";
            _context.Library.UpsertPlaylist(new Playlist
            {
                Id = playlistId,
                Name = folder.Title,
                ModuleId = _moduleId,
                Kind = PlaylistKind.Imported
            });

            var entries = new List<PlaylistEntrySpec>();
            int position = 0;

            foreach (var v in videos)
            {
                var uuid = GenerateUuid(v.Bvid);

                var track = new Track
                {
                    Uuid = uuid,
                    Title = v.Title,
                    Artist = v.Artist,
                    SourceType = SourceType.Stream,
                    SourcePath = v.Bvid,
                    Duration = v.Duration,
                    ModuleId = _moduleId,
                    CoverUri = v.CoverUrl ?? ""
                };
                _context.Library.UpsertTrack(track);

                entries.Add(new PlaylistEntrySpec { TrackUuid = uuid, Position = position++ });
            }

            _context.Library.ReplacePlaylistEntries(playlistId, entries);
        }

        public static string GenerateUuid(string bvid)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"bili_{bvid}"));
            return new System.Guid(hash).ToString("N");
        }
    }
}
