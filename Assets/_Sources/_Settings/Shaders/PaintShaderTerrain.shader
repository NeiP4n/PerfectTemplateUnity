Shader "PSX/Winter/Lite"
{
    Properties
    {
        [Header(Base)]
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)

        [Header(Render Mode)]
        [Toggle] _UseLighting ("Enable Lite Lighting", Float) = 0
        [Toggle] _BothSidesLighting ("Both Sides Lighting", Float) = 1

        [Header(Transparency Foliage)]
        [Toggle] _AlphaClip ("Alpha Clip (Foliage)", Float) = 0
        _AlphaCutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        [Toggle] _DisableTriplanar ("Disable Triplanar (Foliage)", Float) = 0

        [Header(Winter)]
        [Toggle] _EnableSnow ("Enable Snow", Float) = 0
        _SnowTex ("Snow Texture", 2D) = "white" {}
        _SnowAmount ("Snow Amount", Range(0,1)) = 0.5

        [Header(PSX Pixelation)]
        _ResolutionX ("Screen Width", Float) = 320
        _ResolutionY ("Screen Height", Float) = 280
        _PixelSize ("Pixel Size (X,Y,Z)", Vector) = (0.1, 0.1, 0.1, 0)
        _TextureScale ("Texture Scale", Float) = 1.0
        _ColorVariation ("Color Variation", Range(0,0.3)) = 0.08

        [Header(Dither)]
        _DitherStrength ("Dither Strength", Range(0,1)) = 0.5

        [Header(Mapping)]
        [Toggle] _UseLocalSpace ("Use Local Space (No Pixel Swimming)", Float) = 1
        [Toggle] _DominantAxis ("Dominant Axis Mapping (No Lines)", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 100

        Pass
        {
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 localPos : TEXCOORD2;
                float3 worldNormal : TEXCOORD3;
                float snow : TEXCOORD4;
                UNITY_FOG_COORDS(5)
            };

            sampler2D _MainTex;
            sampler2D _SnowTex;

            float4 _Color;
            float _UseLighting;
            float _BothSidesLighting;

            float _AlphaClip;
            float _AlphaCutoff;
            float _DisableTriplanar;

            float _EnableSnow;
            float _SnowAmount;

            float _ResolutionX;
            float _ResolutionY;
            float3 _PixelSize;
            float _TextureScale;
            float _ColorVariation;
            float _DitherStrength;
            float _UseLocalSpace;
            float _DominantAxis;

            static const float4x4 ditherTable = float4x4(
                float4( 0,  8,  2, 10) / 16.0,
                float4(12,  4, 14,  6) / 16.0,
                float4( 3, 11,  1,  9) / 16.0,
                float4(15,  7, 13,  5) / 16.0
            );

            float hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float2 Quantize2(float2 v, float2 step)
            {
                return floor(v / step) * step;
            }

            float4 SampleMapping(
                sampler2D tex,
                float3 worldPos,
                float3 localPos,
                float3 normal
            )
            {
                float3 p = lerp(worldPos, localPos, _UseLocalSpace);
                float3 n = abs(normal);

                if (_DominantAxis > 0.5)
                {
                    float2 uv;
                    if (n.x > n.y && n.x > n.z)
                        uv = Quantize2(float2(p.z, p.y), _PixelSize.zy);
                    else if (n.y > n.z)
                        uv = Quantize2(float2(p.x, p.z), _PixelSize.xz);
                    else
                        uv = Quantize2(float2(p.x, p.y), _PixelSize.xy);

                    return tex2D(tex, uv * _TextureScale);
                }
                else
                {
                    float3 blend = max(n, 0.2);
                    blend /= (blend.x + blend.y + blend.z);

                    float4 c =
                        tex2D(tex, Quantize2(float2(p.z, p.y), _PixelSize.zy) * _TextureScale) * blend.x +
                        tex2D(tex, Quantize2(float2(p.x, p.z), _PixelSize.xz) * _TextureScale) * blend.y +
                        tex2D(tex, Quantize2(float2(p.x, p.y), _PixelSize.xy) * _TextureScale) * blend.z;

                    return c;
                }
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                float4 clip = o.pos;
                clip.xy /= clip.w;
                clip.xy = floor(clip.xy * float2(_ResolutionX, _ResolutionY))
                        / float2(_ResolutionX, _ResolutionY);
                clip.xy *= clip.w;
                o.pos = clip;

                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.localPos = v.vertex.xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.snow = saturate(dot(o.worldNormal, float3(0,1,0)) * _SnowAmount);

                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target
            {
                fixed4 col;

                if (_DisableTriplanar > 0.5)
                    col = tex2D(_MainTex, i.uv);
                else
                    col = SampleMapping(_MainTex, i.worldPos, i.localPos, i.worldNormal);

                if (_AlphaClip > 0.5)
                    clip(col.a - _AlphaCutoff);

                if (_EnableSnow > 0.5 && _DisableTriplanar < 0.5 && _SnowAmount > 0.001)
                {
                    fixed4 snowCol = SampleMapping(_SnowTex, i.worldPos, i.localPos, i.worldNormal);
                    col = lerp(col, snowCol, i.snow);
                }

                float3 p = lerp(i.worldPos, i.localPos, _UseLocalSpace);
                float2 pid = floor(float2(p.x / _PixelSize.x, p.z / _PixelSize.z));
                col.rgb *= 1.0 + (hash12(pid) - 0.5) * _ColorVariation;

                col *= _Color;

                if (_UseLighting > 0.5)
                {
                    float3 n = normalize(i.worldNormal);
                    if (_BothSidesLighting > 0.5)
                        n *= (facing > 0 ? 1.0 : -1.0);

                    float ndl = saturate(dot(n, normalize(_WorldSpaceLightPos0.xyz)));
                    col.rgb *= ndl * 0.8 + 0.2;
                }

                float2 d = floor(fmod(i.pos.xy, 4.0));
                col.rgb = floor(col.rgb * 31.0 + ditherTable[d.x][d.y] * _DitherStrength) / 31.0;

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}
