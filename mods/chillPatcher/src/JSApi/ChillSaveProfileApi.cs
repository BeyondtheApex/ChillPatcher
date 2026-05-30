using System;
using BepInEx.Logging;
using ChillPatcher.Integration;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 存档 Profile 管理 API。
    /// JS 端通过 chill.saveProfile 访问。
    ///
    /// 用法：
    ///   chill.saveProfile.listProfiles()            // ["profile_a", "profile_b"]
    ///   chill.saveProfile.getActiveProfile()         // "" (主存档) 或 "profile_a"
    ///   chill.saveProfile.createProfile("work", ["SettingData", "EnviromentData"])
    ///   chill.saveProfile.createProfile("clean")     // 空白子存档
    ///   chill.saveProfile.createProfile("full", ["*"]) // 完整继承
    ///   chill.saveProfile.switchProfile("work")      // 切换到子存档（触发场景重载）
    ///   chill.saveProfile.switchProfile("")           // 回到主存档
    ///   chill.saveProfile.deleteProfile("work")
    ///   chill.saveProfile.getProfileInfo("work")
    /// </summary>
    public class ChillSaveProfileApi
    {
        private readonly ManualLogSource _logger;
        private readonly SaveProfileService _service;

        public ChillSaveProfileApi(ManualLogSource logger)
        {
            _logger = logger;
            _service = new SaveProfileService(logger);
        }

        /// <summary>locked 只读，C# 通过 service.Locked 控制</summary>
        public bool locked
        {
            get => _service.Locked;
        }

        /// <summary>列出所有子存档名称</summary>
        public string listProfiles() => JSApiHelper.ToJson(_service.listProfiles());

        /// <summary>获取当前激活的 profile 名（空字符串 = 主存档）</summary>
        public string getActiveProfile() => _service.getActiveProfile();

        /// <summary>
        /// 创建子存档。
        /// inheritFrom: 继承的数据名列表。null=空白, ["*"]=全部, ["SettingData","PlayerData"]=选择性。
        /// </summary>
        public bool createProfile(string name, string[] inheritFrom = null)
            => !locked && _service.createProfile(name, inheritFrom);

        /// <summary>删除子存档（不能删当前正在用的）</summary>
        public bool deleteProfile(string name) => !locked && _service.deleteProfile(name);

        /// <summary>
        /// 切换到指定子存档，或回到主存档（传空字符串/null）。
        /// 会触发场景重载（约2-3秒加载屏）。
        /// </summary>
        public void switchProfile(string name) { if (!locked) _service.switchProfile(name); }

        /// <summary>获取子存档信息</summary>
        public string getProfileInfo(string name)
            => JSApiHelper.ToJson(_service.getProfileInfo(name));

        /// <summary>当前是否在子存档中</summary>
        public bool isInSubProfile => SaveProfileService.IsInSubProfile;
    }
}
