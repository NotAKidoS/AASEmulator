#if CVR_CCK_EXISTS
using System;

namespace NAK.AASEmulator.Runtime
{
    public interface ITransformHider : IDisposable
    {
        bool IsActive { get; }
        bool IsValid { get; }
        bool IsHidden { get; }
        
        void HideTransform();
        void ShowTransform();
    }
}
#endif