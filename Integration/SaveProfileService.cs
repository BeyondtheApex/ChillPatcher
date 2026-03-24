using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using Bulbul;
using KanKikuchi.AudioManager;
using UnityEngine.SceneManagement;

namespace ChillPatcher.Integration
{
    /// <summary>
    /// 存档 Profile 管理服务。
    /// 在当前主存档目录下建立子目录作为"子存档"，支持：
    ///   - 列出 / 创建 / 删除子存档
    ///   - 切换到子存档或回到主存档（通过场景重载保证强隔离）
    ///   - 可选从主存档继承数据
    /// 
    /// 目录结构：
    ///   SaveData/Release/v2/{UserID}/              ← 主存档
    ///   SaveData/Release/v2/{UserID}/_profiles/a/  ← 子存档 "a"
    ///   SaveData/Release/v2/{UserID}/_profiles/b/  ← 子存档 "b"
    /// 
    /// 限制：
    ///   - 仅一层深度，不支持递归子存档
    ///   - 子存档之间可互相切换
    /// </summary>
    public sealed class SaveProfileService
    {
        private readonly ManualLogSource _logger;

        /// <summary>子存档目录名（位于主存档下）</summary>
        private const string ProfilesFolder = "_profiles";

        /// <summary>
        /// 当前激活的 profile 名。null/空 = 主存档。
        /// 这个值由 BulbulConstantPatch 在路径拼接时读取。
        /// </summary>
        public static string ActiveProfileName { get; private set; }

        /// <summary>是否当前处于子存档</summary>
        public static bool IsInSubProfile => !string.IsNullOrEmpty(ActiveProfileName);

        /// <summary>为 true 时，JS 侧变更操作被屏蔽。C# 侧不受影响。</summary>
        public bool Locked { get; set; }

        /// <summary>场景重载前触发，可用于清理</summary>
        public event Action OnBeforeSwitch;

        public SaveProfileService(ManualLogSource logger)
        {
            _logger = logger;
        }

        // ─── 路径工具 ────────────────────────────────────────

        /// <summary>
        /// 获取主存档的绝对目录路径。
        /// 基于 BulbulConstant 的原始逻辑（不含 profile 后缀）。
        /// </summary>
        private string GetMainSaveAbsolutePath()
        {
            // 使用 ES3 解析出完整路径
            var basePath = UnityEngine.Application.persistentDataPath;
            var relative = GetMainSaveRelativePath();
            return Path.Combine(basePath, relative);
        }

        /// <summary>
        /// 主存档的 ES3 相对路径（不含 profile）。
        /// </summary>
        private string GetMainSaveRelativePath()
        {
            string userId;
            if (PluginConfig.EnableWallpaperEngineMode.Value
                || PluginConfig.UseMultipleSaveSlots.Value)
            {
                userId = PluginConfig.OfflineUserId.Value;
            }
            else
            {
                try { userId = Steamworks.SteamUser.GetSteamID().ToString(); }
                catch { userId = PluginConfig.OfflineUserId.Value; }
            }
            return Path.Combine("SaveData", "Release", "v2", userId);
        }

        /// <summary>_profiles 目录的绝对路径</summary>
        private string GetProfilesDirectory()
        {
            return Path.Combine(GetMainSaveAbsolutePath(), ProfilesFolder);
        }

        /// <summary>某个子存档的绝对路径</summary>
        private string GetProfileAbsolutePath(string name)
        {
            return Path.Combine(GetProfilesDirectory(), SanitizeName(name));
        }

        /// <summary>
        /// 拿到当前活跃存档的 ES3 相对路径（供 BulbulConstantPatch 使用）。
        /// </summary>
        public static string GetActiveSaveRelativePath(string baseRelativePath)
        {
            if (string.IsNullOrEmpty(ActiveProfileName))
                return baseRelativePath;
            return Path.Combine(baseRelativePath, ProfilesFolder, ActiveProfileName);
        }

        // ─── 公开 API ───────────────────────────────────────

        /// <summary>
        /// 列出所有子存档名称。
        /// </summary>
        public List<string> listProfiles()
        {
            var dir = GetProfilesDirectory();
            if (!Directory.Exists(dir))
                return new List<string>();

            return Directory.GetDirectories(dir)
                .Select(d => Path.GetFileName(d))
                .Where(n => !string.IsNullOrEmpty(n))
                .OrderBy(n => n)
                .ToList();
        }

        /// <summary>
        /// 获取当前激活的 profile 名。空字符串 = 主存档。
        /// </summary>
        public string getActiveProfile()
        {
            return ActiveProfileName ?? "";
        }

