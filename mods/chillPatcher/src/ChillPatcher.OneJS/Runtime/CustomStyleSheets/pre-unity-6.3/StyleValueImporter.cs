using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExCSS;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OneJS.CustomStyleSheets {
    public enum URIValidationResult {
        OK,
        InvalidURILocation,
        InvalidURIScheme,
        InvalidURIProjectAssetPath,
    }

    public abstract class StyleValueImporter {
        private struct StoredAsset {
            public UnityEngine.Object resource;

            public ScalableImage si;

            public int index;
        }

        private static StyleSheetImportGlossary s_Glossary;

        private const string k_ResourcePathFunctionName = "resource";

        private const string k_VariableFunctionName = "var";

        protected readonly Parser m_Parser;

        protected readonly StyleSheetBuilderWrapper m_Builder;

        protected readonly StyleSheetImportErrors m_Errors;

        protected readonly StyleValidatorWrapper m_Validator;

        protected string m_AssetPath;

        protected int m_CurrentLine;
#pragma warning disable 414
        private static readonly string kThemePrefix = "unity-theme://";
#pragma warning restore 414
        private static Dictionary<string, StyleValueKeyword> s_NameCache;

        internal static StyleSheetImportGlossary glossary =>
            s_Glossary ?? (s_Glossary = new StyleSheetImportGlossary());

        public bool disableValidation { get; set; }

        public StyleSheetImportErrors importErrors => m_Errors;

        public string assetPath => m_AssetPath;

        static (StyleSheetImportErrorCode, string) ConvertErrorCode(
            URIValidationResult result) {
            switch (result) {
                case URIValidationResult.InvalidURILocation:
                    return (StyleSheetImportErrorCode.InvalidURILocation,
                        StyleValueImporter.glossary.invalidUriLocation);
                case URIValidationResult.InvalidURIScheme:
                    return (StyleSheetImportErrorCode.InvalidURIScheme, StyleValueImporter.glossary.invalidUriScheme);
                case URIValidationResult.InvalidURIProjectAssetPath:
                    return (StyleSheetImportErrorCode.InvalidURIProjectAssetPath,
                        StyleValueImporter.glossary.invalidAssetPath);
                default:
                    return (StyleSheetImportErrorCode.Internal,
                        StyleValueImporter.glossary.internalErrorWithStackTrace);
            }
        }

        IScriptEngine _scriptEngine;

        public StyleValueImporter(IScriptEngine scriptEngine) {
            _scriptEngine = scriptEngine;
            this.m_AssetPath = (string)null;
            this.m_Parser = new ExCSS.Parser();
            this.m_Builder = new StyleSheetBuilderWrapper();
            this.m_Errors = new StyleSheetImportErrors();
            this.m_Validator = new StyleValidatorWrapper();
        }

        protected void VisitResourceFunction(GenericFunction funcTerm) {
            Term obj = ((IEnumerable<Term>)funcTerm.Arguments).FirstOrDefault();
            PrimitiveTerm val = (PrimitiveTerm)(object)((obj is PrimitiveTerm) ? obj : null);
            if (val == null) {
                m_Errors.AddSemanticError(StyleSheetImportErrorCode.MissingFunctionArgument, funcTerm.Name,
                    m_CurrentLine);
                return;
            }
            string value = val.Value as string;
            m_Builder.AddValue(value, StyleValueType.ResourcePath);
        }

        protected void VisitUrlFunction(PrimitiveTerm term) {
            string path = (string)term.Value;
            var workingDir = _scriptEngine.WorkingDir;
            var fullpath = Path.Combine(workingDir, path);
            if (File.Exists(fullpath)) {
                // test path ends in .jpg or .png
                if (path.EndsWith(".jpg") || path.EndsWith(".png")) {
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(File.ReadAllBytes(fullpath));
                    tex.filterMode = FilterMode.Bilinear;
                    Debug.Log(tex);

                    m_Builder.AddValue(tex);

                    // TODO ScalableImage with @2x
                } else if (path.EndsWith(".ttf")) {
                    Font font = new Font(fullpath);
                    m_Builder.AddValue(font);
                } else {
                    m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidURILocation,
                        string.Format(StyleValueImporter.glossary.invalidUriLocation, path), m_CurrentLine);
                }
            }
        }

        protected void VisitValue(Term term) {
            PrimitiveTerm term1 = term as PrimitiveTerm;
            HtmlColor htmlColor = term as HtmlColor;
            GenericFunction genericFunction = term as GenericFunction;
            TermList termList = term as TermList;
            Comma comma = term as Comma;
            Whitespace whitespace = term as Whitespace;
            if (term == Term.Inherit)
                this.m_Builder.AddValue(StyleValueKeyword.Inherit);
            else if (term1 != null) {
                string rawStr = ((object)term).ToString();
                switch (term1.PrimitiveType) {
                    case UnitType.Number:
                        this.m_Builder.AddValue(term1.GetFloatValue(UnitType.Pixel).Value);
                        break;
                    case UnitType.Percentage:
                        this.m_Builder.AddValue(new Dimension(term1.GetFloatValue(UnitType.Pixel).Value,
                            Dimension.Unit.Percent));
                        break;
                    case UnitType.Pixel:
                        this.m_Builder.AddValue(new Dimension(term1.GetFloatValue(UnitType.Pixel).Value,
                            Dimension.Unit.Pixel));
                        break;
                    case UnitType.Degree:
                        this.m_Builder.AddValue(new Dimension(term1.GetFloatValue(UnitType.Pixel).Value,
                            Dimension.Unit.Degree));
                        break;
                    case UnitType.Radian:
                        this.m_Builder.AddValue(new Dimension(term1.GetFloatValue(UnitType.Pixel).Value,
                            Dimension.Unit.Radian));
                        break;
                    case UnitType.Grad:
                        this.m_Builder.AddValue(new Dimension(term1.GetFloatValue(UnitType.Pixel).Value,
                            Dimension.Unit.Gradian));
                        break;
                    case UnitType.Millisecond:
                        this.m_Builder.AddValue(new Dimension(term1.GetFloatValue(UnitType.Millisecond).Value,
                            Dimension.Unit.Millisecond));
                        break;
                    case UnitType.Second:
                        this.m_Builder.AddValue(new Dimension(term1.GetFloatValue(UnitType.Second).Value,
                            Dimension.Unit.Second));
                        break;
                    case UnitType.String:
                        this.m_Builder.AddValue(rawStr.Trim('\'', '"'), StyleValueType.String);
                        break;
                    case UnitType.Uri:
                        this.VisitUrlFunction(term1);
                        break;
                    case UnitType.Ident:
                        StyleValueKeyword keyword;
                        if (StyleValueImporter.TryParseKeyword(rawStr, out keyword)) {
                            this.m_Builder.AddValue(keyword);
                            break;
                        }
                        if (rawStr.StartsWith("--")) {
                            this.m_Builder.AddValue(rawStr, StyleValueType.Variable);
                            break;
                        }
                        this.m_Builder.AddValue(rawStr, StyleValueType.Enum);
                        break;
                    case UnitType.Turn:
                        this.m_Builder.AddValue(new Dimension(term1.GetFloatValue(UnitType.Pixel).Value,
                            Dimension.Unit.Turn));
                        break;
                    default:
                        this.m_Errors.AddSemanticError(StyleSheetImportErrorCode.UnsupportedUnit,
                            string.Format(StyleValueImporter.glossary.unsupportedUnit,
                                (object)((object)term1).ToString()), this.m_CurrentLine);
                        break;
                }
            } else if (htmlColor != (HtmlColor)null)
                this.m_Builder.AddValue(new Color((float)htmlColor.R / (float)byte.MaxValue,
                    (float)htmlColor.G / (float)byte.MaxValue, (float)htmlColor.B / (float)byte.MaxValue,
                    (float)htmlColor.A / (float)byte.MaxValue));
            else if (genericFunction != null) {
                if (genericFunction.Name == "resource") {
                    this.VisitResourceFunction(genericFunction);
                    // throw new Exception("resource() Not Implemented");
                } else {
                    StyleValueFunction func;
                    if (!this.ValidateFunction(genericFunction, out func))
                        return;
                    this.m_Builder.AddValue(func);
                    this.m_Builder.AddValue(
                        (float)((IEnumerable<Term>)genericFunction.Arguments).Count<Term>(
                            (Func<Term, bool>)(a => !(a is Whitespace))));
                    foreach (Term term2 in genericFunction.Arguments)
                        this.VisitValue(term2);
                }
            } else if (termList != null) {
                int num = 0;
                foreach (Term term3 in termList) {
                    this.VisitValue(term3);
                    ++num;
                    if (num < termList.Length) {
                        switch (termList.GetSeparatorAt(num - 1)) {
                            case TermList.TermSeparator.Comma:
                                this.m_Builder.AddCommaSeparator();
                                goto case TermList.TermSeparator.Space;
                            case TermList.TermSeparator.Space:
                            case TermList.TermSeparator.Colon:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("termSeparator");
                        }
                    }
                }
            } else if (comma != null) {
                this.m_Builder.AddCommaSeparator();
            } else {
                if (whitespace != null)
                    return;
                this.m_Errors.AddSemanticError(StyleSheetImportErrorCode.UnsupportedTerm,
                    string.Format(StyleValueImporter.glossary.unsupportedTerm, (object)((object)term).GetType().Name),
                    this.m_CurrentLine);
            }
        }

        private bool ValidateFunction(GenericFunction term, out StyleValueFunction func) {
            func = StyleValueFunction.Unknown;
            if (term.Arguments.Length == 0) {
                this.m_Errors.AddSemanticError(StyleSheetImportErrorCode.MissingFunctionArgument,
                    string.Format(StyleValueImporter.glossary.missingFunctionArgument, (object)term.Name),
                    this.m_CurrentLine);
                return false;
            }
            if (term.Name == "var") {
                func = StyleValueFunction.Var;
                return this.ValidateVarFunction(term);
            }
            // try {
            func = FromUssString(term.Name);
            // } catch (Exception ex) {
            //     StyleProperty currentProperty = this.m_Builder.currentProperty;
            //     this.m_Errors.AddValidationWarning(
            //         string.Format(StyleValueImporter.glossary.unknownFunction, (object)term.Name,
            //             (object)currentProperty.name), currentProperty.line);
            //     return false;
            // }
            return true;
        }

        public static StyleValueFunction FromUssString(string ussValue) {
            ussValue = ussValue.ToLower();
            string str = ussValue;
            if (str == "var")
                return StyleValueFunction.Var;
            if (str == "env")
                return StyleValueFunction.Env;
            if (str == "linear-gradient")
                return StyleValueFunction.LinearGradient;
            throw new ArgumentOutOfRangeException(nameof(ussValue), (object)ussValue, "Unknown function name");
        }

        private bool ValidateVarFunction(GenericFunction term) {
            int length = term.Arguments.Length;
            Term term1 = term.Arguments[0];
            bool flag1 = false;
            bool flag2 = false;
            for (int index = 0; index < length; ++index) {
                Term term2 = term.Arguments[index];
                if (!(((object)term2).GetType() == typeof(Whitespace))) {
                    if (!flag1) {
                        string str = (term.Arguments[index] is PrimitiveTerm primitiveTerm
                            ? primitiveTerm.Value
                            : (object)null) as string;
                        if (string.IsNullOrEmpty(str)) {
                            this.m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction,
                                StyleValueImporter.glossary.missingVariableName, this.m_CurrentLine);
                            return false;
                        }
                        if (!str.StartsWith("--")) {
                            this.m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction,
                                string.Format(StyleValueImporter.glossary.missingVariablePrefix, (object)str),
                                this.m_CurrentLine);
                            return false;
                        }
                        if (str.Length < 3) {
                            this.m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction,
                                StyleValueImporter.glossary.emptyVariableName, this.m_CurrentLine);
                            return false;
                        }
                        flag1 = true;
                    } else if (((object)term2).GetType() == typeof(Comma)) {
                        if (flag2) {
                            this.m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction,
                                StyleValueImporter.glossary.tooManyFunctionArguments, this.m_CurrentLine);
                            return false;
                        }
                        flag2 = true;
                        ++index;
                        if (index >= length) {
                            this.m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction,
                                StyleValueImporter.glossary.emptyFunctionArgument, this.m_CurrentLine);
                            return false;
                        }
                    } else if (!flag2) {
                        string str = "";
                        while (((object)term2).GetType() == typeof(Whitespace) && index + 1 < length)
                            term2 = term.Arguments[++index];
                        if (((object)term2).GetType() != typeof(Whitespace))
                            str = ((object)term2).ToString();
                        this.m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction,
                            string.Format(StyleValueImporter.glossary.unexpectedTokenInFunction, (object)str),
                            this.m_CurrentLine);
                        return false;
                    }
                }
            }
            return true;
        }

        static bool TryParseKeyword(string rawStr, out StyleValueKeyword value) {
            if (StyleValueImporter.s_NameCache == null) {
                StyleValueImporter.s_NameCache = new Dictionary<string, StyleValueKeyword>();
                foreach (StyleValueKeyword styleValueKeyword in Enum.GetValues(typeof(StyleValueKeyword)))
                    StyleValueImporter.s_NameCache[styleValueKeyword.ToString().ToLower()] = styleValueKeyword;
            }
            return StyleValueImporter.s_NameCache.TryGetValue(rawStr.ToLower(), out value);
        }
    }
}