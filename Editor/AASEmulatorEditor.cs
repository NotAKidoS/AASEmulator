using System;
using System.Collections;
using NAK.AASEmulator.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace NAK.AASEmulator.Editor
{
    [CustomEditor(typeof(AASEmulatorCore))]
    public class AASEmulatorCoreEditor : UnityEditor.Editor
    {
        #region Private Variables

        private AASEmulatorCore _core;

        #endregion

        #region Serialized Properties
        
        private SerializedProperty m_onlyInitializeOnSelect;
        private SerializedProperty m_emulateAASMenu;
        private SerializedProperty m_defaultRuntimeController;

        #endregion

        #region Unity / GUI Methods

        private void OnEnable()
        {
            if (target == null) return;
            _core = (AASEmulatorCore)target;
            
            // version check state this session
            _hasCheckedForUpdates = SessionState.GetBool(CHECKED_VERSION_THIS_SESSION_KEY, false);
            _isLatestVersion = SessionState.GetBool(LATEST_VERSION_KEY, false);
            
            // serialized properties
            m_onlyInitializeOnSelect = serializedObject.FindProperty(nameof(AASEmulatorCore.OnlyInitializeOnSelect));
            m_emulateAASMenu = serializedObject.FindProperty(nameof(AASEmulatorCore.EmulateAASMenu));
            m_defaultRuntimeController = serializedObject.FindProperty(nameof(AASEmulatorCore.defaultRuntimeController));
        }
        
        public override void OnInspectorGUI()
        {
            if (_core == null)
                return;
            
            serializedObject.Update();
            
            Draw_Settings();
            EditorGUILayout.Space();
            
            Draw_Links();
            EditorGUILayout.Space();
            
            Draw_VersionCheck();
            
            serializedObject.ApplyModifiedProperties();
        }

        #endregion Unity / GUI Methods
        
        #region Drawing Methods
        
        private void Draw_Settings()
        {
            EditorGUILayout.LabelField("Emulator Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(m_onlyInitializeOnSelect);
            EditorGUILayout.PropertyField(m_emulateAASMenu);
            EditorGUILayout.PropertyField(m_defaultRuntimeController);
        }
        
        private void Draw_Links()
        {
            EditorGUILayout.LabelField("Links / Community", EditorStyles.boldLabel);
            if (GUILayout.Button("Common Creator FAQ")) Application.OpenURL(AASEmulatorCore.AAS_EMULATOR_COMMON_FAQ_URL);
            if (GUILayout.Button("AAS Emulator GitHub")) Application.OpenURL(AASEmulatorCore.AAS_EMULATOR_GIT_URL);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Links / Official", EditorStyles.boldLabel);
            if (GUILayout.Button("CCK Documentation")) Application.OpenURL(AASEmulatorCore.ABI_CCK_DOCUMENTATION_URL);
            if (GUILayout.Button("Alpha Blend Interactive Hub")) Application.OpenURL(AASEmulatorCore.ABI_HUB_URL);
        }

        private void Draw_VersionCheck()
        {
            EditorGUILayout.LabelField("Version Check", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Current Version: " + AASEmulatorCore.AAS_EMULATOR_VERSION);

            if (_isAttemptingVersionCheck)
            {
                EditorGUILayout.HelpBox("Checking for updates...", MessageType.Info);
                return;
            }
            
            if (_hasCheckedForUpdates)
            {
                if (_isLatestVersion)
                {
                    EditorGUILayout.HelpBox("You are using the latest version of AAS Emulator.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("A new version of the AAS Emulator is available.", MessageType.Warning);
                    if (GUILayout.Button("Open Latest Release Page")) Application.OpenURL(AASEmulatorCore.AAS_EMULATOR_GIT_URL + "/releases/latest");
                }
            }
            else if (GUILayout.Button("Check for Updates"))
            {
                CheckForUpdates();
            }
        }
        
        #endregion Drawing Methods
        
        #region Version Check Implementation
        
        private static Coroutine _versionCheckCoroutine;
        
        private static bool _isLatestVersion;
        private static bool _isAttemptingVersionCheck;
        private static bool _hasCheckedForUpdates;
        
        private const string CHECKED_VERSION_THIS_SESSION_KEY = nameof(AASEmulatorCore) + "_CheckedVersionThisSession";
        private const string LATEST_VERSION_KEY = nameof(AASEmulatorCore) + "_LatestVersion";

        [Serializable]
        private class GitHubRelease
        {
            public string tag_name;
        }

        private void CheckForUpdates()
        {
            // mono behaviour owns the coroutine (this works in-editor surprisingly)
            if (_versionCheckCoroutine != null) _core.StopCoroutine(_versionCheckCoroutine);
            _versionCheckCoroutine = _core.StartCoroutine(CheckForUpdatesCoroutine()); 
            _hasCheckedForUpdates = true;
        }
        
        private IEnumerator CheckForUpdatesCoroutine()
        {
            _isAttemptingVersionCheck = true;
            
            using UnityWebRequest webRequest = UnityWebRequest.Get(AASEmulatorCore.AAS_EMULATOR_GIT_API_RELEASE_URL);
            
            yield return webRequest.SendWebRequest();

            if (webRequest.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
            {
                SimpleLogger.LogError("Error fetching latest version from GitHub: " + webRequest.error, _core.gameObject);
            }
            else
            {
                GitHubRelease latestVersionInfo = JsonUtility.FromJson<GitHubRelease>(webRequest.downloadHandler.text);
                var latestVersion = latestVersionInfo.tag_name.TrimStart('v'); // remove 'v' prefix
                
                bool isCurrentVersionValid = Version.TryParse(AASEmulatorCore.AAS_EMULATOR_VERSION, out Version currentVersion);
                bool isNewVersionValid = Version.TryParse(latestVersion, out Version newVersion);

                if (isCurrentVersionValid && isNewVersionValid)
                {
                    _isLatestVersion = newVersion <= currentVersion;
                    SimpleLogger.Log("Successfully fetched latest version from GitHub: " + latestVersion, _core.gameObject);
                }
                else
                {
                    SimpleLogger.LogError("Invalid version format. Current Version: " + AASEmulatorCore.AAS_EMULATOR_VERSION + ", New Version: " + latestVersion, _core.gameObject);
                }
            }
            
            _versionCheckCoroutine = null;
            _isAttemptingVersionCheck = false;
            SessionState.SetBool(CHECKED_VERSION_THIS_SESSION_KEY, true);
            SessionState.SetBool(LATEST_VERSION_KEY, _isLatestVersion);
        }

        #endregion Version Check Implementation
    }
}