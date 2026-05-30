using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Bulbul.Achievements;

namespace ChillPatcher.Patches
{
    /// <summary>
    /// 成就缓存管理器
    /// 在壁纸引擎模式下，将成就数据缓存到本地，等待有Steam连接时同步
    /// 每个用户ID有独立的成就缓存目录
    /// </summary>
    public static class AchievementCacheManager
    {
        private static readonly string CacheBaseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).Replace("Local", "LocalLow"),
            "Nestopi",
            "Chill With You",
            "ChillPatcherCache"
        );

        /// <summary>
        /// 获取指定用户的缓存文件路径
        /// </summary>
        private static string GetCacheFilePath(string userId)
        {
            string userCacheDir = Path.Combine(CacheBaseDirectory, userId);
            return Path.Combine(userCacheDir, "achievement_cache.json");
        }

        /// <summary>
        /// 确保用户缓存目录存在
        /// </summary>
        private static void EnsureUserCacheDirectoryExists(string userId)
        {
            string userCacheDir = Path.Combine(CacheBaseDirectory, userId);
            if (!Directory.Exists(userCacheDir))
            {
                Directory.CreateDirectory(userCacheDir);
            }
        }

        /// <summary>
        /// 成就缓存数据结构
        /// </summary>
        [Serializable]
        public class AchievementCacheData
        {
            public Dictionary<string, int> CachedAchievements { get; set; } = new Dictionary<string, int>();
            public DateTime LastModified { get; set; } = DateTime.Now;
        }

        /// <summary>
        /// 添加或更新成就缓存
        /// </summary>
        /// <param name="userId">用户ID（Steam ID 或离线用户ID）</param>
        /// <param name="category">成就类别</param>
        /// <param name="progress">成就进度</param>
        public static void CacheAchievement(string userId, AchievementCategory category, int progress)
        {
            try
            {
                EnsureUserCacheDirectoryExists(userId);
                
                var cacheData = LoadCache(userId);
                string key = category.ToString();
                
                // 更新或添加成就进度
                if (cacheData.CachedAchievements.ContainsKey(key))
                {
                    // 只保留最高进度
                    if (progress > cacheData.CachedAchievements[key])
                    {
                        cacheData.CachedAchievements[key] = progress;
                    }
                }
                else
                {
                    cacheData.CachedAchievements.Add(key, progress);
                }
                
                cacheData.LastModified = DateTime.Now;
                SaveCache(userId, cacheData);
                
                Plugin.Logger.LogInfo($"[AchievementCache] 缓存成就 (用户:{userId}): {category} = {progress}");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[AchievementCache] 缓存成就失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取所有缓存的成就
        /// </summary>
        /// <param name="userId">用户ID</param>
        public static Dictionary<string, int> GetCachedAchievements(string userId)
        {
            try
            {
                var cacheData = LoadCache(userId);
                return cacheData.CachedAchievements;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[AchievementCache] 读取缓存失败: {ex.Message}");
                return new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// 清空成就缓存
        /// </summary>
        /// <param name="userId">用户ID，如果为null则清空所有用户的缓存</param>
        public static void ClearCache(string userId = null)
        {
            try
            {
                if (userId == null)
                {
                    // 清空所有缓存
                    if (Directory.Exists(CacheBaseDirectory))
                    {
                        Directory.Delete(CacheBaseDirectory, true);
                        Plugin.Logger.LogInfo("[AchievementCache] 所有缓存已清空");
                    }
                }
                else
                {
                    // 清空指定用户的缓存
                    string cacheFilePath = GetCacheFilePath(userId);
                    if (File.Exists(cacheFilePath))
                    {
                        File.Delete(cacheFilePath);
                        Plugin.Logger.LogInfo($"[AchievementCache] 用户 {userId} 的缓存已清空");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[AchievementCache] 清空缓存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查是否有待同步的成就
        /// </summary>
        /// <param name="userId">用户ID</param>
        public static bool HasPendingAchievements(string userId)
        {
            try
            {
                var cacheData = LoadCache(userId);
                return cacheData.CachedAchievements.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 从缓存文件加载数据
        /// </summary>
        private static AchievementCacheData LoadCache(string userId)
        {
            string cacheFilePath = GetCacheFilePath(userId);
            
            if (!File.Exists(cacheFilePath))
            {
                return new AchievementCacheData();
            }

            try
            {
                string json = File.ReadAllText(cacheFilePath);
                return JsonConvert.DeserializeObject<AchievementCacheData>(json) ?? new AchievementCacheData();
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[AchievementCache] 加载缓存失败，创建新缓存: {ex.Message}");
                return new AchievementCacheData();
            }
        }

        /// <summary>
        /// 保存缓存数据到文件
        /// </summary>
        private static void SaveCache(string userId, AchievementCacheData cacheData)
        {
            try
            {
                string cacheFilePath = GetCacheFilePath(userId);
                string json = JsonConvert.SerializeObject(cacheData, Formatting.Indented);
                File.WriteAllText(cacheFilePath, json);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[AchievementCache] 保存缓存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取缓存文件信息（用于调试）
        /// </summary>
        /// <param name="userId">用户ID</param>
        public static string GetCacheInfo(string userId)
        {
            try
            {
                string cacheFilePath = GetCacheFilePath(userId);
                
                if (!File.Exists(cacheFilePath))
                {
                    return $"用户 {userId} 无缓存文件";
                }

                var cacheData = LoadCache(userId);
                return $"用户: {userId}\n" +
                       $"缓存文件: {cacheFilePath}\n" +
                       $"成就数量: {cacheData.CachedAchievements.Count}\n" +
                       $"最后修改: {cacheData.LastModified}";
            }
            catch (Exception ex)
            {
                return $"获取缓存信息失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 获取当前用户ID（根据模式决定）
        /// 壁纸引擎模式：使用 OfflineUserId
        /// 非壁纸引擎模式：始终使用 SteamId（即使启用了多存档）
        /// </summary>
        public static string GetCurrentUserId()
        {
            try
            {
                if (PluginConfig.EnableWallpaperEngineMode.Value)
                {
                    // 壁纸引擎模式：使用配置的离线用户ID
                    return PluginConfig.OfflineUserId.Value;
                }
                else
                {
                    // 非壁纸引擎模式：始终使用Steam ID（不管是否启用多存档）
                    return GetSteamUserId();
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[AchievementCache] 获取用户ID失败: {ex.Message}");
                return PluginConfig.OfflineUserId.Value; // 降级到配置的ID
            }
        }

        /// <summary>
        /// 获取Steam用户ID
        /// </summary>
        private static string GetSteamUserId()
        {
            try
            {
                // 尝试通过Steamworks API获取Steam ID
                var steamId = Steamworks.SteamUser.GetSteamID();
                if (steamId.IsValid())
                {
                    return steamId.ToString();
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[AchievementCache] 无法获取Steam ID: {ex.Message}");
            }
            
            return PluginConfig.OfflineUserId.Value;
        }
    }
}
