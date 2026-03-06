using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace OneJS.CustomStyleSheets {
    public class StyleSheetBuilderWrapper {
        Type _type;
        object _instance;

        public StyleSheetBuilderWrapper() {
            _type = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.StyleSheets.StyleSheetBuilder");
            _instance = Activator.CreateInstance(_type);
        }

        public void BuildTo(StyleSheet styleSheet) {
            _type.InvokeMember("BuildTo", System.Reflection.BindingFlags.InvokeMethod, null, _instance,
                new object[] { styleSheet });
        }

        public void AddCommaSeparator() {
            _type.InvokeMember("AddCommaSeparator", System.Reflection.BindingFlags.InvokeMethod, null, _instance,
                new object[] { });
        }

        public IDisposable BeginComplexSelector(int specificity) {
            return (IDisposable)_type.InvokeMember("BeginComplexSelector", System.Reflection.BindingFlags.InvokeMethod,
                null,
                _instance,
                new object[] { specificity });
        }

        public void AddSimpleSelector(StyleSelectorPart[] parts, StyleSelectorRelationship previousRelationsip) {
            var t1 = _type.Assembly.GetType("UnityEngine.UIElements.StyleSelectorPart");
            var t1Array = t1.MakeArrayType();
            var t2 = _type.Assembly.GetType("UnityEngine.UIElements.StyleSelectorRelationship");
            var methodInfo = _type.GetMethod("AddSimpleSelector", new[] { t1Array, t2 });

            var newParts = parts.Select(p => p.ToOriginal()).ToArray();
            var length = parts.Length;
            Array destinationArray = Array.CreateInstance(t1, length);
            Array.Copy(newParts, destinationArray, length);

            methodInfo.Invoke(_instance, new object[] { destinationArray, (int)previousRelationsip });
        }

        public void BeginRule(int line) {
            _type.InvokeMember("BeginRule", System.Reflection.BindingFlags.InvokeMethod, null, _instance,
                new object[] { line });
        }

        public void EndRule() {
            _type.InvokeMember("EndRule", System.Reflection.BindingFlags.InvokeMethod, null, _instance,
                new object[] { });
        }

        public void BeginProperty(string name, int line) {
            _type.InvokeMember("BeginProperty", System.Reflection.BindingFlags.InvokeMethod, null, _instance,
                new object[] { name, line });
        }

        public void EndProperty() {
            _type.InvokeMember("EndProperty", System.Reflection.BindingFlags.InvokeMethod, null, _instance,
                new object[] { });
        }
        
        public void AddValue(StyleValueFunction func) {
            var methodInfo = _type.GetMethod("AddValue",
                new[] { _type.Assembly.GetType("UnityEngine.UIElements.StyleValueFunction") });
            methodInfo.Invoke(_instance, new object[] { (int)func });
        }

        public void AddValue(float val) {
            var methodInfo = _type.GetMethod("AddValue",
                new[] { typeof(float) });
            methodInfo.Invoke(_instance, new object[] { val });
        }

        public void AddValue(Color color) {
            var methodInfo = _type.GetMethod("AddValue",
                new[] { typeof(Color) });
            methodInfo.Invoke(_instance, new object[] { color });
        }
        
        public void AddValue(UnityEngine.Object value) {
            var methodInfo = _type.GetMethod("AddValue",
                new[] { typeof(UnityEngine.Object) });
            methodInfo.Invoke(_instance, new object[] { value });
        }

        public void AddValue(Dimension dimension) {
            var methodInfo = _type.GetMethod("AddValue",
                new[] { _type.Assembly.GetType("UnityEngine.UIElements.StyleSheets.Dimension") });
            methodInfo.Invoke(_instance, new object[] { dimension.ToOriginal() });
        }

        public void AddValue(StyleValueKeyword keyword) {
            var methodInfo = _type.GetMethod("AddValue",
                new[] { _type.Assembly.GetType("UnityEngine.UIElements.StyleValueKeyword") });
            methodInfo.Invoke(_instance, new object[] { (int)keyword });
        }

        public void AddValue(string value, StyleValueType type) {
            var methodInfo = _type.GetMethod("AddValue",
                new[] { typeof(string), _type.Assembly.GetType("UnityEngine.UIElements.StyleValueType") });
            methodInfo.Invoke(_instance, new object[] { value, (int)type });
        }
    }
}