using Sirenix.OdinInspector;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[DisallowMultipleComponent]
public class PersistentSnowTrailManager : MonoBehaviour
{
    public static PersistentSnowTrailManager Instance { get; private set; }

    #region Configuration
    [TabGroup("Settings", "Area")]
    [SerializeField] private Vector3 trailAreaCenter = Vector3.zero;
    [TabGroup("Settings", "Area")]
    [SerializeField] private float trailAreaSize = 50f;

    [TabGroup("Settings", "Resources")]
    [Required]
    [SerializeField] private RenderTexture snowCanvasRT;
    [TabGroup("Settings", "Resources")]
    [SerializeField] private ComputeShader snowTrailComputeShader;
    [TabGroup("Settings", "Resources")]
    [SerializeField, Tooltip("Shader used for the high-performance instanced drawing path (CPU fallback).")]
    private Shader trailInstancedShader;

    [TabGroup("Settings", "Effects")]
    [SerializeField, Range(0f, 0.5f)] private float healingRate = 0.01f;
    [TabGroup("Settings", "Effects")]
    [SerializeField] private bool enableBlur = true;
    [TabGroup("Settings", "Effects")]
    [ShowIf("enableBlur")]
    [SerializeField, Range(1, 8)] private int blurPasses = 2;
    [TabGroup("Settings", "Effects")]
    [ShowIf("enableBlur")]
    [SerializeField, Range(0.5f, 5f)] private float blurPixelOffset = 1.5f;

    [TabGroup("Settings", "Performance")]
    [SerializeField] private bool useComputeShader = true;
    #endregion

    #region Private Members
    private Material trailInstancedMaterial;
    [SerializeField] private Material snowEffectsMaterial;
    [SerializeField] private Material kawaseBlurMaterial;

    private RenderTexture tempRT1, tempRT2;

    private const int MAX_DRAW_COMMANDS = 1024;
    private readonly List<SnowTrailDrawCommand> drawQueue = new List<SnowTrailDrawCommand>(MAX_DRAW_COMMANDS);
    private ComputeBuffer drawCommandComputeBuffer;
    private DrawCommandData[] drawCommandDataArray;

    private int snowTrailComputeKernel = -1;
    private bool isComputeShaderPathInitialized = false;

    private readonly HashSet<Texture2D> registeredBrushes = new HashSet<Texture2D>();
    private readonly Dictionary<Texture2D, Rect> brushAtlasUVs = new Dictionary<Texture2D, Rect>();
    private Texture2D brushAtlas;
    private bool atlasNeedsRebuilding = false;
    #endregion

    #region Shader Property IDs
    private static readonly int GlobalEffectRT_ID = Shader.PropertyToID("_GlobalEffectRT");
    private static readonly int InteractorPosition_ID = Shader.PropertyToID("_InteractorPosition");
    private static readonly int OrthographicCamSize_ID = Shader.PropertyToID("_OrthographicCamSize");
    private static readonly int PreviousFrameRT_ID = Shader.PropertyToID("_PreviousFrameRT");
    private static readonly int HealingRate_ID = Shader.PropertyToID("_HealingRate");
    private static readonly int DeltaTime_ID = Shader.PropertyToID("_DeltaTime");
    private static readonly int Result_ID = Shader.PropertyToID("Result");
    private static readonly int DrawCommandsBuffer_ID = Shader.PropertyToID("_DrawCommands");
    private static readonly int DrawCommandCount_ID = Shader.PropertyToID("_DrawCommandCount");
    private static readonly int TrailAreaParams_ID = Shader.PropertyToID("_TrailAreaParams");
    private static readonly int TextureSize_ID = Shader.PropertyToID("_TextureSize");
    private static readonly int BrushAtlas_ID = Shader.PropertyToID("_BrushAtlas");
    private static readonly int PixelOffset_ID = Shader.PropertyToID("_PixelOffset");
    #endregion

    private struct SnowTrailDrawCommand
    {
        public Vector3 WorldPosition;
        public float Radius;
        public float Strength;
        public Texture2D BrushTexture;
    }

    private struct DrawCommandData
    {
        public Vector2 worldPos;
        public float radius;
        public float strength;
        public Vector4 brushUVRect;
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable() => Initialize();
    private void OnDisable() => ReleaseResources();

    private void LateUpdate()
    {
        if (atlasNeedsRebuilding)
        {
            BuildBrushAtlas();
        }

        if (drawQueue.Count > 0)
        {
            Graphics.Blit(snowCanvasRT, tempRT1);

            if (isComputeShaderPathInitialized) ExecuteComputeShaderPass();
            else ExecuteInstancedDrawPass();

            if (enableBlur) ApplyBlur();

            Graphics.Blit(tempRT1, snowCanvasRT);
        }

        ApplyHealing();
        drawQueue.Clear();
    }

    public void RegisterBrush(Texture2D brushTexture)
    {
        if (brushTexture != null && registeredBrushes.Add(brushTexture))
        {
            atlasNeedsRebuilding = true;
        }
    }

