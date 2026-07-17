using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FullscreenHorrorFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material horrorMaterial;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    public Settings settings = new Settings();
    FullscreenHorrorPass _pass;

    public override void Create()
    {
        _pass = new FullscreenHorrorPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.horrorMaterial == null) return;
        renderer.EnqueuePass(_pass);
    }

    class FullscreenHorrorPass : ScriptableRenderPass
    {
        readonly Settings _settings;
        RTHandle _tempRT;

        public FullscreenHorrorPass(Settings settings)
        {
            _settings = settings;
            renderPassEvent = settings.renderPassEvent;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref _tempRT, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_HorrorTemp");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_settings.horrorMaterial == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("FullscreenHorror");
            var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            Blit(cmd, source, _tempRT);
            _settings.horrorMaterial.SetTexture("_MainTex", _tempRT);
            Blit(cmd, _tempRT, source, _settings.horrorMaterial);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd) { }

        public void Dispose()
        {
            _tempRT?.Release();
        }
    }
}
