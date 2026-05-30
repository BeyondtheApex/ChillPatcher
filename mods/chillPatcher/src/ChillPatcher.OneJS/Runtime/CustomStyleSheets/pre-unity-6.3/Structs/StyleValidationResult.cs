using UnityEngine.UIElements;

namespace OneJS.CustomStyleSheets {
    public enum StyleValidationStatus {
        Ok,
        Error,
        Warning,
    }

    public struct StyleValidationResult {
        public StyleValidationStatus status;
        public string message;
        public string errorValue;
        public string hint;

        public bool success => this.status == StyleValidationStatus.Ok;

        public void FromOriginal(object original) {
            var type = typeof(VisualElement).Assembly.GetType(
                "UnityEngine.UIElements.StyleSheets.StyleValidationResult");
            this.status = (StyleValidationStatus)(int)type.GetField("status").GetValue(original);
            this.message = (string)type.GetField("message").GetValue(original);
            this.errorValue = (string)type.GetField("errorValue").GetValue(original);
            this.hint = (string)type.GetField("hint").GetValue(original);
        }
    }
}