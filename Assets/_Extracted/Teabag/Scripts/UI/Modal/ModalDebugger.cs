#if UNITY_EDITOR
using UnityEngine;
using Squido.JungleXRKit.Core;

namespace Teabag.Core
{
    /// <summary>
    /// Debugger component for the Modal system.
    /// Provides hotkeys and mouse interaction for testing modals in the Unity Editor.
    /// Recommended: Attach this to the same GameObject as ModalManager.
    /// </summary>
    [AddComponentMenu("Teabag/UI/Modal Debugger")]
    public class ModalDebugger : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ModalManager _manager;

        private IModalService _modalService;

        private async void Start()
        {
            _modalService = await ServiceLocator.WaitForServiceAsync<IModalService>();

            if (_manager == null)
            {
                _manager = GetComponent<ModalManager>();
            }

            if (_manager == null)
            {
                GameLogger.Warning("[ModalDebugger] ModalManager reference is missing and could not be found on the same GameObject.");
            }
        }

        private void Update()
        {
            if (_modalService == null || _manager == null) return;

            // Key B: Dismiss All
            if (UnityEngine.Input.GetKeyDown(KeyCode.B))
            {
                _modalService.DismissAll();
                GameLogger.Info($"[ModalDebugger] Debug DismissAll - Active: {_manager.ActiveModalCount}, Pooled: {_manager.PooledModalCount}");
            }

            // Key V: Test Re-entrancy / Chaining
            if (UnityEngine.Input.GetKeyDown(KeyCode.V))
            {
                _modalService.Show(ModalRequest.Alert("First", "Dismiss this via key 'B' to test chained modal re-entrancy.", () => {
                    _modalService.Show(ModalRequest.Alert("Chained", "If you see this, re-entrancy is working!"));
                }));
            }

            // Keys M/N: Spawn Random Modals with Animation Toggling
            if (UnityEngine.Input.GetKeyDown(KeyCode.M) || UnityEngine.Input.GetKeyDown(KeyCode.N))
            {
                if (_manager.Config != null)
                {
                    if (UnityEngine.Input.GetKeyDown(KeyCode.M))
                        _manager.Config.EditorSetAnimationType(ModalAnimationType.Slide);
                    else
                        _manager.Config.EditorSetAnimationType(ModalAnimationType.ScaleAndFade);
                }

                int rand = UnityEngine.Random.Range(0, 3);
                ModalRequest req;

                if (rand == 0)
                {
                    string[] titles = { "Are you sure?", "Confirm Action", "Abandon Match?" };
                    string[] bodies = {
                        "Do you really want to delete this item?",
                        "You are about to spend 500 Coins. Proceed?",
                        "Leaving the match now will result in a penalty. Are you sure?"
                    };
                    string title = titles[UnityEngine.Random.Range(0, titles.Length)];
                    string body = bodies[UnityEngine.Random.Range(0, bodies.Length)];

                    req = ModalRequest.Confirmation(
                        title,
                        body,
                        (result) => GameLogger.Info($"Debug Confirm: {result}"));
                }
                else if (rand == 1)
                {
                    string[] titles = { "Critical Danger", "Transaction Failed", "Connection Lost" };
                    string[] bodies = {
                        "Your health is extremely low. Heal immediately to avoid passing out.",
                        "Not enough currency to purchase this item.",
                        "Please check your internet connection and try again."
                    };
                    string title = titles[UnityEngine.Random.Range(0, titles.Length)];
                    string body = bodies[UnityEngine.Random.Range(0, bodies.Length)];

                    req = ModalRequest.Alert(
                        title,
                        body,
                        () => GameLogger.Info("Debug Alert dismissed."));
                }
                else
                {
                    string[] titles = { "Game Saved", "Loot Found", "Level Up!", "Daily Quest" };
                    string[] bodies = {
                        "Your progress has been synchronized with the server.",
                        "You have found a Rare Banana bunch!",
                        "You reached Level 15! New cosmetics unlocked.",
                        "You've completed your daily challenges."
                    };
                    string title = titles[UnityEngine.Random.Range(0, titles.Length)];
                    string body = bodies[UnityEngine.Random.Range(0, bodies.Length)];

                    req = ModalRequest.Info(
                        title,
                        body + "\n\n(Auto dismiss in 4s)",
                        4f,
                        () => GameLogger.Info("Debug Info auto-dismissed."));
                }

                _modalService.Show(req);
            }

            // Debug Raycast to click buttons with mouse
            if (_manager.ActiveModalCount > 0 && UnityEngine.Input.GetMouseButtonDown(0))
            {
                if (Camera.main != null)
                {
                    Ray ray = Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
                    RaycastHit[] hits = Physics.RaycastAll(ray, 100f, Physics.AllLayers, QueryTriggerInteraction.Collide);
                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                    foreach (var hit in hits)
                    {
                        ModalButton btn = hit.collider.GetComponentInParent<ModalButton>();
                        if (btn != null)
                        {
                            Modal parentModal = btn.GetComponentInParent<Modal>();
                            if (parentModal != null && parentModal.CanInteract)
                            {
                                btn.OnPress();
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
#endif
