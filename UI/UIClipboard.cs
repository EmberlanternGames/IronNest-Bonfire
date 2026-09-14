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
using System;

// Portions of the UI construction below are adapted from vergeslich03's APNest-Client
//  (client/ConnectUI.cs, client/MainMenuAPHook.cs), used under the MIT License.
//  https://github.com/vergeslich03/APNest-Client
//  Full license text: THIRD-PARTY-NOTICES.md

// MASSIVE MASSIVE thank you to Vergeslich for providing the
//  techniques used here to modify the clipboard UI
namespace Bonfire
{
    namespace ModSettingsMenuUI
    {
        public static class UILibrary
        {
            // Renames the header object to the desired label
            public static void MakeHeader(GameObject headlineObj, string label)
            {
                headlineObj.SetName($"HeadlineUGUI ({label})");
                
                // Replace the text in the label with the provided label
                GameObject headerObj = headlineObj.transform.Find("TextTf").gameObject;
                StaticLocalisedText headerTitleLocalised = headerObj.GetComponent<StaticLocalisedText>();
                if (headerTitleLocalised)
                {
                    UnityEngine.Object.Destroy(headerTitleLocalised);
                }
                headerObj.GetComponent<TextMeshProUGUI>().text = label;
            }

            // Customizes the provided toggle with a start state and a label
            public static ToggleUGUI MakeToggle(GameObject toggleObj, bool begin, string label)
            {
                toggleObj.SetName($"ToggleUGUI ({label})");

                // Replace the text in the label with the provided label
                GameObject toggleLabelObj = toggleObj.transform.Find("Label").gameObject;
                StaticLocalisedText toggleLabelLocalised = toggleLabelObj.GetComponent<StaticLocalisedText>();
                if (toggleLabelLocalised)
                {
                   UnityEngine.Object.Destroy(toggleLabelLocalised);
                }
                toggleLabelObj.GetComponent<TextMeshProUGUI>().text = label;

                // Initialize the toggle state and remove leftover external links from Toggle cloning
                ToggleUGUI outputToggleUGUI = toggleObj.GetComponent<ToggleUGUI>();
                outputToggleUGUI.Value = begin;
                ToggleUGUIResolver enableResolver = outputToggleUGUI.GetComponent<ToggleUGUIResolver>();
                if (enableResolver)
                {
                   UnityEngine.Object.Destroy(enableResolver);
                    Plugin.Log($"Toggle resolver deleted for {label}", true);
                }
                else
                {
                    Plugin.Log($"No toggle resolver for {label}", true);
                }

                return outputToggleUGUI;
            }

            // Customizes the provided slider with start value, label, range, step size, and value format
            public static SliderUGUI MakeSlider(GameObject sliderObj, float begin, string label, float min, float max, float stepSize, string valueFormat)
            {
                sliderObj.SetName($"SliderConsoleUGUI ({label})");

                // Replace the text in the label with the provided label
                GameObject sliderLabelObj = sliderObj.transform.Find("Label").gameObject;
                StaticLocalisedText sliderLabelLocalized = sliderLabelObj.GetComponent<StaticLocalisedText>();
                if (sliderLabelLocalized)
                {
                    UnityEngine.Object.Destroy(sliderLabelLocalized);
                }
                sliderLabelObj.GetComponent<TextMeshProUGUI>().text = label;

                // Remove the clipboard slider effect from the slider (leftover from cloning)
                SliderUGUI outputSliderUGUI = sliderObj.GetComponent<SliderUGUI>();
                ClipboardStateRelay clipboardRelay = outputSliderUGUI.GetComponent<ClipboardStateRelay>();
                if (clipboardRelay)
                {
                    Plugin.Log($"Deleting clipboard relay from slider {label}", true);
                    UnityEngine.Object.Destroy(clipboardRelay);
                }
                else
                {
                    Plugin.Log($"Could not delete clipboard relay from slider {label}", true);
                }

                // Initialize all slider values
                outputSliderUGUI.MaxValue = max;
                outputSliderUGUI.MinValue = min;
                outputSliderUGUI.StepSize = stepSize;
                outputSliderUGUI.ValueFormat = valueFormat;
                outputSliderUGUI.Slider.wholeNumbers = stepSize % 1 == 0;
                outputSliderUGUI.Value = begin;

                // Remove leftover external links from Slider cloning
                SliderUGUIResolver gunsBrokeMaxAngleResolver = outputSliderUGUI.GetComponent<SliderUGUIResolver>();
                if (gunsBrokeMaxAngleResolver)
                {
                   UnityEngine.Object.Destroy(gunsBrokeMaxAngleResolver);
                    Plugin.Log($"Resolver deleted for slider {label}");
                }
                else
                {
                    Plugin.Log($"No slider resolver for {label}");
                }

                return outputSliderUGUI;
            }

