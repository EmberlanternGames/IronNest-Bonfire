using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine.InputSystem.Utilities;

namespace Bonfire
{
    namespace EngineOut
    {
        public static class Container
        {
            // Lazy-load the engine
            internal static EnginePowerController _engine;
            internal static EnginePowerController GetEngine()
            {
                if (_engine == null)
                {
                    Il2CppArrayBase<EnginePowerController> engineControllers = UnityEngine.Object.FindObjectsByType<EnginePowerController>(UnityEngine.FindObjectsSortMode.None);
                    if (engineControllers.Length > 0)
                        _engine = engineControllers[0];
                    else
                    {
                        Plugin.Log("ERROR: Could not get engine power controller.");
                        return null;
                    }
                }

                return _engine;
            } 

            // Pressure system info for applying the health values to
            internal static Il2CppArrayBase<HighPressureSystemManager> pressureSystems;
            internal static Dictionary<HighPressureSystemManager, float> pressureSystemHealthValues = new();
        }

        // Controller system for the engine out side of the Bonfire mod
        public static class Controller
        {            
            internal static MelonPreferences_Entry<bool> cfg_enabled;
            internal static MelonPreferences_Entry<bool> cfg_disableLeverMalfunctions;
            internal static MelonPreferences_Entry<float> cfg_pressureIncRate;
            internal static MelonPreferences_Entry<float> cfg_pressureDecRatePerValve;
            internal static MelonPreferences_Entry<float> cfg_engineTrickleRate;
            internal static MelonPreferences_Entry<int> cfg_engineShutoffThreshold;
            // internal static MelonPreferences_Entry<bool> cfg_engineCutoutEnabled;
            internal static MelonPreferences_Category _configCategory;

            // Initialize all settings related to the engine/pressure overhaul
            public static void OnInitializeEngineOut()
            {
                Plugin.Log("Initializing Engine Out Features.");
                
                _configCategory = MelonPreferences.CreateCategory("Bonfire_EngineOut", "Engine Out");
                
                cfg_enabled = _configCategory.CreateEntry(
                    "enabled",
                    false,
                    "Enabled",
                    "Enables the overhauled engine/pressure system.\n  Possible Values: true/false | Default: false"
                );
                cfg_disableLeverMalfunctions = _configCategory.CreateEntry(
                    "disableLeverMalfunctions",
                    true,
                    "Disable Pressure-Related Lever Malfunctions",
                    "Removes the possibility for the engine and trapdoor levers to malfunction.\n  Possible Values: true/false | Default: true"
                );
                cfg_pressureIncRate = _configCategory.CreateEntry(
                    "pressureIncRate",
                    0.025f,
                    "Pressure Increase Rate",
                    "The rate at which the pressure increases (%/sec) in a system when all valves are closed.\n  Possible Values: >= 0.0 | Default: 0.025 (maxes in 40 sec)"
                );
                cfg_pressureDecRatePerValve = _configCategory.CreateEntry(
                    "pressureDecRatePerValve",
                    0.1f,
                    "Pressure Decrease Rate",
                    "The rate at which the pressure decreases (%/sec/valve) when valves are open.\n  Possible Values: >= 0.0 | Default: 0.1"
                );
                cfg_engineTrickleRate = _configCategory.CreateEntry(
                    "engineTrickleRate",
                    0.1f,
                    "Engine Trickle Rate",
                    "The rate at which each system's pressure decreases (%/sec) when the engine is off.\n  Possible Values: >= 0.0 | Default: 0.1"
                );
                cfg_engineShutoffThreshold = _configCategory.CreateEntry(
                    "engineShutoffThreshold",
                    3,
                    "Engine System Shutoff Threshold",
                    "When this number of systems have 0 pressure, the nest loses power.\n  Possible Values: > 0 | Default: 3"
                );
                // cfg_engineCutoutEnabled = _configCategory.CreateEntry(
                //     "engineCutoutEnabled",
                //     true,
                //     "Enable Engine-Cutting Impacts",
                //     "Whether to make some impacts cut power to the engine. \n  Possible Values: true/false | Default: true"
                // );
            }

