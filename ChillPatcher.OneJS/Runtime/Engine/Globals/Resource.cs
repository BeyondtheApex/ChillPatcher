using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace OneJS {
    public delegate void MyCallback();

    public class Resource {
        IScriptEngine _engine;
        Dictionary<string, Texture2D> _imageCache = new();
        Dictionary<string, Font> _fontCache = new();
        Dictionary<string, FontDefinition> _fontDefinitionCache = new();

        public Resource(IScriptEngine engine) {
            _engine = engine;
        }

        public Font loadFont(string path) {
            var fullPath = Path.IsPathRooted(path)
                ? path
                : Path.GetFullPath(Path.Combine(_engine.WorkingDir, path));
            if (_fontCache.TryGetValue(fullPath, out var cached))
                return cached;
            var font = new Font(fullPath);
            _fontCache[fullPath] = font;
            return font;
        }

        public FontDefinition loadFontDefinition(string path) {
            var fullPath = Path.IsPathRooted(path)
                ? path
                : Path.GetFullPath(Path.Combine(_engine.WorkingDir, path));
            if (_fontDefinitionCache.TryGetValue(fullPath, out var cached))
                return cached;
            var font = loadFont(path);
            var fd = FontDefinition.FromFont(font);
            _fontDefinitionCache[fullPath] = fd;
            return fd;
        }

        public Texture2D loadImage(string path) {
            var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(_engine.WorkingDir, path);
            if (_imageCache.TryGetValue(fullPath, out var cached))
                return cached;
            var rawData = System.IO.File.ReadAllBytes(fullPath);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(rawData);
            tex.filterMode = FilterMode.Bilinear;
            _imageCache[fullPath] = tex;
            return tex;
        }
    }
}