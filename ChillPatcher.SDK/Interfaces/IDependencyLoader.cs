using System;

namespace ChillPatcher.SDK.Interfaces
{
    /// <summary>
    /// 依赖加载器接口
    /// 允许模块注册和加载原生 DLL 依赖
    /// </summary>
    public interface IDependencyLoader
    {
        /// <summary>
        /// 加载原生 DLL
        /// </summary>
        /// <param name="dllName">DLL 文件名（不含路径）</param>
        /// <param name="moduleId">请求加载的模块 ID</param>
        /// <returns>加载成功返回 true</returns>
        bool LoadNativeLibrary(string dllName, string moduleId);

        /// <summary>
        /// 从模块目录加载原生 DLL
        /// </summary>
        /// <param name="dllPath">相对于模块目录的 DLL 路径</param>
        /// <param name="moduleId">请求加载的模块 ID</param>
        /// <returns>加载成功返回 true</returns>
        bool LoadNativeLibraryFromModulePath(string dllPath, string moduleId);

        /// <summary>
        /// 检查原生 DLL 是否已加载
        /// </summary>
        /// <param name="dllName">DLL 文件名</param>
        /// <returns>已加载返回 true</returns>
        bool IsLoaded(string dllName);

        /// <summary>
        /// 获取模块的原生 DLL 目录路径
        /// </summary>
        /// <param name="moduleId">模块 ID</param>
        /// <returns>原生 DLL 目录的完整路径</returns>
        string GetModuleNativePath(string moduleId);
    }
}
