#if UNITY_EDITOR && CVR_CCK_EXISTS
using System;
using NAK.AASEmulator.Runtime;
using NAK.AASEmulator.Runtime.SubSystems;
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.CVRAdvancedSettingsEntry;
using static NAK.AASEmulator.Runtime.AASEmulatorRuntime;

namespace NAK.AASEmulator.Editor
{
    [CustomEditor(typeof(AASMenu))]
    public class AASMenuEditor : UnityEditor.Editor
    {
        #region Private Variables

        private AASMenu _menu;
        private Vector2 _scrollPosition;
        private bool showCreateProfileField;
        private string newProfileName = "";

        #endregion Private Variables

        #region Unity Events

        private void OnEnable()
        {
            OnRequestRepaint -= Repaint;
            OnRequestRepaint += Repaint;
            _menu = (AASMenu)target;
        }

        public override void OnInspectorGUI()
        {
            if (_menu == null)
                return;

            Draw_ScriptWarning();

            Draw_AASProfiles();
            Draw_AASMenus();
        }

        #endregion Unity Events

        #region Drawing Methods

        private void Draw_ScriptWarning()
        {
            if (_menu.isInitializedExternally)
                return;

            EditorGUILayout.HelpBox("Warning: Do not upload this script with your avatar!\nThis script is prevented from saving to scenes & prefabs.", MessageType.Warning);
            EditorGUILayout.HelpBox("This script will automatically be added if you enable AASEmulator from the Tools menu (Tools > Enable AAS Emulator).", MessageType.Info);
        }

