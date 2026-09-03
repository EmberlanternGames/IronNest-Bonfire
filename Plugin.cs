using System.Reflection;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using MelonLoader;

using Il2Cpp;

[assembly: MelonInfo(typeof(Bonfire.Plugin), Bonfire.MyPluginInfo.PLUGIN_NAME, Bonfire.MyPluginInfo.PLUGIN_VERSION, Bonfire.MyPluginInfo.PLUGIN_DEV)]
[assembly: MelonGame("Iron Nest", "Iron Nest Heavy Turret Simulator")]

namespace Bonfire
{
    public static class MyPluginInfo {
        // public const string PLUGIN_GUID = "dev.emberlantern.recordshuffler";
        public const string PLUGIN_NAME = "Bonfire";
        public const string PLUGIN_VERSION = "0.0.1";
        public const string PLUGIN_DEV = "emberlantern";
    }
    
    public class Plugin : MelonMod
    {
        internal static MelonPreferences_Entry<bool> _verboseLogging;
        internal static MelonPreferences_Category _configCategory;

        // Logs the provided message (sometimes to the verbose logging system to reduce message spam in console)
        internal static void Log(string msg, bool verbose = false)
        {
            if (verbose && _verboseLogging.Value) MelonLogger.Msg($"[{MyPluginInfo.PLUGIN_NAME} - Verbose] {msg}");
            if (!verbose) MelonLogger.Msg($"[{MyPluginInfo.PLUGIN_NAME}] {msg}");
        }

        // Initialize the mod data (including all subsystems)
        public override void OnInitializeMelon()
        {
            // Plugin startup logic
            MelonLogger.Msg($"Plugin {MyPluginInfo.PLUGIN_NAME}_{MyPluginInfo.PLUGIN_VERSION} is loaded!");

            _configCategory = MelonPreferences.CreateCategory("Bonfire", "Bonfire");
            _verboseLogging = _configCategory.CreateEntry(
                "VerboseLogging",
                false,
                "Verbose Logging",
                "Logs every Bonfire action/check. Use for debug purposes unless you like big logs. \n  Possible Values: True/False | Default: False"
            );

            Log("Verbose Logging Active", true);

            BreakGun.Controller.onInitializeBreakGun();
        }
    }
}