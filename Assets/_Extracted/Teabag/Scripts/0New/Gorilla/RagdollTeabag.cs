using System;
using System.Collections;
using Fusion;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using UnityEngine;
using IAudioService = Teabag.Core.IAudioService;

namespace Teabag.Player
{
    public class RagdollTeabag : MonoBehaviour
    {
        [Header("Teabag Element")]
        [Tooltip("Attachment point on the ragdoll (hips bone). Lines originate here.")]
        [SerializeField] private Transform anchor;

        [Tooltip("Target transform for teabag (default is this object)")]
        [SerializeField] private Transform teabagTransform;

        [Tooltip("Renderer on testicle")]
        [SerializeField] private Renderer movementRenderer;
        [SerializeField] private Renderer staticRenderer;

        [Tooltip("Collider on testicle (used for grab range check)")]
        [SerializeField] private Collider teabagCollider;

        [SerializeField] private ParticleSystem ripVFX;
        [SerializeField] private AdvancedAudioClip ripSFX;
        [SerializeField] private Material _trailMaterial;

        [Tooltip("How close a hand must be to initiate grab")]
        [SerializeField] private float grabRange = 0.15f;
        [Tooltip("How far the hand must pull to trigger the rip")]
        [SerializeField] private float pullDistance = 0.3f;
        [Tooltip("How fast the snap-back animation plays")]
        [SerializeField] private float snapBackSpeed = 8f;

        // ── Events ───────────────────────────────────────────────────────────────
        public event Action<RagdollTeabag, Vector3> OnRipLocal;
        public event Action<RagdollTeabag> OnGrabLocal;
        public event Action<RagdollTeabag, float, Vector3> OnPullLocal;
        public event Action<RagdollTeabag> OnCancelLocal;
        public event Action OnRipVisual;

        // ── Teabag state (local only) ────────────────────────────────────────────
        public bool isRipped { get; private set; } = false;

        private Vector3 _grabStartPos;
        private Vector3 _defaultLocalPos;
        private Vector3 _defaultScale;

        // ── Line renderer (created at runtime) ───────────────────────────────────
        private LineRenderer _trailLine;

        // ── Grab/Snap state ──────────────────────────────────────────────────────
        private Grabber _grabbingGrabber;
        private bool _isGrabbingLeft;

        private bool _isSnappingBack = false;
        private float _grabInterpolation = 1f;
        private Vector3 _grabPosition;

        private IAudioService _audioService;
        private IGorillaService _localGorillaService;
        private IHardwareRig _localHardwareRig;
        private Gorilla _localGorrilla;

        private void Start()
        {
            if (teabagTransform == null) teabagTransform = transform;

            _defaultLocalPos = teabagTransform.localPosition;
            _defaultScale = teabagTransform.localScale;

            // Create line renderer for the trail
            _trailLine = CreateTrailLine("TeabagTrail");
            if (_trailLine) _trailLine.enabled = false;

            _audioService = ServiceLocator.Get<IAudioService>();

            if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                _localHardwareRig = rigInfo.HardwareRig;

            _localGorillaService ??= ServiceLocator.Get<IGorillaService>();
            _localGorrilla = (Gorilla)_localGorillaService.LocalGorilla;
        }

        LineRenderer CreateTrailLine(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(teabagTransform, false);

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = 0.005f;
            lr.endWidth = 0.003f;
            if (_trailMaterial != null)
                lr.sharedMaterial = _trailMaterial;
            lr.startColor = new Color(0.8f, 0.3f, 0.3f, 1f);
            lr.endColor = new Color(0.9f, 0.2f, 0.2f, 0.6f);
            lr.useWorldSpace = true;
            lr.enabled = false;
            return lr;
        }

        void UpdateLines()
        {
            if (!anchor || !teabagTransform || !_trailLine) return;
            _trailLine.SetPosition(0, anchor.position);
            _trailLine.SetPosition(1, teabagTransform.position);
        }

