using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Models;

namespace OmniMixPlayer.Module.LocalFolder.Services.Scanner
{
    /// <summary>
    /// 文件夹扫描器
    /// 
    /// 目录结构:
    /// 根目�?
    /// ├── 歌单目录A/ �?作为歌单
    /// �?  ├── 散装歌曲.mp3 �?默认专辑 (歌单名称)
    /// �?  └── 专辑目录/ �?作为专辑
    /// �?      ├── 歌曲1.mp3
    /// �?      └── 子目�? �?扫描两层
    /// �?          └── 歌曲2.mp3
    /// ├── 歌单目录B/ �?作为歌单
    /// └── 散装歌曲.mp3 �?移动�?default 歌单文件�?
    /// </summary>
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
                _logger.LogWarning($"扫描目录不存�? {_rootPath}");
                return result;
            }

            // 第一步：处理根目录散装文件，移动�?default 文件�?
            MoveRootLooseFilesToDefault();

            // 第二步：扫描根目录下的子目录作为歌单
            await ScanPlaylistDirectoriesAsync(result);

            return result;
        }

        /// <summary>
        /// 将根目录散装音频文件移动�?default 文件�?
        /// </summary>
        private void MoveRootLooseFilesToDefault()
        {
            var looseFiles = AudioFileHelper.GetAudioFiles(_rootPath).ToList();
            if (!looseFiles.Any())
                return;

            var defaultPath = Path.Combine(_rootPath, DEFAULT_PLAYLIST_FOLDER);
            
            // 创建 default 文件�?
            if (!Directory.Exists(defaultPath))
            {
                Directory.CreateDirectory(defaultPath);
                _logger.LogInformation($"创建 default 歌单文件�? {defaultPath}");
            }

            // 移动文件
            int movedCount = 0;
            foreach (var filePath in looseFiles)
            {
                try
                {
                    var fileName = Path.GetFileName(filePath);
                    var destPath = Path.Combine(defaultPath, fileName);

                    // 如果目标文件已存在，添加编号
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
                    _logger.LogDebug($"移动文件: {fileName} -> default/");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"移动文件失败 '{filePath}': {ex.Message}");
                }
            }

            if (movedCount > 0)
            {
                _logger.LogInformation($"已将 {movedCount} 个散装音频文件移动到 default 文件夹");
                // 删除 default 文件夹的 rescan 标志，确保重新扫�?
                _rescanFlagManager.DeleteRescanFlag(defaultPath);
            }
        }

        /// <summary>
        /// 扫描歌单目录
        /// </summary>
        private async Task ScanPlaylistDirectoriesAsync(ScanResult result)
        {
            var playlistDirs = Directory.GetDirectories(_rootPath);

            foreach (var playlistDir in playlistDirs)
            {
                var playlistName = Path.GetFileName(playlistDir);
                var tagId = $"local_{playlistName}";

                // 检查是否需要重新扫�?
                bool needRescan = _forceRescan || _rescanFlagManager.NeedsRescan(playlistDir);

                // 读取显示名称
                var displayName = MetadataReader.ReadPlaylistName(playlistDir) ?? playlistName;

                var playlist = new PlaylistInfo
                {
                    TagId = tagId,
                    DisplayName = displayName,
                    DirectoryPath = playlistDir
                };
                result.Playlists.Add(playlist);

                // 尝试从缓存加�?
                bool loadedFromCache = false;
                if (!needRescan)
                {
                    _logger.LogDebug($"尝试从缓存加�? {displayName}");
                    loadedFromCache = _cacheManager.LoadFromCache(tagId, displayName, playlistDir, result);
                }

                if (!loadedFromCache)
                {
                    _logger.LogDebug($"扫描歌单: {displayName}");
                    await ScanSinglePlaylistAsync(playlistDir, tagId, displayName, result);

                    // 保存缓存并创建标志文�?
                    _cacheManager.SaveToCache(tagId, displayName, playlistDir, result);
                    _rescanFlagManager.CreateRescanFlag(playlistDir);
                }
            }
        }

        /// <summary>
        /// 扫描单个歌单
        /// </summary>
        private async Task ScanSinglePlaylistAsync(string playlistDir, string tagId, string playlistDisplayName, ScanResult result)
        {
            var albumDirs = Directory.GetDirectories(playlistDir);

            // 自动创建 playlist.json（如果不存在�?
            MetadataReader.EnsurePlaylistMetadata(playlistDir, playlistDisplayName);

            // 扫描子目录作为专�?
            foreach (var albumDir in albumDirs)
            {
                var albumName = Path.GetFileName(albumDir);
                var albumId = $"{tagId}_{albumName}";
                var albumDisplayName = MetadataReader.ReadAlbumName(albumDir) ?? albumName;
                var albumArtist = MetadataReader.ReadAlbumArtist(albumDir);

                // 扫描专辑内的音频文件（递归两层�?
                var audioFiles = AudioFileHelper.GetAudioFilesRecursive(albumDir, 1);
                var musicList = new System.Collections.Generic.List<MusicInfo>();
                
                foreach (var file in audioFiles)
                {
                    var music = CreateMusicInfo(file, tagId, albumId);
                    musicList.Add(music);
                    result.Music.Add(music);
                }

                // 如果 album.json 没有艺术家，从第一首歌获取
                if (string.IsNullOrEmpty(albumArtist) && musicList.Count > 0)
                {
                    albumArtist = musicList[0].Artist;
                }

                // 自动创建 album.json（如果不存在�?
                MetadataReader.EnsureAlbumMetadata(albumDir, albumDisplayName, albumArtist);

                var album = new AlbumInfo
                {
                    AlbumId = albumId,
                    DisplayName = albumDisplayName,
                    Artist = albumArtist,
                    TagId = tagId,
                    DirectoryPath = albumDir,
                    CoverPath = albumDir
                };
                result.Albums.Add(album);
            }

            // 扫描歌单目录下的散装音频（归入默认专辑，使用歌单名称�?
            var looseAudioFiles = AudioFileHelper.GetAudioFiles(playlistDir).ToList();
            if (looseAudioFiles.Any())
            {
                var defaultAlbumId = $"{tagId}_other";

                foreach (var file in looseAudioFiles)
                {
                    var music = CreateMusicInfo(file, tagId, defaultAlbumId);
                    result.Music.Add(music);
                }

                var defaultAlbum = new AlbumInfo
                {
                    AlbumId = defaultAlbumId,
                    DisplayName = playlistDisplayName,  // 使用歌单名称而非"其他"
                    TagId = tagId,
                    DirectoryPath = playlistDir,
                    CoverPath = playlistDir,
                    IsDefault = true
                    // 默认专辑不需要艺术家
                };
                result.Albums.Add(defaultAlbum);
            }
        }

        private MusicInfo CreateMusicInfo(string filePath, string tagId, string albumId)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            // 基于相对路径生成 UUID，确保目录迁移不影响 UUID
            var relativePath = GetRelativePath(filePath);
            var uuid = GenerateUUIDFromRelativePath(relativePath);

            // 使用 TagLib 读取元数�?
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

            return new MusicInfo
            {
                UUID = uuid,
                Title = title,
                Artist = artist,
                AlbumId = albumId,
                TagId = tagId,
                SourceType = MusicSourceType.File,
                SourcePath = filePath,
                Duration = duration,
                CoverUrl = filePath,
                IsUnlocked = true
            };
        }

        /// <summary>
        /// 获取相对于根目录的路�?
        /// </summary>
        private string GetRelativePath(string filePath)
        {
            if (filePath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
            {
                var relative = filePath.Substring(_rootPath.Length);
                // 移除开头的路径分隔符并统一为正斜杠
                return relative.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Replace('\\', '/');
            }
            return filePath;
        }

        /// <summary>
        /// 基于相对路径生成确定�?UUID
        /// 这确保整个音乐库目录迁移�?UUID 保持不变
        /// </summary>
        private static string GenerateUUIDFromRelativePath(string relativePath)
        {
            // 使用 MD5 基于相对路径生成确定�?UUID
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(relativePath.ToLowerInvariant()));
                return new Guid(hash).ToString("N");
            }
        }
    }
}
