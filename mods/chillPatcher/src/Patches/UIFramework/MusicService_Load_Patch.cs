using Bulbul;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using System;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// 在 MusicService.Load 之后通过 IPC 从 OmniMixPlayer 导入歌曲
    /// 
    /// 新架构：
    /// 1. 连接后端
    /// 2. 声明能力（32-bit AudioTag 布局）
    /// 3. 后端根据能力声明自动注册 Tag/Album/Song
    /// 4. 全量导入歌曲到游戏 MusicService
    /// 后续增量更新通过 WebSocket playlist.updated 事件
    /// </summary>
    [HarmonyPatch(typeof(MusicService), "Load")]
    public static class MusicService_Load_Patch
    {
        private static bool _songsImported = false;

        [HarmonyPostfix]
        static void Postfix(MusicService __instance)
        {
            MusicService_RemoveLimit_Patch.CurrentInstance = __instance;

            var logger = BepInEx.Logging.Logger.CreateLogSource("MusicService_Load_Patch");

            UniTask.Void(async () =>
            {
                try
                {
                    if (!OmniMixIntegration.Instance.IsConnected)
                    {
                        logger.LogInfo("Connecting to OmniMixPlayer backend...");
                        var ok = await OmniMixIntegration.Instance.ConnectAsync();
                        if (!ok)
                        {
                            logger.LogWarning("Failed to connect to OmniMixPlayer backend");
                            return;
                        }
                    }

                    var replaceOnlyFirstTime = !_songsImported;
                    _songsImported = true;

                    logger.LogInfo("Importing songs from OmniMixPlayer...");
                    var count = await OmniMixIntegration.Instance.ImportSongsToGame(replace: replaceOnlyFirstTime);

                    logger.LogInfo($"Imported {count} songs to game MusicService");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Failed to import songs: {ex}");
                }
            });
        }
    }
}
