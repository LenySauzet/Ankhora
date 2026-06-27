Shader "Ankhora/GhostHands_URP"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.6, 0.85, 1.0, 1.0)
        _Alpha ("Alpha", Range(0,1)) = 0.3
        _RimColor ("Rim Color", Color) = (0.7, 0.9, 1.0, 1.0)
        _RimPower ("Rim Power", Range(0.5,8)) = 3.0
        _EmissionStrength ("Emission Strength", Range(0,2)) = 0.4
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "GhostUnlit"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Alpha;
                float4 _RimColor;
                float _RimPower;
                float _EmissionStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);
                OUT.positionHCS = posInputs.positionCS;
                OUT.normalWS = normInputs.normalWS;
                OUT.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirWS = normalize(IN.viewDirWS);
                half fresnel = pow(saturate(1.0 - saturate(dot(normalWS, viewDirWS))), _RimPower);

                half3 rim = _RimColor.rgb * fresnel;
                half3 color = _BaseColor.rgb + rim;
                half3 emission = color * _EmissionStrength;
                half3 finalColor = color + emission;
                half alpha = saturate(_Alpha + fresnel * 0.3);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}