using HarmonyLib;
using System.Collections;
using MelonLoader;
using Il2Cpp;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

// TODO: Investigate the eye opening animation to see if I can lock it and maybe replay it
//         - Visualization here is important, if we can get it before release that's awesome.
// TODO: Narcoleptic Mode - Caffeine wears off after X seconds. Drink more to keep up your speed.
//         - Might be challenging, do after release

namespace Bonfire
{
    namespace CaffeineAddict
    {
        // Controller system for the caffeine addict side of the Bonfire mod
        public static class Controller
        {
            internal static float defaultSprintSpeed = -1.0f;
            internal static float defaultSpeed = -1.0f;
            internal static float nerfFinishedTime = -1.0f;
            internal static float buffFinishedTime = 0.0f;
            internal static float brewReactivatedTime = 0.0f;

            internal static object nerfRoutine = null;

            internal static object buffRoutine = null;


            internal static MelonPreferences_Entry<float> cfg_buffTimePerPct;
            internal static MelonPreferences_Entry<float> cfg_nerfTimePerPct;
            internal static MelonPreferences_Entry<float> cfg_minimumCoffeeQuality;
            internal static MelonPreferences_Entry<float> cfg_greatCoffeeQuality;
            internal static MelonPreferences_Entry<float> cfg_timeBetweenBrews;
            internal static MelonPreferences_Entry<float> cfg_sprintSpeedBuffMult;
            internal static MelonPreferences_Entry<float> cfg_walkSpeedNerfMult;
            internal static MelonPreferences_Entry<bool> cfg_sprintNerfEnabled;
            internal static MelonPreferences_Entry<bool> cfg_enabled;
            internal static MelonPreferences_Category _configCategory;
            
            // Initialize all settings related to being a coffee addict
            public static void OnInitializeCaffeineAddict()
            {
                Plugin.Log("Initializing Caffeine Addict Features.");
                
                _configCategory = MelonPreferences.CreateCategory("Bonfire_CaffeineAddict", "Caffeine Addict");

                cfg_enabled = _configCategory.CreateEntry(
                    "enabled",
                    false,
                    "Enabled",
                    "Enables your addiction to coffee.\n  Possible Values: true/false | Default: false"
                );
                cfg_buffTimePerPct = _configCategory.CreateEntry(
                    "buffTimePerPct",
                    60.0f,
                    "Buff Time Per Percent",
                    "The length of time the coffee buff lasts per percent over the great quality value.\n  Possible Values: >= 0.0 | Default: 60.0"
                );
                cfg_nerfTimePerPct = _configCategory.CreateEntry(
                    "nerfTimePerPct",
                    45.0f,
                    "Nerf Time Per Percent",
                    "The length of time the movement nerf lasts per percent under the bad quality value.\n  Possible Values: >= 0.0 | Default: 45.0"
                );
                cfg_minimumCoffeeQuality = _configCategory.CreateEntry(
                    "minimumCoffeeQuality",
                    85.0f,
                    "Minimum Coffee Quality",
                    "The minimum value the coffee must have to be fully awake immediately.\n  Otherwise you require time to wake up fully.\n  Possible Values: [0.0 - 100.0] | Default: 85.0"
                );
                cfg_greatCoffeeQuality = _configCategory.CreateEntry(
                    "greatCoffeeQuality",
                    90.0f,
                    "Great Coffee Quality",
                    "The minimum value the coffee must have to provide the movement buff.\n  Possible Values: [0.0 - 100.0] | Default: 90.0"
                );
                cfg_timeBetweenBrews = _configCategory.CreateEntry(
                    "timeBetweenBrews",
                    300.0f,
                    "Coffee Machine Reset Time",
                    "The number of seconds it takes for the coffee machine to reset. No spamming coffee for you.\n  Possible Values: >= 0.0 | Default: 300.0"
                );
                cfg_sprintSpeedBuffMult = _configCategory.CreateEntry(
                    "sprintSpeedBuffMult",
                    1.75f,
                    "Buffed Sprint Speed Multiplier",
                    "The scalar that is applied to your speed at which you sprint when you have a great cup.\n  Possible Values: > 0.0 (ideally > 1.0) | Default: 1.75"
                );
                cfg_walkSpeedNerfMult = _configCategory.CreateEntry(
                    "walkSpeedNerfMult",
                    0.6f,
                    "Nerfed Speed Multiplier",
                    "The scalar that is applied to your speed at which you walk before the caffeine kicks in.\n  Possible Values: > 0.0 (ideally < 1.0) | Default: 0.6"
                );
                cfg_sprintNerfEnabled = _configCategory.CreateEntry(
                    "sprintNerfEnabled",
                    true,
                    "No Sprint on No Caffeine",
                    "When this is true, the sprint nerf will apply to your player when the feature is on.\n  When false, it does not apply when the feature is on. \n  Possible Values: true/false | Default: true"
                );
            }

