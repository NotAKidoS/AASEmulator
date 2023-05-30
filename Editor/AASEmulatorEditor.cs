using NAK.AASEmulator.Runtime;
using System;
using UnityEditor;
using UnityEngine;
using static NAK.AASEmulator.Editor.EditorExtensions;

namespace NAK.AASEmulator.Editor
{
    [CustomEditor(typeof(AASEmulatorRuntime))]
    public class AASEmulatorRuntimeEditor : UnityEditor.Editor
    {
        GUIStyle boldFoldoutStyle;
        AASEmulatorRuntime targetScript;

        void OnEnable()
        {
            AASEmulatorRuntime.OnRequestRepaint -= Repaint;
            AASEmulatorRuntime.OnRequestRepaint += Repaint;
        }
        void OnDisable() => AASEmulatorRuntime.OnRequestRepaint -= Repaint;

        public override void OnInspectorGUI()
        {
            targetScript = (AASEmulatorRuntime)target;
            boldFoldoutStyle = new GUIStyle(EditorStyles.foldout);
            boldFoldoutStyle.fontStyle = FontStyle.Bold;

            DrawAvatarInfo();

            DrawLipSync();
            DrawBuiltInGestures();
            DrawBuiltInLocomotion();
            DrawBuiltInEmotes();

            DrawAnimatorParameters();

            //DrawAdvanced();
        }

        void DrawAvatarInfo()
        {
            EditorGUILayout.Space();
            targetScript.avatarInfoFoldout = EditorGUILayout.Foldout(targetScript.avatarInfoFoldout, "Avatar Info", true, boldFoldoutStyle);

            if (targetScript.avatarInfoFoldout)
            {
                EditorGUI.indentLevel++;

                // Add label to show if an emote is currently playing or not
                string emoteStatus = targetScript.IsEmotePlaying() ? "Playing an Emote - Tracking Disabled" : "Not Playing an Emote - Tracking Enabled";
                EditorGUILayout.LabelField("Emote Status:", emoteStatus);

                // Add label to show the eye movement status
                string eyeMovementStatus = targetScript.IsEyeMovement() ? "Enabled - Eye Look On" : "Disabled - Eye Look Off";
                EditorGUILayout.LabelField("Eye Movement:", eyeMovementStatus);

                // Add label to show the blink blendshapes status
                string blinkBlendshapesStatus = targetScript.IsBlinkBlendshapes() ? "Enabled - Eye Blink On" : "Disabled - Eye Blink Off";
                EditorGUILayout.LabelField("Blink Blendshapes:", blinkBlendshapesStatus);

                // Add label to show the lipsync status
                string lipsyncStatus = targetScript.IsLipsync() ? "Enabled - Lipsync On" : "Disabled - Lipsync Off";
                EditorGUILayout.LabelField("Lipsync:", lipsyncStatus);

                EditorGUI.indentLevel--;
            }
        }

        void DrawLipSync()
        {
            EditorGUILayout.Space();
            targetScript.lipSyncFoldout = EditorGUILayout.Foldout(targetScript.lipSyncFoldout, "Lip Sync / Visemes", true, boldFoldoutStyle);

            if (targetScript.lipSyncFoldout)
            {
                EditorGUI.indentLevel++;

                int newVisemeIndex = (int)targetScript.VisemeIdx;
                newVisemeIndex = EditorGUILayout.Popup("Viseme Index", newVisemeIndex, Enum.GetNames(typeof(AASEmulatorRuntime.VisemeIndex)));
                HandlePopupScroll(ref newVisemeIndex, 0, Enum.GetNames(typeof(AASEmulatorRuntime.VisemeIndex)).Length - 1);
                targetScript.VisemeIdx = (AASEmulatorRuntime.VisemeIndex)newVisemeIndex;
                targetScript.Viseme = EditorGUILayout.IntSlider("Viseme", targetScript.Viseme, 0, 14);

                EditorGUI.indentLevel--;
            }
        }