        private void Draw_AASProfiles()
        {
            _menu.aasProfilesFoldout = EditorGUILayout.Foldout(_menu.aasProfilesFoldout, "AAS Profiles", true);
            if (!_menu.aasProfilesFoldout)
                return;

            GUILayout.BeginVertical("box");

            // Use In-Game Profiles Toggle
            bool newUseInGameProfiles = EditorGUILayout.Toggle("Use In-Game Profiles", AvatarProfileManager.UseClientProfiles);
            if (newUseInGameProfiles != AvatarProfileManager.UseClientProfiles)
            {
                if (EditorUtility.DisplayDialog("Switch Profile Source", 
                        "Switching profile source will reload the default profile. Continue?", 
                        "Yes", "No"))
                {
                    AvatarProfileManager.UseClientProfiles = newUseInGameProfiles;
                    _menu.profilesManager.SetupManager();
                }
            }

            var profilesManager = _menu.profilesManager;

            if (profilesManager.ProfileNames.Count > 0)
            {
                int selectedIndex = profilesManager.ProfileNames.IndexOf(profilesManager.SelectedProfileName);
                if (selectedIndex < 0)
                    selectedIndex = 0;

                int newSelectedIndex = EditorGUILayout.Popup("Selected Profile", selectedIndex, profilesManager.ProfileNames.ToArray());
                if (newSelectedIndex != selectedIndex) profilesManager.LoadProfile(profilesManager.ProfileNames[newSelectedIndex]);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Save Profile"))
                {
                    if (EditorUtility.DisplayDialog("Save Profile", 
                            $"Are you sure you want to save profile '{profilesManager.SelectedProfileName}'?", 
                            "Yes", "No"))
                        profilesManager.SaveProfile(profilesManager.SelectedProfileName);
                }

                if (GUILayout.Button("Delete Profile"))
                {
                    if (EditorUtility.DisplayDialog("Delete Profile", 
                            $"Are you sure you want to delete profile '{profilesManager.SelectedProfileName}'?", 
                            "Yes", "No"))
                        profilesManager.DeleteProfile(profilesManager.SelectedProfileName);
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("No profiles available.");
            }

            if (showCreateProfileField)
            {
                newProfileName = EditorGUILayout.TextField("Profile Name", newProfileName);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Create"))
                {
                    if (string.IsNullOrEmpty(newProfileName))
                    {
                        EditorUtility.DisplayDialog("Error", "Profile name cannot be empty.", "OK");
                    }
                    else
                    {
                        if (profilesManager.Profiles.ContainsKey(newProfileName))
                        {
                            EditorUtility.DisplayDialog("Error", $"Profile '{newProfileName}' already exists.", "OK");
                        }
                        else
                        {
                            profilesManager.CreateProfile(newProfileName);
                            showCreateProfileField = false;
                            newProfileName = "";
                        }
                    }
                }
                if (GUILayout.Button("Cancel"))
                {
                    showCreateProfileField = false;
                    newProfileName = "";
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                if (GUILayout.Button("Create New Profile"))
                {
                    showCreateProfileField = true;
                    newProfileName = "";
                }
            }

            GUILayout.EndVertical();
        }

        private void Draw_AASMenus()
        {
            int entriesCount = _menu.entries.Count;
            if (entriesCount == 0)
            {
                EditorGUILayout.HelpBox("No menu entries found for this avatar.", MessageType.Info);
                return;
            }

            int height = Mathf.Min(entriesCount * 100, 600);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition,
                false, false, GUILayout.Height(height));

            foreach (AASMenu.AASMenuEntry t in _menu.entries)
                DisplayMenuEntry(t);

            EditorGUILayout.EndScrollView();
        }

        private void DisplayMenuEntry(AASMenu.AASMenuEntry entry)
        {
            GUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Menu Name", entry.menuName);
            EditorGUILayout.LabelField("Machine Name", entry.machineName);
            EditorGUILayout.LabelField("Settings Type", entry.settingType.ToString());

            switch (entry.settingType)
            {
                case SettingsType.Dropdown:
                    int oldIndex = (int)entry.valueX;
                    int newIndex = EditorGUILayout.Popup("Value", oldIndex, entry.menuOptions);
                    if (newIndex != oldIndex)
                    {
                        _menu.AnimatorManager.Parameters.SetParameter(entry.machineName, entry.valueX = newIndex);
                    }
                    break;
                case SettingsType.Toggle:
                    bool oldValue = entry.valueX >= 0.5f;
                    bool newValue = EditorGUILayout.Toggle("Value", oldValue);
                    if (newValue != oldValue)
                    {
                        _menu.AnimatorManager.Parameters.SetParameter(entry.machineName, entry.valueX = newValue ? 1f : 0f);
                    }
                    break;
                case SettingsType.Slider:
                    float oldSliderValue = entry.valueX;
                    float newSliderValue = EditorGUILayout.Slider("Value", oldSliderValue, 0f, 1f);
                    if (Math.Abs(newSliderValue - oldSliderValue) > float.Epsilon)
                    {
                        _menu.AnimatorManager.Parameters.SetParameter(entry.machineName, entry.valueX = newSliderValue);
                    }
                    break;
                case SettingsType.InputSingle:
                    float oldSingleValue = entry.valueX;
                    float newSingleValue = EditorGUILayout.FloatField("Value", oldSingleValue);
                    if (Math.Abs(newSingleValue - oldSingleValue) > float.Epsilon)
                    {
                        _menu.AnimatorManager.Parameters.SetParameter(entry.machineName, entry.valueX = newSingleValue);
                    }
                    break;
                case SettingsType.InputVector2:
                    Vector2 oldVector2Value = new(entry.valueX, entry.valueY);
                    Vector2 newVector2Value = EditorGUILayout.Vector2Field("Value", oldVector2Value);
                    if (newVector2Value != oldVector2Value)
                    {
                        _menu.AnimatorManager.Parameters.SetParameter(entry.machineName + "-x", entry.valueX = newVector2Value.x);
                        _menu.AnimatorManager.Parameters.SetParameter(entry.machineName + "-y", entry.valueY = newVector2Value.y);
                    }
                    break;
                case SettingsType.InputVector3:
                    Vector3 oldVector3Value = new(entry.valueX, entry.valueY, entry.valueZ);
                    Vector3 newVector3Value = EditorGUILayout.Vector3Field("Value", oldVector3Value);
                    if (newVector3Value != oldVector3Value)
                    {
                        _menu.AnimatorManager.Parameters.SetParameter(entry.machineName + "-x", entry.valueX = newVector3Value.x);
                        _menu.AnimatorManager.Parameters.SetParameter(entry.machineName + "-y", entry.valueY = newVector3Value.y);
                        _menu.AnimatorManager.Parameters.SetParameter(entry.machineName + "-z", entry.valueZ = newVector3Value.z);
                    }
                    break;
            }

            GUILayout.EndVertical();
        }

        #endregion
    }
}
#endif