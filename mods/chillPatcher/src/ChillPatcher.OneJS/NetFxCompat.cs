using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OneJS.Compat
{
    /// <summary>
    /// Polyfills for .NET Framework 4.7.2 compatibility.
    /// OneJS targets newer .NET features; this file bridges the gap for BepInEx 5.
    /// </summary>
    internal static class NetFxCompat
    {
        /// <summary>
        /// Polyfill for Path.GetRelativePath (available in .NET Core 2.0+ only)
        /// </summary>
        public static string GetRelativePath(string relativeTo, string path)
        {
            var fromUri = new Uri(AppendDirectorySeparator(relativeTo));
            var toUri = new Uri(path);
            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());
            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

        private static string AppendDirectorySeparator(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
                !path.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                return path + Path.DirectorySeparatorChar;
            return path;
        }

        /// <summary>
        /// Polyfill for string.Replace(string, string, StringComparison)
        /// </summary>
        public static string Replace(string source, string oldValue, string newValue, StringComparison comparison)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(oldValue))
                return source;

            var sb = new System.Text.StringBuilder();
            int previousIndex = 0;
            int index = source.IndexOf(oldValue, comparison);
            while (index != -1)
            {
                sb.Append(source, previousIndex, index - previousIndex);
                sb.Append(newValue);
                previousIndex = index + oldValue.Length;
                index = source.IndexOf(oldValue, previousIndex, comparison);
            }
            sb.Append(source, previousIndex, source.Length - previousIndex);
            return sb.ToString();
        }

        /// <summary>
        /// Polyfill for GetMethod(string name, int genericParameterCount, Type[] types)
        /// available in .NET Core 2.1+ only. Finds a method by name and generic arity.
        /// </summary>
        public static MethodInfo GetMethodWithArity(Type type, string name, int genericParameterCount, Type[] parameterTypes)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in methods)
            {
                if (method.Name != name) continue;
                if (!method.IsGenericMethodDefinition && genericParameterCount > 0) continue;
                if (method.IsGenericMethodDefinition && method.GetGenericArguments().Length != genericParameterCount) continue;

                var parameters = method.GetParameters();
                if (parameterTypes != null && parameters.Length != parameterTypes.Length) continue;

                if (parameterTypes != null && parameterTypes.Length > 0)
                {
                    bool match = true;
                    for (int i = 0; i < parameterTypes.Length; i++)
                    {
                        if (parameters[i].ParameterType != parameterTypes[i])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (!match) continue;
                }

                return method;
            }
            return null;
        }

        /// <summary>
        /// Polyfill for Enum.TryParse(Type, string, bool, out object) available in .NET 6+
        /// </summary>
        public static bool EnumTryParse(Type enumType, string value, bool ignoreCase, out object result)
        {
            try
            {
                result = Enum.Parse(enumType, value, ignoreCase);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        /// <summary>
        /// Parse a float from a string with culture fallback (current → invariant)
        /// </summary>
        public static bool TryParseFloat(string value, NumberStyles style, out float result)
        {
            if (float.TryParse(value, style, CultureInfo.CurrentCulture, out result)) return true;
            if (float.TryParse(value, style, CultureInfo.InvariantCulture, out result)) return true;
            return false;
        }
    }
}
