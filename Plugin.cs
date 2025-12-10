using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ChillPatcher.Patches;
using ChillPatcher.Patches.UIFramework;
using ChillPatcher.UIFramework;
using ChillPatcher.UIFramework.Config;
using ChillPatcher.UIFramework.Music;
using ChillPatcher.ModuleSystem;
using ChillPatcher.ModuleSystem.Registry;
using ChillPatcher.ModuleSystem.Services;
using ChillPatcher.SDK.Interfaces;
using Cysharp.Threading.Tasks;
using Bulbul;

namespace ChillPatcher
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        internal static ManualLogSource Log; // 别名，用于Patches
        
        private float healthCheckTimer = 0f;
        private const float healthCheckInterval = 5f; // 每5秒检查一次

        /// <summary>
        /// 插件根目录
        /// </summary>
        public static string PluginPath { get; private set; }

        /// <summary>
        /// 模块目录
        /// </summary>
        public static string ModulesPath { get; private set; }

        private void Awake()
        {
            Logger = base.Logger;
            Log = Logger; // 设置别名
            
            // 设置路径
            PluginPath = Path.GetDirectoryName(Info.Location);
            ModulesPath = Path.Combine(PluginPath, "modules");
            
            CoreDependencyLoader.EnsureDependencies(Log);
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            Logger.LogInfo($"Plugin Path: {PluginPath}");

            // 初始化配置
            PluginConfig.Initialize(Config);
            
            // 初始化UI框架配置
            UIFrameworkConfig.Initialize(Config);

            // Apply Harmony patches
            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            Logger.LogInfo("Harmony patches applied!");
            
            // 输出配置状态
            Logger.LogInfo("==== ChillPatcher Configuration ====");
            Logger.LogInfo($"Virtual Scroll: ALWAYS ON (Performance optimization)");
            Logger.LogInfo($"Album Art Display: {(UIFrameworkConfig.EnableAlbumArtDisplay.Value ? "ON" : "OFF")} (Show cover on button)");
            Logger.LogInfo($"Unlimited Songs: {(UIFrameworkConfig.EnableUnlimitedSongs.Value ? "ON" : "OFF")} (May affect save)");
            Logger.LogInfo($"Extended Formats: {(UIFrameworkConfig.EnableExtendedFormats.Value ? "ON" : "OFF")} (OGG/FLAC/AIFF)");
            Logger.LogInfo("====================================");

            // 初始化全局键盘钩子（用于壁纸引擎模式）
            KeyboardHookPatch.Initialize();
            Logger.LogInfo("Keyboard hook initialized!");
            
            // 初始化成就同步管理器
            if (PluginConfig.EnableAchievementCache.Value)
            {
                AchievementSyncManager.Initialize();
                Logger.LogInfo("Achievement sync manager initialized!");
            }
            
            // ========== 初始化模块系统 ==========
            try
            {
                InitializeModuleSystem();
                Logger.LogInfo("Module system initialized!");
                
                // 初始化UI框架
                ChillUIFramework.Initialize();
                Logger.LogInfo("ChillUIFramework initialized!");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to initialize module system: {ex}");
            }
        }

        /// <summary>
        /// 初始化模块系统
        /// </summary>
        private void InitializeModuleSystem()
        {
            // 初始化各个 Registry
            TagRegistry.Initialize(Logger);
            AlbumRegistry.Initialize(Logger);
            MusicRegistry.Initialize(Logger);
            EventBus.Initialize(Logger);
            
            // 初始化核心服务
            DefaultCoverProvider.Initialize();
            CoreAudioLoader.Initialize();
            
            // 初始化 CoverService 的事件订阅
            CoverService.Instance.InitializeEventSubscriptions();
            
            // 订阅 MusicRegistry 事件以同步到游戏 MusicService
            SubscribeMusicRegistryEvents();
            
            // 订阅 TagRegistry 事件以刷新 UI
            SubscribeTagRegistryEvents();
            
            Logger.LogInfo("Core registries and services initialized!");
        }

        /// <summary>
        /// 订阅 MusicRegistry 事件以同步到游戏的 MusicService
        /// </summary>
        private void SubscribeMusicRegistryEvents()
        {
            // 当新歌曲注册时，如果符合当前 Tag 筛选，添加到游戏的播放列表
            MusicRegistry.Instance.OnMusicRegistered += (musicInfo) =>
            {
                try
                {
                    // 确保在主线程执行
                    UIFramework.Audio.MainThreadDispatcher.Instance?.Enqueue(() =>
                    {
                        SyncMusicToGameService(musicInfo, isAdd: true);
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error syncing music to game: {ex}");
                }
            };

            // 当歌曲注销时，从游戏播放列表移除
            MusicRegistry.Instance.OnMusicUnregistered += (musicUUID) =>
            {
                try
                {
                    UIFramework.Audio.MainThreadDispatcher.Instance?.Enqueue(() =>
                    {
                        SyncMusicRemovalToGameService(musicUUID);
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error removing music from game: {ex}");
                }
            };
        }

        /// <summary>
        /// 订阅 TagRegistry 事件以刷新 Tag 列表 UI
        /// </summary>
        private void SubscribeTagRegistryEvents()
        {
            TagRegistry.Instance.OnTagRegistered += (tagInfo) =>
            {
                try
                {
                    Logger.LogDebug($"[TagSync] New tag registered: {tagInfo.DisplayName}");
                    
                    // 确保在主线程执行 UI 刷新
                    UIFramework.Audio.MainThreadDispatcher.Instance?.Enqueue(() =>
                    {
                        Patches.UIFramework.MusicTagListUI_Patches.RefreshCustomTagButtons();
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error refreshing tag UI: {ex}");
                }
            };
        }

        /// <summary>
        /// 同步单首歌曲到游戏的 MusicService
        /// </summary>
        private static void SyncMusicToGameService(SDK.Models.MusicInfo musicInfo, bool isAdd)
        {
            try
            {
                var musicService = Patches.UIFramework.MusicService_RemoveLimit_Patch.CurrentInstance;
                if (musicService == null)
                {
                    Logger.LogWarning($"[MusicSync] MusicService not available, skipping sync for: {musicInfo.Title}");
                    return;
                }

                // 检查歌曲至少有一个有效的 Tag
                SDK.Models.TagInfo primaryTagInfo = null;
                if (musicInfo.TagIds != null && musicInfo.TagIds.Count > 0)
                {
                    primaryTagInfo = TagRegistry.Instance?.GetTag(musicInfo.TagIds[0]);
                }
                else if (!string.IsNullOrEmpty(musicInfo.TagId))
                {
                    primaryTagInfo = TagRegistry.Instance?.GetTag(musicInfo.TagId);
                }
                
                if (primaryTagInfo == null)
                {
                    Logger.LogWarning($"[MusicSync] No valid Tag found for music: {musicInfo.Title}");
                    return;
                }

                // 创建 GameAudioInfo（会合并所有 TagIds 的位值）
                var gameAudio = ConvertToGameAudioInfo(musicInfo, primaryTagInfo);
                if (gameAudio == null)
                {
                    Logger.LogWarning($"[MusicSync] Failed to convert MusicInfo to GameAudioInfo: {musicInfo.Title}");
                    return;
                }

                // 添加到 _allMusicList（持久化，Tag 变动时不会丢失）
                var allMusicList = Traverse.Create(musicService)
                    .Field("_allMusicList")
                    .GetValue<List<GameAudioInfo>>();
                
                if (allMusicList != null)
                {
                    var existing = allMusicList.FirstOrDefault(a => a.UUID == gameAudio.UUID);
                    if (existing != null)
                    {
                        // 更新已存在歌曲的 Tag（合并新 Tag）
                        existing.Tag |= gameAudio.Tag;
                        Logger.LogDebug($"[MusicSync] Updated Tag for: {musicInfo.Title} -> {existing.Tag}");
                    }
                    else
                    {
                        allMusicList.Add(gameAudio);
                        Logger.LogInfo($"[MusicSync] Added to AllMusicList: {musicInfo.Title}");
                    }
                }

                // 检查是否有任何一个 Tag 被当前选中
                var currentAudioTag = SaveDataManager.Instance.MusicSetting.CurrentAudioTag.CurrentValue;
                bool shouldAddToPlaylist = false;
                
                // 检查歌曲的任意 Tag 是否在当前选中的 Tag 中
                if (musicInfo.TagIds != null)
                {
                    foreach (var tagId in musicInfo.TagIds)
                    {
                        var tagInfo = TagRegistry.Instance?.GetTag(tagId);
                        if (tagInfo != null && currentAudioTag.HasFlagFast((AudioTag)tagInfo.BitValue))
                        {
                            shouldAddToPlaylist = true;
                            break;
                        }
                    }
                }
                
                if (shouldAddToPlaylist)
                {
                    var currentPlayList = musicService.CurrentPlayList;
                    if (currentPlayList != null && !currentPlayList.Any(a => a.UUID == gameAudio.UUID))
                    {
                        currentPlayList.Add(gameAudio);
                        Logger.LogDebug($"[MusicSync] Added to CurrentPlayList: {musicInfo.Title}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[MusicSync] Error syncing music: {ex}");
            }
        }

        /// <summary>
        /// 将 MusicInfo 转换为 GameAudioInfo
        /// </summary>
        private static GameAudioInfo ConvertToGameAudioInfo(SDK.Models.MusicInfo musicInfo, SDK.Models.TagInfo primaryTagInfo)
        {
            try
            {
                // 确定 AudioMode
                AudioMode pathType;
                switch (musicInfo.SourceType)
                {
                    case SDK.Models.MusicSourceType.File:
                        pathType = AudioMode.LocalPc;
                        break;
                    case SDK.Models.MusicSourceType.Url:
                    case SDK.Models.MusicSourceType.Stream:
                        pathType = AudioMode.LocalPc;  // 流媒体也使用 LocalPc 模式，由我们的加载器处理
                        break;
                    default:
                        pathType = AudioMode.LocalPc;
                        break;
                }

                // 构造 Tag - 合并所有 TagIds 的位值
                AudioTag tag = 0;  // AudioTag 没有 None，使用 0
                
                // 遍历歌曲的所有 TagIds，合并位值
                if (musicInfo.TagIds != null && musicInfo.TagIds.Count > 0)
                {
                    foreach (var tagId in musicInfo.TagIds)
                    {
                        var tagInfo = TagRegistry.Instance?.GetTag(tagId);
                        if (tagInfo != null)
                        {
                            tag |= (AudioTag)tagInfo.BitValue;
                        }
                    }
                }
                else if (primaryTagInfo != null)
                {
                    // 向后兼容：如果没有 TagIds，使用传入的 primaryTagInfo
                    tag = (AudioTag)primaryTagInfo.BitValue;
                }
                
                // 如果歌曲已收藏，添加 Favorite 标记
                if (musicInfo.IsFavorite)
                {
                    tag |= AudioTag.Favorite;
                }

                // 创建 GameAudioInfo（使用对象初始化器）
                var gameAudio = new GameAudioInfo
                {
                    UUID = musicInfo.UUID,
                    Title = musicInfo.Title ?? "",
                    Credit = musicInfo.Artist ?? "",
                    Tag = tag,
                    IsUnlocked = musicInfo.IsUnlocked,
                    PathType = pathType,
                    LocalPath = musicInfo.SourcePath ?? "",
                    AudioClip = null  // 延迟加载
                };

                return gameAudio;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[ConvertToGameAudioInfo] Error: {ex}");
                return null;
            }
        }

        /// <summary>
        /// 从游戏的 MusicService 移除歌曲
        /// </summary>
        private static void SyncMusicRemovalToGameService(string musicUUID)
        {
            try
            {
                var musicService = Patches.UIFramework.MusicService_RemoveLimit_Patch.CurrentInstance;
                if (musicService == null)
                    return;

                // 检查是否是当前正在播放的歌曲
                bool isCurrentlyPlaying = musicService.PlayingMusic?.UUID == musicUUID;

                var currentPlayList = musicService.CurrentPlayList;
                var toRemove = currentPlayList?.FirstOrDefault(a => a.UUID == musicUUID);
                if (toRemove != null)
                {
                    currentPlayList.Remove(toRemove);
                    Logger.LogInfo($"[MusicSync] Removed from CurrentPlayList: {musicUUID}");
                }

                // 如果移除的是当前播放的歌曲，自动播放下一首
                if (isCurrentlyPlaying)
                {
                    Logger.LogInfo($"[MusicSync] Current playing song was removed, skipping to next...");
                    musicService.SkipCurrentMusic(MusicChangeKind.Auto).Forget();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[MusicSync] Error removing music: {ex}");
            }
        }

        /// <summary>
        /// 加载所有模块（在MusicService.Load后调用）
        /// </summary>
        public static async UniTask LoadModulesAsync()
        {
            try
            {
                Logger.LogInfo("==================================================");
                Logger.LogInfo("Starting module loading...");
                Logger.LogInfo("==================================================");

                // 创建依赖加载器
                var dependencyLoader = new ModuleSystem.Services.DependencyLoader(PluginPath, Logger);

                // 获取插件实例的配置
                var pluginInstance = BepInEx.Bootstrap.Chainloader.PluginInfos.Values
                    .FirstOrDefault(p => p.Metadata.GUID == MyPluginInfo.PLUGIN_GUID)?.Instance as Plugin;
                var configFile = pluginInstance?.Config ?? new BepInEx.Configuration.ConfigFile(
                    System.IO.Path.Combine(BepInEx.Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.cfg"), true);

                // 创建模块上下文工厂（每个模块会获得独立的上下文和配置管理器）
                var contextFactory = new ModuleContextFactory(
                    PluginPath,
                    configFile,
                    Logger,
                    TagRegistry.Instance,
                    AlbumRegistry.Instance,
                    MusicRegistry.Instance,
                    EventBus.Instance,
                    DefaultCoverProvider.Instance,
                    CoreAudioLoader.Instance,
                    dependencyLoader
                );

                // 初始化模块加载器
                ModuleLoader.Initialize(ModulesPath, contextFactory, Logger);

                // 加载所有模块
                await ModuleLoader.Instance.LoadAllModulesAsync();

                // 将模块注册的歌曲同步到游戏
                await SyncMusicToGameAsync();

                Logger.LogInfo("==================================================");
                Logger.LogInfo($"Module loading completed! ({ModuleLoader.Instance.LoadedModules.Count} modules)");
                Logger.LogInfo("==================================================");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to load modules: {ex}");
            }
        }

        /// <summary>
        /// 将模块注册的音乐同步到游戏
        /// </summary>
        private static async UniTask SyncMusicToGameAsync()
        {
            if (MusicService_RemoveLimit_Patch.CurrentInstance == null)
            {
                Logger.LogWarning("MusicService not available, skipping sync");
                return;
            }

            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;

            // 获取游戏的音乐列表
            var allMusicList = Traverse.Create(musicService)
                .Field("_allMusicList")
                .GetValue<List<GameAudioInfo>>();

            var shuffleList = Traverse.Create(musicService)
                .Field("shuffleList")
                .GetValue<List<GameAudioInfo>>();

            if (allMusicList == null)
            {
                Logger.LogError("Cannot access _allMusicList");
                return;
            }

            // 获取所有模块注册的音乐
            var allMusic = MusicRegistry.Instance.GetAllMusic();
            var allTags = TagRegistry.Instance.GetAllTags();

            Logger.LogInfo($"Syncing {allMusic.Count} songs from {allTags.Count} tags...");

            int addedCount = 0;
            AudioTag allCustomTagBits = 0;

            foreach (var tag in allTags)
            {
                allCustomTagBits |= (AudioTag)tag.BitValue;
            }

            foreach (var musicInfo in allMusic)
            {
                // 检查是否已存在
                if (allMusicList.Any(m => m.UUID == musicInfo.UUID))
                    continue;

                // 获取对应的 Tag
                var tag = TagRegistry.Instance.GetTag(musicInfo.TagId);
                if (tag == null)
                {
                    Logger.LogWarning($"Tag not found for music: {musicInfo.Title} (TagId: {musicInfo.TagId})");
                    continue;
                }

                // 构造 AudioTag（模块 Tag + Local + 收藏状态）
                AudioTag audioTag = (AudioTag)tag.BitValue | AudioTag.Local;
                if (musicInfo.IsFavorite)
                {
                    audioTag |= AudioTag.Favorite;
                }

                // 创建 GameAudioInfo
                var gameAudio = new GameAudioInfo
                {
                    UUID = musicInfo.UUID,
                    Title = musicInfo.Title,
                    Credit = musicInfo.Artist,
                    Tag = audioTag,
                    PathType = AudioMode.LocalPc,
                    LocalPath = musicInfo.SourcePath,
                    IsUnlocked = true
                };

                allMusicList.Add(gameAudio);
                shuffleList?.Add(gameAudio);
                addedCount++;
            }

            Logger.LogInfo($"Synced {addedCount} songs to game");

            // 更新 CurrentAudioTag 包含所有自定义 Tag
            if (allCustomTagBits != 0)
            {
                var currentTag = SaveDataManager.Instance.MusicSetting.CurrentAudioTag.Value;
                SaveDataManager.Instance.MusicSetting.CurrentAudioTag.Value = currentTag | allCustomTagBits;
                Logger.LogInfo($"Updated CurrentAudioTag with custom tags: {allCustomTagBits}");
            }

            // 将自定义 Tag 添加到下拉菜单
            foreach (var tag in allTags)
            {
                ChillUIFramework.Music?.TagDropdown?.AddCustomTag(
                    (AudioTag)tag.BitValue,
                    new UIFramework.Core.TagDropdownItem
                    {
                        Tag = (AudioTag)tag.BitValue,
                        DisplayName = tag.DisplayName,
                        Priority = 100 + tag.SortOrder,
                        ShowInDropdown = true
                    }
                );
            }

            await UniTask.CompletedTask;
        }

        // Unity Update方法 - 每帧调用,用于定期健康检查
        private void Update()
        {
            try
            {
                healthCheckTimer += UnityEngine.Time.deltaTime;
                
                if (healthCheckTimer >= healthCheckInterval)
                {
                    healthCheckTimer = 0f;
                    KeyboardHookPatch.HealthCheck();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Update健康检查异常(已隔离): {ex.Message}");
            }
        }

        // Unity 生命周期方法 - 在应用退出时自动调用
        private void OnApplicationQuit()
        {
            Logger.LogInfo("OnApplicationQuit called - cleaning up...");
            
            // 保存播放状态（队列和历史）
            try
            {
                PlaybackStateManager.Instance?.ForceSave();
                Logger.LogInfo("Playback state saved!");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error saving playback state: {ex}");
            }
            
            // 卸载所有模块
            try
            {
                ModuleLoader.Instance?.UnloadAllModules();
                Logger.LogInfo("All modules unloaded!");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error unloading modules: {ex}");
            }
            
            // 清理键盘钩子
            KeyboardHookPatch.Cleanup();
            Logger.LogInfo("Keyboard hook cleanup completed!");
            
            // 清理UI框架
            try
            {
                ChillUIFramework.Cleanup();
                Logger.LogInfo("UI Framework cleanup completed!");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during UI Framework cleanup: {ex}");
            }
        }
    }
}
