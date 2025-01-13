#if CVR_CCK_EXISTS
using UnityEngine;

namespace NAK.AASEmulator.Runtime.Debugging
{
    [AddComponentMenu("/")]
    [HelpURL(AASEmulatorCore.AAS_EMULATOR_GIT_URL)]
    public class NewHiderInit : MonoBehaviour
    {
        private void Start()
        {
            TransformHiderUtils.SetupAvatar(gameObject);
        }
    }
}
#endif