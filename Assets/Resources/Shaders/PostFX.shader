Shader "Hidden/SR/PostFX"
{
    Properties
    {
        _MainTex ("Tex", 2D) = "white" {}
        _BloomTex ("Bloom", 2D) = "black" {}
    }
    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MainTex, _BloomTex;
    float4 _MainTex_TexelSize;
    float _Threshold, _Knee, _BloomIntensity, _Exposure, _Saturation, _Contrast, _Vignette;
    float _EraAmt, _EraWave, _EraTime, _Damage, _Flash, _Fade, _Grain;
    float4 _Tint;

    struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

    v2f vert(appdata_img v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.texcoord;
        return o;
    }

    float3 Box4(float2 uv, float d)
    {
        float4 o = _MainTex_TexelSize.xyxy * float4(-d, -d, d, d);
        float3 s = tex2D(_MainTex, uv + o.xy).rgb + tex2D(_MainTex, uv + o.zy).rgb
                 + tex2D(_MainTex, uv + o.xw).rgb + tex2D(_MainTex, uv + o.zw).rgb;
        return s * 0.25;
    }

    float3 Prefilter(float3 c)
    {
        float br = max(c.r, max(c.g, c.b));
        float soft = clamp(br - _Threshold + _Knee, 0.0, 2.0 * _Knee);
        soft = soft * soft / (4.0 * _Knee + 1e-5);
        float contrib = max(soft, br - _Threshold) / max(br, 1e-5);
        return c * contrib;
    }

    float3 ACES(float3 x)
    {
        const float a = 2.51, b = 0.03, c = 2.43, d = 0.59, e = 0.14;
        return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 frag(v2f i) : SV_Target
            {
                float3 c = min(Box4(i.uv, 1.0), 40.0);
                return float4(Prefilter(c), 1);
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 frag(v2f i) : SV_Target
            {
                return float4(Box4(i.uv, 1.0), 1);
            }
            ENDCG
        }

        Pass
        {
            Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 frag(v2f i) : SV_Target
            {
                return float4(Box4(i.uv, 0.5), 1);
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 cd = uv - 0.5;
                float r = length(cd);
                float2 dir = cd / max(r, 1e-4);
                float ripple = sin(r * 42.0 - _EraTime * 9.0) * _EraWave * 0.007 * smoothstep(0.0, 0.3, r);
                uv += dir * ripple;
                float ca = (0.0012 + _EraAmt * 0.004 + _Damage * 0.003) * r;
                float3 col;
                col.r = tex2D(_MainTex, uv + cd * ca).r;
                col.g = tex2D(_MainTex, uv).g;
                col.b = tex2D(_MainTex, uv - cd * ca).b;
                float3 bloom = tex2D(_BloomTex, uv).rgb;
                col += bloom * _BloomIntensity;
                col *= _Exposure;
                col = ACES(col);

                float l = dot(col, float3(0.2126, 0.7152, 0.0722));
                col = lerp(l.xxx, col, _Saturation);
                col = pow(max(col, 0.0) / 0.18, _Contrast) * 0.18;
                col *= _Tint.rgb;

                float3 eraCol = lerp(float3(0.02, 0.07, 0.10), float3(1.0, 0.8, 0.45), smoothstep(0.0, 0.65, l));
                eraCol += saturate(bloom) * float3(0.35, 0.28, 0.12);
                col = lerp(col, eraCol * 1.15, _EraAmt * 0.65);

                col *= saturate(1.0 - _Vignette * r * r * 1.9);
                col += float3(1.0, 0.7, 0.3) * _EraAmt * pow(saturate(r * 1.35), 3.0) * 0.35;
                col = lerp(col, float3(0.45, 0.02, 0.0), _Damage * saturate(r * 1.7 - 0.35));
                col += _Flash * 0.45;

                float g = frac(sin(dot(i.uv * _ScreenParams.xy + _EraTime * 61.0, float2(12.9898, 78.233))) * 43758.5453);
                col += (g - 0.5) * _Grain;
                col *= (1.0 - _Fade);
                return float4(max(col, 0.0), 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
