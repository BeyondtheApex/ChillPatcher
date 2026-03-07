using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// ChillPatcher JS API 主入口
    /// 通过 OneJS 注入为全局对象 "chill"
    /// 
    /// JS 端用法：
    ///   chill.audio.pause()
    ///   chill.audio.getCurrentSong()
    ///   chill.stream.getSpectrum(256)
    ///   chill.playlist.getAllTags()
    ///   chill.events.on("playStarted", (data) => { ... })
    ///   chill.log.info("hello")
    ///   chill.custom.myModule.doSomething()
    /// </summary>
    public class ChillJSApi : IDisposable
    {
        private readonly ManualLogSource _logger;
        private readonly Dictionary<string, ICustomJSApi> _customApis
            = new Dictionary<string, ICustomJSApi>();

        /// <summary>
        /// 音频控制 API
        /// </summary>
        public ChillAudioApi audio { get; }

        /// <summary>
        /// 音频流/可视化 API
        /// </summary>
        public ChillStreamApi stream { get; }

        /// <summary>
        /// 歌单/播放列表 API
        /// </summary>
        public ChillPlaylistApi playlist { get; }

        /// <summary>
        /// 事件 API
        /// </summary>
        public ChillEventApi events { get; }

        /// <summary>
        /// 日志 API
        /// </summary>
        public ChillLogApi log { get; }

        /// <summary>
        /// 游戏 UI 树操作 API
        /// </summary>
        public ChillUIApi ui { get; }

        /// <summary>
        /// Mod 配置系统 API
        /// </summary>
        public ChillConfigApi config { get; }

        /// <summary>
        /// 模块管理 API
        /// </summary>
        public ChillModuleApi modules { get; }

        /// <summary>
        /// 网络请求 API
        /// </summary>
        public ChillNetApi net { get; }

        /// <summary>
        /// 文件 IO API
        /// </summary>
        public ChillIOApi io { get; }

        /// <summary>
        /// UI 实例管理 API
        /// </summary>
        public ChillInstanceApi instances { get; }

        /// <summary>
        /// 自定义 API 容器（模块可注册）
        /// </summary>
        public CustomApiContainer custom { get; }

        /// <summary>
        /// 插件版本
        /// </summary>
        public string version => MyPluginInfo.PLUGIN_VERSION;

        /// <summary>
        /// 插件根目录
        /// </summary>
        public string pluginPath => Plugin.PluginPath;

        public ChillJSApi(ManualLogSource logger, string uiDir)
        {
            _logger = logger;

            audio = new ChillAudioApi(logger);
            stream = new ChillStreamApi(logger);
            playlist = new ChillPlaylistApi(logger);
            events = new ChillEventApi(logger);
            log = new ChillLogApi(logger, uiDir);
            ui = new ChillUIApi(logger);
            config = new ChillConfigApi(logger);
            modules = new ChillModuleApi(logger);
            net = new ChillNetApi(logger);
            io = new ChillIOApi(logger);
            instances = new ChillInstanceApi(logger);
            custom = new CustomApiContainer();

            // 初始化事件订阅
            events.Initialize();

            logger.LogInfo("[JSApi] ChillJSApi initialized");
        }

        /// <summary>
        /// 注册自定义 API（供 C# 模块调用）
        /// JS 端通过 chill.custom.{name} 访问
        /// </summary>
        public void RegisterCustomApi(string name, ICustomJSApi api)
        {
            if (string.IsNullOrEmpty(name) || api == null) return;

            _customApis[name] = api;
            custom.Register(name, api);
            _logger.LogInfo($"[JSApi] Custom API registered: {name}");
        }

        /// <summary>
        /// 注销自定义 API
        /// </summary>
        public void UnregisterCustomApi(string name)
        {
            if (_customApis.Remove(name))
            {
                custom.Unregister(name);
                _logger.LogInfo($"[JSApi] Custom API unregistered: {name}");
            }
        }

        /// <summary>
        /// 获取自定义 API
        /// </summary>
        public ICustomJSApi GetCustomApi(string name)
        {
            _customApis.TryGetValue(name, out var api);
            return api;
        }

        public void Dispose()
        {
            events?.Dispose();
            log?.Dispose();
        }

        /// <summary>
        /// 全局实例（供模块注册自定义 API 时使用）
        /// </summary>
        public static ChillJSApi Instance { get; internal set; }
    }

    /// <summary>
    /// 自定义 API 容器，支持动态属性访问
    /// JS 端通过 chill.custom.xxx 访问模块自定义 API
    /// </summary>
    public class CustomApiContainer
    {
        private readonly Dictionary<string, ICustomJSApi> _apis
            = new Dictionary<string, ICustomJSApi>();

        public void Register(string name, ICustomJSApi api)
        {
            _apis[name] = api;
        }

        public void Unregister(string name)
        {
            _apis.Remove(name);
        }

        /// <summary>
        /// 通过名称获取自定义 API
        /// </summary>
        public ICustomJSApi get(string name)
        {
            _apis.TryGetValue(name, out var api);
            return api;
        }

        /// <summary>
        /// 获取所有已注册的自定义 API 名称
        /// </summary>
        public string getNames()
        {
            var keys = new string[_apis.Count];
            _apis.Keys.CopyTo(keys, 0);
            return JSApiHelper.ToJson(keys);
        }

        /// <summary>
        /// 检查是否存在指定名称的自定义 API
        /// </summary>
        public bool has(string name)
        {
            return _apis.ContainsKey(name);
        }
    }
}
