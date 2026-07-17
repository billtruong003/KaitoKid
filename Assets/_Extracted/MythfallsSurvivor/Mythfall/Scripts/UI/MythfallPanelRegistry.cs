using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mythfall.UI
{
    /// <summary>
    /// Static UGUI panel registry — Mythfall's UGUI alternative to Bill.UI (which is
    /// UI Toolkit / VisualElement only). State machine and other consumers call
    /// <see cref="Show{T}"/> / <see cref="Hide{T}"/> by panel type; panels themselves
    /// register their MonoBehaviour instance via OnEnable / OnDisable (handled by
    /// <see cref="MythfallPanelBase"/>).
    ///
    /// Why static (not a MonoBehaviour singleton):
    ///   - No DontDestroyOnLoad GameObject to manage.
    ///   - desiredVisible dictionary survives scene unload — when MenuScene unloads
    ///     and GameplayScene's HudPanel registers in OnEnable, the registry already
    ///     knows InRunState wants HudPanel visible and applies it immediately.
    ///
    /// Lifecycle contract:
    ///   1. State.Enter() calls Show&lt;T&gt;() — sets desiredVisible[T] = true,
    ///      invokes Show on the panel instance if currently registered. If panel
    ///      not yet in the scene (mid scene-load), the desire is queued.
    ///   2. Panel scene-loads → Awake → OnEnable → Register&lt;T&gt;(this) →
    ///      registry sees desiredVisible[T] == true → calls panel.Show().
    ///   3. State.Exit() calls Hide&lt;T&gt;() — clears desire + hides instance.
    ///   4. Scene unloads → panel OnDisable → Unregister(typeof(T)).
    ///      desiredVisible state is preserved across the unload.
    ///
    /// Single-instance assumption: at most one panel of each Type is alive at a time.
    /// If two scenes simultaneously contain the same panel type, the second registration
    /// logs a warning and overwrites the first (last-wins).
    /// </summary>
    public static class MythfallPanelRegistry
    {
        static readonly Dictionary<Type, MythfallPanelBase> _instances = new(8);
        static readonly Dictionary<Type, bool> _desiredVisible = new(8);

        // ----- desired-state API (state machine + button handlers call these) -----

        public static void Show<T>() where T : MythfallPanelBase => SetDesired(typeof(T), true);
        public static void Hide<T>() where T : MythfallPanelBase => SetDesired(typeof(T), false);

        public static void Toggle<T>() where T : MythfallPanelBase
            => SetDesired(typeof(T), !IsShown<T>());

        public static bool IsShown<T>() where T : MythfallPanelBase
            => _desiredVisible.TryGetValue(typeof(T), out var v) && v;

        /// <summary>Hide every panel currently desired-visible. Used by states that own a clean slate (e.g. scene transitions).</summary>
        public static void HideAll()
        {
            // Snapshot keys before mutating
            var types = new List<Type>(_desiredVisible.Keys);
            foreach (var t in types) SetDesired(t, false);
        }

        // ----- panel-side API (called from MythfallPanelBase.OnEnable/OnDisable) -----

        public static void Register(MythfallPanelBase panel)
        {
            if (panel == null) return;
            var t = panel.GetType();

            if (_instances.TryGetValue(t, out var existing) && existing != panel)
            {
                Debug.LogWarning($"[PanelRegistry] {t.Name} re-registered while another instance was live — last-wins replace. Previous: {(existing != null ? existing.name : "<destroyed>")}");
            }

            _instances[t] = panel;

            // Apply desired state immediately so scene-loaded panels match what the state wants.
            bool desired = _desiredVisible.TryGetValue(t, out var v) && v;
            ApplyVisible(panel, desired);
        }

        public static void Unregister(MythfallPanelBase panel)
        {
            if (panel == null) return;
            var t = panel.GetType();

            // Only clear the slot if the unregistering panel is the currently-tracked one
            // (avoids late OnDisable from a destroyed predecessor wiping a fresh registration).
            if (_instances.TryGetValue(t, out var current) && current == panel)
                _instances.Remove(t);
        }

        public static T GetInstance<T>() where T : MythfallPanelBase
            => _instances.TryGetValue(typeof(T), out var p) ? (T)p : null;

        // ----- internal -----

        static void SetDesired(Type t, bool visible)
        {
            _desiredVisible[t] = visible;
            if (_instances.TryGetValue(t, out var panel) && panel != null)
                ApplyVisible(panel, visible);
        }

        static void ApplyVisible(MythfallPanelBase panel, bool visible)
        {
            if (visible) panel.InternalShow();
            else panel.InternalHide();
        }

        // ----- editor / test reset -----

#if UNITY_EDITOR
        [UnityEditor.InitializeOnEnterPlayMode]
        static void ResetOnEnterPlayMode()
        {
            // Without this, domain-reload-disabled play mode keeps stale dict entries
            // pointing at destroyed scene objects. Cheap defensive reset.
            _instances.Clear();
            _desiredVisible.Clear();
        }
#endif
    }
}
