Shader "Custom/URP2D/SpriteOutline"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _OutlineEnabled ("Outline Enabled", Range(0,1)) = 1
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlinePixels ("Outline Pixels", Range(0,4)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTex_ST;
            float4 _Color;

            float _OutlineEnabled;
            float4 _OutlineColor;
            float _OutlinePixels;

            // Unity 自动提供：x=1/width, y=1/height, z=width, w=height
            float4 _MainTex_TexelSize;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;
                return OUT;
            }

            float SampleA(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float baseA = tex.a;

                // 本体颜色
                float3 baseRGB = tex.rgb * IN.color.rgb;
                float outA = baseA;

                // 关闭描边
                if (_OutlineEnabled < 0.5)
                {
                    return float4(baseRGB, outA) * IN.color.a;
                }

                // 以“像素”为单位的采样步长（对图集也稳定）
                float2 stepUV = _MainTex_TexelSize.xy * max(_OutlinePixels, 0.0);

                // 8方向采样，更圆润
                float aL  = SampleA(IN.uv + float2(-stepUV.x, 0));
                float aR  = SampleA(IN.uv + float2( stepUV.x, 0));
                float aU  = SampleA(IN.uv + float2(0,  stepUV.y));
                float aD  = SampleA(IN.uv + float2(0, -stepUV.y));
                float aLU = SampleA(IN.uv + float2(-stepUV.x,  stepUV.y));
                float aRU = SampleA(IN.uv + float2( stepUV.x,  stepUV.y));
                float aLD = SampleA(IN.uv + float2(-stepUV.x, -stepUV.y));
                float aRD = SampleA(IN.uv + float2( stepUV.x, -stepUV.y));

                float neighborA = max(max(max(aL,aR), max(aU,aD)),
                                      max(max(aLU,aRU), max(aLD,aRD)));

                // 描边只出现在“本体透明但邻居不透明”的地方
                float outline = saturate(neighborA - baseA);

                // 合成：描边覆盖到透明区域
                float3 rgb = lerp(baseRGB, _OutlineColor.rgb, outline);

                // alpha：本体alpha 或 描边alpha（二者取大）
                outA = max(outA, outline * _OutlineColor.a);

                return float4(rgb, outA) * IN.color.a;
            }

            ENDHLSLPROGRAM
        }
    }
}
