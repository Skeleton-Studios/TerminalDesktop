using HarmonyLib;
using Unity.Netcode;
namespace TerminalDesktopMod
{
    [HarmonyPatch(typeof(Terminal))]
    public static partial class TerminalPatch
    {
        [HarmonyPatch(nameof(Terminal.Awake))]
        [HarmonyPrefix]
        public static void Awake(ref Terminal __instance)
        {
            ReferencesStorage.Terminal = __instance;
        }
        [HarmonyPatch(nameof(Terminal.LoadNewNode))]
        [HarmonyPrefix]
        public static void LoadNewNode(ref Terminal __instance, ref TerminalNode node)
        {
            DesktopStorage.InvokeChangeTerminalNode(__instance, node);
        }
        [HarmonyPatch(nameof(Terminal.RotateShipDecorSelection))]
        [HarmonyPostfix]
        public static void RotateShipDecorSelection(ref Terminal __instance)
        {
            __instance.ShipDecorSelection.Add(DesktopStorage.ComputerPowerUpgrade);
        }
    }
}
