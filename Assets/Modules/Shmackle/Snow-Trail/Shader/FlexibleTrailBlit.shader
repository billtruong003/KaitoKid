Shader "Hidden/FlexibleTrailBlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BrushCenterUV ("Brush Center UV", Vector) = (0.5, 0.5, 0, 0)
        _BrushRadius ("Brush Radius", Float) = 0.1
        _BrushStrength ("Brush Strength", Range(0, 1)) = 1.0
        _BrushShape ("Brush Shape", Int) = 0
        _BrushTexture ("Brush Texture", 2D) = "white" {}
        _UseTextureBrush ("Use Texture Brush", Float) = 0.0
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
            float4 _BrushCenterUV;
            float _BrushRadius;
            float _BrushStrength;
            int _BrushShape;
            sampler2D _BrushTexture;
            float _UseTextureBrush;

            float sdf_triangle(float2 p, float r)
            {
                const float k = sqrt(3.0);
                p.x = abs(p.x) - r;
                p.y = p.y + r / k;
                if (p.x + k * p.y > 0.0) p = float2(p.x - k * p.y, -k * p.x - p.y) / 2.0;
                    p.x -= clamp(p.x, -2.0 * r, 0.0);
                return - length(p) * sign(p.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 originalColor = tex2D(_MainTex, i.uv);
                float brushValue = 0.0;

                if (_UseTextureBrush > 0.5)
                {
                    float2 brushUV = (i.uv - _BrushCenterUV.xy) / (_BrushRadius * 2.0) + 0.5;
                    if (all(brushUV > 0) && all(brushUV < 1))
                    {
                        brushValue = tex2D(_BrushTexture, brushUV).r;
                    }
                }
                else
                {
                    float dist;
                    if (_BrushShape == 1)// Square
                    {
                        dist = max(abs(i.uv.x - _BrushCenterUV.x), abs(i.uv.y - _BrushCenterUV.y));
                    }
                    else if (_BrushShape == 2)// Triangle
                    {
                        float2 p = i.uv - _BrushCenterUV.xy;
                        p.y *= -1.0;
                        // Adjust for UV space
                        dist = sdf_triangle(p, _BrushRadius) * -0.5 / _BrushRadius;
                        dist = 1.0 - (dist + 0.5);
                    }
                    else// Circle (default)
                    {
                        dist = distance(i.uv, _BrushCenterUV.xy);
                    }

                    brushValue = 1.0 - smoothstep(_BrushRadius - 0.01, _BrushRadius, dist);
                }

                brushValue *= _BrushStrength;
                float finalValue = max(originalColor.g, brushValue);
                return fixed4(0, finalValue, 0, 1);
            }
            ENDHLSL
        }
    }
}
