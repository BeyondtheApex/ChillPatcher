using System;
using System.Linq;
using System.Reflection;
using Puerts;
using UnityEngine;
using UnityEngine.Scripting;

namespace OneJS.Extensions {
    public static class GameObjectExts {
        // // Be careful with this one. It somehow messes up the PuerTS side.
        // public static T AddComponent<T>(this GameObject go, T toAdd) where T : Component {
        //     return go.AddComponent<T>().GetCopyOf(toAdd) as T;
        // }

        public static bool HasComp<T>(this GameObject go) where T : Component {
            return go.TryGetComponent<T>(out T res);
        }

        public static T GetAddComp<T>(this GameObject go) where T : Component {
            T res;
            if (!go.TryGetComponent<T>(out res))
                res = go.AddComponent<T>();
            return res;
        }

        public static bool TryAddComp(this GameObject go, string componentName) {
            return TryAddComp(go, componentName, out Component comp);
        }

        public static bool TryAddComp(this GameObject go, string componentName, out Component comp) {
            var type = FindType(componentName);
            if (!go.TryGetComponent(type, out comp)) {
                comp = go.AddComponent(type);
                return true;
            }
            return false;
        }

        public static Component AddComp(this GameObject go, string componentName) {
            var type = FindType(componentName);
            return go.AddComponent(type);
        }

        public static Component AddComp(this GameObject go, Type componentType) {
            return go.AddComponent(componentType);
        }

        public static bool TryGetComp(this GameObject go, string componentName, out Component comp) {
            var components = go.GetComponents(typeof(Component));
            foreach (var component in components) {
                if (component.GetType().Name == componentName) {
                    comp = component;
                    return true;
                }
            }
            comp = null;
            return false;
        }

        public static bool TryGetComp(this GameObject go, Type componentType, out Component comp) {
            return go.TryGetComponent(componentType, out comp);
        }

        public static Component GetComp(this GameObject go, string componentName) {
            var components = go.GetComponents(typeof(Component));
            foreach (var component in components) {
                if (component.GetType().Name == componentName) {
                    return component;
                }
            }
            return null;
        }

        public static Component GetComp(this GameObject go, Type componentType) {
            return go.GetComponent(componentType);
        }

        /// <summary>
        /// Can be slow as it seaches all assemblies.
        /// </summary>
        public static Type FindType(string name) {
            if (String.IsNullOrEmpty(name))
                return null;
            var asms = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in asms) {
                var type = asm.GetType(name);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}