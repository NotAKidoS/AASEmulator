#if CVR_CCK_EXISTS
using System;
using ABI.CCK.Components;
using UnityEngine;

namespace NAK.AASEmulator.Runtime.Wrappers
{
    public partial class CVRAvatarTriggerWrapper
    {
        #region Enter / Exit Tasks
        
        private struct ScheduledTask
        {
            public CVRAdvancedAvatarSettingsTriggerTask Task { get; }
            public float ExecutionTime { get; }
            public ScheduledTask(CVRAdvancedAvatarSettingsTriggerTask task, float executionTime)
            {
                Task = task;
                ExecutionTime = executionTime;
            }
        }

        private void TryRunTriggerTask(CVRAdvancedAvatarSettingsTriggerTask task, CVRPointerWrapper pointer, bool isEnter)
        {
            float delay = task.delay;
            if (isEnter) delay += task.holdTime;

            if (delay > 0f)
            {
                ScheduledTask scheduledTask = new(task, Time.time + delay);
                _taskEntries.Add(scheduledTask);
                _taskPointers.Add(pointer);
                StartDelayedTaskCheckerIfNeeded();
                return;
            }

            ExecuteTask(task, pointer);
        }

        private void ExecuteTask(CVRAdvancedAvatarSettingsTriggerTask task, CVRPointerWrapper pointer = null)
        {
            // Debug.Log($"Executing task {task.settingName} on pointer {pointer?.name}");
            
            switch (task.updateMethod)
            {
                case CVRAdvancedAvatarSettingsTriggerTask.UpdateMethod.Override:
                    _parameterAccess.SetParameter(task.settingName, task.settingValue);
                    break;
                case CVRAdvancedAvatarSettingsTriggerTask.UpdateMethod.Add:
                    _parameterAccess.GetParameter(task.settingName, out float currentValue);
                    _parameterAccess.SetParameter(task.settingName, currentValue + task.settingValue);
                    break;
                case CVRAdvancedAvatarSettingsTriggerTask.UpdateMethod.Subtract:
                    _parameterAccess.GetParameter(task.settingName, out currentValue);
                    _parameterAccess.SetParameter(task.settingName, currentValue - task.settingValue);
                    break;
                case CVRAdvancedAvatarSettingsTriggerTask.UpdateMethod.Toggle:
                    _parameterAccess.GetParameter(task.settingName, out currentValue);
                    _parameterAccess.SetParameter(task.settingName, currentValue == 0f ? 1f : 0f);
                    break;
                case CVRAdvancedAvatarSettingsTriggerTask.UpdateMethod.SetFromPointer:
                    if (pointer == null) return; // LMAO
                    _parameterAccess.SetParameter(task.settingName, pointer.GetPointerValue());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion Enter / Exit Tasks
        
        #region Stay Tasks
        
        private void TryScheduleStayTask(
            CVRAdvancedAvatarSettingsTriggerTaskStay task,
            CVRPointerWrapper pointer,
            bool isOnEnter)
        {
            if (isOnEnter)
            {
                _taskOnStayEntries.Add(task);
                _taskOnStayPointers.Add(pointer);
                StartStayTaskRunnerIfNeeded();
            }
            else
            {
                for (int i = 0; i < _taskOnStayPointers.Count; i++)
                {
                    if (_taskOnStayPointers[i] != pointer || _taskOnStayEntries[i] != task) continue;
                    _taskOnStayEntries.RemoveAt(i);
                    _taskOnStayPointers.RemoveAt(i);
                    break;
                }
                if (_taskOnStayPointers.Count == 0) StopStayTaskRunner();
            }
        }
        
        private void ExecuteStayTask(CVRAdvancedAvatarSettingsTriggerTaskStay task, CVRPointerWrapper pointer)
        {
            // Debug.Log($"Executing stay task {task.settingName} on pointer {pointer.name}");
            
            switch (task.updateMethod)
            {
                case CVRAdvancedAvatarSettingsTriggerTaskStay.UpdateMethod.SetFromPosition:
                case CVRAdvancedAvatarSettingsTriggerTaskStay.UpdateMethod.SetFromDistance:
                {
                    Vector3 closestPoint = _collider.ClosestPoint(pointer.transform.position);
                    
                    Transform colliderTransform = _collider.transform;
                    Vector3 lossyScale = colliderTransform.lossyScale;
                    Vector3 localPoint = colliderTransform.InverseTransformPoint(closestPoint);
                    Vector3 size = Vector3.one;

                    switch (_collider)
                    {
                        case BoxCollider boxCollider:
                            localPoint -= boxCollider.center;
                            size = boxCollider.size * 0.5f;
                            break;
                        case SphereCollider sphereCollider:
                            size.x = size.y = size.z = sphereCollider.radius;
                            break;
                    }

                    // Apply scale
                    localPoint.Scale(lossyScale);

                    // Calculate normalized positions along each axis
                    float normalizedX = Mathf.Clamp01(Mathf.InverseLerp(-size.x * lossyScale.x, size.x * lossyScale.x, localPoint.x));
                    float normalizedY = Mathf.Clamp01(Mathf.InverseLerp(-size.y * lossyScale.y, size.y * lossyScale.y, localPoint.y));
                    float normalizedZ = Mathf.Clamp01(Mathf.InverseLerp(-size.z * lossyScale.z, size.z * lossyScale.z, localPoint.z));

                    float parameterValue = 0f;

                    if (task.updateMethod == CVRAdvancedAvatarSettingsTriggerTaskStay.UpdateMethod.SetFromPosition)
                    {
                        // Set parameter based on position along specified axis
                        parameterValue = _trigger.sampleDirection switch
                        {
                            CVRAdvancedAvatarSettingsTrigger.SampleDirection.XPositive => normalizedX,
                            CVRAdvancedAvatarSettingsTrigger.SampleDirection.XNegative => 1f - normalizedX,
                            CVRAdvancedAvatarSettingsTrigger.SampleDirection.YPositive => normalizedY,
                            CVRAdvancedAvatarSettingsTrigger.SampleDirection.YNegative => 1f - normalizedY,
                            CVRAdvancedAvatarSettingsTrigger.SampleDirection.ZPositive => normalizedZ,
                            CVRAdvancedAvatarSettingsTrigger.SampleDirection.ZNegative => 1f - normalizedZ,
                            _ => parameterValue
                        };
                    }
                    else // SetFromDistance
                    {
                        // Calculate normalized distance from center
                        Vector3 normalizedPos = new(
                            (normalizedX - 0.5f) * 2f,
                            (normalizedY - 0.5f) * 2f,
                            (normalizedZ - 0.5f) * 2f
                        );
                        parameterValue = Mathf.Clamp01(normalizedPos.magnitude);
                    }

                    _parameterAccess.SetParameter(
                        task.settingName, 
                        Mathf.Lerp(task.minValue, task.maxValue, parameterValue)
                    );
                    break;
                }
                
                case CVRAdvancedAvatarSettingsTriggerTaskStay.UpdateMethod.Add:
                    _parameterAccess.GetParameter(task.settingName, out float currentValue);
                    _parameterAccess.SetParameter(task.settingName, currentValue + task.minValue * Time.fixedDeltaTime);
                    break;
                case CVRAdvancedAvatarSettingsTriggerTaskStay.UpdateMethod.Subtract:
                    _parameterAccess.GetParameter(task.settingName, out currentValue);
                    _parameterAccess.SetParameter(task.settingName, currentValue - task.minValue * Time.fixedDeltaTime);
                    break;
                case CVRAdvancedAvatarSettingsTriggerTaskStay.UpdateMethod.SetFromPointer:
                    _parameterAccess.SetParameter(task.settingName, pointer.GetPointerValue());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion Stay Tasks
    }
}
#endif