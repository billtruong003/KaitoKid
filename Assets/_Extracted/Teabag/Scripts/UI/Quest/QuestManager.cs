using System.Collections;
using System.Collections.Generic;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using UnityEngine;

namespace Teabag.UI.Quest
{
    using IAudioService = Teabag.Core.IAudioService;

    /// <summary>
    /// Manager for quest completion notifications using Canvas Overlay.
    /// Polls the QuestService for pending completions and instantiates stacking toasts.
    /// Only active when the player is in the lobby or battle (HasLocalGorilla).
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private QuestNotificationToast _toastPrefab;
        [SerializeField] private Transform _toastContainer;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _anchorPoint;
        [SerializeField] private QuestConfig _config;

        [Header("Audio")]
        [SerializeField] private AdvancedAudioClip _completionSfx;

        private IQuestService _questService;
        private IGorillaService _gorillaService;
        private IAudioService _audioService;

        private Transform _originalParent;
        private Transform _currentHead;

        private readonly List<QuestNotificationToast> _activeToasts = new List<QuestNotificationToast>();

        private WaitForSeconds _waitNextNotification;
        private WaitForSeconds _waitPollShort;
        private WaitForSeconds _waitPollLong;
        private WaitForSeconds _waitSlideIn;

        private readonly Stack<QuestNotificationToast> _pool = new Stack<QuestNotificationToast>();

        private async void Start()
        {
            _questService = await ServiceLocator.WaitForServiceAsync<IQuestService>();
            _gorillaService = await ServiceLocator.WaitForServiceAsync<IGorillaService>();
            _audioService = ServiceLocator.Get<IAudioService>();

            if (_canvas == null)
            _canvas = GetComponent<Canvas>() ?? GetComponentInChildren<Canvas>();

            _originalParent = transform;

            if (_config == null)
            {
                GameLogger.Error("[QuestNotification] QuestConfig is missing! Notification manager will skip processing.");
                return;
            }

            // Cache wait objects to avoid GC allocations during polling
            _waitNextNotification = new WaitForSeconds(_config.NextNotificationDelay);
            _waitPollShort = new WaitForSeconds(0.5f);
            _waitPollLong = new WaitForSeconds(1.0f);
            _waitSlideIn = new WaitForSeconds(_config.SlideInDuration);

            StartCoroutine(PollNotificationsCoroutine());
        }

#if UNITY_EDITOR
        private void Update()
        {
            // Debug: Press P to simulate 3 quest completions for testing the UI queue and stacking
            if (Input.GetKeyDown(KeyCode.P))
            {
                _questService?.DebugEnqueueCompletion("Debug Quest A");
                _questService?.DebugEnqueueCompletion("Debug Quest B");
                _questService?.DebugEnqueueCompletion("Debug Quest C");
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                _questService?.ReportProgressAsync("daily_login");
                _questService?.ReportProgressAsync("play_one_game");
                _questService?.ReportProgressAsync("win_public_game");
            }
        }
#endif

        private IEnumerator PollNotificationsCoroutine()
        {
            yield return _waitPollLong;

            while (true)
            {
                // Only show if in a "showable" state (Lobby or Battle)
                if (_gorillaService != null && _gorillaService.HasLocalGorilla)
                {
                    // Handle Head-Locked World Space UI
                    var rigInfoService = ServiceLocator.Get<IRigInfoService>();
                    var hardwareRig = rigInfoService?.HardwareRig;
                    if (hardwareRig != null && hardwareRig.Headset != null && hardwareRig.Headset.HeadsetTransform != null)
                    {
                        if (_currentHead != hardwareRig.Headset.HeadsetTransform)
                        {
                            _currentHead = hardwareRig.Headset.HeadsetTransform;
                            if (_canvas != null)
                            {
                                _canvas.renderMode = RenderMode.WorldSpace;
                                _canvas.transform.SetParent(_currentHead);
                                _canvas.transform.localPosition = new Vector3(0, 0, _config.WorldSpaceDistance); // Configured distance
                                _canvas.transform.localRotation = Quaternion.identity;
                                _canvas.transform.localScale = Vector3.one * _config.WorldSpaceScale; // Configured scaling
                            }
                        }
                    }
                    else if (_currentHead != null)
                    {
                        // Gorilla/Camera destroyed, revert to original parent
                        ResetCanvasParent();
                    }

                    var snapshot = _questService.DequeueCompletionNotification();
                    if (snapshot.HasValue)
                    {
                        ShowToast(snapshot.Value);

                        // Wait for the slide-in animation of the current toast to finish
                        yield return _waitSlideIn;

                        // Delay between consecutive notifications as per config
                        yield return _waitNextNotification;
                    }
                    else
                    {
                        yield return _waitPollShort; // Nothing to show, wait a bit
                    }
                }
                else
                {
                    yield return _waitPollLong; // Not in world yet, check less frequently
                }
            }
        }

        private void ShowToast(QuestSnapshot snapshot)
        {
            if (_toastPrefab == null) return;

            GameLogger.Info($"[QuestManager] Showing toast for quest: {snapshot.Name}");

            // Push existing toasts up by the vertical spacing in config
            for (int i = _activeToasts.Count - 1; i >= 0; i--)
            {
                if (_activeToasts[i] == null)
                {
                    _activeToasts.RemoveAt(i);
                    continue;
                }
                _activeToasts[i].PushUp();
            }

            // Create or recycle toast instance
            QuestNotificationToast toast;
            if (_pool.Count > 0)
            {
                toast = _pool.Pop();
                toast.gameObject.SetActive(true);
            }
            else
            {
                toast = Instantiate(_toastPrefab, _toastContainer);
            }

            toast.Initialize(snapshot, _config, this, _anchorPoint != null ? _anchorPoint.anchoredPosition : Vector2.zero);
            _activeToasts.Add(toast);

            // Play completion sound effect
            if (_completionSfx != null && _audioService != null)
                _audioService.Play(_completionSfx);
        }

        /// <summary>
        /// Recycles a toast instance back into the pool.
        /// </summary>
        public void ReturnToPool(QuestNotificationToast toast)
        {
            if (toast == null) return;

            toast.gameObject.SetActive(false);
            _activeToasts.Remove(toast);
            _pool.Push(toast);
        }

        private void ResetCanvasParent()
        {
            if (_canvas != null)
            {
                _canvas.transform.SetParent(_originalParent);
                _canvas.transform.localPosition = Vector3.zero;
                _canvas.transform.localRotation = Quaternion.identity;
                _canvas.transform.localScale = Vector3.one;
            }
            _currentHead = null;
        }
    }
}
