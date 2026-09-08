using MelonLoader;
using HarmonyLib;
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
        internal static GenericTimerSceneSync timer;
        private static float lastTime;
        internal static float deltaTime;
        internal static bool missionLoaded = false;
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

        internal static void setTimeDelta()
        {
            float time = getMissionTime();
            deltaTime = time - lastTime;
            lastTime = time;
        }

        internal static void resetTimeDelta()
        {
            lastTime = getMissionTime();
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
                "Logs every Bonfire action/check. Use for debug purposes unless you like big logs. \n  Possible Values: true/false | Default: false"
            );

            Log("Verbose Logging Active", true);

            EngineOut.Controller.OnInitializeEngineOut();
            BreakGun.Controller.OnInitializeBreakGun();
            CaffeineAddict.Controller.OnInitializeCaffeineAddict();

            MelonPreferences.Save();
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (MissionManager.Instance.CurrentMission == null) 
            {
                missionLoaded = false;
                return;
            }

            // OnSceneWasInitialized runs multiple times - once at game load and also on mission load. 
            //   Maybe also on return to mission select, unsure.
            //   Therefore, wipe data on every init and make sure the references are fresh
            CaffeineAddict.Controller.OnInitializeScene();
            EngineOut.Controller.OnInitializeScene();
            missionLoaded = true;
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            Plugin.Log("Scene Unloaded", true);
            missionLoaded = false;
        }

        public override void OnUpdate()
        {
            // Only update if in a loaded mission
            if (MissionManager.Instance.CurrentMission == null || !missionLoaded) return;

            setTimeDelta();
            EngineOut.Controller.OnUpdate();
        }
    }

    // Handles all generalized stuff that happens on mission loading
    [HarmonyPatch(typeof(MissionManager), nameof(MissionManager.LoadMission))]
    public class OnLoadMissionMainPatch
    {
        static void Postfix()
        {
            Plugin.resetTimeDelta();
        }
    }
}