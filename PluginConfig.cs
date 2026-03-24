using BepInEx.Configuration;
using System.Collections.Generic;

namespace ChillPatcher
{
    public static class PluginConfig
    {
        // 配置分区版本号 - 用于自动重置过期配置
        // 当分区默认值发生变化时，增加对应版本号，旧配置会被重置为新默认值
        private static readonly Dictionary<string, int> SectionVersions = new Dictionary<string, int>
        {
            { "Language", 1 },
            { "SaveData", 1 },
            { "DLC", 1 },
            { "Steam", 1 },
            { "SaveSlot", 1 },
            { "Achievement", 1 },
            { "Keyboard", 1 },
            { "Rime", 1 },
            { "UI", 2 },        // v2: TagDropdownHeightOffset 默认值从50改为80
            { "Maintenance", 1 },
            { "Audio", 1 },     // 音频自动静音功能
            { "Build", 1 }      // npm/esbuild 构建开关
        };
        
        // 需要重置的分区集合
        private static readonly HashSet<string> _resetSections = new HashSet<string>();

        // 语言设置
        public static ConfigEntry<int> DefaultLanguage { get; private set; }

        // 用户ID设置
        public static ConfigEntry<string> OfflineUserId { get; private set; }

        // DLC设置
        public static ConfigEntry<bool> EnableDLC { get; private set; }

        // Steam补丁设置（统一开关）
        public static ConfigEntry<bool> EnableWallpaperEngineMode { get; private set; }

        // 多存档设置
        public static ConfigEntry<bool> UseMultipleSaveSlots { get; private set; }

        // 成就缓存设置
        public static ConfigEntry<bool> EnableAchievementCache { get; private set; }

        // 键盘钩子设置
        public static ConfigEntry<bool> EnableKeyboardHook { get; private set; }
        public static ConfigEntry<int> KeyboardHookInterval { get; private set; }

        // Rime输入法设置
        public static ConfigEntry<string> RimeSharedDataPath { get; private set; }
        public static ConfigEntry<string> RimeUserDataPath { get; private set; }
        public static ConfigEntry<bool> EnableRimeInputMethod { get; private set; }

        // UI 设置
        public static ConfigEntry<bool> HideEmptyTags { get; private set; }
        public static ConfigEntry<float> TagDropdownHeightMultiplier { get; private set; }
        public static ConfigEntry<float> TagDropdownHeightOffset { get; private set; }
        public static ConfigEntry<int> MaxTagsInTitle { get; private set; }
        public static ConfigEntry<bool> CleanInvalidMusicData { get; private set; }

        // 音频自动静音设置
        public static ConfigEntry<bool> EnableAutoMuteOnOtherAudio { get; private set; }
        public static ConfigEntry<float> AutoMuteVolumeLevel { get; private set; }
        public static ConfigEntry<float> AudioDetectionInterval { get; private set; }
        public static ConfigEntry<float> AudioResumeFadeInDuration { get; private set; }
        public static ConfigEntry<float> AudioMuteFadeOutDuration { get; private set; }

        // 系统媒体控制设置
        public static ConfigEntry<bool> EnableSystemMediaTransport { get; private set; }

        // npm/esbuild 构建开关
        public static ConfigEntry<bool> EnableNpmAndEsbuild { get; private set; }
        
        // 配置文件引用（用于版本重置）
        private static ConfigFile _configFile;

