using System;
using UnityEngine;
using UnityEngine.Events;
using Il2Cpp;
using Il2CppTMPro;

// MASSIVE MASSIVE thank you to Vergeslich for providing the
//  technique used to hook into the UI and text placement.

namespace Bonfire
{
    namespace ModSettingsMenuUI
    {
        // There will only ever be 1 UI Manager, so static it is
        public static class UIManager
        {
            internal static bool resetting = false;
            private static GameObject _bonfireButtonRef;

            // Initializes the main menu scene load/unload hooks
            //   This technique provided by user `vergeslich`
            public static void initUIManagerOnFirstSceneLoad(string sceneName)
            {
                if (!Plugin.firstLoad)
                Plugin.Log("Subscribing to main menu hooks.");

                Action<string> loadHandler = sceneName =>
                {
                    HandleMainMenuLoaded(sceneName);
                };
                Action<string> unloadHandler = sceneName =>
                {
                    HandleMainMenuUnloaded(sceneName);
                }; 
                MissionManager.Instance.MainMenuLoaded += loadHandler;
                MissionManager.Instance.MainMenuUnloaded += unloadHandler;

                // Handle first main menu load since that was skipped this first cycle
                HandleMainMenuLoaded(sceneName);
            }

            // Load the main menu and initialize the text
            //   A large amount of this was adapted from the work of vergeslich's work too
            private static void HandleMainMenuLoaded(string sceneName)
            {
                Plugin.Log("Loading Main Menu");
                GameObject mainMenuObjects = GameObject.Find("MainMenu Interactable objects");
                if (mainMenuObjects == null)
                {
                    Plugin.Log("ERROR: Main Menu Objects could not be loaded");
                    return;
                }

                // Cloning the credits button to move and re-text
                GameObject creditsButton = GameObject.Find("Credits Button");
                if (creditsButton == null)
                {
                    Plugin.Log("ERROR: Credits Button could not be loaded");
                    return;
                }

                // Extract and copy the object to serve as the text for the settings main menu label
                GameObject interactableParent = GameObject.Find("MainMenu Interactable objects");
                GameObject creditsButtonRef = GameObject.Find("Credits Button");
                GameObject bonfireTextObject = UnityEngine.Object.Instantiate(creditsButtonRef, interactableParent.transform);
                bonfireTextObject.name = "Bonfire Settings Text";

                // Shift the text around on screen
                Vector3 offset = new Vector3(0.1f, 0.45f, -0.3f);
                bonfireTextObject.transform.localPosition += offset;
                bonfireTextObject.transform.localRotation *= Quaternion.Euler(0.0f, 0.0f, 4.0f);
                
                // Removes the credit link from the click handler and replace it with my own
                LookAtTarget lookAtTarget = bonfireTextObject.GetComponent<LookAtTarget>();
                for (int i = 0; i < lookAtTarget.onClickDown.GetPersistentEventCount(); i++)
                    lookAtTarget.onClickDown.SetPersistentListenerState(i, UnityEventCallState.Off);
                for (int i = 0; i < lookAtTarget.onClickUp.GetPersistentEventCount(); i++)
                    lookAtTarget.onClickUp.SetPersistentListenerState(i, UnityEventCallState.Off);
                lookAtTarget.RegisterOnClickDown((Action)(() =>
                {
                    ClipboardUI.ToggleClipboard();
                }));

                // Replace the text with my text
                TextMeshPro[] labels = bonfireTextObject.GetComponentsInChildren<TextMeshPro>(true);
                foreach (TextMeshPro label in labels)
                {
                    label.text = "BONFIRE";

                    // Removes the highlighted Credits text      
                    StaticLocalisedText localisedText = label.GetComponent<StaticLocalisedText>();
                    if (localisedText != null)
                    {
                        UnityEngine.Object.Destroy(localisedText);
                    }
                }
                
                _bonfireButtonRef = bonfireTextObject;
                ClipboardUI.BuildClipboard();
            }

            private static void HandleMainMenuUnloaded(string sceneName)
            {
                Plugin.Log("Unloading Main Menu");
                _bonfireButtonRef = null;
            }
        }
    }
}