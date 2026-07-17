using RootMotion.FinalIK;
using UnityEngine;

namespace Shmackle.Player
{
    /// <summary>
    /// Controls the visibility of a VRIK component by enabling or disabling it when
    /// the protobro renderer is not visible in the main player's camera view.
    /// </summary>
    /// <remarks>
    /// This class requires a VRIK component to function and listens for visibility
    /// change events. When the visibility state changes, it enables or disables
    /// the VRIK component accordingly.
    /// </remarks>
    [RequireComponent(typeof(VRIK))]
    public sealed class IKVisibilityController : VisibilityWatchBase
    {
        [SerializeField] private VRIK _vrik;
        
        private void Awake()
        {
            InitializeRendererVisibilityProvider();
            
            if (!_vrik)
                _vrik = GetComponent<VRIK>();
        }

        private protected override void OnVisibilityChangedEvent(bool isVisible)
        {
            if (_vrik) _vrik.enabled = isVisible;
        }
    }
}