            public static TMP_InputField MakeTextInput(GameObject textObj, string begin, string label, string defaultVal, TMP_InputField.ContentType type, InputSystemSwitcher switcher, System.Action<string, TMP_InputField> limiter)
            {
                
                // Text technique provided by vergeslich, extrapolated to other widgets
                UILeaderboardOptOutListener optOut = textObj.GetComponent<UILeaderboardOptOutListener>();
                if (optOut != null)
                {
                    UnityEngine.Object.Destroy(optOut);
                }

                GameObject labelObj = textObj.transform.Find("Label").gameObject;
                StaticLocalisedText labelLocalised = labelObj.GetComponent<StaticLocalisedText>();
                if (labelLocalised != null)
                {
                    UnityEngine.Object.Destroy(labelLocalised);
                }
                labelObj.GetComponent<TextMeshProUGUI>().text = label;

                Transform inputFieldTransform = textObj.transform.Find("InputField (TMP)");
                TMP_InputField inputField = inputFieldTransform.GetComponent<TMP_InputField>();

                InputFieldHelper inputFieldHelper = inputFieldTransform.GetComponent<InputFieldHelper>();
                if (inputFieldHelper != null)
                {
                    UnityEngine.Object.Destroy(inputFieldHelper);
                }

                inputField.contentType = type;
                inputField.text = begin;
                inputField.interactable = true;
                inputField.readOnly = false;

                CanvasGroup canvasGroup = inputFieldTransform.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }

                inputFieldTransform.Find("Text Area/Placeholder").GetComponent<TextMeshProUGUI>().text = defaultVal;


                System.Action<string> action = input =>
                {
                    limiter(input, inputField);
                };
                inputField.onEndEdit.AddListener(action);

                System.Action<string> onSelect = value =>
                {
                    if (switcher != null)
                    {
                        switcher.EnableTextInput();
                    }
                    else
                    {
                        Plugin.Log("ERROR: Could not find switcher!");
                    }
                };
                System.Action<string> onDeselect = value =>
                {
                    if (switcher != null)
                    {
                        switcher.DisableInputForPopup();
                        switcher.DelayedInputEnable();
                    }
                    else
                    {
                        Plugin.Log("ERROR: Could not find switcher!");
                    }
                };
                
                inputField.onSelect.AddListener(onSelect);
                inputField.onDeselect.AddListener(onDeselect);

                return inputField;
            }
        }

        public static class ClipboardUI
        {
            private static Dictionary<ToggleUGUI, MelonPreferences_Entry<bool>> _toggles = [];
            private static Dictionary<SliderUGUI, MelonPreferences_Entry<float>> _sliderFloats = [];
            private static Dictionary<SliderUGUI, MelonPreferences_Entry<int>> _sliderInts = [];
            private static Dictionary<TMP_InputField, MelonPreferences_Entry<float>> _numberInputs = [];

            private static Button _applyButton;
            private static Button _closeButton;
            private static Button _resetButton;
            

            private static GameObject _clipboard;
            private static InputSystemSwitcher _inputSwitcher;

