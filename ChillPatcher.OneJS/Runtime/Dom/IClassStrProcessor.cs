using OneJS.Dom;

namespace OneJS {
    public interface IClassStrProcessor {
        /// <summary>
        /// Process the class string. Class names that don't get processed are returned.
        /// </summary>
        /// <param name="classStr">String of class names</param>
        /// <returns>Un-processed class names</returns>
        string ProcessClassStr(string classStr, Dom.Dom dom);
    }
}
