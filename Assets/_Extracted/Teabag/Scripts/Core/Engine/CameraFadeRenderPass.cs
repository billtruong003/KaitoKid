using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Teabag.Core
{
    public class CameraFadeRenderPass : ScriptableRenderPass
    {
        private const string k_ProfilerTag = "Teabag CameraFade Overlay";
        private static readonly int OverlayColorId = Shader.PropertyToID("_OverlayColor");

        private readonly Material _material;

        private class PassData
        {
            public TextureHandle target;
            public Material material;
        }

        public CameraFadeRenderPass(Material material)
        {
            _material = material;
            // renderPassEvent is assigned by CameraFadeRenderFeature.Create() from
            // the serialized passEvent field after construction.
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_material == null) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            if (cameraData.isSceneViewCamera) return;

            var alpha = CameraFadeRenderFeature.Alpha;
            if (alpha <= 0.0001f) return;

            var colour = CameraFadeRenderFeature.Color;
            colour.a = Mathf.Clamp01(alpha);
            _material.SetColor(OverlayColorId, colour);

            var target = resourceData.activeColorTexture;

            using var builder = renderGraph.AddUnsafePass<PassData>(k_ProfilerTag, out var passData);
            passData.target = target;
            passData.material = _material;
            builder.UseTexture(target, AccessFlags.ReadWrite);

            builder.SetRenderFunc(static (PassData d, UnsafeGraphContext ctx) =>
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

                // Explicit RenderBufferLoadAction.Load is what fixes the green/red
                // UV-tile artifact on Quest 3. With the prior raster-pass + AccessFlags.ReadWrite
                // approach, RenderGraph hinted "load existing tile content" but
                // the Adreno driver silently fell back to DontCare in some
                // configs, so the SrcAlpha blend composited against
                // uninitialized tile memory (the green/red debug pattern).
                // SetRenderTarget with LoadAction.Load is unambiguous and
                // works on the system backbuffer too, so this stays correct
                // at passEvent 1000 (AfterRendering) where the active color
                // is the backbuffer and a copy/blit pattern cannot be used.
                cmd.SetRenderTarget(d.target,
                    RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
                CoreUtils.DrawFullScreen(cmd, d.material, shaderPassId: 0);
            });
        }
    }
}
