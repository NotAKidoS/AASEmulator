using System;
using System.Collections.Generic;
using System.IO;
using Abi.Newtonsoft.Json;
using UnityEngine.Device;
using static ABI.CCK.Scripts.CVRAdvancedSettingsEntry;

namespace NAK.AASEmulator.Runtime.SubSystems
{
    /// <summary>
    /// The profile management and application of the default profile is very obviously buggy- but sadly this is an enumerator,
    /// so if the client does it, we have to do it too...
    /// </summary>
    public class AvatarProfileManager
    {
        private readonly ParameterAccess parameterAccess;
        private readonly List<AASMenu.AASMenuEntry> menuEntries;
        private readonly string avatarId;
        private AvatarProfiles avatarProfiles = new();

        public AvatarProfileManager(
            string avatarId, 
            ParameterAccess parameterAccess, 
            List<AASMenu.AASMenuEntry> menuEntries)
        {
            // "good enough" handling of null/empty avatarId :)
            this.avatarId = string.IsNullOrEmpty(avatarId) ? "00000000-0000-0000-0000-000000000000" : avatarId;
            this.parameterAccess = parameterAccess;
            this.menuEntries = menuEntries; // only used for default profile
        }

        private const string DEFAULT_PROFILE_NAME = "Default";
        private const string PROFILE_EXTENSION = ".advavtr"; // json
        private const string CLIENT_PROFILES_DIRECTORY = @"ChilloutVR_Data\AvatarsAdvancedSettingsProfiles";
        
        /// <summary>
        /// Whether to use client profiles or not. If true, will load & save profiles to the client profiles folder if provided.
        /// If false, the profiles will be saved to project root (default). Basically just option to set a custom folder path.
        /// </summary>
        public static bool UseClientProfiles
        {
#if UNITY_EDITOR // TODO: this is hack
            get => UnityEditor.EditorPrefs.GetBool("AASEmu-UseClientProfiles", false);
            set
            {
                UnityEditor.EditorPrefs.SetBool("AASEmu-UseClientProfiles", value);
            }
#else
            get => _useClientProfiles;
            set
            {
                _useClientProfiles = value;
                SetupManager();
            }
#endif
        }
        
        public string SelectedProfileName { get; private set; }
        public List<string> ProfileNames { get; } = new();
        public Dictionary<string, ProfileData> Profiles { get; } = new();

        public void SetupManager()
        {
            ResetProfileData();
            LoadProfileDataFromDisk();

            // Client makes no attempt to load the default profile properly...
            // https://github.com/NotAKidoS/NAK_CVR_Mods/blob/379be57b846c7a20b9b66d934cf8ccaa0a2f3691/AASDefaultProfileFix/Main.cs#L21-L22
            if (!string.IsNullOrEmpty(SelectedProfileName) 
                && Profiles.ContainsKey(SelectedProfileName))
                LoadProfile(SelectedProfileName);
        }

        #region Profile Management

        private void ResetProfileData()
        {
            // Clear existing profiles
            ProfileNames.Clear();
            Profiles.Clear();
                
            // Add default profile name
            ProfileNames.Add(DEFAULT_PROFILE_NAME);
            
            // Set empty profile to match client behavior... ^
            SelectedProfileName = string.Empty;
        }

        /// <summary>
        /// Load the default profile values into the parameter access.
        /// This only applies the default values of all menu entries to the controller.
        /// </summary>
        private void LoadDefaultProfile()
        {
            // The game only applies the default menu entry values to the controller when selecting the default profile...
            foreach (AASMenu.AASMenuEntry menuEntry in menuEntries)
            {
                (float x, float y, float z) defaultValue = menuEntry.defaultValue;
                switch (menuEntry.settingType)
                {
                    case SettingsType.Color:
                        parameterAccess.SetParameter(menuEntry.machineName + "-r", defaultValue.x);
                        parameterAccess.SetParameter(menuEntry.machineName + "-g", defaultValue.y);
                        parameterAccess.SetParameter(menuEntry.machineName + "-b", defaultValue.z);
                        break;
                    case SettingsType.InputVector2 or SettingsType.Joystick2D:
                        parameterAccess.SetParameter(menuEntry.machineName + "-x", defaultValue.x);
                        parameterAccess.SetParameter(menuEntry.machineName + "-y", defaultValue.y);
                        break;
                    case SettingsType.InputVector3 or SettingsType.Joystick3D:
                        parameterAccess.SetParameter(menuEntry.machineName + "-x", defaultValue.x);
                        parameterAccess.SetParameter(menuEntry.machineName + "-y", defaultValue.y);
                        parameterAccess.SetParameter(menuEntry.machineName + "-z", defaultValue.z);
                        break;
                    default:
                        parameterAccess.SetParameter(menuEntry.machineName, defaultValue.x);
                        break;
                }
            }
        }

