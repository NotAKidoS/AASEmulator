using System.Collections.Generic;
using ABI.CCK.Scripts;
using NAK.AASEmulator.Runtime.SubSystems;
using UnityEngine;
using static ABI.CCK.Scripts.CVRAdvancedSettingsEntry;

namespace NAK.AASEmulator.Runtime
{
    public class AASMenu : MonoBehaviour
    {
        #region Static Initialization

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            AASEmulator.runtimeInitializedDelegate = runtime =>
            {
                AASMenu menu = runtime.gameObject.AddComponent<AASMenu>();
                menu.runtime = runtime;
                AASEmulator.addTopComponentDelegate?.Invoke(menu);
            };
        }
        
        #endregion

        #region Variables

        public List<AASMenuEntry> entries = new List<AASMenuEntry>();
        private AASEmulatorRuntime runtime;

        #endregion

        #region Menu Setup

        private void Start() => SetupAASMenus();
        
        private void SetupAASMenus()
        {
            entries.Clear();

            if (runtime == null)
            {
                SimpleLogger.LogError("Unable to setup AAS Menus: AASEmulatorRuntime is missing", this);
                return;
            }

            if (runtime.m_avatar == null)
            {
                SimpleLogger.LogError("Unable to setup AAS Menus: CVRAvatar is missing", this);
                return;
            }

            if (runtime.m_avatar.avatarSettings?.settings == null)
            {
                SimpleLogger.LogError("Unable to setup AAS Menus: AvatarAdvancedSettings is missing", this);
                return;
            }

            var avatarSettings = runtime.m_avatar.avatarSettings.settings;

            foreach (CVRAdvancedSettingsEntry setting in avatarSettings)
            {
                AASMenuEntry menuEntry = new AASMenuEntry()
                {
                    menuName = setting.name,
                    settingType = setting.type
                };

                if (runtime.AnimatorManager.Parameters.TryGetValue(setting.name, out AnimatorManager.BaseParam param))
                    menuEntry.baseParam = param;

                entries.Add(menuEntry);
            }

            SimpleLogger.Log($"Successfully created {entries.Count} menu entries for {runtime.m_avatar.name}!", this);
        }

        #endregion

        #region Menu Entry Class
        
        public class AASMenuEntry
        {
            public string menuName;
            public SettingsType settingType;
            public AnimatorManager.BaseParam baseParam;
        }

        #endregion
    }
}