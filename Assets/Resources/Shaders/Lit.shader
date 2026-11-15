Shader "SR/Lit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _DetailTex ("Detail", 2D) = "gray" {}
        _DetailScale ("Detail Scale", Float) = 0.3
        _DetailStrength ("Detail Strength", Range(0,1)) = 0.35
        _Gloss ("Smoothness", Range(0,1)) = 0.12
        _Metal ("Metallic", Range(0,1)) = 0
        _Sway ("Sway", Float) = 0
        _Emission ("Emission", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0

        sampler2D _MainTex, _DetailTex;
        float4 _Color, _Emission;
        float _DetailScale, _DetailStrength, _Gloss, _Metal, _Sway;
        float _WetnessG, _StormG;
        float4 _WindDirG;

        struct Input
        {
            float2 uv_MainTex;
            float4 color : COLOR;
            float3 worldPos;
            float3 wn;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float sw = v.texcoord1.x * _Sway;
            if (sw > 0.0001)
            {
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                float t = _Time.y;
                float g = 0.6 + _StormG * 1.6;
                float3 off = float3(sin(t * 1.4 + wp.x * 0.23 + wp.z * 0.11), sin(t * 2.1 + wp.z * 0.3) * 0.3, cos(t * 1.2 + wp.z * 0.21 + wp.x * 0.07)) * 0.12 * g;
                off += float3(_WindDirG.x, 0, _WindDirG.y) * (0.1 + _StormG * 0.45) * (0.7 + 0.3 * sin(t * 0.9 + wp.x * 0.05));
                v.vertex.xyz += mul((float3x3)unity_WorldToObject, off * sw);
            }
            o.wn = UnityObjectToWorldNormal(v.normal);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 wn = normalize(IN.wn);
            float3 n = abs(wn);
            n /= (n.x + n.y + n.z + 1e-4);
            float3 p = IN.worldPos * _DetailScale;
            float d = tex2D(_DetailTex, p.zy).r * n.x + tex2D(_DetailTex, p.xz).r * n.y + tex2D(_DetailTex, p.xy).r * n.z;
            float det = lerp(1.0, d * 2.0, _DetailStrength);
            float3 alb = IN.color.rgb * _Color.rgb * tex2D(_MainTex, IN.uv_MainTex).rgb * det;
            float wet = _WetnessG * saturate(wn.y * 0.7 + 0.5);
            alb *= lerp(1.0, 0.55, wet);
            o.Albedo = alb;
            o.Smoothness = lerp(_Gloss, 0.75, wet * 0.85);
            o.Metallic = _Metal;
            o.Emission = _Emission.rgb;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack Off
}
