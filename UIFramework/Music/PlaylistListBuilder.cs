using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bulbul;
// ModuleSystem removed - IPC bridge
using ChillPatcher.SDK.Models;
using UnityEngine;

namespace ChillPatcher.UIFramework.Music
{
    /// <summary>
    /// 播放列表构建器 - 将歌曲列表转换为包含专辑分隔的复合列表
    /// 使用新的模块系统 Registry
    /// </summary>
    public class PlaylistListBuilder
    {
        private static readonly BepInEx.Logging.ManualLogSource Logger = 
            BepInEx.Logging.Logger.CreateLogSource("PlaylistBuilder");

        public PlaylistListBuilder()
        {
        }

        /// <summary>
        /// 构建带专辑分隔的播放列表（根据歌曲的专辑信息分组）
        /// </summary>
        public async Task<List<PlaylistListItem>> BuildWithAlbumHeaders(
            IReadOnlyList<GameAudioInfo> songs,
            bool loadCovers = true)
        {
            var result = new List<PlaylistListItem>();
            Logger.LogInfo($"BuildWithAlbumHeaders called with {songs?.Count ?? 0} songs");

            if (songs == null || songs.Count == 0)
                return result;

            // IPC bridge: ModuleSystem registries removed.
            // Fall back to simple list mode (no album grouping for module songs).
            for (int i = 0; i < songs.Count; i++)
            {
                result.Add(PlaylistListItem.CreateSongItem(songs[i], i));
            }
            return result;
        }
    }
}
