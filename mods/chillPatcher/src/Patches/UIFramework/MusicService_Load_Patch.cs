using Bulbul;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using System;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// 在 MusicService.Load 之后通过 IPC 从 OmniMixPlayer 导入歌曲
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
                    // Connect to OmniMixPlayer
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

                    // Import songs
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