        public static void Initialize(ConfigFile config)
        {
            _configFile = config;
            _resetSections.Clear();
            
            // 加载版本号并标记需要重置的分区
            foreach (var kvp in SectionVersions)
            {
                var versionEntry = config.Bind("_Version", kvp.Key, 1, "配置分区版本号（请勿手动修改）");
                if (versionEntry.Value < kvp.Value)
                {
                    _resetSections.Add(kvp.Key);
                    Plugin.Logger.LogInfo($"[Config] 配置版本升级: {kvp.Key} (v{versionEntry.Value} → v{kvp.Value})");
                    versionEntry.Value = kvp.Value;
                }
            }
            
            // 加载实际配置
            // 语言设置 - 使用枚举值
            DefaultLanguage = config.Bind(
                "Language",
                "DefaultLanguage",
                3, // 默认值：ChineseSimplified = 3
                new ConfigDescription(
                    "默认游戏语言\n" +
                    "枚举值说明：\n" +
                    "0 = None (无)\n" +
                    "1 = Japanese (日语)\n" +
                    "2 = English (英语)\n" +
                    "3 = ChineseSimplified (简体中文)\n" +
                    "4 = ChineseTraditional (繁体中文)\n" +
                    "5 = Portuguese (葡萄牙语)",
                    new AcceptableValueRange<int>(0, 5)
                )
            );

            // 离线用户ID设置
            OfflineUserId = config.Bind(
                "SaveData",
                "OfflineUserId",
                "OfflineUser",
                "离线模式使用的用户ID，用于存档路径\n" +
                "修改此值可以使用不同的存档槽位，或读取原Steam用户的存档\n" +
                "例如：使用原Steam ID可以访问原来的存档"
            );

            // DLC设置
            EnableDLC = config.Bind(
                "DLC",
                "EnableDLC",
                false,
                "是否启用DLC功能\n" +
                "true = 启用DLC\n" +
                "false = 禁用DLC（默认）"
            );

            // WallpaperEngine补丁设置
            EnableWallpaperEngineMode = config.Bind(
                "WallpaperEngine",
                "EnableWallpaperEngineMode",
                false,
                "是否启用壁纸引擎兼容功能\n" +
                "true = 启用离线兼容模式，Steam未运行时自动回退到本地存档\n" +
                "  - 启动时不强制要求Steam运行（开机自启友好）\n" +
                "  - Steam就绪后自动重连并同步缓存成就\n" +
                "  - 强制使用配置的OfflineUserId作为存档路径\n" +
                "false = 使用游戏原本逻辑（默认，Steam未运行则退出）"
            );

            // 多存档设置
            UseMultipleSaveSlots = config.Bind(
                "SaveData",
                "UseMultipleSaveSlots",
                false,
                "是否使用多存档功能\n" +
                "true = 使用配置的离线用户ID作为存档路径，可以切换不同存档\n" +
                "false = 使用Steam ID作为存档路径（默认）\n" +
                "注意：启用后即使不在壁纸引擎模式下也会使用配置的存档路径"
            );

            // 成就缓存设置
            EnableAchievementCache = config.Bind(
                "Achievement",
                "EnableAchievementCache",
                true,
                "是否启用成就缓存功能\n" +
                "true = 所有成就都会缓存到本地作为备份（默认）\n" +
                "  - 壁纸引擎模式：仅缓存，不推送到Steam\n" +
                "  - 正常模式：缓存后继续推送到Steam\n" +
                "false = 禁用成就缓存（正常模式直接推送Steam，壁纸引擎模式丢弃成就）\n" +
                "缓存位置: C:\\Users\\(user)\\AppData\\LocalLow\\Nestopi\\Chill With You\\ChillPatcherCache\\[UserID]\n" +
                "注意：缓存永久保留，每次启动会自动同步到Steam"
            );

            // 键盘钩子开关
            EnableKeyboardHook = config.Bind(
                "KeyboardHook",
                "EnableKeyboardHook",
                true,
                new ConfigDescription(
                    "是否启用键盘钩子功能\n" +
                    "true = 启用键盘钩子（默认，支持中文输入和快捷键）\n" +
                    "false = 完全禁用键盘钩子和Rime输入法\n" +
                    "  - 禁用后无法使用中文搜索和自定义输入法\n" +
                    "  - 可减少后台线程和CPU占用\n" +
                    "  - 如果遇到键盘输入冲突问题，可尝试禁用"
                )
            );

            // 键盘钩子消息循环间隔
            KeyboardHookInterval = config.Bind(
                "KeyboardHook",
                "MessageLoopInterval",
                10,
                new ConfigDescription(
                    "键盘钩子消息循环检查间隔（毫秒）\n" +
                    "默认值：10ms（推荐）\n" +
                    "较小值：响应更快，CPU占用略高\n" +
                    "较大值：CPU占用低，响应略慢\n" +
                    "建议范围：1-10ms\n" +
                    "注意：仅在启用键盘钩子时有效",
                    new AcceptableValueRange<int>(1, 100)
                )
            );

            // Rime输入法配置
            EnableRimeInputMethod = config.Bind(
                "Rime",
                "EnableRimeInputMethod",
                true,
                "是否启用Rime输入法引擎\n" +
                "true = 启用Rime（默认）\n" +
                "false = 使用简单队列输入"
            );

            RimeSharedDataPath = config.Bind(
                "Rime",
                "SharedDataPath",
                "",
                "Rime共享数据目录路径（Schema配置文件）\n" +
                "留空则自动查找，优先级：\n" +
                "1. BepInEx/plugins/ChillPatcher/rime-data/shared\n" +
                "2. %AppData%/Rime\n" +
                "3. 此配置指定的自定义路径"
            );

            RimeUserDataPath = config.Bind(
                "Rime",
                "UserDataPath",
                "",
                "Rime用户数据目录路径（词库、用户配置）\n" +
                "留空则使用：BepInEx/plugins/ChillPatcher/rime-data/user"
            );

            // UI 配置
            HideEmptyTags = config.Bind(
                "UI",
                "HideEmptyTags",
                false,
                "是否在Tag下拉框中隐藏空标签\n" +
                "true = 隐藏没有歌曲的Tag\n" +
                "false = 显示所有Tag（默认）"
            );

            TagDropdownHeightMultiplier = config.Bind(
                "UI",
                "TagDropdownHeightMultiplier",
                1f,
                new ConfigDescription(
                    "Tag下拉框高度线性系数（斜率a）\n" +
                    "计算公式：最终高度 = a × 内容实际高度 + b\n" +
                    "默认：1",
                    new AcceptableValueRange<float>(0.1f, 3.0f)
                )
            );

            TagDropdownHeightOffset = config.Bind(
                "UI",
                "TagDropdownHeightOffset",
                80f,
                new ConfigDescription(
                    "Tag下拉框高度偏移量（常数b，单位：像素）\n" +
                    "计算公式：最终高度 = a × (按钮数 × 45) + b\n" +
                    "默认：80\n" +
                    "示例：100 = 增加偏移, 50 = 减少偏移",
                    new AcceptableValueRange<float>(-500f, 500f)
                )
            );

            MaxTagsInTitle = config.Bind(
                "UI",
                "MaxTagsInTitle",
                2,
                new ConfigDescription(
                    "标签下拉框标题最多显示的标签数量\n" +
                    "超过此数量将显示'等其他'\n" +
                    "默认：3",
                    new AcceptableValueRange<int>(1, 10)
                )
            );

            // UI 分区版本重置：强制覆盖为默认值
            if (_resetSections.Contains("UI"))
            {
                HideEmptyTags.Value = false;
                TagDropdownHeightMultiplier.Value = 1f;
                TagDropdownHeightOffset.Value = 80f;
                MaxTagsInTitle.Value = 2;
                Plugin.Logger.LogInfo("[Config] 已重置 UI 分区配置为默认值");
            }


            CleanInvalidMusicData = config.Bind(
                "Maintenance",
                "CleanInvalidMusicData",
                false,
                "清理无效的音乐数据（启动时执行一次）\n" +
                "删除收藏列表和本地音乐列表中不存在的文件\n" +
                "执行后会自动关闭此选项\n" +
                "默认：false"
            );

            // 音频自动静音配置
            EnableAutoMuteOnOtherAudio = config.Bind(
                "Audio",
                "EnableAutoMuteOnOtherAudio",
                false,
                "是否启用系统音频检测自动静音功能\n" +
                "true = 当检测到其他应用播放音频时，自动降低游戏音乐音量\n" +
                "false = 禁用此功能（默认）\n" +
                "注意：此功能使用 Windows WASAPI，仅在 Windows 上有效"
            );

            AutoMuteVolumeLevel = config.Bind(
                "Audio",
                "AutoMuteVolumeLevel",
                0.1f,
                new ConfigDescription(
                    "检测到其他音频时的目标音量（0-1）\n" +
                    "0 = 完全静音\n" +
                    "0.1 = 降低到10%（默认）\n" +
                    "1 = 不降低\n" +
                    "建议：0.05-0.2",
                    new AcceptableValueRange<float>(0f, 1f)
                )
            );

            AudioDetectionInterval = config.Bind(
                "Audio",
                "AudioDetectionInterval",
                1.0f,
                new ConfigDescription(
                    "检测其他音频的间隔（秒）\n" +
                    "默认：1秒\n" +
                    "较小值：响应更快，CPU占用略高\n" +
                    "较大值：CPU占用低，响应略慢\n" +
                    "建议范围：0.5-3秒",
                    new AcceptableValueRange<float>(0.1f, 10f)
                )
            );

            AudioResumeFadeInDuration = config.Bind(
                "Audio",
                "AudioResumeFadeInDuration",
                1.0f,
                new ConfigDescription(
                    "恢复音量的淡入时间（秒）\n" +
                    "当其他音频停止时，游戏音乐会在此时间内逐渐恢复音量\n" +
                    "默认：1秒",
                    new AcceptableValueRange<float>(0f, 5f)
                )
            );

            AudioMuteFadeOutDuration = config.Bind(
                "Audio",
                "AudioMuteFadeOutDuration",
                0.3f,
                new ConfigDescription(
                    "降低音量的淡出时间（秒）\n" +
                    "当检测到其他音频时，游戏音乐会在此时间内逐渐降低音量\n" +
                    "默认：0.3秒（快速响应）",
                    new AcceptableValueRange<float>(0f, 3f)
                )
            );

            // npm/esbuild 构建开关
            EnableNpmAndEsbuild = config.Bind(
                "Build",
                "EnableNpmAndEsbuild",
                false,
                "是否启用npm安装和esbuild监听构建\n" +
                "true = 启用npm自动安装依赖和esbuild实时监听构建\n" +
                "false = 直接使用工作目录中已编译的JS文件（默认）\n" +
                "注意：关闭时请确保@outputs目录中已有编译好的JS文件"
            );

            // 系统媒体控制配置
            EnableSystemMediaTransport = config.Bind(
                "Audio",
                "EnableSystemMediaTransport",
                false,
                "是否启用系统媒体控制功能 (SMTC)\n" +
                "true = 启用，在系统媒体浮窗中显示播放信息，支持媒体键控制\n" +
                "false = 禁用（默认）\n" +
                "注意：此功能需要 ChillSmtcBridge.dll，仅在 Windows 10/11 上有效"
            );

            Plugin.Logger.LogInfo("配置文件已加载:");
            Plugin.Logger.LogInfo($"  - 默认语言: {DefaultLanguage.Value}");
            Plugin.Logger.LogInfo($"  - 离线用户ID: {OfflineUserId.Value}");
            Plugin.Logger.LogInfo($"  - 启用DLC: {EnableDLC.Value}");
            Plugin.Logger.LogInfo($"  - 壁纸引擎模式: {EnableWallpaperEngineMode.Value}");
            Plugin.Logger.LogInfo($"  - 使用多存档: {UseMultipleSaveSlots.Value}");
            Plugin.Logger.LogInfo($"  - 启用成就缓存: {EnableAchievementCache.Value}");
            Plugin.Logger.LogInfo($"  - 键盘钩子间隔: {KeyboardHookInterval.Value}ms");
            Plugin.Logger.LogInfo($"  - 启用Rime: {EnableRimeInputMethod.Value}");
            if (!string.IsNullOrEmpty(RimeSharedDataPath.Value))
                Plugin.Logger.LogInfo($"  - Rime共享目录: {RimeSharedDataPath.Value}");
            if (!string.IsNullOrEmpty(RimeUserDataPath.Value))
                Plugin.Logger.LogInfo($"  - Rime用户目录: {RimeUserDataPath.Value}");
            Plugin.Logger.LogInfo($"  - 隐藏空Tag: {HideEmptyTags.Value}");
            Plugin.Logger.LogInfo($"  - 自动静音功能: {EnableAutoMuteOnOtherAudio.Value}");
            if (EnableAutoMuteOnOtherAudio.Value)
            {
                Plugin.Logger.LogInfo($"    - 目标音量: {AutoMuteVolumeLevel.Value}");
                Plugin.Logger.LogInfo($"    - 检测间隔: {AudioDetectionInterval.Value}秒");
            }
            Plugin.Logger.LogInfo($"  - 系统媒体控制: {EnableSystemMediaTransport.Value}");
            Plugin.Logger.LogInfo($"  - npm/esbuild构建: {EnableNpmAndEsbuild.Value}");
        }
    }
}
