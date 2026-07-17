using Fusion.XR.Shared.Core;
using Prefabs.Player.Temp;
using UnityEngine;

namespace Shmackle.Player
{
    /// <summary>
    /// Controls the visibility of a JiggleRig component by enabling or disabling it
    /// based on visibility changes detected by VisibilityWatchBase.
    /// </summary>
    /// <remarks>
    /// This class requires a JiggleRig component to function and listens for visibility
    /// change events. When the visibility state changes, it enables or disables
    /// the JiggleRig accordingly.
    /// </remarks>
    [RequireComponent(typeof(JiggleRigController))]
    public class JiggleRigVisibilityController : VisibilityWatchBase
    {
        [SerializeField] private JiggleRigController _jiggleRigController;

        private void Awake()
        {
            InitializeRendererVisibilityProvider();
            
            if (!_jiggleRigController)
                _jiggleRigController = GetComponent<JiggleRigController>();
        }

        private protected override void OnVisibilityChangedEvent(bool isVisible)
        {
            if (_jiggleRigController) _jiggleRigController.EnableJiggle(isVisible);
        }
    }
}