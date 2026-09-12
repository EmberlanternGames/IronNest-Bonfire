using System.Collections.Generic;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppKamgam.SettingsGenerator;
using Il2CppKamgam.UGUIComponentsForSettings;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using MelonLoader;

// MASSIVE MASSIVE thank you to Vergeslich for providing the
//  techniques used here to modify the clipboard UI
namespace Bonfire
{
    namespace ModSettingsMenuUI
    {
        public static class ClipboardUI
        {
            private static ToggleUGUI _toggleGunsBroke;

            private static GameObject _clipboard;
            private static InputSystemSwitcher _inputSwitcher;

            private static void DumpTransform(Transform transform)
            {
                Plugin.Log($"Dumping {transform.name}");

                Il2CppArrayBase<Component> cmps = transform.GetComponentsInChildren<Component>();
                Plugin.Log($"  Components: {cmps.Count}");
                foreach (Component comp in cmps)
                {
                    Plugin.Log($"  Name: {comp.gameObject.name} - Type: {comp.GetIl2CppType().Name}");
                }
                int childCount = transform.GetChildCount();
                Plugin.Log($"  Children: {childCount}");
                for (int i = 0; i < childCount; i++)
                {
                    Plugin.Log($"    {transform.GetChild(i).name}");
                }
            }

            public static void IsAThing(Object obj)
            {
                if (obj)
                    Plugin.Log(" YES TextMeshProUGUI");
                else
                    Plugin.Log(" NO TextMeshProUGUI");
            }

