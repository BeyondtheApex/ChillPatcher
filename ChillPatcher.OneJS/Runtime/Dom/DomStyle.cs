using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using OneJS.Compat;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

namespace OneJS.Dom {
    public class DomStyle {
        static readonly Dictionary<string, PropertyInfo> _propCache = new(StringComparer.OrdinalIgnoreCase);

        Dom _dom;
        Coroutine _imageCoroutine;

        public DomStyle(Dom dom) {
            this._dom = dom;
        }

        public IStyle veStyle => _dom.ve.style;

        public object getProperty(string key) {
            if (!_propCache.TryGetValue(key, out var pi)) {
                pi = GetType().GetProperty(key, BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Public);
                if (pi != null) {
                    _propCache[key] = pi;
                } else {
                    Debug.LogWarning($"Dom Style Property \"{key}\" not found in {GetType().Name}");
                    return null;
                }
            }
            return pi.GetValue(this);
        }

        public void setProperty(string key, object value) {
            if (!_propCache.TryGetValue(key, out var pi)) {
                pi = this.GetType().GetProperty(key, BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Public);
                if (pi != null) {
                    _propCache[key] = pi;
                } else {
                    Debug.LogWarning($"Dom Style Property '{key}' not found in {this.GetType().Name}");
                    return;
                }
            }
            if (pi != null) {
                pi.SetValue(this, value);
            }
        }

        // MARK: - Style Properties

        #region Style Properties
        public object alignContent {
            get => veStyle.alignContent;
            set {
                if (TryParseStyleEnum<Align>(value, out var styleEnum))
                    veStyle.alignContent = styleEnum;
            }
        }

        public object alignItems {
            get => veStyle.alignItems;
            set {
                if (TryParseStyleEnum<Align>(value, out var styleEnum))
                    veStyle.alignItems = styleEnum;
            }
        }

        public object alignSelf {
            get => veStyle.alignSelf;
            set {
                if (TryParseStyleEnum<Align>(value, out var styleEnum))
                    veStyle.alignSelf = styleEnum;
            }
        }

        public object backgroundColor {
            get => veStyle.backgroundColor;
            set {
                if (TryParseStyleColor(value, out var styleColor))
                    veStyle.backgroundColor = styleColor;
            }
        }

        public object backgroundImage {
            get => veStyle.backgroundImage;
            set {
                if (value is string s && IsRemoteUrl(s)) {
                    StaticCoroutine.Stop(_imageCoroutine);
                    _imageCoroutine = _dom.document.loadRemoteImage(s, (texture) => {
                        veStyle.backgroundImage = new StyleBackground(Background.FromTexture2D(texture));
                        _imageCoroutine = null;
                    });
                    return;
                }
                if (TryParseStyleBackground(value, out var styleBackground))
                    veStyle.backgroundImage = styleBackground;
            }
        }

#if UNITY_2022_1_OR_NEWER
        public object backgroundSize {
            get => veStyle.backgroundSize;
            set {
                if (TryParseStyleBackgroundSize(value, out var styleBackgroundSize))
                    veStyle.backgroundSize = styleBackgroundSize;
            }
        }

        public object backgroundRepeat {
            get => veStyle.backgroundRepeat;
            set {
                if (TryParseStyleBackgroundRepeat(value, out var styleBackgroundRepeat))
                    veStyle.backgroundRepeat = styleBackgroundRepeat;
            }
        }

        // Composite
        public object backgroundPosition {
            get => (veStyle.backgroundPositionX, veStyle.backgroundPositionY);
            set => SetBackgroundPosition(value);
        }

        public object backgroundPositionX {
            get => veStyle.backgroundPositionX;
            set {
                if (TryParseStyleBackgroundPositionSingle(value, out var styleBackgroundPosition))
                    veStyle.backgroundPositionX = styleBackgroundPosition;
            }
        }

        public object backgroundPositionY {
            get => veStyle.backgroundPositionY;
            set {
                if (TryParseStyleBackgroundPositionSingle(value, out var styleBackgroundPosition))
                    veStyle.backgroundPositionY = styleBackgroundPosition;
            }
        }
#endif

        // Composite
        public object borderColor {
            get => (veStyle.borderTopColor, veStyle.borderRightColor, veStyle.borderBottomColor, veStyle.borderLeftColor);
            set => SetBorderColor(value);
        }

        // Composite
        public object borderWidth {
            get => (veStyle.borderTopWidth, veStyle.borderRightWidth, veStyle.borderBottomWidth, veStyle.borderLeftWidth);
            set => SetBorderWidth(value);
        }

        // Composite
        public object borderRadius {
            get => (veStyle.borderTopLeftRadius, veStyle.borderTopRightRadius, veStyle.borderBottomRightRadius, veStyle.borderBottomLeftRadius);
            set => SetBorderRadius(value);
        }

        public object borderTopColor {
            get => veStyle.borderTopColor;
            set {
                if (TryParseStyleColor(value, out var styleColor))
                    veStyle.borderTopColor = styleColor;
            }
        }

        public object borderRightColor {
            get => veStyle.borderRightColor;
            set {
                if (TryParseStyleColor(value, out var styleColor))
                    veStyle.borderRightColor = styleColor;
            }
        }

        public object borderBottomColor {
            get => veStyle.borderBottomColor;
            set {
                if (TryParseStyleColor(value, out var styleColor))
                    veStyle.borderBottomColor = styleColor;
            }
        }

        public object borderLeftColor {
            get => veStyle.borderLeftColor;
            set {
                if (TryParseStyleColor(value, out var styleColor))
                    veStyle.borderLeftColor = styleColor;
            }
        }

        public object borderTopWidth {
            get => veStyle.borderTopWidth;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.borderTopWidth = styleLength.value.value;
            }
        }

        public object borderRightWidth {
            get => veStyle.borderRightWidth;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.borderRightWidth = styleLength.value.value;
            }
        }

        public object borderBottomWidth {
            get => veStyle.borderBottomWidth;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.borderBottomWidth = styleLength.value.value;
            }
        }

        public object borderLeftWidth {
            get => veStyle.borderLeftWidth;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.borderLeftWidth = styleLength.value.value;
            }
        }

        public object borderTopLeftRadius {
            get => veStyle.borderTopLeftRadius;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.borderTopLeftRadius = styleLength;
            }
        }

        public object borderTopRightRadius {
            get => veStyle.borderTopRightRadius;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.borderTopRightRadius = styleLength;
            }
        }

        public object borderBottomRightRadius {
            get => veStyle.borderBottomRightRadius;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.borderBottomRightRadius = styleLength;
            }
        }

        public object borderBottomLeftRadius {
            get => veStyle.borderBottomLeftRadius;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.borderBottomLeftRadius = styleLength;
            }
        }

        public object bottom {
            get => veStyle.bottom;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.bottom = styleLength;
            }
        }

        public object color {
            get => veStyle.color;
            set {
                if (TryParseStyleColor(value, out var styleColor))
                    veStyle.color = styleColor;
            }
        }

        public object cursor {
            get => veStyle.cursor;
            set {
                if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                    veStyle.cursor = new StyleCursor(keyword);
                } else if (value is Cursor c) {
                    veStyle.cursor = new StyleCursor(c);
                }
            }
        }

        public object display {
            get => veStyle.display;
            set {
                if (TryParseStyleEnum<DisplayStyle>(value, out var styleEnum))
                    veStyle.display = styleEnum;
            }
        }

#if UNITY_6000_3_OR_NEWER
        public object filter {
            get => veStyle.filter;
            set {
                if (TryParseStyleListFilterFunction(value, out var styleList))
                    veStyle.filter = styleList;
            }
        }
