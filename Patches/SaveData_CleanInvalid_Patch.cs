using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bulbul;
using HarmonyLib;
using ChillPatcher.Patches.UIFramework;

namespace ChillPatcher.Patches
{
    /// <summary>
    /// 清理无效音乐数据的补丁
    /// </summary>
    [HarmonyPatch(typeof(MusicService))]
    public class SaveData_CleanInvalid_Patch
    {
        private static bool _hasCleanedThisSession = false;

        /// <summary>
        /// 在MusicService加载音乐后清理无效数据
        /// </summary>
        [HarmonyPatch("Load")]
        [HarmonyPostfix]
        static void Load_Postfix(MusicService __instance, IReadOnlyCollection<GameAudioInfo> musicItems)
        {
            try
            {
                // 每个会话只执行一次
                if (_hasCleanedThisSession)
                {
                    return;
                }

                // 检查是否启用清理功能
                if (!PluginConfig.CleanInvalidMusicData.Value)
                {
                    return;
                }

                _hasCleanedThisSession = true;

                Plugin.Log.LogInfo("[CleanInvalidMusicData] 开始清理无效音乐数据...");

                // 创建有效音乐的UUID集合
                var validMusicUUIDs = new HashSet<string>();
                foreach (var music in musicItems)
                {
                    if (!string.IsNullOrEmpty(music.UUID))
                    {
                        validMusicUUIDs.Add(music.UUID);
                    }
                }

                int cleanedLocalMusic = CleanInvalidLocalMusic();
                int cleanedFavorites = CleanInvalidFavorites(validMusicUUIDs, musicItems);
                int cleanedPlaylistOrder = CleanInvalidPlaylistOrder(validMusicUUIDs);
                int cleanedExcluded = CleanInvalidExcluded(validMusicUUIDs);

                Plugin.Log.LogInfo($"[CleanInvalidMusicData] 清理完成:");
                Plugin.Log.LogInfo($"  - 清理无效本地音乐: {cleanedLocalMusic} 个");
                Plugin.Log.LogInfo($"  - 清理无效收藏: {cleanedFavorites} 个");
                Plugin.Log.LogInfo($"  - 清理无效播放顺序: {cleanedPlaylistOrder} 个");
                Plugin.Log.LogInfo($"  - 清理无效排除列表: {cleanedExcluded} 个");

                // 保存更新后的数据
                if (cleanedLocalMusic > 0 || cleanedFavorites > 0 || cleanedPlaylistOrder > 0 || cleanedExcluded > 0)
                {
                    SaveDataManager.Instance.SaveMusicSetting();
                    SaveDataManager.Instance.SaveLocalMusicSetting();
                    Plugin.Log.LogInfo("[CleanInvalidMusicData] 已保存清理后的数据");
                }

                // 自动关闭配置选项
                PluginConfig.CleanInvalidMusicData.Value = false;
                Plugin.Log.LogInfo("[CleanInvalidMusicData] 已自动关闭清理选项，下次启动不会再执行");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[CleanInvalidMusicData] 清理失败: {ex}");
            }
        }

        /// <summary>
        /// 清理无效的本地音乐路径（只删除文件不存在的记录）
        /// </summary>
        private static int CleanInvalidLocalMusic()
        {
            var localAudioDatas = SaveDataManager.Instance.LocalMusicSetting?.LocalAudioDatas;
            if (localAudioDatas == null || localAudioDatas.Count == 0)
            {
                return 0;
            }

            // 找出文件不存在的本地音乐
            var invalidData = localAudioDatas
                .Where(data => !File.Exists(data.FilePath))
                .ToList();

            // 移除无效的数据
            int count = 0;
            foreach (var data in invalidData)
            {
                localAudioDatas.Remove(data);
                Plugin.Log.LogDebug($"[CleanInvalidLocalMusic] 移除无效路径: {data.FilePath} (UUID: {data.UUID})");
                count++;
            }

            return count;
        }

