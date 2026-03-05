using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BepInEx.Logging;
using ChillPatcher.SDK.Events;

namespace ChillPatcher.Module.QQMusic
{
    /// <summary>
    /// Manages favorite state synchronization with QQ Music
    /// </summary>
    public class QQMusicFavoriteManager
    {
        private readonly QQMusicBridge _bridge;
        private readonly ManualLogSource _logger;
        private readonly HashSet<string> _likeSongMids;
        private readonly Dictionary<string, QQMusicBridge.SongInfo> _songInfoMap;

        public QQMusicFavoriteManager(
            QQMusicBridge bridge,
            ManualLogSource logger,
            Dictionary<string, QQMusicBridge.SongInfo> songInfoMap)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _logger = logger;
            _songInfoMap = songInfoMap;
            _likeSongMids = new HashSet<string>();
        }

        /// <summary>
        /// Loads the like list from QQ Music
        /// </summary>
        public async Task LoadLikeListAsync()
        {
            try
            {
                var songs = await Task.Run(() => _bridge.GetLikeSongs(true));
                if (songs == null) return;

                _likeSongMids.Clear();
                foreach (var song in songs)
                {
                    _likeSongMids.Add(song.Mid);
                }

                _logger?.LogInfo($"Loaded {_likeSongMids.Count} liked songs from QQ Music");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to load like list: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a song is in the like list
        /// </summary>
        public bool IsFavorite(string uuid)
        {
            if (!_songInfoMap.TryGetValue(uuid, out var songInfo))
                return false;

            return _likeSongMids.Contains(songInfo.Mid);
        }

        /// <summary>
        /// Sets the favorite state for a song
        /// </summary>
        public async Task<bool> SetFavoriteAsync(string uuid, bool isFavorite)
        {
            if (!_songInfoMap.TryGetValue(uuid, out var songInfo))
            {
                _logger?.LogWarning($"Song not found for UUID: {uuid}");
                return false;
            }

            try
            {
                var success = await Task.Run(() => _bridge.LikeSong(songInfo.Mid, isFavorite));
                if (success)
                {
                    if (isFavorite)
                    {
                        _likeSongMids.Add(songInfo.Mid);
                    }
                    else
                    {
                        _likeSongMids.Remove(songInfo.Mid);
                    }
                    _logger?.LogInfo($"Set favorite for {songInfo.Name}: {isFavorite}");
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to set favorite: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handles a favorite changed event from the event bus
        /// </summary>
        public void HandleFavoriteChanged(
            FavoriteChangedEvent evt,
            string moduleId,
            Action<string, bool> onStateChanged)
        {
            // Only handle events for our module
            if (evt.ModuleId != moduleId)
                return;

            // Get song info
            if (!_songInfoMap.TryGetValue(evt.UUID, out var songInfo))
                return;

            // Update local state
            if (evt.IsFavorite)
            {
                _likeSongMids.Add(songInfo.Mid);
            }
            else
            {
                _likeSongMids.Remove(songInfo.Mid);
            }

            // Sync to QQ Music in background
            Task.Run(async () =>
            {
                try
                {
                    await SetFavoriteAsync(evt.UUID, evt.IsFavorite);
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Failed to sync favorite to QQ Music: {ex.Message}");
                }
            });

            // Notify caller of state change
            onStateChanged?.Invoke(evt.UUID, evt.IsFavorite);
        }

        /// <summary>
        /// Gets all favorite song MIDs
        /// </summary>
        public IReadOnlyCollection<string> GetFavoriteMids()
        {
            return _likeSongMids;
        }

        /// <summary>
        /// Checks if a song MID is in the like list
        /// </summary>
        public bool IsSongMidFavorite(string songMid)
        {
            return _likeSongMids.Contains(songMid);
        }
    }
}