#endif

        public object flexBasis {
            get => veStyle.flexBasis;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.flexBasis = styleLength;
            }
        }

        public object flexDirection {
            get => veStyle.flexDirection;
            set {
                if (TryParseStyleEnum<FlexDirection>(value, out var styleEnum))
                    veStyle.flexDirection = styleEnum;
            }
        }

        public object flexGrow {
            get => veStyle.flexGrow;
            set {
                if (TryParseStyleFloat(value, out var styleFloat))
                    veStyle.flexGrow = styleFloat;
            }
        }

        public object flexShrink {
            get => veStyle.flexShrink;
            set {
                if (TryParseStyleFloat(value, out var styleFloat))
                    veStyle.flexShrink = styleFloat;
            }
        }

        public object flexWrap {
            get => veStyle.flexWrap;
            set {
                if (TryParseStyleEnum<Wrap>(value, out var styleEnum))
                    veStyle.flexWrap = styleEnum;
            }
        }

        public object fontSize {
            get => veStyle.fontSize;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.fontSize = styleLength;
            }
        }

        public object height {
            get => veStyle.height;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.height = styleLength;
            }
        }

        public object justifyContent {
            get => veStyle.justifyContent;
            set {
                if (TryParseStyleEnum<Justify>(value, out var styleEnum))
                    veStyle.justifyContent = styleEnum;
            }
        }

        public object left {
            get => veStyle.left;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.left = styleLength;
            }
        }

        public object letterSpacing {
            get => veStyle.letterSpacing;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.letterSpacing = styleLength;
            }
        }

        // Composite
        public object margin {
            get => (veStyle.marginTop, veStyle.marginRight, veStyle.marginBottom, veStyle.marginLeft);
            set => SetMargin(value);
        }

        public object marginTop {
            get => veStyle.marginTop;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.marginTop = styleLength;
            }
        }

        public object marginRight {
            get => veStyle.marginRight;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.marginRight = styleLength;
            }
        }

        public object marginBottom {
            get => veStyle.marginBottom;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.marginBottom = styleLength;
            }
        }

        public object marginLeft {
            get => veStyle.marginLeft;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.marginLeft = styleLength;
            }
        }

        public object maxHeight {
            get => veStyle.maxHeight;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.maxHeight = styleLength;
            }
        }

        public object maxWidth {
            get => veStyle.maxWidth;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.maxWidth = styleLength;
            }
        }

        public object minHeight {
            get => veStyle.minHeight;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.minHeight = styleLength;
            }
        }

        public object minWidth {
            get => veStyle.minWidth;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.minWidth = styleLength;
            }
        }

        public object opacity {
            get => veStyle.opacity;
            set {
                if (TryParseStyleFloat(value, out var styleFloat))
                    veStyle.opacity = styleFloat;
            }
        }

        public object overflow {
            get => veStyle.overflow;
            set {
                if (TryParseStyleEnum<Overflow>(value, out var styleEnum))
                    veStyle.overflow = styleEnum;
            }
        }

        public object padding {
            get => (veStyle.paddingTop, veStyle.paddingRight, veStyle.paddingBottom, veStyle.paddingLeft);
            set => SetPadding(value);
        }

        public object paddingTop {
            get => veStyle.paddingTop;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.paddingTop = styleLength;
            }
        }

        public object paddingRight {
            get => veStyle.paddingRight;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.paddingRight = styleLength;
            }
        }

        public object paddingBottom {
            get => veStyle.paddingBottom;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.paddingBottom = styleLength;
            }
        }

        public object paddingLeft {
            get => veStyle.paddingLeft;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.paddingLeft = styleLength;
            }
        }

        public object position {
            get => veStyle.position;
            set {
                if (TryParseStyleEnum<Position>(value, out var styleEnum))
                    veStyle.position = styleEnum;
            }
        }

        public object right {
            get => veStyle.right;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.right = styleLength;
            }
        }

        public object rotate {
            get => veStyle.rotate;
            set {
                if (TryParseStyleRotate(value, out var styleRotate))
                    veStyle.rotate = styleRotate;
            }
        }

        public object scale {
            get => veStyle.scale;
            set {
                if (TryParseStyleScale(value, out var styleScale))
                    veStyle.scale = styleScale;
            }
        }

        public object textOverflow {
            get => veStyle.textOverflow;
            set {
                if (TryParseStyleEnum<TextOverflow>(value, out var styleEnum))
                    veStyle.textOverflow = styleEnum;
            }
        }

        public object textShadow {
            get => veStyle.textShadow;
            set {
                if (TryParseStyleTextShadow(value, out var styleTextShadow))
                    veStyle.textShadow = styleTextShadow;
            }
        }

        public object top {
            get => veStyle.top;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.top = styleLength;
            }
        }

        public object transformOrigin {
            get => veStyle.transformOrigin;
            set {
                if (TryParseStyleTransformOrigin(value, out var styleTransformOrigin))
                    veStyle.transformOrigin = styleTransformOrigin;
            }
        }

        public object transitionDelay {
            get => veStyle.transitionDelay;
            set {
                if (TryParseStyleListTimeValue(value, out var timeValues))
                    veStyle.transitionDelay = timeValues;
            }
        }

        public object transitionDuration {
            get => veStyle.transitionDuration;
            set {
                if (TryParseStyleListTimeValue(value, out var timeValues)) {
                    veStyle.transitionDuration = timeValues;
                }
            }
        }

        public object transitionProperty {
            get => veStyle.transitionProperty;
            set {
                // NOTE: make sure to use StylePropertyName and not PropertyName, as the latter is not
                // from UIElements
                if (TryParseStyleListPropertyName(value, out var propertyNames))
                    veStyle.transitionProperty = propertyNames;
            }
        }

        public object transitionTimingFunction {
            get => veStyle.transitionTimingFunction;
            set {
                if (TryParseStyleListEasingFunction(value, out var timingFunctions)) {
                    veStyle.transitionTimingFunction = timingFunctions;
                }
            }
        }

        public object translate {
            get => veStyle.translate;
            set {
                if (TryParseStyleTranslate(value, out var styleTranslate))
                    veStyle.translate = styleTranslate;
            }
        }

        public object unityBackgroundImageTintColor {
            get => veStyle.unityBackgroundImageTintColor;
            set {
                if (TryParseStyleColor(value, out var styleColor))
                    veStyle.unityBackgroundImageTintColor = styleColor;
            }
        }

        public object unityBackgroundScaleMode {
            get => veStyle.unityBackgroundScaleMode;
            set {
                if (TryParseStyleEnum<ScaleMode>(value, out var styleEnum))
                    veStyle.unityBackgroundScaleMode = styleEnum;
            }
        }

        public object unityFont {
            get => veStyle.unityFont;
            set {
                if (TryParseStyleFont(value, out var styleFont))
                    veStyle.unityFont = styleFont;
            }
        }

        public object unityFontDefinition {
            get => veStyle.unityFontDefinition;
            set {
                if (TryParseStyleFontDefinition(value, out var styleFontDefinition))
                    veStyle.unityFontDefinition = styleFontDefinition;
            }
        }

        public object unityFontStyleAndWeight {
            get => veStyle.unityFontStyleAndWeight;
            set {
                if (TryParseStyleEnum<FontStyle>(value, out var styleEnum))
                    veStyle.unityFontStyleAndWeight = styleEnum;
            }
        }

        public object unityOverflowClipBox {
            get => veStyle.unityOverflowClipBox;
            set {
                if (TryParseStyleEnum<OverflowClipBox>(value, out var styleEnum))
                    veStyle.unityOverflowClipBox = styleEnum;
            }
        }

        public object unityParagraphSpacing {
            get => veStyle.unityParagraphSpacing;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.unityParagraphSpacing = styleLength;
            }
        }

        public object unitySliceBottom {
            get => veStyle.unitySliceBottom;
            set {
                if (TryParseStyleInt(value, out var styleInt)) {
                    veStyle.unitySliceBottom = styleInt;
                }
            }
        }

        public object unitySliceLeft {
            get => veStyle.unitySliceLeft;
            set {
                if (TryParseStyleInt(value, out var styleInt))
                    veStyle.unitySliceLeft = styleInt;
            }
        }

        public object unitySliceRight {
            get => veStyle.unitySliceRight;
            set {
                if (TryParseStyleInt(value, out var styleInt))
                    veStyle.unitySliceRight = styleInt;
            }
        }

        public object unitySliceTop {
            get => veStyle.unitySliceTop;
            set {
                if (TryParseStyleInt(value, out var styleInt))
                    veStyle.unitySliceTop = styleInt;
            }
        }
#if UNITY_2022_1_OR_NEWER
        public object unitySliceScale {
            get => veStyle.unitySliceScale;
            set {
                if (TryParseStyleFloat(value, out var styleFloat))
                    veStyle.unitySliceScale = styleFloat;
            }
        }
#endif
        public object unityTextAlign {
            get => veStyle.unityTextAlign;
            set {
                if (TryParseStyleEnum<TextAnchor>(value, out var styleEnum))
                    veStyle.unityTextAlign = styleEnum;
            }
        }

        public object unityTextOutlineColor {
            get => veStyle.unityTextOutlineColor;
            set {
                if (TryParseStyleColor(value, out var styleColor))
                    veStyle.unityTextOutlineColor = styleColor;
            }
        }

        public object unityTextOutlineWidth {
            get => veStyle.unityTextOutlineWidth;
            set {
                if (TryParseStyleFloat(value, out var styleFloat))
                    veStyle.unityTextOutlineWidth = styleFloat;
            }
        }

        public object unityTextOverflowPosition {
            get => veStyle.unityTextOverflowPosition;
            set {
                if (TryParseStyleEnum<TextOverflowPosition>(value, out var styleEnum))
                    veStyle.unityTextOverflowPosition = styleEnum;
            }
        }

        public object visibility {
            get => veStyle.visibility;
            set {
                if (TryParseStyleEnum<Visibility>(value, out var styleEnum))
                    veStyle.visibility = styleEnum;
            }
        }

        public object whiteSpace {
            get => veStyle.whiteSpace;
            set {
                if (TryParseStyleEnum<WhiteSpace>(value, out var styleEnum))
                    veStyle.whiteSpace = styleEnum;
            }
        }

        public object width {
            get => veStyle.width;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.width = styleLength;
            }
        }

        public object wordSpacing {
            get => veStyle.wordSpacing;
            set {
                if (TryParseStyleLength(value, out var styleLength))
                    veStyle.wordSpacing = styleLength;
            }
        }
        #endregion

        #region Composites
        public object transform {
            get => $"{veStyle.translate} {veStyle.scale} {veStyle.rotate}";
            set {
                if (value is string s) {
                    var regex = new Regex(@"(\w+)\(([^)]+)\)");
                    var matches = regex.Matches(s);
                    foreach (Match match in matches) {
                        var transformType = match.Groups[1].Value.ToLower();
                        var transformValue = match.Groups[2].Value;

                        switch (transformType) {
                            // case "translatex":
                            // case "translatey":
                            case "translate":
                                if (TryParseStyleTranslate(transformValue, out var translateValue))
                                    veStyle.translate = translateValue;
                                break;
                            case "scale":
                                if (TryParseStyleScale(transformValue, out var scaleValue))
                                    veStyle.scale = scaleValue;
                                break;
                            case "rotate":
                                if (TryParseStyleRotate(transformValue, out var rotateValue))
                                    veStyle.rotate = rotateValue;
                                break;
                        }
                    }
                }
            }
        }

        public object transition {
            get => $"{veStyle.transitionProperty} {veStyle.transitionDuration} {veStyle.transitionTimingFunction}";
            set {
                if (value is string s) {
                    var transitions = s.Split(',');
                    var properties = new List<StylePropertyName>();
                    var durations = new List<TimeValue>();
                    var easingFunctions = new List<EasingFunction>();

                    foreach (var transition in transitions) {
                        var parts = transition.Trim().Split(' ');
                        if (parts.Length < 2) continue;

                        properties.Add(new StylePropertyName(parts[0]));

                        if (TryParseStyleListTimeValue(parts[1], out var duration))
                            durations.AddRange(duration.value);

                        if (parts.Length > 2 && TryParseStyleListEasingFunction(parts[2], out var easing))
                            easingFunctions.AddRange(easing.value);
                    }

                    veStyle.transitionProperty = new StyleList<StylePropertyName>(properties);
                    veStyle.transitionDuration = new StyleList<TimeValue>(durations);
                    veStyle.transitionTimingFunction = new StyleList<EasingFunction>(easingFunctions);
                }
            }
        }
        #endregion

        // MARK: - Fast Path

        #region Fast Path
        public void SetAlignContent(Align value) => veStyle.alignContent = new StyleEnum<Align>(value);
        public void SetAlignItems(Align value) => veStyle.alignItems = new StyleEnum<Align>(value);
        public void SetAlignSelf(Align value) => veStyle.alignSelf = new StyleEnum<Align>(value);
        public void SetBackgroundColor(Color value) => veStyle.backgroundColor = new StyleColor(value);
        public void SetBackgroundImage(Background value) => veStyle.backgroundImage = new StyleBackground(value);
#if UNITY_2022_1_OR_NEWER
        public void SetBackgroundSize(BackgroundSize value) => veStyle.backgroundSize = new StyleBackgroundSize(value);
        public void SetBackgroundRepeat(StyleBackgroundRepeat value) => veStyle.backgroundRepeat = value;
        public void SetBackgroundPosition(StyleBackgroundPosition value) => veStyle.backgroundPositionX = veStyle.backgroundPositionY = value;
        public void SetBackgroundPositionX(StyleBackgroundPosition value) => veStyle.backgroundPositionX = value;
        public void SetBackgroundPositionY(StyleBackgroundPosition value) => veStyle.backgroundPositionY = value;
