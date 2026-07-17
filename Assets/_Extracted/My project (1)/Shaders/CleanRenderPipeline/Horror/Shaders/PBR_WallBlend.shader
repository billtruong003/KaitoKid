Shader "CleanRender/Horror/PBR Wall Blend"
{
    Properties
    {
        // ── Layer A (Base: Cement) ──
        [MainTexture] _BaseMapA("Base A", 2D) = "white"{}
        [MainColor]   _BaseColorA("Color A", Color) = (0.6, 0.58, 0.55, 1)
        _BumpMapA("Normal A", 2D) = "bump"{}
        _BumpScaleA("Normal Scale A", Range(0, 2)) = 1.0
        _MetallicA("Metallic A", Range(0, 1)) = 0.0
        _SmoothnessA("Smoothness A", Range(0, 1)) = 0.25

        // ── Layer B (Overlay: Brick) ──
        _BaseMapB("Base B", 2D) = "white"{}
        _BaseColorB("Color B", Color) = (0.7, 0.35, 0.2, 1)
        _BumpMapB("Normal B", 2D) = "bump"{}
        _BumpScaleB("Normal Scale B", Range(0, 2)) = 1.0
        _MetallicB("Metallic B", Range(0, 1)) = 0.0
        _SmoothnessB("Smoothness B", Range(0, 1)) = 0.35
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

        // ── PBR shared ──
        _OcclusionMap("Occlusion", 2D) = "white"{}
        _OcclusionStrength("Occlusion Str", Range(0, 1)) = 1.0

        // ── Dirt Overlay ──
        [Toggle(_DIRT_ON)] _EnableDirt("Enable Dirt", Float) = 0
        _DirtColor("Dirt Color", Color) = (0.2, 0.15, 0.1, 1)
        _DirtScale("Dirt Scale", Float) = 2.0
        _DirtAmount("Dirt Amount", Range(0, 1)) = 0.4
        _DirtRoughness("Dirt Roughness", Range(0, 1)) = 0.9

        // ── Water Stain ──
        [Toggle(_WATER_STAIN_ON)] _EnableWaterStain("Enable Water Stain", Float) = 0
        _StainColor("Stain Color", Color) = (0.25, 0.22, 0.18, 1)
        _StainHeight("Stain Height", Range(0, 1)) = 0.4
        _StainSoftness("Stain Softness", Range(0.01, 0.5)) = 0.15
        _StainNoiseScale("Stain Noise Scale", Float) = 1.5
        _StainSmoothness("Stain Smoothness", Range(0, 1)) = 0.7

        // ── Blood Splat ──
        [Toggle(_BLOOD_ON)] _EnableBlood("Enable Blood", Float) = 0
        _BloodColor("Blood Color", Color) = (0.35, 0.02, 0.02, 1)
        _BloodScale("Blood Scale", Float) = 1.0
        _BloodThreshold("Blood Threshold", Range(0, 1)) = 0.6
        _BloodEdge("Blood Edge", Range(0.01, 0.3)) = 0.05
        _BloodSmoothness("Blood Smoothness", Range(0, 1)) = 0.8

        // ── Cracks ──
        [Toggle(_CRACKS_ON)] _EnableCracks("Enable Cracks", Float) = 0
        _CrackScale("Crack Scale", Float) = 3.0
        _CrackWidth("Crack Width", Range(0.01, 0.3)) = 0.06
        _CrackDepth("Crack Depth", Range(0, 1)) = 0.8
        _CrackColor("Crack Color", Color) = (0.05, 0.04, 0.03, 1)

        // ── Emission ──
        [Toggle(_EMISSION)] _EnableEmission("Enable Emission", Float) = 0
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
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "../../Core/Shaders/Includes/NoiseLib.hlsl"
        #include "Includes/HorrorLib.hlsl"

        CBUFFER_START(UnityPerMaterial)
            // Layer A
            float4 _BaseMapA_ST;
            half4  _BaseColorA;
            half   _BumpScaleA;
            half   _MetallicA;
            half   _SmoothnessA;
            // Layer B
            float4 _BaseMapB_ST;
            half4  _BaseColorB;
            half   _BumpScaleB;
            half   _MetallicB;
            half   _SmoothnessB;
            float  _ScaleB;
            // Triplanar
            float  _BaseScale;
            half   _TriSharpness;
            // Blend
            float  _BlendNoiseScale;
            half   _BlendThreshold;
            half   _BlendEdge;
            half   _HeightBias;
            // PBR
            half   _OcclusionStrength;
            // Dirt
            half4  _DirtColor;
            float  _DirtScale;
            half   _DirtAmount;
            half   _DirtRoughness;
            // Water
            half4  _StainColor;
            half   _StainHeight;
            half   _StainSoftness;
            float  _StainNoiseScale;
            half   _StainSmoothness;
            // Blood
            half4  _BloodColor;
            float  _BloodScale;
            half   _BloodThreshold;
            half   _BloodEdge;
            half   _BloodSmoothness;
            // Cracks
            float  _CrackScale;
            half   _CrackWidth;
            half   _CrackDepth;
            half4  _CrackColor;
            // Emission
            half4  _EmissionColor;
            // Options
            float  _Cull;
        CBUFFER_END

        TEXTURE2D(_BaseMapA);  SAMPLER(sampler_BaseMapA);
        TEXTURE2D(_BumpMapA);  SAMPLER(sampler_BumpMapA);
        TEXTURE2D(_BaseMapB);  SAMPLER(sampler_BaseMapB);
        TEXTURE2D(_BumpMapB);  SAMPLER(sampler_BumpMapB);
        TEXTURE2D(_NoiseTex);  SAMPLER(sampler_NoiseTex);
        TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);
        ENDHLSL

        // ============================================================
        // Pass 0: ForwardLit
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
            #pragma shader_feature_local _BLOOD_ON
            #pragma shader_feature_local _CRACKS_ON
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
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                half3  normalWS     : TEXCOORD1;
                half4  tangentWS    : TEXCOORD2;
                float2 lightmapUV   : TEXCOORD3;
                half   fogFactor    : TEXCOORD4;
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

                real sign = input.tangentOS.w * GetOddNegativeScale();
                float3 tWS = TransformObjectToWorldDir(input.tangentOS.xyz);
                o.tangentWS  = half4((half3)tWS, (half)sign);

                #ifdef LIGHTMAP_ON
                    o.lightmapUV = input.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
                #else
                    o.lightmapUV = float2(0, 0);
                #endif

                o.fogFactor = (half)ComputeFogFactor(o.positionCS.z);
                return o;
            }

            // ── Triplanar normal sampling (returns tangent-space normal) ──
            half3 SampleTriplanarNormal(TEXTURE2D_PARAM(nmap, nsamp), TriplanarUV tp, half scale)
            {
                half3 nX = UnpackNormalScale(SAMPLE_TEXTURE2D(nmap, nsamp, tp.uvX), scale);
                half3 nY = UnpackNormalScale(SAMPLE_TEXTURE2D(nmap, nsamp, tp.uvY), scale);
                half3 nZ = UnpackNormalScale(SAMPLE_TEXTURE2D(nmap, nsamp, tp.uvZ), scale);
                return normalize(nX * tp.blend.x + nY * tp.blend.y + nZ * tp.blend.z);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 posWS = input.positionWS;
                half3  N     = normalize(input.normalWS);

                // ── Triplanar UVs ──
                TriplanarUV tpA = ComputeTriplanarUV(posWS, N, _BaseScale, _TriSharpness);
                TriplanarUV tpB = ComputeTriplanarUV(posWS, N, _BaseScale * _ScaleB, _TriSharpness);

                // ── Sample Layer A (cement) ──
                half4 albedoA = SampleTriplanar(TEXTURE2D_ARGS(_BaseMapA, sampler_BaseMapA), tpA) * _BaseColorA;
                half3 normalA = SampleTriplanarNormal(TEXTURE2D_ARGS(_BumpMapA, sampler_BumpMapA), tpA, _BumpScaleA);

                // ── Sample Layer B (brick) ──
                half4 albedoB = SampleTriplanar(TEXTURE2D_ARGS(_BaseMapB, sampler_BaseMapB), tpB) * _BaseColorB;
                half3 normalB = SampleTriplanarNormal(TEXTURE2D_ARGS(_BumpMapB, sampler_BumpMapB), tpB, _BumpScaleB);

                // ── Blend mask from noise triplanar ──
                TriplanarUV tpNoise = ComputeTriplanarUV(posWS, N, _BlendNoiseScale, _TriSharpness);
                half blendNoise = SampleTriplanarR(TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex), tpNoise);

                // Height bias: shift blend based on world height (higher = more exposed brick)
                blendNoise += _HeightBias * (half)saturate(posWS.y * 0.2);

                // Threshold cutoff with soft edge
                half blendMask = (half)smoothstep(_BlendThreshold - _BlendEdge, _BlendThreshold + _BlendEdge, blendNoise);

                // ── Final blended surface ──
                half3 albedo     = lerp(albedoA.rgb, albedoB.rgb, blendMask);
                half3 normalTS   = normalize(lerp(normalA, normalB, blendMask));
                half  metallic   = lerp(_MetallicA, _MetallicB, blendMask);
                half  smoothness = lerp(_SmoothnessA, _SmoothnessB, blendMask);

                // ── Apply tangent-space normal to world ──
                half3 bitangent = cross(N, input.tangentWS.xyz) * input.tangentWS.w;
                half3 finalN = normalize(normalTS.x * input.tangentWS.xyz + normalTS.y * bitangent + normalTS.z * N);

                // ── Occlusion ──
                TriplanarUV tpOcc = ComputeTriplanarUV(posWS, N, _BaseScale, _TriSharpness);
                half occ = lerp(1.0h, SampleTriplanarR(TEXTURE2D_ARGS(_OcclusionMap, sampler_OcclusionMap), tpOcc), _OcclusionStrength);

                // ══════════════════════════════════════
                // HORROR OVERLAYS
                // ══════════════════════════════════════

                #if defined(_CRACKS_ON)
                    half crackMask = CrackPatternTriplanar(posWS, N, _CrackScale, _TriSharpness, _CrackWidth, TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex));
                    albedo = lerp(albedo, _CrackColor.rgb, crackMask * _CrackDepth);
                    smoothness = lerp(smoothness, 0.1h, crackMask * _CrackDepth);
                    // Deepen cracks in AO
                    occ *= (1.0h - crackMask * _CrackDepth * 0.5h);
                #endif

                #if defined(_DIRT_ON)
                    half dirtMask = DirtOverlay(posWS, N, TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex), _DirtScale, _DirtAmount);
                    albedo = lerp(albedo, _DirtColor.rgb, dirtMask);
                    smoothness = lerp(smoothness, 1.0h - _DirtRoughness, dirtMask);
                #endif

                #if defined(_WATER_STAIN_ON)
                    half stainMask = WaterDamageStain(posWS, N, _StainHeight, _StainSoftness, TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex), _StainNoiseScale);
                    albedo = lerp(albedo, albedo * _StainColor.rgb, stainMask);
                    smoothness = lerp(smoothness, _StainSmoothness, stainMask);
                #endif

                #if defined(_BLOOD_ON)
                    half bloodMask = BloodSplatTriplanar(TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex), posWS, N, _BloodScale, _TriSharpness, _BloodThreshold, _BloodEdge);
                    albedo = lerp(albedo, _BloodColor.rgb, bloodMask);
                    smoothness = lerp(smoothness, _BloodSmoothness, bloodMask);
                #endif

                // ══════════════════════════════════════
                // LIGHTING (PBR)
                // ══════════════════════════════════════

                float4 shadowCoord = TransformWorldToShadowCoord(posWS);
                Light mainLight = GetMainLight(shadowCoord);

                half3 V = (half3)GetWorldSpaceNormalizeViewDir(posWS);
                half3 L = (half3)mainLight.direction;
                half3 H = normalize(V + L);

                half NdotL = saturate(dot(finalN, L));
                half NdotV = saturate(dot(finalN, V));
                half NdotH = saturate(dot(finalN, H));

                half shadow = (half)(mainLight.shadowAttenuation * mainLight.distanceAttenuation);

                // Diffuse
                half3 directDiffuse = albedo * (half3)mainLight.color * NdotL * shadow;

                // Specular (GGX simplified)
                half  perceptualRoughness = 1.0h - smoothness;
                half  roughness = max(perceptualRoughness * perceptualRoughness, 0.002h);
                half  d = NdotH * NdotH * (roughness * roughness - 1.0h) + 1.0001h;
                half  specTerm = roughness * roughness / (d * d * max(0.1h, NdotV) * (roughness * 4.0h + 2.0h));
                half3 F0 = lerp(half3(0.04h, 0.04h, 0.04h), albedo, metallic);
                half3 specular = F0 * specTerm * (half3)mainLight.color * shadow * NdotL;

                // Additional lights
                half3 addColor = half3(0, 0, 0);
                #if defined(_ADDITIONAL_LIGHTS)
                {
                    uint lightCount = GetAdditionalLightsCount();
                    UNITY_LOOP
                    for (uint i = 0u; i < lightCount; i++)
                    {
                        Light addLight = GetAdditionalLight(i, posWS);
                        half addAtten = (half)(addLight.distanceAttenuation * addLight.shadowAttenuation);
                        half addNdotL = saturate(dot(finalN, (half3)addLight.direction));
                        addColor += albedo * (half3)addLight.color * addNdotL * addAtten;
                    }
                }
                #endif

                // GI
                half3 indirect;
                #ifdef LIGHTMAP_ON
                    indirect = SampleLightmap(input.lightmapUV, 0.0, finalN) * albedo;
                #else
                    indirect = SampleSH(finalN) * albedo;
                #endif

                half3 color = (directDiffuse + specular + addColor + indirect) * occ;

                // Emission
                #if defined(_EMISSION)
                    color += _EmissionColor.rgb;
                #endif

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        // ============================================================
        // Pass 1: ShadowCaster
        // ============================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_STEREO_INSTANCING_ENABLED UNITY_STEREO_MULTIVIEW_ENABLED

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    o.positionCS.z = min(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    o.positionCS.z = max(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return o;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // ============================================================
        // Pass 2: DepthOnly
        // ============================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_STEREO_INSTANCING_ENABLED UNITY_STEREO_MULTIVIEW_ENABLED

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }

            half4 DepthFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // ============================================================
        // Pass 3: DepthNormals
        // ============================================================
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DNVert
            #pragma fragment DNFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_STEREO_INSTANCING_ENABLED UNITY_STEREO_MULTIVIEW_ENABLED

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3  normalWS   : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DNVert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.normalWS   = (half3)TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            half4 DNFrag(Varyings input) : SV_Target
            {
                return half4(normalize(input.normalWS), 0.0h);
            }
            ENDHLSL
        }

        // ============================================================
        // Pass 4: Meta
        // ============================================================
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex MetaVert
            #pragma fragment MetaFrag
            #pragma shader_feature_local _EMISSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv0        : TEXCOORD0;
                float2 uv1        : TEXCOORD1;
                float2 uv2        : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings MetaVert(Attributes input)
            {
                Varyings o = (Varyings)0;
                o.positionCS = UnityMetaVertexPosition(input.positionOS.xyz, input.uv1, input.uv2);
                o.uv = input.uv0;
                return o;
            }

            half4 MetaFrag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMapA, sampler_BaseMapA, input.uv) * _BaseColorA;
                MetaInput metaInput = (MetaInput)0;
                metaInput.Albedo = albedo.rgb;
                #if defined(_EMISSION)
                    metaInput.Emission = _EmissionColor.rgb;
                #endif
                return UnityMetaFragment(metaInput);
            }
            ENDHLSL
        }
    }

    CustomEditor "PBRWallBlendGUI"
}