            public static void BuildClipboard()
            {
                // Clone and rename the top of the clipboard
                GameObject clipboardParent = GameObject.Find("MainMenu Interactable objects");
                GameObject clipboardRef =  clipboardParent.transform.Find("Clipboard Menu").gameObject;
                GameObject clipboardClone = Object.Instantiate(clipboardRef, clipboardParent.transform);
                clipboardClone.name = "Bonfire Settings Menu";
                
                clipboardClone.GetComponentInChildren<Interactable>(true).enabled = false;
                foreach (BoxCollider box in clipboardClone.GetComponentsInChildren<BoxCollider>(true))
                {
                    if (box.gameObject.name == "Settings Button")
                    {
                        box.enabled = false;
                    }
                }

                Transform layoutParent = clipboardClone.transform.Find("Canvas/Settings menu/Settings/ContentCtn/Content (Game)/Scroll View/Viewport/Content/Layout");
                Plugin.Log("Transforms for LayoutParent");
                for (int i = 0; i < layoutParent.childCount; i++) {
                    Plugin.Log($"  {layoutParent.GetChild(i).name}");
                }

                // Rows to clone and steal features from (yoink)
                Transform toggleTemplate = layoutParent.Find("ToggleConsoleUGUI (Outline)");
                Transform headlineTemplate = layoutParent.Find("HeadlineUGUI (Game)");
                Transform sliderTemplate = layoutParent.Find("SliderConsoleUGUI (clipboardOffsetPercent)");
                Transform textTemplate = layoutParent.Find("TextfieldConsoleUGUI (DiscordKey)");

                List<GameObject> originalRows = new();
                for (int i = 0; i < layoutParent.childCount; i++) 
                {
                    originalRows.Add(layoutParent.GetChild(i).gameObject);
                }

                // Instantiate from templates before they're deleted
                GameObject gunsBrokeHeadline = Object.Instantiate(headlineTemplate.gameObject, layoutParent);
                GameObject enableGunsBroke = Object.Instantiate(toggleTemplate.gameObject, layoutParent);

                foreach (GameObject row in originalRows)
                {
                    Object.Destroy(row);
                }

                // Remove the base settings UI stuff that I don't need
                Object.Destroy(clipboardClone.transform.Find("Canvas/Settings menu/Settings/TabsCtn").gameObject);
                Object.Destroy(clipboardClone.transform.Find("Canvas/Settings menu/Settings/ContentCtn/Content (Graphics)").gameObject);
                Object.Destroy(clipboardClone.transform.Find("Canvas/Settings menu/Settings/ContentCtn/Content (Audio)").gameObject);
                Object.Destroy(clipboardClone.transform.Find("Canvas/Settings menu/Settings/ContentCtn/Content (Controls)").gameObject);
                Object.Destroy(clipboardClone.transform.Find("Canvas/Settings menu/Settings/ContentCtn/Content (Controls)Gamepad").gameObject);
                // TODO: Hook into the reset button that's leftover to NOT reset the main settings and ONLY the mod settings
                // TODO: Hook into the apply button to set the setting values
                
                // ---- TITLE ----
                // Rename the title to "Bonfire Settings"
                GameObject titleObj = clipboardClone.transform.Find("Canvas/Settings menu/Settings/Title Settings").gameObject;
                // Break link to localization manager
                StaticLocalisedText titleLocalised = titleObj.GetComponent<StaticLocalisedText>();
                if (titleLocalised)
                {
                    Object.Destroy(titleLocalised);
                }
                titleObj.GetComponent<TextMeshProUGUI>().text = "Bonfire Settings";

                // ---- REMOVE SCROLL ----
                // Remove Scroll (for now)
                Transform scrollView = clipboardClone.transform.Find("Canvas/Settings menu/Settings/ContentCtn/Content (Game)/Scroll View");
                scrollView.GetComponent<UnityEngine.UI.ScrollRect>().vertical = false;
                Object.Destroy(scrollView.Find("Scrollbar Vertical").gameObject);

                // ---- CREATE HEADER FOR GUN'S BROKE ----

                gunsBrokeHeadline.SetName("HeadlineUGUI (Gun's Broke)");
                DumpTransform(gunsBrokeHeadline.transform);
                GameObject breakGunHeaderObj = gunsBrokeHeadline.transform.Find("TextTf").gameObject;
                StaticLocalisedText headerTitleLocalised = breakGunHeaderObj.GetComponent<StaticLocalisedText>();
                if (headerTitleLocalised)
                {
                    Object.Destroy(headerTitleLocalised);
                }
                breakGunHeaderObj.GetComponent<TextMeshProUGUI>().text = "Gun's Broke";

                // ---- SETUP ALL ROWS (generalize pls) ----



                // Enable Gun's Broke Row
                enableGunsBroke.SetName("ToggleConsoleUGUI (Break Gun)");

                GameObject labelObj = enableGunsBroke.transform.Find("Label").gameObject;
                StaticLocalisedText labelLocalised = labelObj.GetComponent<StaticLocalisedText>();
                if (labelLocalised)
                {
                    Object.Destroy(labelLocalised);
                }
                labelObj.GetComponent<TextMeshProUGUI>().text = "Enable Broken Gun";

                _toggleGunsBroke = enableGunsBroke.GetComponent<ToggleUGUI>();
                TextMeshProUGUI textUGUI = _toggleGunsBroke.GetComponent<TextMeshProUGUI>();

                _toggleGunsBroke.Value = BreakGun.Controller.cfg_enabled.Value;
                ToggleUGUIResolver resolver = _toggleGunsBroke.GetComponent<ToggleUGUIResolver>();
                if (resolver)
                {
                    Object.Destroy(resolver);
                    Plugin.Log("Resolver deleted");
                }
                else
                {
                    Plugin.Log("No Resolver");
                }

                // bool isInEvent = false; // Prevent _toggleGunsBroke from recursing
                _toggleGunsBroke.OnValueChangedEvent.AddListener((UnityAction<bool>)(val =>
                {
                    if (_toggleGunsBroke.GetComponent<ToggleUGUIResolver>())
                        Plugin.Log(" YES ToggleUGUIResolver");
                    else
                        Plugin.Log(" NO ToggleUGUIResolver");
                }));
                // End Enable Gun's Broke Row

                // toggleField.

                // int childCount = enableGunsBroke.transform.GetChildCount();
                // for (int i = 0; i < childCount; i++)
                // {
                //     Plugin.Log($"  {enableGunsBroke.transform.GetChild(i).name}");
                // }
                

                // DumpTransform(layoutParent.Find("ToggleConsoleUGUI (Outline)/Toggle"));

                // ------ GENERALIZE THIS ------
                // UILeaderboardOptOutListener optOut = enableGunsBroke.GetComponent<UILeaderboardOptOutListener>();
                // if (optOut != null)
                // {
                //     Object.Destroy(optOut);
                // }




                // SetupRow Shenaniganery
                // Other Stuff too. 
                // Repurpose the settings "Reset" button to apply Bonfire settings.
                GameObject resetButtonObj = clipboardClone.transform.Find("Canvas/Settings menu/Settings/ContentCtn/ButtonSecondaryUGUI (reset all)").gameObject;
                Button resetButton = resetButtonObj.GetComponent<Button>();
                Plugin.Log($"Reset event count: {resetButton.onClick.GetPersistentEventCount()}");
                for (int i = 0; i < resetButton.onClick.GetPersistentEventCount(); i++)
                {
                    resetButton.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
                }
                resetButton.onClick.AddListener((UnityAction)ResetBonfireSettings);

                // Repurpose the settings "Apply" button to apply Bonfire settings.
                GameObject applyButtonObj = clipboardClone.transform.Find("Canvas/Settings menu/Settings/ContentCtn/SGButtonPrimaryUGUI (apply)").gameObject;
                Button applyButton = applyButtonObj.GetComponent<Button>();
                Plugin.Log($"Apply event count: {applyButton.onClick.GetPersistentEventCount()}");
                for (int i = 0; i < applyButton.onClick.GetPersistentEventCount(); i++)
                {
                    applyButton.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
                }
                applyButton.onClick.AddListener((UnityAction)ApplyBonfireSettings);
                
                // Make sure "Close" works properly
                GameObject closeButtonObj = clipboardClone.transform.Find("Canvas/Settings menu/Settings/ContentCtn/ButtonSecondaryUGUI (close)").gameObject;
                Button closeButton = closeButtonObj.GetComponent<Button>();
                Plugin.Log($"Close event count: {closeButton.onClick.GetPersistentEventCount()}");
                for (int i = 0; i < closeButton.onClick.GetPersistentEventCount(); i++)
                {
                    closeButton.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
                }
                closeButton.onClick.AddListener((UnityAction)Hide);

                _clipboard = clipboardClone;

                // Necessary for text input
                _inputSwitcher = Object.FindObjectOfType<InputSystemSwitcher>();
                if (_inputSwitcher == null)
                {
                    Plugin.Log("ERROR: InputSystemSwitcher not found - Bonfire Settings Menu Text Input will not work.");
                }                
            }

