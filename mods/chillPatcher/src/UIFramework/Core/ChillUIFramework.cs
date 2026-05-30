using System;
using ChillPatcher.UIFramework.Core;
using ChillPatcher.UIFramework.Music;

namespace ChillPatcher.UIFramework
{
    /// <summary>
    /// ChillPatcher UI框架统一入口
    /// </summary>
    public static class ChillUIFramework
    {
        private static bool _initialized = false;
        private static MusicUIManager _musicManager;

        /// <summary>
        /// 音乐UI管理器
        /// </summary>
        public static IMusicUIManager Music => _musicManager;

        /// <summary>
        /// 框架版本
        /// </summary>
        public static Version Version => new Version(1, 0, 0);

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public static bool IsInitialized => _initialized;

        /// <summary>
        /// 初始化框架
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
            {
                BepInEx.Logging.Logger.CreateLogSource("ChillUIFramework").LogWarning("ChillUIFramework already initialized");
                return;
            }

            try
            {
                // 初始化音乐管理器
                _musicManager = new MusicUIManager();
                _musicManager.Initialize();

                _initialized = true;
                BepInEx.Logging.Logger.CreateLogSource("ChillUIFramework").LogInfo($"ChillUIFramework v{Version} initialized successfully");
            }
            catch (Exception ex)
            {
                BepInEx.Logging.Logger.CreateLogSource("ChillUIFramework").LogError($"Failed to initialize ChillUIFramework: {ex}");
                throw;
            }
        }

        /// <summary>
        /// 清理框架资源
        /// </summary>
        public static void Cleanup()
        {
            if (!_initialized)
                return;

            try
            {
                _musicManager?.Cleanup();
                _musicManager = null;
                _initialized = false;

                BepInEx.Logging.Logger.CreateLogSource("ChillUIFramework").LogInfo("ChillUIFramework cleaned up");
            }
            catch (Exception ex)
            {
                BepInEx.Logging.Logger.CreateLogSource("ChillUIFramework").LogError($"Error during ChillUIFramework cleanup: {ex}");
            }
        }
    }
}

