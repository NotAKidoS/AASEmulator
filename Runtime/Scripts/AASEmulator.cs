using ABI.CCK.Components;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NAK.AASEmulator.Runtime
{
    public class AASEmulator : MonoBehaviour
    {
        #region Support Delegates

        public delegate void AddTopComponent(Component component);

        public static AddTopComponent addTopComponentDelegate;

        public delegate void RuntimeInitialized(AASEmulatorRuntime runtime);

        public static RuntimeInitialized runtimeInitializedDelegate;

        #endregion Support Delegates

        public static AASEmulator Instance;
        private readonly List<AASEmulatorRuntime> m_runtimes = new List<AASEmulatorRuntime>();

        public bool OnlyInitializeOnSelect = false;
        public bool EmulateAASMenu = false;

        [HideInInspector]
        public RuntimeAnimatorController defaultRuntimeController;
        private string controllerGUID = "ff926e022d914b84e8975ba6188a26f0";
        private string controllerPath = "Assets/ABI.CCK/Animations/AvatarAnimator.controller";
        
        #region Unity Methods

        private void Awake()
        {
            if (Instance != null)
            {
                DestroyImmediate(this);
                return;
            }

            Instance = this;

            LoadDefaultCCKController();
            StartEmulator();
        }

        private void OnDestroy()
        {
            StopEmulator();
        }

        #endregion Unity Methods

        #region Public Methods

        public void StartEmulator()
        {
            //ScanForAvatars(gameObject.scene);
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void StopEmulator()
        {
            foreach (AASEmulatorRuntime runtime in m_runtimes)
                Destroy(runtime);

            m_runtimes.Clear();
            //m_scannedAvatars.Clear();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        #endregion Public Methods

        #region Private Methods

        private void LoadDefaultCCKController()
        {
#if UNITY_EDITOR
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(controllerGUID);
            Object controllerObject = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(path) 
                ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(controllerPath);
            
            defaultRuntimeController = controllerObject as RuntimeAnimatorController;
#endif
            if (defaultRuntimeController == null)
                SimpleLogger.LogError("Failed to load default avatar controller. Did you move the ABI.CCK folder?", gameObject);
        }

        private void ScanForAvatars(Scene scene)
        {
            var avatars = scene.GetRootGameObjects()
                .SelectMany(x => x.GetComponentsInChildren<CVRAvatar>(true)).ToArray();
            //m_scannedAvatars.UnionWith(avatars);

            // TODO: Only log new avatar additions
            SimpleLogger.Log("Setting up AASEmulator on " + avatars.Length + " avatars.",
                this);

            foreach (CVRAvatar avatar in avatars)
            {
                AASEmulatorRuntime runtime = avatar.GetComponent<AASEmulatorRuntime>();
                if (runtime != null)
                    continue;
                runtime = avatar.gameObject.AddComponent<AASEmulatorRuntime>();
                runtime.isInitializedExternally = true;
                m_runtimes.Add(runtime);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ScanForAvatars(scene);

        #endregion Private Methods
    }
}