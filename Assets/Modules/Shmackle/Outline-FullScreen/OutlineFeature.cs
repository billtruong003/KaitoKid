using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class OutlineFeature : ScriptableRendererFeature
{
    public static event Action<RasterCommandBuffer, LayerMask> OnRenderFoliageMask;

    class LayerMaskPass : ScriptableRenderPass
    {
        private Material maskMaterial;
        private LayerMask layerMask;
        private FilteringSettings filteringSettings;
        private readonly ShaderTagId[] shaderTags;
        private string profilerTag;
        private string textureName;

        public TextureHandle MaskTexture { get; private set; }

        public LayerMaskPass(string tag, string texName)
        {
            profilerTag = tag;
            textureName = texName;
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            shaderTags = new ShaderTagId[]
            {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("LightweightForward")
            };
            filteringSettings = new FilteringSettings(RenderQueueRange.all);
        }

        public void Setup(LayerMask mask)
        {
            this.layerMask = mask;
            filteringSettings.layerMask = mask;
            if (maskMaterial == null) maskMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Outline/SelectionMask"));
        }

        private class MaskData
        {
            public RendererListHandle rendererList;
            public TextureHandle maskDest;
            public LayerMask layerMask;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            MaskTexture = TextureHandle.nullHandle;
            if (maskMaterial == null || layerMask == 0) return;

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.colorFormat = RenderTextureFormat.R8;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            TextureDesc texDesc = new TextureDesc(desc)
            {
                name = textureName,
                clearBuffer = true,
                clearColor = Color.black
            };

            MaskTexture = renderGraph.CreateTexture(texDesc);
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle depthTexture = resourceData.activeDepthTexture;

            RendererListParams rlParams = new RendererListParams(
                renderingData.cullResults,
                new DrawingSettings(shaderTags[0], new SortingSettings(cameraData.camera))
                {
                    overrideMaterial = maskMaterial,
                    overrideMaterialPassIndex = 0,
                    enableInstancing = true
                },
                filteringSettings
            );

            for (int i = 1; i < shaderTags.Length; ++i)
                rlParams.drawSettings.SetShaderPassName(i, shaderTags[i]);

            RendererListHandle rendererList = renderGraph.CreateRendererList(rlParams);

            using (var builder = renderGraph.AddRasterRenderPass<MaskData>(profilerTag, out var passData))
            {
                passData.rendererList = rendererList;
                passData.maskDest = MaskTexture;
                passData.layerMask = this.layerMask;

                builder.UseRendererList(passData.rendererList);
                builder.SetRenderAttachment(passData.maskDest, 0, AccessFlags.Write);
                if (depthTexture.IsValid()) builder.SetRenderAttachmentDepth(depthTexture, AccessFlags.Read);

                builder.SetRenderFunc((MaskData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.rendererList);
                    OnRenderFoliageMask?.Invoke(context.cmd, data.layerMask);
                });
            }
        }
        public void Dispose() { CoreUtils.Destroy(maskMaterial); }
    }

    class OutlinePass : ScriptableRenderPass
    {
        private Material material;
        private OutlineVolume volumeSettings;
        private const string ShaderName = "Hidden/FullScreen/Outline";

        private static readonly int ThicknessID = Shader.PropertyToID("_Thickness");
        private static readonly int ColorID = Shader.PropertyToID("_OutlineColor");
        private static readonly int DepthThresholdSqID = Shader.PropertyToID("_DepthThresholdSq");
        private static readonly int NormalThresholdSqID = Shader.PropertyToID("_NormalThresholdSq");
        private static readonly int ColorThresholdSqID = Shader.PropertyToID("_ColorThresholdSq");
        private static readonly int DebugModeID = Shader.PropertyToID("_DebugMode");
        private static readonly int SelectionMaskID = Shader.PropertyToID("_SelectionMaskTexture");
        private static readonly int OcclusionMaskID = Shader.PropertyToID("_OcclusionMaskTexture");
        private static readonly int FadeParamsID = Shader.PropertyToID("_FadeParams");

        private LayerMaskPass selectionMaskPass;
        private LayerMaskPass occlusionMaskPass;

        private class PassData
        {
            public Material material;
            public TextureHandle source;
            public TextureHandle destination;
            public TextureHandle mask;
            public TextureHandle occlusion;
        }

        public OutlinePass() { renderPassEvent = RenderPassEvent.AfterRenderingTransparents; }

        public void SetupReference(LayerMaskPass selectPass, LayerMaskPass occludePass)
        {
            this.selectionMaskPass = selectPass;
            this.occlusionMaskPass = occludePass;
        }

        private bool UpdateMaterial()
        {
            var stack = VolumeManager.instance.stack;
            volumeSettings = stack.GetComponent<OutlineVolume>();
            if (volumeSettings == null || !volumeSettings.IsActive()) return false;
            if (material == null) material = CoreUtils.CreateEngineMaterial(Shader.Find(ShaderName));
            if (material == null) return false;

            material.SetFloat(ThicknessID, volumeSettings.thickness.value);
            material.SetColor(ColorID, volumeSettings.outlineColor.value);

            float dT = volumeSettings.depthThreshold.value;
            float nT = volumeSettings.normalThreshold.value;
            float cT = volumeSettings.colorThreshold.value;

            material.SetFloat(DepthThresholdSqID, dT * dT);
            material.SetFloat(NormalThresholdSqID, nT * nT);
            material.SetFloat(ColorThresholdSqID, cT * cT);
            material.SetInt(DebugModeID, (int)volumeSettings.debugMode.value);

            float fadeDistRange = Mathf.Max(0.001f, volumeSettings.fadeDistanceEnd.value - volumeSettings.fadeDistanceStart.value);
            float fadeHeightRange = Mathf.Max(0.001f, volumeSettings.fadeHeightMax.value - volumeSettings.fadeHeightMin.value);

            material.SetVector(FadeParamsID, new Vector4(
                volumeSettings.fadeDistanceStart.value,
                1.0f / fadeDistRange,
                volumeSettings.fadeHeightMin.value,
                1.0f / fadeHeightRange
            ));

            SetKeyword("USE_DEPTH", volumeSettings.useDepth.value);
            SetKeyword("USE_NORMALS", volumeSettings.useNormals.value);
            SetKeyword("USE_COLOR", volumeSettings.useColor.value);
            SetKeyword("ALGO_SOBEL", volumeSettings.algorithm.value == OutlineVolume.OutlineAlgorithm.Sobel);
            SetKeyword("ALGO_ROBERTS", volumeSettings.algorithm.value == OutlineVolume.OutlineAlgorithm.RobertsCross);
            SetKeyword("USE_DISTANCE_FADE", volumeSettings.useDistanceFade.value);
            SetKeyword("USE_HEIGHT_FADE", volumeSettings.useHeightFade.value);

            var mode = volumeSettings.mode.value;
            SetKeyword("OUTLINE_FULL", mode == OutlineVolume.OutlineMode.FullScreen);
            SetKeyword("OUTLINE_SELECTION", mode == OutlineVolume.OutlineMode.SelectionOnly);
            SetKeyword("OUTLINE_MIXED", mode == OutlineVolume.OutlineMode.Mixed);

            return true;
        }

        private void SetKeyword(string k, bool v) { if (v) material.EnableKeyword(k); else material.DisableKeyword(k); }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!UpdateMaterial()) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (cameraData.cameraType == CameraType.Preview) return;

            TextureHandle source = resourceData.activeColorTexture;
            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            TextureDesc texDesc = new TextureDesc(desc)
            {
                name = "OutlineTemp",
                clearBuffer = true,
                clearColor = Color.black
            };

            TextureHandle tempTexture = renderGraph.CreateTexture(texDesc);
            TextureHandle maskHandle = (selectionMaskPass != null) ? selectionMaskPass.MaskTexture : TextureHandle.nullHandle;
            TextureHandle occlusionHandle = (occlusionMaskPass != null) ? occlusionMaskPass.MaskTexture : TextureHandle.nullHandle;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Outline Composite", out var passData))
            {
                passData.material = material;
                passData.source = source;
                passData.destination = tempTexture;
                passData.mask = maskHandle;
                passData.occlusion = occlusionHandle;

                builder.UseTexture(passData.source, AccessFlags.Read);
                if (passData.mask.IsValid()) builder.UseTexture(passData.mask, AccessFlags.Read);
                if (passData.occlusion.IsValid()) builder.UseTexture(passData.occlusion, AccessFlags.Read);

                builder.SetRenderAttachment(passData.destination, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    if (data.mask.IsValid()) data.material.SetTexture(SelectionMaskID, data.mask);
                    if (data.occlusion.IsValid()) data.material.SetTexture(OcclusionMaskID, data.occlusion);
                    else data.material.SetTexture(OcclusionMaskID, Texture2D.blackTexture);

                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Outline Copy Back", out var passData))
            {
                passData.source = tempTexture;
                passData.destination = source;
                builder.UseTexture(passData.source, AccessFlags.Read);
                builder.SetRenderAttachment(passData.destination, 0, AccessFlags.Write);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 0, false);
                });
            }
        }
        public void Dispose() { CoreUtils.Destroy(material); }
    }

    private LayerMaskPass selectionPass;
    private LayerMaskPass occlusionPass;
    private OutlinePass outlinePass;

    public override void Create()
    {
        selectionPass = new LayerMaskPass("Outline Selection Mask", "_SelectionMaskTexture");
        occlusionPass = new LayerMaskPass("Outline Occlusion Mask", "_OcclusionMaskTexture");
        outlinePass = new OutlinePass();
        outlinePass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var stack = VolumeManager.instance.stack;
        var settings = stack.GetComponent<OutlineVolume>();

        if (settings != null && settings.IsActive())
        {
            outlinePass.SetupReference(selectionPass, occlusionPass);
            if (settings.selectionLayer.value != 0)
            {
                selectionPass.Setup(settings.selectionLayer.value);
                renderer.EnqueuePass(selectionPass);
            }
            if (settings.occlusionLayer.value != 0)
            {
                occlusionPass.Setup(settings.occlusionLayer.value);
                renderer.EnqueuePass(occlusionPass);
            }
            renderer.EnqueuePass(outlinePass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        selectionPass.Dispose();
        occlusionPass.Dispose();
        outlinePass.Dispose();
    }
}