#endif
        public void SetBorderColor(Color value) => veStyle.borderTopColor =
            veStyle.borderRightColor = veStyle.borderBottomColor = veStyle.borderLeftColor = new StyleColor(value);

        public void SetBorderTopColor(Color value) => veStyle.borderTopColor = new StyleColor(value);
        public void SetBorderRightColor(Color value) => veStyle.borderRightColor = new StyleColor(value);
        public void SetBorderBottomColor(Color value) => veStyle.borderBottomColor = new StyleColor(value);
        public void SetBorderLeftColor(Color value) => veStyle.borderLeftColor = new StyleColor(value);

        public void SetBorderWidth(float value) => veStyle.borderTopWidth =
            veStyle.borderRightWidth = veStyle.borderBottomWidth = veStyle.borderLeftWidth = new StyleFloat(value);

        public void SetBorderTopWidth(float value) => veStyle.borderTopWidth = new StyleFloat(value);
        public void SetBorderRightWidth(float value) => veStyle.borderRightWidth = new StyleFloat(value);
        public void SetBorderBottomWidth(float value) => veStyle.borderBottomWidth = new StyleFloat(value);
        public void SetBorderLeftWidth(float value) => veStyle.borderLeftWidth = new StyleFloat(value);

        public void SetBorderRadius(Length value) => veStyle.borderTopLeftRadius =
            veStyle.borderTopRightRadius = veStyle.borderBottomRightRadius = veStyle.borderBottomLeftRadius = value;

        public void SetBorderTopLeftRadius(Length value) => veStyle.borderTopLeftRadius = value;
        public void SetBorderTopRightRadius(Length value) => veStyle.borderTopRightRadius = value;
        public void SetBorderBottomRightRadius(Length value) => veStyle.borderBottomRightRadius = value;
        public void SetBorderBottomLeftRadius(Length value) => veStyle.borderBottomLeftRadius = value;
        public void SetBottom(Length value) => veStyle.bottom = value;
        public void SetColor(Color value) => veStyle.color = new StyleColor(value);
        public void SetCursor(Cursor value) => veStyle.cursor = new StyleCursor(value);
        public void SetDisplay(DisplayStyle value) => veStyle.display = new StyleEnum<DisplayStyle>(value);
#if UNITY_6000_3_OR_NEWER
        public void SetFilter(List<FilterFunction> value) => veStyle.filter = value;
        public void SetFilter(FilterFunction value) => veStyle.filter = new List<FilterFunction> { value };
#endif
        public void SetFlexBasis(StyleLength value) => veStyle.flexBasis = value;
        public void SetFlexDirection(FlexDirection value) => veStyle.flexDirection = new StyleEnum<FlexDirection>(value);
        public void SetFlexGrow(float value) => veStyle.flexGrow = new StyleFloat(value);
        public void SetFlexShrink(float value) => veStyle.flexShrink = new StyleFloat(value);
        public void SetFlexWrap(Wrap value) => veStyle.flexWrap = new StyleEnum<Wrap>(value);
        public void SetFontSize(Length value) => veStyle.fontSize = value;
        public void SetHeight(Length value) => veStyle.height = value;
        public void SetJustifyContent(Justify value) => veStyle.justifyContent = new StyleEnum<Justify>(value);
        public void SetLeft(Length value) => veStyle.left = value;
        public void SetLetterSpacing(Length value) => veStyle.letterSpacing = value;
        public void SetMargin(Length value) => veStyle.marginTop = veStyle.marginRight = veStyle.marginBottom = veStyle.marginLeft = value;
        public void SetMarginTop(Length value) => veStyle.marginTop = value;
        public void SetMarginRight(Length value) => veStyle.marginRight = value;
        public void SetMarginBottom(Length value) => veStyle.marginBottom = value;
        public void SetMarginLeft(Length value) => veStyle.marginLeft = value;
        public void SetMaxHeight(Length value) => veStyle.maxHeight = value;
        public void SetMaxWidth(Length value) => veStyle.maxWidth = value;
        public void SetMinHeight(Length value) => veStyle.minHeight = value;
        public void SetMinWidth(Length value) => veStyle.minWidth = value;
        public void SetOpacity(float value) => veStyle.opacity = new StyleFloat(value);
        public void SetOverflow(Overflow value) => veStyle.overflow = new StyleEnum<Overflow>(value);
        public void SetPadding(Length value) => veStyle.paddingTop = veStyle.paddingRight = veStyle.paddingBottom = veStyle.paddingLeft = value;
        public void SetPaddingTop(Length value) => veStyle.paddingTop = value;
        public void SetPaddingRight(Length value) => veStyle.paddingRight = value;
        public void SetPaddingBottom(Length value) => veStyle.paddingBottom = value;
        public void SetPaddingLeft(Length value) => veStyle.paddingLeft = value;
        public void SetPosition(Position value) => veStyle.position = new StyleEnum<Position>(value);
        public void SetRight(Length value) => veStyle.right = value;
        public void SetRotate(Rotate value) => veStyle.rotate = value;
        public void SetScale(Scale value) => veStyle.scale = value;
        public void SetTextOverflow(TextOverflow value) => veStyle.textOverflow = new StyleEnum<TextOverflow>(value);
        public void SetTextShadow(TextShadow value) => veStyle.textShadow = value;
        public void SetTop(Length value) => veStyle.top = value;
        public void SetTransformOrigin(TransformOrigin value) => veStyle.transformOrigin = value;
        public void SetTransitionDelay(List<TimeValue> value) => veStyle.transitionDelay = value;
        public void SetTransitionDuration(List<TimeValue> value) => veStyle.transitionDuration = value;
        public void SetTransitionProperty(List<StylePropertyName> value) => veStyle.transitionProperty = value;
        public void SetTransitionTimingFunction(List<EasingFunction> value) => veStyle.transitionTimingFunction = value;
        public void SetTranslate(Translate value) => veStyle.translate = value;
        public void SetTranslate(float x, float y) => veStyle.translate = new Translate(x, y);
        public void SetUnityBackgroundImageTintColor(Color value) => veStyle.unityBackgroundImageTintColor = new StyleColor(value);
        public void SetUnityBackgroundScaleMode(ScaleMode value) => veStyle.unityBackgroundScaleMode = new StyleEnum<ScaleMode>(value);
        public void SetUnityFont(Font value) => veStyle.unityFont = value;
        public void SetUnityFontDefinition(FontDefinition value) => veStyle.unityFontDefinition = value;
        public void SetUnityFontStyleAndWeight(FontStyle value) => veStyle.unityFontStyleAndWeight = new StyleEnum<FontStyle>(value);
        public void SetUnityOverflowClipBox(OverflowClipBox value) => veStyle.unityOverflowClipBox = new StyleEnum<OverflowClipBox>(value);
        public void SetUnityParagraphSpacing(Length value) => veStyle.unityParagraphSpacing = value;
        public void SetUnitySliceBottom(int value) => veStyle.unitySliceBottom = new StyleInt(value);
        public void SetUnitySliceLeft(int value) => veStyle.unitySliceLeft = new StyleInt(value);
        public void SetUnitySliceRight(int value) => veStyle.unitySliceRight = new StyleInt(value);
        public void SetUnitySliceTop(int value) => veStyle.unitySliceTop = new StyleInt(value);
#if UNITY_2022_1_OR_NEWER
        public void SetUnitySliceScale(float value) => veStyle.unitySliceScale = new StyleFloat(value);
#endif
        public void SetUnityTextAlign(TextAnchor value) => veStyle.unityTextAlign = new StyleEnum<TextAnchor>(value);
        public void SetUnityTextOutlineColor(Color value) => veStyle.unityTextOutlineColor = new StyleColor(value);
        public void SetUnityTextOutlineWidth(float value) => veStyle.unityTextOutlineWidth = new StyleFloat(value);

        public void SetUnityTextOverflowPosition(TextOverflowPosition value) =>
            veStyle.unityTextOverflowPosition = new StyleEnum<TextOverflowPosition>(value);

        public void SetVisibility(Visibility value) => veStyle.visibility = new StyleEnum<Visibility>(value);
        public void SetWhiteSpace(WhiteSpace value) => veStyle.whiteSpace = new StyleEnum<WhiteSpace>(value);
        public void SetWidth(Length value) => veStyle.width = value;
        public void SetWordSpacing(Length value) => veStyle.wordSpacing = value;
        #endregion

        // MARK: - Parse Styles

        #region ParseStyles
        bool TryParseStyleEnum<T>(object value, out StyleEnum<T> styleEnum) where T : struct, IConvertible {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                styleEnum = new StyleEnum<T>(keyword);
                return true;
            }
            if (value == null) {
                styleEnum = new StyleEnum<T>(StyleKeyword.Null);
                return true;
            }
            if (value is StyleEnum<T> se) {
                styleEnum = se;
                return true;
            }
            if (value is T t) {
                styleEnum = new StyleEnum<T>(t);
                return true;
            }

            if (value is double d) {
                if (Enum.IsDefined(typeof(T), (int)d)) {
                    styleEnum = new StyleEnum<T>((T)((object)(int)d));
                    return true;
                }
            } else if (value is string s) {
                if (OneJS.Compat.NetFxCompat.EnumTryParse(typeof(T), s, true, out var e)) {
                    styleEnum = new StyleEnum<T>((T)e);
                    return true;
                }
            }
            styleEnum = default;
            return false;
        }

        bool TryParseStyleColor(object value, out StyleColor styleColor) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                styleColor = new StyleColor(keyword);
                return true;
            }
            if (value == null) {
                styleColor = new StyleColor(StyleKeyword.Null);
                return true;
            }
            if (value is StyleColor sc) {
                styleColor = sc;
                return true;
            }

            if (value is string s) {
                var c = ColorUtility.TryParseHtmlString(s, out var color) ? color : Color.white;
                styleColor = new StyleColor(c);
                return true;
            } else if (value is Color c) {
                styleColor = new StyleColor(c);
                return true;
            }
            styleColor = default;
            return false;
        }

        bool TryParseStyleBackground(object value, out StyleBackground styleBackground) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                styleBackground = new StyleBackground(keyword);
                return true;
            }
            if (value == null) {
                styleBackground = new StyleBackground(StyleKeyword.Null);
                return true;
            }
            if (value is StyleBackground sb) {
                styleBackground = sb;
                return true;
            }
            if (value is Background b) {
                styleBackground = new StyleBackground(b);
                return true;
            }

            if (value is string s) {
                var texture = _dom.document.loadImage(s);
                if (texture != null) {
                    styleBackground = new StyleBackground(Background.FromTexture2D(texture));
                    return true;
                }
            } else if (value is Texture2D t) {
                styleBackground = new StyleBackground(Background.FromTexture2D(t));
                return true;
            } else if (value is Sprite sp) {
                styleBackground = new StyleBackground(Background.FromSprite(sp));
                return true;
            } else if (value is RenderTexture rt) {
                styleBackground = new StyleBackground(Background.FromRenderTexture(rt));
                return true;
            } else if (value is VectorImage vi) {
                styleBackground = new StyleBackground(Background.FromVectorImage(vi));
                return true;
            }
            styleBackground = default;
            return false;
        }

