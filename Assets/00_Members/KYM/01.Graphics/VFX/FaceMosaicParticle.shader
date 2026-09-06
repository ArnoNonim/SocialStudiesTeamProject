Shader "KYM/Face Mosaic Particle"
{
    Properties
    {
        _BlockSize ("Block Size", Range(2, 64)) = 12
        _ColorSteps ("Color Steps", Range(2, 32)) = 10
        _Opacity ("Opacity", Range(0, 1)) = 1
        _UseSceneColor ("Use Scene Color", Range(0, 1)) = 1
        _FallbackTint ("Fallback Tint", Color) = (0.42, 0.44, 0.46, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+100"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "FaceMosaic"
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _BlockSize;
                float _ColorSteps;
                float _Opacity;
                float _UseSceneColor;
                half4 _FallbackTint;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 screenPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screenPosition = ComputeScreenPos(output.positionCS);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 screenUv = input.screenPosition.xy / max(input.screenPosition.w, 0.00001);
                float2 screenPixels = screenUv * _ScreenParams.xy;
                float safeBlockSize = max(_BlockSize, 1.0);
                float2 mosaicUv = (floor(screenPixels / safeBlockSize) + 0.5) * safeBlockSize / _ScreenParams.xy;

                half3 sceneColor = SampleSceneColor(saturate(mosaicUv));
                float safeColorSteps = max(_ColorSteps, 2.0);
                sceneColor = floor(sceneColor * safeColorSteps + 0.5) / safeColorSteps;

                float2 localCell = floor(input.uv * float2(12.0, 16.0)) / float2(12.0, 16.0);
                float noise = frac(sin(dot(localCell, float2(12.9898, 78.233))) * 43758.5453);
                half3 fallbackColor = _FallbackTint.rgb * lerp(0.58h, 1.35h, (half)noise);
                half3 finalColor = lerp(fallbackColor, sceneColor, saturate(_UseSceneColor));

                // Pixelated oval mask shaped to cover a face without hard rectangular corners.
                float2 maskUv = (floor(input.uv * float2(12.0, 16.0)) + 0.5) / float2(12.0, 16.0);
                float2 centered = (maskUv - 0.5) * 2.0;
                float ellipse = dot(centered, centered);
                half mask = ellipse <= 1.0 ? 1.0h : 0.0h;
                half alpha = mask * saturate(_Opacity) * input.color.a;

                return half4(finalColor * input.color.rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
