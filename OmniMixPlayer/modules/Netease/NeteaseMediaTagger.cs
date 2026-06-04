using System;
using System.IO;
using System.Net.Http;
using System.Text;

using Microsoft.Extensions.Logging;
using TagLib;

namespace OmniMixPlayer.Module.Netease
{
    /// <summary>
    /// 为下载的音乐文件补充 ID3v2 标签和 LRC 歌词
    /// </summary>
    public static class NeteaseMediaTagger
    {
        private const string LogTag = "[NeteaseMediaTagger] ";

        /// <summary>
        /// 检查并补充音频文件的标签（Title, Artists, Album, Cover）
        /// 已有标签的字段不覆盖。
        /// </summary>
        public static void EnsureTags(string filePath, NeteaseBridge.SongInfo songInfo, ILogger logger, string lyricText = null)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                    return;

                using (var file = TagLib.File.Create(filePath))
                {
                    var tag = file.Tag;
                    bool anyWritten = false;

                    if (string.IsNullOrEmpty(tag.Title))
                    {
                        tag.Title = songInfo.Name;
                        anyWritten = true;
                    }

                    if (tag.Performers == null || tag.Performers.Length == 0)
                    {
                        tag.Performers = songInfo.Artists.ToArray();
                        anyWritten = true;
                    }

                    if (string.IsNullOrEmpty(tag.Album))
                    {
                        tag.Album = songInfo.Album;
                        anyWritten = true;
                    }

                    if (tag.Pictures == null || tag.Pictures.Length == 0)
                    {
                        if (!string.IsNullOrEmpty(songInfo.CoverUrl))
                        {
                            try
                            {
                                byte[] coverData;
                                using (var client = new HttpClient())
                                {
                                    coverData = client.GetByteArrayAsync(songInfo.CoverUrl).GetAwaiter().GetResult();
                                }

                                var mime = songInfo.CoverUrl.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                                        || songInfo.CoverUrl.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                                    ? "image/jpeg"
                                    : "image/png";

                                var picture = new Picture(new ByteVector(coverData))
                                {
                                    Type = PictureType.FrontCover,
                                    MimeType = mime
                                };

                                tag.Pictures = new IPicture[] { picture };
                                anyWritten = true;
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(LogTag + "Cover download failed: " + ex.Message);
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(tag.Lyrics) && !string.IsNullOrEmpty(lyricText))
                    {
                        tag.Lyrics = lyricText;
                        anyWritten = true;
                    }

                    if (anyWritten)
                    {
                        file.Save();
                        logger.LogInformation(LogTag + "Tags written: " + songInfo.Name);
                    }
                    else
                    {
                        logger.LogDebug(LogTag + "Tags already complete: " + songInfo.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(LogTag + "EnsureTags failed for " + filePath + ": " + ex.Message);
            }
        }

        /// <summary>
        /// 检查并保存 LRC 歌词文件（同目录同名.lrc）
        /// 已存在则跳过。
        /// </summary>
        public static void EnsureLyrics(string filePath, long songId, NeteaseBridge bridge, ILogger logger, string lyricText = null)
        {
            try
            {
                var lrcPath = Path.ChangeExtension(filePath, ".lrc");

                if (System.IO.File.Exists(lrcPath))
                {
                    logger.LogDebug(LogTag + "Lyrics already exist: " + lrcPath);
                    return;
                }

                if (string.IsNullOrEmpty(lyricText))
                    lyricText = bridge.GetSongLyric(songId);

                if (string.IsNullOrEmpty(lyricText))
                {
                    logger.LogDebug(LogTag + "No lyrics available for songId " + songId);
                    return;
                }

                System.IO.File.WriteAllText(lrcPath, lyricText, Encoding.UTF8);
                logger.LogInformation(LogTag + "Lyrics saved: " + lrcPath);
            }
            catch (Exception ex)
            {
                logger.LogWarning(LogTag + "EnsureLyrics failed for " + filePath + ": " + ex.Message);
            }
        }
    }
}
