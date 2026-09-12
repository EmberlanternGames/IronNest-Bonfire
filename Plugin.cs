using MelonLoader;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Bonfire.ModSettingsMenuUI;

[assembly: MelonInfo(typeof(Bonfire.Plugin), Bonfire.MyPluginInfo.PLUGIN_NAME, Bonfire.MyPluginInfo.PLUGIN_VERSION, Bonfire.MyPluginInfo.PLUGIN_DEV)]
[assembly: MelonGame("Iron Nest", "Iron Nest Heavy Turret Simulator")]

// NOTE FOR EMBER: When updating to new patches, make sure lever ids are good

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
        internal static bool firstLoad = true;

        internal static FirstPersonController playerController;
        internal static GenericTimerSceneSync timer;
        internal static bool missionLoaded = false;
        private static float lastTime;
        internal static float deltaTime;
                
        internal static MelonPreferences_Entry<bool> _verboseLogging;
        internal static MelonPreferences_Category _configCategory;

        // Logs the provided message (sometimes to the verbose logging system to reduce message spam in console)
        internal static void Log(string msg, bool verbose = false)
        {
            if (verbose && _verboseLogging.Value) MelonLogger.Msg($"[{MyPluginInfo.PLUGIN_NAME} - Verbose] {msg}");
            if (!verbose) MelonLogger.Msg($"[{MyPluginInfo.PLUGIN_NAME}] {msg}");
        }

        // If the local reference to the timer is not initialized, init it
        //   Then provide the time.
        internal static float getMissionTime()
        {
            if (timer == null)
            {
                timer = Object.FindObjectsByType<GenericTimerSceneSync>(FindObjectsSortMode.InstanceID)[0];
            }

            return timer.CurrentTime;
        }

        // Sets the time delta between last frame and this.
        internal static void setTimeDelta()
        {
            float time = getMissionTime();
            deltaTime = time - lastTime;
            lastTime = time;
        }

        // Sets the time delta to this moment for init purposes
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
                "Logs many Bonfire actions/checks. Use for debug purposes unless you like big logs. \n  Possible Values: true/false | Default: false"
            );

            Log("Verbose Logging Active", true);

            EngineOut.Controller.OnInitializeEngineOut();
            BreakGun.Controller.OnInitializeBreakGun();
            CaffeineAddict.Controller.OnInitializeCaffeineAddict();

            MelonPreferences.Save();
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (firstLoad)
            {
                ModSettingsMenuUI.UIManager.initUIManagerOnFirstSceneLoad(sceneName);
            }
            firstLoad = false;

            bool loading = MissionManager.Instance != null && MissionManager.Instance.CurrentMission != null;
            if (loading) 
            {
                // OnSceneWasInitialized runs multiple times - once at game load and also on mission load. 
                //   Maybe also on return to mission select, unsure.
                //   Therefore, wipe data on every init and make sure the references are fresh
                resetTimeDelta();
                CaffeineAddict.Controller.OnInitializeScene();
                EngineOut.Controller.OnInitializeScene();
                Log("Scene Loaded For Mod", true);
            }

            // Always fix the rotation bug
            Il2CppArrayBase<DialInteractable> dials = UnityEngine.Object.FindObjectsByType<DialInteractable>(UnityEngine.FindObjectsSortMode.None);
            Log($"  Patching rotation system lever bug for this scene", true);
            foreach (DialInteractable dial in dials)
            {
                if (dial.highPressureSystemManager == null || dial.highPressureSystemManager.systemId.CompareTo("RotationHydrolics") != 0) continue;
                
                dial.currentRotationAngle = 0.0f;
                dial.lastRawAngle = 0.0f;
                dial.lastAngle = 0.0f;
                dial.currentRotationAngle = 0.0f;
                dial.detentCurrentAngle = 0.0f;
                dial.detentTargetAngle = 0.0f;
                dial.accumulatedValue = 0.0f;
                Log($"  Patched for scene", true);
            }

            missionLoaded = loading;
            
            if (loading)
                Log("Scene Loaded For Mission", true);
            else 
                Log("Scene Loaded For Main Menu", true);
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            Log("Scene Unloaded", true);
            missionLoaded = false;
        }

        public override void OnUpdate()
        {
            // Only update if in a loaded mission
            if (MissionManager.Instance == null || MissionManager.Instance.CurrentMission == null || !missionLoaded) return;

            setTimeDelta();
            EngineOut.Controller.OnUpdate();
        }
    }
}