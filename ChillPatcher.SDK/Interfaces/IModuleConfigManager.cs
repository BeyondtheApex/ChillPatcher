using BepInEx.Configuration;

namespace ChillPatcher.SDK.Interfaces
{
    /// <summary>
    /// 模块配置管理器接口
    /// </summary>
    public interface IModuleConfigManager
    {
        /// <summary>
        /// BepInEx 配置文件实例
        /// </summary>
        ConfigFile Config { get; }

        /// <summary>
        /// 绑定配置项
        /// </summary>
        /// <typeparam name="T">配置值类型</typeparam>
        /// <param name="section">配置分区</param>
        /// <param name="key">配置键</param>
        /// <param name="defaultValue">默认值</param>
        /// <param name="description">描述</param>
        /// <returns>配置条目</returns>
        ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, string description);

        /// <summary>
        /// 绑定配置项（带配置描述）
        /// </summary>
        ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, ConfigDescription description);

        /// <summary>
        /// 覆盖主程序的配置项
        /// 注意：只有在模块有更高优先级时才会生效
        /// </summary>
        /// <typeparam name="T">配置值类型</typeparam>
        /// <param name="section">配置分区</param>
        /// <param name="key">配置键</param>
        /// <param name="value">要覆盖的值</param>
        /// <returns>是否覆盖成功</returns>
        bool Override<T>(string section, string key, T value);

        /// <summary>
        /// 获取配置项的当前值
        /// </summary>
        /// <typeparam name="T">配置值类型</typeparam>
        /// <param name="section">配置分区</param>
        /// <param name="key">配置键</param>
        /// <param name="defaultValue">如果不存在时的默认值</param>
        /// <returns>配置值</returns>
        T GetValue<T>(string section, string key, T defaultValue = default);

        /// <summary>
        /// 设置配置项的值
        /// </summary>
        void SetValue<T>(string section, string key, T value);

        /// <summary>
        /// 检查配置项是否存在
        /// </summary>
        bool HasKey(string section, string key);

        /// <summary>
        /// 保存配置
        /// </summary>
        void Save();
    }
}
