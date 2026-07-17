Shader "Hyper/DarkSpectrum_AudioReact"
{
    Properties
    {
        [HDR][MainColor] _BaseColor("Tint Color", Color) = (1, 1, 1, 1)
        _ColorRamp("Gradient Ramp", 2D) = "white"{}
        _NoiseTex("Noise Texture (R)", 2D) = "white"{}
        _NoiseTiling("Noise Tiling (XY)", Vector) = (1, 1, 0, 0)
        _Segments("Column Count", Float) = 64.0
        _ScrollSpeed("Scroll Speed", Float) = 0.5
        _MinHeight("Min Height", Range(0.0, 1.0)) = 0.1
        _MaxHeight("Max Height", Range(0.0, 2.0)) = 1.0
        _Gap("Gap Width", Range(0.0, 0.5)) = 0.1
        _FadePower("Top Fade", Range(0.1, 5.0)) = 1.5
        _AudioIntensity("Audio Intensity", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float4 _NoiseTiling;
            float _Segments;
            float _ScrollSpeed;
            float _MinHeight;
            float _MaxHeight;
            float _Gap;
            float _FadePower;
            float _AudioIntensity;
            CBUFFER_END

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_ColorRamp);
            SAMPLER(sampler_ColorRamp);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 uv = input.uv;
                float quantizedX = floor(uv.x * _Segments) / _Segments;

                float2 noiseUV = float2(quantizedX * _NoiseTiling.x, _Time.y * _ScrollSpeed * _NoiseTiling.y);
                half noiseVal = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;

                float dynamicMax = _MaxHeight * _AudioIntensity;
                float height = lerp(_MinHeight, dynamicMax, noiseVal);

                float heightMask = step(uv.y, height);
                float localX = frac(uv.x * _Segments);
                float gapMask = step(_Gap, localX) * step(localX, 1.0 - _Gap);

                float fade = pow(saturate(uv.y / max(0.001, height)), _FadePower);

                half4 rampColor = SAMPLE_TEXTURE2D(_ColorRamp, sampler_ColorRamp, float2(uv.y, 0.5));
                half4 finalColor = rampColor * _BaseColor;

                float finalAlpha = finalColor.a * heightMask * gapMask * (1.0 - fade);

                return half4(finalColor.rgb, finalAlpha);
            }
            ENDHLSL
        }
    }
}
