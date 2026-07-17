using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Teabag.Core
{
    /// <summary>
    /// URP ScriptableRendererFeature that draws a full-screen colour overlay for VR
    /// fade-to-black transitions. Add this feature to the active URP Renderer asset.
    /// Driven by <see cref="CameraFade"/> which writes to the static
    /// <see cref="Color"/> and <see cref="Alpha"/> properties each frame during a fade.
    /// </summary>
    public class CameraFadeRenderFeature : ScriptableRendererFeature
    {
        [Tooltip("Overlay shader. Leave null to auto-resolve Hidden/Teabag/CameraFade.")]
        public Shader overlayShader;

        [Tooltip("Render pass event. AfterRendering draws on top of everything, including UI.")]
        public RenderPassEvent passEvent = RenderPassEvent.AfterRendering;

        [Tooltip("Global multiplier applied on top of every FadeIn/FadeOut speed argument. " +
                 "1 = use the caller's speed verbatim; 2 = all fades take twice as long; " +
                 "0.5 = all fades take half as long. Tune this if fades feel too snappy or too slow on device.")]
        [Min(0.01f)]
        public float fadeDurationMultiplier = 1f;

        public static CameraFadeRenderFeature Instance { get; private set; }

        // Overlay starts opaque black so the first scene holds fade-to-black until
        // CameraFade performs the initial FadeIn.
        public static Color Color = UnityEngine.Color.black;
        public static float Alpha = 1f;

        // Cached from the serialized field in Create(). CameraFade reads this each
        // fade to scale the caller-supplied duration. Kept as a static so the
        // animation loop doesn't need to resolve Instance on every frame.
        public static float DurationMultiplier { get; private set; } = 1f;

        private Material _material;
        private CameraFadeRenderPass _pass;

        public override void Create()
        {
            if (overlayShader == null)
                overlayShader = Shader.Find("Hidden/Teabag/CameraFade");

            if (overlayShader == null)
            {
                Debug.LogError("[CameraFadeRenderFeature] Overlay shader 'Hidden/Teabag/CameraFade' " +
                               "is missing. Assign it on this render feature OR add it to " +
                               "Project Settings > Graphics > Always Included Shaders.");
                return;
            }

            if (!overlayShader.isSupported)
            {
                Debug.LogError($"[CameraFadeRenderFeature] Overlay shader '{overlayShader.name}' is not " +
                               "supported on this GPU/graphics API. Fade overlay will not render.");
                return;
            }

            if (_material == null)
                _material = CoreUtils.CreateEngineMaterial(overlayShader);

            _pass = new CameraFadeRenderPass(_material) { renderPassEvent = passEvent };
            Instance = this;
            DurationMultiplier = Mathf.Max(0.01f, fadeDurationMultiplier);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            DurationMultiplier = Mathf.Max(0.01f, fadeDurationMultiplier);
        }
#endif

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);
            _material = null;
            if (Instance == this) Instance = null;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_material == null) return;
            if (Alpha <= 0.0001f) return;
            renderer.EnqueuePass(_pass);
        }
    }
}
