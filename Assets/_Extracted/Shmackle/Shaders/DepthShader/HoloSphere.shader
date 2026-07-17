Shader "VR/OptimizedHoloScanner"
{
    Properties
    {
        [Header(Visuals)]
        _MainColor("Scanner Color", Color) = (0, 0.8, 1, 1)
        _EdgeColor("Pulse Edge Color", Color) = (1, 1, 1, 1)
        _ScanTex("Scan Pattern (Grayscale)", 2D) = "white"{}

        [Header(Configuration)]
        _ScanParams("Speed(X) Width(Y) Radius(Z) Tiling(W)", Vector) = (2.0, 0.2, 5.0, 1.0)
        _EffectParams("Fresnel(X) Intersection(Y) TextureStr(Z) Alpha(W)", Vector) = (4.0, 1.0, 0.5, 0.3)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+100"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "HoloScanner"
            Blend SrcAlpha One
            ZWrite Off
            ZTest Always
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _MainColor;
            float4 _EdgeColor;
            float4 _ScanParams;
            float4 _EffectParams;
            float4 _ScanTex_ST;
            CBUFFER_END

            TEXTURE2D(_ScanTex);
            SAMPLER(sampler_ScanTex);

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.screenPos = ComputeScreenPos(output.positionCS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 screenUV = input.screenPos.xy / input.screenPos.w;

                #if UNITY_REVERSED_Z
                    float depth = SampleSceneDepth(screenUV);
                #else
                        float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(screenUV));
                #endif

                float3 worldPosReconstructed = ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);
                float3 centerPos = float3(unity_ObjectToWorld[0].w, unity_ObjectToWorld[1].w, unity_ObjectToWorld[2].w);

                float dist = distance(centerPos, worldPosReconstructed);

                float wave = frac(_Time.y * _ScanParams.x);
                float currentRadius = _ScanParams.z * wave;

                float mask = smoothstep(currentRadius + _ScanParams.y, currentRadius, dist) *
                smoothstep(currentRadius - _ScanParams.y, currentRadius, dist);

                float2 texUV = worldPosReconstructed.xz * _ScanParams.w;
                half pattern = SAMPLE_TEXTURE2D(_ScanTex, sampler_ScanTex, texUV).r;

                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);
                half fresnel = pow(1.0 - saturate(dot(input.normalWS, viewDir)), _EffectParams.x);

                float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
                float objectDepth = LinearEyeDepth(input.screenPos.z / input.screenPos.w, _ZBufferParams);
                half depthDiff = saturate((linearDepth - objectDepth) * _EffectParams.y);
                half intersection = 1.0 - depthDiff;

                half3 finalColor = _MainColor.rgb * (pattern * _EffectParams.z + 0.2);

                half pulseIntensity = mask * 2.0;
                half3 edgeEffect = _EdgeColor.rgb * pulseIntensity;

                half alpha = saturate(fresnel + intersection + mask * pattern);
                alpha *= _EffectParams.w;

                return half4(finalColor + edgeEffect, alpha);
            }
            ENDHLSL
        }
    }
}
