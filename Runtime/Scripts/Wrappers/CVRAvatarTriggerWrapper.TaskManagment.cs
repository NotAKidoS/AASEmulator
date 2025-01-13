#if CVR_CCK_EXISTS
using System.Collections;
using UnityEngine;

namespace NAK.AASEmulator.Runtime.Wrappers
{
    public partial class CVRAvatarTriggerWrapper
    {
        #region Enter/Exit Task Checker Routine

        
        private void StartDelayedTaskCheckerIfNeeded()
        {
            _taskCheckerCoroutine ??= StartCoroutine(TaskCheckerCoroutine());
        }

        private void StopTaskChecker()
        {
            if (_taskCheckerCoroutine == null) return;
            StopCoroutine(_taskCheckerCoroutine);
            _taskCheckerCoroutine = null;
        }
        
        private IEnumerator TaskCheckerCoroutine()
        {
            while (_taskEntries.Count > 0)
            {
                float currentTime = Time.time;
        
                for (int i = _taskEntries.Count - 1; i >= 0; i--)
                {
                    if (!(currentTime >= _taskEntries[i].ExecutionTime)) continue;
                    //ExecuteTask(_taskEntries[i].Task, _taskPointers[i]);
                    ExecuteTask(_taskEntries[i].Task); // Pass null to replicate CVR bug :)
                    _taskPointers.RemoveAt(i);
                    _taskEntries.RemoveAt(i);
                }

                if (_taskEntries.Count == 0)
                {
                    _taskCheckerCoroutine = null;
                    yield break;
                }

                yield return null;
            }
        }

        #endregion Enter/Exit Task Checker Routine
        
        #region Stay Task Runner Routine
        
        private void StartStayTaskRunnerIfNeeded()
        {
            //Debug.Log("StartStayTaskRunnerIfNeeded");
            _taskOnStayCoroutine ??= StartCoroutine(StayTaskRunnerCoroutine());
        }
        
        private void StopStayTaskRunner()
        {
            if (_taskOnStayCoroutine == null) return;
            //Debug.Log("StopStayTaskRunner");
            StopCoroutine(_taskOnStayCoroutine);
            _taskOnStayCoroutine = null;
        }
        
        private IEnumerator StayTaskRunnerCoroutine()
        {
            //Debug.Log("StayTaskRunnerCoroutine");
            // I believe this is close enough to when OnTriggerStay would execute... :P
            YieldInstruction forFixedUpdate = new WaitForFixedUpdate();
            while (_taskOnStayEntries.Count > 0)
            {
                for (int i = 0; i < _taskOnStayEntries.Count; i++)
                    ExecuteStayTask(_taskOnStayEntries[i], _taskOnStayPointers[i]);

                //Debug.Log("StayTaskRunnerCoroutine: Looping");
                
                if (_taskOnStayEntries.Count == 0)
                {
                    _taskOnStayCoroutine = null;
                    yield break;
                }

                yield return forFixedUpdate;
            }
        }
        
        #endregion Stay Task Runner Routine
    }
}
#endif