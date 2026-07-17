Shader "CleanRender/Horror/Glitch Entity"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white"{}
        [MainColor]   _BaseColor("Base Color", Color) = (0.1, 0.1, 0.12, 1)

        [Header(Vertex Displacement)]
        _NoiseTex("Displacement Noise", 2D) = "gray"{}
        _DisplaceStrength("Displace Strength", Range(0, 0.5)) = 0.08
        _DisplaceSpeed("Displace Speed", Float) = 3.0
        _DisplaceScale("Displace Scale", Float) = 5.0

        [Header(Slice Glitch)]
        [Toggle(_SLICE_ON)] _EnableSlice("Enable Slice", Float) = 1
        _SliceCount("Slice Count", Range(1, 64)) = 12
        _SliceShift("Slice Shift", Range(0, 0.3)) = 0.1
        _SliceSpeed("Slice Speed", Float) = 5.0

        [Header(Pixelation)]
        [Toggle(_PIXEL_ON)] _EnablePixel("Enable Pixelation", Float) = 0
        _PixelSize("Pixel Size", Range(2, 128)) = 32

        [Header(Dissolve)]
        [Toggle(_DISSOLVE_ON)] _EnableDissolve("Enable Dissolve", Float) = 0
        _DissolveAmount("Dissolve Amount", Range(0, 1)) = 0.0
        _DissolveEdge("Dissolve Edge Width", Range(0.01, 0.2)) = 0.05
        [HDR] _DissolveEdgeColor("Dissolve Edge Color", Color) = (0, 3, 1, 1)

        [Header(Emission)]
        [Toggle(_EMISSION)] _EnableEmission("Enable Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0.5, 0.3, 1)
        _EmissionPulseSpeed("Pulse Speed", Float) = 2.0

        [Header(Options)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _BaseColor;
            float4 _NoiseTex_ST;
            half   _DisplaceStrength;
            float  _DisplaceSpeed;
            float  _DisplaceScale;
            float  _SliceCount;
            half   _SliceShift;
            float  _SliceSpeed;
            float  _PixelSize;
            half   _DissolveAmount;
            half   _DissolveEdge;
            half4  _DissolveEdgeColor;
            half4  _EmissionColor;
            float  _EmissionPulseSpeed;
            float  _Cull;
            half   _Cutoff;
        CBUFFER_END

        TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
        TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

        float GlitchHash(float n) { return frac(sin(n) * 43758.5453); }
        float GlitchHash2(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma shader_feature_local _SLICE_ON
            #pragma shader_feature_local _PIXEL_ON
            #pragma shader_feature_local _DISSOLVE_ON
            #pragma shader_feature_local _EMISSION

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_STEREO_INSTANCING_ENABLED UNITY_STEREO_MULTIVIEW_ENABLED

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3  normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
                half   fogFactor  : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 posOS = input.positionOS.xyz;
                o.positionOS = posOS;

                // ── Vertex displacement via noise ──
                float t = _Time.y * _DisplaceSpeed;
                float2 noiseUV = posOS.xy * _DisplaceScale + float2(t, t * 0.7);
                float noise = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, noiseUV * 0.1, 0).r;
                float3 displace = input.normalOS * (noise - 0.5) * 2.0 * _DisplaceStrength;

                // ── Slice glitch: horizontal slices shift randomly ──
                #if defined(_SLICE_ON)
                {
                    float sliceY = floor(posOS.y * _SliceCount);
                    float sliceTime = floor(_Time.y * _SliceSpeed);
                    float sliceRand = GlitchHash(sliceY + sliceTime * 13.7);
                    float trigger = step(0.7, sliceRand);
                    float shift = (GlitchHash(sliceY * 3.1 + sliceTime) - 0.5) * _SliceShift;
                    displace.x += shift * trigger;
                }
                #endif

                posOS += displace;
                o.positionWS = TransformObjectToWorld(posOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS   = (half3)TransformObjectToWorldNormal(input.normalOS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.fogFactor = (half)ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 uv = input.uv;

                // ── Pixelation ──
                #if defined(_PIXEL_ON)
                {
                    uv = floor(uv * _PixelSize) / _PixelSize;
                }
                #endif

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;

                // ── Dissolve ──
                #if defined(_DISSOLVE_ON)
                {
                    float2 dUV = input.positionOS.xy * 3.0 + _Time.y * 0.5;
                    half dissolveNoise = (half)GlitchHash2(floor(dUV * 32.0));
                    clip(dissolveNoise - _DissolveAmount);
                    // Edge glow
                    half edgeMask = 1.0h - (half)smoothstep(0.0, _DissolveEdge, dissolveNoise - _DissolveAmount);
                    albedo.rgb += _DissolveEdgeColor.rgb * edgeMask;
                }
                #endif

                // ── Simple lighting ──
                half3 N = normalize(input.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half NdotL = saturate(dot(N, (half3)mainLight.direction));
                half shadow = (half)mainLight.shadowAttenuation;

                half3 color = albedo.rgb * (half3)mainLight.color * NdotL * shadow;
                half3 ambient = SampleSH(N) * albedo.rgb * 0.4h;
                color += ambient;

                // ── Emission with pulse ──
                #if defined(_EMISSION)
                {
                    half pulse = (half)(sin(_Time.y * _EmissionPulseSpeed * 6.2831) * 0.3 + 0.7);
                    color += _EmissionColor.rgb * pulse;
                }
                #endif

                // ── Scanline-like horizontal lines on the entity ──
                half scan = (half)(sin(input.positionOS.y * 80.0 + _Time.y * 10.0) * 0.5 + 0.5);
                color *= lerp(0.85h, 1.0h, scan);

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
            V DV(A i) { V o=(V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.positionCS=TransformObjectToHClip(i.positionOS.xyz); return o; }
            half4 DF(V i):SV_Target { return 0; }
            ENDHLSL
        }
    }

    CustomEditor "GlitchEntityGUI"
}
