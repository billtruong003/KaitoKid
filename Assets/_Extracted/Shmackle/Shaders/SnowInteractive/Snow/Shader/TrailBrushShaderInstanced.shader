Shader "Hidden/TrailBrushInstanced"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
        }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "UnityCG.cginc"

            struct DrawCommandData
            {
                float2 worldPos;
                float radius;
                float strength;
                float4 brushUVRect;
            };

            StructuredBuffer < DrawCommandData> _DrawCommands;
            sampler2D _BrushAtlas;
            float4 _TrailAreaParams;
            // x: centerX, y: centerZ, z: size

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 brushUVRect : TEXCOORD1;
                float strength : TEXCOORD2;
            };

            // Dữ liệu cho một quad (6 đỉnh)
            static const float3 vertices[6] =
            {
                float3(-0.5, -0.5, 0),
                float3(0.5, -0.5, 0),
                float3(0.5, 0.5, 0),
                float3(-0.5, -0.5, 0),
                float3(0.5, 0.5, 0),
                float3(-0.5, 0.5, 0)
            };

            // UV cho quad
            static const float2 uvs[6] =
            {
                float2(0, 0),
                float2(1, 0),
                float2(1, 1),
                float2(0, 0),
                float2(1, 1),
                float2(0, 1)
            };

            v2f vert(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
            {
                DrawCommandData data = _DrawCommands[instanceID];

                // Chuyển world position sang UV space của RenderTexture (0 to 1)
                float2 centerUV = (data.worldPos - _TrailAreaParams.xy) / _TrailAreaParams.z + 0.5;
                // Chuyển world radius sang UV space
                float radiusUV = data.radius / _TrailAreaParams.z;

                // Tính toán vị trí của quad trong clip space
                float3 localPos = vertices[vertexID];
                float2 clipPos = (localPos.xy * radiusUV + centerUV) * 2.0 - 1.0;

                v2f o;
                o.vertex = float4(clipPos, 0.0, 1.0);
                o.uv = uvs[vertexID];
                o.brushUVRect = data.brushUVRect;
                o.strength = data.strength;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Lấy mẫu từ brush atlas
                float2 atlasUV = i.brushUVRect.xy + i.uv * i.brushUVRect.zw;
                float brushValue = tex2D(_BrushAtlas, atlasUV).r;

                // Giá trị cuối cùng chỉ chứa cường độ
                return fixed4(0, 0, 0, brushValue * i.strength);
            }
            ENDHLSL
        }
    }
}
