using ABI.CCK.Components;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NAK.AASEmulator.Runtime
{
    public class AASEmulator : MonoBehaviour
    {
        public static AASEmulator Instance;

        [Header("Global Settings")]
        public bool EnableEmulator = true;
        bool _enableEmulator;

        public List<AASEmulatorRuntime> runtimes = new List<AASEmulatorRuntime>();
        public HashSet<CVRAvatar> m_scannedAvatars = new HashSet<CVRAvatar>();

        void Awake()
        {
            Instance = this;
            OnEnabledChanged();
        }

        void Update()
        {
            if (_enableEmulator != EnableEmulator)
            {
                OnEnabledChanged();
            }
        }

        void OnEnabledChanged()
        {
            if (EnableEmulator)
            {
                OnEmulatorEnabled();

            }
            else
            {
                OnEmulatorDisabled();
            }
            _enableEmulator = EnableEmulator;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ScanForAvatars(scene);
        }

        void ScanForAvatars(Scene scene)
        {
            var targetScene = scene;
            CVRAvatar[] avatars = targetScene.GetRootGameObjects()
                .SelectMany(x => x.GetComponentsInChildren<CVRAvatar>(true)).ToArray();
            m_scannedAvatars.UnionWith(avatars);

            Debug.Log(this.name + ": Setting up AASEmulator on " + avatars.Length + " avatars.", this);

            foreach (var avatar in avatars)
            {
                AASEmulatorRuntime runtime = avatar.GetComponent<AASEmulatorRuntime>();
                if (runtime == null)
                {
                    runtime = avatar.gameObject.AddComponent<AASEmulatorRuntime>();
                    runtimes.Add(runtime);
                }
            }
        }

        void OnEmulatorEnabled()
        {
            ScanForAvatars(gameObject.scene);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnEmulatorDisabled()
        {
            foreach (var runtime in runtimes)
            {
                Destroy(runtime);
            }
            runtimes.Clear();
            m_scannedAvatars.Clear();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnDestroy()
        {
            OnEmulatorDisabled();
        }
    }
}