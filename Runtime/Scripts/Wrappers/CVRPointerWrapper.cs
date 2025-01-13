#if CVR_CCK_EXISTS
using ABI.CCK.Components;
using UnityEngine;

namespace NAK.AASEmulator.Runtime.Wrappers
{
    [AddComponentMenu("/")]
    [HelpURL(AASEmulatorCore.AAS_EMULATOR_GIT_URL)]
    public class CVRPointerWrapper : MonoBehaviour
    {
        #region Variables
        
        private CVRPointer _pointer;
        private int _typeHash;

        private int _emulatorRuntimeHash;
        
        #endregion Variables
        
        #region Unity Events
        
        private void Start()
        {
            if (!TryGetComponent(out _pointer))
            {
                Destroy(this);
                return;
            }
            _typeHash = _pointer.type.GetHashCode();
            
            // Ensure the pointer has a collider
            if (!TryGetComponent(out Collider _))
            {
                SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
                Vector3 lossyScale = transform.lossyScale;
                sphereCollider.radius = 0.00125f / Mathf.Max(Mathf.Max(lossyScale.x, lossyScale.y), lossyScale.z);
                sphereCollider.isTrigger = true;
            }
            
            // TEMP: Refactor AASEmulatorRuntime & AASEmulatorRemote to use a common base class
            IAASEmulatorAvatar runtimeAvatar = GetComponentInParent<IAASEmulatorAvatar>();
            if (runtimeAvatar != null) _emulatorRuntimeHash = runtimeAvatar.GetHashCode();
        }
        
        #endregion Unity Events
        
        #region Public Methods

        public int GetComputedTypeHash()
            => _typeHash;
        
        public bool CheckRuntimeHashMatch(int hash)
            => _emulatorRuntimeHash == hash;
        
        public float GetPointerValue()
            => _pointer.value;

        #endregion Public Methods
    }
}
#endif