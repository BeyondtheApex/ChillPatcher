using HarmonyLib;
using Bulbul.Achievements;
using NestopiSystem.Steam;
using System.Reflection;

namespace ChillPatcher.Patches
{
    /// <summary>
    /// Patch 8: 哑巴成就 - SteamAchievements 构造函数
    /// 删除 achievementEventBroker 的初始化
    /// </summary>
    [HarmonyPatch(typeof(SteamAchievements), MethodType.Constructor, new System.Type[] { typeof(SteamManager) })]
    public class SteamAchievements_Constructor_Patch
    {
        static void Postfix(SteamAchievements __instance)
        {
            if (!PluginConfig.EnableWallpaperEngineMode.Value)
                return; // 不屏蔽，保持原功能
                
            // 将 achievementEventBroker 设置为 null
            FieldInfo field = AccessTools.Field(typeof(SteamAchievements), "achievementEventBroker");
            field.SetValue(__instance, null);
            Plugin.Logger.LogInfo("[ChillPatcher] SteamAchievements - 禁用成就事件代理");
        }
    }

    /// <summary>
    /// Patch 9: 成就缓存 - SteamAchievements.SetProgress
    /// 壁纸引擎模式：拦截并缓存成就，不推送到Steam
    /// 正常模式：缓存成就到本地作为备份，然后继续执行原方法推送到Steam
    /// </summary>
    [HarmonyPatch(typeof(SteamAchievements), "SetProgress")]
    public class SteamAchievements_SetProgress_Patch
    {
        static bool Prefix(AchievementCategory category, int progress, ref int __result)
        {
            // 如果启用成就缓存，缓存成就数据到本地
            if (PluginConfig.EnableAchievementCache.Value)
            {
                string userId = AchievementCacheManager.GetCurrentUserId();
                AchievementCacheManager.CacheAchievement(userId, category, progress);
            }
            
            if (!PluginConfig.EnableWallpaperEngineMode.Value)
                return true; // 正常模式：缓存后继续执行原方法
            
            // 壁纸引擎模式：只缓存，不推送到Steam
            __result = progress;
            return false; // 阻止原方法执行
        }
    }

    /// <summary>
    /// Patch 10: 成就缓存 - SteamAchievements.ProgressIncrement
    /// 壁纸引擎模式：拦截并缓存成就增量，不推送到Steam
    /// 正常模式：缓存成就到本地作为备份，然后继续执行原方法推送到Steam
    /// </summary>
    [HarmonyPatch(typeof(SteamAchievements), "ProgressIncrement")]
    public class SteamAchievements_ProgressIncrement_Patch
    {
        static bool Prefix(SteamAchievements __instance, AchievementCategory category, int count, ref int __result)
        {
            if (!PluginConfig.EnableWallpaperEngineMode.Value)
            {
                // 正常模式：先执行原方法获取新进度，然后在Postfix中缓存
                return true; // 继续执行原方法
            }
            
            // 壁纸引擎模式：从缓存中获取当前进度并增加
            if (PluginConfig.EnableAchievementCache.Value)
            {
                string userId = AchievementCacheManager.GetCurrentUserId();
                var cached = AchievementCacheManager.GetCachedAchievements(userId);
                string key = category.ToString();
                int currentProgress = cached.ContainsKey(key) ? cached[key] : 0;
                
                // 增加进度
                int newProgress = currentProgress + count;
                if (newProgress < 0) newProgress = 0;
                
                // 缓存新进度
                AchievementCacheManager.CacheAchievement(userId, category, newProgress);
                __result = newProgress;
            }
            else
            {
                __result = -1;
            }
            
            return false; // 阻止原方法执行
        }
        
        static void Postfix(AchievementCategory category, int __result)
        {
            // 正常模式：原方法执行后，缓存最新的进度值
            if (!PluginConfig.EnableWallpaperEngineMode.Value && 
                PluginConfig.EnableAchievementCache.Value && 
                __result >= 0)
            {
                string userId = AchievementCacheManager.GetCurrentUserId();
                AchievementCacheManager.CacheAchievement(userId, category, __result);
            }
        }
    }

    /// <summary>
    /// Patch 11: 哑巴成就 - SteamAchievements.TryGetStat
    /// 直接返回 false
    /// </summary>
    [HarmonyPatch(typeof(SteamAchievements), "TryGetStat")]
    public class SteamAchievements_TryGetStat_Patch
    {
        static bool Prefix(ref bool __result)
        {
            if (!PluginConfig.EnableWallpaperEngineMode.Value)
                return true; // 不屏蔽，执行原方法
                
            __result = false;
            return false; // 阻止原方法执行
        }
    }

    /// <summary>
    /// Patch 12: 哑巴成就 - SteamAchievements.TrySetStat
    /// 直接返回 false
    /// </summary>
    [HarmonyPatch(typeof(SteamAchievements), "TrySetStat")]
    public class SteamAchievements_TrySetStat_Patch
    {
        static bool Prefix(ref bool __result)
        {
            if (!PluginConfig.EnableWallpaperEngineMode.Value)
                return true; // 不屏蔽，执行原方法
                
            __result = false;
            return false; // 阻止原方法执行
        }
    }

    /// <summary>
    /// Patch 13: 哑巴成就 - SteamAchievements.GetAchievement
    /// 只保留创建本地缓存的代码，删除联网获取部分
    /// </summary>
    [HarmonyPatch(typeof(SteamAchievements), "GetAchievement")]
    public class SteamAchievements_GetAchievement_Patch
    {
        static bool Prefix(SteamAchievements __instance, AchievementCategory category, ref AchievementStats __result)
        {
            if (!PluginConfig.EnableWallpaperEngineMode.Value)
                return true; // 不屏蔽，执行原方法
                
            // 直接创建本地缓存，不从 Steam 获取
            __result = AchievementStats.Create(category, 0);
            Plugin.Logger.LogInfo($"[ChillPatcher] GetAchievement - 返回本地缓存: {category}");
            return false; // 阻止原方法执行
        }
    }
}
