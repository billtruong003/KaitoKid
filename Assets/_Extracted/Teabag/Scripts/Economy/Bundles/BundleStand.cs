using System;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Services;
using UnityEngine;

namespace Teabag.Economy
{
    /// <summary>
    /// Controller for a single bundle stand in the shop.
    /// Manages visibility based on countdown timers and first-time-user logic.
    /// Place this on the root GameObject of a bundle display alongside or
    /// above the existing <see cref="PackShelf"/> / <c>PackBuy</c> hierarchy.
    /// </summary>
    public sealed class BundleStand : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BundleConfig _config;

        [Header("References")]
        [Tooltip("The root GameObjects that contains all bundle visuals. " +
                 "Will be activated/deactivated based on timer state.")]
        [SerializeField] private GameObject[] _bundleRoots;

        [Tooltip("Optional countdown display component. If assigned, will " +
                 "receive the remaining time every update interval.")]
        [SerializeField] private BundleCountdownDisplay _countdownDisplay;

        [Header("Performance")]
        [Tooltip("How often (in seconds) to refresh the countdown. " +
                 "Lower values = smoother countdown but more work per frame.")]
        [SerializeField] private float _updateInterval = 0.5f;

        private IBundleService _timerService;
        private float _nextUpdateTime;
        private bool _hasInitialized;

        // ── Lifecycle ───────────────────────────────────────────────────────

        private void OnEnable()
        {
            var authService = ServiceLocator.Get<IAuthenticationService>();
            if (authService != null)
            {
                authService.OnLogin += OnPlayerLogin;

                // If the player is already fully logged in (e.g. scene reload), init now.
                if (authService.FullyLoggedIn)
                    OnPlayerLogin();
            }
        }

        private void OnDisable()
        {
            var authService = ServiceLocator.Get<IAuthenticationService>();
            if (authService != null)
            {
                authService.OnLogin -= OnPlayerLogin;
            }
        }

        private void Update()
        {
            if (!_hasInitialized || _timerService == null || !_timerService.IsInitialized)
                return;

            if (Time.time < _nextUpdateTime)
                return;

            _nextUpdateTime = Time.time + _updateInterval;
            RefreshState();
        }

        // ── Private ─────────────────────────────────────────────────────────

        private async void OnPlayerLogin()
        {
            if (_config == null)
            {
                GameLogger.Warning($"[BundleStand] '{name}' has no BundleConfig assigned.");
                SetVisible(false);
                return;
            }

            _timerService = ServiceLocator.Get<IBundleService>();

            if (_timerService == null)
            {
                GameLogger.Error($"[BundleStand] '{name}' failed to find IBundleService in ServiceLocator.");
                SetVisible(false);
                return;
            }

            try
            {
                await _timerService.InitializeAsync();
                _hasInitialized = true;
                RefreshState();
            }
            catch (Exception ex)
            {
                GameLogger.Error($"[BundleStand] '{name}' failed to initialize: {ex.Message}");
                SetVisible(false);
            }
        }

        private void RefreshState()
        {
            bool visible = _timerService.IsBundleVisible(_config);
            SetVisible(visible);

            if (visible)
            {
                if (_countdownDisplay != null)
                {
                    TimeSpan remaining = _timerService.GetRemainingTime(_config);
                    _countdownDisplay.SetRemainingTime(remaining);
                }
            }
            else
            {
                // Bundle is no longer visible (expired or not a target user).
                // Disable this component so Update() stops running, saving CPU cycles.
                this.enabled = false;
            }
        }

        private void SetVisible(bool visible)
        {
            if (_bundleRoots == null) return;
            
            for (int i = 0; i < _bundleRoots.Length; i++)
            {
                if (_bundleRoots[i] != null && _bundleRoots[i].activeSelf != visible)
                {
                    _bundleRoots[i].SetActive(visible);
                }
            }
        }
    }
}
