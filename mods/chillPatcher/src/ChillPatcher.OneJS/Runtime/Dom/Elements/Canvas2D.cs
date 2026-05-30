using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace OneJS.Dom
{
    /// <summary>
    /// Generic 2D drawing element backed by Unity's Painter2D API.
    /// From JS: access element.ve to call drawing methods, then Commit().
    /// </summary>
    public class Canvas2D : VisualElement
    {
        readonly List<Action<Painter2D>> _commands = new List<Action<Painter2D>>();

        public Canvas2D()
        {
            generateVisualContent = OnGenerateVisualContent;
        }

        // ---- Path commands ----

        public void BeginPath() => _commands.Add(p => p.BeginPath());
        public void ClosePath() => _commands.Add(p => p.ClosePath());

        public void MoveTo(float x, float y) =>
            _commands.Add(p => p.MoveTo(new Vector2(x, y)));

        public void LineTo(float x, float y) =>
            _commands.Add(p => p.LineTo(new Vector2(x, y)));

        public void Arc(float cx, float cy, float radius, float startAngleDeg, float endAngleDeg) =>
            _commands.Add(p => p.Arc(
                new Vector2(cx, cy), radius,
                new Angle(startAngleDeg, AngleUnit.Degree),
                new Angle(endAngleDeg, AngleUnit.Degree)));

        public void ArcTo(float x1, float y1, float x2, float y2, float radius) =>
            _commands.Add(p => p.ArcTo(
                new Vector2(x1, y1), new Vector2(x2, y2), radius));

        public void BezierCurveTo(float cp1x, float cp1y, float cp2x, float cp2y, float x, float y) =>
            _commands.Add(p => p.BezierCurveTo(
                new Vector2(cp1x, cp1y), new Vector2(cp2x, cp2y), new Vector2(x, y)));

        public void QuadraticCurveTo(float cpx, float cpy, float x, float y) =>
            _commands.Add(p => p.QuadraticCurveTo(
                new Vector2(cpx, cpy), new Vector2(x, y)));

        // ---- Style commands ----

        public void SetFillColor(string color)
        {
            if (TryParseColor(color, out var c))
                _commands.Add(p => p.fillColor = c);
        }

        public void SetStrokeColor(string color)
        {
            if (TryParseColor(color, out var c))
                _commands.Add(p => p.strokeColor = c);
        }

        public void SetLineWidth(float width) =>
            _commands.Add(p => p.lineWidth = width);

        public void SetLineCap(int cap) =>
            _commands.Add(p => p.lineCap = (LineCap)cap);

        public void SetLineJoin(int join) =>
            _commands.Add(p => p.lineJoin = (LineJoin)join);

        // ---- Fill / Stroke ----

        public void Fill() => _commands.Add(p => p.Fill());
        public void Stroke() => _commands.Add(p => p.Stroke());

        public void FillWithRule(int rule) =>
            _commands.Add(p => p.Fill((FillRule)rule));

        // ---- Control ----

        public void ClearCommands()
        {
            _commands.Clear();
            MarkDirtyRepaint();
        }

        public void Commit() => MarkDirtyRepaint();

        // ---- Render ----

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (_commands.Count == 0) return;
            var painter = mgc.painter2D;
            foreach (var cmd in _commands)
                cmd(painter);
        }

        // ---- Color parsing ----

        static readonly Regex RgbaRegex = new Regex(
            @"rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*(?:,\s*([\d.]+))?\s*\)",
            RegexOptions.Compiled);

        static bool TryParseColor(string value, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrEmpty(value)) return false;

            // rgba(r, g, b, a)
            var m = RgbaRegex.Match(value);
            if (m.Success)
            {
                float r = int.Parse(m.Groups[1].Value) / 255f;
                float g = int.Parse(m.Groups[2].Value) / 255f;
                float b = int.Parse(m.Groups[3].Value) / 255f;
                float a = m.Groups[4].Success
                    ? float.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture)
                    : 1f;
                color = new Color(r, g, b, a);
                return true;
            }

            // #hex
            var hex = value.StartsWith("#") ? value : "#" + value;
            return ColorUtility.TryParseHtmlString(hex, out color);
        }
    }
}
