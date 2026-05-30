using System;
using System.Threading;
using Bulbul;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// 反向补丁：将GameAudioInfo的private方法暴露为public
    /// </summary>
    [HarmonyPatch]
    public static class GameAudioInfo_ReversePatch
    {
        /// <summary>
        /// 反向补丁：暴露DownloadAudioFile方法
        /// </summary>
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(GameAudioInfo), "DownloadAudioFile")]
        public static UniTask<(AudioClip, string, string)> DownloadAudioFile(string uri, CancellationToken ct)
        {
            // 这个方法体会被Harmony自动替换为原始方法的实现
            throw new NotImplementedException("This is a stub for Harmony reverse patch");
        }
    }
}
