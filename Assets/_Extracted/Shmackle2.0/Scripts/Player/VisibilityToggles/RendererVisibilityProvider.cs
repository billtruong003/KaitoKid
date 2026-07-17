using System;
using UnityEngine;

namespace Shmackle.Player
{
    /// <summary>
    /// Monitors the visibility state of a renderer component and provides event-based notifications when visibility changes.
    /// Acts as a proxy between Unity's renderer visibility system and other components that need to react to visibility changes.
    /// </summary>
    
    [DisallowMultipleComponent]
    public class RendererVisibilityProvider : MonoBehaviour
    {
        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            private set
            {
                if (_isVisible == value) return;
                _isVisible = value;
                VisibilityChangedEvent?.Invoke(value);
            }
        }
        
        public event Action<bool> VisibilityChangedEvent;

        void OnBecameVisible()
        {
            IsVisible = true;
        }

        void OnBecameInvisible()
        {
            IsVisible = false;
        }
        
    }
}