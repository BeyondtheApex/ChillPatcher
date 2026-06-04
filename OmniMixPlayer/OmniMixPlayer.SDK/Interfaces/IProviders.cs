using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OmniMixPlayer.SDK.Protos.Models;

namespace OmniMixPlayer.SDK.Interfaces
{
    public interface IMusicSourceProvider
    {
        Task<List<Track>> GetMusicListAsync();
        Task RefreshAsync();
        SourceType SourceType { get; }
    }

    public interface ICoverProvider
    {
        Task<(byte[] data, string mimeType)> GetMusicCoverAsync(string uuid);
        Task<(byte[] data, string mimeType)> GetAlbumCoverAsync(string albumId);
        void ClearCache();
        void RemoveMusicCoverCache(string uuid);
        void RemoveAlbumCoverCache(string albumId);
    }

    public interface IFavoriteExcludeHandler
    {
        bool IsFavorite(string uuid);
        void SetFavorite(string uuid, bool isFavorite);
        bool IsExcluded(string uuid);
        void SetExcluded(string uuid, bool isExcluded);
        IReadOnlyList<string> GetFavorites();
        IReadOnlyList<string> GetExcluded();
    }

    public interface IDeleteHandler
    {
        bool CanDelete { get; }
        bool Delete(string uuid);
    }

    public interface ILyricProvider
    {
        string GetLyric(string uuid);
    }
}
