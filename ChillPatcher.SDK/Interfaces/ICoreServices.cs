using System.Threading.Tasks;
using UnityEngine;

namespace ChillPatcher.SDK.Interfaces
{
    /// <summary>
    /// 默认封面提供器接口
    /// 由主程序实现，提供默认封面
    /// </summary>
    public interface IDefaultCoverProvider
    {
        /// <summary>
        /// 获取默认歌曲封面
        /// </summary>
        Sprite DefaultMusicCover { get; }

        /// <summary>
        /// 获取默认专辑封面
        /// </summary>
        Sprite DefaultAlbumCover { get; }

        /// <summary>
        /// 获取本地音乐默认封面
        /// </summary>
        Sprite LocalMusicCover { get; }
    }

    /// <summary>
    /// 音频加载器接口
    /// 由主程序实现，提供音频加载功能
    /// </summary>
    public interface IAudioLoader
    {
        /// <summary>
        /// 支持的音频格式扩展名
        /// </summary>
        string[] SupportedFormats { get; }

        /// <summary>
        /// 检查文件是否是支持的音频格式
        /// </summary>
        bool IsSupportedFormat(string filePath);

        /// <summary>
        /// 从文件加载 AudioClip
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>加载的 AudioClip</returns>
        Task<AudioClip> LoadFromFileAsync(string filePath);

        /// <summary>
        /// 从 URL 加载 AudioClip
        /// </summary>
        /// <param name="url">音频 URL</param>
        /// <returns>加载的 AudioClip</returns>
        Task<AudioClip> LoadFromUrlAsync(string url);

        /// <summary>
        /// 从文件加载并提取元数据
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>元组: (AudioClip, Title, Artist)</returns>
        Task<(AudioClip clip, string title, string artist)> LoadWithMetadataAsync(string filePath);

        /// <summary>
        /// 卸载 AudioClip
        /// </summary>
        void UnloadClip(AudioClip clip);
    }
}
