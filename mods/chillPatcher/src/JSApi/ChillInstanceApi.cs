using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// JS 端 UI 实例管理 API。
    /// 通过 chill.instances 访问。
    /// </summary>
    public class ChillInstanceApi
    {
        private readonly ManualLogSource _logger;

        public ChillInstanceApi(ManualLogSource logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 获取所有实例信息。
        /// </summary>
        public object[] list()
        {
            var result = new List<object>();
            foreach (var kv in OneJSBridge.Instances)
            {
                var inst = kv.Value;
                result.Add(new
                {
                    id = inst.Id,
                    workingDir = inst.WorkingDir,
                    entryFile = inst.EntryFile,
                    sortingOrder = inst.SortingOrder,
                    enabled = inst.Enabled,
                    interactive = inst.Interactive,
                    initialized = inst.IsInitialized
                });
            }
            return result.ToArray();
        }

        /// <summary>
        /// 添加一个新实例。
        /// </summary>
        public bool add(string id, string workingDir, string entryFile, int sortingOrder, bool enabled, bool interactive)
        {
            return OneJSBridge.AddInstance(id, workingDir, entryFile, sortingOrder, enabled, interactive) != null;
        }

        /// <summary>
        /// 移除一个实例。
        /// </summary>
        public bool remove(string id)
        {
            return OneJSBridge.RemoveInstance(id);
        }

        /// <summary>
        /// 启用或禁用一个实例。
        /// </summary>
        public void setEnabled(string id, bool enabled)
        {
            OneJSBridge.SetInstanceEnabled(id, enabled);
        }

        /// <summary>
        /// 热重载指定实例。
        /// </summary>
        public void reload(string id)
        {
            OneJSBridge.ReloadInstance(id);
        }

        /// <summary>
        /// 设置实例的层叠排序值。
        /// </summary>
        public void setSortingOrder(string id, int order)
        {
            var inst = OneJSBridge.GetInstance(id);
            if (inst != null)
            {
                inst.SetSortingOrder(order);
                UIInstanceConfig.SetSortingOrder(id, order);
            }
        }
    }
}
