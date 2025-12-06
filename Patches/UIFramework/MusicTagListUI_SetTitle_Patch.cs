using System;
using System.Collections.Generic;
using System.Linq;
using Bulbul;
using ChillPatcher.ModuleSystem.Registry;
using ChillPatcher.SDK.Models;
using HarmonyLib;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// 修复SetTitle方法，支持自定义Tag显示
    /// </summary>
    [HarmonyPatch(typeof(MusicTagListUI))]
    public class MusicTagListUI_SetTitle_Patch
    {
        /// <summary>
        /// 完全替换SetTitle方法，添加自定义Tag支持
        /// </summary>
        [HarmonyPatch("SetTitle")]
        [HarmonyPrefix]
        static bool SetTitle_Prefix(MusicTagListUI __instance)
        {
            try
            {
                Plugin.Log.LogDebug("[SetTitle_Prefix] Intercepting SetTitle call");
                // 获取必要的字段
                var musicService = Traverse.Create(__instance).Field("musicService").GetValue<MusicService>();
                var pulldown = Traverse.Create(__instance).Field("_pulldown").GetValue<PulldownListUI>();
                var localizationMaster = Traverse.Create(__instance).Field("_localizationMaster").GetValue<LocalizationMasterWrapper>();

                if (musicService == null || pulldown == null)
                {
                    Plugin.Log.LogWarning("[SetTitle] Missing required fields");
                    return true; // 执行原方法
                }

                Plugin.Log.LogDebug($"[SetTitle] Current tag: {SaveDataManager.Instance.MusicSetting.CurrentAudioTag.Value}");

                // 获取当前Tag
                AudioTag currentTag = SaveDataManager.Instance.MusicSetting.CurrentAudioTag.Value;
                bool hasFavorite = currentTag.HasFlagFast(AudioTag.Favorite);
                AudioTag tagsWithoutFavorite = currentTag.RemoveFlag(AudioTag.Favorite);

                // 获取所有自定义Tag的位值（用于排除）
                var allCustomTags = TagRegistry.Instance?.GetAllTags() ?? new List<TagInfo>();
                var allCustomBits = allCustomTags
                    .Select(t => (AudioTag)t.BitValue)
                    .Aggregate(AudioTag.All, (acc, bit) => acc.AddFlag(bit));

                // 移除所有自定义Tag位
                AudioTag tagsWithoutFavoriteAndCustom = tagsWithoutFavorite;
                foreach (var customTag in allCustomTags)
                {
                    tagsWithoutFavoriteAndCustom = tagsWithoutFavoriteAndCustom.RemoveFlag((AudioTag)customTag.BitValue);
                }

                // 获取所有预定义Tag的位值（All减去Favorite）
                var allPredefinedBits = AudioTag.All.RemoveFlag(AudioTag.Favorite);

                string title = BuildTitle(
                    currentTag,
                    hasFavorite,
                    tagsWithoutFavorite,
                    tagsWithoutFavoriteAndCustom,
                    allPredefinedBits,
                    localizationMaster,
                    __instance
                );

                // 更新标题
                pulldown.ChangeSelectContentText(title);

                Plugin.Log.LogInfo($"[SetTitle] Updated title to: {title}");

                return false; // 阻止原方法执行
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[SetTitle] Error: {ex}");
                return true; // 发生错误时执行原方法
            }
        }

        /// <summary>
        /// 构建标题文本
        /// </summary>
        private static string BuildTitle(
            AudioTag currentTag,
            bool hasFavorite,
            AudioTag tagsWithoutFavorite,
            AudioTag tagsWithoutFavoriteAndCustom,
            AudioTag allPredefinedBits,
            LocalizationMasterWrapper localizationMaster,
            MusicTagListUI instance)
        {
            // 原生Tag本地化映射
            var audioTagLocalizationMap = new Dictionary<AudioTag, string>
            {
                { AudioTag.All, "ui_music_tag_all" },
                { AudioTag.Original, "ui_music_tag_original" },
                { AudioTag.Special, "ui_music_tag_special" },
                { AudioTag.Other, "ui_music_tag_other" },
                { AudioTag.Local, "ui_music_tag_local" },
                { AudioTag.Favorite, "ui_music_tag_favorite" }
            };

            // 1. 纯收藏
            if (currentTag == AudioTag.Favorite)
            {
                return GetLocalizeText(audioTagLocalizationMap[AudioTag.Favorite], localizationMaster, instance);
            }

            // 2. 全部（包含收藏）
            if (hasFavorite && tagsWithoutFavoriteAndCustom == allPredefinedBits)
            {
                return GetLocalizeText(audioTagLocalizationMap[AudioTag.All], localizationMaster, instance);
            }

            // 3. 有收藏 + 其他Tag
            if (hasFavorite)
            {
                return BuildTitleWithFavorite(
                    tagsWithoutFavorite,
                    tagsWithoutFavoriteAndCustom,
                    audioTagLocalizationMap,
                    localizationMaster,
                    instance
                );
            }

            // 4. 全部（不含收藏）
            if (tagsWithoutFavoriteAndCustom == allPredefinedBits)
            {
                return GetLocalizeText(audioTagLocalizationMap[AudioTag.All], localizationMaster, instance);
            }

            // 5. 无Tag选中
            var predefinedTags = GetPredefinedTags(tagsWithoutFavoriteAndCustom);
            var customTags = GetCustomTags(tagsWithoutFavorite);
            
            // ✅ 如果包含Local标签，则忽略自定义标签（因为本地音乐已包含所有自定义标签的歌曲）
            bool hasLocalTag = predefinedTags.Contains(AudioTag.Local);
            if (hasLocalTag)
            {
                customTags.Clear();
            }
            
            Plugin.Log.LogDebug($"[BuildTitle] Predefined tags count: {predefinedTags.Count}, Custom tags count: {customTags.Count}, Has Local: {hasLocalTag}");
            Plugin.Log.LogDebug($"[BuildTitle] tagsWithoutFavorite: {tagsWithoutFavorite}, tagsWithoutFavoriteAndCustom: {tagsWithoutFavoriteAndCustom}");
            
            if (predefinedTags.Count == 0 && customTags.Count == 0)
            {
                return GetLocalizeText("ui_music_tag_select_tag", localizationMaster, instance);
            }

            // 6. 只有自定义Tag
            if (predefinedTags.Count == 0 && customTags.Count > 0)
            {
                return BuildCustomOnlyTitle(customTags);
            }

            // 7. 只有预定义Tag
            if (customTags.Count == 0 && predefinedTags.Count > 0)
            {
                return BuildPredefinedOnlyTitle(predefinedTags, audioTagLocalizationMap, localizationMaster, instance);
            }

            // 8. 混合Tag（预定义 + 自定义）
            return BuildMixedTitle(predefinedTags, customTags, audioTagLocalizationMap, localizationMaster, instance);
        }

        /// <summary>
        /// 构建包含收藏的标题
        /// </summary>
        private static string BuildTitleWithFavorite(
            AudioTag tagsWithoutFavorite,
            AudioTag tagsWithoutFavoriteAndCustom,
            Dictionary<AudioTag, string> audioTagLocalizationMap,
            LocalizationMasterWrapper localizationMaster,
            MusicTagListUI instance)
        {
            var predefinedTags = GetPredefinedTags(tagsWithoutFavoriteAndCustom);
            var customTags = GetCustomTags(tagsWithoutFavorite);
            var favoriteText = GetLocalizeText(audioTagLocalizationMap[AudioTag.Favorite], localizationMaster, instance);

            // ✅ 如果包含Local标签，则忽略自定义标签（因为本地音乐已包含所有自定义标签的歌曲）
            bool hasLocalTag = predefinedTags.Contains(AudioTag.Local);
            if (hasLocalTag)
            {
                customTags.Clear();
            }

            // 只有收藏（已在前面处理）
            if (predefinedTags.Count == 0 && customTags.Count == 0)
            {
                return favoriteText;
            }

            // 单个预定义Tag + 收藏
            if (predefinedTags.Count == 1 && customTags.Count == 0)
            {
                var tagText = GetLocalizeText(audioTagLocalizationMap[predefinedTags[0]], localizationMaster, instance);
                return $"{favoriteText} & {tagText}";
            }

            // 多个Tag（收藏显示在最前面）
            var allTagTexts = new List<string>();
            
            // 添加预定义Tag文本
            foreach (var tag in predefinedTags)
            {
                if (audioTagLocalizationMap.ContainsKey(tag))
                {
                    allTagTexts.Add(GetLocalizeText(audioTagLocalizationMap[tag], localizationMaster, instance));
                }
            }
            
            // 添加自定义Tag文本
            foreach (var customTag in customTags)
            {
                allTagTexts.Add(customTag.DisplayName);
            }

            // 获取配置的最大显示数量
            int maxTags = PluginConfig.MaxTagsInTitle.Value;
            int totalTags = allTagTexts.Count;
            var displayTags = allTagTexts.Take(maxTags).ToList();
            
            string tagsText;
            if (totalTags > maxTags)
            {
                tagsText = string.Join(" & ", displayTags) + " 等其他";
            }
            else
            {
                tagsText = string.Join(" & ", displayTags);
            }
            
            // 收藏显示在最前面
            return $"{favoriteText} & {tagsText}";
        }

        /// <summary>
        /// 构建纯自定义Tag标题
        /// </summary>
        private static string BuildCustomOnlyTitle(List<TagInfo> customTags)
        {
            if (customTags.Count == 1)
            {
                return customTags[0].DisplayName;
            }

            // 使用配置的最大显示数量
            int maxTags = PluginConfig.MaxTagsInTitle.Value;
            var displayTags = customTags.Take(maxTags).Select(t => t.DisplayName).ToList();
            
            if (customTags.Count > maxTags)
            {
                return string.Join(" & ", displayTags) + " 等其他";
            }
            
            return string.Join(" & ", displayTags);
        }

        /// <summary>
        /// 构建纯预定义Tag标题
        /// </summary>
        private static string BuildPredefinedOnlyTitle(
            List<AudioTag> predefinedTags,
            Dictionary<AudioTag, string> audioTagLocalizationMap,
            LocalizationMasterWrapper localizationMaster,
            MusicTagListUI instance)
        {
            if (predefinedTags.Count == 1)
            {
                return GetLocalizeText(audioTagLocalizationMap[predefinedTags[0]], localizationMaster, instance);
            }

            var tagTexts = predefinedTags
                .Where(tag => audioTagLocalizationMap.ContainsKey(tag))
                .Select(tag => GetLocalizeText(audioTagLocalizationMap[tag], localizationMaster, instance))
                .ToList();

            return string.Join(" & ", tagTexts);
        }

        /// <summary>
        /// 构建混合Tag标题（预定义 + 自定义）
        /// </summary>
        private static string BuildMixedTitle(
            List<AudioTag> predefinedTags,
            List<TagInfo> customTags,
            Dictionary<AudioTag, string> audioTagLocalizationMap,
            LocalizationMasterWrapper localizationMaster,
            MusicTagListUI instance)
        {
            var allTagTexts = new List<string>();
            
            // 添加预定义Tag（本地化）
            foreach (var tag in predefinedTags)
            {
                if (audioTagLocalizationMap.ContainsKey(tag))
                {
                    allTagTexts.Add(GetLocalizeText(audioTagLocalizationMap[tag], localizationMaster, instance));
                }
            }
            
            // 添加自定义Tag（直接使用字符串）
            foreach (var customTag in customTags)
            {
                allTagTexts.Add(customTag.DisplayName);
            }

            // 使用配置的最大显示数量
            int maxTags = PluginConfig.MaxTagsInTitle.Value;
            int totalTags = allTagTexts.Count;
            var displayTags = allTagTexts.Take(maxTags).ToList();
            
            if (totalTags > maxTags)
            {
                return string.Join(" & ", displayTags) + " 等其他";
            }
            
            return string.Join(" & ", displayTags);
        }

        /// <summary>
        /// 获取预定义Tag列表（排除All和Favorite）
        /// </summary>
        private static List<AudioTag> GetPredefinedTags(AudioTag tags)
        {
            var result = new List<AudioTag>();
            
            // 手动检查每个预定义Tag
            if ((tags & AudioTag.Original) == AudioTag.Original)
                result.Add(AudioTag.Original);
            if ((tags & AudioTag.Special) == AudioTag.Special)
                result.Add(AudioTag.Special);
            if ((tags & AudioTag.Other) == AudioTag.Other)
                result.Add(AudioTag.Other);
            if ((tags & AudioTag.Local) == AudioTag.Local)
                result.Add(AudioTag.Local);
                
            return result;
        }

        /// <summary>
        /// 获取自定义Tag列表
        /// </summary>
        private static List<TagInfo> GetCustomTags(AudioTag tags)
        {
            var allCustomTags = TagRegistry.Instance?.GetAllTags() ?? new List<TagInfo>();
            Plugin.Log.LogDebug($"[GetCustomTags] Total custom tags registered: {allCustomTags.Count}");
            Plugin.Log.LogDebug($"[GetCustomTags] Input tags: {tags}");
            
            var result = allCustomTags
                .Where(customTag =>
                {
                    bool hasTag = (tags & (AudioTag)customTag.BitValue) == (AudioTag)customTag.BitValue;
                    Plugin.Log.LogDebug($"[GetCustomTags] Tag '{customTag.DisplayName}' (bit: {customTag.BitValue}): {hasTag}");
                    return hasTag;
                })
                .ToList();
                
            Plugin.Log.LogDebug($"[GetCustomTags] Matched {result.Count} custom tags");
            return result;
        }

        /// <summary>
        /// 获取本地化文本
        /// </summary>
        private static string GetLocalizeText(string localizationId, LocalizationMasterWrapper localizationMaster, MusicTagListUI instance)
        {
            if (localizationMaster == null)
            {
                Plugin.Log.LogWarning($"[GetLocalizeText] LocalizationMaster is null for ID: {localizationId}");
                return localizationId;
            }

            string text;
            if (localizationMaster.TryGet(localizationId, out text))
            {
                return text;
            }

            Plugin.Log.LogWarning($"[GetLocalizeText] Localization not found: {localizationId}");
            return localizationId;
        }
    }
}