        /// <summary>
        /// 清理无效的收藏UUID
        /// 策略：检查每个收藏的UUID对应的音乐信息
        /// - 如果有内置tag（非Local）→ 保留
        /// - 如果只有Local tag → 检查文件是否存在，不存在则删除
        /// </summary>
        private static int CleanInvalidFavorites(HashSet<string> validMusicUUIDs, IReadOnlyCollection<GameAudioInfo> musicItems)
        {
            var favoriteUUIDs = SaveDataManager.Instance.MusicSetting.FavoriteAudioUUIDs;
            if (favoriteUUIDs == null || favoriteUUIDs.Count == 0)
            {
                return 0;
            }

            // 创建UUID到AudioInfo的映射
            var uuidToMusic = new Dictionary<string, GameAudioInfo>();
            foreach (var music in musicItems)
            {
                if (!string.IsNullOrEmpty(music.UUID))
                {
                    uuidToMusic[music.UUID] = music;
                }
            }

            var invalidUUIDs = new List<string>();

            foreach (var uuid in favoriteUUIDs.ToList())
            {
                // 检查UUID是否在有效音乐列表中
                if (!validMusicUUIDs.Contains(uuid))
                {
                    // UUID不在当前加载的音乐列表中，可能是已删除的
                    invalidUUIDs.Add(uuid);
                    Plugin.Log.LogDebug($"[CleanInvalidFavorites] 收藏UUID不在音乐列表中: {uuid}");
                }
                else if (uuidToMusic.TryGetValue(uuid, out var music))
                {
                    // UUID在列表中，检查是否是本地音乐且文件不存在
                    bool hasLocalTag = music.Tag.HasFlagFast(AudioTag.Local);
                    bool hasOtherTag = (music.Tag & ~AudioTag.Local & ~AudioTag.Favorite) != 0;

                    if (hasLocalTag && !hasOtherTag)
                    {
                        // 只有Local tag，检查文件
                        if (!string.IsNullOrEmpty(music.LocalPath) && !File.Exists(music.LocalPath))
                        {
                            invalidUUIDs.Add(uuid);
                            Plugin.Log.LogDebug($"[CleanInvalidFavorites] 本地音乐文件不存在: {music.LocalPath} (UUID: {uuid})");
                        }
                    }
                    // 如果有其他内置tag，即使是本地音乐也保留收藏
                }
            }

            // 移除无效的UUID
            int count = 0;
            foreach (var uuid in invalidUUIDs)
            {
                favoriteUUIDs.Remove(uuid);
                count++;
            }

            return count;
        }

        /// <summary>
        /// 清理无效的播放顺序UUID
        /// </summary>
        private static int CleanInvalidPlaylistOrder(HashSet<string> validMusicUUIDs)
        {
            var playlistOrder = SaveDataManager.Instance.MusicSetting.PlaylistOrder;
            if (playlistOrder == null || playlistOrder.Count == 0)
            {
                return 0;
            }

            // 找出无效的UUID（不在当前有效音乐列表中）
            var invalidUUIDs = playlistOrder
                .Where(uuid => !validMusicUUIDs.Contains(uuid))
                .ToList();

            // 移除无效UUID
            int count = 0;
            foreach (var uuid in invalidUUIDs)
            {
                playlistOrder.Remove(uuid);
                Plugin.Log.LogDebug($"[CleanInvalidPlaylistOrder] 移除无效UUID: {uuid}");
                count++;
            }

            return count;
        }

        /// <summary>
        /// 清理无效的排除列表UUID（参考收藏清理逻辑）
        /// 策略：检查每个排除的UUID对应的音乐信息
        /// - 如果有内置tag（非Local）→ 保留
        /// - 如果只有Local tag → 检查文件是否存在，不存在则删除
        /// </summary>
        private static int CleanInvalidExcluded(HashSet<string> validMusicUUIDs)
        {
            var excludedUUIDs = SaveDataManager.Instance.MusicSetting.ExcludedFromPlaylistUUIDs;
            if (excludedUUIDs == null || excludedUUIDs.Count == 0)
            {
                return 0;
            }

            // 获取所有音乐列表（需要访问MusicService）
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            if (musicService == null)
            {
                Plugin.Log.LogWarning("[CleanInvalidExcluded] MusicService实例未找到，跳过清理");
                return 0;
            }

            var allMusicList = HarmonyLib.Traverse.Create(musicService)
                .Field("_allMusicList")
                .GetValue<List<GameAudioInfo>>();

            if (allMusicList == null)
            {
                return 0;
            }

            // 创建UUID到AudioInfo的映射
            var uuidToMusic = new Dictionary<string, GameAudioInfo>();
            foreach (var music in allMusicList)
            {
                if (!string.IsNullOrEmpty(music.UUID))
                {
                    uuidToMusic[music.UUID] = music;
                }
            }

            var invalidUUIDs = new List<string>();

            foreach (var uuid in excludedUUIDs.ToList())
            {
                // 检查UUID是否在有效音乐列表中
                if (!validMusicUUIDs.Contains(uuid))
                {
                    // UUID不在当前加载的音乐列表中，可能是已删除的
                    invalidUUIDs.Add(uuid);
                    Plugin.Log.LogDebug($"[CleanInvalidExcluded] 排除UUID不在音乐列表中: {uuid}");
                }
                else if (uuidToMusic.TryGetValue(uuid, out var music))
                {
                    // UUID在列表中，检查是否是本地音乐且文件不存在
                    bool hasLocalTag = music.Tag.HasFlagFast(AudioTag.Local);
                    bool hasOtherTag = (music.Tag & ~AudioTag.Local & ~AudioTag.Favorite) != 0;

                    if (hasLocalTag && !hasOtherTag)
                    {
                        // 只有Local tag，检查文件
                        if (!string.IsNullOrEmpty(music.LocalPath) && !File.Exists(music.LocalPath))
                        {
                            invalidUUIDs.Add(uuid);
                            Plugin.Log.LogDebug($"[CleanInvalidExcluded] 本地音乐文件不存在: {music.LocalPath} (UUID: {uuid})");
                        }
                    }
                    // 如果有其他内置tag，即使是本地音乐也保留排除状态
                }
            }

            // 移除无效UUID
            int count = 0;
            foreach (var uuid in invalidUUIDs)
            {
                excludedUUIDs.Remove(uuid);
                count++;
            }

            return count;
        }
    }
}

