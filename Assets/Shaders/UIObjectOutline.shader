Shader "Custom/UIObjectOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _Color ("UI Color", Color) = (1, 1, 1, 1)

        _OutlineColor ("Outline Color", Color) = (0, 1, 0, 1)

        _OutlineWidth ("Outline Width", Range(1, 8)) = 3
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;

            float4 _MainTex_TexelSize;

            fixed4 _Color;
            fixed4 _OutlineColor;

            float _OutlineWidth;

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 offset =
                    _MainTex_TexelSize.xy * _OutlineWidth;

                float centerAlpha = tex2D(
                    _MainTex,
                    i.uv
                ).a;

                float surroundingAlpha = 0;

                surroundingAlpha = max(
                    surroundingAlpha,
                    tex2D(_MainTex, i.uv + float2(offset.x, 0)).a
                );

                surroundingAlpha = max(
                    surroundingAlpha,
                    tex2D(_MainTex, i.uv - float2(offset.x, 0)).a
                );

                surroundingAlpha = max(
                    surroundingAlpha,
                    tex2D(_MainTex, i.uv + float2(0, offset.y)).a
                );

                surroundingAlpha = max(
                    surroundingAlpha,
                    tex2D(_MainTex, i.uv - float2(0, offset.y)).a
                );

                surroundingAlpha = max(
                    surroundingAlpha,
                    tex2D(_MainTex, i.uv + offset).a
                );

                surroundingAlpha = max(
                    surroundingAlpha,
                    tex2D(_MainTex, i.uv - offset).a
                );

                surroundingAlpha = max(
                    surroundingAlpha,
                    tex2D(
                        _MainTex,
                        i.uv + float2(offset.x, -offset.y)
                    ).a
                );

                surroundingAlpha = max(
                    surroundingAlpha,
                    tex2D(
                        _MainTex,
                        i.uv + float2(-offset.x, offset.y)
                    ).a
                );

                float outline = saturate(
                    surroundingAlpha - centerAlpha
                );

                return fixed4(
                    _OutlineColor.rgb,
                    outline * _OutlineColor.a * i.color.a
                );
            }

            ENDCG
        }
    }
}