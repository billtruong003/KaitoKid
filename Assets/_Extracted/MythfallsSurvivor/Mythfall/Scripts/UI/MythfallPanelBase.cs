using UnityEngine;

namespace Mythfall.UI
{
    /// <summary>
    /// Base class for all Mythfall UGUI panels. Auto-registers with
    /// <see cref="MythfallPanelRegistry"/> on enable, unregisters on disable.
    ///
    /// Visibility model: CanvasGroup-based, NOT GameObject-active toggle.
    ///   - Panel root GameObject is ALWAYS active in the scene.
    ///   - Show/Hide manipulates CanvasGroup.alpha + interactable + blocksRaycasts.
    ///   - This keeps OnEnable/OnDisable tied to scene lifetime, not visibility,
    ///     so the panel stays registered even while hidden — required for the
    ///     state machine's "Show&lt;T&gt;() finds and shows the instance" contract.
    ///
    /// Required component: CanvasGroup (auto-added via [RequireComponent]).
    ///
    /// Concrete panels:
    ///   - Override <see cref="OnPanelShown"/> for subscribe + initial-state sync.
    ///   - Override <see cref="OnPanelHidden"/> for unsubscribe.
    ///   - Wire button onClick handlers in <see cref="MonoBehaviour"/>'s Awake using [SerializeField] refs.
    ///
    /// Visibility flow:
    ///   - State.Enter() → MythfallPanelRegistry.Show&lt;ThisPanel&gt;()
    ///   - Registry → InternalShow() → alpha=1 → OnPanelShown()
    ///   - State.Exit() → MythfallPanelRegistry.Hide&lt;ThisPanel&gt;()
    ///   - Registry → InternalHide() → OnPanelHidden() → alpha=0
    ///
    /// Initial state: panels load HIDDEN by default (desired state = false until
    /// a state asks for them). The state machine flips them on as needed. If a
    /// panel's state is the active one when the scene loads, the registry applies
    /// the show immediately on register.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class MythfallPanelBase : MonoBehaviour
    {
        CanvasGroup _canvasGroup;
        bool _isShown;

        public bool IsShown => _isShown;

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            // Default to hidden so first-frame render doesn't flash visible content
            // before MythfallPanelRegistry.Register decides what to do.
            ApplyVisible(false);
        }

        protected virtual void OnEnable() => MythfallPanelRegistry.Register(this);
        protected virtual void OnDisable() => MythfallPanelRegistry.Unregister(this);

        // Called by registry — never call directly from concrete panels.
        internal void InternalShow()
        {
            if (_isShown) return;
            _isShown = true;
            ApplyVisible(true);
            OnPanelShown();
        }

        internal void InternalHide()
        {
            if (!_isShown)
            {
                // Always reapply CanvasGroup state — covers the initial Register call
                // when desired = false (we want alpha synced, but no OnPanelHidden hook).
                ApplyVisible(false);
                return;
            }
            // Fire OnPanelHidden BEFORE alpha=0 so subscribers can read current values.
            OnPanelHidden();
            _isShown = false;
            ApplyVisible(false);
        }

        void ApplyVisible(bool visible)
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }

        /// <summary>Hook for subscribe + per-show state refresh. Default: no-op.</summary>
        protected virtual void OnPanelShown() { }

        /// <summary>Hook for unsubscribe + cleanup. Default: no-op.</summary>
        protected virtual void OnPanelHidden() { }
    }
}