        /// <summary>
        /// 创建子存档。
        /// </summary>
        /// <param name="name">子存档名称（仅字母数字下划线横线）</param>
        /// <param name="inheritFrom">
        ///   要继承的数据文件名列表（如 ["PlayerData", "SettingData"]）。
        ///   传 null 或空 → 空白存档。传 ["*"] → 继承全部。
        /// </param>
        public bool createProfile(string name, string[] inheritFrom = null)
        {
            name = SanitizeName(name);
            if (string.IsNullOrEmpty(name))
            {
                _logger.LogWarning("[SaveProfile] Invalid profile name");
                return false;
            }

            var profileDir = GetProfileAbsolutePath(name);
            if (Directory.Exists(profileDir))
            {
                _logger.LogWarning($"[SaveProfile] Profile '{name}' already exists");
                return false;
            }

            Directory.CreateDirectory(profileDir);
            _logger.LogInfo($"[SaveProfile] Created profile directory: {profileDir}");

            // 继承数据
            if (inheritFrom != null && inheritFrom.Length > 0)
            {
                var mainDir = GetMainSaveAbsolutePath();
                CopyFiles(mainDir, profileDir, inheritFrom);
            }

            return true;
        }

        /// <summary>
        /// 删除子存档（不能删除当前正在使用的）。
        /// </summary>
        public bool deleteProfile(string name)
        {
            name = SanitizeName(name);
            if (string.IsNullOrEmpty(name))
                return false;

            if (string.Equals(ActiveProfileName, name, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"[SaveProfile] Cannot delete active profile '{name}'. Switch away first.");
                return false;
            }

            var profileDir = GetProfileAbsolutePath(name);
            if (!Directory.Exists(profileDir))
            {
                _logger.LogWarning($"[SaveProfile] Profile '{name}' does not exist");
                return false;
            }

            Directory.Delete(profileDir, recursive: true);
            _logger.LogInfo($"[SaveProfile] Deleted profile '{name}'");
            return true;
        }

