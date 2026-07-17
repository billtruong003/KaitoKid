using Fusion.XR.Shared.Core;
using UnityEngine;

namespace Shmackle.Player
{
    /// <summary>
    /// An abstract base class that provides functionality for monitoring and responding to visibility states.
    /// Derived classes must implement the <c>OnVisibilityChangedEvent</c> method to define specific behaviors
    /// when visibility changes occur.
    /// </summary>
    public abstract class VisibilityWatchBase : MonoBehaviour
    {
        
        /// <summary>
        /// Reference to the RendererVisibilityProvider component.
        /// Can be assigned through the inspector to save overhead during runtime.
        /// If not assigned, it will be automatically found in Awake when InitializeRendererVisibilityProvider is called.
        /// </summary>
        [SerializeField]
        [Tooltip("Optional: Assign in inspector to save overhead. Will auto-reference in Awake if not assigned.")]
        private RendererVisibilityProvider rendererVisibilityProvider;

        /// <summary>
        /// Initializes the renderer visibility provider by finding it in the parent IRig component.
        /// Inheritors must call this method in their Awake method.
        /// </summary>
        private protected void InitializeRendererVisibilityProvider()
        {
            if (!rendererVisibilityProvider)
            {
                var rig = GetComponentInParent<IRig>();
                if (rig != null)
                    rendererVisibilityProvider = rig.gameObject.GetComponentInChildren<RendererVisibilityProvider>();
            }
        }
        
        private void OnEnable()
        {
            if (rendererVisibilityProvider)
                rendererVisibilityProvider.VisibilityChangedEvent += OnVisibilityChangedEvent;
        }

        private void OnDisable()
        {
            if (rendererVisibilityProvider)
                rendererVisibilityProvider.VisibilityChangedEvent -= OnVisibilityChangedEvent;
        }

        private protected abstract void OnVisibilityChangedEvent(bool isVisible);
    }
}