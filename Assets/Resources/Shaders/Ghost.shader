Shader "SR/Ghost"
{
    Properties
    {
        _Color ("Color", Color) = (1,0.7,0.3,1)
        _Intensity ("Intensity", Float) = 1.4
        _Base ("Base Visibility", Range(0,1)) = 0
        _Vis ("Visibility", Range(0,1)) = 1
        _NoiseTex ("Noise", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.0
            #include "UnityCG.cginc"

            float4 _Color;
            float _Intensity, _Base, _Vis, _EraShiftG;
            sampler2D _NoiseTex;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 wp : TEXCOORD0;
                float3 wn : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                wp.y += sin(_Time.y * 1.3 + wp.x * 0.1) * 0.05;
                o.wp = wp;
                o.pos = mul(UNITY_MATRIX_VP, float4(wp, 1.0));
                o.wn = UnityObjectToWorldNormal(v.normal);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float vis = saturate(max(_EraShiftG, _Base)) * _Vis;
                float n = tex2D(_NoiseTex, i.wp.xz * 0.04 + i.wp.y * 0.05 + _Time.y * 0.03).r;
                float reveal = smoothstep(n - 0.15, n + 0.15, vis * 1.3 - 0.15);
                float3 V = normalize(_WorldSpaceCameraPos - i.wp);
                float fres = pow(1.0 - saturate(abs(dot(normalize(i.wn), V))), 2.2);
                float scan = 0.8 + 0.2 * sin(i.wp.y * 7.0 - _Time.y * 4.0);
                float3 col = _Color.rgb * _Intensity * (fres * 1.5 + 0.12) * scan * reveal;
                UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0, 0, 0, 0));
                return float4(col, 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
