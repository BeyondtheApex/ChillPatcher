using System.Threading.Tasks;

namespace OmniMixPlayer.SDK.Interfaces
{
    public interface IDefaultCoverProvider
    {
        byte[] DefaultMusicCover { get; }
        byte[] DefaultAlbumCover { get; }
        byte[] LocalMusicCover { get; }
    }
}