            // Always initialize so UI can enable mid-game
            public static void OnInitializeScene()
            {
                bool powerOn = Container.GetEngine().dieselEngine.EnginesRunning;
                Plugin.Log($"Engine On = {powerOn}", true);

                if (Container.pressureSystems != null && Container.pressureSystems.Count > 0)
                {
                    Plugin.Log("Wiping pressure system data to rebuild for the mission", true);
                    Container.pressureSystems.Clear();
                }

                // Initialize all pressure system variables for the mission
                Container.pressureSystemHealthValues.Clear();
                Container.pressureSystems = UnityEngine.Object.FindObjectsByType<HighPressureSystemManager>(UnityEngine.FindObjectsSortMode.None);
                Plugin.Log($"Pressure Systems: {Container.pressureSystems.Count}", true);
                foreach (HighPressureSystemManager manager in Container.pressureSystems)
                {
                    Plugin.Log($"  System: {manager.systemId} initialized", true);
                    Container.pressureSystemHealthValues.Add(manager, powerOn ? 1.0f : 0.0f);
                }
            }

            public static void OnUpdate()
            {
                if (!cfg_enabled.Value || Container.pressureSystems == null)
                    return;

                // Track the total number of empty systems
                int totalEmpty = 0;
                EnginePowerController engine = Container.GetEngine();

                // Calculate the fill rate (positive or negative) to be applied to each pressure system
                foreach (HighPressureSystemManager manager in Container.pressureSystems)
                {
                    float totalFillRate = engine.dieselEngine.EnginesRunning ? cfg_pressureIncRate.Value * Plugin.deltaTime : -cfg_engineTrickleRate.Value * Plugin.deltaTime;
                    foreach (ValveController valve in manager.valves)
                    {
                        totalFillRate -= cfg_pressureDecRatePerValve.Value * valve.GetDamage01() * Plugin.deltaTime;
                    }

                    // Apply the fill rate to each pressure system, clamping them to [0-1]
                    Container.pressureSystemHealthValues[manager] += totalFillRate;
                    Container.pressureSystemHealthValues[manager] = Math.Clamp(Container.pressureSystemHealthValues[manager], 0.0f, 1.0f);
                    totalEmpty += Container.pressureSystemHealthValues[manager] > 0.0f ? 0 : 1;

                    // Apply the health via `ComputeHealthTestPatch`
                    manager.RecomputeHealthAndNotify();
                }

                // Stop the engine if enough valves are open
                if (engine.dieselEngine.EnginesRunning 
                    && engine.dieselEngine.WarningCountdownRemaining <= 0.0f
                    && totalEmpty >= cfg_engineShutoffThreshold.Value)
                {
                    engine.dieselEngine.ForceStop();
                }
            }
        }

        // Disables the requisition console by blocking AttemptRequisition on a non-running engine
        [HarmonyPatch(typeof(RequisitionSlot), nameof(RequisitionSlot.AttemptRequisition))]
        public class DisableRequisitionConsole
        {
            static bool Prefix()
            {
                return !Plugin.missionLoaded || !Controller.cfg_enabled.Value || Container.GetEngine().dieselEngine.EnginesRunning;
            }
        }

        // Hijacks the health value of the system to be driven by our system, not the game's vanilla system
        [HarmonyPatch(typeof(HighPressureSystemManager), nameof(HighPressureSystemManager.ComputeHealth))]
        public class ComputeHealthHijackPatch
        {
            static void Postfix(HighPressureSystemManager __instance, ref float __result)
            {
                if (!Plugin.missionLoaded || !Controller.cfg_enabled.Value)
                    return;

                // Applies the stored health values to the systems
                __result = Container.pressureSystemHealthValues[__instance];
            }
        }

        // Suppresses the pressure-related malfunctions that are applied to the trapdoor levers
        [HarmonyPatch(typeof(LookAtTarget), nameof(LookAtTarget.EvaluateMalfunction))]
        public class SuppressMalfunctionPatch
        {
            static bool Prefix(LookAtTarget __instance)
            {
                // If I can't figure out the exact names of the levers (they're just named "Lever" in the name values) 
                //  then I'll use the EntityId (look at the numbers if this fails in the future, this is IMPERATIVE with every patch)
                if (Controller.cfg_disableLeverMalfunctions.Value 
                        && (__instance.GetEntityId().ToString().CompareTo("156760") == 0
                        || __instance.GetEntityId().ToString().CompareTo("149128") == 0))
                {
                    Plugin.Log($"Supressed Malfunctions for lever {__instance.GetEntityId()}", true);
                    return false;
                }

                Plugin.Log($"Malfunctions for lever {__instance.GetEntityId()} not suppressed", true);
                // Yeah I can do this in 1 line. This is more readable
                return true;
            }
        }
    }
}