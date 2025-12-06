using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using BepInEx.Logging;
using ChillPatcher.SDK.Interfaces;

namespace ChillPatcher.ModuleSystem.Services
{
    /// <summary>
    /// 依赖加载器实现
    /// 允许模块加载原生 DLL 依赖
    /// </summary>
    public class DependencyLoader : IDependencyLoader
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string libFilename);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string moduleName);

        private readonly string _pluginDir;
        private readonly string _modulesDir;
        private readonly ManualLogSource _logger;
        private readonly HashSet<string> _loadedLibraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public DependencyLoader(string pluginDir, ManualLogSource logger)
        {
            _pluginDir = pluginDir;
            _modulesDir = Path.Combine(pluginDir, "Modules");
            _logger = logger;
        }

        /// <inheritdoc/>
        public bool LoadNativeLibrary(string dllName, string moduleId)
        {
            // 首先检查模块自己的原生目录
            var arch = IntPtr.Size == 8 ? "x64" : "x86";
            var moduleNativePath = Path.Combine(_modulesDir, moduleId, "native", arch, dllName);
            
            if (File.Exists(moduleNativePath))
            {
                return LoadFromPath(moduleNativePath, dllName);
            }

            // 然后检查主插件的原生目录
            var mainNativePath = Path.Combine(_pluginDir, "native", arch, dllName);
            if (File.Exists(mainNativePath))
            {
                return LoadFromPath(mainNativePath, dllName);
            }

            _logger.LogWarning($"[DependencyLoader] 未找到原生库: {dllName} (模块: {moduleId})");
            return false;
        }

        /// <inheritdoc/>
        public bool LoadNativeLibraryFromModulePath(string dllPath, string moduleId)
        {
            var arch = IntPtr.Size == 8 ? "x64" : "x86";
            var fullPath = Path.Combine(_modulesDir, moduleId, "native", arch, dllPath);

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning($"[DependencyLoader] 未找到原生库: {fullPath}");
                return false;
            }

            return LoadFromPath(fullPath, Path.GetFileName(dllPath));
        }

        /// <inheritdoc/>
        public bool IsLoaded(string dllName)
        {
            if (_loadedLibraries.Contains(dllName))
                return true;

            // 检查系统是否已加载
            var handle = GetModuleHandle(dllName);
            return handle != IntPtr.Zero;
        }

        /// <inheritdoc/>
        public string GetModuleNativePath(string moduleId)
        {
            var arch = IntPtr.Size == 8 ? "x64" : "x86";
            return Path.Combine(_modulesDir, moduleId, "native", arch);
        }

        private bool LoadFromPath(string path, string dllName)
        {
            try
            {
                var handle = LoadLibrary(path);
                if (handle != IntPtr.Zero)
                {
                    _loadedLibraries.Add(dllName);
                    _logger.LogInfo($"[DependencyLoader] 已加载: {dllName} 从 {path}");
                    return true;
                }
                else
                {
                    var error = Marshal.GetLastWin32Error();
                    _logger.LogWarning($"[DependencyLoader] 加载失败: {dllName} (错误码: {error})");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[DependencyLoader] 加载异常: {dllName} - {ex.Message}");
                return false;
            }
        }
    }
}
