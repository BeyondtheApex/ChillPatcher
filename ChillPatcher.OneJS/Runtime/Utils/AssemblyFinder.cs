using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OneJS.Utils {
    public class AssemblyFinder {
        static Assembly[] _assemblies;
        static Assembly[] Assemblies => _assemblies ??= AppDomain.CurrentDomain.GetAssemblies();

        static HashSet<string> _allNamespaces;
        static HashSet<string> AllNamespaces =>
            _allNamespaces ??= new HashSet<string>(
                Assemblies.SelectMany(a => a.GetTypes().Select(t => t.Namespace))
                    .Where(n => n != null)
            );

        /// <summary>
        /// Can be slow
        /// </summary>
        public static Type FindType(string name) {
            if (string.IsNullOrEmpty(name))
                return null;
            foreach (var asm in Assemblies) {
                var type = asm.GetType(name);
                if (type != null)
                    return type;
            }

            // foreach (var asm in _assemblies) {
            //     var types = asm.GetTypes();
            //     var type = types.Where(t => t.FullName == name).FirstOrDefault();
            //     if (type != null)
            //         return type;
            // }
            return null;
        }

        public static bool IsValidNamespace(string namespaceName) {
            return AllNamespaces.Contains(namespaceName);
        }

        public static List<Type> FindTypesInNamespace(string namespaceName) {
            var typesInNamespace = new List<Type>();
            if (!IsValidNamespace(namespaceName))
                return typesInNamespace;
            foreach (var asm in Assemblies) {
                foreach (var type in asm.GetTypes()) {
                    if (type.Namespace == namespaceName)
                        typesInNamespace.Add(type);
                }
            }

            return typesInNamespace;
        }
    }
}
