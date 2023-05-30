using ABI.CCK.Components;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NAK.AASEmulator.Runtime
{
    public class AASEmulatorRuntime : MonoBehaviour
    {
        public delegate void AddRuntime(Component runtime);
        public static AddRuntime addRuntimeDelegate;

        // EditorGUI stuff
        public delegate void RepaintRequestHandler();
        public static event RepaintRequestHandler OnRequestRepaint;
        [HideInInspector] public bool avatarInfoFoldout = true;
        [HideInInspector] public bool lipSyncFoldout = true;
        [HideInInspector] public bool builtInLocomotionFoldout = true;
        [HideInInspector] public bool builtInEmotesFoldout = true;
        [HideInInspector] public bool builtInGesturesFoldout = true;
        [HideInInspector] public bool joystickFoldout = false;
        [HideInInspector] public bool floatsFoldout = false;
        [HideInInspector] public bool intsFoldout = false;
        [HideInInspector] public bool boolsFoldout = false;
        public bool EnableCCKEmulator;

        [Header("Lip Sync / Visemes")]
        VisemeIndex _visemeIdx;
        public VisemeIndex VisemeIdx
        {
            get => _visemeIdx;
            set
            {
                _visemeIdx = value;
                _viseme = (int)value;
            }
        }
        [SerializeField, Range(0, 14)] int _viseme;
        public int Viseme
        {
            get => _viseme;
            set
            {
                _viseme = value;
                _visemeIdx = (VisemeIndex)value;
            }
        }

        [Header("Built-in inputs / Hand Gestures")]
        GestureIndex _gestureLeftIdx;
        public GestureIndex GestureLeftIdx
        {
            get => _gestureLeftIdx;
            set
            {
                _gestureLeftIdx = value;
                _gestureLeft = (float)value - 1;
            }
        }
        [SerializeField, Range(-1, 6)]
        float _gestureLeft;
        public float GestureLeft
        {
            get => _gestureLeft;
            set
            {
                _gestureLeft = value;
                if (_gestureLeft > 0 && _gestureLeft <= 1)
                {
                    _gestureLeftIdx = GestureIndex.Fist;
                    return;
                }
                _gestureLeftIdx = (GestureIndex)Mathf.FloorToInt(value + 1);
            }
        }
        GestureIndex _gestureRightIdx;
        public GestureIndex GestureRightIdx
        {
            get => _gestureRightIdx;
            set
            {
                _gestureRightIdx = value;
                _gestureRight = (float)value - 1;
            }
        }
        [SerializeField, Range(-1, 6)]
        float _gestureRight;
        public float GestureRight
        {
            get => _gestureRight;
            set
            {
                _gestureRight = value;
                if (_gestureRight > 0 && _gestureRight <= 1)
                {
                    _gestureRightIdx = GestureIndex.Fist;
                    return;
                }
                _gestureRightIdx = (GestureIndex)Mathf.FloorToInt(value + 1);
            }
        }

        [Header("Built-in inputs / Locomotion")]
        Vector2 _movement;
        public Vector2 Movement
        {
            get => _movement;
            set => _movement = new Vector2(Mathf.Clamp(value.x, -1f, 1f), Mathf.Clamp(value.y, -1f, 1f));
        }

        public bool Crouching;
        public bool Prone;
        public bool Flying;
        public bool Sitting;
        public bool Grounded = true;

        [Header("Built-in inputs / Emotes")]
        [SerializeField, Range(0, 8)] float _toggle;
        public int Toggle { get => Mathf.RoundToInt(_toggle); set => _toggle = value; }

        [SerializeField, Range(0, 8)] float _emote;
        public int Emote { get => Mathf.RoundToInt(_emote); set => _emote = value; }
        public bool CancelEmote;

        [Header("User-generated inputs")]
        public List<FloatParam> Floats = new List<FloatParam>();
        public List<IntParam> Ints = new List<IntParam>();
        public List<BoolParam> Bools = new List<BoolParam>();

        public Dictionary<string, AnimatorControllerParameterType> Core = new Dictionary<string, AnimatorControllerParameterType>();

        public enum VisemeIndex
        {
            sil, PP, FF, TH, DD, kk, CH, SS, nn, RR, aa, E, I, O, U
        }
        public enum GestureIndex
        {
            HandOpen, Neutral, Fist, ThumbsUp, HandGun, Fingerpoint, Victory, RockNRoll,
        }
        public static HashSet<string> BUILTIN_PARAMETERS = new HashSet<string>
        {
            "GestureLeft",
            "GestureRight",
            "MovementX",
            "MovementY",
            "Crouching",
            "Prone",
            "Sitting",
            "Flying",
            "Toggle",
            "Emote",
            "CancelEmote",
            "Grounded",
        };
        [Serializable]
        public class FloatParam
        {
            [HideInInspector] public string name;
            [HideInInspector] public bool synced;
            [HideInInspector] public bool isControlledByCurve;
            public string machineName;
            public float value;
        }
        [Serializable]
        public class IntParam
        {
            [HideInInspector] public string name;
            [HideInInspector] public bool synced;
            [HideInInspector] public bool isControlledByCurve;
            public string machineName;
            public int value;
        }
        [Serializable]
        public class BoolParam
        {
            [HideInInspector] public string name;
            [HideInInspector] public bool synced;
            [HideInInspector] public bool isControlledByCurve;
            public string machineName;
            public bool value;
        }

        CVRAvatar m_avatar;
        public Animator m_animator;
        int m_locomotionEmotesLayerIdx = -1;
        int[] m_visemeBlendShapeIdxs;
        bool m_emotePlayed;
        bool m_emotePlaying;
        bool m_emoteCanceled;
        bool m_isInitialized;

        void Awake()
        {
            if (addRuntimeDelegate != null)
            {
                addRuntimeDelegate(this);
            }

            m_avatar = GetComponent<CVRAvatar>();
            m_animator = GetComponent<Animator>();
            AnalyzeAnimator();

            SetDefaultValues();

            InitializeVisemeBlendShapeIndexes();

            m_isInitialized = true;
        }

        void SetDefaultValues()
        {
            Viseme = 0;
            GestureLeft = 0;
            GestureRight = 0;
            Grounded = true;
        }

        void AnalyzeAnimator()
        {
            if (m_avatar == null || m_animator == null) return;

            if (m_animator.runtimeAnimatorController == null)
            {
                m_animator.runtimeAnimatorController = m_avatar.overrides.runtimeAnimatorController;
            }
            
            // Check for "Locomotion/Emotes" layer and store its index if found
            m_locomotionEmotesLayerIdx = m_animator.GetLayerIndex("Locomotion/Emotes");
            if (m_locomotionEmotesLayerIdx == -1)
            {
                Debug.Log("Locomotion/Emotes layer not found.");
            }

            AnimatorControllerParameter[] parameters = m_animator.parameters;
            Core.Clear();
            Floats.Clear();
            Ints.Clear();
            Bools.Clear();

            foreach (AnimatorControllerParameter param in parameters)
            {
                string paramName = param.name;
                if (BUILTIN_PARAMETERS.Contains(paramName))
                {
                    Core[paramName] = param.type;
                    continue;
                }

                bool isLocal = paramName.StartsWith("#");

                switch (param.type)
                {
                    case AnimatorControllerParameterType.Bool:
                        BoolParam boolParam = new BoolParam();
                        boolParam.name = paramName;
                        boolParam.machineName = paramName;
                        boolParam.synced = !isLocal;
                        boolParam.value = m_animator.GetBool(paramName);
                        boolParam.isControlledByCurve = m_animator.IsParameterControlledByCurve(paramName);
                        Bools.Add(boolParam);
                        break;
                    case AnimatorControllerParameterType.Int:
                        IntParam intParam = new IntParam();
                        intParam.name = paramName;
                        intParam.machineName = paramName;
                        intParam.synced = !isLocal;
                        intParam.value = m_animator.GetInteger(paramName);
                        intParam.isControlledByCurve = m_animator.IsParameterControlledByCurve(paramName);
                        Ints.Add(intParam);
                        break;
                    case AnimatorControllerParameterType.Float:
                        FloatParam floatParam = new FloatParam();
                        floatParam.name = paramName;
                        floatParam.machineName = paramName;
                        floatParam.synced = !isLocal;
                        floatParam.value = m_animator.GetFloat(paramName);
                        floatParam.isControlledByCurve = m_animator.IsParameterControlledByCurve(paramName);
                        Floats.Add(floatParam);
                        break;
                }
            }
        }

        void InitializeVisemeBlendShapeIndexes()
        {
            if (m_avatar.bodyMesh != null && m_avatar.visemeBlendshapes != null)
            {
                m_visemeBlendShapeIdxs = new int[m_avatar.visemeBlendshapes == null ? 0 : m_avatar.visemeBlendshapes.Length];
                for (int i = 0; i < m_avatar.visemeBlendshapes.Length; i++)
                {
                    m_visemeBlendShapeIdxs[i] = m_avatar.bodyMesh.sharedMesh.GetBlendShapeIndex(m_avatar.visemeBlendshapes[i]);
                }
            }
            else
            {
                m_visemeBlendShapeIdxs = new int[0];
            }
        }

        void Update()
        {
            if (!m_isInitialized)
            {
                return;
            }

            // EditorGUI Repaint
            CheckAndResetEmoteValues();

            ApplyLipSync();
            ApplyAvatarParameters();

            m_emotePlayed = Emote != 0;
            m_emoteCanceled = CancelEmote;

            UpdateCachedParameterValues();
        }

        public void ApplyLipSync()
        {
            if (!m_avatar.useVisemeLipsync) return;

            if (m_avatar.visemeMode == CVRAvatar.CVRAvatarVisemeMode.Visemes && m_avatar.bodyMesh != null)
            {
                for (int i = 0; i < m_visemeBlendShapeIdxs.Length; i++)
                {
                    if (m_visemeBlendShapeIdxs[i] != -1)
                    {
                        m_avatar.bodyMesh.SetBlendShapeWeight(m_visemeBlendShapeIdxs[i], (i == Viseme ? 100.0f : 0.0f));
                    }
                }
            }
        }

        void CheckAndResetEmoteValues()
        {
            bool shouldRepaint = false;

            if (m_emotePlayed)
            {
                m_emotePlayed = false;
                Emote = 0;
                shouldRepaint = true;
            }

            if (m_emoteCanceled)
            {
                m_emoteCanceled = false;
                CancelEmote = false;
                shouldRepaint = true;
            }

            bool emotePlaying = IsEmotePlaying();
            if (emotePlaying != m_emotePlaying)
            {
                m_emotePlaying = emotePlaying;
                shouldRepaint = true;
            }

            // Cannot play an emote while running, this applies next frame
            if (Movement.magnitude > 0 && m_emotePlaying)
            {
                CancelEmote = true;
                shouldRepaint = true;
            }

            if (shouldRepaint)
            {
                OnRequestRepaint?.Invoke();
            }
        }

        public void UpdateCachedParameterValues()
        {
            // Update floats
            foreach (FloatParam floatParam in Floats)
            {
                float animatorValue = m_animator.GetFloat(floatParam.machineName);
                if (floatParam.value != animatorValue)
                {
                    floatParam.value = animatorValue;
                }
            }

            // Update ints
            foreach (IntParam intParam in Ints)
            {
                int animatorValue = m_animator.GetInteger(intParam.machineName);
                if (intParam.value != animatorValue)
                {
                    intParam.value = animatorValue;
                }
            }

            // Update bools
            foreach (BoolParam boolParam in Bools)
            {
                bool animatorValue = m_animator.GetBool(boolParam.machineName);
                if (boolParam.value != animatorValue)
                {
                    boolParam.value = animatorValue;
                }
            }
        }

        public bool IsEmotePlaying()
        {
            if (m_locomotionEmotesLayerIdx != -1)
            {
                AnimatorClipInfo[] clipInfo = m_animator.GetCurrentAnimatorClipInfo(m_locomotionEmotesLayerIdx);
                foreach (var clip in clipInfo)
                {
                    if (clip.clip.name.Contains("Emote")) return true;
                }
            }
            return false;
        }

        public bool IsEyeMovement()
        {
            return m_avatar.useEyeMovement;
        }

        public bool IsBlinkBlendshapes()
        {
            return m_avatar.useBlinkBlendshapes;
        }

        public bool IsLipsync()
        {
            return m_avatar.useVisemeLipsync;
        }

        // TODO: replace this so I can make sure built-in parameters exist first
        // I also want to integrate this with the older CCKEmulator project
        public void ApplyAvatarParameters()
        {
            SetCoreParameter("GestureLeft", GestureLeft);
            SetCoreParameter("GestureRight", GestureRight);
            SetCoreParameter("Grounded", Grounded ? 1 : 0);
            SetCoreParameter("Crouching", Crouching ? 1 : 0);
            SetCoreParameter("Prone", Prone ? 1 : 0);
            SetCoreParameter("Flying", Flying ? 1 : 0);
            SetCoreParameter("Sitting", Sitting ? 1 : 0);
            SetCoreParameter("MovementX", Movement.x);
            SetCoreParameter("MovementY", Movement.y);
            SetCoreParameter("Emote", Emote);
            SetCoreParameter("Toggle", Toggle);

            if (CancelEmote)
            {
                CancelEmote = false;
                SetCoreParameter("CancelEmote");
            }
        }

        public void SetCoreParameter(string parameter, float value = 0)
        {
            if (Core.ContainsKey(parameter))
            {
                switch (Core[parameter])
                {
                    case AnimatorControllerParameterType.Float:
                        m_animator.SetFloat(parameter, value);
                        break;
                    case AnimatorControllerParameterType.Int:
                        m_animator.SetInteger(parameter, (int)value);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        m_animator.SetBool(parameter, value > 0.5f);
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        m_animator.SetTrigger(parameter);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}