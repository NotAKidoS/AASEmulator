using ABI.CCK.Components;
using NAK.AASEmulator.Runtime.Scripts;
using NAK.AASEmulator.Runtime.SubSystems;
using System;
using UnityEngine;

namespace NAK.AASEmulator.Runtime
{
    [HelpURL("https://github.com/NotAKidOnSteam/AASEmulator")]
    public class AASEmulatorRuntime : MonoBehaviour
    {
        #region EditorGUI

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

        private bool m_shouldRepaintEditor = false;

        #endregion EditorGUI

        #region CVR_VISEME_GESTURE_INDEX

        // Oculus Lipsync
        public enum VisemeIndex
        {
            sil,
            PP,
            FF,
            TH,
            DD,
            kk,
            CH,
            SS,
            nn,
            RR,
            aa,
            E,
            I,
            O,
            U
        }

        // -1f to 6f, 0-1f is Fist weight
        public enum GestureIndex
        {
            HandOpen,
            Neutral,
            Fist,
            ThumbsUp,
            HandGun,
            Fingerpoint,
            Victory,
            RockNRoll
        }

        // Oculus Lipsync, Loudness, Loudness
        public enum VisemeModeIndex
        {
            Visemes = 0,
            Single_Blendshape,
            Jaw_Bone,
        }
        
        #endregion CVR_VISEME_GESTURE_INDEX

        #region Lip Sync / Visemes

        [Header("Lip Sync / Visemes")]
        [SerializeField][Range(0, 14)] private int _viseme;

        private VisemeIndex _visemeIdx;

        public VisemeIndex VisemeIdx
        {
            get => _visemeIdx;
            set
            {
                _visemeIdx = value;
                _viseme = (int)value;
            }
        }

        public int Viseme
        {
            get => _viseme;
            set
            {
                _viseme = value;
                _visemeIdx = (VisemeIndex)value;
            }
        }

        private int _visemeSmoothing = 50;
        private float _visemeSmoothingFactor = 0.5f;

        // Single Blendshape & Jaw Bone
        public float VisemeLoudness { get; set; }

        #endregion Lip Sync / Visemes

        #region Built-in inputs / Hand Gestures

        [Header("Built-in inputs / Hand Gestures")]
        [SerializeField][Range(-1, 6)] private float _gestureLeft;

        [SerializeField][Range(-1, 6)] private float _gestureRight;
        private GestureIndex _gestureLeftIdx;
        private GestureIndex _gestureRightIdx;

        public GestureIndex GestureLeftIdx
        {
            get => _gestureLeftIdx;
            set
            {
                _gestureLeftIdx = value;
                _gestureLeft = (float)value - 1;
            }
        }

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

        public GestureIndex GestureRightIdx
        {
            get => _gestureRightIdx;
            set
            {
                _gestureRightIdx = value;
                _gestureRight = (float)value - 1;
            }
        }

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

        #endregion Built-in inputs / Hand Gestures

        #region Built-in inputs / Locomotion

        [Header("Built-in inputs / Locomotion")]
        [SerializeField] private Vector2 _movement;

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

        #endregion Built-in inputs / Locomotion

        #region Built-in inputs / Emotes

        [Header("Built-in inputs / Toggles & Emotes")]
        [SerializeField][Range(0, 8)] private float _toggle;

        public int Toggle
        {
            get => Mathf.RoundToInt(_toggle);
            set => _toggle = value;
        }

        [SerializeField][Range(0, 8)] private float _emote;

        public int Emote
        {
            get => Mathf.RoundToInt(_emote);
            set => _emote = value;
        }

        public bool CancelEmote;

        #endregion Built-in inputs / Emotes
        
        #region Public Properties

        public bool UseLipsync => m_avatar?.useVisemeLipsync ?? false;
        public VisemeModeIndex VisemeMode => m_avatar != null ? (VisemeModeIndex)m_avatar.visemeMode : VisemeModeIndex.Visemes;
        public bool UseEyeMovement => m_avatar?.useEyeMovement ?? false;
        public bool UseBlinkBlendshapes => m_avatar?.useBlinkBlendshapes ?? false;
        public bool IsEmotePlaying => m_emotePlaying;
        
        #endregion Public Properties

        #region Variables

        public AnimatorManager AnimatorManager { get; private set; }

        public CVRAvatar m_avatar;
        public Animator m_animator;

        // Emotes
        private bool m_emotePlayed;
        private bool m_emotePlaying;
        private bool m_emoteCanceled;

        // Visemes
        private float[] m_visemeCurrentBlendShapeWeights;
        private int[] m_visemeBlendShapeIndicies;

        // Jaw Bone handling
        private HumanPoseHandler m_humanPoseHandler;
        private HumanPose m_humanPose;

        #endregion Variables

        #region Initialization

