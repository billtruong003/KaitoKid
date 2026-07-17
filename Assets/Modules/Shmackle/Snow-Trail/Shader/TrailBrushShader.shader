Shader "Hidden/TrailBlitShader"
{
    Properties
    {
        _MainTex ("Previous Frame", 2D) = "white" {}
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _BrushTex;
            // x: brushCenterU, y: brushCenterV, z: brushRadiusUV, w: strength
            float4 _BrushParams;

            fixed4 frag(v2f i) : SV_Target
            {
                float2 brushUV = (i.uv - _BrushParams.xy) / _BrushParams.z + 0.5;

                float brushValue = 0;
                if (all(saturate(brushUV) == brushUV))
                {
                    brushValue = tex2D(_BrushTex, brushUV).r * _BrushParams.w;
                }

                float originalValue = tex2D(_MainTex, i.uv).r;
                float finalValue = max(originalValue, brushValue);
                return fixed4(finalValue.rrr, 1);
            }
            ENDHLSL
        }
    }
}