        private void OnDrawGizmosSelected()
        {
            if (!teabagTransform) return;
            var color = Color.red;
            color.a = 0.5f;

            Gizmos.color = color;
            Gizmos.DrawSphere(teabagTransform.position, grabRange);

            color = Color.blue;
            color.a = 0.5f;

            Gizmos.color = color;
            Gizmos.DrawSphere(teabagTransform.position, pullDistance);
        }

        private void Update()
        {
            if (isRipped || _isSnappingBack || anchor == null || teabagTransform == null) return;

            if (_localHardwareRig == null) return;

            if (_grabbingGrabber == null)
            {
                if (TryGrab(_localHardwareRig.LeftFollowerHand.Position, true)) return;
                TryGrab(_localHardwareRig.RightFollowerHand.Position, false);
            }
            else
            {
                // Check if grip is released → snap back
                if (!VRInputHandler.GetInputDown(_isGrabbingLeft, InputType.Grip))
                {
                    if (_grabbingGrabber != null && _grabbingGrabber.hand != null)
                    {
                        _grabbingGrabber.hand.isGrabbed = false;
                    }
                    _grabbingGrabber = null;
                    OnCancelLocal?.Invoke(this);
                    return;
                }

                // Track pull progress
                Vector3 handPos = _isGrabbingLeft ? _localHardwareRig.LeftFollowerHand.Position : _localHardwareRig.RightFollowerHand.Position;

                // Use GrabPoint if available to avoid hand overlap
                Vector3 targetHandPos = (_grabbingGrabber != null && _grabbingGrabber.GrabPoint != null) ? _grabbingGrabber.GrabPoint.position : handPos;

                // Use anchor as consistent reference for both visuals and ripping
                Vector3 defaultWorldPos = anchor.TransformPoint(_defaultLocalPos);
                float distance = Vector3.Distance(defaultWorldPos, targetHandPos);
                float progress = Mathf.Clamp01(distance / pullDistance);

                OnPullLocal?.Invoke(this, progress, targetHandPos);

                // Rip threshold reached
                if (distance >= pullDistance)
                {
                    if (_grabbingGrabber != null && _grabbingGrabber.hand != null)
                    {
                        _grabbingGrabber.hand.isGrabbed = false;
                    }
                    _grabbingGrabber = null;
                    OnRipLocal?.Invoke(this, teabagTransform.position);
                }
            }
        }

        public bool showLog;
        bool TryGrab(Vector3 handPosition, bool isLeft)
        {
            if (teabagTransform == null) return false;

            if (!VRInputHandler.GetInputDown(isLeft, InputType.Grip))
            {
                return false;
            }

            float dist = Vector3.Distance(handPosition, teabagTransform.position);
            if (showLog)
            {
                Debug.Log("Try Grab: " + (dist <= grabRange));
            }

            if (dist <= grabRange)
            {
                if (_localGorrilla)
                {
                    var hand = isLeft ? _localGorrilla.leftHand : _localGorrilla.rightHand;
                    if (hand.isGrabbed) return false;

                    _grabbingGrabber = hand.grabber;
                    hand.isGrabbed = true;
                }

                _isGrabbingLeft = isLeft;
                _grabStartPos = (_grabbingGrabber != null && _grabbingGrabber.GrabPoint != null) ? _grabbingGrabber.GrabPoint.position : handPosition;
                OnGrabLocal?.Invoke(this);
                return true;
            }
            return false;
        }

        // ── Visual methods (called by RPC handlers on ALL clients) ───────────────

        public void ShowGrab()
        {
            _grabInterpolation = 0f;
            _grabPosition = teabagTransform.position;
            if (staticRenderer != null && movementRenderer != null)
            {
                movementRenderer.enabled = true;
                staticRenderer.enabled = false;
            }

            if (_trailLine) _trailLine.enabled = true;
        }

