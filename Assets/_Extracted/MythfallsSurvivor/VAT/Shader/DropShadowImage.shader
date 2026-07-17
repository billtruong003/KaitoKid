Shader "UI/DropShadow URP"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // Shadow Properties
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.6)
        _ShadowOffsetX ("Shadow Offset X", Float) = 3.0
        _ShadowOffsetY ("Shadow Offset Y", Float) = -3.0
        _ShadowBlur ("Shadow Blur", Range(0.0, 8.0)) = 2.5

        // Stencil
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent" 
            "IgnoreProjector" = "True" 
            "RenderType" = "Transparent" 
            "PreviewType" = "Plane"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        // ==================== PASS 1: SHADOW ====================
        Pass
        {
            Name "Shadow"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragShadow
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex           : SV_POSITION;
                half4 color             : COLOR;        // ← sửa thành half4
                float2 texcoord         : TEXCOORD0;
                float4 worldPosition    : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _ShadowColor;
            float _ShadowOffsetX;
            float _ShadowOffsetY;
            float _ShadowBlur;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            half SampleAlpha(float2 uv)
            {
                return tex2D(_MainTex, uv).a;
            }

            half4 fragShadow(v2f i) : SV_Target
            {
                float2 offset = float2(_ShadowOffsetX, _ShadowOffsetY) * 0.01;
                float2 uv = i.texcoord + offset;

                half alpha = 0;
                float blur = _ShadowBlur * 0.005;

                // 3x3 blur
                alpha += SampleAlpha(uv + float2(-blur, -blur));
                alpha += SampleAlpha(uv + float2(    0, -blur));
                alpha += SampleAlpha(uv + float2( blur, -blur));

                alpha += SampleAlpha(uv + float2(-blur,    0));
                alpha += SampleAlpha(uv + float2(    0,    0));
                alpha += SampleAlpha(uv + float2( blur,    0));

                alpha += SampleAlpha(uv + float2(-blur,  blur));
                alpha += SampleAlpha(uv + float2(    0,  blur));
                alpha += SampleAlpha(uv + float2( blur,  blur));

                alpha /= 9.0;

                half4 shadow = _ShadowColor;
                shadow.a *= alpha * i.color.a;

                return shadow;
            }
            ENDHLSL
        }

        // ==================== PASS 2: MAIN IMAGE ====================
        Pass
        {
            Name "Default"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                half4 color          : COLOR;           // ← sửa thành half4
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _Color;
            float4 _ClipRect;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 color = tex2D(_MainTex, i.texcoord) * i.color * _Color;

                // Clip Rect (hỗ trợ Mask)
                color.a *= step(0.0, i.worldPosition.x * _ClipRect.z + _ClipRect.x) *
                           step(0.0, i.worldPosition.y * _ClipRect.w + _ClipRect.y) *
                           step(0.0, -i.worldPosition.x * _ClipRect.z - _ClipRect.x) *
                           step(0.0, -i.worldPosition.y * _ClipRect.w - _ClipRect.y);

                return color;
            }
            ENDHLSL
        }
    }
}