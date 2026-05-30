using System;
using UnityEngine.UIElements;

namespace OneJS.CustomStyleSheets {
    public class StyleValidatorWrapper {
        Type _type;
        object _instance;

        public StyleValidatorWrapper() {
            this._type = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.StyleSheets.StyleValidator");
            _instance = Activator.CreateInstance(_type);
        }

        public StyleValidationResult ValidateProperty(string name, string str) {
            var res = new StyleValidationResult();
            var ori = this._type.GetMethod("ValidateProperty")
                .Invoke(_instance, new object[] { name, str });
            res.FromOriginal(ori);
            return res;
        }
    }
}