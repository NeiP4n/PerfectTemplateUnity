Shader "Custom/PixelatedTexture"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _PixelResolution ("Pixel Resolution", Float) = 64
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline"
        }
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct appdata
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float _PixelResolution;
            CBUFFER_END
            
            v2f vert(appdata v)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                // Пикселизация UV координат
                float2 pixelatedUV = floor(i.uv * _PixelResolution) / _PixelResolution;
                
                // Сэмплирование текстуры с пикселизированными UV
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, pixelatedUV);
                
                return texColor * _BaseColor;
            }
            ENDHLSL
        }
    }
}
