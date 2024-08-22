Shader "PlantsVsZombies/PoolShadingShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _AlphaTex ("AlphaTexture", 2D) = "white" {}
        _EffectClip ("EffectClip", vector) = (0.0333333, 0.1, 0.9666667, 0.9)
    }
    SubShader
    {
        Tags 
        {
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent"
            "DisableBatching" = "True"
        }
        Cull Off 
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off 
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            float4 _EffectClip;

            static const float pi = 3.14159265259;
            static const float pi2 = 6.28318530718;

            v2f vert (appdata v)
            {
                v2f o;
                float phase = pi2 * _Time.y * 100.0;

                float wavetime1 = phase / 800.0;
                float wavetime2 = phase / 150.0;
                float wavetime3 = phase / 900.0;
                float wavetime4 = phase / 800.0;
                float wavetime5 = phase / 110.0;

                float2 phase2 = v.vertex.xy * (3.0 * pi2);

                float2 uvoffset;
                uvoffset.x = sin(phase2.y * 0.2 + wavetime2) * 0.015 + sin(phase2.y * 0.2 + wavetime1) * 0.012;
                uvoffset.y = sin(phase2.x * 0.2 + wavetime5) * 0.005 + sin(phase2.x * 0.2 + wavetime3) * 0.015 + sin(phase2.x * 0.2 + wavetime4) * 0.02;
                uvoffset *= step(_EffectClip.x, v.vertex.x);
                uvoffset *= step(_EffectClip.y, -v.vertex.y);
                uvoffset *= step(v.vertex.x, _EffectClip.z);
                uvoffset *= step(-v.vertex.y, _EffectClip.w);
                uvoffset.y = -uvoffset.y;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uv += uvoffset;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _AlphaTex;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                col.a *= tex2D(_AlphaTex, i.uv).r;
                return col;
            }
            ENDCG
        }
    }
}

