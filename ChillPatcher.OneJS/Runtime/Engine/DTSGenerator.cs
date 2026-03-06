using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace OneJS {
    [Serializable]
    public class DTSGenerator {
        [PlainString]
        [Tooltip("Use this list to restrict the assemblies you want to generate typings for. It's recommended to provide at least one Assembly name here. Use 'Assembly-CSharp' for the default (non-asmdef) assembly.")]
        public string[] assemblies = new string[] {
            "Assembly-CSharp",
            // "UnityEngine.CoreModule", "UnityEngine.PhysicsModule", "UnityEngine.UIElementsModule",
            // "UnityEngine.IMGUIModule", "UnityEngine.TextRenderingModule",
            // "Unity.Mathematics", "OneJS.Runtime"
        };
        [PlainString]
        [Tooltip("Use this list to restrict the namespaces you want to generate typings for. Keep list empty for no restrictions. Use empty string for global namespace.")]
        public string[] namespaces = new string[] {
            // "UnityEngine", "UnityEngine.UIElements", "Unity.Mathematics", "OneJS", "OneJS.Dom", "OneJS.Utils"
        };
        [PlainString]
        public string[] whitelistedTypes = new string[] { };
        [PlainString]
        public string[] blacklistedTypes = new string[] {
            // "UnityEngine.UIElements.ITransform", "UnityEngine.UIElements.ICustomStyle"
        };
        [Tooltip("Relative to the OneJS WorkingDir.")]
        public string savePath = "app.d.ts";
        [Tooltip("Check to only generate typings for the declared Assemblies.")]
        public bool strictAssemblies = false;
        [Tooltip("Check to only generate typings for the declared namespaces.")]
        public bool strictNamespaces = false;
        [Tooltip("Check to only generate exact typings (no supporting types will be generated).")]
        public bool exact = false;
        [Tooltip("Check to only generate typings for whitelisted types (supporting types will still be generated unless 'Exact' is checked).")]
        public bool whitelistOnly = false;
        [Tooltip("Check to also generate typings for the global objects defined on ScriptEngine.")]
        public bool includeGlobalObjects = true;

        /// <summary>
        /// Returns all types from the specified assemblies, namespaces, whitelisted types and blacklisted types.
        /// `strictAssemblies` and `strictNamespaces` are not being used here.
        /// </summary>
        public Type[] GetAllTypes() {
            var result = new HashSet<Type>();
            Assembly[] assembliesToSearch;
            if (assemblies != null && assemblies.Length > 0) {
                assembliesToSearch = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => assemblies.Contains(a.GetName().Name)).ToArray();
            } else {
                assembliesToSearch = AppDomain.CurrentDomain.GetAssemblies();
            }

            foreach (var asm in assembliesToSearch) {
                Type[] types;
                try {
                    types = asm.GetTypes();
                } catch (ReflectionTypeLoadException ex) {
                    types = ex.Types.Where(t => t != null).ToArray();
                }

                foreach (var t in types) {
                    // Namespace filtering
                    if (namespaces != null && namespaces.Length > 0) {
                        bool nsMatch = false;
                        foreach (var ns in namespaces) {
                            if (string.IsNullOrEmpty(ns)) {
                                if (string.IsNullOrEmpty(t.Namespace)) {
                                    nsMatch = true;
                                    break;
                                }
                            } else if (t.Namespace != null) {
                                if (t.Namespace == ns || t.Namespace.StartsWith(ns + ".")) {
                                    nsMatch = true;
                                    break;
                                }
                            }
                        }
                        if (!nsMatch)
                            continue;
                    }

                    // Blacklist filtering: exclude types whose FullName is in the list.
                    if (blacklistedTypes != null && blacklistedTypes.Length > 0) {
                        if (blacklistedTypes.Contains(t.FullName))
                            continue;
                    }

                    result.Add(t);
                }
            }

            // Add any whitelisted types that are not already in the set.
            if (whitelistedTypes != null && whitelistedTypes.Length > 0) {
                foreach (var typeName in whitelistedTypes) {
                    var type = Type.GetType(typeName);
                    if (type != null)
                        result.Add(type);
                }
            }

            return result.ToArray();
        }
    }
}