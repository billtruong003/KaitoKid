Shader "Hidden/KawaseBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv[5] : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _PixelOffset;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float2 T = _MainTex_TexelSize.xy * _PixelOffset;
                o.uv[0] = v.uv;
                o.uv[1] = v.uv + T;
                o.uv[2] = v.uv - T;
                o.uv[3] = v.uv + float2(T.x, -T.y);
                o.uv[4] = v.uv + float2(-T.x, T.y);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = 0;
                col += tex2D(_MainTex, i.uv[0]);
                col += tex2D(_MainTex, i.uv[1]);
                col += tex2D(_MainTex, i.uv[2]);
                col += tex2D(_MainTex, i.uv[3]);
                col += tex2D(_MainTex, i.uv[4]);
                return col / 5.0;
            }
            ENDHLSL
        }
    }
}
