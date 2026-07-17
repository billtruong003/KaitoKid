// BowDraw.shader — Unity URP
//
// Weight tính tự động từ khoảng cách vertex đến pivot (Object Space).
// Không cần paint vertex color.
//
// Cách hoạt động:
//   t = |dot(posOS, spineAxis)| / halfBowLength   → 0 ở center, 1 ở tip
//   weight = pow(t, curveExponent)                 → curve như beam deflection
//   displacement = bendAxisOS * weight * drawAmount * maxDisplacement

Shader "Custom/URP/BowDraw"
{
    Properties
    {
        // ── Surface ──────────────────────────────────────────────────
        _BaseMap        ("Albedo",      2D)             = "white" {}
        _BaseColor      ("Base Color",  Color)          = (1,1,1,1)
        _Smoothness     ("Smoothness",  Range(0,1))     = 0.5
        _Metallic       ("Metallic",    Range(0,1))     = 0.0

        // ── Bow params (set bởi BowDrawComponent) ────────────────────
        [HideInInspector] _DrawAmount      ("Draw Amount",       Range(0,1))  = 0
        [HideInInspector] _MaxDisplacement ("Max Displacement",  Float)       = 0.25
        [HideInInspector] _HalfBowLength   ("Half Bow Length",   Float)       = 0.5
        [HideInInspector] _CurveExponent   ("Curve Exponent",    Float)       = 2
        [HideInInspector] _SpineAxisOS     ("Spine Axis OS",     Vector)      = (0,1,0,0)
        [HideInInspector] _BendAxisOS      ("Bend Axis OS",      Vector)      = (0,0,-1,0)

        // ── Debug ─────────────────────────────────────────────────────
        [Toggle] _ShowWeight ("Debug: Show Weight Gradient", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }

        // ════════════════════════════════════════════════════════════
        // ForwardLit Pass
        // ════════════════════════════════════════════════════════════
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   BowVert
            #pragma fragment BowFrag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma shader_feature_local _SHOWWEIGHT_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half   _Smoothness;
                half   _Metallic;

                float  _DrawAmount;
                float  _MaxDisplacement;
                float  _HalfBowLength;
                float  _CurveExponent;
                float4 _SpineAxisOS;    // xyz = spine direction (local), w = 0
                float4 _BendAxisOS;     // xyz = bend direction  (local), w = 0
                float  _ShowWeight;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float  weight     : TEXCOORD3;  // debug
                float  fogFactor  : TEXCOORD4;
            };

            // ── Hàm tính displacement weight ────────────────────────
            // Vertex ở càng gần pivot (center) → weight càng gần 0 → càng cứng.
            // Vertex ở tip → weight ≈ 1 → bị kéo nhiều nhất.
            float CalcBowWeight(float3 posOS, float3 spineDir, float halfLen, float exponent)
            {
                // Project vị trí vertex lên spine để lấy khoảng cách từ center
                float distAlongSpine = abs(dot(posOS, spineDir));

                // Normalize về [0..1]
                float t = saturate(distAlongSpine / halfLen);

                // Áp dụng curve — pow(t,2) = parabolic như beam deflection thật
                return pow(t, exponent);
            }

            // ════════════════════════════════════════════════════════
            // Vertex Shader
            // ════════════════════════════════════════════════════════
            Varyings BowVert(Attributes IN)
            {
                Varyings OUT;

                float3 spineDir = normalize(_SpineAxisOS.xyz);
                float3 bendDir  = normalize(_BendAxisOS.xyz);

                // Weight thuần từ geometry — không cần vertex color
                float w = CalcBowWeight(IN.positionOS, spineDir, _HalfBowLength, _CurveExponent);

                // Displacement trong Object Space
                float3 dispOS = bendDir * (w * _DrawAmount * _MaxDisplacement);

                float3 displacedOS = IN.positionOS + dispOS;

                // WS transforms
                float3 posWS = TransformObjectToWorld(displacedOS);

                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.positionWS = posWS;
                // Normal cần recalculate gần đúng — với bend nhỏ TransformObjectToWorldNormal đủ dùng
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.weight     = w;
                OUT.fogFactor  = ComputeFogFactor(OUT.positionCS.z);

                return OUT;
            }

            // ════════════════════════════════════════════════════════
            // Fragment Shader
            // ════════════════════════════════════════════════════════
            half4 BowFrag(Varyings IN) : SV_Target
            {
                // Debug: visualize weight gradient (đen = cứng, đỏ = mềm/tip)
                #if defined(_SHOWWEIGHT_ON)
                    return half4(IN.weight, 0, 0, 1);
                #endif

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                InputData   li = (InputData)0;
                SurfaceData sd = (SurfaceData)0;

                li.positionWS       = IN.positionWS;
                li.normalWS         = normalize(IN.normalWS);
                li.viewDirectionWS  = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                li.shadowCoord      = TransformWorldToShadowCoord(IN.positionWS);
                li.fogCoord         = IN.fogFactor;
                li.bakedGI          = SampleSH(li.normalWS);

                sd.albedo      = albedo.rgb;
                sd.alpha       = albedo.a;
                sd.smoothness  = _Smoothness;
                sd.metallic    = _Metallic;
                sd.occlusion   = 1.0;
                sd.normalTS    = half3(0, 0, 1);

                half4 color = UniversalFragmentPBR(li, sd);
                color.rgb   = MixFog(color.rgb, IN.fogFactor);

                return color;
            }

            ENDHLSL
        }

        // ════════════════════════════════════════════════════════════
        // ShadowCaster — displacement phải khớp để bóng không sai
        // ════════════════════════════════════════════════════════════
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex   ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half   _Smoothness;
                half   _Metallic;
                float  _DrawAmount;
                float  _MaxDisplacement;
                float  _HalfBowLength;
                float  _CurveExponent;
                float4 _SpineAxisOS;
                float4 _BendAxisOS;
                float  _ShowWeight;
            CBUFFER_END

            struct AttrShadow { float3 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct VaryShadow { float4 positionCS : SV_POSITION; };

            float CalcBowWeight(float3 posOS, float3 spineDir, float halfLen, float exponent)
            {
                float t = saturate(abs(dot(posOS, spineDir)) / halfLen);
                return pow(t, exponent);
            }

            VaryShadow ShadowVert(AttrShadow IN)
            {
                VaryShadow OUT;

                float3 spineDir  = normalize(_SpineAxisOS.xyz);
                float3 bendDir   = normalize(_BendAxisOS.xyz);
                float  w         = CalcBowWeight(IN.positionOS, spineDir, _HalfBowLength, _CurveExponent);
                float3 dispOS    = bendDir * (w * _DrawAmount * _MaxDisplacement);
                float3 posWS     = TransformObjectToWorld(IN.positionOS + dispOS);
                float3 normalWS  = TransformObjectToWorldNormal(IN.normalOS);

                OUT.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, float3(0,0,0)));
                return OUT;
            }

            half4 ShadowFrag(VaryShadow IN) : SV_Target { return 0; }

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
