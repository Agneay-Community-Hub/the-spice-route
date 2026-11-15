Shader "SR/Sky"
{
    Properties
    {
        _NoiseTex ("Noise", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _NoiseTex;
            float4 _SunDirG, _SunColorG, _SkyZenithG, _SkyHorizonG, _CloudLitG, _CloudShadeG, _FogColorG;
            float _SunVisG, _CloudCoverG, _NightG, _FlashG, _WaveTime, _EraShiftG, _StormG;

            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert(float4 v : POSITION)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v);
                o.dir = v.xyz;
                return o;
            }

            float hash3(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y;
                float3 c = lerp(_SkyHorizonG.rgb, _SkyZenithG.rgb, pow(saturate(h), 0.45));
                float sd = dot(d, _SunDirG.xyz);
                float sdp = saturate(sd);
                c += _SunColorG.rgb * (pow(sdp, 5.0) * 0.22 + pow(sdp, 48.0) * 0.55) * _SunVisG;

                if (_NightG > 0.01)
                {
                    float3 sp = floor(d * 280.0);
                    float s = hash3(sp);
                    float star = step(0.9972, s) * saturate(h * 3.0);
                    c += star * _NightG * 1.6 * (0.6 + 0.4 * sin(_Time.y * 3.0 + s * 100.0));
                }

                float cov = 0;
                float3 cloudCol = 0;
                if (h > 0.0)
                {
                    float2 uv = d.xz / (h + 0.18);
                    float t = _WaveTime;
                    float n = tex2D(_NoiseTex, uv * 0.045 + float2(t * 0.0016, t * 0.0009)).r * 0.62
                            + tex2D(_NoiseTex, uv * 0.11 + float2(t * 0.0032, -t * 0.0011)).g * 0.28
                            + tex2D(_NoiseTex, uv * 0.31 + float2(t * 0.006, 0)).b * 0.10;
                    float thr = 1.0 - _CloudCoverG;
                    cov = smoothstep(thr - 0.06, thr + 0.22, n) * smoothstep(0.0, 0.12, h);
                    float thick = smoothstep(thr, thr + 0.45, n);
                    float silver = pow(sdp, 6.0) * (1.0 - thick) * 2.2;
                    cloudCol = lerp(_CloudLitG.rgb, _CloudShadeG.rgb, thick) + _SunColorG.rgb * silver * _SunVisG;
                }
                c = lerp(c, cloudCol, cov * 0.93);

                float disc = smoothstep(0.99935, 0.9997, sd) * _SunVisG * (1.0 - cov * 0.9);
                c += _SunColorG.rgb * disc * 22.0;

                c = lerp(c, _FogColorG.rgb, saturate(-h * 6.0));
                c = lerp(c, _FogColorG.rgb, exp(-max(h, 0.0) * 12.0) * 0.4);
                c += _FlashG * float3(0.55, 0.6, 0.85) * (0.3 + cov);
                return float4(c, 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