        void DrawBuiltInGestures()
        {
            EditorGUILayout.Space();
            targetScript.builtInGesturesFoldout = EditorGUILayout.Foldout(targetScript.builtInGesturesFoldout, "Built-in inputs / Hand Gestures", true, boldFoldoutStyle);

            if (targetScript.builtInGesturesFoldout)
            {
                EditorGUI.indentLevel++;

                int newLeftGestureIndex = EditorGUILayout.Popup("Gesture Left Index", (int)targetScript.GestureLeftIdx, Enum.GetNames(typeof(AASEmulatorRuntime.GestureIndex)));
                HandlePopupScroll(ref newLeftGestureIndex, 0, Enum.GetNames(typeof(AASEmulatorRuntime.GestureIndex)).Length - 1);
                if ((AASEmulatorRuntime.GestureIndex)newLeftGestureIndex != targetScript.GestureLeftIdx)
                {
                    targetScript.GestureLeftIdx = (AASEmulatorRuntime.GestureIndex)newLeftGestureIndex;
                }
                float newLeftGestureValue = EditorGUILayout.Slider("Gesture Left", targetScript.GestureLeft, -1, 6);
                if (!Mathf.Approximately(newLeftGestureValue, targetScript.GestureLeft))
                {
                    targetScript.GestureLeft = newLeftGestureValue;
                }

                int newRightGestureIndex = EditorGUILayout.Popup("Gesture Right Index", (int)targetScript.GestureRightIdx, Enum.GetNames(typeof(AASEmulatorRuntime.GestureIndex)));
                HandlePopupScroll(ref newRightGestureIndex, 0, Enum.GetNames(typeof(AASEmulatorRuntime.GestureIndex)).Length - 1);
                if ((AASEmulatorRuntime.GestureIndex)newRightGestureIndex != targetScript.GestureRightIdx)
                {
                    targetScript.GestureRightIdx = (AASEmulatorRuntime.GestureIndex)newRightGestureIndex;
                }
                float newRightGestureValue = EditorGUILayout.Slider("Gesture Right", targetScript.GestureRight, -1, 6);
                if (!Mathf.Approximately(newRightGestureValue, targetScript.GestureRight))
                {
                    targetScript.GestureRight = newRightGestureValue;
                }

                EditorGUI.indentLevel--;
            }
        }

        void DrawBuiltInLocomotion()
        {
            EditorGUILayout.Space();
            targetScript.builtInLocomotionFoldout = EditorGUILayout.Foldout(targetScript.builtInLocomotionFoldout, "Built-in inputs / Locomotion", true, boldFoldoutStyle);

            if (targetScript.builtInLocomotionFoldout)
            {
                EditorGUI.indentLevel++;

                // Custom joystick GUI
                targetScript.joystickFoldout = EditorGUILayout.Foldout(targetScript.joystickFoldout, "Joystick", true, boldFoldoutStyle);
                if (targetScript.joystickFoldout)
                {
                    Rect joystickRect = GUILayoutUtility.GetRect(100, 100, GUILayout.MaxWidth(100), GUILayout.MaxHeight(100));
                    Vector2 newMovementValue = Joystick2DField(joystickRect, targetScript.Movement, true);
                    if (newMovementValue != targetScript.Movement)
                    {
                        targetScript.Movement = newMovementValue;
                    }
                }
                // Movement field
                Vector2 newMovementValue2 = EditorGUILayout.Vector2Field("Movement", targetScript.Movement);
                if (newMovementValue2 != targetScript.Movement)
                {
                    targetScript.Movement = newMovementValue2;
                }

                targetScript.Crouching = EditorGUILayout.Toggle("Crouching", targetScript.Crouching);
                targetScript.Prone = EditorGUILayout.Toggle("Prone", targetScript.Prone);
                targetScript.Flying = EditorGUILayout.Toggle("Flying", targetScript.Flying);
                targetScript.Sitting = EditorGUILayout.Toggle("Sitting", targetScript.Sitting);
                targetScript.Grounded = EditorGUILayout.Toggle("Grounded", targetScript.Grounded);

                EditorGUI.indentLevel--;
            }
        }

