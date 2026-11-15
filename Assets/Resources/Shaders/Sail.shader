Shader "SR/Sail"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Tex", 2D) = "white" {}
        _Billow ("Billow", Float) = 1
        _Flutter ("Flutter", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        float4 _Color;
        float _Billow, _Flutter;
        float4 _SunColorG, _SunDirG;
        float _SunVisG;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float2 uv = v.texcoord.xy;
            float b = sin(uv.x * 3.14159) * sin(uv.y * 2.8 + 0.25);
            float t = _Time.y;
            float fl = (sin(t * 11.0 + uv.x * 9.0 + uv.y * 5.0) * 0.6 + sin(t * 17.0 - uv.y * 13.0) * 0.4) * _Flutter * 0.12 * sin(uv.x * 3.14159);
            v.vertex.z += b * _Billow + fl;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 tex = tex2D(_MainTex, IN.uv_MainTex).rgb * _Color.rgb;
            o.Albedo = tex;
            o.Smoothness = 0.06;
            o.Metallic = 0;
            float trans = pow(saturate(dot(-normalize(IN.viewDir), _SunDirG.xyz)), 2.0);
            o.Emission = tex * _SunColorG.rgb * trans * _SunVisG * 0.35;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack Off
}