            public static void OnInitializeScene()
            {
                // If the defaults have not yet been initialized, init here
                if (defaultSpeed < 0)
                {
                    Il2CppArrayBase<FirstPersonController> controllers = Object.FindObjectsByType<FirstPersonController>(FindObjectsSortMode.None);
                    Plugin.playerController = controllers[0];
                    defaultSprintSpeed = Plugin.playerController.sprintSpeed;
                    defaultSpeed = Plugin.playerController.walkSpeed;
                }
                
                if (!cfg_enabled.Value) return;

                //Make sure any leftover coroutines are wrapped up
                if (nerfRoutine != null)
                {
                    MelonCoroutines.Stop(nerfRoutine);
                    nerfRoutine = null;
                }
                if (buffRoutine != null)
                {
                    MelonCoroutines.Stop(buffRoutine);
                    buffRoutine = null;
                }

                // Reset the timer vars
                nerfFinishedTime = -1.0f;
                buffFinishedTime = 0.0f;
                brewReactivatedTime = Plugin.getMissionTime();

                // Apply the speed nerf
                Plugin.playerController.walkSpeed = defaultSpeed * cfg_walkSpeedNerfMult.Value;

                // Make the player slow and not able to sprint from start of mission
                if (cfg_sprintNerfEnabled.Value)
                {
                    Plugin.playerController.enableSprint = false;
                    Plugin.playerController.sprintSpeed = defaultSprintSpeed;
                }
            }
        }

        // Apply the effects of the coffee to the player
        [HarmonyPatch(typeof(EspressoCupDrinker), nameof(EspressoCupDrinker.DrinkCoffee))]
        public class ApplyEffects
        {            
            // Runs when the nerf is applied - checks for how much time is left on the nerf.
            //   When the timer expires, restore movement.
            static IEnumerator NerfListener()
            {
                // Timer runs every second
                while (Plugin.getMissionTime() < Controller.nerfFinishedTime)
                {
                    Plugin.Log($"Time left on nerf: {Controller.nerfFinishedTime - Plugin.getMissionTime()}", true);
                    yield return new WaitForSeconds(1.0f);
                }

                // Restore walk speed/sprint
                Plugin.playerController.walkSpeed = Controller.defaultSpeed;
                Plugin.playerController.enableSprint = true;
                Controller.nerfRoutine = null;
            }

            // Runs when the buff is applied - checks for how much time is left on the buff.
            //   When the timer expires, return sprint speed to normal.
            static IEnumerator BuffListener()
            {
                Plugin.playerController.sprintSpeed = Controller.cfg_sprintSpeedBuffMult.Value * Controller.defaultSprintSpeed;

                // Timer runs every second
                while (Plugin.getMissionTime() < Controller.buffFinishedTime)
                {
                    Plugin.Log($"Time left on buff: {Controller.buffFinishedTime - Plugin.getMissionTime()}", true);
                    yield return new WaitForSeconds(1.0f);
                }

                Plugin.playerController.sprintSpeed = Controller.defaultSprintSpeed;
                Controller.buffRoutine = null;
            }

