using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using BepInEx.Logging;

namespace ChillPatcher
{
    /// <summary>
    /// Win32 native overlay window for startup logs.
    /// Runs on a dedicated STA thread with its own message pump.
    /// Completely independent of Unity's rendering pipeline.
    /// </summary>
    public static class BuildSetupOverlay
    {
        private enum LogLvl { Info, Warning, Error }

        private struct LogEntry
        {
            public string Message;
            public LogLvl Level;
        }

        private static readonly object _lock = new object();
        private static readonly List<LogEntry> _pending = new List<LogEntry>();
        private static OverlayForm _form;
        private static Thread _thread;
        private static LogListener _listener;
        private static volatile bool _visible;

        public static void Show()
        {
            if (_visible) return;
            _visible = true;

            _listener = new LogListener();
            BepInEx.Logging.Logger.Listeners.Add(_listener);
            Enqueue("ChillPatcher starting...", LogLvl.Info);

            _thread = new Thread(FormThread);
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.IsBackground = true;
            _thread.Name = "ChillOverlay";
            _thread.Start();
        }

        public static void Show(string message)
        {
            if (_visible) return;
            _visible = true;

            _listener = new LogListener();
            BepInEx.Logging.Logger.Listeners.Add(_listener);
            Enqueue(message, LogLvl.Info);

            _thread = new Thread(FormThread);
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.IsBackground = true;
            _thread.Name = "ChillOverlay";
            _thread.Start();
        }

        public static void Hide()
        {
            _visible = false;
            if (_listener != null)
            {
                BepInEx.Logging.Logger.Listeners.Remove(_listener);
                _listener.Dispose();
                _listener = null;
            }
            var f = _form;
            if (f != null && !f.IsDisposed)
            {
                try { f.BeginInvoke((Action)(() => f.Close())); } catch { }
            }
            _form = null;
        }

        /// <summary>Called from PlayerLoop. Flushes pending entries to the form thread.</summary>
        public static void Tick()
        {
            if (!_visible) return;
            var f = _form;
            if (f == null || f.IsDisposed) return;

            lock (_lock)
            {
                if (_pending.Count == 0) return;
                var batch = _pending.ToArray();
                _pending.Clear();
                try { f.BeginInvoke((Action)(() => f.AddEntries(batch))); } catch { }
            }
        }

        private static void FormThread()
        {
            _form = new OverlayForm();
            // Flush anything already queued before form was ready
            lock (_lock)
            {
                if (_pending.Count > 0)
                {
                    var batch = _pending.ToArray();
                    _pending.Clear();
                    _form.AddEntries(batch);
                }
            }
            Application.Run(_form);
        }

        private static void Enqueue(string msg, LogLvl level)
        {
            var entry = new LogEntry { Message = msg, Level = level };
            var f = _form;
            if (f != null && !f.IsDisposed && f.IsHandleCreated)
            {
                try { f.BeginInvoke((Action)(() => f.AddEntries(new[] { entry }))); return; } catch { }
            }
            lock (_lock) _pending.Add(entry);
        }

        private class OverlayForm : Form
        {
            [DllImport("user32.dll", SetLastError = true)]
            private static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
            private const int GWL_HWNDPARENT = -8;

            private readonly List<LogEntry> _lines = new List<LogEntry>();
            private const int MaxLines = 50;
            private const int Pad = 10;
            private const int LineH = 16;
            private const int TitleH = 24;
            private readonly System.Drawing.Font _titleFont;
            private readonly System.Drawing.Font _logFont;
            private int _panelW;

            public OverlayForm()
            {
                Text = "ChillPatcher";
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;

                var screen = Screen.PrimaryScreen.WorkingArea;
                _panelW = screen.Width - 20;
                Location = new Point(10, 10);
                Size = new Size(_panelW, TitleH + Pad * 2 + 30);
                BackColor = System.Drawing.Color.FromArgb(12, 12, 25);
                Opacity = 0.92;
                DoubleBuffered = true;

                _titleFont = new System.Drawing.Font("Consolas", 11f, System.Drawing.FontStyle.Bold);
                _logFont = new System.Drawing.Font("Consolas", 9.5f, System.Drawing.FontStyle.Regular);
            }

