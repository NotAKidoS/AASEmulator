#if CVR_CCK_EXISTS
using System.Collections.Generic;
using ABI.CCK.Components;
using UnityEngine;

namespace NAK.AASEmulator.Runtime.Wrappers
{
    public partial class CVRAvatarTriggerWrapper
    {
        #region Shape & Component Setup

        private void SetupTriggerShapeIfNeeded()
        {
            // Ensure collider
            if (!TryGetComponent(out _collider))
            {
                if (OnlyHasDistanceTaskWhichForSomeReasonMeansSphereCollider())
                {
                    // Sphere Collider
                    SphereCollider sphereCol = gameObject.AddComponent<SphereCollider>();
                    sphereCol.isTrigger = true;
                    sphereCol.radius = _trigger.areaSize.x;
                    _collider = sphereCol;
                }
                else
                {
                    // Box Collider
                    BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
                    boxCol.isTrigger = true;
                    boxCol.size = _trigger.areaSize;
                    boxCol.center = _trigger.areaOffset;
                    _collider = boxCol;
                }
            }
            
            // Ensure rigidbody
            if (!TryGetComponent(out Rigidbody rigBody))
                rigBody = gameObject.AddComponent<Rigidbody>();
            
            // We love hardcoded bullshit at ChilloutVR!
            rigBody.isKinematic = true;
            rigBody.useGravity = false;

            return;
            bool OnlyHasDistanceTaskWhichForSomeReasonMeansSphereCollider()
            {
                // No enter or exit tasks, only stay tasks, and all stay tasks are distance
                // What the fuck is this hardcoded bullshit...
                return _trigger.enterTasks.Count == 0 && _trigger.exitTasks.Count == 0 
                    && _trigger.stayTasks.Count > 0 && _trigger.stayTasks.TrueForAll(task 
                        => task.updateMethod == CVRAdvancedAvatarSettingsTriggerTaskStay.UpdateMethod.SetFromDistance);
            }
        }
        
        #endregion Shape & Component Setup

        #region Advanced Trigger Setup

        private void SetupAdvancedTriggerIfNeeded()
        {
            if (!_trigger.useAdvancedTrigger)
                return; // This trigger doesn't need to check for pointer references, it's a simple trigger

            _taskPointers = new List<CVRPointerWrapper>();
            _taskEntries = new List<ScheduledTask>();

            if (_trigger.stayTasks.Count > 0)
            {
                _taskOnStayPointers = new List<CVRPointerWrapper>();
                _taskOnStayEntries = new List<CVRAdvancedAvatarSettingsTriggerTaskStay>();
            }
            
            // allowedPointers take priority when non-zero and prevent allowedTypes check from running.
            // This is weird behaviour, but it's just how it is.
            
            _pointerHashes = new HashSet<int>();
            if (_trigger.allowedPointer.Count > 0) // This trigger will only check for referenced pointers
            {
                foreach (CVRPointer pointer in _trigger.allowedPointer) _pointerHashes.Add(pointer.GetHashCode());
                _triggerOnlyPointerReferences = true;
            }
            else // This trigger will check for referenced types on any pointer
            {
                foreach (string type in _trigger.allowedTypes) _pointerHashes.Add(type.GetHashCode());
                _triggerOnlyPointerReferences = false;
            }
        }

        #endregion Advanced Trigger Setup
    }
}
#endif