#if UNITY_2022_1_OR_NEWER
        bool TryParseStyleBackgroundSize(object value, out StyleBackgroundSize styleBackgroundSize) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                styleBackgroundSize = new StyleBackgroundSize(keyword);
                return true;
            }
            if (value == null) {
                styleBackgroundSize = new StyleBackgroundSize(StyleKeyword.Null);
                return true;
            }
            if (value is StyleBackgroundSize sbs) {
                styleBackgroundSize = sbs;
                return true;
            }
            if (value is BackgroundSize bs) {
                styleBackgroundSize = new StyleBackgroundSize(bs);
                return true;
            }

            if (value is string str) {
                var parts = str.ToLower().Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (GetLength(parts[0], out var x)) {
                    if (parts.Length > 1 && GetLength(parts[1], out var y)) {
                        styleBackgroundSize = new BackgroundSize(x, y);
                        return true;
                    }
                    styleBackgroundSize = new BackgroundSize(x, x); // If only one value is provided, use it for both x and y
                    return true;
                }
            } else if (value is Puerts.JSObject jsObj) {
                if (jsObj.Get<int>("length") == 1) {
                    var l = jsObj.Get<float>("0");
                    styleBackgroundSize = new BackgroundSize(l, l);
                    return true;
                } else if (jsObj.Get<int>("length") == 2) {
                    var x = jsObj.Get<float>("0");
                    var y = jsObj.Get<float>("1");
                    styleBackgroundSize = new BackgroundSize(x, y);
                    return true;
                }
            }
            styleBackgroundSize = default;
            return false;
        }

        bool TryParseStyleBackgroundRepeat(object value, out StyleBackgroundRepeat styleBackgroundRepeat) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                styleBackgroundRepeat = new StyleBackgroundRepeat(keyword);
                return true;
            }
            if (value == null) {
                styleBackgroundRepeat = new StyleBackgroundRepeat(StyleKeyword.Null);
                return true;
            }
            if (value is StyleBackgroundRepeat sbr) {
                styleBackgroundRepeat = sbr;
                return true;
            }
            if (value is BackgroundRepeat br) {
                styleBackgroundRepeat = new StyleBackgroundRepeat(br);
                return true;
            }

            if (value is string str) {
                var parts = str.ToLower().Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1) {
                    if (Enum.TryParse(parts[0], true, out Repeat repeat)) {
                        styleBackgroundRepeat = new BackgroundRepeat(repeat, repeat);
                        return true;
                    }
                } else if (parts.Length == 2) {
                    if (Enum.TryParse(parts[0], true, out Repeat x) && Enum.TryParse(parts[1], true, out Repeat y)) {
                        styleBackgroundRepeat = new BackgroundRepeat(x, y);
                        return true;
                    }
                }
            } else if (value is Puerts.JSObject jsObj) {
                if (jsObj.Get<int>("length") == 1) {
                    var r = jsObj.Get<Repeat>("0");
                    styleBackgroundRepeat = new BackgroundRepeat(r, r);
                    return true;
                } else if (jsObj.Get<int>("length") == 2) {
                    var x = jsObj.Get<Repeat>("0");
                    var y = jsObj.Get<Repeat>("1");
                    styleBackgroundRepeat = new BackgroundRepeat(x, y);
                    return true;
                }
            }
            styleBackgroundRepeat = default;
            return false;
        }

        void SetBackgroundPosition(object value) {
            if (value is string s && StyleKeyword.TryParse(s, true, out StyleKeyword keyword)) {
                _dom.ve.style.backgroundPositionX = new StyleBackgroundPosition(keyword);
                _dom.ve.style.backgroundPositionY = new StyleBackgroundPosition(keyword);
                return;
            }
            if (value == null) {
                _dom.ve.style.backgroundPositionX = new StyleBackgroundPosition(StyleKeyword.Null);
                _dom.ve.style.backgroundPositionY = new StyleBackgroundPosition(StyleKeyword.Null);
                return;
            }
            if (value is StyleBackgroundPosition sbp) {
                _dom.ve.style.backgroundPositionX = sbp;
                _dom.ve.style.backgroundPositionY = sbp;
                return;
            }
            if (value is BackgroundPosition bp) {
                _dom.ve.style.backgroundPositionX = new StyleBackgroundPosition(bp);
                _dom.ve.style.backgroundPositionY = new StyleBackgroundPosition(bp);
                return;
            }

            if (value is string str) {
                var parts = str.ToLower().Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1) {
                    if (Enum.TryParse(parts[0], true, out BackgroundPositionKeyword x)) {
                        _dom.ve.style.backgroundPositionX = new BackgroundPosition(x);
                        _dom.ve.style.backgroundPositionY = new BackgroundPosition(x);
                    }
                } else if (parts.Length == 2) {
                    if (Enum.TryParse(parts[0], true, out BackgroundPositionKeyword x) && GetLength(parts[1], out var l)) {
                        _dom.ve.style.backgroundPositionX = new BackgroundPosition(x, l);
                    } else if (Enum.TryParse(parts[0], true, out x) && Enum.TryParse(parts[1], true, out BackgroundPositionKeyword y)) {
                        _dom.ve.style.backgroundPositionX = new BackgroundPosition(x, 0);
                        _dom.ve.style.backgroundPositionY = new BackgroundPosition(y, 0);
                    }
                } else if (parts.Length == 4) {
                    if (Enum.TryParse(parts[0], true, out BackgroundPositionKeyword x) && GetLength(parts[1], out var lx) &&
                        Enum.TryParse(parts[2], true, out BackgroundPositionKeyword y) && GetLength(parts[3], out var ly)) {
                        _dom.ve.style.backgroundPositionX = new BackgroundPosition(x, lx);
                        _dom.ve.style.backgroundPositionY = new BackgroundPosition(y, ly);
                    }
                }
            }
        }

        bool TryParseStyleBackgroundPositionSingle(object value, out StyleBackgroundPosition styleBackgroundPosition) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                styleBackgroundPosition = new StyleBackgroundPosition(keyword);
                return true;
            }
            if (value == null) {
                styleBackgroundPosition = new StyleBackgroundPosition(StyleKeyword.Null);
                return true;
            }
            if (value is StyleBackgroundPosition sbp) {
                styleBackgroundPosition = sbp;
                return true;
            }
            if (value is BackgroundPosition bp) {
                styleBackgroundPosition = new StyleBackgroundPosition(bp);
                return true;
            }

            if (value is string s) {
                var parts = s.ToLower().Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1) {
                    if (Enum.TryParse(parts[0], true, out BackgroundPositionKeyword posKeyword)) {
                        styleBackgroundPosition = new BackgroundPosition(posKeyword);
                        return true;
                    }
                } else if (parts.Length == 2) {
                    if (Enum.TryParse(parts[0], true, out BackgroundPositionKeyword posKeyword) && GetLength(parts[1], out var l)) {
                        styleBackgroundPosition = new BackgroundPosition(posKeyword, l);
                        return true;
                    }
                }
            }
            styleBackgroundPosition = default;
            return false;
        }
