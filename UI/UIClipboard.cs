using System.Collections.Generic;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppKamgam.UGUIComponentsForSettings;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// MASSIVE MASSIVE thank you to Vergeslich for providing the
//  techniques used here to modify the clipboard UI
namespace Bonfire
{
    namespace ModSettingsMenuUI
    {
        public static class ClipboardUI
        {
            private static Toggle _toggleGunsBroke; // ???
            private static bool _toggleGunsBrokeIsOn;

            private static GameObject _clipboard;
            private static InputSystemSwitcher _inputSwitcher;

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
                // What to use...
                Transform rowTemplate = layoutParent.Find("ToggleConsoleUGUI (Outline)");
                Transform rowTemplate2 = layoutParent.Find("TextfieldConsoleUGUI (DiscordKey)");

                // GameObject enableGunsBrokeInput = Object.Instantiate(rowTemplate.gameObject, layoutParent);
                
                List<GameObject> originalRows = new();
                for (int i = 0; i < layoutParent.childCount; i++) 
                {
                    originalRows.Add(layoutParent.GetChild(i).gameObject);
                }
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
                
                // Rename the title to "Bonfire Settings"
                GameObject titleObj = clipboardClone.transform.Find("Canvas/Settings menu/Settings/Title Settings").gameObject;
                StaticLocalisedText titleLocalised = titleObj.GetComponent<StaticLocalisedText>();
                if (titleLocalised != null)
                {
                    Object.Destroy(titleLocalised);
                }
                titleObj.GetComponent<TextMeshProUGUI>().text = "Bonfire Settings";

                Transform scrollView = clipboardClone.transform.Find("Canvas/Settings menu/Settings/ContentCtn/Content (Game)/Scroll View");
                scrollView.GetComponent<UnityEngine.UI.ScrollRect>().vertical = false;
                Object.Destroy(scrollView.Find("Scrollbar Vertical").gameObject);

                // SetupRow Shenaniganery
                // Other Stuff too. 
                
                _clipboard = clipboardClone;
                // May not be needed
                _inputSwitcher = Object.FindObjectOfType<InputSystemSwitcher>();
                if (_inputSwitcher == null)
                {
                    Plugin.Log("ERROR: InputSystemSwitcher not found - Bonfire Settings Menu will not work.");
                }                
            }

            // Ensure all settings are applied when apply button is pressed
            private static void AcceptSettings()
            {
                BreakGun.Controller.cfg_enabled.Value = _toggleGunsBrokeIsOn;
            }

            // Lol no idea
            private static TMP_InputField SetupRow(GameObject row, string label, string placeholder, string initialValue, TMP_InputField.ContentType contentType, System.Action<string> onEndEdit)
            {
                Transform inputTransform = row.transform.Find("InputField (TMP)");
                TMP_InputField input = inputTransform.GetComponent<TMP_InputField>();



                return input;
            }

            // Ripped from vergeslich's ConnectUI and modified to be 1 method
            // Brings the clipboard up and places it back down
            public static void ToggleClipboard()
            {
                PickUpZoomTarget zoomTarget = _clipboard.GetComponentInChildren<PickUpZoomTarget>();
                if (zoomTarget.isHeld)
                {
                    zoomTarget.Release();
                    if (EventSystem.current != null)
                    {
                        EventSystem.current.SetSelectedGameObject(null);
                    }
                    if (_inputSwitcher != null)
                    {
                        _inputSwitcher.DisableTextInput();
                    }
                }
                zoomTarget.PickUp();
            }
        }
    }
}