        private void Start()
        {
            m_avatar = gameObject.GetComponent<CVRAvatar>();
            if (m_avatar == null)
            {
                SimpleLogger.LogError("The CVRAvatar component is missing on the attached gameObject. Destroying...", gameObject);
                DestroyImmediate(this);
                return;
            }

            if (AASEmulator.Instance == null)
                SimpleLogger.LogWarning("AAS Emulator Control is missing from the scene. Emulator will run without scene settings!", gameObject);
            else
                AASEmulator.addComponentDelegate?.Invoke(this);
            
            // CVR will ensure this on initialization
            if (!gameObject.TryGetComponent<Animator>(out m_animator))
                m_animator = gameObject.AddComponent<Animator>();
            m_animator.applyRootMotion = false;
            m_animator.enabled = true;

            if (m_animator.isHuman)
            {
                m_humanPoseHandler?.Dispose();
                m_humanPoseHandler = new HumanPoseHandler(m_animator.avatar, m_animator.transform);
                m_humanPoseHandler.GetHumanPose(ref m_humanPose);
            }

            AnimatorManager = new AnimatorManager(m_animator);

            SetValuesToDefault();
            InitializeVisemeBlendShapeIndexes();
        }

        private void SetValuesToDefault()
        {
            _viseme = 0;
            _visemeIdx = 0;

            _gestureLeft = 0f;
            _gestureLeftIdx = GestureIndex.Neutral;

            _gestureRight = 0f;
            _gestureRightIdx = GestureIndex.Neutral;

            Grounded = true;
        }

        private void InitializeVisemeBlendShapeIndexes()
        {
            if (m_avatar.bodyMesh != null && m_avatar.visemeBlendshapes != null)
            {
                // Rough replication of games iffy viseme smoothing... OVRLipSync only wants 1-100!
                _visemeSmoothing = m_avatar.visemeSmoothing;
                _visemeSmoothingFactor = Mathf.Clamp(100 - _visemeSmoothing, 1f, 100f) / 100f;

                m_visemeBlendShapeIndicies =
                    new int[m_avatar.visemeBlendshapes?.Length ?? 0];

                if (m_avatar.visemeBlendshapes == null)
                    return;

                for (var i = 0; i < m_avatar.visemeBlendshapes.Length; i++)
                    m_visemeBlendShapeIndicies[i] =
                        m_avatar.bodyMesh.sharedMesh.GetBlendShapeIndex(m_avatar.visemeBlendshapes[i]);
            }
            else
            {
                m_visemeBlendShapeIndicies = Array.Empty<int>();
            }
        }

        #endregion Initialization

        #region Unity Methods

        private void Update()
        {
            Update_EmoteValues_Update();
            Update_CachedParametersFromAnimator();

            Apply_LipSync();
            Apply_CoreParameters();

            if (m_shouldRepaintEditor)
            {
                OnRequestRepaint?.Invoke();
                m_shouldRepaintEditor = false;
            }
        }

        // fixedDeltaTime is wack in ChilloutVR... Needs proper handling.
        // Desktop = 0.02 : OpenXR = 0.02 : OpenVR = Headset Refresh Rate
        private void FixedUpdate()
        {
            Update_EmoteValues_FixedUpdate();
        }

        #endregion Unity Methods

        #region Private Methods

        private void Apply_LipSync()
        {
            if (m_avatar.bodyMesh == null) 
                return;

            // TODO: Compare with in-game behaviour. Should be similar enough.
            float useVisemeLipsync = m_avatar.useVisemeLipsync ? 1f : 0f;
            
            switch (m_avatar.visemeMode)
            {
                case CVRAvatar.CVRAvatarVisemeMode.Visemes:
                {
                    if (_visemeSmoothing != m_avatar.visemeSmoothing)
                        _visemeSmoothingFactor = Mathf.Clamp(100 - m_avatar.visemeSmoothing, 1f, 100f) / 100f;
                    _visemeSmoothing = m_avatar.visemeSmoothing;

                    if (m_visemeCurrentBlendShapeWeights == null || m_visemeCurrentBlendShapeWeights.Length != m_visemeBlendShapeIndicies.Length)
                        m_visemeCurrentBlendShapeWeights = new float[m_visemeBlendShapeIndicies.Length];

                    for (var i = 0; i < m_visemeBlendShapeIndicies.Length; i++)
                        if (m_visemeBlendShapeIndicies[i] != -1)
                            m_avatar.bodyMesh.SetBlendShapeWeight(m_visemeBlendShapeIndicies[i],
                                m_visemeCurrentBlendShapeWeights[i] = Mathf.Lerp(m_visemeCurrentBlendShapeWeights[i],
                                    i == _viseme ? 100.0f : 0.0f, _visemeSmoothingFactor) * useVisemeLipsync);
                    break;
                }
                case CVRAvatar.CVRAvatarVisemeMode.SingleBlendshape:
                {
                    if (m_visemeBlendShapeIndicies.Length > 0 && m_visemeBlendShapeIndicies[0] != -1)
                        m_avatar.bodyMesh.SetBlendShapeWeight(m_visemeBlendShapeIndicies[0],
                            VisemeLoudness * 100.0f * useVisemeLipsync);
                    break;
                }
                // TODO: Actually test this. For now, I assume it works.
                case CVRAvatar.CVRAvatarVisemeMode.JawBone when m_animator.isHuman:
                {
                    const int jawMuscleIndex = (int)HumanBodyBones.Jaw;
                    m_humanPoseHandler.GetHumanPose(ref m_humanPose);
                    if (jawMuscleIndex < m_humanPose.muscles.Length)
                    {
                        m_humanPose.muscles[jawMuscleIndex] = VisemeLoudness * useVisemeLipsync;
                        m_humanPoseHandler.SetHumanPose(ref m_humanPose);
                    }
                    break;
                }
            }
        }
        
