// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "TexturePaint/PaintBump"
{
    Properties
    {
        [HideInInspector]
        _MainTex("MainTex", 2D) = "white"
        [HideInInspector]
        _Blush("Blush", 2D) = "white"
        [HideInInspector]
        _BlushBump("BlushBump", 2D) = "white"
        [HideInInspector]
        _BlushScale("BlushScale", FLOAT) = 0.1
        [HideInInspector]
        _PaintUV("Hit UV Position", VECTOR) = (0,0,0,0)
        [HideInInspector]
        _BumpBlend("BumpBlend", FLOAT) = 1
    }

    SubShader
    {

        CGINCLUDE

            struct app_data {
                float4 vertex:POSITION;
                float4 uv:TEXCOORD0;
            };

            struct v2f {
                float4 screen:SV_POSITION;
                float4 uv:TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _Blush;
            sampler2D _BlushBump;
            float4 _PaintUV;
            float _BlushScale;
            float _BumpBlend;
        ENDCG

        Pass
        {
            CGPROGRAM
#pragma vertex vert
#pragma fragment frag

            v2f vert(app_data i) {
                v2f o;
                o.screen = UnityObjectToClipPos(i.vertex);
                o.uv = i.uv;
                return o;
            }


            float4 frag(v2f i) : SV_TARGET {
                float h = _BlushScale;

            float4 base = tex2D(_MainTex, i.uv);

                if (_PaintUV.x - h < i.uv.x && i.uv.x < _PaintUV.x + h &&
                        _PaintUV.y - h < i.uv.y && i.uv.y < _PaintUV.y + h) {
                            float4 brushCol = tex2D(_Blush, (_PaintUV.xy - i.uv) / h * 0.5 + 0.5);
                            if (brushCol.a - 1 >= 0) {
                                float4 bump = tex2D(_BlushBump, (_PaintUV.xy - i.uv) / h * 0.5 + 0.5);
                                return normalize(lerp(base, bump, _BumpBlend));
                            }
                }

                return base;
            }

            ENDCG
        }
    }
}