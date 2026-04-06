using HarmonyLib;
namespace TerminalDesktopMod
{
    [HarmonyPatch(typeof(HUDManager))]
    public static partial class HUDManagerPatch
    {
        [HarmonyPatch(nameof(HUDManager.SetClock))]
        [HarmonyPostfix]
        private static void StartPatch(HUDManager __instance, float timeNormalized, float numberOfHours, bool createNewLine)
        {
            ReferencesStorage.DayTime = __instance.GetClockTimeFormatted(timeNormalized, numberOfHours, createNewLine);
        }
    }
}
