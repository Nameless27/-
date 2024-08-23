Shader "PlantsVsZombies/PoolCausticEffectShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _EffectClip ("EffectClip", vector) = (0.0333333, 0.1, 0.9666667, 0.9)
        _Step ("Step", Range(0, 1)) = 0.0
        _IsNight ("IsNight", Range(0, 1)) = 0.0
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
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };
            
            float4 _EffectClip;
            float _Step;
            float _IsNight;

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
                uvoffset.x = sin(phase2.y + wavetime1 * 1.5) * 0.004 + sin(phase2.y + wavetime2 * 1.5) * 0.005;
                uvoffset.y = sin(phase2.x * 4.0 + wavetime5 * 2.5) * 0.005 + sin(phase2.x * 2.0 + wavetime3 * 2.5) * 0.04 + sin(phase2.x * 3.0 + wavetime4 * 2.5) * 0.02;
                
                float effectClipFactor = 1.0;
                effectClipFactor *= step(_EffectClip.x, v.vertex.x);
                effectClipFactor *= step(_EffectClip.y, -v.vertex.y);
                effectClipFactor *= step(v.vertex.x, _EffectClip.z);
                uvoffset *= effectClipFactor;
                uvoffset *= step(v.vertex.y, _EffectClip.w);
                uvoffset += v.vertex.xy;
                //uvoffset.y = -uvoffset.y;

                float a = 1.0;
                a = lerp(192.0 / 255.0, 128.0 / 255.0, step(_Step, v.vertex.x));
                a = lerp(a, 48.0 / 255.0, step(0.5, _IsNight));
                a = lerp(32.0 / 255.0, a, effectClipFactor);
                o.color = v.color;
                o.color.a *= a;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uv += uvoffset;
                o.uv.y = -o.uv.y;

                return o;
            }

            sampler2D _MainTex;

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv0 = i.uv;
                float2 uv1 = uv0;
                float time_mod655_36 = fmod(_Time.y, 655.36);
                uv0.x = (2.0 * uv0.x) - (fmod(time_mod655_36 + 0.01, 655.36) / 6.0);
                uv0.y = (2.0 - (2.0 * uv0.y)) + (time_mod655_36 / 8.0);
                uv1.x = (2.0 * uv1.x) + (time_mod655_36 / 10.0);
                uv1.y = (2.0 * uv1.y);

                uv0 /= float2(2.0, 4.0);
                uv1 /= float2(2.0, 4.0);

                float a0 = tex2D(_MainTex, uv0).r;
                float a1 = tex2D(_MainTex, uv1).r;
                float a = (a0 + a1) / 2.0;
                
                float alpha = lerp(0.0, 5.0 * (a - (128.0 / 255.0)), step(128.0 / 255.0, a));
                alpha = lerp(alpha, 1.0 - (2.0 * (a - (160.0 / 255.0))), step(160.0 / 255.0, a));

                fixed4 color = i.color;
                color.a *= alpha / 3.0;

                return color;
            }
            ENDCG
        }
    }
}
