using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.Module.LocalFolder;
using OmniMixPlayer.SDK.Protos.Models;

namespace OmniMixPlayer.Module.LocalFolder.Services.Scanner
{
    public class FolderScanner
    {
        private const string DEFAULT_PLAYLIST_FOLDER = "default";

        private string _rootPath;
        private readonly bool _forceRescan;
        private readonly ILogger _logger;

        private readonly RescanFlagManager _rescanFlagManager;
        private readonly CacheManager _cacheManager;

        public FolderScanner(
            string rootPath,
            bool forceRescan,
            LocalDatabase database,
            ILogger logger)
        {
            _rootPath = rootPath;
            _forceRescan = forceRescan;
            _logger = logger;

            _rescanFlagManager = new RescanFlagManager(logger);
            _cacheManager = new CacheManager(database, logger);
        }

        public void UpdateRootPath(string newPath)
        {
            _rootPath = newPath;
        }

        public async Task<ScanResult> ScanAsync()
        {
            var result = new ScanResult();

            if (!Directory.Exists(_rootPath))
            {
                _logger.LogWarning($"鎵弿鐩綍涓嶅瓨锟? {_rootPath}");
                return result;
            }

            // 绗竴姝ワ細澶勭悊鏍圭洰褰曟暎瑁呮枃浠讹紝绉诲姩锟?default 鏂囦欢锟?
            MoveRootLooseFilesToDefault();

            // 绗簩姝ワ細鎵弿鏍圭洰褰曚笅鐨勫瓙鐩綍浣滀负姝屽崟
            await ScanPlaylistDirectoriesAsync(result);

            return result;
        }

        /// <summary>
        /// 灏嗘牴鐩綍鏁ｈ闊抽鏂囦欢绉诲姩锟?default 鏂囦欢锟?
        /// </summary>
        private void MoveRootLooseFilesToDefault()
        {
            var looseFiles = AudioFileHelper.GetAudioFiles(_rootPath).ToList();
            if (!looseFiles.Any())
                return;

            var defaultPath = Path.Combine(_rootPath, DEFAULT_PLAYLIST_FOLDER);

            // 鍒涘缓 default 鏂囦欢锟?
            if (!Directory.Exists(defaultPath))
            {
                Directory.CreateDirectory(defaultPath);
                _logger.LogInformation($"鍒涘缓 default 姝屽崟鏂囦欢锟? {defaultPath}");
            }

            // 绉诲姩鏂囦欢
            int movedCount = 0;
            foreach (var filePath in looseFiles)
            {
                try
                {
                    var fileName = Path.GetFileName(filePath);
                    var destPath = Path.Combine(defaultPath, fileName);

                    // 濡傛灉鐩爣鏂囦欢宸插瓨鍦紝娣诲姞缂栧彿
                    if (File.Exists(destPath))
                    {
                        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        var ext = Path.GetExtension(fileName);
                        int counter = 1;
                        while (File.Exists(destPath))
                        {
                            destPath = Path.Combine(defaultPath, $"{nameWithoutExt}_{counter}{ext}");
                            counter++;
                        }
                    }

                    File.Move(filePath, destPath);
                    movedCount++;
                    _logger.LogDebug($"Move file: {fileName} -> default/");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to move '{filePath}': {ex.Message}");
                }
            }

            if (movedCount > 0)
            {
                _logger.LogInformation($"Moved {movedCount} loose audio files to default folder");
                _rescanFlagManager.DeleteRescanFlag(defaultPath);
            }
        }

        /// <summary>
        /// 鎵弿姝屽崟鐩綍
        /// </summary>
        private async Task ScanPlaylistDirectoriesAsync(ScanResult result)
        {
            var playlistDirs = Directory.GetDirectories(_rootPath);

            foreach (var playlistDir in playlistDirs)
            {
                var playlistName = Path.GetFileName(playlistDir);
                var tagId = $"local_{playlistName}";

                // 妫€鏌ユ槸鍚﹂渶瑕侀噸鏂版壂锟?
                bool needRescan = _forceRescan || _rescanFlagManager.NeedsRescan(playlistDir);

                // 璇诲彇鏄剧ず鍚嶇О
                var displayName = MetadataReader.ReadPlaylistName(playlistDir) ?? playlistName;

                var playlist = new PlaylistInfo
                {
                    TagId = tagId,
                    DisplayName = displayName,
                    DirectoryPath = playlistDir
                };
                result.Playlists.Add(playlist);

                // 灏濊瘯浠庣紦瀛樺姞锟?
                bool loadedFromCache = false;
                if (!needRescan)
                {
                    _logger.LogDebug($"灏濊瘯浠庣紦瀛樺姞锟? {displayName}");
                    loadedFromCache = _cacheManager.LoadFromCache(tagId, displayName, playlistDir, result);
                }

                if (!loadedFromCache)
                {
                    _logger.LogDebug($"鎵弿姝屽崟: {displayName}");
                    await ScanSinglePlaylistAsync(playlistDir, tagId, displayName, result);

                    // 淇濆瓨缂撳瓨骞跺垱寤烘爣蹇楁枃锟?
                    _cacheManager.SaveToCache(tagId, displayName, playlistDir, result);
                    _rescanFlagManager.CreateRescanFlag(playlistDir);
                }
            }
        }

