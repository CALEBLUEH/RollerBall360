Shader "MGD2/Neon Roller Ball"
{
    Properties
    {
        _BaseMap ("Metal Plate Color", 2D) = "white" {}
        _NormalMap ("Metal Plate Normal", 2D) = "bump" {}
        _MetalnessMap ("Metalness", 2D) = "white" {}
        _RoughnessMap ("Roughness", 2D) = "gray" {}
        [HDR] _NeonColor ("White Neon", Color) = (4, 4, 4, 1)
        _EmissionStrength ("Emission Strength", Range(0, 10)) = 3.5
        _Tiling ("Texture Tiling", Range(0.25, 8)) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "BallForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_MetalnessMap); SAMPLER(sampler_MetalnessMap);
            TEXTURE2D(_RoughnessMap); SAMPLER(sampler_RoughnessMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half3 tangentWS : TEXCOORD2;
                half3 bitangentWS : TEXCOORD3;
                float2 uv : TEXCOORD4;
                half fogFactor : TEXCOORD5;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _NeonColor;
                half _EmissionStrength;
                half _Tiling;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = normalInputs.tangentWS;
                output.bitangentWS = normalInputs.bitangentWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap) * _Tiling;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                half3 normalTS = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv).rgb * 2.0h - 1.0h;
                half3x3 tangentToWorld = half3x3(
                    normalize(input.tangentWS),
                    normalize(input.bitangentWS),
                    normalize(input.normalWS));
                half3 normalWS = normalize(mul(normalTS, tangentToWorld));
                half3 viewDirection = SafeNormalize(GetCameraPositionWS() - input.positionWS);
                Light mainLight = GetMainLight();
                half diffuse = saturate(dot(normalWS, mainLight.direction));
                half3 halfDirection = SafeNormalize(mainLight.direction + viewDirection);
                half metalness = SAMPLE_TEXTURE2D(_MetalnessMap, sampler_MetalnessMap, input.uv).r;
                half roughness = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, input.uv).r;
                half specularPower = lerp(80.0h, 8.0h, roughness);
                half specular = pow(saturate(dot(normalWS, halfDirection)), specularPower) * lerp(0.35h, 1.0h, metalness);
                half rim = pow(1.0h - saturate(dot(normalWS, viewDirection)), 2.2h);
                half plateLines = saturate((0.62h - dot(albedo, half3(0.299h, 0.587h, 0.114h))) * 2.5h);
                half3 surface = albedo * (0.12h + diffuse * 0.58h) + specular;
                half3 glow = _NeonColor.rgb * _EmissionStrength * saturate(rim + plateLines * 0.28h);
                half3 color = MixFog(surface + glow, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }
}