        /// <summary>
        /// 切换到指定子存档，或回到主存档（name 为 null/空）。
        /// 会触发场景重载。
        /// </summary>
        public void switchProfile(string name)
        {
            name = string.IsNullOrWhiteSpace(name) ? null : SanitizeName(name);

            // 验证目标存在
            if (name != null)
            {
                var profileDir = GetProfileAbsolutePath(name);
                if (!Directory.Exists(profileDir))
                {
                    _logger.LogError($"[SaveProfile] Profile '{name}' does not exist, cannot switch");
                    return;
                }
            }

            var current = ActiveProfileName ?? "(main)";
            var target = name ?? "(main)";
            _logger.LogInfo($"[SaveProfile] Switching: {current} → {target}");

            // 1) 保存当前存档
            try
            {
                if (SaveDataManager.HasInstance)
                {
                    SaveDataManager.Instance.SaveAll();
                    _logger.LogInfo("[SaveProfile] SaveAll() completed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[SaveProfile] SaveAll failed: {ex.Message}");
            }

            // 2) 通知订阅者
            try { OnBeforeSwitch?.Invoke(); }
            catch (Exception ex) { _logger.LogError($"[SaveProfile] OnBeforeSwitch error: {ex}"); }

            // 2.5) 清理 GameApi 事件订阅（指向旧 RoomScene 服务）— 遍历所有 UIInstance
            try
            {
                foreach (var kv in OneJSBridge.Instances)
                {
                    kv.Value.JSApi?.game?.ResetForSceneReload();
                }
            }
            catch (Exception ex) { _logger.LogWarning($"[SaveProfile] Game API reset error: {ex.Message}"); }

            // 2.5.1) 重置 UI 重排列状态
            try { Patches.UIRearrangePatch.Reset(); }
            catch (Exception ex) { _logger.LogWarning($"[SaveProfile] UIRearrange reset error: {ex.Message}"); }

            // 2.5.2) 重置虚拟滚动状态（使场景重载后能重新初始化）
            try { Patches.UIFramework.MusicUI_VirtualScroll_Patch.ResetForSceneReload(); }
            catch (Exception ex) { _logger.LogWarning($"[SaveProfile] VirtualScroll reset error: {ex.Message}"); }

            // 2.5.3) 重置队列按钮的 MusicService 订阅（旧 MusicService 将被销毁）
            try { Patches.UIFramework.MusicPlayListButtons_QueueActions_Patch.ResetForSceneReload(); }
            catch (Exception ex) { _logger.LogWarning($"[SaveProfile] QueueActions reset error: {ex.Message}"); }

            // 2.6) 让音乐继续播放（MusicManager 存活过场景切换）
            try { Patches.UIFramework.PlayQueuePatch.PrepareForSceneReload(); }
            catch (Exception ex) { _logger.LogWarning($"[SaveProfile] Music prepare error: {ex.Message}"); }

            // 2.7) 停止环境音效（AmbientBGMManager 是 DontDestroyOnLoad 的单例，不会因场景切换销毁）
            //      新场景初始化时 ApplyWindowBySavedata() 会根据新存档数据重新播放
            try
            {
                var ambientMgr = SingletonMonoBehaviour<AmbientBGMManager>.Instance;
                if (ambientMgr != null)
                {
                    ambientMgr.Stop();
                    _logger.LogInfo("[SaveProfile] Stopped ambient BGM for profile switch");
                }
            }
            catch (Exception ex) { _logger.LogWarning($"[SaveProfile] Ambient BGM stop error: {ex.Message}"); }

            // 3) 设置新的 profile
            ActiveProfileName = name;

            // 4) 销毁旧的 SaveDataManager 单例（通过反射）
            NukeSaveDataManagerInstance();

            // 5) 重载 Entry 场景 — 重走完整启动流程
            _logger.LogInfo("[SaveProfile] Loading Entry scene...");
            
            // 注册一次性 sceneLoaded 回调 → 延迟通知 JS 插件场景已重载
            void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                // 等待 RoomScene 加载完毕（Entry → RoomScene 需要一段时间）
                if (scene.name != "RoomScene") return;
                SceneManager.sceneLoaded -= OnSceneLoaded;
                _logger.LogInfo($"[SaveProfile] RoomScene loaded, delaying 1.5s for NotifySceneReloaded...");
                // 延迟 1.5 秒：确保 FacilityMusic.Setup、MusicUI.Setup、UIRearrange 等全部完成
                Patches.CoroutineRunner.Instance.RunDelayed(1.5f, () =>
                {
                    try
                    {
                        // 遍历所有 UIInstance 通知场景重载
                        foreach (var kv in OneJSBridge.Instances)
                        {
                            var game = kv.Value.JSApi?.game;
                            if (game != null)
                            {
                                _logger.LogInfo($"[SaveProfile] NotifySceneReloaded → instance={kv.Key}");
                                game.NotifySceneReloaded();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"[SaveProfile] NotifySceneReloaded error: {ex.Message}");
                    }
                });
            }
            SceneManager.sceneLoaded += OnSceneLoaded;

            BuildSetupOverlay.Show($"Switching profile: {current} \u2192 {target}");
            SceneManager.LoadScene("Entry");
        }

        /// <summary>
        /// 获取子存档信息。
        /// </summary>
        public Dictionary<string, object> getProfileInfo(string name)
        {
            name = SanitizeName(name);
            var profileDir = GetProfileAbsolutePath(name);
            if (!Directory.Exists(profileDir))
                return null;

            var dirInfo = new DirectoryInfo(profileDir);
            var files = dirInfo.GetFiles("*", SearchOption.TopDirectoryOnly);
            long totalSize = files.Sum(f => f.Length);

            return new Dictionary<string, object>
            {
                ["name"] = name,
                ["path"] = profileDir,
                ["fileCount"] = files.Length,
                ["totalSizeBytes"] = totalSize,
                ["createdAt"] = dirInfo.CreationTimeUtc.ToString("o"),
                ["isActive"] = string.Equals(ActiveProfileName, name,
                                   StringComparison.OrdinalIgnoreCase)
            };
        }

        // ─── 内部工具 ───────────────────────────────────────

        /// <summary>反射清空 SaveDataManager._instance</summary>
        private void NukeSaveDataManagerInstance()
        {
            try
            {
                var field = typeof(SaveDataManager).GetField(
                    "_instance",
                    System.Reflection.BindingFlags.Static
                    | System.Reflection.BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(null, null);
                    _logger.LogInfo("[SaveProfile] SaveDataManager._instance set to null");
                }
                else
                {
                    _logger.LogWarning("[SaveProfile] Could not find _instance field via reflection");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[SaveProfile] Reflection error: {ex}");
            }
        }

        /// <summary>复制文件到目标目录</summary>
        private void CopyFiles(string sourceDir, string destDir, string[] patterns)
        {
            if (!Directory.Exists(sourceDir)) return;

            bool copyAll = patterns.Length == 1 && patterns[0] == "*";

            var sourceFiles = Directory.GetFiles(sourceDir);
            foreach (var srcFile in sourceFiles)
            {
                var fileName = Path.GetFileName(srcFile);

                // 跳过 _profiles 目录中的内容（不应该有文件在此）
                if (fileName.StartsWith("_")) continue;

                if (!copyAll)
                {
                    // 检查文件名是否匹配任一 pattern（不含扩展名对比）
                    var nameNoExt = Path.GetFileNameWithoutExtension(fileName);
                    bool match = patterns.Any(p =>
                        fileName.Contains(p, StringComparison.OrdinalIgnoreCase)
                        || nameNoExt.Contains(p, StringComparison.OrdinalIgnoreCase));
                    if (!match) continue;
                }

                var destFile = Path.Combine(destDir, fileName);
                File.Copy(srcFile, destFile, overwrite: true);
                _logger.LogInfo($"[SaveProfile] Copied: {fileName}");
            }
        }

        /// <summary>清理名称：仅保留字母数字下划线横线</summary>
        private static string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            // 移除不安全字符
            var sanitized = new string(name.Where(c =>
                char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
            return string.IsNullOrEmpty(sanitized) ? null : sanitized;
        }
    }
}
