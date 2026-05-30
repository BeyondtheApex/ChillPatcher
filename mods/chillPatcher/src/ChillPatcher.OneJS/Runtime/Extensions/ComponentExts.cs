using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace OneJS.Extensions {
    public static class ComponentExts {
        public static T GetCopyOf<T>(this Component comp, T other) where T : Component {
            Type type = comp.GetType();
            if (type != other.GetType()) return null; // type mis-match
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                 BindingFlags.Default | BindingFlags.DeclaredOnly;
            // PropertyInfo[] pinfos = type.GetProperties(flags);
            var pinfos = from property in type.GetProperties(flags)
                where !property.CustomAttributes.Any(attribute => attribute.AttributeType == typeof(ObsoleteAttribute))
                select property;
            foreach (var pinfo in pinfos) {
                if (pinfo.CanWrite) {
                    try {
                        pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                    } catch {
                    } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }
            FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos) {
                finfo.SetValue(comp, finfo.GetValue(other));
            }
            return comp as T;
        }

        public static bool TryGetComp(this GameObject go, string componentName, out Component comp) {
            var type = FindType(componentName);
            return go.TryGetComponent(type, out comp);
        }

        public static bool TryGetComp(this GameObject go, Type componentType, out Component comp) {
            return go.TryGetComponent(componentType, out comp);
        }

        public static Component GetComp(this Component comp, string componentName) {
            var type = FindType(componentName);
            return comp.GetComponent(type);
        }

        public static Component GetComp(this Component comp, Type componentType) {
            return comp.GetComponent(componentType);
        }

        public static T GetComp<T>(this Component comp, Type componentType) where T : Component {
            return comp.GetComponent<T>();
        }

        public static Component AddComp(this Component comp, string componentName) {
            var type = FindType(componentName);
            return comp.gameObject.AddComponent(type);
        }

        public static Component AddComp(this Component comp, Type componentType) {
            return comp.gameObject.AddComponent(componentType);
        }

        private static Type FindType(string name) {
            var type = FindTypeInAssembly(name, typeof(GameObject).Assembly);
            if (type == null)
                type = FindTypeInAssembly(name, typeof(MeshCollider).Assembly);
            // if (type == null)
            //     type = FindTypeInAssembly(name, typeof(ObiEmitter).Assembly);
            if (type == null)
                throw new Exception("[ComponentExtensions] Cannot Find type " + name);
            return type;
        }

        private static Type FindTypeInAssembly(string name, Assembly asm) {
            var res = asm.GetTypes().Where(t => t.Name == name).FirstOrDefault();
            return res;
        }
    }
}