            // Ensure all settings are reset when reset button is pressed
            private static void ResetBonfireSettings()
            {
                Plugin.Log("Settings Reset");
                BreakGun.Controller.cfg_enabled.Value = BreakGun.Controller.cfg_enabled.DefaultValue;
                _toggleGunsBroke.Value = BreakGun.Controller.cfg_enabled.Value;
                Plugin.Log($"  BrkGun Enable: {BreakGun.Controller.cfg_enabled.Value}", true);
                MelonPreferences.Save();
                // BreakGun.Controller.cfg_enabled.Value = _toggleGunsBrokeIsOn;
            }

            // Ensure all settings are applied when apply button is pressed
            private static void ApplyBonfireSettings()
            {
                BreakGun.Controller.cfg_enabled.Value = _toggleGunsBroke.Value;
                Plugin.Log($"  BrkGun Enable: {BreakGun.Controller.cfg_enabled.Value}", true);
                MelonPreferences.Save();
                Plugin.Log("Settings Applied");
            }

            // Lol no idea
            private static TMP_InputField SetupRow(GameObject row, string label, string placeholder, string initialValue, TMP_InputField.ContentType contentType, System.Action<string> onEndEdit)
            {
                Transform inputTransform = row.transform.Find("InputField (TMP)");
                TMP_InputField input = inputTransform.GetComponent<TMP_InputField>();



                return input;
            }

            public static void Hide()
            {
                // Reset all clipboard settings to current config values
                _toggleGunsBroke.Value = BreakGun.Controller.cfg_enabled.Value;

                // Place clipboard down
                _clipboard.GetComponentInChildren<PickUpZoomTarget>().Release();
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
                if (_inputSwitcher != null)
                {
                    _inputSwitcher.DisableTextInput();
                }
            }

            // Ripped from vergeslich's ConnectUI and modified to be 1 method
            // Brings the clipboard up and places it back down
            public static void ToggleClipboard()
            {
                PickUpZoomTarget zoomTarget = _clipboard.GetComponentInChildren<PickUpZoomTarget>();
                if (zoomTarget.isHeld)
                {
                    Hide();
                    return;
                }
                zoomTarget.PickUp();
            }
        }
    }
}