            private static void DumpTransform(Transform transform)
            {
                Plugin.Log($"Dumping {transform.gameObject.name}");

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

            public static void IsAThing(UnityEngine.Object obj)
            {
                if (obj)
                    Plugin.Log(" YES TextMeshProUGUI");
                else
                    Plugin.Log(" NO TextMeshProUGUI");
            }

            public static void BuildClipboard()
            {
                _toggles.Clear();
                _sliderFloats.Clear();
                _sliderInts.Clear();
                _numberInputs.Clear();

                // Necessary for text input
                _inputSwitcher = UnityEngine.Object.FindObjectOfType<InputSystemSwitcher>();
                if (_inputSwitcher == null)
                {
                    InputSystemSwitcher[] allSwitchers = Resources.FindObjectsOfTypeAll<InputSystemSwitcher>();
                    if (allSwitchers.Length > 0)
                    {
                        _inputSwitcher = allSwitchers[0];
                        Plugin.Log($"Switcher: {_inputSwitcher.name}", true);
                    }
                }

                if (_inputSwitcher == null)
                {
                    Plugin.Log("ERROR: InputSystemSwitcher not found — Bonfire settings text input non-functional.");
                }

                // Clone and rename the top of the clipboard
                GameObject clipboardParent = GameObject.Find("MainMenu Interactable objects");
                GameObject clipboardRef =  clipboardParent.transform.Find("Clipboard Menu").gameObject;
                GameObject clipboardClone = UnityEngine.Object.Instantiate(clipboardRef, clipboardParent.transform);
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
                GameObject gunsBrokeHeadline = UnityEngine.Object.Instantiate(headlineTemplate.gameObject, layoutParent);
                GameObject gunsBrokeEnable = UnityEngine.Object.Instantiate(toggleTemplate.gameObject, layoutParent);
                GameObject gunsBrokeMaxAngleSlider = UnityEngine.Object.Instantiate(sliderTemplate.gameObject, layoutParent);
                GameObject gunsBrokeMaxChargesSlider = UnityEngine.Object.Instantiate(sliderTemplate.gameObject, layoutParent);

                GameObject caffeineAddictHeadline = UnityEngine.Object.Instantiate(headlineTemplate.gameObject, layoutParent);
                GameObject caffeineAddictEnable = UnityEngine.Object.Instantiate(toggleTemplate.gameObject, layoutParent);
                GameObject caffeineAddictMinQualitySlider = UnityEngine.Object.Instantiate(sliderTemplate.gameObject, layoutParent);
                GameObject caffeineAddictMaxQualitySlider = UnityEngine.Object.Instantiate(sliderTemplate.gameObject, layoutParent);
                GameObject caffeineAddictMoveNerfMultiplier = UnityEngine.Object.Instantiate(sliderTemplate.gameObject, layoutParent);
                GameObject caffeineAddictSprintBuffMultiplier = UnityEngine.Object.Instantiate(sliderTemplate.gameObject, layoutParent);
                GameObject caffeineAddictBrewCooldown = UnityEngine.Object.Instantiate(textTemplate.gameObject, layoutParent);
                GameObject caffeineAddictSprintNerfEnable = UnityEngine.Object.Instantiate(toggleTemplate.gameObject, layoutParent);

                GameObject engineOutHeadline = UnityEngine.Object.Instantiate(headlineTemplate.gameObject, layoutParent);
                GameObject engineOutEnable = UnityEngine.Object.Instantiate(toggleTemplate.gameObject, layoutParent);
                GameObject engineOutPressureIncRate = UnityEngine.Object.Instantiate(textTemplate.gameObject, layoutParent);
                GameObject engineOutPressureDecRate = UnityEngine.Object.Instantiate(textTemplate.gameObject, layoutParent);
                GameObject engineOutPressureTrickleRate = UnityEngine.Object.Instantiate(textTemplate.gameObject, layoutParent);
                GameObject engineOutShutoffThreshold = UnityEngine.Object.Instantiate(sliderTemplate.gameObject, layoutParent);

                // Remove all original rows to make room for new ones
                foreach (GameObject row in originalRows)
                {
                   UnityEngine.Object.Destroy(row);
                }

                // Remove the base settings UI stuff that I don't need
                UnityEngine.Object.Destroy(clipboardClone.transform.Find("Canvas/Settings menu/Settings/TabsCtn").gameObject);
                UnityEngine.Object.Destroy(clipboardClone.transform.Find("Canvas/Settings menu/Settings/ContentCtn/Content (Graphics)").gameObject);
                UnityEngine.Object.Destroy(clipboardClone.transform.Find("Canvas/Settings menu/Settings/ContentCtn/Content (Audio)").gameObject);
                UnityEngine.Object.Destroy(clipboardClone.transform.Find("Canvas/Settings menu/Settings/ContentCtn/Content (Controls)").gameObject);
                UnityEngine.Object.Destroy(clipboardClone.transform.Find("Canvas/Settings menu/Settings/ContentCtn/Content (Controls)Gamepad").gameObject);
                
                // Rename the title to "Bonfire Settings"
                GameObject titleObj = clipboardClone.transform.Find("Canvas/Settings menu/Settings/Title Settings").gameObject;
                // Break link to localization manager
                StaticLocalisedText titleLocalised = titleObj.GetComponent<StaticLocalisedText>();
                if (titleLocalised)
                {
                   UnityEngine.Object.Destroy(titleLocalised);
                }
                titleObj.GetComponent<TextMeshProUGUI>().text = "Bonfire Settings";

                // Setup all feature headers
                UILibrary.MakeHeader(gunsBrokeHeadline, "Gun's Broke");
                UILibrary.MakeHeader(caffeineAddictHeadline, "Caffeine Addict");
                UILibrary.MakeHeader(engineOutHeadline, "Engine Out");

                // Setup all content rows
                _toggles.Add(UILibrary.MakeToggle(gunsBrokeEnable, BreakGun.Controller.cfg_enabled.Value, "Enable Gun's Broke"), BreakGun.Controller.cfg_enabled);
                _toggles.Add(UILibrary.MakeToggle(caffeineAddictEnable, CaffeineAddict.Controller.cfg_enabled.Value, "Enable Caffeine Addict"), CaffeineAddict.Controller.cfg_enabled);
                _toggles.Add(UILibrary.MakeToggle(caffeineAddictSprintNerfEnable, CaffeineAddict.Controller.cfg_sprintNerfEnabled.Value, "Enable Sprint Nerf"), CaffeineAddict.Controller.cfg_sprintNerfEnabled);
                _toggles.Add(UILibrary.MakeToggle(engineOutEnable, EngineOut.Controller.cfg_enabled.Value, "Enable Engine Out"), EngineOut.Controller.cfg_enabled);

                _sliderFloats.Add(UILibrary.MakeSlider(gunsBrokeMaxAngleSlider, BreakGun.Controller.cfg_maxAngle.Value, "Max Angle", 0.0f, 60.0f, 1.0f, "{0:N0}°"), BreakGun.Controller.cfg_maxAngle);
                _sliderInts.Add(UILibrary.MakeSlider(gunsBrokeMaxChargesSlider, BreakGun.Controller.cfg_maxCharges.Value, "Max Charges", 1, 6, 1, "{0:N0}"), BreakGun.Controller.cfg_maxCharges);
                _sliderFloats.Add(UILibrary.MakeSlider(caffeineAddictMinQualitySlider, CaffeineAddict.Controller.cfg_minimumCoffeeQuality.Value, "Min Coffee Quality", 0.0f, 100.0f, 1.0f, "{0:N0}%"), CaffeineAddict.Controller.cfg_minimumCoffeeQuality);
                _sliderFloats.Add(UILibrary.MakeSlider(caffeineAddictMaxQualitySlider, CaffeineAddict.Controller.cfg_greatCoffeeQuality.Value, "Great Coffee Quality", 0.0f, 100.0f, 1.0f, "{0:N0}%"), CaffeineAddict.Controller.cfg_greatCoffeeQuality);
                _sliderFloats.Add(UILibrary.MakeSlider(caffeineAddictMoveNerfMultiplier, CaffeineAddict.Controller.cfg_walkSpeedNerfMult.Value, "Walk Speed Nerf", 0.0f, 1.0f, 0.01f, "{0:N2}"), CaffeineAddict.Controller.cfg_walkSpeedNerfMult);
                _sliderFloats.Add(UILibrary.MakeSlider(caffeineAddictSprintBuffMultiplier, CaffeineAddict.Controller.cfg_sprintSpeedBuffMult.Value, "Sprint Speed Buff", 1.0f, 5.0f, 0.05f, "{0:N2}"),  CaffeineAddict.Controller.cfg_sprintSpeedBuffMult);
                _sliderInts.Add(UILibrary.MakeSlider(engineOutShutoffThreshold, EngineOut.Controller.cfg_engineShutoffThreshold.Value, "Empty System Shutoff Threshold", 1, 13, 1, "{0:N0}"), EngineOut.Controller.cfg_engineShutoffThreshold);
        
                // This action checks if an item entered in the field is greater than or equal to 0, otherwise it bumps to 0
                Action<string, TMP_InputField> grtrEqualZeroTemplate = (value, inputField) =>
                {
                    if (float.TryParse(value, out float result))
                    {
                        // Only ever check if greater than or equal to 0 for now, may need to change in the future to customize check
                        if (result < 0)
                        inputField.text = CaffeineAddict.Controller.cfg_timeBetweenBrews.DefaultValue.ToString();
                    }
                    else
                    {
                        Plugin.Log($"ERROR: Could not assign `{value}` to text input");
                        inputField.text = inputField.transform.Find("Text Area/Placeholder").GetComponent<TextMeshProUGUI>().text;
                    }
                };
                _numberInputs.Add(UILibrary.MakeTextInput(caffeineAddictBrewCooldown, CaffeineAddict.Controller.cfg_timeBetweenBrews.Value.ToString(), "Brew Recharge (sec)", 
                    CaffeineAddict.Controller.cfg_timeBetweenBrews.DefaultValue.ToString(), TMP_InputField.ContentType.DecimalNumber, _inputSwitcher, grtrEqualZeroTemplate), CaffeineAddict.Controller.cfg_timeBetweenBrews);
                _numberInputs.Add(UILibrary.MakeTextInput(engineOutPressureIncRate, EngineOut.Controller.cfg_pressureIncRate.Value.ToString(), "Pressure Gain (%/sec)", 
                    EngineOut.Controller.cfg_pressureIncRate.DefaultValue.ToString(), TMP_InputField.ContentType.DecimalNumber, _inputSwitcher, grtrEqualZeroTemplate), EngineOut.Controller.cfg_pressureIncRate);
                _numberInputs.Add(UILibrary.MakeTextInput(engineOutPressureDecRate, EngineOut.Controller.cfg_pressureDecRatePerValve.Value.ToString(), "Pressure Drain (%/sec/valve)", 
                    EngineOut.Controller.cfg_pressureDecRatePerValve.DefaultValue.ToString(), TMP_InputField.ContentType.DecimalNumber, _inputSwitcher, grtrEqualZeroTemplate), EngineOut.Controller.cfg_pressureDecRatePerValve);
                _numberInputs.Add(UILibrary.MakeTextInput(engineOutPressureTrickleRate, EngineOut.Controller.cfg_engineTrickleRate.Value.ToString(), "Pwr Off Trickle (%/sec)", 
                    EngineOut.Controller.cfg_engineTrickleRate.DefaultValue.ToString(), TMP_InputField.ContentType.DecimalNumber, _inputSwitcher, grtrEqualZeroTemplate), EngineOut.Controller.cfg_engineTrickleRate);

                // Idrc about templatizing the buttons lol
                // Repurpose the settings "Reset" button to apply Bonfire settings.
                GameObject resetButtonObj = clipboardClone.transform.Find("Canvas/Settings menu/Settings/ContentCtn/ButtonSecondaryUGUI (reset all)").gameObject;
                _resetButton = resetButtonObj.GetComponent<Button>();
                for (int i = 0; i < _resetButton.onClick.GetPersistentEventCount(); i++)
                {
                    _resetButton.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
                }
                _resetButton.onClick.AddListener((UnityAction)ResetBonfireSettings);

                // Repurpose the settings "Apply" button to apply Bonfire settings.
                GameObject applyButtonObj = clipboardClone.transform.Find("Canvas/Settings menu/Settings/ContentCtn/SGButtonPrimaryUGUI (apply)").gameObject;
                _applyButton = applyButtonObj.GetComponent<Button>();
                for (int i = 0; i < _applyButton.onClick.GetPersistentEventCount(); i++)
                {
                    _applyButton.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
                }
                _applyButton.onClick.AddListener((UnityAction)ApplyBonfireSettings);
                
                // Make sure "Close" works properly
                GameObject closeButtonObj = clipboardClone.transform.Find("Canvas/Settings menu/Settings/ContentCtn/ButtonSecondaryUGUI (close)").gameObject;
                _closeButton = closeButtonObj.GetComponent<Button>();
                for (int i = 0; i < _closeButton.onClick.GetPersistentEventCount(); i++)
                {
                    _closeButton.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
                }

                _clipboard = clipboardClone;
                
                PickUpZoomTarget zoomTarget = _clipboard.GetComponentInChildren<PickUpZoomTarget>();
                _closeButton.onClick.AddListener((UnityAction)zoomTarget.Release);
                zoomTarget.onReleased.AddListener((UnityAction)Hide);
            }

            // Ensure all settings are reset when reset button is pressed
            private static void ResetBonfireSettings()
            {
                Plugin.Log("Resetting Settings");
                Plugin.Log("Toggles", true);
                foreach((ToggleUGUI toggle, MelonPreferences_Entry<bool> entry) in _toggles)
                {
                    toggle.Value = entry.DefaultValue;
                    Plugin.Log($"  {toggle.name}: {toggle.Value}", true);
                }
                
                Plugin.Log("Float Sliders", true);
                foreach ((SliderUGUI slider, MelonPreferences_Entry<float> entry) in _sliderFloats)
                {
                    slider.Value = entry.DefaultValue;
                    Plugin.Log($"  {slider.name}: {slider.Value}", true);
                }
                
                Plugin.Log("Int Sliders", true);
                foreach ((SliderUGUI slider, MelonPreferences_Entry<int> entry) in _sliderInts)
                {
                    slider.Value = entry.DefaultValue;
                    Plugin.Log($"  {slider.name}: {slider.Value}", true);
                }

                Plugin.Log("Input Boxes", true);
                foreach((TMP_InputField inputField, MelonPreferences_Entry<float> entry) in _numberInputs)
                {
                    inputField.SetText(entry.DefaultValue.ToString());
                    Plugin.Log($"  {inputField.name}: {inputField.text}", true);
                }
            }

            // Ensure all settings are applied when apply button is pressed
            private static void ApplyBonfireSettings()
            {
                Plugin.Log("Applying Settings");
                Plugin.Log("Toggles", true);
                foreach((ToggleUGUI toggle, MelonPreferences_Entry<bool> entry) in _toggles)
                {
                    entry.Value = toggle.Value;
                    Plugin.Log($"  {toggle.name}: {toggle.Value}", true);
                }
                
                Plugin.Log("Slider Floats", true);
                foreach((SliderUGUI slider, MelonPreferences_Entry<float> entry) in _sliderFloats)
                {
                    entry.Value = slider.Value;
                    Plugin.Log($"  {slider.name}: {slider.Value}", true);
                }
                
                Plugin.Log("Slider Ints", true);
                foreach((SliderUGUI slider, MelonPreferences_Entry<int> entry) in _sliderInts)
                {
                    entry.Value = (int)Math.Round(slider.Value);
                    Plugin.Log($"  {slider.name}: {slider.Value}", true);
                }

                Plugin.Log("Input Boxes", true);
                foreach((TMP_InputField inputField, MelonPreferences_Entry<float> entry) in _numberInputs)
                {
                    if (float.TryParse(inputField.text, out float result))
                    {
                        entry.Value = result;
                        Plugin.Log($"  {inputField.name}: {inputField.text}", true);
                    }
                    else
                    {
                        Plugin.Log($"  Could not load input: {inputField.name}: {inputField.text}");
                    }
                }

                MelonPreferences.Save();
            }

            public static void Hide()
            {
                // Place clipboard down
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
                    zoomTarget.Release();
                    return;
                }
                
                // Reset all clipboard settings to current config values on pickup
                foreach((ToggleUGUI toggle, MelonPreferences_Entry<bool> entry) in _toggles)
                {
                    toggle.Value = entry.Value;
                }
                foreach((SliderUGUI slider, MelonPreferences_Entry<float> entry) in _sliderFloats)
                {
                    slider.Value = entry.Value;
                }
                foreach((SliderUGUI slider, MelonPreferences_Entry<int> entry) in _sliderInts)  
                {
                    slider.Value = entry.Value;
                }
                foreach((TMP_InputField inputField, MelonPreferences_Entry<float> entry) in _numberInputs)
                {
                    inputField.text = entry.Value.ToString();
                }
                zoomTarget.PickUp();
            }
        }
    }
}