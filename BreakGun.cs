using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using Il2Cpp;


namespace Bonfire
{
    namespace BreakGun
    {
        // Controller system for the gun breaking side of the Bonfire mod
        public static class Controller
        {
            // The first and second tutorials have no Req console 
            //  and thus cannot be completed with this side of the mod active
            internal static bool isTutorial1Or2 = false;
            
            // Config settings
            internal static MelonPreferences_Entry<bool> cfg_enabled;
            internal static MelonPreferences_Entry<float> cfg_maxAngle;
            internal static MelonPreferences_Entry<int> cfg_maxCharges;
            internal static MelonPreferences_Category _configCategory;

            // Initialize all settings related to breaking the gun
            public static void OnInitializeBreakGun()
            {
                Plugin.Log("Initializing Gun Breaking Features.");

                _configCategory = MelonPreferences.CreateCategory("Bonfire_BreakGuns", "Break Guns");

                cfg_enabled = _configCategory.CreateEntry(
                    "enabled",
                    false,
                    "Enabled",
                    "Enables the limits for the gun angle and max powder charges per shot (first 2 missions not included).\n  Warning: This will make White Shells' traitor ending very, very painful... \n  Useful Tip: Max Range = (maxAngle * maxCharges) / 12\n  Possible Values: true/false | Default: false"
                );
                cfg_maxAngle = _configCategory.CreateEntry(
                    "maxAngle",
                    15.0f,
                    "Max Angle",
                    "Limits the maximum elevation angle the gun may raise to.\n  Possible Values: [0.0 - 60.0] | Default: 15.0"
                );
                cfg_maxCharges = _configCategory.CreateEntry(
                    "maxCharges",
                    2,
                    "Max Powder Charges",
                    "Limits the number of charges you may load per shot.\n  Possible Values: [1 - 6] | Default: 2"
                );
            }
        }
        
        // Clamps the elevation dials (and the slider) to the desired limit
        [HarmonyPatch(typeof(GunElevationDialBinding), nameof(GunElevationDialBinding.Update))]
        public class ElevatorDialUpdatePatch
        {
            static void Postfix(GunElevationDialBinding __instance)
            {
                // Return early if not enabled or in first 2 missions
                if (!Controller.cfg_enabled.Value || Controller.isTutorial1Or2)
                {
                    __instance.elevationDial.maxOutputValue = 60;
                    __instance.elevationDial.maxRotationAngle = 60000;
                    return;
                }
                    
                // Ensure the max angles for the dial are set right
                __instance.elevationDial.maxOutputValue = Controller.cfg_maxAngle.Value;
                __instance.elevationDial.maxRotationAngle = Controller.cfg_maxAngle.Value * 1000.0f;
                
                // Stop the dial and slider when over the max angle
                if (__instance.gun.CurrentElevation > Controller.cfg_maxAngle.Value)
                {
                    // If going fast enough when stopped, CLANG!!!
                    if (__instance.gun.elevationChangeVelocity > 0.5f)
                    {
                        __instance.OnDialOverrideSliderBegan.Invoke(); 
                        __instance.gun.elevationChangeVelocity = 0;
                    }

                    // Sets the slider visually
                    __instance.sliderBindingForVisualSync.SetDesiredSliderSafely(Controller.cfg_maxAngle.Value);
                    // Sets a variety of angle data for the dial to ensure the right limits
                    __instance.elevationDial.lastRawAngle = Controller.cfg_maxAngle.Value * 1000;
                    __instance.elevationDial.lastAngle = Controller.cfg_maxAngle.Value * 1000;
                    __instance.elevationDial.currentRotationAngle = Controller.cfg_maxAngle.Value * 1000;
                    __instance.elevationDial.accumulatedValue = Controller.cfg_maxAngle.Value;
                    __instance.elevationDial.SetDialValue(Controller.cfg_maxAngle.Value);
                    // Set the elevation of the gun to the max angle
                    __instance.gun.SetDesiredElevationFromDial(Controller.cfg_maxAngle.Value);
                }
            }
        }

        // Limits the number of powder charges you may load per shot
        [HarmonyPatch(typeof(PowderChargeController), nameof(PowderChargeController.OnChargeButtonPressed))]
        public class LimitPowderPatch
        {
            static bool Prefix(int index)
            {
                // Return early if not enabled or in first 2 missions
                if (!Controller.cfg_enabled.Value || Controller.isTutorial1Or2)
                    return true;

                // Only dispense N charges, but allow the (N+1)'th lever to be pulled, doing nothing
                return index < Controller.cfg_maxCharges.Value;
            }
        }

        // Ensures the player ALWAYS has access to the Big 2 cards
        //   Move Direction, Position Report
        [HarmonyPatch(typeof(ProgressionManager), nameof(ProgressionManager.BuildUnlockedPunchcards))]
        public class SetupPunchcardPatch
        {
            // Unlocks a given card
            private static void Unlock(ProgressionManager __instance, PunchcardDefinitionV2 card)
            {
                if (!__instance.IsCardUnlocked(card.ID))
                {
                    Plugin.Log($"Unlocking Card: {card.ID}", true);
                    __instance.UserProgression.UnlockedCards.Add(card.ID);
                }
                if (!__instance.UserProgression.CardStates.ContainsKey(card.ID))
                {
                    UserProgression.UserCardState state = new UserProgression.UserCardState();
                    state.CardID = card.ID;
                    state.RemainingUses = card.MaxUses;
                    __instance.UserProgression.CardStates.Add(card.ID, state);
                }
            }

            // Unlocks both required cards
            public static bool Prefix(ProgressionManager __instance, List<PunchcardDefinitionV2> __result, Dictionary<string, PunchcardDefinitionV2> allDefinitions)
            {
                // Return early if not enabled or in first 2 missions
                if (!Controller.cfg_enabled.Value || Controller.isTutorial1Or2)
                {
                    return true;
                }

                Unlock(__instance, allDefinitions["MoveDirection"]);
                Unlock(__instance, allDefinitions["LocationReport"]);

                return true;
            }
        }

        // Only allow the gun to break from this part of the mod when not in mission 1 or 2
        //   (when the req console is inactive, no movement allowed)
        [HarmonyPatch(typeof(MissionManager), nameof(MissionManager.LoadMission))]
        public class SetBreakGunPatch
        {
            static void Postfix(MissionManager __instance)
            {
                Controller.isTutorial1Or2 = __instance.CurrentMissionSceneName.CompareTo("Mission tutorial 1") == 0 
                                || __instance.CurrentMissionSceneName.CompareTo("Mission tutorial 2") == 0;
            }
        }
    }
}