using System;
using BepInEx.Logging;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 模块管理 API (IPC 版本)
    /// Module loading now handled by OmniMixPlayer backend
    /// </summary>
    public class ChillModuleApi
    {
        private readonly ManualLogSource _logger;

        public ChillModuleApi(ManualLogSource logger)
        {
            _logger = logger;
        }

        public string getAll()
        {
            return "[]"; // IPC: module list from OmniMixPlayer not yet bridged to JS
        }

        public string get(string moduleId)
        {
            return "null";
        }

        public string getIds()
        {
            return "[]";
        }

        public bool isEnabled(string moduleId)
        {
            return false;
        }

        public void enable(string moduleId) { }

        public void disable(string moduleId) { }
    }
}
