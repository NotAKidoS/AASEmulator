/* Copyright (c) 2020-2022 Lyuma <xn.lyuma@gmail.com>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */

// https://github.com/lyuma/Av3Emulator/blob/master/Editor/LyumaAv3EditorSupport.cs

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
