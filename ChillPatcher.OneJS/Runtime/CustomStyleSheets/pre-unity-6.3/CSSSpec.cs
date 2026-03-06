using System;
using System.Text.RegularExpressions;

namespace OneJS.CustomStyleSheets {
    public static class CSSSpec {
        private static readonly Regex rgx =
            new Regex(
                "(?<id>#[-]?\\w[\\w-]*)|(?<class>\\.[\\w-]+)|(?<pseudoclass>:[\\w-]+(\\((?<param>.+)\\))?)|(?<type>[^\\-]\\w+)|(?<wildcard>\\*)|\\s+",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private const int typeSelectorWeight = 1;
        private const int classSelectorWeight = 10;
        private const int idSelectorWeight = 100;

        public static int GetSelectorSpecificity(string selector) {
            int selectorSpecificity = 0;
            StyleSelectorPart[] parts;
            if (CSSSpec.ParseSelector(selector, out parts))
                selectorSpecificity = CSSSpec.GetSelectorSpecificity(parts);
            return selectorSpecificity;
        }

        public static int GetSelectorSpecificity(StyleSelectorPart[] parts) {
            int selectorSpecificity = 1;
            for (int index = 0; index < parts.Length; ++index) {
                switch (parts[index].type) {
                    case StyleSelectorType.Type:
                        ++selectorSpecificity;
                        break;
                    case StyleSelectorType.Class:
                    case StyleSelectorType.PseudoClass:
                        selectorSpecificity += 10;
                        break;
                    case StyleSelectorType.RecursivePseudoClass:
                        throw new ArgumentException("Recursive pseudo classes are not supported");
                    case StyleSelectorType.ID:
                        selectorSpecificity += 100;
                        break;
                }
            }
            return selectorSpecificity;
        }

        public static bool ParseSelector(string selector, out StyleSelectorPart[] parts) {
            MatchCollection matchCollection = CSSSpec.rgx.Matches(selector);
            int count = matchCollection.Count;
            if (count < 1) {
                parts = (StyleSelectorPart[])null;
                return false;
            }
            parts = new StyleSelectorPart[count];
            for (int i = 0; i < count; ++i) {
                Match match = matchCollection[i];
                StyleSelectorType styleSelectorType = StyleSelectorType.Unknown;
                string str1 = string.Empty;
                if (!string.IsNullOrEmpty(match.Groups["wildcard"].Value)) {
                    str1 = "*";
                    styleSelectorType = StyleSelectorType.Wildcard;
                } else if (!string.IsNullOrEmpty(match.Groups["id"].Value)) {
                    str1 = match.Groups["id"].Value.Substring(1);
                    styleSelectorType = StyleSelectorType.ID;
                } else if (!string.IsNullOrEmpty(match.Groups["class"].Value)) {
                    str1 = match.Groups["class"].Value.Substring(1);
                    styleSelectorType = StyleSelectorType.Class;
                } else if (!string.IsNullOrEmpty(match.Groups["pseudoclass"].Value)) {
                    string str2 = match.Groups["param"].Value;
                    if (!string.IsNullOrEmpty(str2)) {
                        str1 = str2;
                        styleSelectorType = StyleSelectorType.RecursivePseudoClass;
                    } else {
                        str1 = match.Groups["pseudoclass"].Value.Substring(1);
                        styleSelectorType = StyleSelectorType.PseudoClass;
                    }
                } else if (!string.IsNullOrEmpty(match.Groups["type"].Value)) {
                    str1 = match.Groups["type"].Value;
                    styleSelectorType = StyleSelectorType.Type;
                }
                parts[i] = new StyleSelectorPart() {
                    type = styleSelectorType,
                    value = str1
                };
            }
            return true;
        }
    }
}