#endif
        void SetBorderColor(object value) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                __setBorderColorKeyword(_dom, keyword);
                return;
            }
            if (value == null) {
                __setBorderColorKeyword(_dom, StyleKeyword.Null);
                return;
            }
            if (value is StyleColor sc) {
                __setBorderColors(_dom, sc.value, sc.value, sc.value, sc.value);
                return;
            }

            if (value is string s) {
                var parts = s.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1) {
                    if (TryParseColorString(parts[0], out var c)) {
                        __setBorderColors(_dom, c, c, c, c);
                    }
                } else if (parts.Length == 2) {
                    if (TryParseColorString(parts[0], out var tb) && TryParseColorString(parts[1], out var lr)) {
                        __setBorderColors(_dom, tb, lr, tb, lr);
                    }
                } else if (parts.Length == 3) {
                    if (TryParseColorString(parts[0], out var t) && TryParseColorString(parts[1], out var lr) &&
                        TryParseColorString(parts[2], out var b)) {
                        __setBorderColors(_dom, t, lr, b, lr);
                    }
                } else if (parts.Length == 4) {
                    if (TryParseColorString(parts[0], out var t) && TryParseColorString(parts[1], out var r) &&
                        TryParseColorString(parts[2], out var b) && TryParseColorString(parts[3], out var l)) {
                        __setBorderColors(_dom, t, r, b, l);
                    }
                }
            } else if (value is Color c) {
                __setBorderColors(_dom, c, c, c, c);
            } else if (value is Puerts.JSObject jsObj) {
                if (jsObj.Get<int>("length") == 1) {
                    var cc = jsObj.Get<Color>("0");
                    __setBorderColors(_dom, cc, cc, cc, cc);
                } else if (jsObj.Get<int>("length") == 2) {
                    var tb = jsObj.Get<Color>("0");
                    var lr = jsObj.Get<Color>("1");
                    __setBorderColors(_dom, tb, lr, tb, lr);
                } else if (jsObj.Get<int>("length") == 3) {
                    var t = jsObj.Get<Color>("0");
                    var lr = jsObj.Get<Color>("1");
                    var b = jsObj.Get<Color>("2");
                    __setBorderColors(_dom, t, lr, b, lr);
                } else if (jsObj.Get<int>("length") == 4) {
                    var t = jsObj.Get<Color>("0");
                    var r = jsObj.Get<Color>("1");
                    var b = jsObj.Get<Color>("2");
                    var l = jsObj.Get<Color>("3");
                    __setBorderColors(_dom, t, r, b, l);
                }
            }
        }

        void SetBorderWidth(object value) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                __setBorderWidthKeyword(_dom, keyword);
                return;
            }
            if (value == null) {
                __setBorderWidthKeyword(_dom, StyleKeyword.Null);
                return;
            }
            if (value is StyleFloat sf) {
                __setBorderWidths(_dom, sf.value, sf.value, sf.value, sf.value);
                return;
            }

            if (value is string s) {
                var parts = OneJS.Compat.NetFxCompat.Replace(s, "px", "", StringComparison.OrdinalIgnoreCase).Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1) {
                    if (TryParseFloat(parts[0], out var l)) {
                        __setBorderWidths(_dom, l, l, l, l);
                    }
                } else if (parts.Length == 2) {
                    if (TryParseFloat(parts[0], out var tb) && TryParseFloat(parts[1], out var lr)) {
                        __setBorderWidths(_dom, tb, lr, tb, lr);
                    }
                } else if (parts.Length == 3) {
                    if (TryParseFloat(parts[0], out var t) && TryParseFloat(parts[1], out var lr) &&
                        TryParseFloat(parts[2], out var b)) {
                        __setBorderWidths(_dom, t, lr, b, lr);
                    }
                } else if (parts.Length == 4) {
                    if (TryParseFloat(parts[0], out var t) && TryParseFloat(parts[1], out var r) &&
                        TryParseFloat(parts[2], out var b) && TryParseFloat(parts[3], out var l)) {
                        __setBorderWidths(_dom, t, r, b, l);
                    }
                }
            } else if (value is double d) {
                __setBorderWidths(_dom, (float)d, (float)d, (float)d, (float)d);
            } else if (value is Puerts.JSObject jsObj) {
                if (jsObj.Get<int>("length") == 1) {
                    var l = jsObj.Get<float>("0");
                    __setBorderWidths(_dom, l, l, l, l);
                } else if (jsObj.Get<int>("length") == 2) {
                    var tb = jsObj.Get<float>("0");
                    var lr = jsObj.Get<float>("1");
                    __setBorderWidths(_dom, tb, lr, tb, lr);
                } else if (jsObj.Get<int>("length") == 3) {
                    var t = jsObj.Get<float>("0");
                    var lr = jsObj.Get<float>("1");
                    var b = jsObj.Get<float>("2");
                    __setBorderWidths(_dom, t, lr, b, lr);
                } else if (jsObj.Get<int>("length") == 4) {
                    var t = jsObj.Get<float>("0");
                    var r = jsObj.Get<float>("1");
                    var b = jsObj.Get<float>("2");
                    var l = jsObj.Get<float>("3");
                    __setBorderWidths(_dom, t, r, b, l);
                }
            }
        }

        void SetBorderRadius(object value) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                __setBorderRadiusKeyword(_dom, keyword);
                return;
            }
            if (value == null) {
                __setBorderRadiusKeyword(_dom, StyleKeyword.Null);
                return;
            }
            if (value is StyleLength sl) {
                __setBorderRadii(_dom, sl.value, sl.value, sl.value, sl.value);
                return;
            }
            if (value is Length ll) {
                __setBorderRadii(_dom, ll, ll, ll, ll);
                return;
            }

            if (value is string s) {
                var parts = s.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1) {
                    if (GetLength(parts[0], out var l)) {
                        __setBorderRadii(_dom, l, l, l, l);
                    }
                } else if (parts.Length == 2) {
                    if (GetLength(parts[0], out var tlbr) && GetLength(parts[1], out var trbl)) {
                        __setBorderRadii(_dom, tlbr, trbl, tlbr, trbl);
                    }
                } else if (parts.Length == 3) {
                    if (GetLength(parts[0], out var tl) && GetLength(parts[1], out var trbl) &&
                        GetLength(parts[2], out var br)) {
                        __setBorderRadii(_dom, tl, trbl, br, trbl);
                    }
                } else if (parts.Length == 4) {
                    if (GetLength(parts[0], out var tl) && GetLength(parts[1], out var tr) &&
                        GetLength(parts[2], out var br) && GetLength(parts[3], out var bl)) {
                        __setBorderRadii(_dom, tl, tr, br, bl);
                    }
                }
            } else if (value is double d) {
                var l = new Length((float)d);
                __setBorderRadii(_dom, l, l, l, l);
            } else if (value is Puerts.JSObject jsObj) {
                if (jsObj.Get<int>("length") == 1) {
                    var l = new Length(jsObj.Get<float>("0"));
                    __setBorderRadii(_dom, l, l, l, l);
                } else if (jsObj.Get<int>("length") == 2) {
                    var tlbr = new Length(jsObj.Get<float>("0"));
                    var trbl = new Length(jsObj.Get<float>("1"));
                    __setBorderRadii(_dom, tlbr, trbl, trbl, tlbr);
                } else if (jsObj.Get<int>("length") == 3) {
                    var tl = new Length(jsObj.Get<float>("0"));
                    var trbl = new Length(jsObj.Get<float>("1"));
                    var br = new Length(jsObj.Get<float>("2"));
                    __setBorderRadii(_dom, tl, trbl, br, trbl);
                } else if (jsObj.Get<int>("length") == 4) {
                    var tl = new Length(jsObj.Get<float>("0"));
                    var tr = new Length(jsObj.Get<float>("1"));
                    var br = new Length(jsObj.Get<float>("2"));
                    var bl = new Length(jsObj.Get<float>("3"));
                    __setBorderRadii(_dom, tl, tr, br, bl);
                }
            }
        }

        bool TryParseStyleLength(object value, out StyleLength styleLength) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                styleLength = new StyleLength(keyword);
                return true;
            }
            if (value == null) {
                styleLength = new StyleLength(StyleKeyword.Null);
                return true;
            }
            if (value is StyleLength sl) {
                styleLength = sl;
                return true;
            }
            if (value is Length ll) {
                styleLength = new StyleLength(ll);
                return true;
            }

            if (value is string s) {
                if (GetLength(s, out var length)) {
                    styleLength = new StyleLength(length);
                    return true;
                }
            } else if (value is double d) {
                styleLength = new StyleLength((float)d);
                return true;
            }
            styleLength = default;
            return false;
        }

        bool TryParseStyleFloat(object value, out StyleFloat styleFloat) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                styleFloat = new StyleFloat(keyword);
                return true;
            }
            if (value == null) {
                styleFloat = new StyleFloat(StyleKeyword.Null);
                return true;
            }
            if (value is StyleFloat sf) {
                styleFloat = sf;
                return true;
            }

            if (value is double d) {
                styleFloat = new StyleFloat((float)d);
                return true;
            }
            styleFloat = default;
            return false;
        }

        void SetMargin(object value) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                __setMarginKeyword(_dom, keyword);
                return;
            }
            if (value == null) {
                __setMarginKeyword(_dom, StyleKeyword.Null);
                return;
            }
            if (value is StyleLength sl) {
                __setMargins(_dom, sl.value, sl.value, sl.value, sl.value);
                return;
            }
            if (value is Length ll) {
                __setMargins(_dom, ll, ll, ll, ll);
                return;
            }

            if (value is string s) {
                var parts = s.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1) {
                    if (GetLength(parts[0], out var l)) {
                        __setMargins(_dom, l, l, l, l);
                    }
                } else if (parts.Length == 2) {
                    if (GetLength(parts[0], out var tb) && GetLength(parts[1], out var lr)) {
                        __setMargins(_dom, tb, lr, tb, lr);
                    }
                } else if (parts.Length == 3) {
                    if (GetLength(parts[0], out var t) && GetLength(parts[1], out var lr) &&
                        GetLength(parts[2], out var b)) {
                        __setMargins(_dom, t, lr, b, lr);
                    }
                } else if (parts.Length == 4) {
                    if (GetLength(parts[0], out var t) && GetLength(parts[1], out var r) &&
                        GetLength(parts[2], out var b) && GetLength(parts[3], out var l)) {
                        __setMargins(_dom, t, r, b, l);
                    }
                }
            } else if (value is double d) {
                var l = new Length((float)d);
                __setMargins(_dom, l, l, l, l);
            } else if (value is Puerts.JSObject jsObj) {
                if (jsObj.Get<int>("length") == 1) {
                    var l = new Length(jsObj.Get<float>("0"));
                    __setMargins(_dom, l, l, l, l);
                } else if (jsObj.Get<int>("length") == 2) {
                    var tb = new Length(jsObj.Get<float>("0"));
                    var lr = new Length(jsObj.Get<float>("1"));
                    __setMargins(_dom, tb, lr, tb, lr);
                } else if (jsObj.Get<int>("length") == 3) {
                    var t = new Length(jsObj.Get<float>("0"));
                    var lr = new Length(jsObj.Get<float>("1"));
                    var b = new Length(jsObj.Get<float>("2"));
                    __setMargins(_dom, t, lr, b, lr);
                } else if (jsObj.Get<int>("length") == 4) {
                    var t = new Length(jsObj.Get<float>("0"));
                    var r = new Length(jsObj.Get<float>("1"));
                    var b = new Length(jsObj.Get<float>("2"));
                    var l = new Length(jsObj.Get<float>("3"));
                    __setMargins(_dom, t, r, b, l);
                }
            }
        }

        void SetPadding(object value) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                __setPaddingKeyword(_dom, keyword);
                return;
            }
            if (value == null) {
                __setPaddingKeyword(_dom, StyleKeyword.Null);
                return;
            }
            if (value is StyleLength sl) {
                __setPaddings(_dom, sl.value, sl.value, sl.value, sl.value);
                return;
            }
            if (value is Length ll) {
                __setPaddings(_dom, ll, ll, ll, ll);
                return;
            }

            if (value is string s) {
                var parts = s.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1) {
                    if (GetLength(parts[0], out var l)) {
                        __setPaddings(_dom, l, l, l, l);
                    }
                } else if (parts.Length == 2) {
                    if (GetLength(parts[0], out var tb) && GetLength(parts[1], out var lr)) {
                        __setPaddings(_dom, tb, lr, tb, lr);
                    }
                } else if (parts.Length == 3) {
                    if (GetLength(parts[0], out var t) && GetLength(parts[1], out var lr) &&
                        GetLength(parts[2], out var b)) {
                        __setPaddings(_dom, t, lr, b, lr);
                    }
                } else if (parts.Length == 4) {
                    if (GetLength(parts[0], out var t) && GetLength(parts[1], out var r) &&
                        GetLength(parts[2], out var b) && GetLength(parts[3], out var l)) {
                        __setPaddings(_dom, t, r, b, l);
                    }
                }
            } else if (value is double d) {
                var l = new Length((float)d);
                __setPaddings(_dom, l, l, l, l);
            } else if (value is Puerts.JSObject jsObj) {
                if (jsObj.Get<int>("length") == 1) {
                    var l = new Length(jsObj.Get<float>("0"));
                    __setPaddings(_dom, l, l, l, l);
                } else if (jsObj.Get<int>("length") == 2) {
                    var tb = new Length(jsObj.Get<float>("0"));
                    var lr = new Length(jsObj.Get<float>("1"));
                    __setPaddings(_dom, tb, lr, tb, lr);
                } else if (jsObj.Get<int>("length") == 3) {
                    var t = new Length(jsObj.Get<float>("0"));
                    var lr = new Length(jsObj.Get<float>("1"));
                    var b = new Length(jsObj.Get<float>("2"));
                    __setPaddings(_dom, t, lr, b, lr);
                } else if (jsObj.Get<int>("length") == 4) {
                    var t = new Length(jsObj.Get<float>("0"));
                    var r = new Length(jsObj.Get<float>("1"));
                    var b = new Length(jsObj.Get<float>("2"));
                    var l = new Length(jsObj.Get<float>("3"));
                    __setPaddings(_dom, t, r, b, l);
                }
            }
        }

        static Regex rotateRegex = new Regex(@"(-?\d+\.?\d*|\.\d+)(deg|grad|rad|turn)", RegexOptions.IgnoreCase);

        bool TryParseStyleRotate(object value, out StyleRotate styleRotate) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                styleRotate = new StyleRotate(keyword);
                return true;
            }
            if (value == null) {
                styleRotate = new StyleRotate(StyleKeyword.Null);
                return true;
            }
            if (value is StyleRotate sr) {
                styleRotate = sr;
                return true;
            }
            if (value is Rotate r) {
                styleRotate = new StyleRotate(r);
                return true;
            }
            if (value is Angle a) {
                styleRotate = new StyleRotate(new Rotate(a));
                return true;
            }

            if (value is string s) {
                var match = rotateRegex.Match(s);
                if (match.Success) {
                    float f = float.Parse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture);
                    var unit = match.Groups[2].Value.ToLower();
                    AngleUnit angleUnit = AngleUnit.Degree; // Default to Degree

                    switch (unit) {
                        case "deg":
                            angleUnit = AngleUnit.Degree;
                            break;
                        case "grad":
                            angleUnit = AngleUnit.Gradian;
                            break;
                        case "rad":
                            angleUnit = AngleUnit.Radian;
                            break;
                        case "turn":
                            angleUnit = AngleUnit.Turn;
                            break;
                    }

                    styleRotate = new Rotate(new Angle(f, angleUnit));
                    return true;
                }
            } else if (value is double d) {
                styleRotate = new StyleRotate(new Rotate((float)d));
                return true;
            } else if (value is Puerts.JSObject jsObj) {
                var f = jsObj.Get<float>("0");
                styleRotate = new StyleRotate(new Rotate(f));
                return true;
            }
            styleRotate = default;
            return false;
        }

        bool TryParseStyleScale(object value, out StyleScale styleScale) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                styleScale = new StyleScale(keyword);
                return true;
            }
            if (value == null) {
                styleScale = new StyleScale(StyleKeyword.Null);
                return true;
            }
            if (value is StyleScale ssss) {
                styleScale = ssss;
                return true;
            }
            if (value is Scale sss) {
                styleScale = new StyleScale(sss);
                return true;
            }

            if (value is string s) {
                var parts = s.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1) {
                    if (TryParseFloat(parts[0], out var l)) {
                        styleScale = new Scale(new Vector2(l, l));
                        return true;
                    }
                } else if (parts.Length == 2) {
                    if (TryParseFloat(parts[0], out var x) && TryParseFloat(parts[1], out var y)) {
                        styleScale = new Scale(new Vector2(x, y));
                        return true;
                    }
                }
            } else if (value is double d) {
                styleScale = new StyleScale(new Scale(new Vector2((float)d, (float)d)));
                return true;
            } else if (value is Puerts.JSObject jsObj && jsObj.Get<int>("length") == 2) {
                var x = jsObj.Get<float>("0");
                var y = jsObj.Get<float>("1");
                styleScale = new StyleScale(new Scale(new Vector2(x, y)));
                return true;
            }
            styleScale = default;
            return false;
        }

        bool TryParseStyleTextShadow(object value, out StyleTextShadow styleTextShadow) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                styleTextShadow = new StyleTextShadow(keyword);
                return true;
            }
            if (value == null) {
                styleTextShadow = new StyleTextShadow(StyleKeyword.Null);
                return true;
            }
            if (value is StyleTextShadow sts) {
                styleTextShadow = sts;
                return true;
            }
            if (value is TextShadow ts) {
                styleTextShadow = new StyleTextShadow(ts);
                return true;
            }

            if (value is string s) {
                var parts = s.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 3) {
                    if (GetLength(parts[0], out var x) && GetLength(parts[1], out var y) && GetLength(parts[2], out var blur)) {
                        styleTextShadow = new TextShadow() {
                            offset = new Vector2(x.value, y.value),
                            blurRadius = blur.value
                        };
                        return true;
                    }
                } else if (parts.Length == 4) {
                    if (GetLength(parts[0], out var x) && GetLength(parts[1], out var y) && GetLength(parts[2], out var blur) &&
                        TryParseColorString(parts[3], out var color)) {
                        styleTextShadow = new TextShadow() {
                            offset = new Vector2(x.value, y.value),
                            blurRadius = blur.value,
                            color = color
                        };
                        return true;
                    }
                }
            }
            styleTextShadow = default;
            return false;
        }

        bool TryParseStyleTransformOrigin(object value, out StyleTransformOrigin styleTransformOrigin) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                styleTransformOrigin = new StyleTransformOrigin(keyword);
                return true;
            }
            if (value == null) {
                styleTransformOrigin = new StyleTransformOrigin(StyleKeyword.Null);
                return true;
            }
            if (value is StyleTransformOrigin sto) {
                styleTransformOrigin = sto;
                return true;
            }
            if (value is TransformOrigin to) {
                styleTransformOrigin = new StyleTransformOrigin(to);
                return true;
            }

            if (value is string s) {
                var parts = s.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1) {
                    if (GetLength(parts[0], out var l)) {
                        styleTransformOrigin = new TransformOrigin(l, l);
                        return true;
                    }
                } else if (parts.Length == 2) {
                    if (GetLength(parts[0], out var x) && GetLength(parts[1], out var y)) {
                        styleTransformOrigin = new TransformOrigin(x, y);
                        return true;
                    }
                }
            } else if (value is double d) {
                styleTransformOrigin = new StyleTransformOrigin(new TransformOrigin((float)d, (float)d));
                return true;
            } else if (value is Puerts.JSObject jsObj && jsObj.Get<int>("length") == 2) {
                var x = jsObj.Get<float>("0");
                var y = jsObj.Get<float>("1");
                styleTransformOrigin = new StyleTransformOrigin(new TransformOrigin(x, y));
                return true;
            }
            styleTransformOrigin = default;
            return false;
        }

        static Regex timeRegex = new Regex(@"(-?\d+\.?\d*)(s|ms)", RegexOptions.IgnoreCase);

        bool TryParseStyleListTimeValue(object value, out StyleList<TimeValue> styleListTimeValue) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                styleListTimeValue = new StyleList<TimeValue>(keyword);
                return true;
            }
            if (value == null) {
                styleListTimeValue = new StyleList<TimeValue>(StyleKeyword.Null);
                return true;
            }
            if (value is StyleList<TimeValue> sltv) {
                styleListTimeValue = sltv;
                return true;
            }
            if (value is List<TimeValue> ltv) {
                styleListTimeValue = new StyleList<TimeValue>(ltv);
                return true;
            }

            if (value is string s) {
                var parts = s.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                var timeValues = new List<TimeValue>();
                foreach (var part in parts) {
                    var match = timeRegex.Match(part);
                    if (match.Success) {
                        float f = float.Parse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture);
                        var unit = match.Groups[2].Value.ToLower();
                        TimeUnit timeUnit = TimeUnit.Second; // Default to Second

                        switch (unit) {
                            case "s":
                                timeUnit = TimeUnit.Second;
                                break;
                            case "ms":
                                timeUnit = TimeUnit.Millisecond;
                                break;
                        }

                        timeValues.Add(new TimeValue(f, timeUnit));
                    }
                }
                styleListTimeValue = new StyleList<TimeValue>(timeValues);
                return true;
            } else if (value is TimeValue tv) {
                styleListTimeValue = new StyleList<TimeValue>(new List<TimeValue>() { tv });
                return true;
            } else if (value is Puerts.JSObject jsObj) {
                var timeValues = new List<TimeValue>();
                for (int i = 0; i < jsObj.Get<int>("length"); i++) {
                    var match = timeRegex.Match(jsObj.Get<string>(i.ToString()));
                    if (match.Success) {
                        float f = float.Parse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture);
                        var unit = match.Groups[2].Value.ToLower();
                        TimeUnit timeUnit = TimeUnit.Second; // Default to Second

                        switch (unit) {
                            case "s":
                                timeUnit = TimeUnit.Second;
                                break;
                            case "ms":
                                timeUnit = TimeUnit.Millisecond;
                                break;
                        }

                        timeValues.Add(new TimeValue(f, timeUnit));
                    }
                }
                styleListTimeValue = new StyleList<TimeValue>(timeValues);
                return true;
            }
            styleListTimeValue = default;
            return false;
        }

        bool TryParseStyleListPropertyName(object value, out StyleList<StylePropertyName> styleListPropertyName) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                styleListPropertyName = new StyleList<StylePropertyName>(keyword);
                return true;
            }
            if (value == null) {
                styleListPropertyName = new StyleList<StylePropertyName>(StyleKeyword.Null);
                return true;
            }
            if (value is StyleList<StylePropertyName> slpn) {
                styleListPropertyName = slpn;
                return true;
            }
            if (value is List<StylePropertyName> lpn) {
                styleListPropertyName = new StyleList<StylePropertyName>(lpn);
                return true;
            }

            if (value is string s) {
                var parts = s.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                var propertyNames = new List<StylePropertyName>();
                foreach (var part in parts) {
                    propertyNames.Add(part);
                }
                styleListPropertyName = new StyleList<StylePropertyName>(propertyNames);
                return true;
            } else if (value is StylePropertyName spn) {
                styleListPropertyName = new StyleList<StylePropertyName>(new List<StylePropertyName>() { spn });
                return true;
            } else if (value is Puerts.JSObject jsObj) {
                var propertyNames = new List<StylePropertyName>();
                for (int i = 0; i < jsObj.Get<int>("length"); i++) {
                    propertyNames.Add(jsObj.Get<string>(i.ToString()));
                }
                styleListPropertyName = new StyleList<StylePropertyName>(propertyNames);
                return true;
            }
            styleListPropertyName = default;
            return false;
        }

        bool TryParseStyleListEasingFunction(object value, out StyleList<EasingFunction> styleListEasingFunction) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                styleListEasingFunction = new StyleList<EasingFunction>(keyword);
                return true;
            }
            if (value == null) {
                styleListEasingFunction = new StyleList<EasingFunction>(StyleKeyword.Null);
                return true;
            }
            if (value is StyleList<EasingFunction> slef) {
                styleListEasingFunction = slef;
                return true;
            }
            if (value is List<EasingFunction> lef) {
                styleListEasingFunction = new StyleList<EasingFunction>(lef);
                return true;
            }

            if (value is string s) {
                var parts = s.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                var easingFunctions = new List<EasingFunction>();
                foreach (var part in parts) {
                    if (Enum.TryParse(part.Replace("_", "").Replace("-", ""), true, out EasingMode easing)) {
                        easingFunctions.Add(easing);
                    }
                }
                styleListEasingFunction = new StyleList<EasingFunction>(easingFunctions);
                return true;
            } else if (value is EasingFunction ef) {
                styleListEasingFunction = new StyleList<EasingFunction>(new List<EasingFunction>() { ef });
                return true;
            } else if (value is Puerts.JSObject jsObj) {
                var easingFunctions = new List<EasingFunction>();
                for (int i = 0; i < jsObj.Get<int>("length"); i++) {
                    if (Enum.TryParse(jsObj.Get<string>(i.ToString()), true, out EasingMode easing)) {
                        easingFunctions.Add(easing);
                    }
                }
                styleListEasingFunction = new StyleList<EasingFunction>(easingFunctions);
                return true;
            }
            styleListEasingFunction = default;
            return false;
        }

        bool TryParseStyleTranslate(object value, out StyleTranslate styleTranslate) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                styleTranslate = new StyleTranslate(keyword);
                return true;
            }
            if (value == null) {
                styleTranslate = new StyleTranslate(StyleKeyword.Null);
                return true;
            }
            if (value is Translate tt) {
                styleTranslate = tt;
                return true;
            }
            if (value is StyleTranslate st) {
                styleTranslate = st;
                return true;
            }

            if (value is string s) {
                var parts = s.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1) {
                    if (GetLength(parts[0], out var l)) {
                        styleTranslate = new Translate(l, l);
                        return true;
                    }
                } else if (parts.Length == 2) {
                    if (GetLength(parts[0], out var x) && GetLength(parts[1], out var y)) {
                        styleTranslate = new Translate(x, y);
                        return true;
                    }
                }
            } else if (value is double d) {
                styleTranslate = new StyleTranslate(new Translate((float)d, (float)d));
                return true;
            } else if (value is Vector2 v2) {
                styleTranslate = new StyleTranslate(new Translate(v2.x, v2.y));
                return true;
            } else if (value is Vector3 v3) {
                styleTranslate = new StyleTranslate(new Translate(v3.x, v3.y));
                return true;
            } else if (value is Puerts.JSObject jsObj && jsObj.Get<int>("length") == 2) {
                var x = jsObj.Get<float>("0");
                var y = jsObj.Get<float>("1");
                styleTranslate = new StyleTranslate(new Translate(x, y));
                return true;
            }
            styleTranslate = default;
            return false;
        }

        bool TryParseStyleFont(object value, out StyleFont styleFont) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                styleFont = new StyleFont(keyword);
                return true;
            }
            if (value == null) {
                styleFont = new StyleFont(StyleKeyword.Null);
                return true;
            }
            if (value is StyleFont sf) {
                styleFont = sf;
                return true;
            }
            if (value is Font f) {
                styleFont = new StyleFont(f);
                return true;
            }

            if (value is string s) {
                var font = _dom.document.loadFont(s);
                if (font != null) {
                    styleFont = new StyleFont(font);
                    return true;
                }
            }
            styleFont = default;
            return false;
        }

        bool TryParseStyleFontDefinition(object value, out StyleFontDefinition styleFontDefinition) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                styleFontDefinition = new StyleFontDefinition(keyword);
                return true;
            }
            if (value == null) {
                styleFontDefinition = new StyleFontDefinition(StyleKeyword.Null);
                return true;
            }
            if (value is StyleFontDefinition sfd) {
                styleFontDefinition = sfd;
                return true;
            }
            if (value is FontDefinition fd) {
                styleFontDefinition = new StyleFontDefinition(fd);
                return true;
            }

            if (value is string s) {
                var fontDefinition = _dom.document.loadFontDefinition(s);
                if (fontDefinition != null) {
                    styleFontDefinition = new StyleFontDefinition(fontDefinition);
                    return true;
                }
            }
            styleFontDefinition = default;
            return false;
        }

        bool TryParseStyleInt(object value, out StyleInt styleInt) {
            if (value is string ss && StyleKeyword.TryParse(ss, true, out StyleKeyword keyword)) {
                styleInt = new StyleInt(keyword);
                return true;
            }
            if (value == null) {
                styleInt = new StyleInt(StyleKeyword.Null);
                return true;
            }
            if (value is StyleInt si) {
                styleInt = si;
                return true;
            }
            if (value is double d) {
                styleInt = new StyleInt((int)d);
                return true;
            }
            if (value is string sss) {
                // use regex to parse the string for leading digits
                var match = new Regex(@"^(\d+)").Match(sss);
                if (match.Success) {
                    styleInt = new StyleInt(int.Parse(match.Groups[1].Value));
                    return true;
                }
            }
            styleInt = default;
            return false;
        }

