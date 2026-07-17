using RootMotion.FinalIK;
using UnityEngine;

namespace Shmackle.Player
{
    /// <summary>
    /// Controls the visibility of a FingerRig component by enabling or disabling it
    /// based on visibility changes detected by VisibilityWatchBase.
    /// </summary>
    /// <remarks>
    /// This class requires a FingerRig component to function and listens for visibility
    /// change events. When the visibility state changes, it enables or disables
    /// the FingerRig accordingly.
    /// </remarks>
    [RequireComponent(typeof(FingerRig))]
    public class FingerRigVisibilityController : VisibilityWatchBase
    {
        [SerializeField] private FingerRig _fingerRig;

        private void Awake()
        {
            InitializeRendererVisibilityProvider();
            
            if (!_fingerRig)
                _fingerRig = GetComponent<FingerRig>();
        }

        private protected override void OnVisibilityChangedEvent(bool isVisible)
        {
            if (_fingerRig) _fingerRig.enabled = isVisible;
        }
    }
}