    public void QueueDrawCommand(Vector3 worldPosition, float radius, float strength, Texture2D brush)
    {
        if (drawQueue.Count >= MAX_DRAW_COMMANDS) return;
        drawQueue.Add(new SnowTrailDrawCommand
        {
            WorldPosition = worldPosition,
            Radius = radius,
            Strength = strength,
            BrushTexture = brush
        });
    }

    private void Initialize()
    {
        if (!ValidateConfiguration()) return;
        CreateMaterials();
        CreateRenderTextures();
        InitializeComputePath();
        InitializeInstancedPath();
        BuildBrushAtlas();
        SetGlobalShaderProperties();
        ClearRenderTextures();
    }

    private bool ValidateConfiguration()
    {
        if (snowCanvasRT == null) { Debug.LogError("SnowCanvasRT is not assigned!", this); return false; }
        if (useComputeShader && (snowTrailComputeShader == null || !SystemInfo.supportsComputeShaders))
        {
            Debug.LogWarning("Compute Shader not supported or not assigned. Falling back to instanced drawing path.", this);
            useComputeShader = false;
        }
        if (!useComputeShader && trailInstancedShader == null)
        {
            Debug.LogError("Trail Instanced Shader is not assigned! This is required for the non-compute path.", this);
            return false;
        }
        if (useComputeShader && !snowCanvasRT.enableRandomWrite)
        {
            Debug.LogWarning("'Enable Random Write' on SnowCanvasRT is required for the Compute Shader path. Disabling compute path.", this);
            useComputeShader = false;
        }
        return true;
    }

    private void CreateMaterials()
    {
        if (trailInstancedShader != null)
            trailInstancedMaterial = new Material(trailInstancedShader);
        if (snowEffectsMaterial == null && kawaseBlurMaterial == null)
        {
            snowEffectsMaterial = new Material(Shader.Find("Hidden/SnowEffects"));
            kawaseBlurMaterial = new Material(Shader.Find("Hidden/KawaseBlur"));
        }
    }

    // SỬA LỖI TẠI ĐÂY
    private void CreateRenderTextures()
    {
        if (tempRT1 != null) tempRT1.Release();
        if (tempRT2 != null) tempRT2.Release();

        var descriptor = snowCanvasRT.descriptor;

        // tempRT1 là buffer trung gian, không bao giờ cần random write.
        descriptor.enableRandomWrite = false;
        tempRT1 = new RenderTexture(descriptor);
        tempRT1.Create();

        // tempRT2 là đích đến (UAV) của Compute Shader, vì vậy nó CẦN random write.
        // Đối với đường dẫn CPU, nó không cần flag này.
        descriptor.enableRandomWrite = useComputeShader;
        tempRT2 = new RenderTexture(descriptor);
        tempRT2.Create();
    }

    private void ClearRenderTextures()
    {
        Graphics.Blit(Texture2D.blackTexture, snowCanvasRT);
    }

    private void InitializeComputePath()
    {
        isComputeShaderPathInitialized = false;
        if (!useComputeShader) return;

        snowTrailComputeKernel = snowTrailComputeShader.FindKernel("ProcessSnowTrails");
        if (snowTrailComputeKernel == -1)
        {
            Debug.LogWarning("Kernel 'ProcessSnowTrails' not found, disabling compute path.", this);
            return;
        }

        drawCommandDataArray = new DrawCommandData[MAX_DRAW_COMMANDS];
        drawCommandComputeBuffer = new ComputeBuffer(MAX_DRAW_COMMANDS, sizeof(float) * 8, ComputeBufferType.Structured);

        snowTrailComputeShader.SetVector(TextureSize_ID, new Vector2(snowCanvasRT.width, snowCanvasRT.height));
        snowTrailComputeShader.SetVector(TrailAreaParams_ID, new Vector4(trailAreaCenter.x, trailAreaCenter.z, trailAreaSize, 0));

        isComputeShaderPathInitialized = true;
    }

    private void InitializeInstancedPath()
    {
        if (useComputeShader) return;
        drawCommandDataArray = new DrawCommandData[MAX_DRAW_COMMANDS];
        drawCommandComputeBuffer = new ComputeBuffer(MAX_DRAW_COMMANDS, sizeof(float) * 8, ComputeBufferType.Structured);
    }

    private void SetGlobalShaderProperties()
    {
        Shader.SetGlobalTexture(GlobalEffectRT_ID, snowCanvasRT);
        Shader.SetGlobalVector(InteractorPosition_ID, new Vector4(trailAreaCenter.x, trailAreaCenter.y, trailAreaCenter.z, 0));
        Shader.SetGlobalFloat(OrthographicCamSize_ID, trailAreaSize / 2f);
    }

    private void ReleaseResources()
    {
        tempRT1?.Release();
        tempRT2?.Release();
        drawCommandComputeBuffer?.Release();
        Destroy(trailInstancedMaterial);
        Destroy(snowEffectsMaterial);
        Destroy(kawaseBlurMaterial);
        Destroy(brushAtlas);
    }

