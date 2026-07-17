// Assets/Scripts/Economy/Cosmetics/VRMirror.cs
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Teabag.Customization
{
    /// <summary>
    /// Static mirror — fixed camera in scene renders into an RT, which is mapped 1:1
    /// onto the mirror quad via mesh UV (no parallax when viewer rotates).
    ///
    /// Gating is handled by ColourPickerZone enabling/disabling this GameObject.
    /// When outside the zone, LateUpdate doesn't tick → zero cost.
    ///
    /// Additional visibility gating: render only when the player head is within
    /// _maxDistance AND the mirror lies inside the head's view cone. Skips the
    /// expensive Camera.Render() entirely when the player isn't looking at it.
    ///
    /// Quest perf strips applied:
    ///   - allowXRRendering = false (mono render, avoids 2x stereo cost)
    ///   - No shadows / post / HDR / MSAA
    ///   - RGB565 RT (half bandwidth vs ARGB32)
    ///   - 1/2 frame throttle (36Hz on Quest 72Hz, invisible)
    /// </summary>
    public sealed class VRMirror : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Camera placed statically in scene, aimed at the player area. Must be disabled.")]
        [SerializeField] private Camera _mirrorCamera;

        [Tooltip("Quad/Plane renderer with ToonMirror material.")]
        [SerializeField] private Renderer _mirrorRenderer;
        [SerializeField] private int _mirrorMaterialIndex;

        [Header("Render Texture")]
        [SerializeField] private Vector2Int _rtSize = new Vector2Int(512, 512);

        [Header("Throttle")]
        [Tooltip("1 = every frame, 2 = half rate (~36Hz on Quest 72Hz, invisible to user).")]
        [SerializeField, Range(1, 4)] private int _renderEveryNFrames = 2;

        [Header("Visibility Gating")]
        [Tooltip("Skip rendering when the player head is farther than this from the mirror.")]
        [SerializeField] private float _maxDistance = 12f;

        [Tooltip("Full cone angle (deg) centred on the player's head forward. Render only when the mirror is inside this cone. 360 = always render regardless of facing.")]
        [SerializeField, Range(60f, 360f)] private float _viewAngleDegrees = 200f;

        private RenderTexture _rt;
        private MaterialPropertyBlock _mpb;
        private int _frameCounter;
        private float _minViewDot;
        private Transform _mirrorAnchor;
        private IGorillaService _gorillaService;
        private bool _wasInView;
        private static readonly int PropMainTex = Shader.PropertyToID("_MainTex");

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            _mirrorAnchor = _mirrorRenderer ? _mirrorRenderer.transform : transform;
            _minViewDot = Mathf.Cos(_viewAngleDegrees * 0.5f * Mathf.Deg2Rad);

            // RGB565 = 16bpp, half the bandwidth of ARGB32. Mirror is opaque, no alpha needed.
            _rt = new RenderTexture(_rtSize.x, _rtSize.y, 16, RenderTextureFormat.RGB565)
            {
                name = $"{name}_MirrorRT",
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            _rt.Create();

            if (_mirrorCamera)
            {
                _mirrorCamera.enabled = false;
                _mirrorCamera.targetTexture = _rt;
                _mirrorCamera.allowHDR = false;
                _mirrorCamera.allowMSAA = false;
                _mirrorCamera.useOcclusionCulling = false;

                var urp = _mirrorCamera.GetUniversalAdditionalCameraData();
                if (urp != null)
                {
                    urp.renderShadows = false;
                    urp.renderPostProcessing = false;
                    urp.antialiasing = AntialiasingMode.None;
                    urp.requiresColorOption = CameraOverrideOption.Off;
                    urp.requiresDepthOption = CameraOverrideOption.Off;
                    urp.allowXRRendering = false; // force mono, avoid 2x stereo cost
                }
            }

            if (_mirrorRenderer)
            {
                _mirrorRenderer.GetPropertyBlock(_mpb, _mirrorMaterialIndex);
                _mpb.SetTexture(PropMainTex, _rt);
                _mirrorRenderer.SetPropertyBlock(_mpb, _mirrorMaterialIndex);
            }
        }

        private void LateUpdate()
        {
            if (!_mirrorCamera) return;

            if (!IsInPlayerView())
            {
                _wasInView = false;
                return;
            }

            // First frame back inside the cone: render immediately so the user doesn't see a
            // stale image for up to _renderEveryNFrames frames, and resync the throttle rhythm.
            if (!_wasInView)
            {
                _wasInView = true;
                _frameCounter = 0;
                _mirrorCamera.Render();
                return;
            }

            _frameCounter++;
            if (_frameCounter % _renderEveryNFrames != 0) return;

            _mirrorCamera.Render();
        }

        private bool IsInPlayerView()
        {
            var headset = GetLocalHeadset();
            // Fail open: if the rig hasn't spawned yet, keep rendering so editor previews / first-frame
            // pose still work. Once the service is populated, the gate becomes active.
            if (headset == null) return true;

            Vector3 delta = _mirrorAnchor.position - headset.Position;
            float sqrDist = delta.sqrMagnitude;
            if (sqrDist > _maxDistance * _maxDistance) return false;
            if (_viewAngleDegrees >= 360f) return true;
            if (sqrDist < 1e-4f) return true;

            Vector3 toMirror = delta / Mathf.Sqrt(sqrDist);
            return Vector3.Dot(headset.ForwardVector, toMirror) >= _minViewDot;
        }

        private IHardwareHeadset GetLocalHeadset()
        {
            if (_gorillaService == null)
                ServiceLocator.TryGet<IGorillaService>(out _gorillaService);
            if (_gorillaService == null) return null;

            var rig = _gorillaService.LocalGorillaRig;
            return rig ? rig.Headset : null;
        }

        private void OnValidate()
        {
            _minViewDot = Mathf.Cos(_viewAngleDegrees * 0.5f * Mathf.Deg2Rad);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            var origin = _mirrorRenderer ? _mirrorRenderer.transform.position : transform.position;
            Gizmos.DrawWireSphere(origin, _maxDistance);
        }

        private void OnDestroy()
        {
            if (_rt)
            {
                _rt.Release();
                Destroy(_rt);
                _rt = null;
            }
        }
    }
}