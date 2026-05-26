using System;
using System.IO;
using OmniMixPlayer.SDK.Interfaces;

namespace OmniMixPlayer.Backend.ModuleSystem.Services
{
    public class DefaultCoverProvider : IDefaultCoverProvider
    {
        private static DefaultCoverProvider _instance;
        public static DefaultCoverProvider Instance => _instance;

        private byte[] _defaultMusicCover;
        private byte[] _defaultAlbumCover;
        private byte[] _localMusicCover;

        public byte[] DefaultMusicCover => _defaultMusicCover;
        public byte[] DefaultAlbumCover => _defaultAlbumCover;
        public byte[] LocalMusicCover => _localMusicCover;

        public static void Initialize()
        {
            if (_instance != null) return;
            _instance = new DefaultCoverProvider();
            _instance.LoadDefaultCovers();
        }

        private DefaultCoverProvider() { }

        private void LoadDefaultCovers()
        {
            _defaultMusicCover = LoadResourceBytes("defaultcover.png")
                ?? CreatePlaceholderCover(300, 300);
            _defaultAlbumCover = _defaultMusicCover;
            _localMusicCover = LoadResourceBytes("localcover.jpg")
                ?? CreatePlaceholderCover(300, 300);
        }

        private byte[] LoadResourceBytes(string resourceName)
        {
            try
            {
                var assembly = typeof(DefaultCoverProvider).Assembly;
                var resourceFullName = "OmniMixPlayer.Backend.Resources." + resourceName;
                using (var stream = assembly.GetManifestResourceStream(resourceFullName))
                {
                    if (stream == null) return null;
                    var bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                    return bytes;
                }
            }
            catch
            {
                return null;
            }
        }

        private byte[] CreatePlaceholderCover(int width, int height)
        {
            using (var bitmap = new System.Drawing.Bitmap(width, height))
            {
                using (var g = System.Drawing.Graphics.FromImage(bitmap))
                {
                    g.Clear(System.Drawing.Color.FromArgb(40, 40, 50));
                    using (var font = new System.Drawing.Font("Arial", 16))
                    using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Gray))
                    {
                        g.DrawString("No Cover", font, brush, 50, 120);
                    }
                }
                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }
    }
}
