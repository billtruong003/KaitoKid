Shader "CleanRender/Horror/Fullscreen Horror"
{
    Properties
    {
        _MainTex("Source", 2D) = "white"{}

        [Header(Drowsiness Vignette)]
        _DrowsyAmount("Drowsy Amount (0=awake 1=asleep)", Range(0, 1)) = 0.0
        _DrowsyColor("Drowsy Color", Color) = (0.02, 0.01, 0.02, 1)

        [Header(Eye Blink)]
        _BlinkAmount("Blink Close (0=open 1=closed)", Range(0, 1)) = 0.0

        [Header(Portal Warp)]
        _WarpAmount("Warp Amount (0=none 1=full)", Range(0, 1)) = 0.0
        _WarpCenter("Warp Center", Vector) = (0.5, 0.5, 0, 0)
        _WarpStrength("Warp Strength", Range(0, 3)) = 2.0

        [Header(Chromatic Aberration Burst)]
        _ChromaBurst("Chroma Burst", Range(0, 0.05)) = 0.0

        [Header(Damage Flash)]
        _DamageFlash("Damage Flash", Range(0, 1)) = 0.0
        _DamageColor("Damage Color", Color) = (0.6, 0.02, 0.02, 1)

        [Header(Peephole)]
        _PeepholeAmount("Peephole (0=off 1=full)", Range(0, 1)) = 0.0
        _PeepholeRadius("Peephole Radius", Range(0.05, 0.5)) = 0.15
        _PeepholeFisheye("Fisheye Strength", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "FullscreenHorror"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half   _DrowsyAmount;
                half4  _DrowsyColor;
                half   _BlinkAmount;
                half   _WarpAmount;
                float4 _WarpCenter;
                half   _WarpStrength;
                half   _ChromaBurst;
                half   _DamageFlash;
                half4  _DamageColor;
                half   _PeepholeAmount;
                half   _PeepholeRadius;
                half   _PeepholeFisheye;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // ══════════════════════════════
                // PEEPHOLE MODE
                // ══════════════════════════════
                half peepholeMask = 1.0h;
                if (_PeepholeAmount > 0.01)
                {
                    float2 center = float2(0.5, 0.5);
                    float2 delta = uv - center;

                    // Aspect ratio correction
                    float aspect = _ScreenParams.x / _ScreenParams.y;
                    delta.x *= aspect;

                    float dist = length(delta);
                    float radius = _PeepholeRadius;

                    // Fisheye distortion inside the hole
                    float2 fishDelta = (uv - center);
                    float r2 = dot(fishDelta, fishDelta);
                    float fishPower = _PeepholeFisheye * 4.0;
                    uv = center + fishDelta * (1.0 + r2 * fishPower);

                    // Circular mask with soft edge
                    peepholeMask = (half)(1.0 - smoothstep(radius - 0.02, radius + 0.005, dist));
                    peepholeMask = lerp(1.0h, peepholeMask, _PeepholeAmount);
                }

                // ══════════════════════════════
                // PORTAL WARP
                // ══════════════════════════════
                if (_WarpAmount > 0.01)
                {
                    float2 wCenter = _WarpCenter.xy;
                    float2 wDelta = uv - wCenter;
                    float wDist = length(wDelta);
                    float warp = _WarpAmount * _WarpStrength;
                    float pull = warp / (wDist * 10.0 + 1.0);
                    uv = lerp(uv, wCenter, pull * _WarpAmount);
                }

                // ══════════════════════════════
                // CHROMATIC ABERRATION
                // ══════════════════════════════
                half3 color;
                if (_ChromaBurst > 0.001)
                {
                    float2 dir = uv - 0.5;
                    float2 offset = dir * _ChromaBurst;
                    color.r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset).r;
                    color.g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).g;
                    color.b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - offset).b;
                }
                else
                {
                    color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
                }

                // ══════════════════════════════
                // DROWSINESS VIGNETTE
                // ══════════════════════════════
                if (_DrowsyAmount > 0.01)
                {
                    float2 vig = uv - 0.5;
                    float vigDist = dot(vig, vig);

                    // Progressive vignette: starts from edges, creeps inward
                    float vigRadius = lerp(2.0, 0.05, _DrowsyAmount);
                    half vigMask = (half)smoothstep(0.0, vigRadius, vigDist);
                    color = lerp(color, _DrowsyColor.rgb, vigMask);

                    // Desaturation as drowsiness increases
                    half lum = dot(color, half3(0.299h, 0.587h, 0.114h));
                    color = lerp(color, half3(lum, lum, lum), _DrowsyAmount * 0.6h);
                }

                // ══════════════════════════════
                // EYE BLINK (top/bottom black bars closing)
                // ══════════════════════════════
                if (_BlinkAmount > 0.01)
                {
                    float blinkEdge = _BlinkAmount * 0.5;
                    float topBar = smoothstep(blinkEdge, blinkEdge - 0.02, uv.y);
                    float botBar = smoothstep(1.0 - blinkEdge, 1.0 - blinkEdge + 0.02, uv.y);
                    half blink = (half)max(topBar, botBar);
                    color = lerp(color, half3(0, 0, 0), blink);
                }

                // ══════════════════════════════
                // DAMAGE FLASH
                // ══════════════════════════════
                color = lerp(color, _DamageColor.rgb, _DamageFlash);

                // ══════════════════════════════
                // PEEPHOLE DARKNESS
                // ══════════════════════════════
                color *= peepholeMask;

                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }

    CustomEditor "FullscreenHorrorGUI"
}
