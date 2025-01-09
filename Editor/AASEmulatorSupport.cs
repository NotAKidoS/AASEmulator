#if UNITY_EDITOR && CVR_CCK_EXISTS
using System.Linq;
using NAK.AASEmulator.Runtime;
using NAK.AASEmulator.Runtime.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NAK.AASEmulator.Support
{
    [InitializeOnLoad]
    public static class AASEmulatorSupport
    {
        private const string AAS_EMULATOR_CONTROL_NAME = "AAS Emulator Control";
        
        static AASEmulatorSupport()
        {
            Initialize();
        }
        
        private static void Initialize()
        {
            //AASEmulatorCore.OnCoreInitialized -= MoveComponentToTop;
            AASEmulatorCore.addTopComponentDelegate -= MoveComponentToTop;
            AASEmulatorCore.addTopComponentDelegate += MoveComponentToTop;
        }

        private static void MoveComponentToTop(Component c)
        {
            GameObject go = c.gameObject;
            Component[] components = go.GetComponents<Component>();
            
            try
            {
                if (PrefabUtility.IsPartOfAnyPrefab(go))
                    PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
            catch (System.Exception)
            {
                // ignored
            }

            if (PrefabUtility.IsPartOfAnyPrefab(components[1])) 
                return;
            
            int moveUpCalls = components.Length - 2;
            for (int i = 0; i < moveUpCalls; i++)
                UnityEditorInternal.ComponentUtility.MoveComponentUp(c);
        }

        [MenuItem("Tools/Enable AAS Emulator")]
        public static void EnableAASTesting()
        {
            AASEmulatorCore control = AASEmulatorCore.Instance;
            if (control == null)
            {
                GameObject foundSceneControl = SceneManager.GetActiveScene()
                    .GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .FirstOrDefault(t => t.name == AAS_EMULATOR_CONTROL_NAME)?.gameObject;

                control = foundSceneControl == null
                    ? new GameObject(AAS_EMULATOR_CONTROL_NAME).AddComponent<AASEmulatorCore>() 
                    : foundSceneControl.AddComponentIfMissing<AASEmulatorCore>();
            }
            
            control.enabled = true;
            
            GameObject gameObject = control.gameObject;
            gameObject.SetActive(true);
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
            Selection.SetActiveObjectWithContext(gameObject, gameObject);
            EditorGUIUtility.PingObject(gameObject);
        }
        
        // internal static T AddComponentIfMissing<T>(this Component component) where T : Component
        // {
        //     return component.gameObject.AddComponentIfMissing<T>();
        // }
    }
}
#endif