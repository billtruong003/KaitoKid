Shader "CleanRender/Horror/Convex Mirror"
{
    Properties
    {
        _MainTex("Mirror RT", 2D) = "black"{}
        _Tint("Tint", Color) = (0.8, 0.85, 0.9, 1)
        _Brightness("Brightness", Range(0, 2)) = 0.7

        [Header(Distortion)]
        _BarrelPower("Barrel Distortion", Range(0, 1)) = 0.4
        _EdgeDarken("Edge Darken", Range(0, 1)) = 0.5

        [Header(Surface)]
        _Metallic("Metallic", Range(0, 1)) = 0.8
        _Smoothness("Smoothness", Range(0, 1)) = 0.7
        _BumpMap("Frame Normal", 2D) = "bump"{}
        _BumpScale("Normal Scale", Range(0, 2)) = 0.5

        [Header(Dirt)]
        _NoiseTex("Dirt Noise", 2D) = "white"{}
        _DirtAmount("Dirt Amount", Range(0, 1)) = 0.2
        _DirtColor("Dirt Color", Color) = (0.3, 0.28, 0.25, 1)

        [Header(Flicker)]
        [Toggle(_FLICKER_ON)] _EnableFlicker("Enable Flicker", Float) = 0
        _FlickerSpeed("Flicker Speed", Float) = 20.0
        _FlickerIntensity("Flicker Intensity", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local _FLICKER_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_STEREO_INSTANCING_ENABLED UNITY_STEREO_MULTIVIEW_ENABLED

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Tint;
                half   _Brightness;
                half   _BarrelPower;
                half   _EdgeDarken;
                half   _Metallic;
                half   _Smoothness;
                half   _BumpScale;
                float4 _NoiseTex_ST;
                half   _DirtAmount;
                half4  _DirtColor;
                float  _FlickerSpeed;
                half   _FlickerIntensity;
            CBUFFER_END

            TEXTURE2D(_MainTex);  SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);  SAMPLER(sampler_BumpMap);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3  normalWS   : TEXCOORD1;
                half4  tangentWS  : TEXCOORD2;
                float2 uv         : TEXCOORD3;
                half   fogFactor  : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float2 BarrelDistort(float2 uv, float power)
            {
                float2 c = uv - 0.5;
                float r2 = dot(c, c);
                return c * (1.0 + r2 * power * 4.0) + 0.5;
            }

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = (half3)TransformObjectToWorldNormal(input.normalOS);
                real sign = input.tangentOS.w * GetOddNegativeScale();
                o.tangentWS = half4((half3)TransformObjectToWorldDir(input.tangentOS.xyz), (half)sign);
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                o.fogFactor = (half)ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 uv = input.uv;
                float2 mirrorUV = BarrelDistort(uv, _BarrelPower);

                // Mirror reflection
                half3 reflection = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, mirrorUV).rgb * _Tint.rgb * _Brightness;

                // Edge darkening (convex mirror gets dark at edges)
                float2 edgeDist = mirrorUV - 0.5;
                half edgeMask = 1.0h - (half)saturate(dot(edgeDist, edgeDist) * 4.0) * _EdgeDarken;
                reflection *= edgeMask;

                // Dirt overlay
                half dirt = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * 3.0).r;
                half dirtMask = (half)smoothstep(1.0 - _DirtAmount, 1.0, dirt);
                reflection = lerp(reflection, _DirtColor.rgb, dirtMask * 0.7);

                // Flicker
                #if defined(_FLICKER_ON)
                {
                    float f = sin(_Time.y * _FlickerSpeed * 43.0) * sin(_Time.y * _FlickerSpeed * 17.3) * sin(_Time.y * _FlickerSpeed * 7.7);
                    half flicker = (half)saturate(f * _FlickerIntensity + (1.0 - _FlickerIntensity));
                    reflection *= flicker;
                }
                #endif

                // PBR frame: the mirror surface itself catches specular highlights
                half3 N = normalize(input.normalWS);
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
                half3 bitangent = cross(N, input.tangentWS.xyz) * input.tangentWS.w;
                half3 finalN = normalize(normalTS.x * input.tangentWS.xyz + normalTS.y * bitangent + normalTS.z * N);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half3 V = (half3)GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 H = normalize(V + (half3)mainLight.direction);
                half NdotH = saturate(dot(finalN, H));

                // Specular highlight on glass surface
                half spec = pow(NdotH, 64.0h) * _Smoothness * (half)mainLight.shadowAttenuation;
                half3 specColor = (half3)mainLight.color * spec * 0.5h;

                half3 color = reflection + specColor;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        // ShadowCaster
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex SV
            #pragma fragment SF
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V{float4 positionCS:SV_POSITION;UNITY_VERTEX_OUTPUT_STEREO};
            V SV(A i){V o=(V)0;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);float3 pw=TransformObjectToWorld(i.positionOS.xyz);float3 nw=TransformObjectToWorldNormal(i.normalOS);o.positionCS=TransformWorldToHClip(ApplyShadowBias(pw,nw,_LightDirection));
            #if UNITY_REVERSED_Z
                o.positionCS.z=min(o.positionCS.z,UNITY_NEAR_CLIP_VALUE);
            #else
                o.positionCS.z=max(o.positionCS.z,UNITY_NEAR_CLIP_VALUE);
            #endif
            return o;}
            half4 SF(V i):SV_Target{return 0;}
            ENDHLSL
        }

        // DepthOnly
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex DV
            #pragma fragment DF
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A{float4 positionOS:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V{float4 positionCS:SV_POSITION;UNITY_VERTEX_OUTPUT_STEREO};
            V DV(A i){V o=(V)0;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.positionCS=TransformObjectToHClip(i.positionOS.xyz);return o;}
            half4 DF(V i):SV_Target{return 0;}
            ENDHLSL
        }
    }

    CustomEditor "ConvexMirrorGUI"
}
