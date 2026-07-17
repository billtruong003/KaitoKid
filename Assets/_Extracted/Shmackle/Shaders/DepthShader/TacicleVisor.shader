Shader "VR/ARTacticalVisor_TextureScan"
{
    Properties
    {
        [Header(Control)]
        _EffectProgress("Scan Progress", Range(0, 1)) = 0.0

        [Header(Visuals)]
        _MainTex("Pattern Texture", 2D) = "white"{}
        [HDR] _MainColor("Pattern Color", Color) = (0, 1, 1, 1)
        [HDR] _PulseColor("Pulse Color", Color) = (1, 0.2, 0, 1)

        [Header(Settings)]
        _TextureScale("Texture Scale", Float) = 1.0
        _PulseWidth("Pulse Width", Float) = 1.5
        _MaxDistance("Max Distance", Float) = 50.0
        _EdgeFade("Edge Fade", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+100"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ARVisorTexturePass"
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _MainColor;
            float4 _PulseColor;
            float _EffectProgress;
            float _TextureScale;
            float _PulseWidth;
            float _MaxDistance;
            float _EdgeFade;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.screenPos = ComputeScreenPos(vertexInput.positionCS);
                    // Vector từ Camera đến Vertex của Visor
                output.viewDirWS = vertexInput.positionWS - GetCameraPositionWS();

                return output;
            }

                // Tái tạo vị trí thế giới từ Depth Texture
            float3 ReconstructWorldPosition(float2 screenUV, float3 viewDirWS)
            {
                float rawDepth = SampleSceneDepth(screenUV);
                float linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

                float3 viewDirNorm = normalize(viewDirWS);
                    // Chiếu vector view lên vector forward của camera để sửa méo phối cảnh
                float3 forward = GetViewForwardDir();
                float dist = linearDepth / dot(viewDirNorm, forward);

                return GetCameraPositionWS() + viewDirNorm * dist;
            }

                // Triplanar Mapping không cần Mesh Normal (tính Normal từ đạo hàm vị trí)
            float3 GetTriplanarTexture(float3 worldPos, float scale)
            {
                    // Tính toán Normal của bề mặt dựa trên độ thay đổi của WorldPos pixel
                float3 dx = ddx(worldPos);
                float3 dy = ddy(worldPos);
                float3 normal = normalize(cross(dx, dy));
                float3 blending = abs(normal);

                    // Chuẩn hóa blending weights để tổng = 1
                blending /= (blending.x + blending.y + blending.z);

                    // Sample texture 3 hướng
                float2 uvX = worldPos.zy * scale;
                float2 uvY = worldPos.xz * scale;
                float2 uvZ = worldPos.xy * scale;

                float3 colX = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvX).rgb;
                float3 colY = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvY).rgb;
                float3 colZ = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvZ).rgb;

                    // Blend lại
                return colX * blending.x + colY * blending.y + colZ * blending.z;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);

                    // Loại bỏ background (bầu trời)
                if (Linear01Depth(rawDepth, _ZBufferParams) >= 0.99)
                    return 0;

                float3 worldPos = ReconstructWorldPosition(screenUV, input.viewDirWS);
                float distToCam = distance(worldPos, GetCameraPositionWS());

                    // Logic Radius
                float currentRadius = _EffectProgress * _MaxDistance;

                    // 1. TEXTURE LAYER (Hiện ra trong vùng đã quét)
                float3 texColor = GetTriplanarTexture(worldPos, _TextureScale) * _MainColor.rgb;
                    // Mask để chỉ hiện texture bên trong vòng quét
                float revealMask = 1.0 - smoothstep(currentRadius, currentRadius + _EdgeFade, distToCam);

                    // 2. PULSE LAYER (Vòng sáng di chuyển)
                    // Tạo một vòng ring rực sáng tại vị trí currentRadius
                float pulseDist = abs(distToCam - currentRadius);
                float pulseMask = 1.0 - smoothstep(0.0, _PulseWidth, pulseDist);
                    // Làm cho pulse sắc cạnh hơn
                pulseMask = pow(pulseMask, 3.0);

                    // 3. COMBINE
                    // Texture hiện nền + Pulse đè lên trên (Additive)
                float3 finalRGB = (texColor * revealMask) + (_PulseColor.rgb * pulseMask);

                    // Alpha logic: Texture mờ hơn Pulse
                float alpha = saturate((length(texColor) * revealMask * 0.8) + pulseMask);

                    // Fade xa dần để không bị cắt đột ngột ở MaxDistance
                float globalFade = 1.0 - smoothstep(_MaxDistance * 0.8, _MaxDistance, distToCam);

                return half4(finalRGB, alpha * globalFade);
            }
            ENDHLSL
        }
    }
}
