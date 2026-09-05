using MelonLoader;

using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

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
        internal static GenericTimerSceneSync timer;
        internal static FirstPersonController playerController;
                
        internal static MelonPreferences_Entry<bool> _verboseLogging;
        internal static MelonPreferences_Category _configCategory;

        // Logs the provided message (sometimes to the verbose logging system to reduce message spam in console)
        internal static void Log(string msg, bool verbose = false)
        {
            if (verbose && _verboseLogging.Value) MelonLogger.Msg($"[{MyPluginInfo.PLUGIN_NAME} - Verbose] {msg}");
            if (!verbose) MelonLogger.Msg($"[{MyPluginInfo.PLUGIN_NAME}] {msg}");
        }

        internal static float getMissionTime()
        {
            if (timer == null)
                timer = UnityEngine.Object.FindObjectsByType<GenericTimerSceneSync>(UnityEngine.FindObjectsSortMode.InstanceID)[0];

            return timer.CurrentTime;
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
            CaffeineAddict.Controller.onInitializeCaffeineAddict();

            MelonPreferences.Save();
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            Il2CppArrayBase<FirstPersonController> controllers = UnityEngine.Object.FindObjectsByType<FirstPersonController>(UnityEngine.FindObjectsSortMode.None);
            if (playerController == null)
            {
                playerController = controllers[0];
                CaffeineAddict.Controller.defaultSprintSpeed = playerController.sprintSpeed;
                CaffeineAddict.Controller.defaultSpeed = playerController.walkSpeed;
            }
        }
    }
}