        public void ShowPull(float progress, Vector3 handPosition)
        {
            if (!this.anchor || !teabagTransform) return;

            // Increment interpolation locally for smooth snap
            _grabInterpolation += Time.deltaTime * 10f;
            _grabInterpolation = Mathf.Clamp01(_grabInterpolation);

            // RESTING position in world space
            Vector3 defaultWorldPos = this.anchor.TransformPoint(_defaultLocalPos);

            // Direction from RESTING position to current hand position
            Vector3 dirToHand = (handPosition - defaultWorldPos);

            // Ideal position is the hand position, but clamped to pullDistance
            float distToHand = dirToHand.magnitude;
            float clampedDist = Mathf.Min(distToHand, pullDistance);
            Vector3 idealPos;

            // Handle edge case where hand is exactly at default position
            if (distToHand < 0.0001f)
            {
                idealPos = defaultWorldPos;
            }
            else
            {
                idealPos = defaultWorldPos + dirToHand.normalized * clampedDist;
            }

            if (_grabInterpolation < 1f)
            {
                teabagTransform.position = Vector3.Lerp(_grabPosition, idealPos, _grabInterpolation);
            }
            else
            {
                teabagTransform.position = idealPos;
            }

            // Keep default scale
            teabagTransform.localScale = _defaultScale;

            UpdateLines();
        }

        public void ShowRip(Vector3 position)
        {
            if (isRipped) return; // idempotent — prevents double VFX/SFX from direct call + RPC loopback
            isRipped = true;
            if (_trailLine) _trailLine.enabled = false;

            // Disable collider but keep renderer for despawn animation
            if (teabagCollider) teabagCollider.enabled = false;

            // Spawn VFX using pool
            if (ripVFX)
            {
                // Using fully qualified name to avoid ambiguity
                PoolObject.Get(ripVFX.gameObject, position, Quaternion.identity);
            }

            // Play SFX
            _audioService?.Play(ripSFX, position);

            // Notify parent ragdoll for immediate despawn
            OnRipVisual?.Invoke();
        }

        private IEnumerator ReleaseVFXCoroutine(GameObject vfxGo, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (vfxGo != null)
            {
                var po = vfxGo.GetComponent<Teabag.Core.PoolObject>();
                if (po != null)
                {
                    po.Return();
                }
                else
                {
                    // Fallback
                    Destroy(vfxGo);
                }
            }
        }

        public void ResetVisual()
        {
            // Trigger snap-back animation
            if (gameObject.activeInHierarchy && !isRipped)
            {
                StartCoroutine(SnapBackCoroutine());
            }
        }

        IEnumerator SnapBackCoroutine()
        {
            _isSnappingBack = true;
            if (_trailLine) _trailLine.enabled = true;

            float t = 0f;

            // Cache current positions/scales
            Vector3 startPos = teabagTransform ? teabagTransform.position : Vector3.zero;
            Vector3 startScale = teabagTransform ? teabagTransform.localScale : Vector3.one;

            while (t < 1f)
            {
                t += Time.deltaTime * snapBackSpeed;
                // Overshoot curve: goes past 1.0 then settles (elastic feel)
                float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
                // Add a slight bounce overshoot
                float bounce = ease > 0.8f ? 1f + Mathf.Sin((ease - 0.8f) * Mathf.PI * 5f) * 0.05f * (1f - ease) * 5f : ease;

                if (teabagTransform && anchor)
                {
                    Vector3 targetPos = anchor.TransformPoint(_defaultLocalPos);
                    teabagTransform.position = Vector3.Lerp(startPos, targetPos, bounce);
                    teabagTransform.localScale = Vector3.Lerp(startScale, _defaultScale, ease);
                }

                UpdateLines();
                yield return null;
            }

            // Ensure final state
            if (teabagTransform)
            {
                teabagTransform.localPosition = _defaultLocalPos;
                teabagTransform.localScale = _defaultScale;
            }

            // Reset color
            if (staticRenderer != null && movementRenderer != null)
            {
                movementRenderer.enabled = false;
                staticRenderer.enabled = true;
            }

            if (_trailLine) _trailLine.enabled = false;
            _isSnappingBack = false;
        }
    }
}
