namespace OneJS.CustomStyleSheets {
    public class StyleSheetImportGlossary {
        public readonly string internalError = "Internal import error: {0}";

        public readonly string internalErrorWithStackTrace = "Internal import error: {0}\n{1}";

        public readonly string error = "error";

        public readonly string warning = "warning";

        public readonly string line = "line";

        public readonly string unsupportedUnit = "Unsupported unit: '{0}'";

        public readonly string ussParsingError = "USS parsing error: {0}";

        public readonly string unsupportedTerm = "Unsupported USS term: {0}";

        public readonly string missingFunctionArgument = "Missing function argument: '{0}'";

        public readonly string missingVariableName = "Missing variable name";

        public readonly string emptyVariableName = "Empty variable name";

        public readonly string tooManyFunctionArguments = "Too many function arguments";

        public readonly string emptyFunctionArgument = "Empty function argument";

        public readonly string unexpectedTokenInFunction = "Expected ',', got '{0}'";

        public readonly string missingVariablePrefix = "Variable '{0}' is missing '--' prefix";

        public readonly string invalidHighResAssetType =
            "Unsupported type {0} for asset at path '{1}' ; only Texture2D is supported for variants with @2x suffix\nSuggestion: verify the import settings of this asset.";

        public readonly string invalidSelectorListDelimiter = "Invalid selector list delimiter: '{0}'";

        public readonly string invalidComplexSelectorDelimiter = "Invalid complex selector delimiter: '{0}'";

        public readonly string unsupportedSelectorFormat = "Unsupported selector format: '{0}'";

        public readonly string unknownFunction = "Unknown function '{0}' in declaration '{1}: {0}'";

        public readonly string circularImport =
            "Circular @import dependencies detected. All @import directives will be ignored for this StyleSheet.";

        public readonly string invalidUriLocation = "Invalid URI location: '{0}'";

        public readonly string invalidUriScheme = "Invalid URI scheme: '{0}'";

        public readonly string invalidAssetPath = "Invalid asset path: '{0}'";

        public readonly string invalidAssetType =
            "Unsupported type {0} for asset at path '{1}' ; only the following types are supported: {2}\nSuggestion: verify the import settings of this asset.";
    }
}