        private void Update_EmoteValues_Update()
        {
            if (m_emotePlayed)
            {
                m_emotePlayed = false;
                Emote = 0;
                m_shouldRepaintEditor = true;
            }

            if (m_emoteCanceled)
            {
                m_emoteCanceled = false;
                CancelEmote = false;
                m_shouldRepaintEditor = true;
            }

            var emotePlaying = AnimatorManager.IsEmotePlaying();
            if (emotePlaying != m_emotePlaying)
            {
                m_emotePlaying = emotePlaying;
                m_shouldRepaintEditor = true;
            }

            // TODO: Emote should return to 0 after 0.1s
            m_emotePlayed = Emote != 0;
            m_emoteCanceled = CancelEmote;
        }
        
        private void Update_EmoteValues_FixedUpdate()
        {
            // Cannot play an emote while running
            if (Movement.magnitude > 0 && m_emotePlaying)
            {
                CancelEmote = true;
                m_shouldRepaintEditor = true;
            }
        }

        private void Update_CachedParametersFromAnimator()
        {
            // Will not support Animator -> Core Parameter
            // It is bloat...

            // Additional Parameters
            foreach (AnimatorManager.BaseParam baseParam in AnimatorManager.Parameters.Values)
            {
                switch (baseParam)
                {
                    case AnimatorManager.FloatParam floatParam when floatParam.value != m_animator.GetFloat(baseParam.name):
                        floatParam.value = m_animator.GetFloat(baseParam.name);
                        m_shouldRepaintEditor = true;
                        break;

                    case AnimatorManager.IntParam intParam when intParam.value != m_animator.GetInteger(baseParam.name):
                        intParam.value = m_animator.GetInteger(baseParam.name);
                        m_shouldRepaintEditor = true;
                        break;

                    case AnimatorManager.BoolParam boolParam when boolParam.value != m_animator.GetBool(baseParam.name):
                        boolParam.value = m_animator.GetBool(baseParam.name);
                        m_shouldRepaintEditor = true;
                        break;

                    case AnimatorManager.TriggerParam triggerParam when triggerParam.value != m_animator.GetBool(baseParam.name):
                        triggerParam.value = m_animator.GetBool(baseParam.name);
                        m_shouldRepaintEditor = true;
                        break;
                }
            }
        }

        // TODO: Rework this so multiple streams of input can set Core Parameters!
        private void Apply_CoreParameters()
        {
            AnimatorManager.SetCoreParameter("GestureLeft", _gestureLeft);
            AnimatorManager.SetCoreParameter("GestureRight", _gestureRight);
            AnimatorManager.SetCoreParameter("Grounded", Grounded);
            AnimatorManager.SetCoreParameter("Crouching", Crouching);
            AnimatorManager.SetCoreParameter("Prone", Prone);
            AnimatorManager.SetCoreParameter("Flying", Flying);
            AnimatorManager.SetCoreParameter("Sitting", Sitting);
            AnimatorManager.SetCoreParameter("MovementX", _movement.x);
            AnimatorManager.SetCoreParameter("MovementY", _movement.y);
            AnimatorManager.SetCoreParameter("Emote", _emote);
            AnimatorManager.SetCoreParameter("Toggle", _toggle);
            
            AnimatorManager.SetLayerWeight(AnimatorManager.HAND_LEFT_LAYER_NAME, m_emotePlaying ? 0f : 1f);
            AnimatorManager.SetLayerWeight(AnimatorManager.HAND_RIGHT_LAYER_NAME, m_emotePlaying ? 0f : 1f);

            if (CancelEmote)
            {
                CancelEmote = false;
                AnimatorManager.SetCoreParameter("CancelEmote", null);
            }
        }

        #endregion Private Methods
    }
}