using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExCSS;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OneJS.CustomStyleSheets {
    public class CustomStyleSheetImporterImpl : StyleValueImporter {
        public CustomStyleSheetImporterImpl(IScriptEngine scriptEngine) : base(scriptEngine) {
        }

        public void BuildStyleSheet(UnityEngine.UIElements.StyleSheet asset, string contents) {
            ExCSS.StyleSheet styleSheet = this.m_Parser.Parse(contents);
            this.ImportParserStyleSheet(asset, styleSheet);
            Hash128 hash = new Hash128();
            byte[] bytes = Encoding.UTF8.GetBytes(contents);
            if (bytes.Length != 0)
                HashUtilities.ComputeHash128(bytes, ref hash);
            asset.contentHash = hash.GetHashCode();
        }

        protected void ImportParserStyleSheet(UnityEngine.UIElements.StyleSheet asset, ExCSS.StyleSheet styleSheet) {
            this.m_Errors.assetPath = this.assetPath;
            if (styleSheet.Errors.Count > 0) {
                foreach (StylesheetParseError error in styleSheet.Errors)
                    this.m_Errors.AddSyntaxError(
                        string.Format(StyleValueImporter.glossary.ussParsingError, (object)error.Message), error.Line);
            } else {
                // try {
                this.VisitSheet(styleSheet);
                // } catch (Exception ex) {
                // this.m_Errors.AddInternalError(
                //     string.Format(StyleValueImporter.glossary.internalErrorWithStackTrace, (object)ex.Message,
                //         (object)ex.StackTrace), this.m_CurrentLine);
                // }
            }
            bool hasErrors = this.m_Errors.hasErrors;
            if (!hasErrors) {
                this.m_Builder.BuildTo(asset);
            }
        }

        void VisitSheet(ExCSS.StyleSheet styleSheet) {
            foreach (ExCSS.StyleRule styleRule in (IEnumerable<ExCSS.StyleRule>)styleSheet.StyleRules) {
                this.m_Builder.BeginRule(styleRule.Line);
                this.m_CurrentLine = styleRule.Line;
                this.VisitBaseSelector(styleRule.Selector);
                foreach (Property declaration in styleRule.Declarations) {
                    this.m_CurrentLine = declaration.Line;
                    this.ValidateProperty(declaration);
                    this.m_Builder.BeginProperty(declaration.Name, declaration.Line);
                    this.VisitValue(declaration.Term);
                    this.m_Builder.EndProperty();
                }
                this.m_Builder.EndRule();
            }
        }

        void VisitBaseSelector(BaseSelector selector) {
            switch (selector) {
                case AggregateSelectorList selectorList:
                    this.VisitSelectorList(selectorList);
                    break;
                case ComplexSelector complexSelector:
                    this.VisitComplexSelector(complexSelector);
                    break;
                case SimpleSelector simpleSelector:
                    this.VisitSimpleSelector(((object)simpleSelector).ToString());
                    break;
            }
        }

        void VisitSelectorList(AggregateSelectorList selectorList) {
            if (selectorList.Delimiter == ",") {
                foreach (BaseSelector selector in (SelectorList)selectorList)
                    this.VisitBaseSelector(selector);
            } else if (selectorList.Delimiter == string.Empty)
                this.VisitSimpleSelector(((object)selectorList).ToString());
            else
                this.m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidSelectorListDelimiter,
                    string.Format(StyleValueImporter.glossary.invalidSelectorListDelimiter,
                        (object)selectorList.Delimiter), this.m_CurrentLine);
        }

        void VisitSimpleSelector(string selector) {
            StyleSelectorPart[] parts;
            if (!this.CheckSimpleSelector(selector, out parts))
                return;
            int selectorSpecificity = CSSSpec.GetSelectorSpecificity(parts);
            if (selectorSpecificity == 0) {
                this.m_Errors.AddInternalError(
                    string.Format(StyleValueImporter.glossary.internalError,
                        (object)("Failed to calculate selector specificity " + selector)), this.m_CurrentLine);
            } else {
                using (this.m_Builder.BeginComplexSelector(selectorSpecificity))
                    this.m_Builder.AddSimpleSelector(parts, StyleSelectorRelationship.None);
            }
        }

        void VisitComplexSelector(ComplexSelector complexSelector) {
            int selectorSpecificity = CSSSpec.GetSelectorSpecificity(((object)complexSelector).ToString());
            if (selectorSpecificity == 0) {
                this.m_Errors.AddInternalError(
                    string.Format(StyleValueImporter.glossary.internalError,
                        (object)("Failed to calculate selector specificity " + ((object)complexSelector)?.ToString())),
                    this.m_CurrentLine);
            } else {
                using (this.m_Builder.BeginComplexSelector(selectorSpecificity)) {
                    StyleSelectorRelationship previousRelationsip = StyleSelectorRelationship.None;
                    foreach (CombinatorSelector combinatorSelector in complexSelector) {
                        string simpleSelector = this.ExtractSimpleSelector(combinatorSelector.Selector);
                        if (string.IsNullOrEmpty(simpleSelector)) {
                            this.m_Errors.AddInternalError(
                                string.Format(StyleValueImporter.glossary.internalError,
                                    (object)("Expected simple selector inside complex selector " + simpleSelector)),
                                this.m_CurrentLine);
                            break;
                        }
                        StyleSelectorPart[] parts;
                        if (!this.CheckSimpleSelector(simpleSelector, out parts))
                            break;
                        this.m_Builder.AddSimpleSelector(parts, previousRelationsip);
                        switch (combinatorSelector.Delimiter) {
                            case Combinator.Child:
                                previousRelationsip = StyleSelectorRelationship.Child;
                                break;
                            case Combinator.Descendent:
                                previousRelationsip = StyleSelectorRelationship.Descendent;
                                break;
                            default:
                                this.m_Errors.AddSemanticError(
                                    StyleSheetImportErrorCode.InvalidComplexSelectorDelimiter,
                                    string.Format(StyleValueImporter.glossary.invalidComplexSelectorDelimiter,
                                        (object)complexSelector), this.m_CurrentLine);
                                return;
                        }
                    }
                }
            }
        }

        bool CheckSimpleSelector(string selector, out StyleSelectorPart[] parts) {
            if (!CSSSpec.ParseSelector(selector, out parts)) {
                this.m_Errors.AddSemanticError(StyleSheetImportErrorCode.UnsupportedSelectorFormat,
                    string.Format(StyleValueImporter.glossary.unsupportedSelectorFormat, (object)selector),
                    this.m_CurrentLine);
                return false;
            }
            if (((IEnumerable<StyleSelectorPart>)parts).Any<StyleSelectorPart>(
                    (Func<StyleSelectorPart, bool>)(p => p.type == StyleSelectorType.Unknown))) {
                this.m_Errors.AddSemanticError(StyleSheetImportErrorCode.UnsupportedSelectorFormat,
                    string.Format(StyleValueImporter.glossary.unsupportedSelectorFormat, (object)selector),
                    this.m_CurrentLine);
                return false;
            }
            if (!((IEnumerable<StyleSelectorPart>)parts).Any<StyleSelectorPart>(
                    (Func<StyleSelectorPart, bool>)(p => p.type == StyleSelectorType.RecursivePseudoClass)))
                return true;
            this.m_Errors.AddSemanticError(StyleSheetImportErrorCode.RecursiveSelectorDetected,
                string.Format(StyleValueImporter.glossary.unsupportedSelectorFormat, (object)selector),
                this.m_CurrentLine);
            return false;
        }

        void ValidateProperty(Property property) {
            if (this.disableValidation)
                return;
            string name = property.Name;
            string str = ((object)property.Term).ToString();
            StyleValidationResult validationResult = this.m_Validator.ValidateProperty(name, str);
            if (!validationResult.success) {
                string message = validationResult.message + "\n    " + name + ": " + str;
                if (!string.IsNullOrEmpty(validationResult.hint))
                    message = message + " -> " + validationResult.hint;
                this.m_Errors.AddValidationWarning(message, property.Line);
            }
        }

        string ExtractSimpleSelector(BaseSelector selector) {
            int num;
            switch (selector) {
                case SimpleSelector _:
                    return ((object)selector).ToString();
                case AggregateSelectorList aggregateSelectorList:
                    num = aggregateSelectorList.Delimiter == string.Empty ? 1 : 0;
                    break;
                default:
                    num = 0;
                    break;
            }
            return num != 0 ? ((object)selector).ToString() : string.Empty;
        }
    }
}