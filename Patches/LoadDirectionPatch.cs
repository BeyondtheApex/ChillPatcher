using HarmonyLib;

namespace ChillPatcher.Patches
{
    /// <summary>
    /// Hook LoadDirectionService.FadeInGame to hide the startup overlay.
    /// FadeInGame is called when the loading screen starts its fade-out animation,
    /// which is the exact moment the game transitions from loading to gameplay.
    /// </summary>
    [HarmonyPatch(typeof(LoadDirectionService), nameof(LoadDirectionService.FadeInGame))]
    public class LoadDirection_FadeInGame_Patch
    {
        static void Postfix()
        {
            Plugin.Log?.LogInfo("[LoadDirection] FadeInGame called - hiding startup overlay");
            BuildSetupOverlay.Hide();
        }
    }
}
