using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MyceliumNetworking;
using System.Collections.Generic;

namespace ContentLibrary
{
    [BepInDependency("RugbugRedfern.MyceliumNetworking", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class ContentPlugin : BaseUnityPlugin
    {
        public static ContentPlugin Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;
        internal static Harmony? Harmony { get; set; }
        internal static ConfigEntry<bool>? ConfigDebugMode { get; private set; }
        internal const uint modID = 1215985525;
        internal bool DebugMode = false;
        internal static bool DebugState;

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            Patch();
            ConfigDebugMode = Config.Bind("General", "Debug", false,
            "If the library should log debug messages.");
            #if DEBUG
                DebugMode = true;
            #endif
            DebugState = DebugMode || ConfigDebugMode.Value;

            MyceliumNetwork.RegisterNetworkObject(this, modID);

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        internal static void Patch()
        {
            Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

            Logger.LogDebug("Patching...");

            Harmony.PatchAll();

            Logger.LogDebug("Finished patching!");
        }

        internal static void Unpatch()
        {
            Logger.LogDebug("Unpatching...");

            Harmony?.UnpatchSelf();

            Logger.LogDebug("Finished unpatching!");
        }
    }
}
