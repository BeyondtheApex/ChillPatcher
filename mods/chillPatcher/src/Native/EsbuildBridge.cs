using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using BepInEx.Logging;

namespace ChillPatcher.Native
{
    /// <summary>
    /// P/Invoke wrapper for the Go-based EsbuildBridge DLL.
    /// Provides esbuild build/watch and npm install without Node.js dependency.
    /// </summary>
    public static class EsbuildBridge
    {
        private const string DllName = "ChillEsbuildBridge";
        private static readonly ManualLogSource _log = Logger.CreateLogSource("EsbuildBridge");
        private static bool _dllLoaded;

        /// <summary>
        /// Progress callback delegate for npm install.
        /// Called from native code with: pkgPath (UTF-8), status ("skip"/"download"/"done"/"error"), msg.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NpmProgressCallback(IntPtr pkgPath, IntPtr status, IntPtr msg);

        static EsbuildBridge()
        {
            try
            {
                var pluginDir = Path.GetDirectoryName(typeof(Plugin).Assembly.Location);
                var arch = IntPtr.Size == 8 ? "x64" : "x86";
                var dllPath = Path.Combine(pluginDir, "native", arch, DllName + ".dll");

                if (!File.Exists(dllPath))
                {
                    _log.LogWarning($"EsbuildBridge DLL not found: {dllPath}");
                    return;
                }

                var handle = LoadLibrary(dllPath);
                if (handle == IntPtr.Zero)
                {
                    _log.LogError($"Failed to load EsbuildBridge DLL: error {Marshal.GetLastWin32Error()}");
                    return;
                }

                _dllLoaded = true;
                _log.LogInfo($"Loaded EsbuildBridge from: {dllPath}");
            }
            catch (Exception ex)
            {
                _log.LogError($"Exception loading EsbuildBridge: {ex}");
            }
        }

        public static bool IsLoaded => _dllLoaded;

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        #region P/Invoke Declarations

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "EsbuildBuild")]
        private static extern IntPtr EsbuildBuildNative(IntPtr configJson);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "EsbuildWatch")]
        private static extern int EsbuildWatchNative(IntPtr configJson);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "EsbuildStop")]
        private static extern void EsbuildStopNative(int watchId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "EsbuildStopAll")]
        private static extern void EsbuildStopAllNative();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "NpmInstallFromLock")]
        private static extern IntPtr NpmInstallNative(IntPtr workingDir);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "NpmInstallWithProgress")]
        private static extern IntPtr NpmInstallWithProgressNative(IntPtr workingDir, NpmProgressCallback callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "FreeString")]
        private static extern void FreeStringNative(IntPtr ptr);

        #endregion

        #region UTF-8 Marshalling

        private static IntPtr ToUTF8(string s)
        {
            if (s == null) return IntPtr.Zero;
            var bytes = Encoding.UTF8.GetBytes(s);
            var ptr = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            Marshal.WriteByte(ptr, bytes.Length, 0);
            return ptr;
        }

        private static string FromUTF8(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return null;
            int len = 0;
            while (Marshal.ReadByte(ptr, len) != 0) len++;
            if (len == 0) return "";
            var buf = new byte[len];
            Marshal.Copy(ptr, buf, 0, len);
            return Encoding.UTF8.GetString(buf);
        }

        private static string CallAndFree(IntPtr resultPtr)
        {
            var s = FromUTF8(resultPtr);
            if (resultPtr != IntPtr.Zero)
                FreeStringNative(resultPtr);
            return s;
        }

        #endregion

        #region Public API

        /// <summary>One-shot esbuild build. Returns null on success, error message on failure.</summary>
        public static string Build(string configJson)
        {
            if (!_dllLoaded) return "EsbuildBridge DLL not loaded";
            var ptr = ToUTF8(configJson);
            try
            {
                var result = CallAndFree(EsbuildBuildNative(ptr));
                return string.IsNullOrEmpty(result) ? null : result;
            }
            finally { Marshal.FreeHGlobal(ptr); }
        }

        /// <summary>Start esbuild watch mode. Returns watch ID (>0) on success, -1 on failure.</summary>
        public static int Watch(string configJson)
        {
            if (!_dllLoaded) return -1;
            var ptr = ToUTF8(configJson);
            try { return EsbuildWatchNative(ptr); }
            finally { Marshal.FreeHGlobal(ptr); }
        }

        /// <summary>Stop a specific watch by ID.</summary>
        public static void Stop(int watchId)
        {
            if (_dllLoaded) EsbuildStopNative(watchId);
        }

        /// <summary>Stop all active watches.</summary>
        public static void StopAll()
        {
            if (_dllLoaded) EsbuildStopAllNative();
        }

        /// <summary>Install npm packages from package-lock.json. Returns null on success.</summary>
        public static string NpmInstall(string workingDir)
        {
            if (!_dllLoaded) return "EsbuildBridge DLL not loaded";
            var ptr = ToUTF8(workingDir);
            try
            {
                var result = CallAndFree(NpmInstallNative(ptr));
                return string.IsNullOrEmpty(result) ? null : result;
            }
            finally { Marshal.FreeHGlobal(ptr); }
        }

        /// <summary>Install npm packages with per-package progress callback. Returns null on success.</summary>
        public static string NpmInstall(string workingDir, Action<string, string, string> onProgress)
        {
            if (!_dllLoaded) return "EsbuildBridge DLL not loaded";
            if (onProgress == null) return NpmInstall(workingDir);

            var ptr = ToUTF8(workingDir);
            // Must prevent GC of the delegate during the native call
            NpmProgressCallback callback = (pPkg, pStatus, pMsg) =>
            {
                var pkg = FromUTF8(pPkg);
                var status = FromUTF8(pStatus);
                var msg = FromUTF8(pMsg);
                try { onProgress(pkg, status, msg); } catch { }
            };
            // Pin to prevent GC
            var handle = GCHandle.Alloc(callback);
            try
            {
                var result = CallAndFree(NpmInstallWithProgressNative(ptr, callback));
                return string.IsNullOrEmpty(result) ? null : result;
            }
            finally
            {
                handle.Free();
                Marshal.FreeHGlobal(ptr);
            }
        }

        #endregion
    }
}
