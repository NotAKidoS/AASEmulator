#if CVR_CCK_EXISTS
using System.Collections.Generic;
using ABI.CCK.Components;
using NAK.AASEmulator.Runtime.SubSystems;
using UnityEngine;

namespace NAK.AASEmulator.Runtime.Wrappers
{
    [AddComponentMenu("/")]
    [HelpURL(AASEmulatorCore.AAS_EMULATOR_GIT_URL)]
    public partial class CVRAvatarTriggerWrapper : MonoBehaviour
    {
        private CVRAdvancedAvatarSettingsTrigger _trigger;
        private HashSet<int> _pointerHashes;
        private bool _triggerOnlyPointerReferences;
        
        private Collider _collider;

        // Delayed Enter/Exit tasks
        private List<CVRPointerWrapper> _taskPointers;
        private List<ScheduledTask> _taskEntries;
        private Coroutine _taskCheckerCoroutine;
        
        // Running Stay tasks
        private List<CVRPointerWrapper> _taskOnStayPointers;
        private List<CVRAdvancedAvatarSettingsTriggerTaskStay> _taskOnStayEntries;
        private Coroutine _taskOnStayCoroutine;
        
        private int _emulatorRuntimeHash;
        private ParameterAccess _parameterAccess;

        #region Unity Events
        
        private void Start()
        {
            if (!TryGetComponent(out _trigger))
            {
                Destroy(this);
                return;
            }
            
            IAASEmulatorAvatar runtimeAvatar = GetComponentInParent<IAASEmulatorAvatar>();
            if (runtimeAvatar is not { IsLocal: true })
            {
                Destroy(this); // On remote avatar, triggers only run on local avatar
                return;
            }
            _parameterAccess = runtimeAvatar.AnimatorManager.Parameters;
            _emulatorRuntimeHash = runtimeAvatar.GetRuntimeHash();
            
            SetupTriggerShapeIfNeeded();
            SetupAdvancedTriggerIfNeeded();
        }

        private void OnDestroy()
        {
            StopTaskChecker();
            StopStayTaskRunner();
        }

        private void OnTriggerEnter(Collider other)
        {
            // Debug.Log("OnTriggerEnter");
            
            if (!other.TryGetComponent(out CVRPointerWrapper pointer)) return;
            if (!CheckPointerCanInteract(pointer)) return;
            
            if (!_trigger.useAdvancedTrigger)
            {
                // Apply basic parameter and fuck off
                _parameterAccess.SetParameter(_trigger.settingName, _trigger.settingValue);
                return;
            }
            
            if (!CheckAdvancedPointerAllowed(pointer)) return;
            
            RunAdvancedTriggerTasks(pointer, _trigger.enterTasks, true);
            RunAdvancedTriggerStayTasks(pointer, _trigger.stayTasks, true);
        }
        
        private void OnTriggerExit(Collider other)
        {
            // Debug.Log("OnTriggerExit");
            
            // Simple triggers do not have OnExit or OnStay events
            if (!_trigger.useAdvancedTrigger) return;
            
            if (!other.TryGetComponent(out CVRPointerWrapper pointer)) return;
            if (!CheckPointerCanInteract(pointer)) return;

            RunAdvancedTriggerTasks(pointer, _trigger.exitTasks, false);
            RunAdvancedTriggerStayTasks(pointer, _trigger.stayTasks, false);
        }
        
        #endregion Unity Events

        #region Trigger Events

        private bool CheckPointerCanInteract(CVRPointerWrapper pointer)
        {
            // Check if the pointer is on a Local or Networked avatar.
            // There can be multiple "Local" avatars, so we check the runtime hash.
            bool isOnThisAvatar = pointer.CheckRuntimeHashMatch(_emulatorRuntimeHash);
            return (_trigger.isLocalInteractable && isOnThisAvatar) ||
                   (_trigger.isNetworkInteractable && !isOnThisAvatar);
        }

        private bool CheckAdvancedPointerAllowed(CVRPointerWrapper pointer)
        {
            return _pointerHashes.Count == 0 || _pointerHashes.Contains(_triggerOnlyPointerReferences 
                ? pointer.GetHashCode() : pointer.GetComputedTypeHash());
        }
        
        private void RunAdvancedTriggerTasks(
            CVRPointerWrapper pointer,
            List<CVRAdvancedAvatarSettingsTriggerTask> tasks, 
            bool isOnEnter)
        {
            // Remove any existing tasks for this pointer
            for (int i = _taskPointers.Count - 1; i >= 0; i--)
            {
                if (_taskPointers[i] != pointer) continue;
                _taskPointers.RemoveAt(i);
                _taskEntries.RemoveAt(i);
            }
            
            // Schedule or execute tasks
            foreach (CVRAdvancedAvatarSettingsTriggerTask task in tasks)
                TryRunTriggerTask(task, pointer, isOnEnter);
        }
        
        private void RunAdvancedTriggerStayTasks(
            CVRPointerWrapper pointer,
            List<CVRAdvancedAvatarSettingsTriggerTaskStay> tasks,
            bool isOnEnter)
        {
            // Schedule or execute tasks
            foreach (CVRAdvancedAvatarSettingsTriggerTaskStay task in tasks)
                TryScheduleStayTask(task, pointer, isOnEnter);
        }
        
        #endregion Trigger Events
    }
}
#endif