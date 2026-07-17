Shader "VR/StylizedSkybox_Optimized"
{
    Properties
    {
        _Stars("Stars Texture", 2D) = "black" {}
        _StarsCutoff("Stars Cutoff", Range(0, 1)) = 0.08
        _StarsSpeed("Stars Move Speed", Range(0, 1)) = 0.3
        [HDR]_StarsSkyColor("Stars Sky Color", Color) = (0.0, 0.2, 0.1, 1)
        [Toggle] _EnableStars("Enable Stars", Float) = 1
        _OffsetHorizon("Horizon Offset", Range(-1, 1)) = 0
        _HorizonWidth("Horizon Intensity", Range(0, 10)) = 3.3
        [HDR]_HorizonColorDay("Day Horizon Color", Color) = (0, 0.8, 1, 1)
        [HDR]_HorizonColorNight("Night Horizon Color", Color) = (0, 0.8, 1, 1)
        _HorizonCloudsFade("Fade at horizon", Vector) = (.25, .5, 0, 0)
        [HDR]_SunColor("Sun Color", Color) = (1, 1, 1, 1)
        _SunRadius("Sun Radius", Range(0, 2)) = 0.1
        [HDR]_MoonColor("Moon Color", Color) = (1, 1, 1, 1)
        _MoonRadius("Moon Radius", Range(0, 2)) = 0.15
        _MoonOffset("Moon Crescent", Vector) = (.25, .5, .5, 0)
        _BaseNoise("Base Noise", 2D) = "black" {}
        _BaseNoiseSpeed("Base Noise Speed", Vector) = (.25, .5, 0, 0)
        _Distort("Distort", 2D) = "black" {}
        _SecNoise("Secondary Noise", 2D) = "black" {}
        _BaseNoiseScale("Base Noise Scale", Range(0, 1)) = 0.2
        _DistortScale("Distort Noise Scale", Range(0, 1)) = 0.06
        _SecNoiseScale("Secondary Noise Scale", Range(0, 1)) = 0.05
        _Distortion("Distortion", Range(0, 1)) = 0.1
        _CloudsLayerSpeed("Movement Speed", Vector) = (.25, .5, 0, 0)
        _CloudCutoff("Cloud Cutoff", Range(0, 1)) = 0.3
        _Fuzziness("Cloud Fuzziness", Range(0, 1)) = 0.04
        [Toggle] _EnableSecondLayer("Enable Second Cloud Layer", Float) = 1
        _CloudCutoff2("Cloud Cutoff Secondary", Range(0, 1)) = 0.3
        _Fuzziness2("Cloud Fuzziness Secondary", Range(0, 1)) = 0.04
        _OpacitySec("Secondary Layer Opacity", Range(0, 1)) = 0.04
        _ColorStretch("Color Stretch", Range(-10, 10)) = 0.01
        _ColorOffset("Color Offset", Range(-10, 10)) = 0.04
        [HDR]_DayTopColor("Day Sky Color Top", Color) = (0.4, 1, 1, 1)
        [HDR]_DayBottomColor("Day Sky Color Bottom", Color) = (0, 0.8, 1, 1)
        [HDR]_CloudColorDayEdge("Clouds Edge Day", Color) = (1, 1, 1, 1)
        [HDR]_CloudColorDayMain("Clouds Main Day", Color) = (0.8, 0.9, 0.8, 1)
        [HDR]_NightTopColor("Night Sky Color Top", Color) = (0, 0, 0, 1)
        [HDR]_NightBottomColor("Night Sky Color Bottom", Color) = (0, 0, 0.2, 1)
        [HDR]_CloudColorNightEdge("Clouds Edge Night", Color) = (0, 1, 1, 1)
        [HDR]_CloudColorNightMain("Clouds Main Night", Color) = (0, 0.2, 0.8, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Background"
            "Queue" = "Background"
            "PreviewType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_STEREO_INSTANCING_ENABLED UNITY_STEREO_MULTIVIEW_ENABLED
            #pragma shader_feature_local _ENABLESTARS_ON
            #pragma shader_feature_local _ENABLESECONDLAYER_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half  _SunRadius;
                half  _MoonRadius;
                half4 _MoonOffset;
                half4 _SunColor;
                half4 _MoonColor;
                half4 _DayTopColor;
                half4 _DayBottomColor;
                half4 _NightBottomColor;
                half4 _NightTopColor;
                half4 _HorizonColorDay;
                half4 _HorizonColorNight;
                half  _StarsCutoff;
                half  _StarsSpeed;
                half4 _StarsSkyColor;
                half  _BaseNoiseScale;
                half  _DistortScale;
                half  _SecNoiseScale;
                half  _Distortion;
                half  _CloudCutoff;
                half  _Fuzziness;
                half  _CloudCutoff2;
                half  _Fuzziness2;
                half  _OpacitySec;
                half  _HorizonWidth;
                half  _OffsetHorizon;
                half  _ColorStretch;
                half  _ColorOffset;
                half4 _CloudColorDayEdge;
                half4 _CloudColorDayMain;
                half4 _CloudColorNightEdge;
                half4 _CloudColorNightMain;
                half4 _BaseNoiseSpeed;
                half4 _CloudsLayerSpeed;
                half4 _HorizonCloudsFade;
                float4 _Stars_ST;
                float4 _BaseNoise_ST;
                float4 _Distort_ST;
                float4 _SecNoise_ST;
            CBUFFER_END

            TEXTURE2D(_Stars);       SAMPLER(sampler_Stars);
            TEXTURE2D(_BaseNoise);   SAMPLER(sampler_BaseNoise);
            TEXTURE2D(_Distort);     SAMPLER(sampler_Distort);
            TEXTURE2D(_SecNoise);    SAMPLER(sampler_SecNoise);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS    : SV_POSITION;
                float3 uv            : TEXCOORD0;
                float4 skyData       : TEXCOORD1;
                half4  skyColors     : TEXCOORD2;
                half4  cloudEdgeColor: TEXCOORD3;
                half4  cloudMainColor: TEXCOORD4;
                half4  starData      : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;

                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);
                half rcpY = 1.0h / (abs(worldPos.y) + 0.001h);
                o.skyData.xy = worldPos.xz * rcpY;

                Light mainLight = GetMainLight();
                half3 lightDir = (half3)mainLight.direction;
                half daynightLerp = saturate(lightDir.y + 0.5h);
                o.skyData.z = daynightLerp;
                o.skyData.w = saturate(1.0h - abs((v.uv.y * _HorizonWidth) - _OffsetHorizon));

                half topBottomLerp = saturate(v.uv.y);
                half4 nightSky = lerp(_NightBottomColor, _NightTopColor, topBottomLerp);
                half4 daySky = lerp(_DayBottomColor, _DayTopColor, topBottomLerp);
                half4 dayNightSky = lerp(nightSky, daySky, daynightLerp);
                half4 horizonColor = lerp(_HorizonColorNight, _HorizonColorDay, daynightLerp);
                o.skyColors = lerp(dayNightSky, horizonColor, o.skyData.w);

                o.cloudEdgeColor = lerp(_CloudColorNightEdge, _CloudColorDayEdge, daynightLerp);
                o.cloudMainColor = lerp(_CloudColorNightMain, _CloudColorDayMain, daynightLerp);

                #if defined(_ENABLESTARS_ON)
                    o.starData.xy = o.skyData.xy;
                    o.starData.z = saturate(-lightDir.y * 5.0h);
                    o.starData.w = 0;
                #else
                    o.starData = half4(0, 0, 0, 0);
                #endif

                return o;
            }

            half remap(half In, half2 InMinMax, half2 OutMinMax)
            {
                return OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x)
                       / (InMinMax.y - InMinMax.x);
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                half2 skyUV = i.skyData.xy;
                half daynightLerp = i.skyData.z;

                Light mainLight = GetMainLight();
                half3 lightDir = (half3)mainLight.direction;

                half3 sunDelta = i.uv.xyz - lightDir;
                half sunDistSq = dot(sunDelta, sunDelta);
                half sunRadiusSq = _SunRadius * _SunRadius;
                half sunDisc = saturate((1.0h - sunDistSq / sunRadiusSq) * 50.0h);

                half3 moonDelta = i.uv.xyz + lightDir;
                half moonDistSq = dot(moonDelta, moonDelta);
                half moonRadiusSq = _MoonRadius * _MoonRadius;
                half moonDisc = saturate((1.0h - moonDistSq / moonRadiusSq) * 50.0h);

                half3 crescentDelta = i.uv.xyz + _MoonOffset.xyz + lightDir;
                half crescentDistSq = dot(crescentDelta, crescentDelta);
                half crescentDisc = saturate((1.0h - crescentDistSq / moonRadiusSq) * 50.0h);

                half crescentMoonFinal = saturate(moonDisc - crescentDisc);
                half4 sunMoonColor = crescentMoonFinal * _MoonColor + sunDisc * _SunColor;
                half4 fullSky = i.skyColors + sunMoonColor;

                half timeX = _Time.x;
                half2 baseNoiseUV = (skyUV + timeX * _BaseNoiseSpeed.xy) * _BaseNoiseScale;
                half baseNoise = SAMPLE_TEXTURE2D(_BaseNoise, sampler_BaseNoise, baseNoiseUV).r;

                half2 cloud1UV = (skyUV + baseNoise * _Distortion + timeX * _CloudsLayerSpeed.xy) * _DistortScale;
                half clouds1 = SAMPLE_TEXTURE2D(_Distort, sampler_Distort, cloud1UV).r;

                half cloudsCutoff = saturate(smoothstep(_CloudCutoff, _CloudCutoff + _Fuzziness, clouds1));
                half cloudsStretch = saturate(clouds1 * _ColorStretch + _ColorOffset);
                half4 cloudsColor = lerp(i.cloudEdgeColor, i.cloudMainColor, cloudsStretch);

                half cloudsCutoff2 = 0;
                #if defined(_ENABLESECONDLAYER_ON)
                    half2 cloud2UV = (skyUV + clouds1 + timeX * _CloudsLayerSpeed.xy * 0.5h) * _SecNoiseScale;
                    half clouds2 = SAMPLE_TEXTURE2D(_SecNoise, sampler_SecNoise, cloud2UV).r;
                    cloudsCutoff2 = saturate(smoothstep(_CloudCutoff2, _CloudCutoff2 + _Fuzziness2, clouds2)) * _OpacitySec;
                #endif

                half fadeHorizon = remap(abs(i.uv.y), _HorizonCloudsFade.xy, half2(0, 1));
                half cloudsLerp = saturate(cloudsCutoff2 * fadeHorizon + cloudsCutoff * fadeHorizon * 2.0h);

                half4 starsColor = 0;
                #if defined(_ENABLESTARS_ON)
                    half2 starUV = (i.starData.xy + lightDir.xz * 0.5h) * _StarsSpeed;
                    half4 starsTex = SAMPLE_TEXTURE2D(_Stars, sampler_Stars, starUV);
                    starsTex *= i.starData.z;
                    starsColor = step(_StarsCutoff, starsTex) * _StarsSkyColor * (1.0h - moonDisc);
                #endif

                half4 finalColor = lerp(fullSky + starsColor, cloudsColor, cloudsLerp);
                return finalColor;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
