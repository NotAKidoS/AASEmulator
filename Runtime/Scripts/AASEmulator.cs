﻿using ABI.CCK.Components;
using System.Collections.Generic;
using System.Linq;
using NAK.AASEmulator.Runtime.Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NAK.AASEmulator.Runtime
{
    public class AASEmulator : MonoBehaviour
    {
        #region Support Delegate

        public delegate void AddComponent(Component runtime);
        public static AddComponent addComponentDelegate;

        #endregion
        
        public static AASEmulator Instance;

        private readonly List<AASEmulatorRuntime> m_runtimes = new List<AASEmulatorRuntime>();
        //private readonly HashSet<CVRAvatar> m_scannedAvatars = new HashSet<CVRAvatar>();

        #region Unity Methods
        
        private void Awake()
        {
            if (Instance != null)
            {
                DestroyImmediate(this);
                return;
            }

            Instance = this;

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
                m_runtimes.Add(runtime);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ScanForAvatars(scene);

        #endregion Private Methods
    }
}