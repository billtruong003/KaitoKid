Shader "Custom/VR_Optimized_Fog"
{
    Properties
    {
        [Header(Core)]
        [MainColor] _BaseColor("Color", Color) = (0.5, 0.6, 0.7, 0.5)

        [Header(Depth Intersection)]
        _Softness("Softness", Range(0.1, 10.0)) = 1.0

        [Header(Height Gradient)]
        _HeightBase("Base Y (Opaque)", Float) = 0.0
        _HeightTop("Top Y (Transparent)", Float) = 5.0

        [Header(Distance Gradient)]
        _DistMin("Start Distance", Float) = 0.0
        _DistMax("End Distance", Float) = 50.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "VRFogPass"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _Softness;
            float _HeightBase;
            float _HeightTop;
            float _DistMin;
            float _DistMax;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.screenPos = ComputeScreenPos(output.positionCS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 screenUV = input.screenPos.xy / input.screenPos.w;

                float rawDepth = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float surfaceDepth = LinearEyeDepth(input.screenPos.w, _ZBufferParams);

                float intersectMask = saturate((sceneDepth - surfaceDepth) / _Softness);

                float heightFactor = (_HeightTop - input.positionWS.y) / (_HeightTop - _HeightBase);
                float heightMask = saturate(heightFactor);

                float distToCam = length(input.positionWS - _WorldSpaceCameraPos);
                float distMask = saturate((distToCam - _DistMin) / (_DistMax - _DistMin));

                half finalAlpha = _BaseColor.a * intersectMask * heightMask * distMask;

                return half4(_BaseColor.rgb, finalAlpha);
            }
            ENDHLSL
        }
    }
}
