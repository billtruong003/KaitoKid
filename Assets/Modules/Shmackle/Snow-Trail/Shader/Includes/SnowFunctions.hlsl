#ifndef MINIONS_ART_SNOW_FUNCTIONS_INCLUDED
#define MINIONS_ART_SNOW_FUNCTIONS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _SnowColor, _ShadowColor, _PathColorIn, _PathColorOut;
    float _SnowHeight, _SnowTextureOpacity, _SnowTextureScale, _NoiseScale;
    float _NoiseWeight, _PathBlending, _SnowPathStrength;
CBUFFER_END

TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
TEXTURE2D(_NoiseTexture); SAMPLER(sampler_NoiseTexture);
TEXTURE2D(_GlobalEffectRT); SAMPLER(sampler_GlobalEffectRT);

float3 _InteractorPosition;
float _OrthographicCamSize;

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv           : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
    float3 positionWS   : TEXCOORD1;
    float3 normalWS     : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

float GetDeformationStrength(float2 worldPosXZ)
{
    float2 rtUV = (worldPosXZ - _InteractorPosition.xz) / (_OrthographicCamSize * 2.0) + 0.5;
    if (rtUV.x < 0 || rtUV.x > 1 || rtUV.y < 0 || rtUV.y > 1)
    {
        return 0.0;
    }
    float effectSample = SAMPLE_TEXTURE2D_LOD(_GlobalEffectRT, sampler_GlobalEffectRT, rtUV, 0).r;
    
    // Làm mờ cạnh của vùng tương tác để tránh bị ngắt đột ngột
    float edgeFade = smoothstep(0.0, 0.05, rtUV.x) * smoothstep(1.0, 0.95, rtUV.x);
    edgeFade *= smoothstep(0.0, 0.05, rtUV.y) * smoothstep(1.0, 0.95, rtUV.y);
    return effectSample * edgeFade;
}

Varyings Vertex(Attributes IN)
{
    Varyings OUT;
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

    OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
    OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
    
    float baseDeformation = GetDeformationStrength(OUT.positionWS.xz);
    float snowNoise = SAMPLE_TEXTURE2D_LOD(_NoiseTexture, sampler_NoiseTexture, OUT.positionWS.xz * _NoiseScale, 0).r;
    
    // Cải tiến: Sử dụng smoothstep để tạo độ lún mềm mại hơn ở các cạnh
    float deformationFactor = 1.0 - smoothstep(0.0, 0.7, baseDeformation * _SnowPathStrength);
    
    float heightDisplacement = (_SnowHeight + (snowNoise * _NoiseWeight)) * deformationFactor;
    
    OUT.positionWS += OUT.normalWS * heightDisplacement;
    OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
    OUT.uv = IN.uv;

    return OUT;
}

float4 Fragment(Varyings IN) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

    float deformation = GetDeformationStrength(IN.positionWS.xz);
    float saturatedEffect = saturate(deformation);

    float3 snowTexture = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv * _SnowTextureScale).rgb;
    float3 mainColors = lerp(_SnowColor.rgb, snowTexture * _SnowColor.rgb, _SnowTextureOpacity);

    float3 path = lerp(_PathColorOut.rgb, _PathColorIn.rgb, saturate(saturatedEffect * _PathBlending));
    float3 albedo = lerp(mainColors, path, saturatedEffect);

    float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
    Light mainLight = GetMainLight(shadowCoord);
    
    float NdotL = saturate(dot(IN.normalWS, mainLight.direction));
    float3 attenuatedLightColor = mainLight.color * mainLight.shadowAttenuation;
    float3 finalLighting = lerp(_ShadowColor.rgb, attenuatedLightColor, NdotL);
    
    float3 finalColor = albedo * finalLighting;
    
    return float4(finalColor, 1.0);
}

#endif