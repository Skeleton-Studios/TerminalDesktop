using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;
using TerminalApi;
using Unity.Netcode;
using static TerminalApi.TerminalApi;
namespace TerminalDesktopMod
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency("atomic.terminalapi")]
    public class Main : BaseUnityPlugin
    {
        public const string ModGUID = "ss.desktop.terminal";
        public const string ModName = "Terminal Desktop";
        public const string ModVersion = "1.1.0";
        internal static ManualLogSource Log;
        private readonly Harmony harmony = new Harmony(ModGUID);
        private AssetBundle bundle;

        private void Awake()
        {
            Log = Logger;
            Logger.LogInfo($"Plugin Terminal Desktop is loading! v {ModVersion}");
            LoadAssetBundle();

            LoadFlashItem();
            
            var desktopPrefab = LoadAsset<GameObject>("desktop.prefab");
            GenerateRpc(desktopPrefab);
            DesktopStorage.DesktopPrefab = desktopPrefab;
            
            LoadUsbPortObject();
            LoadDesktopIcons();
            LoadDesktopWindows();
            harmony.PatchAll();

            GenerateTerminalCommand();
            Logger.LogInfo($"Plugin Terminal Desktop is loaded! v {ModVersion}");
        }

        private void LoadAssetBundle()
        {
            string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string fullPath = Path.Combine(baseDir, "terminaldesktop");
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Asset bundle not found at path: {fullPath}");
            }
            bundle = AssetBundle.LoadFromFile(fullPath);
            if(!bundle)
            {
                throw new System.Exception("Failed to load AssetBundle from path: " + fullPath);
            }
        }

        private T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            if(bundle is null)
            {
                throw new System.Exception("AssetBundle is not loaded. Cannot load asset.");
            }

            string root = "assets/terminaldesktop/";

            return bundle.LoadAsset<T>(root + assetPath);
        }

        private void LoadFlashItem()
        {
            var flashItem = LoadAsset<Item>("items/flashdrive.asset");
            DesktopStorage.FlashDriveItem = flashItem;
            DesktopStorage.SpawnableScraps.Add(flashItem);
        }

        private void LoadUsbPortObject()
        {
            var usbPortObject = LoadAsset<GameObject>("usbport.prefab");
            DesktopStorage.UsbFlashPort = usbPortObject;
            GenerateRpc(usbPortObject);
        }

        private void LoadDesktopIcons()
        {
            var icons = bundle.LoadAllAssets<GameObject>()
                .Where(x => x.GetComponent<DesktopIconBase>());
            foreach (var icon in icons)
            {
                var desktopIcon = icon.GetComponent<DesktopIconBase>();
                if (desktopIcon is not null && desktopIcon.name != "DesktopIcon")
                    DesktopStorage.AddIcon(desktopIcon);
            }
        }
        private void LoadDesktopWindows()
        {
            var windows = bundle.LoadAllAssets<GameObject>()
                .Where(x => x.GetComponent<DesktopWindowBase>());
            foreach (var window in windows)
            {
                var desktopWindow = window.GetComponent<DesktopWindowBase>();
                if (desktopWindow is not null)
                    DesktopStorage.AddWindow(desktopWindow);
            }
        }
        private void GenerateTerminalCommand()
        {
            TerminalNode buyNode =
                CreateTerminalNode($"Computer improved! \n Your new balance is [playerCredits].\n", true);
            buyNode.creatureName = "upgrade computer";
            TerminalKeyword buyVerbKeyword = CreateTerminalKeyword("upgrade", true);
            TerminalKeyword buyKeyword = CreateTerminalKeyword("computer");

            buyVerbKeyword = buyVerbKeyword.AddCompatibleNoun(buyKeyword, buyNode);
            buyKeyword.defaultVerb = buyVerbKeyword;

            AddTerminalKeyword(buyVerbKeyword);
            AddTerminalKeyword(buyKeyword);
            DesktopStorage.ComputerPowerUpgrade = buyNode;
        }
        /// <summary>
        /// InitializeRPCS methods are not called automatically, so let's call them manually
        /// </summary>
        /// <param name="gameObject"></param>
        private void GenerateRpc(GameObject gameObject)
        {
            var nets = gameObject.GetComponentsInChildren<NetworkBehaviour>();
            if (nets is null) return;
            foreach (var net in nets)
            {
                var methods = net.GetType().GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
                foreach (var initMethod in methods)
                {
                    if (initMethod.DeclaringType is null)
                        continue;
                    if (initMethod.DeclaringType.Namespace is null)
                        continue;
                    if (!initMethod.DeclaringType.Namespace.Contains(nameof(TerminalDesktopMod)))
                        continue;
                    if (initMethod.GetParameters().Length == 0)
                        initMethod.Invoke(null, null);
                }
            }
        }
    }
}