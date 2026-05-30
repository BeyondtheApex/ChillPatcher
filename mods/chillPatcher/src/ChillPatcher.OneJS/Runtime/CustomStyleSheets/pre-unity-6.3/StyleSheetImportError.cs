namespace OneJS.CustomStyleSheets {
    public struct StyleSheetImportError {
        public readonly StyleSheetImportErrorType error;
        public readonly StyleSheetImportErrorCode code;
        public readonly string assetPath;
        public readonly string message;
        public readonly int line;
        public readonly bool isWarning;

        public StyleSheetImportError(
            StyleSheetImportErrorType error,
            StyleSheetImportErrorCode code,
            string assetPath,
            string message,
            int line = -1,
            bool isWarning = false) {
            this.error = error;
            this.code = code;
            this.assetPath = assetPath;
            this.message = message;
            this.line = line;
            this.isWarning = isWarning;
        }

        public override string ToString() => this.ToString(StyleValueImporter.glossary);

        public string ToString(StyleSheetImportGlossary glossary) => this.assetPath +
                                                                     (this.line > -1
                                                                         ? string.Format(" ({0} {1})",
                                                                             (object)glossary.line, (object)this.line)
                                                                         : "") + ": " +
                                                                     (this.isWarning
                                                                         ? glossary.warning
                                                                         : glossary.error) + ": " + this.message;
    }
}