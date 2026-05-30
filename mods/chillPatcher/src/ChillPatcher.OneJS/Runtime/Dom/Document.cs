using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace OneJS.Dom {
    public class ElementCreationOptions {
        public string @is;
    }

    public struct ElementTypeInfo {
        public string tagName;
        public Type type;
        public FieldInfo documentField;
    }

    public class Document : IDocument {
        public ScriptEngine scriptEngine => _scriptEngine;
        public VisualElement Root { get { return _root; } }
        public Dom body => _body;

        Dom _body;
        VisualElement _root;
        ScriptEngine _scriptEngine;
        List<StyleSheet> _runtimeStyleSheets = new List<StyleSheet>();

        Dictionary<VisualElement, Dom> _elementToDomLookup = new();

        Dictionary<string, ElementTypeInfo> _tagCache = new();
        Dictionary<string, Texture2D> _imageCache = new();
        Dictionary<string, Font> _fontCache = new();
        Dictionary<string, FontDefinition> _fontDefinitionCache = new();
        Type[] _tagTypes;
        WebApi _webApi = new WebApi();

        public Document(VisualElement root, ScriptEngine scriptEngine) {
            _root = root;
            _body = new Dom(_root, this);
            _scriptEngine = scriptEngine;
            _tagTypes = GetAllVisualElementTypes();
        }

        public void addRuntimeUSS(string uss) {
            var ss = ScriptableObject.CreateInstance<StyleSheet>();
            var builder = new OneJS.CustomStyleSheets.CustomStyleSheetImporterImpl(_scriptEngine);
            builder.BuildStyleSheet(ss, uss);
            if (builder.importErrors.hasErrors) {
                Debug.LogError($"Runtime USS Error(s)");
                foreach (var error in builder.importErrors) {
                    Debug.LogError(error);
                }
                return;
            }
            _runtimeStyleSheets.Add(ss);
            _root.styleSheets.Add(ss);
        }

        public void removeRuntimeStyleSheet(StyleSheet sheet) {
            _root.styleSheets.Remove(sheet);
            Object.Destroy(sheet);
        }

        public void clearRuntimeStyleSheets() {
            foreach (var sheet in _runtimeStyleSheets) {
                _root.styleSheets.Remove(sheet);
                Object.Destroy(sheet);
            }
            _runtimeStyleSheets.Clear();
        }

        public Dom createElement(string tagName) {
            ElementTypeInfo typeInfo;
            // Try to lookup from tagCache, may still be null if not a VE type.
            if (!_tagCache.TryGetValue(tagName, out typeInfo)) {
                var type = GetVisualElementType(tagName);
                var fieldInfo = type?.GetField("_document", BindingFlags.Instance | BindingFlags.NonPublic);
                typeInfo = new ElementTypeInfo() {
                    tagName = tagName,
                    type = GetVisualElementType(tagName),
                    documentField = fieldInfo != null && typeof(IDocument).IsAssignableFrom(fieldInfo.FieldType) ? fieldInfo : null
                };
                _tagCache[tagName] = typeInfo;
            }

            if (typeInfo.type == null) {
                return new Dom(new VisualElement(), this);
            }
            var obj = Activator.CreateInstance(typeInfo.type);
            if (typeInfo.documentField != null) {
                typeInfo.documentField.SetValue(obj, this);
            }
            return new Dom(obj as VisualElement, this);
        }

        public Dom createElement(string tagName, ElementCreationOptions options) {
            return createElement(tagName);
        }

        public Dom createElementNS(string ns, string tagName, ElementCreationOptions options) { // namespace currently not used
            return createElement(tagName);
        }

        public Dom createTextNode(string text) {
            var tn = new TextElement();
            tn.text = text;
            return new Dom(tn, this);
        }

        /// <summary>
        /// finds and returns an Element object representing the element whose id property matches the specified string.
        /// </summary>
        /// <param name="id">Element ID</param>
        /// <returns>Dom element or null if not found</returns>
        public Dom getElementById(string id) {
            //var firstElement = _root.Q<VisualElement>(id);
            var elem = body.First((d) => d.ve.name == id);
            return elem;
        }

        public Dom[] querySelectorAll(string selector) {
            var elems = _root.Query<VisualElement>(selector).Build();
            var doms = elems
                .Select(elem => _elementToDomLookup.TryGetValue(elem, out var dom) ? dom : null)
                .Where(dom => dom != null)
                .ToArray();
            return doms;
        }

        public Dom getDomFromVE(VisualElement ve) {
            if (_elementToDomLookup.TryGetValue(ve, out var dom)) {
                return dom;
            }
            return null;
        }

        public void clearCache() {
            foreach (var tex in _imageCache.Values) {
                if (tex != null) Object.Destroy(tex);
            }
            _imageCache.Clear();
            _fontCache.Clear();
            _fontDefinitionCache.Clear();
        }

        public Coroutine loadRemoteImage(string url, Action<Texture2D> callback) {
            if (_imageCache.TryGetValue(url, out var tex)) {
                callback(tex);
                return null;
            }
            return _webApi.getImage(url, (tex) => {
                if (tex == null) {
                    Debug.LogError($"Failed to load image: {url}");
                    return;
                }
                _imageCache[url] = tex;
                callback(tex);
            });
        }

        /// <summary>
        /// Loads an image from the specified path and returns a Texture2D object.
        /// </summary>
        /// <param name="path">Relative to the WorkingDir</param>
        public Texture2D loadImage(string path, FilterMode filterMode = FilterMode.Bilinear) {
            if (_imageCache.TryGetValue(path, out var texture)) {
                return texture;
            }
            try {
                var fullpath = Path.IsPathRooted(path) ? path : Path.Combine(_scriptEngine.WorkingDir, path);
                var rawData = File.ReadAllBytes(fullpath);
                Texture2D tex = new Texture2D(2, 2); // Create an empty Texture; size doesn't matter
                tex.LoadImage(rawData);
                tex.filterMode = filterMode;
                _imageCache[path] = tex; // caches the original path
                return tex;
            } catch (Exception) {
                Debug.LogError($"Failed to load image: {path}");
                // Debug.LogError(e);
                return null;
            }
        }

        /// <summary>
        /// Loads a font from the specified path and returns a Font object.
        /// </summary>
        /// <param name="path">Relative to the WorkingDir</param>
        public Font loadFont(string path) {
            if (_fontCache.TryGetValue(path, out var f)) {
                return f;
            }
            try {
                path = Path.IsPathRooted(path) ? path : Path.Combine(_scriptEngine.WorkingDir, path);
                var font = new Font(path);
                _fontCache[path] = font;
                return font;
            } catch (Exception) {
                Debug.LogError($"Failed to load font: {path}");
                // Debug.LogError(e);
                return null;
            }
        }

        /// <summary>
        /// Loads a font from the specified path and returns a FontDefinition object.
        /// </summary>
        /// <param name="path">Relative to the WorkingDir</param>
        public FontDefinition loadFontDefinition(string path) {
            if (_fontDefinitionCache.TryGetValue(path, out var fd)) {
                return fd;
            }
            path = Path.IsPathRooted(path) ? path : Path.Combine(_scriptEngine.WorkingDir, path);
            var font = new Font(path);
            _fontCache[path] = font;
            return FontDefinition.FromFont(font);
        }

        public static object createStyleEnum(int v, Type type) {
            Type myParameterizedSomeClass = typeof(StyleEnum<>).MakeGenericType(type);
            ConstructorInfo constr = myParameterizedSomeClass.GetConstructor(new[] { type });
            object instance = constr.Invoke(new object[] { v });
            return instance;
        }

        public static object createStyleEnumWithKeyword(StyleKeyword keyword, Type type) {
            Type myParameterizedSomeClass = typeof(StyleEnum<>).MakeGenericType(type);
            ConstructorInfo constr = myParameterizedSomeClass.GetConstructor(new[] { typeof(StyleKeyword) });
            object instance = constr.Invoke(new object[] { keyword });
            return instance;
        }

        public static object createStyleList(object v, Type type) {
            Type listType = typeof(List<>).MakeGenericType(type);
            Type myParameterizedSomeClass = typeof(StyleList<>).MakeGenericType(type);
            ConstructorInfo constr = myParameterizedSomeClass.GetConstructor(new[] { listType });
            object instance = constr.Invoke(new object[] { v });
            return instance;
        }

        public static object createStyleListWithKeyword(StyleKeyword keyword, Type type) {
            Type listType = typeof(List<>).MakeGenericType(type);
            Type myParameterizedSomeClass = typeof(StyleList<>).MakeGenericType(listType);
            ConstructorInfo constr = myParameterizedSomeClass.GetConstructor(new[] { typeof(StyleKeyword) });
            object instance = constr.Invoke(new object[] { keyword });
            return instance;
        }

        // public void addEventListener(string name, JsValue jsval, bool useCapture = false) {
        //     _body.addEventListener(name, jsval, useCapture);
        // }
        //
        // public void removeEventListener(string name, JsValue jsval, bool useCapture = false) {
        //     _body.removeEventListener(name, jsval, useCapture);
        // }

        void IDocument.AddCachingDom(Dom dom) {
            _elementToDomLookup[dom.ve] = dom;
        }

        void IDocument.RemoveCachingDom(Dom dom) {
            _elementToDomLookup.Remove(dom.ve);
        }

        Type[] GetAllVisualElementTypes() {
            List<Type> visualElementTypes = new List<Type>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies) {
                Type[] types = null;

                try {
                    types = assembly.GetTypes();
                } catch (ReflectionTypeLoadException ex) {
                    types = ex.Types.Where(t => t != null).ToArray(); // Handle partially loaded types
                }

                foreach (Type type in types) {
                    if (type.IsSubclassOf(typeof(VisualElement))) {
                        visualElementTypes.Add(type);
                    }
                }
            }

            return visualElementTypes.ToArray();
        }

        Type GetVisualElementType(string tagName) {
            Type foundType = null;
            var typeNameL = tagName.Replace("-", "").ToLower();
            foreach (var tagType in _tagTypes) {
                if (tagType.Name.ToLower() == typeNameL) {
                    foundType = tagType;
                    break;
                }
            }
            return foundType;
        }
    }
}