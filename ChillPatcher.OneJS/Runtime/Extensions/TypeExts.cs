using System;
using System.Collections.Generic;
using System.Linq;

namespace OneJS.Extensions {
    public static class TypeExts {
        /// <summary>
        /// Returns the type name. If this is a generic type, appends
        /// the list of generic type arguments between angle brackets.
        /// (Does not account for embedded / inner generic arguments.)
        ///
        /// https://stackoverflow.com/a/66604069/150094
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.String.</returns>
        public static string GetFormattedName(this Type type) {
            if (type.IsGenericType) {
                string genericArguments = type.GetGenericArguments()
                    .Select(x => x.GetFormattedName())
                    .Aggregate((x1, x2) => $"{x1}, {x2}");
                return $"{type.Name.Substring(0, type.Name.IndexOf("`"))}"
                       + $"<{genericArguments}>";
            }
            return type.Name;
        }


        /// <summary>
        /// Returns a lazy enumerable of all the base types of this type including interfaces and classes
        /// </summary>
        public static IEnumerable<Type> GetBaseTypes(this Type type, bool includeSelf = false) {
            var first = type.GetBaseClasses(includeSelf).Concat(type.GetInterfaces());
            if (includeSelf && type.IsInterface)
                first.Concat(new Type[1] {type});

            return first;
        }

        /// <summary>
        /// Returns a lazy enumerable of all the base classes of this type
        /// </summary>
        public static IEnumerable<Type> GetBaseClasses(this Type type, bool includeSelf = false) {
            if (type is not {BaseType: not null}) yield break;

            if (includeSelf)
                yield return type;

            for (var current = type.BaseType; current != null; current = current.BaseType)
                yield return current;
        }
    }
}
