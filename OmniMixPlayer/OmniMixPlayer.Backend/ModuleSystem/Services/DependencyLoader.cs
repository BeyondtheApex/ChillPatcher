using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Interfaces;

namespace OmniMixPlayer.Backend.ModuleSystem.Services
{
    public class DependencyLoader : IDependencyLoader
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string libFilename);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string moduleName);

        private readonly string _pluginDir;
        private readonly string _modulesDir;
        private readonly ILogger _logger;
        private readonly HashSet<string> _loadedLibraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public DependencyLoader(string pluginDir, ILogger logger)
        {
            _pluginDir = pluginDir;
            _modulesDir = Path.Combine(pluginDir, "modules");
            _logger = logger;
        }

        public bool LoadNativeLibrary(string dllName, string moduleId)
        {
            var arch = IntPtr.Size == 8 ? "x64" : "x86";
            var moduleNativePath = Path.Combine(_modulesDir, moduleId, "native", arch, dllName);

            if (File.Exists(moduleNativePath))
                return LoadFromPath(moduleNativePath, dllName);

            var mainNativePath = Path.Combine(_pluginDir, "native", arch, dllName);
            if (File.Exists(mainNativePath))
                return LoadFromPath(mainNativePath, dllName);

            _logger.LogWarning("[DependencyLoader] Native library not found: {DllName} (module: {ModuleId})", dllName, moduleId);
            return false;
        }

        public bool LoadNativeLibraryFromModulePath(string dllPath, string moduleId)
        {
            if (!File.Exists(dllPath))
            {
                _logger.LogWarning("[DependencyLoader] DLL not found at path: {Path} (module: {ModuleId})", dllPath, moduleId);
                return false;
            }
            return LoadFromPath(dllPath, Path.GetFileName(dllPath));
        }

        private bool LoadFromPath(string fullPath, string dllName)
        {
            if (_loadedLibraries.Contains(dllName))
            {
                _logger.LogDebug("[DependencyLoader] DLL already loaded: {DllName}", dllName);
                return GetModuleHandle(dllName) != IntPtr.Zero;
            }

            var handle = LoadLibrary(fullPath);
            if (handle != IntPtr.Zero)
            {
                _loadedLibraries.Add(dllName);
                _logger.LogInformation("[DependencyLoader] Loaded native DLL: {DllName} from {Path}", dllName, fullPath);
                return true;
            }
            else
            {
                _logger.LogWarning("[DependencyLoader] Failed to load DLL: {DllName} from {Path}", dllName, fullPath);
                return false;
            }
        }
        public bool IsLoaded(string dllName)
        {
            if (string.IsNullOrEmpty(dllName)) return false;
            return _loadedLibraries.Contains(dllName) || GetModuleHandle(dllName) != IntPtr.Zero;
        }

        public string GetModuleNativePath(string moduleId)
        {
            var arch = IntPtr.Size == 8 ? "x64" : "x86";
            return Path.Combine(_modulesDir, moduleId, "native", arch);
        }

        public void Dispose()
        {
        }
    }
}
