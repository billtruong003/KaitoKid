Shader "BillEnv/URP/Interactive Snow (VR Optimized) - Vertex"
{
    Properties
    {
        [Header(Main)]
        [MainColor] _SnowColor("Snow Color", Color) = (0.8, 0.85, 0.9, 1.0)
        _MainTex("Snow Texture", 2D) = "white" {}
        _SnowTextureOpacity("Snow Texture Opacity", Range(0, 1)) = 0.3
        _SnowTextureScale("Snow Texture Scale", Range(0, 2)) = 0.3
        [HDR] _ShadowColor("Shadow Color", Color) = (0.5, 0.5, 0.6, 1)

        [Header(Snow Shape)]
        _NoiseTexture("Snow Noise", 2D) = "gray" {}
        _NoiseScale("Noise Scale", Range(0, 100)) = 0.1
        _NoiseWeight("Noise Weight", Range(0, 100)) = 0.1
        _SnowHeight("Snow Height", Range(0, 2)) = 0.3

        [Header(Interactive Path)]
        [HDR] _PathColorIn("Path Inner Color", Color) = (0.7, 0.7, 1.0, 1)
        [HDR] _PathColorOut("Path Outer Color", Color) = (0.4, 0.4, 0.8, 1)
        _PathBlending("Path Blending", Range(0, 3)) = 2
        _SnowPathStrength("Path Strength", Range(0, 4)) = 2
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Includes/SnowFunctions.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vertex
            // Sửa lỗi: Đổi tên fragment shader để tránh trùng lặp với hàm trong SnowFunctions.hlsl
            #pragma fragment ShadowFragment
            #include "Includes/SnowFunctions.hlsl"

            // Hàm này chỉ dành cho ShadowCaster pass và giờ có tên riêng
            float4 ShadowFragment(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Lit"
    CustomEditor "InteractiveSnowShaderGUI"
}
