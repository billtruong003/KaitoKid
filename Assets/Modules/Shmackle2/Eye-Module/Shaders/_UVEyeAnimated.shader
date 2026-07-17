Shader "Universal Render Pipeline/Custom/UVEyeAnimated"
{
    Properties
    {
        [Header(Sclera Configuration)]
        _ScleraColor ("Sclera Color", Color) = (0.95, 0.95, 0.95, 1)
        _VeinTex ("Vein Noise Map", 2D) = "white"{}
        _VeinColor ("Vein Color", Color) = (0.8, 0.0, 0.0, 1)
        _VeinThreshold ("Vein Threshold", Range(0, 1)) = 0.55
        _VeinSoftness ("Vein Softness", Range(0.001, 0.2)) = 0.05

        [Header(Iris Configuration)]
        _IrisTex ("Iris Texture", 2D) = "black"{}
        _IrisColor ("Iris Tint", Color) = (1, 1, 1, 1)
        _IrisScale ("Iris Scale", Range(0.1, 5.0)) = 1.0

        [Header(Internal Data)]
        _PupilOffset ("Pupil UV Offset", Vector) = (0, 0, 0, 0)
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
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            TEXTURE2D(_VeinTex);
            SAMPLER(sampler_VeinTex);
            TEXTURE2D(_IrisTex);
            SAMPLER(sampler_IrisTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _VeinTex_ST;
            float4 _ScleraColor;
            float4 _VeinColor;
            float4 _IrisColor;
            float2 _PupilOffset;
            float _VeinThreshold;
            float _VeinSoftness;
            float _IrisScale;
            CBUFFER_END

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _VeinTex);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float veinNoise = SAMPLE_TEXTURE2D(_VeinTex, sampler_VeinTex, input.uv).r;
                float veinMask = smoothstep(_VeinThreshold, _VeinThreshold + _VeinSoftness, veinNoise);
                half3 sclera = lerp(_ScleraColor.rgb, _VeinColor.rgb, veinMask);

                float2 centeredUV = input.uv - 0.5;
                float2 scaledUV = centeredUV / _IrisScale;
                float2 irisUV = scaledUV + 0.5 - (_PupilOffset / _IrisScale);

                half4 irisSample = SAMPLE_TEXTURE2D(_IrisTex, sampler_IrisTex, irisUV);

                float2 dist = abs(irisUV - 0.5);
                float bounds = step(dist.x, 0.5) * step(dist.y, 0.5);
                float irisAlpha = irisSample.a * bounds;

                half3 finalRGB = lerp(sclera, irisSample.rgb * _IrisColor.rgb, irisAlpha);

                return half4(finalRGB, 1.0);
            }
            ENDHLSL
        }
    }
}
