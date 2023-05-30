using NAK.AASEmulator.Runtime;
using UnityEditor;
using UnityEngine;

namespace NAK.AASEmulator.Support
{
    [InitializeOnLoad]
    public static class AASEmulatorSupport
    {
        // register an event handler when the class is initialized
        static AASEmulatorSupport()
        {
            InitDefaults();
        }

        static void InitDefaults()
        {
            AASEmulatorRuntime.addRuntimeDelegate = (runtime) =>
            {
                MoveComponentToTop(runtime);
            };
        }

        static void MoveComponentToTop(Component c)
        {
            GameObject go = c.gameObject;
            Component[] components = go.GetComponents<Component>();
            try
            {
                if (PrefabUtility.IsPartOfAnyPrefab(go))
                {
                    PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }
            }
            catch (System.Exception) { }
            int moveUpCalls = components.Length - 2;
            if (!PrefabUtility.IsPartOfAnyPrefab(go.GetComponents<Component>()[1]))
            {
                for (int i = 0; i < moveUpCalls; i++)
                {
                    UnityEditorInternal.ComponentUtility.MoveComponentUp(c);
                }
            }
        }

        [MenuItem("Tools/Enable AAS Emulator")]
        public static void EnableAASTesting()
        {
            GameObject go;
            if (Runtime.AASEmulator.Instance == null)
            {
                go = GameObject.Find("/AAS Emulator Control");
                if (go != null)
                {
                    go.SetActive(true);
                }
                else
                {
                    go = new GameObject("AAS Emulator Control");
                }
                AddComponentIfMissing<Runtime.AASEmulator>(go);
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            }
            else
            {
                go = Runtime.AASEmulator.Instance.gameObject;
            }

            Selection.SetActiveObjectWithContext(go, go);
            EditorGUIUtility.PingObject(go);
        }

        public static T AddComponentIfMissing<T>(this GameObject go) where T : Component
        {
            if (go.GetComponent<T>() == null)
            {
                return go.AddComponent<T>();
            }
            return go.GetComponent<T>();
        }

        public static T AddComponentIfMissing<T>(this Component go) where T : Component
        {
            return go.gameObject.AddComponentIfMissing<T>();
        }
    }
}
