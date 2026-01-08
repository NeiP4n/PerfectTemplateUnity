Shader "URP/OutlineUnlit_Pencil"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Float) = 0.015
        _NoiseScale ("Noise Scale", Float) = 40
        _Breakup ("Line Breakup", Range(0,1)) = 0.45
        _Alpha ("Alpha", Range(0,1)) = 1
        _EdgeSoftness ("Edge Softness", Range(0,1)) = 0.3
        _BlurStrength ("Blur Strength", Range(0,1)) = 0.5
        _AnimationSpeed ("Animation Speed", Float) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            Name "Outline"
            Cull Off  // Двусторонняя отрисовка
            ZWrite On
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
                float3 viewDir     : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float _OutlineWidth;
                float4 _OutlineColor;
                float _NoiseScale;
                float _Breakup;
                float _Alpha;
                float _EdgeSoftness;
                float _BlurStrength;
                float _AnimationSpeed;
            CBUFFER_END

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Улучшенный шум с octaves для более детального эффекта
            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for(int i = 0; i < 3; i++)
                {
                    value += amplitude * noise(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            Varyings vert (Attributes v)
            {
                Varyings o;

                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                
                // Анимированная вариация ширины для "дрожащего" эффекта
                float timeOffset = _Time.y * _AnimationSpeed * 0.3;
                float noiseVariation = noise(positionWS.xz * 5.0 + timeOffset) * 0.3 + 0.85;
                float width = _OutlineWidth * noiseVariation;
                
                // Экструзия по нормали
                positionWS += normalWS * width;

                o.positionHCS = TransformWorldToHClip(positionWS);
                o.worldPos = positionWS;
                o.viewDir = normalize(_WorldSpaceCameraPos - positionWS);

                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // Анимация шума со временем
                float timeScroll = _Time.y * _AnimationSpeed;
                float2 noiseUV = i.worldPos.xz * _NoiseScale;
                
                // Многослойный анимированный шум
                float noise1 = fbm(noiseUV + float2(timeScroll, timeScroll * 0.5));
                float noise2 = fbm(noiseUV * 1.7 + float2(-timeScroll * 0.7, timeScroll * 0.9)) * 0.5;
                float combinedNoise = (noise1 + noise2) * 0.66;

                // Мягкий градиент с расплывчатыми краями
                float breakupThreshold = _Breakup + _EdgeSoftness * (combinedNoise - 0.5);
                float edgeGradient = smoothstep(
                    breakupThreshold - _BlurStrength * 0.15, 
                    breakupThreshold + _BlurStrength * 0.15, 
                    combinedNoise
                );

                // Более мягкое отсечение
                if (edgeGradient < 0.05)
                    discard;

                // Вариация нажима карандаша с анимацией
                float pressureVariation = lerp(0.3, 1.0, combinedNoise);
                
                // Затухание по краям для мягкости
                float edgeFade = pow(edgeGradient, 0.8);
                
                float alpha = _Alpha * pressureVariation * edgeFade;

                // Легкое осветление на краях штрихов
                float3 finalColor = lerp(
                    _OutlineColor.rgb * 1.2, 
                    _OutlineColor.rgb, 
                    edgeGradient
                );

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
}
