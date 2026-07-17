Shader "Hidden/SnowEffects"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SecondaryTex("Secondary Texture", 2D) = "black" {}
        // Thêm texture phụ
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // PASS 0: HEALING (Không đổi)
        Pass
        {
            Name "Healing"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_heal
            #include "UnityCG.cginc"

            struct appdata
            { float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            { float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            v2f vert (appdata v)
            { v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _HealingRate;
            float _DeltaTime;

            fixed4 frag_heal (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                col.r = max(0, col.r - _HealingRate * _DeltaTime);
                return col;
            }
            ENDHLSL
        }

        // PASS 1: MAX BLEND (Pass mới)
        // Dùng để trộn các vết lún mới (vẽ bằng instancing) vào RT cũ
        Pass
        {
            Name "MaxBlend"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_blend
            #include "UnityCG.cginc"

            struct appdata
            { float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            { float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            v2f vert (appdata v)
            { v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _SecondaryTex;

            fixed4 frag_blend(v2f i) : SV_Target
            {
                float oldDepth = tex2D(_MainTex, i.uv).r;
                float newDepth = tex2D(_SecondaryTex, i.uv).a;
                // Lấy từ kênh alpha của RT vẽ instanced

                // Lấy giá trị lớn hơn để các vết lún chồng lên nhau
                float finalDepth = max(oldDepth, newDepth);

                return fixed4(finalDepth.rrr, 1);
            }
            ENDHLSL
        }
    }
}
