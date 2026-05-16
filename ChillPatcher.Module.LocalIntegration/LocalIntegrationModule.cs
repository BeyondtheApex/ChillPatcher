using System.Threading.Tasks;
using BepInEx.Configuration;
using ChillPatcher.SDK.Attributes;
using ChillPatcher.SDK.Interfaces;
using UnityEngine;

namespace ChillPatcher.Module.LocalIntegration
{
    [MusicModule(ModuleInfo.MODULE_ID, ModuleInfo.MODULE_NAME,
        Version = ModuleInfo.MODULE_VERSION,
        Author = ModuleInfo.MODULE_AUTHOR,
        Description = ModuleInfo.MODULE_DESCRIPTION,
        Priority = 1)]
    public sealed class LocalIntegrationModule : IMusicModule
    {
        private IModuleContext _context;
        private ConfigEntry<string> _host;
        private ConfigEntry<int> _port;
        private ConfigEntry<string> _token;
        private GameObject _runnerObject;
        private LocalIntegrationRunner _runner;

        public string ModuleId => ModuleInfo.MODULE_ID;
        public string DisplayName => ModuleInfo.MODULE_NAME;
        public string Version => ModuleInfo.MODULE_VERSION;
        public int Priority => 1;

        public ModuleCapabilities Capabilities => new ModuleCapabilities
        {
            CanDelete = false,
            CanFavorite = false,
            CanExclude = false,
            SupportsLiveUpdate = false,
            ProvidesCover = false,
            ProvidesAlbum = false
        };

        public Task InitializeAsync(IModuleContext context)
        {
            _context = context;
            RegisterConfig();
            return Task.CompletedTask;
        }

        public void OnEnable()
        {
            if (_runner != null) return;

            _runnerObject = new GameObject("ChillPatcher.LocalIntegrationBus");
            Object.DontDestroyOnLoad(_runnerObject);
            _runnerObject.hideFlags = HideFlags.HideAndDontSave;
            _runner = _runnerObject.AddComponent<LocalIntegrationRunner>();
            _runner.Initialize(_context.Logger, _host.Value, _port.Value, _token.Value);
            _runner.StartBus();
            _context.Logger.LogInfo($"[{DisplayName}] 已启用：http://{_host.Value}:{_port.Value}");
        }

        public void OnDisable()
        {
            StopRunner();
            _context?.Logger.LogInfo($"[{DisplayName}] 已禁用");
        }

        public void OnUnload()
        {
            StopRunner();
            _context?.Logger.LogInfo($"[{DisplayName}] 已卸载");
        }

        private void RegisterConfig()
        {
            var config = _context.ConfigManager;

            _host = config.BindDefault(
                "Host",
                "127.0.0.1",
                "本地集成总线监听地址。除非需要局域网访问，否则建议保持 127.0.0.1。"
            );

            _port = config.Bind(
                null,
                "Port",
                18792,
                new ConfigDescription(
                    "本地集成总线监听端口。",
                    new AcceptableValueRange<int>(1024, 65535)
                )
            );

            _token = config.BindDefault(
                "Token",
                "",
                "可选的 bearer token。留空时允许本地调用方不带鉴权访问。"
            );
        }

        private void StopRunner()
        {
            if (_runner != null)
            {
                _runner.StopBus();
                _runner = null;
            }

            if (_runnerObject != null)
            {
                Object.Destroy(_runnerObject);
                _runnerObject = null;
            }
        }
    }
}
