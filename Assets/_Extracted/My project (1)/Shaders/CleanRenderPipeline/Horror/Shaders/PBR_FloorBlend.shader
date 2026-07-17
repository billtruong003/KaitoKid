Shader "CleanRender/Horror/PBR Floor Blend"
{
    Properties
    {
        // ── Layer A (Base: Concrete/Tile) ──
        [MainTexture] _BaseMapA("Base A", 2D) = "white"{}
        [MainColor]   _BaseColorA("Color A", Color) = (0.5, 0.48, 0.44, 1)
        _BumpMapA("Normal A", 2D) = "bump"{}
        _BumpScaleA("Normal Scale A", Range(0, 2)) = 1.0
        _SmoothnessA("Smoothness A", Range(0, 1)) = 0.3

        // ── Layer B (Overlay: Damaged/Exposed) ──
        _BaseMapB("Base B", 2D) = "white"{}
        _BaseColorB("Color B", Color) = (0.35, 0.3, 0.25, 1)
        _BumpMapB("Normal B", 2D) = "bump"{}
        _BumpScaleB("Normal Scale B", Range(0, 2)) = 1.0
        _SmoothnessB("Smoothness B", Range(0, 1)) = 0.15
        _ScaleB("Scale B", Float) = 1.0

        // ── Triplanar ──
        _BaseScale("Triplanar Scale", Float) = 0.5
        _TriSharpness("Tri Sharpness", Range(1, 16)) = 4.0

        // ── Blend ──
        _NoiseTex("Blend Noise", 2D) = "gray"{}
        _BlendThreshold("Blend Threshold", Range(0, 1)) = 0.5
        _BlendEdge("Blend Edge Softness", Range(0.001, 0.5)) = 0.05
        _BlendNoiseScale("Blend Noise Scale", Float) = 1.0

        // ── PBR ──
        _OcclusionMap("Occlusion", 2D) = "white"{}
        _OcclusionStrength("Occlusion Str", Range(0, 1)) = 1.0

        // ── Dirt ──
        [Toggle(_DIRT_ON)] _EnableDirt("Enable Dirt", Float) = 0
        _DirtColor("Dirt Color", Color) = (0.15, 0.12, 0.08, 1)
        _DirtScale("Dirt Scale", Float) = 2.0
        _DirtAmount("Dirt Amount", Range(0, 1)) = 0.5

        // ── Blood Puddle ──
        [Toggle(_BLOOD_ON)] _EnableBlood("Enable Blood", Float) = 0
        _BloodColor("Blood Color", Color) = (0.3, 0.01, 0.01, 1)
        _BloodScale("Blood Scale", Float) = 1.5
        _BloodThreshold("Blood Threshold", Range(0, 1)) = 0.65
        _BloodEdge("Blood Edge", Range(0.01, 0.3)) = 0.08
        _BloodSmoothness("Blood Smoothness", Range(0, 1)) = 0.85

        // ── Wet Floor ──
        [Toggle(_WET_ON)] _EnableWet("Enable Wet Floor", Float) = 0
        _WetAmount("Wet Amount", Range(0, 1)) = 0.5
        _WetDarken("Wet Darken", Range(0, 0.5)) = 0.2
        _WetSmoothness("Wet Smoothness", Range(0, 1)) = 0.8

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
            float4 _BaseMapA_ST;
            half4  _BaseColorA;
            half   _BumpScaleA;
            half   _SmoothnessA;
            float4 _BaseMapB_ST;
            half4  _BaseColorB;
            half   _BumpScaleB;
            half   _SmoothnessB;
            float  _ScaleB;
            float  _BaseScale;
            half   _TriSharpness;
            float  _BlendNoiseScale;
            half   _BlendThreshold;
            half   _BlendEdge;
            half   _OcclusionStrength;
            half4  _DirtColor;
            float  _DirtScale;
            half   _DirtAmount;
            half4  _BloodColor;
            float  _BloodScale;
            half   _BloodThreshold;
            half   _BloodEdge;
            half   _BloodSmoothness;
            half   _WetAmount;
            half   _WetDarken;
            half   _WetSmoothness;
            half4  _EmissionColor;
            float  _Cull;
        CBUFFER_END

        TEXTURE2D(_BaseMapA);  SAMPLER(sampler_BaseMapA);
        TEXTURE2D(_BumpMapA);  SAMPLER(sampler_BumpMapA);
        TEXTURE2D(_BaseMapB);  SAMPLER(sampler_BaseMapB);
        TEXTURE2D(_BumpMapB);  SAMPLER(sampler_BumpMapB);
        TEXTURE2D(_NoiseTex);  SAMPLER(sampler_NoiseTex);
        TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local _DIRT_ON
            #pragma shader_feature_local _BLOOD_ON
            #pragma shader_feature_local _WET_ON
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

            half3 SampleTriplanarNormal(TEXTURE2D_PARAM(nmap, nsamp), TriplanarUV tp, half scale)
            {
                half3 nX = UnpackNormalScale(SAMPLE_TEXTURE2D(nmap, nsamp, tp.uvX), scale);
                half3 nY = UnpackNormalScale(SAMPLE_TEXTURE2D(nmap, nsamp, tp.uvY), scale);
                half3 nZ = UnpackNormalScale(SAMPLE_TEXTURE2D(nmap, nsamp, tp.uvZ), scale);
                return normalize(nX * tp.blend.x + nY * tp.blend.y + nZ * tp.blend.z);
            }

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
                o.tangentWS  = half4((half3)TransformObjectToWorldDir(input.tangentOS.xyz), (half)sign);
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
                float3 posWS = input.positionWS;
                half3  N = normalize(input.normalWS);

                // Floor uses mostly Y-projected UVs (top-down)
                TriplanarUV tpA = ComputeTriplanarUV(posWS, N, _BaseScale, _TriSharpness);
                TriplanarUV tpB = ComputeTriplanarUV(posWS, N, _BaseScale * _ScaleB, _TriSharpness);

                half4 albedoA = SampleTriplanar(TEXTURE2D_ARGS(_BaseMapA, sampler_BaseMapA), tpA) * _BaseColorA;
                half3 normalA = SampleTriplanarNormal(TEXTURE2D_ARGS(_BumpMapA, sampler_BumpMapA), tpA, _BumpScaleA);
                half4 albedoB = SampleTriplanar(TEXTURE2D_ARGS(_BaseMapB, sampler_BaseMapB), tpB) * _BaseColorB;
                half3 normalB = SampleTriplanarNormal(TEXTURE2D_ARGS(_BumpMapB, sampler_BumpMapB), tpB, _BumpScaleB);

                TriplanarUV tpNoise = ComputeTriplanarUV(posWS, N, _BlendNoiseScale, _TriSharpness);
                half blendNoise = SampleTriplanarR(TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex), tpNoise);
                half blendMask = (half)smoothstep(_BlendThreshold - _BlendEdge, _BlendThreshold + _BlendEdge, blendNoise);

                half3 albedo     = lerp(albedoA.rgb, albedoB.rgb, blendMask);
                half3 normalTS   = normalize(lerp(normalA, normalB, blendMask));
                half  smoothness = lerp(_SmoothnessA, _SmoothnessB, blendMask);

                half3 bitangent = cross(N, input.tangentWS.xyz) * input.tangentWS.w;
                half3 finalN = normalize(normalTS.x * input.tangentWS.xyz + normalTS.y * bitangent + normalTS.z * N);

                TriplanarUV tpOcc = ComputeTriplanarUV(posWS, N, _BaseScale, _TriSharpness);
                half occ = lerp(1.0h, SampleTriplanarR(TEXTURE2D_ARGS(_OcclusionMap, sampler_OcclusionMap), tpOcc), _OcclusionStrength);

                // ── Horror overlays ──
                #if defined(_DIRT_ON)
                    half dirtMask = DirtOverlay(posWS, N, TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex), _DirtScale, _DirtAmount);
                    albedo = lerp(albedo, _DirtColor.rgb, dirtMask);
                    smoothness *= (1.0h - dirtMask * 0.5h);
                #endif

                #if defined(_BLOOD_ON)
                    half bloodMask = BloodSplatTriplanar(TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex), posWS, N, _BloodScale, _TriSharpness, _BloodThreshold, _BloodEdge);
                    albedo = lerp(albedo, _BloodColor.rgb, bloodMask);
                    smoothness = lerp(smoothness, _BloodSmoothness, bloodMask);
                #endif

                #if defined(_WET_ON)
                    TriplanarUV tpWet = ComputeTriplanarUV(posWS, N, 2.0, _TriSharpness);
                    half wetNoise = SampleTriplanarR(TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex), tpWet);
                    half wetMask = (half)smoothstep(1.0 - _WetAmount, 1.0, wetNoise);
                    // Floors facing up get more wet
                    wetMask *= saturate((half)N.y);
                    albedo *= lerp(1.0h, 1.0h - _WetDarken, wetMask);
                    smoothness = lerp(smoothness, _WetSmoothness, wetMask);
                #endif

                // ── PBR Lighting (same as WallBlend) ──
                float4 shadowCoord = TransformWorldToShadowCoord(posWS);
                Light mainLight = GetMainLight(shadowCoord);
                half3 V = (half3)GetWorldSpaceNormalizeViewDir(posWS);
                half3 L = (half3)mainLight.direction;
                half3 H = normalize(V + L);
                half NdotL = saturate(dot(finalN, L));
                half NdotV = saturate(dot(finalN, V));
                half NdotH = saturate(dot(finalN, H));
                half shadow = (half)(mainLight.shadowAttenuation * mainLight.distanceAttenuation);

                half3 directDiffuse = albedo * (half3)mainLight.color * NdotL * shadow;
                half perceptualRoughness = 1.0h - smoothness;
                half roughness = max(perceptualRoughness * perceptualRoughness, 0.002h);
                half d = NdotH * NdotH * (roughness * roughness - 1.0h) + 1.0001h;
                half specTerm = roughness * roughness / (d * d * max(0.1h, NdotV) * (roughness * 4.0h + 2.0h));
                half3 specular = half3(0.04h, 0.04h, 0.04h) * specTerm * (half3)mainLight.color * shadow * NdotL;

                half3 addColor = half3(0, 0, 0);
                #if defined(_ADDITIONAL_LIGHTS)
                {
                    uint lc = GetAdditionalLightsCount();
                    UNITY_LOOP
                    for (uint i = 0u; i < lc; i++)
                    {
                        Light al = GetAdditionalLight(i, posWS);
                        half aa = (half)(al.distanceAttenuation * al.shadowAttenuation);
                        half aN = saturate(dot(finalN, (half3)al.direction));
                        addColor += albedo * (half3)al.color * aN * aa;
                    }
                }
                #endif

                half3 indirect;
                #ifdef LIGHTMAP_ON
                    indirect = SampleLightmap(input.lightmapUV, 0.0, finalN) * albedo;
                #else
                    indirect = SampleSH(finalN) * albedo;
                #endif

                half3 color = (directDiffuse + specular + addColor + indirect) * occ;

                #if defined(_EMISSION)
                    color += _EmissionColor.rgb;
                #endif

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        // ShadowCaster
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex SV
            #pragma fragment SF
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
            V SV(A i){V o=(V)0;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);float3 pw=TransformObjectToWorld(i.positionOS.xyz);float3 nw=TransformObjectToWorldNormal(i.normalOS);o.positionCS=TransformWorldToHClip(ApplyShadowBias(pw,nw,_LightDirection));
            #if UNITY_REVERSED_Z
                o.positionCS.z=min(o.positionCS.z,UNITY_NEAR_CLIP_VALUE);
            #else
                o.positionCS.z=max(o.positionCS.z,UNITY_NEAR_CLIP_VALUE);
            #endif
            return o;}
            half4 SF(V i):SV_Target{return 0;}
            ENDHLSL
        }

        // DepthOnly
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask R Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex DV
            #pragma fragment DF
            #pragma multi_compile_instancing
            struct A { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
            V DV(A i){V o=(V)0;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.positionCS=TransformObjectToHClip(i.positionOS.xyz);return o;}
            half4 DF(V i):SV_Target{return 0;}
            ENDHLSL
        }

        // DepthNormals
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex DNV
            #pragma fragment DNF
            #pragma multi_compile_instancing
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; half3 normalWS : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V DNV(A i){V o=(V)0;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.normalWS=(half3)TransformObjectToWorldNormal(i.normalOS);return o;}
            half4 DNF(V i):SV_Target{return half4(normalize(i.normalWS),0);}
            ENDHLSL
        }

        // Meta
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
            V MV(A i){V o=(V)0;o.positionCS=UnityMetaVertexPosition(i.positionOS.xyz,i.uv1,i.uv2);o.uv=i.uv0;return o;}
            half4 MF(V i):SV_Target{half4 a=SAMPLE_TEXTURE2D(_BaseMapA,sampler_BaseMapA,i.uv)*_BaseColorA;MetaInput mi=(MetaInput)0;mi.Albedo=a.rgb;
            #if defined(_EMISSION)
                mi.Emission=_EmissionColor.rgb;
            #endif
            return UnityMetaFragment(mi);}
            ENDHLSL
        }
    }

    CustomEditor "PBRFloorBlendGUI"
}