    private void BuildBrushAtlas()
    {
        if (brushAtlas != null) Destroy(brushAtlas);

        var textures = registeredBrushes.Where(t => t != null).ToArray();
        if (textures.Length == 0) return;

        brushAtlas = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        var uvs = brushAtlas.PackTextures(textures, 2, 4096, false);
        brushAtlas.Apply(true, true);

        brushAtlasUVs.Clear();
        for (int i = 0; i < textures.Length; i++)
        {
            brushAtlasUVs[textures[i]] = uvs[i];
        }

        if (isComputeShaderPathInitialized)
            snowTrailComputeShader.SetTexture(snowTrailComputeKernel, BrushAtlas_ID, brushAtlas);
        else if (trailInstancedMaterial != null)
            trailInstancedMaterial.SetTexture(BrushAtlas_ID, brushAtlas);


        atlasNeedsRebuilding = false;
    }

    private void PrepareDrawCommandBuffer()
    {
        int commandCount = drawQueue.Count;
        for (int i = 0; i < commandCount; i++)
        {
            var command = drawQueue[i];
            if (brushAtlasUVs.TryGetValue(command.BrushTexture, out var uvRect))
            {
                drawCommandDataArray[i] = new DrawCommandData
                {
                    worldPos = new Vector2(command.WorldPosition.x, command.WorldPosition.z),
                    radius = command.Radius,
                    strength = command.Strength,
                    brushUVRect = new Vector4(uvRect.x, uvRect.y, uvRect.width, uvRect.height)
                };
            }
        }
        drawCommandComputeBuffer.SetData(drawCommandDataArray, 0, 0, commandCount);
    }

    private void ExecuteComputeShaderPass()
    {
        if (brushAtlas == null || drawQueue.Count == 0) return;

        PrepareDrawCommandBuffer();

        snowTrailComputeShader.SetTexture(snowTrailComputeKernel, PreviousFrameRT_ID, tempRT1);
        snowTrailComputeShader.SetTexture(snowTrailComputeKernel, Result_ID, tempRT2);
        snowTrailComputeShader.SetBuffer(snowTrailComputeKernel, DrawCommandsBuffer_ID, drawCommandComputeBuffer);
        snowTrailComputeShader.SetInt(DrawCommandCount_ID, drawQueue.Count);

        int threadGroupsX = Mathf.CeilToInt(snowCanvasRT.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(snowCanvasRT.height / 8.0f);
        snowTrailComputeShader.Dispatch(snowTrailComputeKernel, threadGroupsX, threadGroupsY, 1);

        Graphics.Blit(tempRT2, tempRT1);
    }

    private void ExecuteInstancedDrawPass()
    {
        if (brushAtlas == null || drawQueue.Count == 0 || trailInstancedMaterial == null) return;

        PrepareDrawCommandBuffer();

        trailInstancedMaterial.SetBuffer(DrawCommandsBuffer_ID, drawCommandComputeBuffer);
        trailInstancedMaterial.SetVector(TrailAreaParams_ID, new Vector4(trailAreaCenter.x, trailAreaCenter.z, trailAreaSize, 0));
        trailInstancedMaterial.SetTexture("_BrushAtlas", brushAtlas);

        RenderTexture prevActive = RenderTexture.active;
        Graphics.SetRenderTarget(tempRT2);
        GL.Clear(false, true, Color.clear);
        trailInstancedMaterial.SetPass(0);

        Graphics.DrawProceduralNow(MeshTopology.Triangles, 6, drawQueue.Count);
        Graphics.SetRenderTarget(prevActive);

        snowEffectsMaterial.SetTexture("_SecondaryTex", tempRT2);
        Graphics.Blit(tempRT1, tempRT2, snowEffectsMaterial, 1);
        Graphics.Blit(tempRT2, tempRT1);
    }

    private void ApplyBlur()
    {
        RenderTexture bufferA = tempRT1;
        RenderTexture bufferB = tempRT2;

        for (int i = 0; i < blurPasses; i++)
        {
            kawaseBlurMaterial.SetFloat(PixelOffset_ID, (i * 0.5f + 1f) * blurPixelOffset);
            Graphics.Blit(bufferA, bufferB, kawaseBlurMaterial);
            (bufferA, bufferB) = (bufferB, bufferA);
        }

        if (bufferA != tempRT1)
        {
            Graphics.Blit(bufferA, tempRT1);
        }
    }

    private void ApplyHealing()
    {
        if (healingRate <= 0) return;

        snowEffectsMaterial.SetFloat(HealingRate_ID, healingRate);
        snowEffectsMaterial.SetFloat(DeltaTime_ID, Time.deltaTime);

        Graphics.Blit(snowCanvasRT, tempRT1, snowEffectsMaterial, 0);
        Graphics.Blit(tempRT1, snowCanvasRT);
    }
}