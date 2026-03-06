using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace OneJS.Dom {
    public interface IDocument {
        Dom createElement(string tagName);
        Dom createElement(string tagName, ElementCreationOptions options);
        Dom createElementNS(string ns, string tagName, ElementCreationOptions options);
        Dom createTextNode(string text);
        void clearCache();
        Coroutine loadRemoteImage(string path, Action<Texture2D> callback);
        Texture2D loadImage(string path, FilterMode filterMode = FilterMode.Bilinear);
        Font loadFont(string path);
        FontDefinition loadFontDefinition(string path);
        void AddCachingDom(Dom dom);
        void RemoveCachingDom(Dom dom);
    }
}