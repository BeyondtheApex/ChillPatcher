using System.Collections.Generic;
using BepInEx.Logging;
using ChillPatcher.Patches;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// IME（输入法）状态 API
    /// 
    /// JS 端用法：
    ///   chill.ime.getContext()    → { preedit, cursorPos, highlightedIndex, candidates: [{text, comment}] }
    ///   chill.ime.getInputRect()  → { x, y, width, height } 当前获焦 TextField 的屏幕坐标
    ///   chill.ime.isActive()      → 是否正在输入（有 preedit）
    ///   chill.ime.selectCandidate(index) → 选择候选词
    /// </summary>
    public class ChillIMEApi
    {
        private readonly ManualLogSource _logger;

        public ChillIMEApi(ManualLogSource logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 获取当前 Rime 输入上下文（preedit + 候选词列表）
        /// </summary>
        public string getContext()
        {
            var ctx = KeyboardHookPatch.GetRimeContext();
            if (ctx == null || string.IsNullOrEmpty(ctx.Preedit))
                return "null";

            var candidates = new List<Dictionary<string, object>>();
            if (ctx.Candidates != null)
            {
                foreach (var c in ctx.Candidates)
                {
                    var item = new Dictionary<string, object>
                    {
                        ["text"] = c.Text ?? "",
                    };
                    if (!string.IsNullOrEmpty(c.Comment))
                        item["comment"] = c.Comment;
                    candidates.Add(item);
                }
            }

            var result = new Dictionary<string, object>
            {
                ["preedit"] = ctx.Preedit,
                ["cursorPos"] = ctx.CursorPos,
                ["highlightedIndex"] = ctx.HighlightedIndex,
                ["candidates"] = candidates,
            };

            return JSApiHelper.ToJson(result);
        }

        /// <summary>
        /// 获取当前获焦 TextField 的屏幕坐标和尺寸
        /// </summary>
        public string getInputRect()
        {
            var rect = UIToolkitInputDispatcher.GetFocusedTextFieldRect();
            if (!rect.HasValue)
                return "null";

            var r = rect.Value;
            return JSApiHelper.ToJson(new Dictionary<string, object>
            {
                ["x"] = r.x,
                ["y"] = r.y,
                ["width"] = r.width,
                ["height"] = r.height,
            });
        }

        /// <summary>
        /// 当前是否有 preedit（正在输入中文）
        /// </summary>
        public bool isActive()
        {
            var ctx = KeyboardHookPatch.GetRimeContext();
            return ctx != null && !string.IsNullOrEmpty(ctx.Preedit);
        }

        /// <summary>
        /// 选择指定索引的候选词（0-based）
        /// </summary>
        public bool selectCandidate(int index)
        {
            if (index < 0 || index > 8) return false;
            return KeyboardHookPatch.SelectRimeCandidate(index);
        }
    }
}