        /// <summary>
        /// 鎵弿鍗曚釜姝屽崟
        /// </summary>
        private async Task ScanSinglePlaylistAsync(string playlistDir, string tagId, string playlistDisplayName, ScanResult result)
        {
            var albumDirs = Directory.GetDirectories(playlistDir);

            // 鑷姩鍒涘缓 playlist.json锛堝鏋滀笉瀛樺湪锟?
            MetadataReader.EnsurePlaylistMetadata(playlistDir, playlistDisplayName);

            // 鎵弿瀛愮洰褰曚綔涓轰笓锟?
            foreach (var albumDir in albumDirs)
            {
                var albumName = Path.GetFileName(albumDir);
                var albumId = $"{tagId}_{albumName}";
                var albumDisplayName = MetadataReader.ReadAlbumName(albumDir) ?? albumName;
                var albumArtist = MetadataReader.ReadAlbumArtist(albumDir);

                // 鎵弿涓撹緫鍐呯殑闊抽鏂囦欢锛堥€掑綊涓ゅ眰锟?
                var audioFiles = AudioFileHelper.GetAudioFilesRecursive(albumDir, 1);
                var musicList = new System.Collections.Generic.List<Track>();

                foreach (var file in audioFiles)
                {
                    var music = CreateTrack(file, tagId, albumId);
                    musicList.Add(music);
                    result.Music.Add(music);
                    result.AddPlaylistMembership(music.Uuid, tagId);
                }

                // 濡傛灉 album.json 娌℃湁鑹烘湳瀹讹紝浠庣涓€棣栨瓕鑾峰彇
                if (string.IsNullOrEmpty(albumArtist) && musicList.Count > 0)
                {
                    albumArtist = musicList[0].Artist;
                }

                // 鑷姩鍒涘缓 album.json锛堝鏋滀笉瀛樺湪锟?
                MetadataReader.EnsureAlbumMetadata(albumDir, albumDisplayName, albumArtist);

                var album = new Album
                {
                    Id = albumId,
                    Title = albumDisplayName,
                    Artist = albumArtist ?? "",
                    ModuleId = ModuleInfo.MODULE_ID
                };
                result.Albums.Add(album);
            }

            // 鎵弿姝屽崟鐩綍涓嬬殑鏁ｈ闊抽锛堝綊鍏ラ粯璁や笓杈戯紝浣跨敤姝屽崟鍚嶇О锟?
            var looseAudioFiles = AudioFileHelper.GetAudioFiles(playlistDir).ToList();
            if (looseAudioFiles.Any())
            {
                var defaultAlbumId = $"{tagId}_other";

                foreach (var file in looseAudioFiles)
                {
                    var music = CreateTrack(file, tagId, defaultAlbumId);
                    result.Music.Add(music);
                    result.AddPlaylistMembership(music.Uuid, tagId);
                }

                var defaultAlbum = new Album
                {
                    Id = defaultAlbumId,
                    Title = playlistDisplayName,
                    ModuleId = ModuleInfo.MODULE_ID
                };
                result.Albums.Add(defaultAlbum);
            }
        }

        private Track CreateTrack(string filePath, string tagId, string albumId)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            // 鍩轰簬鐩稿璺緞鐢熸垚 UUID锛岀‘淇濈洰褰曡縼绉讳笉褰卞搷 UUID
            var relativePath = GetRelativePath(filePath);
            var uuid = GenerateUUIDFromRelativePath(relativePath);

            // 浣跨敤 TagLib 璇诲彇鍏冩暟锟?
            string title = fileName;
            string artist = null;
            float duration = 0f;
            try
            {
                using (var tagFile = TagLib.File.Create(filePath))
                {
                    if (!string.IsNullOrEmpty(tagFile.Tag.Title))
                        title = tagFile.Tag.Title;
                    if (!string.IsNullOrEmpty(tagFile.Tag.FirstPerformer))
                        artist = tagFile.Tag.FirstPerformer;
                    if (tagFile.Properties?.Duration.TotalSeconds > 0)
                        duration = (float)tagFile.Properties.Duration.TotalSeconds;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Failed to read metadata from {filePath}: {ex.Message}");
            }

            return new Track
            {
                Uuid = uuid,
                Title = title,
                Artist = artist ?? "",
                AlbumId = albumId,
                SourceType = SourceType.File,
                SourcePath = filePath,
                Duration = duration,
                CoverUri = filePath
            };
        }

        /// <summary>
        /// 鑾峰彇鐩稿浜庢牴鐩綍鐨勮矾锟?
        /// </summary>
        private string GetRelativePath(string filePath)
        {
            if (filePath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
            {
                var relative = filePath.Substring(_rootPath.Length);
                // 绉婚櫎寮€澶寸殑璺緞鍒嗛殧绗﹀苟缁熶竴涓烘鏂滄潬
                return relative.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Replace('\\', '/');
            }
            return filePath;
        }

        /// <summary>
        /// 鍩轰簬鐩稿璺緞鐢熸垚纭畾锟?UUID
        /// 杩欑‘淇濇暣涓煶涔愬簱鐩綍杩佺Щ锟?UUID 淇濇寔涓嶅彉
        /// </summary>
        private static string GenerateUUIDFromRelativePath(string relativePath)
        {
            // 浣跨敤 MD5 鍩轰簬鐩稿璺緞鐢熸垚纭畾锟?UUID
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(relativePath.ToLowerInvariant()));
                return new Guid(hash).ToString("N");
            }
        }
    }
}

