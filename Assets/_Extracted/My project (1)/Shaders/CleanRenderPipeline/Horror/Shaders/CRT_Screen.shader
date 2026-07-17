Shader "CleanRender/Horror/CRT Screen"
{
    Properties
    {
        [MainTexture] _MainTex("Camera Feed (RT)", 2D) = "black"{}
        _Tint("Tint", Color) = (0.85, 0.95, 0.8, 1)
        _Brightness("Brightness", Range(0, 2)) = 0.9

        [Header(Scanlines)]
        _ScanlineCount("Scanline Count", Float) = 240
        _ScanlineIntensity("Scanline Intensity", Range(0, 1)) = 0.35
        _ScanlineSpeed("Scanline Scroll Speed", Float) = 0.5

        [Header(Barrel Distortion)]
        _BarrelPower("Barrel Distortion", Range(0, 0.5)) = 0.15

        [Header(Static Noise)]
        _NoiseTex("Noise Texture", 2D) = "gray"{}
        _NoiseAmount("Noise Amount", Range(0, 1)) = 0.08
        _NoiseSpeed("Noise Speed", Float) = 15.0

        [Header(Glitch)]
        [Toggle(_GLITCH_ON)] _EnableGlitch("Enable Glitch", Float) = 0
        _GlitchIntensity("Glitch Intensity", Range(0, 1)) = 0.3
        _GlitchSpeed("Glitch Speed", Float) = 3.0
        _GlitchBlockSize("Glitch Block Size", Range(1, 64)) = 16

        [Header(Chromatic Aberration)]
        _ChromaOffset("Chroma Offset", Range(0, 0.02)) = 0.003

        [Header(Vignette)]
        _VignettePower("Vignette Power", Range(0, 3)) = 1.5
        _VignetteStrength("Vignette Strength", Range(0, 1)) = 0.6

        [Header(Flicker)]
        [Toggle(_FLICKER_ON)] _EnableFlicker("Enable Flicker", Float) = 0
        _FlickerSpeed("Flicker Speed", Float) = 25.0

        [Header(Dead Signal)]
        _DeadSignal("Dead Signal (0=live 1=dead)", Range(0, 1)) = 0.0
        _DeadColor("Dead Signal Color", Color) = (0.05, 0.08, 0.05, 1)

        [Header(Options)]
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

        Pass
        {
            Name "CRTForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma shader_feature_local _GLITCH_ON
            #pragma shader_feature_local _FLICKER_ON
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_STEREO_INSTANCING_ENABLED UNITY_STEREO_MULTIVIEW_ENABLED

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Tint;
                half   _Brightness;
                float  _ScanlineCount;
                half   _ScanlineIntensity;
                float  _ScanlineSpeed;
                half   _BarrelPower;
                float4 _NoiseTex_ST;
                half   _NoiseAmount;
                float  _NoiseSpeed;
                half   _GlitchIntensity;
                float  _GlitchSpeed;
                float  _GlitchBlockSize;
                half   _ChromaOffset;
                half   _VignettePower;
                half   _VignetteStrength;
                float  _FlickerSpeed;
                half   _DeadSignal;
                half4  _DeadColor;
                float  _Cull;
            CBUFFER_END

            TEXTURE2D(_MainTex);   SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);  SAMPLER(sampler_NoiseTex);

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ── Hash functions ──
            float Hash11(float p)
            {
                return frac(sin(p * 127.1) * 43758.5453);
            }

            float Hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // ── Barrel distortion ──
            float2 BarrelDistort(float2 uv, float power)
            {
                float2 center = uv - 0.5;
                float r2 = dot(center, center);
                float f = 1.0 + r2 * power;
                return center * f + 0.5;
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
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 uv = input.uv;

                // ── Barrel distortion ──
                float2 distUV = BarrelDistort(uv, _BarrelPower * 4.0);

                // Out of bounds = black border
                half oob = step(0.0, distUV.x) * step(distUV.x, 1.0) * step(0.0, distUV.y) * step(distUV.y, 1.0);

                float2 sampleUV = distUV;

                // ── Glitch horizontal shift ──
                #if defined(_GLITCH_ON)
                {
                    float blockY = floor(distUV.y * _GlitchBlockSize);
                    float glitchTime = floor(_Time.y * _GlitchSpeed);
                    float glitchRand = Hash21(float2(blockY, glitchTime));
                    float trigger = step(1.0 - _GlitchIntensity * 0.3, glitchRand);
                    float shift = (Hash11(blockY + glitchTime * 7.3) - 0.5) * 0.15 * _GlitchIntensity;
                    sampleUV.x += shift * trigger;
                }
                #endif

                // ── Chromatic aberration ──
                half r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV + float2(_ChromaOffset, 0)).r;
                half g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV).g;
                half b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV - float2(_ChromaOffset, 0)).b;
                half3 feed = half3(r, g, b) * _Tint.rgb * _Brightness;

                // ── Scanlines ──
                float scanline = sin((distUV.y + _Time.y * _ScanlineSpeed * 0.01) * _ScanlineCount * 3.14159) * 0.5 + 0.5;
                feed *= lerp(1.0h, (half)scanline, _ScanlineIntensity);

                // ── Rolling bar (thick horizontal line slowly scrolling) ──
                float rollPos = frac(_Time.y * 0.08);
                float rollDist = abs(distUV.y - rollPos);
                half rollBar = 1.0h - (half)smoothstep(0.0, 0.06, rollDist) * 0.15h;
                feed *= rollBar;

                // ── Static noise ──
                float2 noiseUV = distUV * 128.0 + float2(0, _Time.y * _NoiseSpeed);
                half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                feed = lerp(feed, half3(noise, noise, noise), _NoiseAmount);

                // ── Vignette ──
                float2 vig = distUV - 0.5;
                half vigMask = (half)saturate(1.0 - dot(vig, vig) * _VignettePower * 2.0);
                vigMask = pow(vigMask, 0.6);
                feed *= lerp(1.0h, vigMask, _VignetteStrength);

                // ── Flicker ──
                #if defined(_FLICKER_ON)
                {
                    float f = sin(_Time.y * _FlickerSpeed * 43.0) * sin(_Time.y * _FlickerSpeed * 17.3);
                    half flicker = (half)saturate(f * 0.15 + 0.92);
                    feed *= flicker;
                }
                #endif

                // ── Dead signal override ──
                half3 deadNoise = (half3)(Hash21(distUV * 512.0 + _Time.y * 30.0));
                half3 deadScreen = lerp(_DeadColor.rgb, deadNoise * 0.3, 0.5);
                feed = lerp(feed, deadScreen, _DeadSignal);

                // ── Phosphor glow (slight green tinge on bright areas) ──
                half lum = dot(feed, half3(0.299h, 0.587h, 0.114h));
                feed += half3(0, 0.02h, 0) * lum;

                // ── Ambient light response (so CRT glows in dark rooms) ──
                float3 posWS = input.positionWS;
                half3 N = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                half ambient = saturate(dot(N, (half3)mainLight.direction) * 0.3 + 0.7);

                // CRT emits light: mostly self-illuminated, slightly affected by environment
                half3 color = feed * lerp(0.7h, 1.0h, ambient);

                color *= oob;

                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        // ShadowCaster
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex SV
            #pragma fragment SF
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };

            V SV(A i) { V o=(V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); float3 pw=TransformObjectToWorld(i.positionOS.xyz); float3 nw=TransformObjectToWorldNormal(i.normalOS); o.positionCS=TransformWorldToHClip(ApplyShadowBias(pw,nw,_LightDirection));
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
            ZWrite On ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DV
            #pragma fragment DF
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
            V DV(A i) { V o=(V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.positionCS=TransformObjectToHClip(i.positionOS.xyz); return o; }
            half4 DF(V i):SV_Target { return 0; }
            ENDHLSL
        }
    }

    CustomEditor "CRTScreenGUI"
}
