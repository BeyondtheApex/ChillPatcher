using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace OneJS.CustomStyleSheets {
    public enum StyleSelectorType {
        Unknown,
        Wildcard,
        Type,
        Class,
        PseudoClass,
        RecursivePseudoClass,
        ID,
        Predicate,
    }

    public struct StyleSelectorPart {
        [SerializeField]
        private string m_Value;
        [SerializeField]
        private StyleSelectorType m_Type;
        internal object tempData;

        public string value {
            get => this.m_Value;
            internal set => this.m_Value = value;
        }

        public StyleSelectorType type {
            get => this.m_Type;
            internal set => this.m_Type = value;
        }

        public override string ToString() => String.Format("[StyleSelectorPart: value={0}, type={1}]",
            (object)this.value, (object)this.type);

        public static StyleSelectorPart CreateClass(string className) => new StyleSelectorPart() {
            m_Type = StyleSelectorType.Class,
            m_Value = className
        };

        public static StyleSelectorPart CreatePseudoClass(string className) => new StyleSelectorPart() {
            m_Type = StyleSelectorType.PseudoClass,
            m_Value = className
        };

        public static StyleSelectorPart CreateId(string Id) => new StyleSelectorPart() {
            m_Type = StyleSelectorType.ID,
            m_Value = Id
        };

        public static StyleSelectorPart CreateType(System.Type t) => new StyleSelectorPart() {
            m_Type = StyleSelectorType.Type,
            m_Value = t.Name
        };

        public static StyleSelectorPart CreateType(string typeName) => new StyleSelectorPart() {
            m_Type = StyleSelectorType.Type,
            m_Value = typeName
        };

        public static StyleSelectorPart CreatePredicate(object predicate) => new StyleSelectorPart() {
            m_Type = StyleSelectorType.Predicate,
            tempData = predicate
        };

        public static StyleSelectorPart CreateWildCard() => new StyleSelectorPart() {
            m_Type = StyleSelectorType.Wildcard
        };

        public object ToOriginal() {
            var type = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.StyleSelectorPart");
            var obj = Activator.CreateInstance(type);
            type.GetField("m_Value", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, this.m_Value);
            type.GetField("m_Type", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, (int)this.m_Type);
            type.GetField("tempData", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, this.tempData);
            return obj;
        }
    }
}