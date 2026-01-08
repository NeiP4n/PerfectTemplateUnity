Shader "PSX/WinterLit"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Color ("Winter Tint", Color) = (0.85, 0.92, 1.0, 1.0)

        _SnowTex ("Snow Texture", 2D) = "white" {}
        _SnowAmount ("Snow Amount", Range(0,1)) = 0.5

        _PixelSize ("Pixel Size (World)", Float) = 0.1
        _ColorVariation ("Color Variation", Range(0,0.3)) = 0.08

        _ResolutionX ("Screen Width", Float) = 320
        _ResolutionY ("Screen Height", Float) = 280
        _DitherStrength ("Dither Strength", Range(0,1)) = 0.5

        [Toggle] _UnlitLighting ("Unlit Color × Point/Spot Light", Float) = 0
        _UnlitBrightness ("Unlit Base Brightness", Range(0,2)) = 0.3

        [Enum(Off,0,On,1)] _AlphaTest ("Alpha Cutout", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="AlphaTest"
            "RenderType"="TransparentCutout"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float4 color      : COLOR;
                float  snow       : TEXCOORD3;
                float  fog        : TEXCOORD4;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_SnowTex);
            SAMPLER(sampler_SnowTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _SnowAmount;
                float  _PixelSize;
                float  _ColorVariation;
                float  _ResolutionX;
                float  _ResolutionY;
                float  _DitherStrength;
                float  _UnlitLighting;
                float  _UnlitBrightness;
                float  _AlphaTest;
                float  _Cutoff;
            CBUFFER_END

            static const float4x4 ditherTable = float4x4(
                float4(0,8,2,10)/16.0,
                float4(12,4,14,6)/16.0,
                float4(3,11,1,9)/16.0,
                float4(15,7,13,5)/16.0
            );

            float hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float4 SampleTriplanar(TEXTURE2D_PARAM(tex, samp), float3 ws, float3 n)
            {
                float3 w = pow(abs(n), 8.0);
                w /= (w.x + w.y + w.z);

                float2 uvX = floor(ws.zy / _PixelSize) * _PixelSize;
                float2 uvY = floor(ws.xz / _PixelSize) * _PixelSize;
                float2 uvZ = floor(ws.xy / _PixelSize) * _PixelSize;

                return
                    SAMPLE_TEXTURE2D(tex, samp, uvX) * w.x +
                    SAMPLE_TEXTURE2D(tex, samp, uvY) * w.y +
                    SAMPLE_TEXTURE2D(tex, samp, uvZ) * w.z;
            }

            Varyings vert (Attributes v)
            {
                Varyings o;

                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = pos.positionCS;

                float4 clip = o.positionCS;
                clip.xy /= clip.w;
                clip.xy = floor(clip.xy * float2(_ResolutionX, _ResolutionY)) / float2(_ResolutionX, _ResolutionY);
                clip.xy *= clip.w;
                o.positionCS = clip;

                o.positionWS = pos.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.snow = saturate(dot(v.normalOS, float3(0,1,0)) * _SnowAmount);
                o.fog = ComputeFogFactor(o.positionCS.z);

                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 snow = SampleTriplanar(TEXTURE2D_ARGS(_SnowTex, sampler_SnowTex), i.positionWS, i.normalWS);

                if (_AlphaTest > 0.5)
                    clip(col.a - _Cutoff);

                col = lerp(col, snow, i.snow);
                col *= i.color;
                col *= _Color;

                float rnd = hash12(floor(i.positionWS.xz / _PixelSize));
                col.rgb *= 1.0 + (rnd - 0.5) * _ColorVariation;

                half3 finalRGB;
                half3 N = normalize(i.normalWS);

                if (_UnlitLighting > 0.5)
                {
                    // Unlit mode: только базовая яркость + Point/Spot без Directional
                    half3 lighting = half3(_UnlitBrightness, _UnlitBrightness, _UnlitBrightness);
                    
                    #ifdef _ADDITIONAL_LIGHTS
                    uint count = GetAdditionalLightsCount();
                    
                    for (uint li = 0; li < count; li++)
                    {
                        Light light = GetAdditionalLight(li, i.positionWS, half4(1,1,1,1));
                        half3 L = normalize(light.direction);
                        half ndl = saturate(dot(N, L));

                        lighting += light.color
                                 * ndl
                                 * light.distanceAttenuation
                                 * light.shadowAttenuation;
                    }
                    #endif

                    finalRGB = col.rgb * lighting;
                }
                else
                {
                    // Standard Lit mode: ambient + все источники света
                    
                    // Ambient освещение из Unity
                    half3 ambient = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
                    // Добавляем базовую яркость
                    half3 lighting = max(ambient, _UnlitBrightness);

                    // Главный свет (Directional)
                    #ifdef _MAIN_LIGHT_SHADOWS
                    float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                    Light mainLight = GetMainLight(shadowCoord);
                    #else
                    Light mainLight = GetMainLight();
                    #endif
                    
                    half3 L = normalize(mainLight.direction);
                    half ndl = saturate(dot(N, L));
                    lighting += mainLight.color * ndl * mainLight.shadowAttenuation;

                    // Дополнительные источники (Point/Spot)
                    #ifdef _ADDITIONAL_LIGHTS
                    uint count = GetAdditionalLightsCount();
                    for (uint li = 0; li < count; li++)
                    {
                        Light light = GetAdditionalLight(li, i.positionWS, half4(1,1,1,1));
                        half3 L2 = normalize(light.direction);
                        half ndl2 = saturate(dot(N, L2));

                        lighting += light.color
                                 * ndl2
                                 * light.distanceAttenuation
                                 * light.shadowAttenuation;
                    }
                    #endif

                    finalRGB = col.rgb * lighting;
                }

                // Dithering
                float2 sp = floor(fmod(i.positionCS.xy, 4));
                float d = ditherTable[sp.x][sp.y];
                finalRGB = floor(finalRGB * 31 + d * _DitherStrength) / 31;

                finalRGB = MixFog(finalRGB, i.fog);
                return half4(finalRGB, col.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}
