Shader "Custom/BlinkSphere"
{
    Properties
    {
        _BlinkAmount ("Blink Amount", Range(-1, 2)) = 0.0
        _Color ("Blink Color", Color) = (0, 0, 0, 1)
        _Smoothness ("Edge Smoothness", Range(0.01, 0.5)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        LOD 100

        Cull Front
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 screenUV : TEXCOORD0;
            };

            float _BlinkAmount;
            float4 _Color;
            float _Smoothness;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenUV = ComputeScreenPos(o.pos).xy / o.pos.w;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.screenUV;
                float distanceToCenterY = abs(uv.y - 0.5) * 2.0;
                
                float curve = sin(uv.x * 3.14159) * 0.2;
                float adjustedDistance = distanceToCenterY + curve;

                float threshold = 1.0 - _BlinkAmount;
                float alpha = smoothstep(threshold - _Smoothness, threshold + _Smoothness, adjustedDistance);

                return fixed4(_Color.rgb, alpha * _Color.a);
            }
            ENDHLSL
        }
    }
}