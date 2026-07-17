Shader "Ultimate/URP/SusanooFlowNoUV"
{
    Properties
    {
        [Header(Colors)]
        [HDR] _BaseColor("Core Color", Color) = (0.2, 0.0, 0.8, 1.0)
        [HDR] _RimColor("Rim Edge Color", Color) = (0.4, 0.1, 1.0, 1.0)

        [Header(Energy Noise)]
        _NoiseMap("Energy Texture (Grayscale)", 2D) = "white"{}
        _NoiseScale("Noise Tiling", Float) = 1.0
        _FlowSpeed("Flow Speed (World Space)", Vector) = (0.0, 1.0, 0.0, 0.0)
        _EnergyDensity("Energy Density", Range(0.01, 5.0)) = 1.0

        [Header(Toon Effect)]
        _FresnelPower("Rim Sharpness", Range(0.1, 10.0)) = 3.0
        _FresnelThreshold("Rim Threshold", Range(0.0, 1.0)) = 0.5
        _FresnelSmoothness("Rim Smoothness", Range(0.001, 0.5)) = 0.05
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
            Name "SusanooEnergyFlow"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend SrcAlpha One
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            TEXTURE2D(_NoiseMap);
            SAMPLER(sampler_NoiseMap);

            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _RimColor;
            float4 _FlowSpeed;
            float _NoiseScale;
            half _EnergyDensity;
            half _FresnelPower;
            half _FresnelThreshold;
            half _FresnelSmoothness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;

                return output;
            }

            half3 TriplanarSample(float3 positionWS, float3 normalWS, float2 timeOffset)
            {
                float3 blending = abs(normalWS);
                blending = normalize(max(blending, 0.00001));
                float b = (blending.x + blending.y + blending.z);
                blending /= b;

                float2 uvX = positionWS.zy * _NoiseScale + timeOffset;
                float2 uvY = positionWS.xz * _NoiseScale + timeOffset;
                float2 uvZ = positionWS.xy * _NoiseScale + timeOffset;

                half3 colX = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, uvX).rgb;
                half3 colY = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, uvY).rgb;
                half3 colZ = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, uvZ).rgb;

                return colX * blending.x + colY * blending.y + colZ * blending.z;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 normalWS = normalize(input.normalWS);

                float2 flowOffset = _Time.y * _FlowSpeed.xy;
                half noiseVal = TriplanarSample(input.positionWS, normalWS, flowOffset).r;

                half NdotV = saturate(dot(normalWS, viewDirWS));
                half fresnelRaw = pow(1.0 - NdotV, _FresnelPower);

                half toonRim = smoothstep(_FresnelThreshold, _FresnelThreshold + _FresnelSmoothness, fresnelRaw);

                half energyMask = pow(noiseVal, _EnergyDensity);

                half3 coreEmission = _BaseColor.rgb * energyMask * 2.0;
                half3 rimEmission = _RimColor.rgb * toonRim * 4.0;

                half3 finalColor = coreEmission + rimEmission;
                half finalAlpha = saturate((energyMask * _BaseColor.a) + toonRim);

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}