        /// <summary>
        /// Loads a profile by name into the parameter access.
        /// This will apply the profile values to the controller.
        /// </summary>
        /// <param name="profileName"></param>
        public void LoadProfile(string profileName)
        {
            SelectedProfileName = profileName;

            if (profileName == DEFAULT_PROFILE_NAME)
            {
                LoadDefaultProfile(); // Default profile is not saved to disk and is a special case
                UpdateAASMenuEntries();
                return;
            }

            if (!Profiles.TryGetValue(profileName, out ProfileData profileData))
            {
                SimpleLogger.LogError($"Profile '{profileName}' not found.");
                return;
            }

            // Won't error if parameter doesn't exist
            foreach (ProfileValue profileValue in profileData.values)
                parameterAccess.SetParameter(profileValue.name, profileValue.value);
            
            UpdateAASMenuEntries();
        }

        /// <summary>
        /// Save the current controller values to a profile with the given name.
        /// </summary>
        public void SaveProfile(string profileName)
        {
            ProfileData profileData = new()
            {
                profileName = profileName,
                values = new List<ProfileValue>()
            };

            foreach (ParameterDefinition parameter in parameterAccess.GetParameters())
            {
                if (!parameter.CanSaveToProfile) continue;

                ProfileValue profileValue = new()
                {
                    name = parameter.name,
                    value = parameter.GetFloat() // casts internally to float
                };

                profileData.values.Add(profileValue);
            }

            if (Profiles.TryAdd(profileName, profileData))
                ProfileNames.Add(profileName);
            else
                Profiles[profileName] = profileData;

            WriteProfileDataToDisk();
        }

        /// <summary>
        /// Deletes the profile with the given name.
        /// </summary>
        public void DeleteProfile(string profileName)
        {
            if (Profiles.Remove(profileName)) ProfileNames.Remove(profileName);
            WriteProfileDataToDisk();
        }
        
        /// <summary>
        /// Creates a new profile with the given name.
        /// </summary>
        public void CreateProfile(string profileName)
        {
            if (Profiles.ContainsKey(profileName))
            {
                SimpleLogger.LogError($"Profile '{profileName}' already exists.");
                return;
            }

            ProfileData profileData = new()
            {
                profileName = profileName,
                values = new List<ProfileValue>()
            };

            Profiles.Add(profileName, profileData);
            ProfileNames.Add(profileName);
            WriteProfileDataToDisk();
        }

        /// <summary>
        /// Sets the default profile to load when initializing.
        /// </summary>
        public void SetDefaultProfile(string profileName)
        {
            if (Profiles.ContainsKey(profileName))
            {
                SelectedProfileName = profileName;
                WriteProfileDataToDisk();
            }
            else
            {
                SimpleLogger.LogError($"Profile '{profileName}' not found.");
            }
        }
        
        private void UpdateAASMenuEntries()
        {
            // Update the menu entries with the new values from the parameter access
            foreach (AASMenu.AASMenuEntry menuEntry in menuEntries)
            {
                switch (menuEntry.settingType)
                {
                    case SettingsType.Color:
                        parameterAccess.GetParameter(menuEntry.machineName + "-r", out menuEntry.valueX);
                        parameterAccess.GetParameter(menuEntry.machineName + "-g", out menuEntry.valueY);
                        parameterAccess.GetParameter(menuEntry.machineName + "-b", out menuEntry.valueZ);
                        break;
                    case SettingsType.InputVector2 or SettingsType.Joystick2D:
                        parameterAccess.GetParameter(menuEntry.machineName + "-x", out menuEntry.valueX);
                        parameterAccess.GetParameter(menuEntry.machineName + "-y", out menuEntry.valueY);
                        parameterAccess.GetParameter(menuEntry.machineName + "-z", out menuEntry.valueZ);
                        break;
                    case SettingsType.InputVector3 or SettingsType.Joystick3D:
                        parameterAccess.GetParameter(menuEntry.machineName + "-x", out menuEntry.valueX);
                        parameterAccess.GetParameter(menuEntry.machineName + "-y", out menuEntry.valueY);
                        parameterAccess.GetParameter(menuEntry.machineName + "-z", out menuEntry.valueZ);
                        break;
                    default:
                        parameterAccess.GetParameter(menuEntry.machineName, out menuEntry.valueX);
                        break;
                }
            }
        }

