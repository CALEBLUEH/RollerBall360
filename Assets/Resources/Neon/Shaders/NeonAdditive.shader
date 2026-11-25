Shader "MGD2/Neon Additive"
{
    Properties
    {
        [HDR] _Color ("Glow Color", Color) = (4, 4, 4, 1)
        _MainTex ("Particle Texture", 2D) = "white" {}
        _Intensity ("Intensity", Range(0, 8)) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "NeonAdditive"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Intensity;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 textureSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half alpha = textureSample.a * input.color.a * _Color.a;
                return half4(textureSample.rgb * input.color.rgb * _Color.rgb * _Intensity, alpha);
            }
            ENDHLSL
        }
    }
}
