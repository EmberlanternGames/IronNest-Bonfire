using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Net;
using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine.Events;
using UnityEngine.InputSystem.Utilities;

namespace Bonfire
{
    namespace EngineOut
    {
        public static class Collection
        {
            internal static EnginePowerController _engine;
            // Lazy-load the engine
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

            internal static Il2CppArrayBase<HighPressureSystemManager> pressureSystems;
            internal static Dictionary<HighPressureSystemManager, float> pressureSystemHealthValues = new();
        }

        // Controller system for the engine out side of the Bonfire mod
        public static class Controller
        {            
            internal static MelonPreferences_Entry<bool> cfg_enabled;
            internal static MelonPreferences_Entry<float> cfg_pressureIncRate;
            internal static MelonPreferences_Entry<float> cfg_pressureDecRatePerValve;
            internal static MelonPreferences_Entry<float> cfg_engineTrickleMult;
            internal static MelonPreferences_Entry<float> cfg_engineShutoffThreshold;
            internal static MelonPreferences_Entry<float> cfg_enginePowerOnPressure;
            internal static MelonPreferences_Entry<bool> cfg_engineCutoutEnabled;
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
                    "Enables the overhauled engine/pressure system. \n  Possible Values: true/false | Default: false"
                );
                cfg_pressureIncRate = _configCategory.CreateEntry(
                    "pressureIncRate",
                    0.025f,
                    "Pressure Increase Rate",
                    "The rate at which the pressure increases (%/sec) in a system when all valves are closed. \n  Possible Values: >= 0.0 | Default: 0.025 (maxes in 40 sec)"
                );
                cfg_pressureDecRatePerValve = _configCategory.CreateEntry(
                    "pressureDecRatePerValve",
                    0.05f,
                    "Pressure Decrease Rate",
                    "The rate at which the pressure decreases (%/sec/valve) when valves are open. \n  Possible Values: >= 0.0 | Default: 0.05 (1 open drops in 20 sec, 2 in 10, 3 in 6.667 sec)"
                );
                cfg_engineTrickleMult = _configCategory.CreateEntry(
                    "engineTrickleMult",
                    0.02f,
                    "Pressure Decrease Rate",
                    "The rate at which each system's pressure decreases (%/sec/valve) when the engine is off. \n  Possible Values: >= 0.0 | Default: 0.02 (drops from full in 50 sec)"
                );
                cfg_engineShutoffThreshold = _configCategory.CreateEntry(
                    "engineShutoffThreshold",
                    0.73f,
                    "Engine Pressure Shutoff Threshold",
                    "The lowest the total average pressure may get before the engine shuts off. \n  Possible Values: >= 0.0 | Default: 0.73 (73% pressure of 11 systems means 3 fully drained)"
                );
                cfg_enginePowerOnPressure = _configCategory.CreateEntry(
                    "enginePowerOnPressure",
                    0.75f,
                    "Powerup Pressure Boost",
                    "Each system is brought to this pressure setting on powerup. \n  Possible Values: >= engineShutoffThreshold | Default: 0.75 (Just over engineShutoffThreshold value)"
                );
                cfg_engineCutoutEnabled = _configCategory.CreateEntry(
                    "engineCutoutEnabled",
                    true,
                    "Enable Engine-Cutting Impacts",
                    "Whether to make some impacts cut power to the engine. \n  Possible Values: true/false | Default: true"
                );
            }

            // Always initialize so UI can enable mid-game
            public static void OnInitializeScene()
            {
                bool powerOn = Collection.GetEngine().dieselEngine.CaptureMissionState().EnginesRunning;
                Plugin.Log($"Engine On = {powerOn}", true);

                if (Collection.pressureSystems != null && Collection.pressureSystems.Count > 0)
                    Plugin.Log("Wiping pressure system data to rebuild for the mission", true);

                // Initialize all pressure system variables
                Collection.pressureSystems.Clear();
                Collection.pressureSystems = UnityEngine.Object.FindObjectsByType<HighPressureSystemManager>(UnityEngine.FindObjectsSortMode.None);
                Plugin.Log($"Pressure Systems: {Collection.pressureSystems.Count}", true);

                Collection.pressureSystemHealthValues.Clear();
                foreach (HighPressureSystemManager manager in Collection.pressureSystems)
                {
                    Plugin.Log($"  System: {manager.systemId} initialized", true);
                    Collection.pressureSystemHealthValues.Add(manager, powerOn ? 1.0f : 0.0f);
                }
            }

            public static void OnUpdate()
            {
                if (!cfg_enabled.Value || Collection.pressureSystems == null)
                    return;

                // Calculate the fill rate (positive or negative) to be applied to each pressure system
                foreach (HighPressureSystemManager manager in Collection.pressureSystems)
                {
                    float totalFillRate = Collection.GetEngine().Power > 0.0f ? Controller.cfg_pressureIncRate.Value * Plugin.deltaTime : -Controller.cfg_engineTrickleMult.Value * Plugin.deltaTime;
                    foreach (ValveController valve in manager.valves)
                    {
                        totalFillRate -= Controller.cfg_pressureDecRatePerValve.Value * valve.GetDamage01() * Plugin.deltaTime;
                    }
                    Collection.pressureSystemHealthValues[manager] += totalFillRate;
                    Collection.pressureSystemHealthValues[manager] = Math.Min(Math.Max(Collection.pressureSystemHealthValues[manager], 0.0f), 1.0f);

                    // Apply the health via `ComputeHealthTestPatch`
                    manager.RecomputeHealthAndNotify();
                }
            }
        }

        // Hijacks the health value of the system to be driven by our system, not the game's vanilla system
        [HarmonyPatch(typeof(HighPressureSystemManager), nameof(HighPressureSystemManager.ComputeHealth))]
        public class ComputeHealthTestPatch
        {
            static void Postfix(HighPressureSystemManager __instance, ref float __result)
            {
                if (!Controller.cfg_enabled.Value)
                    return;

                __result = Collection.pressureSystemHealthValues[__instance];
            }
        }
    }
}