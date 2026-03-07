namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 自定义 JS API 接口
    /// 模块实现此接口来注册自定义 API，暴露给 Preact/OneJS 前端
    /// </summary>
    public interface ICustomJSApi
    {
        /// <summary>
        /// API 名称（JS 端通过 chill.custom.{Name} 访问）
        /// </summary>
        string Name { get; }
    }
}