            static bool Prefix(EspressoCupDrinker __instance)
            {
                if (!Controller.cfg_enabled.Value) return true;
                
                float quality = __instance._cup.quality;
                // If the coffee is "Great," apply the buff
                if (quality > Controller.cfg_greatCoffeeQuality.Value)
                {
                    // Gets an ending time for the buff based on how far over you went
                    float qualityOver = quality - Controller.cfg_greatCoffeeQuality.Value;
                    float buffLength = Controller.cfg_buffTimePerPct.Value * qualityOver;
                    float proposedBuffFinishedTime = Plugin.getMissionTime() + buffLength;
                    
                    // Restore walk speed/sprint
                    Plugin.playerController.walkSpeed = Controller.defaultSpeed;
                    Plugin.playerController.enableSprint = true;

                    // If the just drank cup would extend the buff, get ready to replace it
                    if (Controller.buffFinishedTime < proposedBuffFinishedTime && Controller.buffRoutine != null)
                    {
                        MelonCoroutines.Stop(Controller.buffRoutine);
                        Controller.buffRoutine = null;
                    }

                    // If the buff needs applying, apply the buff
                    if (Controller.buffRoutine == null)
                    {
                        // Begin the buff timer
                        Controller.buffFinishedTime = proposedBuffFinishedTime;
                        Controller.nerfFinishedTime = Plugin.getMissionTime();
                        Controller.buffRoutine = MelonCoroutines.Start(BuffListener());
                    }
                }
                // If the coffee is "Poor," apply the nerf
                else if (quality < Controller.cfg_minimumCoffeeQuality.Value)
                {
                    // Gets an ending time for the nerf based on how far under you were
                    float qualityUnder = Controller.cfg_minimumCoffeeQuality.Value - quality;
                    float nerfLength = Controller.cfg_nerfTimePerPct.Value * qualityUnder;
                    float proposedNerfFinishedTime = Plugin.getMissionTime() + nerfLength;

                    // If this is the first cup or a better cup, set the nerfFinishedTime
                    if (Controller.nerfFinishedTime < 0.0f || proposedNerfFinishedTime < Controller.nerfFinishedTime)
                    {
                        // Update the nerfRoutine if needed
                        if (Controller.nerfRoutine != null)
                        {
                            MelonCoroutines.Stop(Controller.nerfRoutine);
                            Controller.nerfRoutine = null;
                        }

                        // Applies the nerf if the nerf is enabled
                        if (Controller.cfg_sprintNerfEnabled.Value)
                        {
                            Controller.nerfFinishedTime = proposedNerfFinishedTime;
                            Controller.nerfRoutine = MelonCoroutines.Start(NerfListener());
                        }
                    }
                }
                // If the coffee is "Good," just restore movement, no buff
                else
                {
                    // Stop the nerfRoutine if needed
                    if (Controller.nerfRoutine != null)
                    {
                        MelonCoroutines.Stop(Controller.nerfRoutine);
                        Controller.nerfRoutine = null;
                    }

                    // Restore walk speed/sprint
                    Controller.nerfFinishedTime = Plugin.getMissionTime();
                    Plugin.playerController.walkSpeed = Controller.defaultSpeed;
                    Plugin.playerController.enableSprint = true;
                }

                return true;
            }
        }

        // Limit the minimum time between brewings to the number of seconds specified in the config
        [HarmonyPatch(typeof(EspressoBrewingController), nameof(EspressoBrewingController.ToggleBrew))]
        public class LimitEspressoMachineUsage
        {
            static bool Prefix()
            {
                // Always return true if feature disabled, only restrict if below time
                return !Controller.cfg_enabled.Value || Plugin.getMissionTime() > Controller.brewReactivatedTime;
            }
        }

        // When brewing is completed, set the time for the next brew to be allowed
        [HarmonyPatch(typeof(EspressoBrewingController), nameof(EspressoBrewingController.CompleteBrew))]
        public class SetEspressoRechargeTime
        {
            static void Postfix()
            {
                if (!Controller.cfg_enabled.Value) return;
                
                // Set the time at which the machine can reactivate
                Controller.brewReactivatedTime = Plugin.getMissionTime() + Controller.cfg_timeBetweenBrews.Value;
                Plugin.Log($"No brew until {Controller.brewReactivatedTime}, currently {Plugin.getMissionTime()}", true);
            }
        }             
    }
}