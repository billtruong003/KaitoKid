Shader "CleanRender/Horror/Toon Wall Blend"
{
    Properties
    {
        // ── Layer A (Base: Cement) ──
        [MainTexture] _BaseMapA("Base A", 2D) = "white"{}
        [MainColor]   _BaseColorA("Color A", Color) = (0.6, 0.58, 0.55, 1)

        // ── Layer B (Overlay: Brick) ──
        _BaseMapB("Base B", 2D) = "white"{}
        _BaseColorB("Color B", Color) = (0.7, 0.35, 0.2, 1)
        _ScaleB("Scale B", Float) = 1.0

        // ── Triplanar ──
        _BaseScale("Triplanar Scale", Float) = 0.5
        _TriSharpness("Tri Sharpness", Range(1, 16)) = 4.0

        // ── Blend ──
        _NoiseTex("Blend Noise", 2D) = "gray"{}
        _BlendThreshold("Blend Threshold", Range(0, 1)) = 0.5
        _BlendEdge("Blend Edge Softness", Range(0.001, 0.5)) = 0.05
        _BlendNoiseScale("Blend Noise Scale", Float) = 1.0
        _HeightBias("Height Bias", Range(-1, 1)) = 0.0

        // ── Cel Shading ──
        _ShadowColor("Shadow Color", Color) = (0.2, 0.18, 0.22, 1)
        _Threshold("Shadow Threshold", Range(0, 1)) = 0.35
        _Smoothness("Shadow Smoothness", Range(0.001, 0.5)) = 0.06
        _RimColor("Rim Color (RGB) Intensity (A)", Color) = (0.5, 0.45, 0.55, 0.4)
        _RimPower("Rim Power", Range(1, 20)) = 6.0

        // ── Dirt ──
        [Toggle(_DIRT_ON)] _EnableDirt("Enable Dirt", Float) = 0
        _DirtColor("Dirt Color", Color) = (0.2, 0.15, 0.1, 1)
        _DirtScale("Dirt Scale", Float) = 2.0
        _DirtAmount("Dirt Amount", Range(0, 1)) = 0.4
        _NormalStrength("Dirt Normal Str", Range(0, 3)) = 1.0

        // ── Water Stain ──
        [Toggle(_WATER_STAIN_ON)] _EnableWaterStain("Enable Water Stain", Float) = 0
        _StainColor("Stain Color", Color) = (0.25, 0.22, 0.18, 1)
        _StainHeight("Stain Height", Range(0, 1)) = 0.4
        _StainSoftness("Stain Softness", Range(0.01, 0.5)) = 0.15
        _StainNoiseScale("Stain Noise Scale", Float) = 1.5

        // ── Ceiling Stain ──
        [Toggle(_CEILING_STAIN_ON)] _EnableCeilingStain("Enable Ceiling Stain", Float) = 0
        _CeilingStainThreshold("Ceiling Stain Thresh", Range(0, 1)) = 0.5

        // ── Blood ──
        [Toggle(_BLOOD_ON)] _EnableBlood("Enable Blood", Float) = 0
        _BloodColor("Blood Color", Color) = (0.35, 0.02, 0.02, 1)
        _BloodScale("Blood Scale", Float) = 1.0
        _BloodThreshold("Blood Threshold", Range(0, 1)) = 0.6
        _BloodEdge("Blood Edge", Range(0.01, 0.3)) = 0.05

        // ── Cracks ──
        [Toggle(_CRACKS_ON)] _EnableCracks("Enable Cracks", Float) = 0
        _CrackScale("Crack Scale", Float) = 3.0
        _CrackWidth("Crack Width", Range(0.01, 0.3)) = 0.06
        _CrackDepth("Crack Depth", Range(0, 1)) = 0.8
        _CrackColor("Crack Color", Color) = (0.05, 0.04, 0.03, 1)

        // ── Flicker ──
        [Toggle(_FLICKER_ON)] _EnableFlicker("Enable Flicker", Float) = 0
        [HDR] _FlickerColor("Flicker Color", Color) = (1, 0.95, 0.8, 1)
        _FlickerSpeed("Flicker Speed", Float) = 25.0
        _FlickerMask("Flicker Mask (R)", 2D) = "white"{}

        // ── Emission ──
        [Toggle(_EMISSION)] _Emission("Enable Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 0)

        // ── Options ──
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 100

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "../../Core/Shaders/Includes/ToonLighting.hlsl"
        #include "../../Core/Shaders/Includes/NoiseLib.hlsl"
        #include "Includes/HorrorLib.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMapA_ST;
            half4  _BaseColorA;
            float4 _BaseMapB_ST;
            half4  _BaseColorB;
            float  _ScaleB;
            float  _BaseScale;
            half   _TriSharpness;
            float  _BlendNoiseScale;
            half   _BlendThreshold;
            half   _BlendEdge;
            half   _HeightBias;
            // Cel
            half4  _ShadowColor;
            half   _Threshold;
            half   _Smoothness;
            half4  _RimColor;
            half   _RimPower;
            // Dirt
            half4  _DirtColor;
            float  _DirtScale;
            half   _DirtAmount;
            half   _NormalStrength;
            // Water
            half4  _StainColor;
            half   _StainHeight;
            half   _StainSoftness;
            float  _StainNoiseScale;
            half   _CeilingStainThreshold;
            // Blood
            half4  _BloodColor;
            float  _BloodScale;
            half   _BloodThreshold;
            half   _BloodEdge;
            // Cracks
            float  _CrackScale;
            half   _CrackWidth;
            half   _CrackDepth;
            half4  _CrackColor;
            // Flicker
            half4  _FlickerColor;
            float  _FlickerSpeed;
            // Emission
            half4  _EmissionColor;
            float  _Cull;
        CBUFFER_END

        TEXTURE2D(_BaseMapA);   SAMPLER(sampler_BaseMapA);
        TEXTURE2D(_BaseMapB);   SAMPLER(sampler_BaseMapB);
        TEXTURE2D(_NoiseTex);   SAMPLER(sampler_NoiseTex);
        TEXTURE2D(_FlickerMask);SAMPLER(sampler_FlickerMask);
        ENDHLSL

        // ============================================================
        // ForwardLit
        // ============================================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma shader_feature_local _DIRT_ON
            #pragma shader_feature_local _WATER_STAIN_ON
            #pragma shader_feature_local _CEILING_STAIN_ON
            #pragma shader_feature_local _BLOOD_ON
            #pragma shader_feature_local _CRACKS_ON
            #pragma shader_feature_local _FLICKER_ON
            #pragma shader_feature_local _EMISSION

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_STEREO_INSTANCING_ENABLED UNITY_STEREO_MULTIVIEW_ENABLED

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                half3  normalWS     : TEXCOORD1;
                float2 lightmapUV   : TEXCOORD2;
                half   fogFactor    : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS   = (half3)TransformObjectToWorldNormal(input.normalOS);

                #ifdef LIGHTMAP_ON
                    o.lightmapUV = input.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
                #else
                    o.lightmapUV = float2(0, 0);
                #endif

                o.fogFactor = (half)ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 posWS = input.positionWS;
                half3  N     = normalize(input.normalWS);

                // ── Triplanar sample both layers ──
                TriplanarUV tpA = ComputeTriplanarUV(posWS, N, _BaseScale, _TriSharpness);
                TriplanarUV tpB = ComputeTriplanarUV(posWS, N, _BaseScale * _ScaleB, _TriSharpness);

                half3 albedoA = SampleTriplanar(TEXTURE2D_ARGS(_BaseMapA, sampler_BaseMapA), tpA).rgb * _BaseColorA.rgb;
                half3 albedoB = SampleTriplanar(TEXTURE2D_ARGS(_BaseMapB, sampler_BaseMapB), tpB).rgb * _BaseColorB.rgb;

                // ── Blend mask ──
                TriplanarUV tpNoise = ComputeTriplanarUV(posWS, N, _BlendNoiseScale, _TriSharpness);
                half blendNoise = SampleTriplanarR(TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex), tpNoise);
                blendNoise += _HeightBias * (half)saturate(posWS.y * 0.2);
                half blendMask = (half)smoothstep(_BlendThreshold - _BlendEdge, _BlendThreshold + _BlendEdge, blendNoise);

                half3 albedo = lerp(albedoA, albedoB, blendMask);

                // ── Normal perturbation from noise (dirt) ──
                half3 perturbedN = N;
                #if defined(_DIRT_ON)
                {
                    perturbedN = TriplanarNormalFromNoise(
                        TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex),
                        posWS, N, _DirtScale, _TriSharpness, _NormalStrength);
                }
                #endif

                // ══════════════════════════════════════
                // HORROR OVERLAYS
                // ══════════════════════════════════════

                #if defined(_CRACKS_ON)
                    half crackMask = CrackPatternTriplanar(posWS, N, _CrackScale, _TriSharpness, _CrackWidth, TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex));
                    albedo = lerp(albedo, _CrackColor.rgb, crackMask * _CrackDepth);
                #endif

                #if defined(_DIRT_ON)
                    half dirtMask = DirtOverlay(posWS, N, TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex), _DirtScale, _DirtAmount);
                    albedo = lerp(albedo, _DirtColor.rgb, dirtMask);
                #endif

                #if defined(_WATER_STAIN_ON)
                    half stainMask = WaterDamageStain(posWS, N, _StainHeight, _StainSoftness, TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex), _StainNoiseScale);
                    albedo = lerp(albedo, albedo * _StainColor.rgb, stainMask);
                #endif

                #if defined(_CEILING_STAIN_ON)
                    half ceilMask = CeilingStain(posWS, N, TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex), _StainNoiseScale, _CeilingStainThreshold);
                    albedo = lerp(albedo, albedo * _StainColor.rgb * 0.7h, ceilMask);
                #endif

                #if defined(_BLOOD_ON)
                    half bloodMask = BloodSplatTriplanar(TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex), posWS, N, _BloodScale, _TriSharpness, _BloodThreshold, _BloodEdge);
                    albedo = lerp(albedo, _BloodColor.rgb, bloodMask);
                #endif

                // ══════════════════════════════════════
                // CEL SHADING (ToonLighting)
                // ══════════════════════════════════════

                half3 V = (half3)GetWorldSpaceNormalizeViewDir(posWS);
                ToonLightResult lit = ComputeToonMainLight(
                    posWS, perturbedN, V, albedo, input.lightmapUV,
                    _Threshold, _Smoothness, _ShadowColor.rgb,
                    _RimColor.rgb, _RimPower, _RimColor.a, 1.0h);

                half3 color = lit.diffuse + lit.rim + lit.globalIllumination;

                // Additional lights
                color += ComputeToonAdditionalLights(posWS, perturbedN, albedo, _Threshold, _Smoothness);

                // ── Flicker ──
                #if defined(_FLICKER_ON)
                {
                    half flicker = FlickerIntensity(_Time.y, _FlickerSpeed);
                    TriplanarUV tpFl = ComputeTriplanarUV(posWS, N, _BaseScale, _TriSharpness);
                    half flickMask = SampleTriplanarR(TEXTURE2D_ARGS(_FlickerMask, sampler_FlickerMask), tpFl);
                    color += _FlickerColor.rgb * flicker * flickMask;
                }
                #endif

                // ── Emission ──
                #if defined(_EMISSION)
                    color += _EmissionColor.rgb;
                #endif

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        // ============================================================
        // ShadowCaster
        // ============================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex SV
            #pragma fragment SF
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_STEREO_INSTANCING_ENABLED UNITY_STEREO_MULTIVIEW_ENABLED
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
            V SV(A i) { V o=(V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 pw=TransformObjectToWorld(i.positionOS.xyz); float3 nw=TransformObjectToWorldNormal(i.normalOS);
                o.positionCS=TransformWorldToHClip(ApplyShadowBias(pw,nw,_LightDirection));
                #if UNITY_REVERSED_Z
                    o.positionCS.z=min(o.positionCS.z,UNITY_NEAR_CLIP_VALUE);
                #else
                    o.positionCS.z=max(o.positionCS.z,UNITY_NEAR_CLIP_VALUE);
                #endif
                return o; }
            half4 SF(V i):SV_Target { return 0; }
            ENDHLSL
        }

        // ============================================================
        // DepthOnly
        // ============================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask R Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex DV
            #pragma fragment DF
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_STEREO_INSTANCING_ENABLED UNITY_STEREO_MULTIVIEW_ENABLED
            struct A { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
            V DV(A i) { V o=(V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.positionCS=TransformObjectToHClip(i.positionOS.xyz); return o; }
            half4 DF(V i):SV_Target { return 0; }
            ENDHLSL
        }

        // ============================================================
        // DepthNormals
        // ============================================================
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex DNV
            #pragma fragment DNF
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_STEREO_INSTANCING_ENABLED UNITY_STEREO_MULTIVIEW_ENABLED
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; half3 normalWS : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V DNV(A i) { V o=(V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.normalWS=(half3)TransformObjectToWorldNormal(i.normalOS); return o; }
            half4 DNF(V i):SV_Target { return half4(normalize(i.normalWS),0); }
            ENDHLSL
        }

        // ============================================================
        // Meta
        // ============================================================
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex MV
            #pragma fragment MF
            #pragma shader_feature_local _EMISSION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
            struct A { float4 positionOS : POSITION; float2 uv0 : TEXCOORD0; float2 uv1 : TEXCOORD1; float2 uv2 : TEXCOORD2; };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            V MV(A i) { V o=(V)0; o.positionCS=UnityMetaVertexPosition(i.positionOS.xyz,i.uv1,i.uv2); o.uv=i.uv0; return o; }
            half4 MF(V i):SV_Target { half4 a=SAMPLE_TEXTURE2D(_BaseMapA,sampler_BaseMapA,i.uv)*_BaseColorA; MetaInput mi=(MetaInput)0; mi.Albedo=a.rgb;
                #if defined(_EMISSION)
                    mi.Emission=_EmissionColor.rgb;
                #endif
                return UnityMetaFragment(mi); }
            ENDHLSL
        }
    }

    CustomEditor "ToonWallBlendGUI"
}
