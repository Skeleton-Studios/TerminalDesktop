using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
namespace TerminalDesktopMod
{
    [HarmonyPatch(typeof(ManualCameraRenderer))]
    public static partial class ManualCameraRendererPatch
    {
        [HarmonyPatch(nameof(ManualCameraRenderer.Awake))]
        [HarmonyPostfix]
        public static void Start(ref ManualCameraRenderer __instance)
        {
            if (__instance.gameObject.name == "CameraMonitorScript")
                ReferencesStorage.ManualCameraRenderer = __instance;
        }
    }
}
