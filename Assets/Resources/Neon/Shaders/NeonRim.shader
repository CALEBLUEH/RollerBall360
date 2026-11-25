Shader "MGD2/Neon Rim"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.006, 0.01, 0.025, 1)
        [HDR] _NeonColor ("Neon Color", Color) = (0, 4, 8, 1)
        _EmissionStrength ("Emission Strength", Range(0, 12)) = 5
        _RimPower ("Rim Power", Range(0.25, 8)) = 2.2
        _RimThreshold ("Rim Threshold", Range(0, 1)) = 0.22
        _FullEmission ("Surface Emission", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "NeonForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _NeonColor;
                half _EmissionStrength;
                half _RimPower;
                half _RimThreshold;
                half _FullEmission;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirection = SafeNormalize(GetCameraPositionWS() - input.positionWS);
                half fresnel = pow(1.0h - saturate(dot(normalWS, viewDirection)), _RimPower);
                half rim = smoothstep(_RimThreshold, 1.0h, fresnel);
                half glowMask = saturate(rim + _FullEmission);
                half3 color = _BaseColor.rgb + _NeonColor.rgb * (_EmissionStrength * glowMask);
                color = MixFog(color, input.fogFactor);
                return half4(color, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