        void DrawBuiltInEmotes()
        {
            EditorGUILayout.Space();
            targetScript.builtInEmotesFoldout = EditorGUILayout.Foldout(targetScript.builtInEmotesFoldout, "Built-in inputs / Emotes", true, boldFoldoutStyle);

            if (targetScript.builtInEmotesFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Emote", GUILayout.Width(60));
                for (int i = 0; i <= 8; i++)
                {
                    bool emote = EditorGUILayout.Toggle(targetScript.Emote == i, GUILayout.Width(30));
                    if (emote)
                    {
                        targetScript.Emote = i;
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Toggle", GUILayout.Width(60));
                for (int i = 0; i <= 8; i++)
                {
                    bool toggle = EditorGUILayout.Toggle(targetScript.Toggle == i, GUILayout.Width(30));
                    if (toggle)
                    {
                        targetScript.Toggle = i;
                    }
                }
                EditorGUILayout.EndHorizontal();

                targetScript.CancelEmote = EditorGUILayout.Toggle("Cancel Emote", targetScript.CancelEmote);

                EditorGUI.indentLevel--;
            }
        }

        void DrawAnimatorParameters()
        {
            EditorGUILayout.Space();

            targetScript.floatsFoldout = EditorGUILayout.Foldout(targetScript.floatsFoldout, "Floats", true, boldFoldoutStyle);
            if (targetScript.floatsFoldout)
            {
                EditorGUI.indentLevel++;
                foreach (AASEmulatorRuntime.FloatParam floatParam in targetScript.Floats)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(floatParam.machineName, GUILayout.MaxWidth(150));
                    EditorGUILayout.LabelField(floatParam.synced ? "Synced" : "Local", GUILayout.MaxWidth(75));
                    EditorGUI.BeginDisabledGroup(floatParam.isControlledByCurve);
                    float newFloatValue = EditorGUILayout.FloatField(floatParam.value);
                    EditorGUI.EndDisabledGroup();
                    if (floatParam.value != newFloatValue)
                    {
                        floatParam.value = newFloatValue;
                        targetScript.m_animator.SetFloat(floatParam.machineName, newFloatValue);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }

            targetScript.intsFoldout = EditorGUILayout.Foldout(targetScript.intsFoldout, "Ints", true, boldFoldoutStyle);
            if (targetScript.intsFoldout)
            {
                EditorGUI.indentLevel++;
                foreach (AASEmulatorRuntime.IntParam intParam in targetScript.Ints)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(intParam.machineName, GUILayout.MaxWidth(150));
                    EditorGUILayout.LabelField(intParam.synced ? "Synced" : "Local", GUILayout.MaxWidth(75));
                    EditorGUI.BeginDisabledGroup(intParam.isControlledByCurve);
                    int newIntValue = EditorGUILayout.IntField(intParam.value);
                    EditorGUI.EndDisabledGroup();
                    if (intParam.value != newIntValue)
                    {
                        intParam.value = newIntValue;
                        targetScript.m_animator.SetInteger(intParam.machineName, newIntValue);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }

            targetScript.boolsFoldout = EditorGUILayout.Foldout(targetScript.boolsFoldout, "Bools", true, boldFoldoutStyle);
            if (targetScript.boolsFoldout)
            {
                EditorGUI.indentLevel++;
                foreach (AASEmulatorRuntime.BoolParam boolParam in targetScript.Bools)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(boolParam.machineName, GUILayout.MaxWidth(150));
                    EditorGUILayout.LabelField(boolParam.synced ? "Synced" : "Local", GUILayout.MaxWidth(75));
                    EditorGUI.BeginDisabledGroup(boolParam.isControlledByCurve);
                    bool newBoolValue = EditorGUILayout.Toggle(boolParam.value);
                    EditorGUI.EndDisabledGroup();
                    if (boolParam.value != newBoolValue)
                    {
                        boolParam.value = newBoolValue;
                        targetScript.m_animator.SetBool(boolParam.machineName, newBoolValue);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
        }

        void DrawAdvanced()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel);
            targetScript.EnableCCKEmulator = EditorGUILayout.Toggle("Enable CCK Emulator", targetScript.EnableCCKEmulator);
        }
    }
}