            public void AddEntries(LogEntry[] entries)
            {
                _lines.AddRange(entries);
                while (_lines.Count > MaxLines) _lines.RemoveAt(0);
                int h = TitleH + Pad * 2 + Math.Max(_lines.Count, 1) * LineH + Pad;
                if (h != Height) Size = new Size(_panelW, h);
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.Clear(BackColor);

                var textArea = new RectangleF(Pad, Pad, _panelW - Pad * 2, TitleH);
                using (var fmt = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                {
                    using (var b = new SolidBrush(System.Drawing.Color.FromArgb(153, 217, 255)))
                        g.DrawString($"ChillPatcher v{MyPluginInfo.PLUGIN_VERSION}", _titleFont, b, textArea, fmt);

                    float y = Pad + TitleH;
                    for (int i = 0; i < _lines.Count; i++)
                    {
                        var entry = _lines[i];
                        System.Drawing.Color c;
                        switch (entry.Level)
                        {
                            case LogLvl.Error: c = System.Drawing.Color.FromArgb(255, 77, 77); break;
                            case LogLvl.Warning: c = System.Drawing.Color.FromArgb(255, 217, 51); break;
                            default: c = System.Drawing.Color.FromArgb(217, 217, 217); break;
                        }
                        var lineRect = new RectangleF(Pad, y, _panelW - Pad * 2, LineH);
                        using (var brush = new SolidBrush(c))
                            g.DrawString(entry.Message ?? "", _logFont, brush, lineRect, fmt);
                        y += LineH;
                    }
                }
            }

            protected override void OnLoad(EventArgs e)
            {
                base.OnLoad(e);
                // Set the game window as owner so overlay stays on top of game only
                try
                {
                    var gameHwnd = Process.GetCurrentProcess().MainWindowHandle;
                    if (gameHwnd != IntPtr.Zero)
                        SetWindowLong(Handle, GWL_HWNDPARENT, gameHwnd);
                }
                catch { }
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    cp.ExStyle |= 0x80;       // WS_EX_TOOLWINDOW
                    cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                    cp.ExStyle |= 0x20;       // WS_EX_TRANSPARENT (click-through)
                    return cp;
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) { _titleFont?.Dispose(); _logFont?.Dispose(); }
                base.Dispose(disposing);
            }
        }

        private class LogListener : ILogListener
        {
            public void LogEvent(object sender, LogEventArgs eventArgs)
            {
                if (!_visible) return;
                var src = eventArgs.Source?.SourceName ?? "";
                bool isOur = src.IndexOf("Chill", StringComparison.OrdinalIgnoreCase) >= 0
                    || src.IndexOf("OneJS", StringComparison.OrdinalIgnoreCase) >= 0
                    || src.IndexOf("Esbuild", StringComparison.OrdinalIgnoreCase) >= 0
                    || src.IndexOf("Harmony", StringComparison.OrdinalIgnoreCase) >= 0;
                bool isErr = eventArgs.Level == BepInEx.Logging.LogLevel.Error
                    || eventArgs.Level == BepInEx.Logging.LogLevel.Fatal;
                if (!isOur && !isErr) return;

                LogLvl level;
                switch (eventArgs.Level)
                {
                    case BepInEx.Logging.LogLevel.Error:
                    case BepInEx.Logging.LogLevel.Fatal:
                        level = LogLvl.Error; break;
                    case BepInEx.Logging.LogLevel.Warning:
                        level = LogLvl.Warning; break;
                    default:
                        level = LogLvl.Info; break;
                }
                Enqueue(eventArgs.ToString(), level);
            }
            public void Dispose() { }
        }
    }
}
