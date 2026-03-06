using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OneJS.CustomStyleSheets {
    public enum StyleSheetImportErrorType {
        Syntax,
        Semantic,
        Validation,
        Internal,
    }

    public enum StyleSheetImportErrorCode {
        None,
        Internal,
        UnsupportedUnit,
        UnsupportedTerm,
        InvalidSelectorListDelimiter,
        InvalidComplexSelectorDelimiter,
        UnsupportedSelectorFormat,
        RecursiveSelectorDetected,
        MissingFunctionArgument,
        InvalidProperty,
        InvalidURILocation,
        InvalidURIScheme,
        InvalidURIProjectAssetPath,
        InvalidVarFunction,
        InvalidHighResolutionImage,
    }

    public class StyleSheetImportErrors : IEnumerable<StyleSheetImportError>, IEnumerable {
        private List<StyleSheetImportError> m_Errors = new List<StyleSheetImportError>();

        public string assetPath { get; set; }

        public void AddSyntaxError(string message, int line) => this.m_Errors.Add(
            new StyleSheetImportError(StyleSheetImportErrorType.Syntax, StyleSheetImportErrorCode.None, this.assetPath,
                message, line));

        public void AddSemanticError(StyleSheetImportErrorCode code, string message, int line) => this.m_Errors.Add(
            new StyleSheetImportError(StyleSheetImportErrorType.Semantic, code, this.assetPath, message, line));

        public void AddSemanticWarning(StyleSheetImportErrorCode code, string message, int line) => this.m_Errors.Add(
            new StyleSheetImportError(StyleSheetImportErrorType.Semantic, code, this.assetPath, message, line, true));

        public void AddInternalError(string message, int line = -1) => this.m_Errors.Add(
            new StyleSheetImportError(StyleSheetImportErrorType.Internal, StyleSheetImportErrorCode.None,
                this.assetPath, message, line));

        public void AddValidationWarning(string message, int line) => this.m_Errors.Add(
            new StyleSheetImportError(StyleSheetImportErrorType.Validation, StyleSheetImportErrorCode.InvalidProperty,
                this.assetPath, message, line, true));

        public IEnumerator<StyleSheetImportError> GetEnumerator() =>
            (IEnumerator<StyleSheetImportError>)this.m_Errors.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => (IEnumerator)this.m_Errors.GetEnumerator();

        public bool hasErrors =>
            this.m_Errors.Any<StyleSheetImportError>((Func<StyleSheetImportError, bool>)(e => !e.isWarning));

        public bool hasWarning =>
            this.m_Errors.Any<StyleSheetImportError>((Func<StyleSheetImportError, bool>)(e => e.isWarning));
    }
}