#if UNITY_6000_3_OR_NEWER
        bool TryParseStyleListFilterFunction(object value, out StyleList<FilterFunction> styleListFilter) {
            // Keywords: none, initial, inherit, etc.
            if (value is string sk && StyleKeyword.TryParse(sk, true, out StyleKeyword kw)) {
                // styleListFilter = new StyleList<FilterFunction>(kw); // setting kw like this will throw
                styleListFilter = new List<FilterFunction>(); // so just use empty instead
                return true;
            }
            if (value == null) {
                styleListFilter = new StyleList<FilterFunction>(StyleKeyword.Null);
                return true;
            }
            if (value is StyleList<FilterFunction> sll) {
                styleListFilter = sll;
                return true;
            }
            if (value is List<FilterFunction> lf) {
                styleListFilter = new StyleList<FilterFunction>(lf);
                return true;
            }

            // Common case: CSS string like "blur(4px) brightness(0.9) drop-shadow(2px 4px 6px #0008)"
            if (value is string s) {
                var list = ParseFilterFunctionsFromString(s);
                styleListFilter = new StyleList<FilterFunction>(list);
                return true;
            }

            // JS array support (accept array of function strings or a single CSS string)
            if (value is Puerts.JSObject js) {
                var list = new List<FilterFunction>();
                int len = js.Get<int>("length");
                for (int i = 0; i < len; i++) {
                    // Each element may be "blur(4px)" or a whole "blur(...) brightness(...)" chunk
                    var part = js.Get<string>(i.ToString());
                    var sub = ParseFilterFunctionsFromString(part);
                    list.AddRange(sub);
                }
                styleListFilter = new StyleList<FilterFunction>(list);
                return true;
            }

            styleListFilter = default;
            return false;
        }

        List<FilterFunction> ParseFilterFunctionsFromString(string input) {
            var results = new List<FilterFunction>();
            if (string.IsNullOrWhiteSpace(input)) return results;

            // Split by filter functions: name(args)
            // Matches: "blur(4px)", "brightness(0.5)", "drop-shadow(2px 4px 6px rgba(0,0,0,.35))", etc.
            var rx = new Regex(@"([a-zA-Z-]+)\(([^)]*)\)");
            var matches = rx.Matches(input);

            foreach (Match m in matches) {
                var name = m.Groups[1].Value.Trim().ToLowerInvariant();
                var args = m.Groups[2].Value.Trim();

                switch (name) {
                    case "blur": {
                        // blur(<length>)
                        if (TryParseLengthFloat(args, out float radius)) {
                            var ff = new FilterFunction(FilterFunctionType.Blur);
                            ff.AddParameter(new FilterParameter(radius));
                            results.Add(ff);
                        }
                        break;
                    }
                    case "contrast": {
                        if (TryParsePercentOrFloat(args, out float v)) {
                            var ff = new FilterFunction(FilterFunctionType.Contrast);
                            ff.AddParameter(new FilterParameter(v));
                            results.Add(ff);
                        }
                        break;
                    }
                    case "grayscale": {
                        if (TryParsePercentOrFloat(args, out float v)) {
                            var ff = new FilterFunction(FilterFunctionType.Grayscale);
                            ff.AddParameter(new FilterParameter(v));
                            results.Add(ff);
                        }
                        break;
                    }
                    case "sepia": {
                        if (TryParsePercentOrFloat(args, out float v)) {
                            var ff = new FilterFunction(FilterFunctionType.Sepia);
                            ff.AddParameter(new FilterParameter(v));
                            results.Add(ff);
                        }
                        break;
                    }
                    case "invert": {
                        if (TryParsePercentOrFloat(args, out float v)) {
                            var ff = new FilterFunction(FilterFunctionType.Invert);
                            ff.AddParameter(new FilterParameter(v));
                            results.Add(ff);
                        }
                        break;
                    }
                    case "opacity": {
                        if (TryParsePercentOrFloat(args, out float v)) {
                            var ff = new FilterFunction(FilterFunctionType.Opacity);
                            ff.AddParameter(new FilterParameter(v));
                            results.Add(ff);
                        }
                        break;
                    }
                    case "hue-rotate": {
                        // hue-rotate(<angle>) supports deg|rad|turn|grad
                        if (TryParseAngleDegrees(args, out float deg)) {
                            var ff = new FilterFunction(FilterFunctionType.HueRotate);
                            ff.AddParameter(new FilterParameter(deg));
                            results.Add(ff);
                        }
                        break;
                    }
                    // case "drop-shadow": {
                    //     // drop-shadow(<offset-x> <offset-y> [<blur-radius>] [<color>])
                    //     // color can be before/after the lengths, so we need tolerant tokenization
                    //     var tokens = TokenizeArgs(args);
                    //     float ox = 0, oy = 0, blur = 0;
                    //     bool haveOx = false, haveOy = false, haveBlur = false, haveColor = false;
                    //     Color color = Color.black;
                    //
                    //     foreach (var t in tokens) {
                    //         if (!haveColor && TryParseColorString(t, out var c)) {
                    //             color = c;
                    //             haveColor = true;
                    //             continue;
                    //         }
                    //         if (!haveOx && TryParseLengthFloat(t, out var v0)) {
                    //             ox = v0;
                    //             haveOx = true;
                    //             continue;
                    //         }
                    //         if (!haveOy && TryParseLengthFloat(t, out var v1)) {
                    //             oy = v1;
                    //             haveOy = true;
                    //             continue;
                    //         }
                    //         if (!haveBlur && TryParseLengthFloat(t, out var v2)) {
                    //             blur = v2;
                    //             haveBlur = true;
                    //             continue;
                    //         }
                    //     }
                    //
                    //     if (haveOx && haveOy) {
                    //         var ff = new FilterFunction(FilterFunctionType.DropShadow);
                    //         ff.AddParameter(new FilterParameter(ox));
                    //         ff.AddParameter(new FilterParameter(oy));
                    //         ff.AddParameter(new FilterParameter(blur)); // blur may be 0
                    //         if (haveColor) ff.AddParameter(new FilterParameter(color));
                    //         results.Add(ff);
                    //     }
                    //     break;
                    // }
                    default:
                        // Unknown filter function — ignore gracefully (or you could log once if desired)
                        break;
                }
            }

            return results;
        }

        static List<string> TokenizeArgs(string s) {
            // Splits by spaces/commas while respecting nested (...) like rgba(…)
            var tokens = new List<string>();
            var sb = new StringBuilder();
            int depth = 0;
            for (int i = 0; i < s.Length; i++) {
                char ch = s[i];
                if (ch == '(') depth++;
                if (ch == ')') depth = Math.Max(0, depth - 1);

                if ((char.IsWhiteSpace(ch) || ch == ',') && depth == 0) {
                    if (sb.Length > 0) {
                        tokens.Add(sb.ToString());
                        sb.Clear();
                    }
                } else {
                    sb.Append(ch);
                }
            }
            if (sb.Length > 0) tokens.Add(sb.ToString());
            return tokens;
        }
