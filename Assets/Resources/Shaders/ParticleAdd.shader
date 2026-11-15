Shader "SR/ParticleAdd"
{
    Properties
    {
        _MainTex ("Tex", 2D) = "white" {}
        _Intensity ("Intensity", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Intensity;
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_FOG_COORDS(1) };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                float4 t = tex2D(_MainTex, i.uv);
                float3 c = t.rgb * t.a * i.color.rgb * i.color.a * _Intensity;
                UNITY_APPLY_FOG_COLOR(i.fogCoord, c, fixed4(0, 0, 0, 0));
                return float4(c, 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
