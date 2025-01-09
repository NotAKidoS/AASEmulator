#if CVR_CCK_EXISTS
using ABI.CCK.Scripts;
using NAK.AASEmulator.Runtime.SubSystems;
using System.Collections.Generic;
using UnityEngine;
using static ABI.CCK.Scripts.CVRAdvancedSettingsEntry;

namespace NAK.AASEmulator.Runtime
{
    [AddComponentMenu("")]
    [HelpURL(AASEmulatorCore.AAS_EMULATOR_GIT_URL)]
    public class AASMenu : EditorOnlyMonoBehaviour
    {
        #region Static Initialization

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            AASEmulatorCore.runtimeInitializedDelegate -= OnRuntimeInitialized; // unsub from last play mode session
            AASEmulatorCore.runtimeInitializedDelegate += OnRuntimeInitialized;
        }

        private static void OnRuntimeInitialized(AASEmulatorRuntime runtime)
        {
            if (AASEmulatorCore.Instance != null
                && !AASEmulatorCore.Instance.EmulateAASMenu)
                return;

            AASMenu menu = runtime.gameObject.AddComponent<AASMenu>();
            menu.isInitializedExternally = true;
            menu.runtime = runtime;
            AASEmulatorCore.addTopComponentDelegate?.Invoke(menu);
        }

        #endregion Static Initialization

        #region EditorGUI

        public bool aasProfilesFoldout;

        #endregion EditorGUI

        #region Variables

        public readonly List<AASMenuEntry> entries = new();
        public AASEmulatorRuntime runtime { get; private set; }
        public AvatarAnimator AnimatorManager => runtime.AnimatorManager;
        public AvatarProfileManager profilesManager;

        #endregion Variables

        #region Menu Setup

        private void Start()
        {
            SetupAASMenus();
            InitializeProfiles();
        }

        private void SetupAASMenus()
        {
            entries.Clear();

            if (runtime == null || runtime.m_avatar == null || runtime.m_avatar.avatarSettings?.settings == null)
            {
                SimpleLogger.LogError("Unable to setup AAS Menus: Missing required components", this);
                return;
            }

            var avatarSettings = runtime.m_avatar.avatarSettings.settings;

            foreach (CVRAdvancedSettingsEntry aasEntry in avatarSettings)
            {
                string[] postfixes;
                switch (aasEntry.type)
                {
                    case SettingsType.Joystick2D:
                    case SettingsType.InputVector2:
                        postfixes = new[] { "-x", "-y" };
                        break;
                    case SettingsType.Joystick3D:
                    case SettingsType.InputVector3:
                        postfixes = new[] { "-x", "-y", "-z" };
                        break;
                    case SettingsType.Color:
                        postfixes = new[] { "-r", "-g", "-b" };
                        break;
                    default:
                        postfixes = new[] { "" };
                        break;
                }

                AASMenuEntry menuEntry = new()
                {
                    aasEntry = aasEntry
                };

                if (aasEntry.setting is CVRAdvancesAvatarSettingGameObjectDropdown dropdown)
                    menuEntry.menuOptions = dropdown.optionNames;

                for (int i = 0; i < postfixes.Length; i++)
                {
                    // Accurate to game, by default it will load what is in controler vs what is the default in the AAS entries
                    // I believe this to be a game bug but others say it is intended (shitty behavior)
                    // https://github.com/NotAKidoS/NAK_CVR_Mods/blob/main/AASDefaultProfileFix/Main.cs
                    if (AnimatorManager.Parameters.GetParameter(aasEntry.machineName + postfixes[i], out float value))
                    {
                        switch (i)
                        {
                            case 0:
                                menuEntry.valueX = value;
                                break;
                            case 1:
                                menuEntry.valueY = value;
                                break;
                            case 2:
                                menuEntry.valueZ = value;
                                break;
                        }
                    }
                }

                entries.Add(menuEntry);
            }

            SimpleLogger.Log($"Successfully created {entries.Count} menu entries for {runtime.m_avatar.name}!", gameObject);
        }

        public void InitializeProfiles()
        {
            profilesManager = new AvatarProfileManager(runtime.AvatarGuid, runtime.AnimatorManager.Parameters, entries);
            profilesManager.SetupManager();
        }

        #endregion Menu Setup

        #region Menu Entry Class

        public class AASMenuEntry
        {
            public string menuName => aasEntry.name;
            public string machineName => aasEntry.machineName;
            public SettingsType settingType => aasEntry.type;
            
            public float valueX, valueY, valueZ;
            public string[] menuOptions;
            public CVRAdvancedSettingsEntry aasEntry;
            
            public (float def, float d, float a) defaultValue
            {
                get
                {
                    return aasEntry.setting switch
                    {
                        CVRAdvancesAvatarSettingGameObjectToggle targetSetting => (targetSetting.defaultValue ? 1f : 0f, 0f, 0f),
                        CVRAdvancesAvatarSettingGameObjectDropdown dropdownSetting => (dropdownSetting.defaultValue, 0f, 0f),
                        CVRAdvancesAvatarSettingSlider sliderSetting => (sliderSetting.defaultValue, 0f, 0f),
                        CVRAdvancesAvatarSettingJoystick2D joystick2DSetting => (joystick2DSetting.defaultValue.x, joystick2DSetting.defaultValue.y, 0f),
                        CVRAdvancesAvatarSettingJoystick3D joystick3DSetting => (joystick3DSetting.defaultValue.x, joystick3DSetting.defaultValue.y, joystick3DSetting.defaultValue.z),
                        CVRAdvancesAvatarSettingInputSingle inputSingleSetting => (inputSingleSetting.defaultValue, 0f, 0f),
                        CVRAdvancesAvatarSettingInputVector2 inputVector2Setting => (inputVector2Setting.defaultValue.x, inputVector2Setting.defaultValue.y, 0f),
                        CVRAdvancesAvatarSettingInputVector3 inputVector3Setting => (inputVector3Setting.defaultValue.x, inputVector3Setting.defaultValue.y, inputVector3Setting.defaultValue.z),
                        CVRAdvancedAvatarSettingMaterialColor materialColorSetting => (materialColorSetting.defaultValue.r, materialColorSetting.defaultValue.g, materialColorSetting.defaultValue.b),
                        _ => (0f, 0f, 0f)
                    };
                }
            } 
        }

        #endregion Menu Entry Class
    }
}
#endif