#endif

        bool TryParseLengthFloat(string token, out float value) {
            // Accept "4px", "12", "0.5" etc. We just need a float (px assumed).
            if (GetLength(token, out var l)) {
                value = l.value;
                return true;
            }
            return TryParseFloat(token, out value);
        }

        bool TryParsePercentOrFloat(string token, out float value) {
            token = token.Trim();
            if (token.EndsWith("%", StringComparison.Ordinal)) {
                var num = token.Substring(0, token.Length - 1).Trim();
                if (TryParseFloat(num, out var pct)) {
                    value = pct / 100f;
                    return true;
                }
                value = 0;
                return false;
            }
            return TryParseFloat(token, out value);
        }

        bool TryParseAngleDegrees(string token, out float degrees) {
            // Uses your rotate regex semantics: (-?\d+\.?\d*|\.\d+)(deg|grad|rad|turn)
            var m = rotateRegex.Match(token);
            if (!m.Success) {
                degrees = 0;
                return false;
            }

            float f = float.Parse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture);
            switch (m.Groups[2].Value.ToLowerInvariant()) {
                case "deg":
                    degrees = f;
                    break;
                case "rad":
                    degrees = f * (180f / Mathf.PI);
                    break;
                case "turn":
                    degrees = f * 360f;
                    break;
                case "grad":
                    degrees = f * 0.9f;
                    break; // 400 grad == 360 deg
                default:
                    degrees = f;
                    break;
            }
            return true;
        }
        #endregion

        #region Static Utils
        public static bool GetLength(object value, out Length lengthValue) {
            if (value is string s) {
                s = s.Trim();

                if (s.Length >= 2 && s.EndsWith("px", StringComparison.OrdinalIgnoreCase)) {
                    var num = s.Substring(0, s.Length - 2).TrimEnd();
                    if (NetFxCompat.TryParseFloat(num, NumberStyles.Float, out var px)) {
                        lengthValue = new Length(px);
                        return true;
                    }
                } else if (s.Length >= 1 && s.EndsWith("%", StringComparison.Ordinal)) {
                    var num = s.Substring(0, s.Length - 1).TrimEnd();
                    if (NetFxCompat.TryParseFloat(num, NumberStyles.Float, out var pct)) {
                        lengthValue = new Length(pct, LengthUnit.Percent);
                        return true;
                    }
                } else {
                    if (NetFxCompat.TryParseFloat(s, NumberStyles.Float, out var val)) {
                        lengthValue = new Length(val);
                        return true;
                    }
                }
            } else if (value is IConvertible c) {
                try {
                    lengthValue = new Length(Convert.ToSingle(c, CultureInfo.CurrentCulture));
                    return true;
                } catch { }
                try {
                    lengthValue = new Length(Convert.ToSingle(c, CultureInfo.InvariantCulture));
                    return true;
                } catch { }
            }

            lengthValue = default;
            return false;
        }

        public static bool TryParseColorString(string s, out Color color) {
            return ColorUtility.TryParseHtmlString(s, out color);
        }

        public static bool TryParseFloat(string token, out float value) {
            token = token.Trim();

            // Primary: CSS-style invariant (dot as decimal point)
            if (float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return true;

            // Fallback: user's current culture (e.g., comma decimals)
            if (float.TryParse(token, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                return true;

            // Extra tolerant path: if it looks like a comma-decimal, normalize to dot and retry invariant
            var nfi = CultureInfo.CurrentCulture.NumberFormat;
            if (token.IndexOf(',') >= 0 && token.IndexOf('.') < 0 && nfi.NumberDecimalSeparator == ",") {
                var normalized = token.Replace(',', '.');
                if (float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    return true;
            }

            value = 0f;
            return false;
        }

        static void __setBorderColors(Dom dom, Color t, Color r, Color b, Color l) {
            dom.ve.style.borderTopColor = new StyleColor(t);
            dom.ve.style.borderRightColor = new StyleColor(r);
            dom.ve.style.borderBottomColor = new StyleColor(b);
            dom.ve.style.borderLeftColor = new StyleColor(l);
        }

        static void __setBorderColorKeyword(Dom dom, StyleKeyword keyword) {
            dom.ve.style.borderTopColor = new StyleColor(keyword);
            dom.ve.style.borderRightColor = new StyleColor(keyword);
            dom.ve.style.borderBottomColor = new StyleColor(keyword);
            dom.ve.style.borderLeftColor = new StyleColor(keyword);
        }

        static void __setBorderWidths(Dom dom, float t, float r, float b, float l) {
            dom.ve.style.borderTopWidth = new StyleFloat(t);
            dom.ve.style.borderRightWidth = new StyleFloat(r);
            dom.ve.style.borderBottomWidth = new StyleFloat(b);
            dom.ve.style.borderLeftWidth = new StyleFloat(l);
        }

        static void __setBorderWidthKeyword(Dom dom, StyleKeyword keyword) {
            dom.ve.style.borderTopWidth = new StyleFloat(keyword);
            dom.ve.style.borderRightWidth = new StyleFloat(keyword);
            dom.ve.style.borderBottomWidth = new StyleFloat(keyword);
            dom.ve.style.borderLeftWidth = new StyleFloat(keyword);
        }

        static void __setBorderRadii(Dom dom, Length tl, Length tr, Length br, Length bl) {
            dom.ve.style.borderTopLeftRadius = new StyleLength(tl);
            dom.ve.style.borderTopRightRadius = new StyleLength(tr);
            dom.ve.style.borderBottomRightRadius = new StyleLength(br);
            dom.ve.style.borderBottomLeftRadius = new StyleLength(bl);
        }

        static void __setBorderRadiusKeyword(Dom dom, StyleKeyword keyword) {
            dom.ve.style.borderTopLeftRadius = new StyleLength(keyword);
            dom.ve.style.borderTopRightRadius = new StyleLength(keyword);
            dom.ve.style.borderBottomRightRadius = new StyleLength(keyword);
            dom.ve.style.borderBottomLeftRadius = new StyleLength(keyword);
        }

        static void __setMargins(Dom dom, Length t, Length r, Length b, Length l) {
            dom.ve.style.marginTop = new StyleLength(t);
            dom.ve.style.marginRight = new StyleLength(r);
            dom.ve.style.marginBottom = new StyleLength(b);
            dom.ve.style.marginLeft = new StyleLength(l);
        }

        static void __setMarginKeyword(Dom dom, StyleKeyword keyword) {
            dom.ve.style.marginTop = new StyleLength(keyword);
            dom.ve.style.marginRight = new StyleLength(keyword);
            dom.ve.style.marginBottom = new StyleLength(keyword);
            dom.ve.style.marginLeft = new StyleLength(keyword);
        }

        static void __setPaddings(Dom dom, Length t, Length r, Length b, Length l) {
            dom.ve.style.paddingTop = new StyleLength(t);
            dom.ve.style.paddingRight = new StyleLength(r);
            dom.ve.style.paddingBottom = new StyleLength(b);
            dom.ve.style.paddingLeft = new StyleLength(l);
        }

        static void __setPaddingKeyword(Dom dom, StyleKeyword keyword) {
            dom.ve.style.paddingTop = new StyleLength(keyword);
            dom.ve.style.paddingRight = new StyleLength(keyword);
            dom.ve.style.paddingBottom = new StyleLength(keyword);
            dom.ve.style.paddingLeft = new StyleLength(keyword);
        }

        static bool IsRemoteUrl(string path) {
            if (Uri.TryCreate(path, UriKind.Absolute, out Uri uriResult)) {
                return uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps || uriResult.Scheme == Uri.UriSchemeFtp;
            }
            return false;
        }
        #endregion

        /*
        // TS Typings (for reference and AI generation)
        alignContent: StyleEnum<Align> | Align | string | null | number;
        alignItems: StyleEnum<Align> | Align | string | null | number;
        alignSelf: StyleEnum<Align> | Align | string | null | number;
        backgroundColor: StyleColor | Color | string | null;
        backgroundImage: StyleBackground | Background | string | null;
        backgroundSize: StyleBackgroundSize | BackgroundSize | string | null;
        backgroundRepeat: StyleBackgroundRepeat | BackgroundRepeat | string | null;
        backgroundPosition: StyleBackgroundPosition | BackgroundPosition | string | null;
        backgroundPositionX: StyleBackgroundPosition | BackgroundPosition | string | null;
        backgroundPositionY: StyleBackgroundPosition | BackgroundPosition | string | null;
        borderColor: StyleColor | Color | string | null | Color[];
        borderWidth: StyleFloat | number | string | null | number[];
        borderRadius: StyleLength | Length | string | null | number[];
        borderBottomColor: StyleColor | Color | string | null;
        borderTopColor: StyleColor | Color | string | null;
        borderLeftColor: StyleColor | Color | string | null;
        borderRightColor: StyleColor | Color | string | null;
        borderBottomWidth: StyleFloat | number | string | null;
        borderTopWidth: StyleFloat | number | string | null;
        borderLeftWidth: StyleFloat | number | string | null;
        borderRightWidth: StyleFloat | number | string | null;
        borderTopLeftRadius: StyleLength | Length | string | null;
        borderTopRightRadius: StyleLength | Length | string | null;
        borderBottomRightRadius: StyleLength | Length | string | null;
        borderBottomLeftRadius: StyleLength | Length | string | null;
        bottom: StyleLength | Length | string | null;
        color: StyleColor | Color | string | null;
        cursor: StyleCursor | Cursor | null;
        display: StyleEnum<DisplayStyle> | DisplayStyle | string | null | number;
        flexBasis: StyleLength | Length | string | null;
        flexDirection: StyleEnum<FlexDirection> | FlexDirection | string | null | number;
        flexGrow: StyleFloat | number | string | null;
        flexShrink: StyleFloat | number | string | null;
        flexWrap: StyleEnum<Wrap> | Wrap | string | null | number;
        fontSize: StyleLength | Length | string | null;
        height: StyleLength | Length | string | null;
        justifyContent: StyleEnum<Justify> | Justify | string | null | number;
        left: StyleLength | Length | string | null;
        letterSpacing: StyleLength | Length | string | null;
        margin: StyleLength | Length | string | null | number[];
        marginBottom: StyleLength | Length | string | null;
        marginLeft: StyleLength | Length | string | null;
        marginRight: StyleLength | Length | string | null;
        marginTop: StyleLength | Length | string | null;
        maxHeight: StyleLength | Length | string | null;
        maxWidth: StyleLength | Length | string | null;
        minHeight: StyleLength | Length | string | null;
        minWidth: StyleLength | Length | string | null;
        opacity: StyleFloat | number | string | null;
        overflow: StyleEnum<Overflow> | Overflow | string | null | number;
        padding: StyleLength | Length | string | null | number[];
        paddingBottom: StyleLength | Length | string | null;
        paddingLeft: StyleLength | Length | string | null;
        paddingRight: StyleLength | Length | string | null;
        paddingTop: StyleLength | Length | string | null;
        position: StyleEnum<Position> | Position | string | null | number;
        right: StyleLength | Length | string | null;
        rotate: StyleRotate | Rotate | string | null | number;
        scale: StyleScale | Scale | string | null | number;
        textOverflow: StyleEnum<TextOverflow> | TextOverflow | string | null | number;
        textShadow: StyleTextShadow | TextShadow | string | null;
        top: StyleLength | Length | string | null;
        transformOrigin: StyleTransformOrigin | TransformOrigin | string | null | number[];
        transitionDelay: StyleList<TimeValue> | TimeValue | string | null | string[];
        transitionDuration: StyleList<TimeValue> | TimeValue | string | null | string[];
        transitionProperty: StyleList<StylePropertyName> | StylePropertyName | string | null | string[];
        transitionTimingFunction: StyleList<EasingFunction> | EasingFunction | string | null | string[];
        translate: StyleTranslate | Translate | string | null | number[];
        unityBackgroundImageTintColor: StyleColor | Color | string | null;
        unityBackgroundScaleMode: StyleEnum<ScaleMode> | ScaleMode | string | null | number;
        unityFont: StyleFont | Font | string | null;
        unityFontDefinition: StyleFontDefinition | FontDefinition | string | null;
        unityFontStyleAndWeight: StyleEnum<FontStyle> | FontStyle | string | null | number;
        unityOverflowClipBox: StyleEnum<OverflowClipBox> | OverflowClipBox | string | null | number;
        unityParagraphSpacing: StyleLength | Length | string | null;
        unitySliceBottom: StyleInt | number | string | null;
        unitySliceLeft: StyleInt | number | string | null;
        unitySliceRight: StyleInt | number | string | null;
        unitySliceTop: StyleInt | number | string | null;
        unitySliceScale: StyleFloat | number | string | null;
        unityTextAlign: StyleEnum<TextAnchor> | TextAnchor | string | null | number;
        unityTextOutlineColor: StyleColor | Color | string | null;
        unityTextOutlineWidth: StyleFloat | number | string | null;
        unityTextOverflowPosition: StyleEnum<TextOverflowPosition> | TextOverflowPosition | string | null | number;
        visibility: StyleEnum<Visibility> | Visibility | string | null | number;
        whiteSpace: StyleEnum<WhiteSpace> | WhiteSpace | string | null | number;
        width: StyleLength | Length | string | null;
        wordSpacing: StyleLength | Length | string | null;
        */
    }
}