        #endregion Profile Management

        #region Profile IO

        /// <summary>
        /// Gets the profile directory based on the setting.
        /// </summary>
        private string GetProfileDirectory()
        {
            if (UseClientProfiles)
            {
                if (AASEmulatorCore.Instance == null)
                {
                    SimpleLogger.LogError("AASEmulatorCore not found.");
                    return null;
                }
                string clientInstallPath = AASEmulatorCore.Instance.ClientInstallPath;
                if (string.IsNullOrEmpty(clientInstallPath))
                {
                    SimpleLogger.LogError("Client install path not found.");
                    return null;
                }
                return Path.Combine(clientInstallPath, CLIENT_PROFILES_DIRECTORY);
            }
            return Application.dataPath + "/../AASEmulator/Profiles";
        }

        /// <summary>
        /// Loads profile data from disk.
        /// </summary>
        private void LoadProfileDataFromDisk()
        {
            string profileDirectory = GetProfileDirectory();
            if (string.IsNullOrEmpty(profileDirectory))
            {
                SimpleLogger.LogError("Profile directory not found.");
                return;
            }
            
            string profileFilePath = Path.Combine(profileDirectory, avatarId + PROFILE_EXTENSION);
            if (!File.Exists(profileFilePath))
            {
                SimpleLogger.Log($"Profile file not found at {profileFilePath}");
                return;
            }

            string json = File.ReadAllText(profileFilePath);

            try
            {
                avatarProfiles = JsonConvert.DeserializeObject<AvatarProfiles>(json);
                if (avatarProfiles == null)
                {
                    SimpleLogger.LogError("Failed to deserialize avatar profiles.");
                    return;
                }

                // Add saved profiles
                foreach (ProfileData profileData in avatarProfiles.savedSettings)
                {
                    Profiles.Add(profileData.profileName, profileData);
                    ProfileNames.Add(profileData.profileName);
                }

                // Set the default profile name
                SelectedProfileName = avatarProfiles.defaultProfileName;
            }
            catch (Exception ex)
            {
                SimpleLogger.LogError($"Failed to load profile data: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies profile changes to disk.
        /// </summary>
        private void WriteProfileDataToDisk()
        {
            string profileDirectory = GetProfileDirectory();
            if (string.IsNullOrEmpty(profileDirectory))
            {
                SimpleLogger.LogError("Profile directory not found. Cannot write profile data to disk.");
                return;
            }
            
            string profileFilePath = Path.Combine(profileDirectory, avatarId + PROFILE_EXTENSION);

            try
            {
                avatarProfiles.avatarid = avatarId;
                avatarProfiles.defaultProfileName = SelectedProfileName;
                avatarProfiles.savedSettings = new List<ProfileData>(Profiles.Values);

                string json = JsonConvert.SerializeObject(avatarProfiles, Formatting.Indented);

                // Ensure directory exists
                Directory.CreateDirectory(profileDirectory);

                File.WriteAllText(profileFilePath, json);
            }
            catch (Exception ex)
            {
                SimpleLogger.LogError($"Failed to write profile data: {ex.Message}");
            }
        }

        #endregion Profile IO

        #region Data Classes

        [Serializable]
        public class AvatarProfiles
        {
            public string avatarid;
            public List<ProfileData> savedSettings;
            public string defaultProfileName;
        }

        [Serializable]
        public class ProfileData
        {
            public string profileName;
            public List<ProfileValue> values = new();
        }

        [Serializable]
        public class ProfileValue
        {
            public string name;
            public float value;
        }

        #endregion Data Classes
    }
}