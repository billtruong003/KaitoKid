Shader "Unlit/SelectableModeFlow"
{
    Properties
    {
        [Header(Flow Mode)]
        [Toggle(_LOCAL_SPACE_FLOW)] _UseLocalSpace("Use Local Space Flow (No UVs Needed)", Float) = 0

        [Header(Base Layer and Master Opacity)]
        _BaseMap("Base Texture (Albedo & Opacity)", 2D) = "white" {}
        _BaseMapScale("Base Map Scale (for Local Space)", Float) = 1.0
        [HDR]_BlendColor("Blend Color (HDR & Opacity)", Color) = (0.1, 0.1, 0.1, 1)
        _BlendAmount("Blend Amount", Range(0.0, 1.0)) = 0.0

        [Header(Energy Flow Layer)]
        _NoiseTex("Noise Texture (Grayscale)", 2D) = "white" {}
        _PulseDirection("Pulse Direction (UV or Local Space)", Vector) = (1, 0, 0, 0)
        _PulseOrigin("Pulse Origin (Local Space)", Vector) = (0, 0, 0, 0)
        [HDR]_PulseColor("Pulse Color (HDR)", Color) = (0, 10, 10, 1)
        _PulseSpeed("Speed", Float) = 1.0
        _PulseWidth("Width", Range(0.01, 10.0)) = 0.2
        _LineDensity("Line Density", Range(1, 100)) = 5.0
        _PulseIntensity("Intensity", Range(0, 20)) = 2.0
        _EdgeSoftness("Edge Softness", Range(0.0, 1.0)) = 0.5
        _NoiseScale("Noise Scale", Float) = 1.0

        [Header(Local Mask)]
        _FlowMaskIntensity("Vertex Color Mask (A)", Range(0, 5)) = 1.0
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent" "Queue" = "Transparent"
        }
        LOD 100

        Pass
        {
            Blend One OneMinusSrcAlpha
            ZWrite On
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Đây là chỉ thị quan trọng để tạo ra lựa chọn bật/tắt
            #pragma shader_feature _LOCAL_SPACE_FLOW
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 localPos : TEXCOORD1;
                float3 localNormal : TEXCOORD2;
                fixed4 color : COLOR;
            };

            sampler2D _BaseMap;
            float _BaseMapScale;
            fixed4 _BlendColor;
            float _BlendAmount;

            sampler2D _NoiseTex;
            float4 _PulseDirection;
            float4 _PulseOrigin;
            fixed4 _PulseColor;
            float _PulseSpeed;
            float _PulseWidth;
            float _LineDensity;
            float _PulseIntensity;
            float _EdgeSoftness;
            float _NoiseScale;
            float _FlowMaskIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.localPos = v.vertex.xyz;
                o.localNormal = v.normal.xyz;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // ---- Lớp Nền (Base Layer) ----
                fixed4 baseTexColor;

                #if _LOCAL_SPACE_FLOW
                    // Chế độ Local Space: Dùng Triplanar Mapping cho Base Map
                    float3 blendWeights = abs(i.localNormal);
                    blendWeights /= (blendWeights.x + blendWeights.y + blendWeights.z);

                    fixed4 x_sample = tex2D(_BaseMap, i.localPos.yz * _BaseMapScale);
                    fixed4 y_sample = tex2D(_BaseMap, i.localPos.xz * _BaseMapScale);
                    fixed4 z_sample = tex2D(_BaseMap, i.localPos.xy * _BaseMapScale);

                    baseTexColor = x_sample * blendWeights.x + y_sample * blendWeights.y + z_sample * blendWeights.z;
                #else
                        // Chế độ UV: Dùng UV thông thường
                    baseTexColor = tex2D(_BaseMap, i.uv);
                #endif

                fixed4 finalBaseColor = lerp(baseTexColor, _BlendColor, _BlendAmount);

                // ---- Lớp Năng Lượng (Energy Flow) ----
                float distanceAlongAxis;
                float2 noiseCoords;

                #if _LOCAL_SPACE_FLOW
                    // Chế độ Local Space: Tính toán dựa trên vị trí 3D
                    float3 pulseDirection = normalize(_PulseDirection.xyz);
                    float3 posFromOrigin = i.localPos - _PulseOrigin.xyz;
                    distanceAlongAxis = dot(posFromOrigin, pulseDirection);
                    noiseCoords = i.localPos.xy * _NoiseScale;
                #else
                        // Chế độ UV: Tính toán dựa trên toạ độ UV
                    float2 pulseDirection = normalize(_PulseDirection.xy);
                    distanceAlongAxis = dot(i.uv, pulseDirection);
                    noiseCoords = i.uv * _NoiseScale;
                #endif

                float animatedTime = _Time.y * _PulseSpeed;
                float repeatingPulseProgress = frac(distanceAlongAxis * _LineDensity - animatedTime);
                float pulseShapeValue = 1.0 - smoothstep(0.0, _PulseWidth * 0.5, abs(repeatingPulseProgress - 0.5));

                float noiseValue = tex2D(_NoiseTex, noiseCoords).r;
                float finalPulseIntensity = pulseShapeValue * noiseValue;

                finalPulseIntensity = smoothstep(0.0, 1.0 - _EdgeSoftness, finalPulseIntensity);

                float vertexMask = i.color.a * _FlowMaskIntensity;
                finalPulseIntensity *= vertexMask;

                fixed3 pulseEmission = _PulseColor.rgb * finalPulseIntensity * _PulseIntensity;

                // ---- Kết hợp cuối cùng (Final Combination) ----
                fixed3 conceptualRGB = finalBaseColor.rgb + pulseEmission;
                float finalAlpha = finalBaseColor.a;
                fixed3 finalPremultipliedRGB = conceptualRGB * finalAlpha;

                return fixed4(finalPremultipliedRGB, finalAlpha);
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}
