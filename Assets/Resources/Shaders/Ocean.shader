Shader "SR/Ocean"
{
    Properties
    {
        _DeepColor ("Deep", Color) = (0.004,0.035,0.07,1)
        _MidColor ("Mid", Color) = (0.01,0.13,0.19,1)
        _ShallowColor ("Shallow", Color) = (0.06,0.48,0.47,1)
        _SSSColor ("SSS", Color) = (0.06,0.6,0.5,1)
        _FoamColor ("Foam", Color) = (0.92,0.95,0.97,1)
        _NormalTex ("Detail Normals", 2D) = "bump" {}
        _NoiseTex ("Noise", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent-100" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            ZWrite On
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.0
            #include "UnityCG.cginc"

            float4 _Waves[6];
            float _WaveTime, _WaveAmp;
            float4 _SunDirG, _SunColorG, _SkyZenithG, _SkyHorizonG;
            float _SunVisG, _StormG, _EraShiftG, _DayLightG;
            float4 _ShipA, _ShipB, _ShipC, _ShipSpeeds;
            float4 _DeepColor, _MidColor, _ShallowColor, _SSSColor, _FoamColor;
            sampler2D _NormalTex, _NoiseTex;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 wpos : TEXCOORD0;
                float4 base : TEXCOORD1;
                float4 screen : TEXCOORD2;
                float eye : TEXCOORD3;
                UNITY_FOG_COORDS(4)
            };

            float WaveFade(int k, float dist)
            {
                return k < 3 ? saturate(1.0 - (dist - 1400.0) / 1600.0) : saturate(1.0 - (dist - 140.0) / 520.0);
            }

            float3 GerstnerDisp(float2 p, float dist)
            {
                float3 d = 0;
                [unroll] for (int k = 0; k < 6; k++)
                {
                    float4 w = _Waves[k];
                    float kk = 6.2831853 / w.w;
                    float c = sqrt(9.8 / kk);
                    float f = kk * (dot(w.xy, p) - c * _WaveTime);
                    float a = w.z / kk * WaveFade(k, dist);
                    float h = a * min(_WaveAmp, 1.0);
                    float v = a * _WaveAmp;
                    float cf = cos(f);
                    d.x += w.x * h * cf;
                    d.z += w.y * h * cf;
                    d.y += v * sin(f);
                }
                return d;
            }

            void GerstnerNormal(float2 p, float dist, out float3 n, out float jac)
            {
                float3 T = float3(1, 0, 0);
                float3 B = float3(0, 0, 1);
                [unroll] for (int k = 0; k < 6; k++)
                {
                    float4 w = _Waves[k];
                    float kk = 6.2831853 / w.w;
                    float c = sqrt(9.8 / kk);
                    float f = kk * (dot(w.xy, p) - c * _WaveTime);
                    float a = w.z / kk * WaveFade(k, dist);
                    float h = a * min(_WaveAmp, 1.0);
                    float v = a * _WaveAmp;
                    float sf = sin(f), cf = cos(f);
                    float wa = kk * h * sf;
                    float wv = kk * v * cf;
                    T.x -= w.x * w.x * wa; T.y += w.x * wv; T.z -= w.x * w.y * wa;
                    B.x -= w.x * w.y * wa; B.y += w.y * wv; B.z -= w.y * w.y * wa;
                }
                n = normalize(cross(B, T));
                jac = T.x * B.z - T.z * B.x;
            }

            float Wake(float2 p, float4 ship, float spd)
            {
                float2 rel = p - ship.xy;
                if (dot(rel, rel) > 48400.0) return 0;
                float2 fwd = ship.zw;
                float2 rgt = float2(fwd.y, -fwd.x);
                float lz = dot(rel, fwd);
                float lx = dot(rel, rgt);
                float e = length(float2(lx / 3.3, lz / 10.3));
                float hull = saturate(1.0 - abs(e - 1.0) * 4.0) * (0.2 + saturate(spd / 6.0));
                float t = -lz - 8.0;
                float wk = 0;
                if (t > 0)
                {
                    float fade = exp(-t / 60.0);
                    float core = saturate(1.0 - abs(lx) / (1.7 + t * 0.07)) * exp(-t / 30.0);
                    float arm = saturate(1.0 - abs(abs(lx) - (2.2 + t * 0.33)) / (0.9 + t * 0.025)) * fade;
                    wk = (core + arm * 0.75) * saturate(spd / 7.0);
                }
                return saturate(hull + wk);
            }

            float3 SkyCol(float3 d)
            {
                float h = saturate(d.y);
                float3 c = lerp(_SkyHorizonG.rgb, _SkyZenithG.rgb, pow(h, 0.5));
                float sd = saturate(dot(d, _SunDirG.xyz));
                c += _SunColorG.rgb * (pow(sd, 10.0) * 0.35 + pow(sd, 80.0) * 0.8) * _SunVisG;
                return c;
            }

            v2f vert(appdata v)
            {
                v2f o;
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                float dist = distance(wp.xz, _WorldSpaceCameraPos.xz);
                float3 d = GerstnerDisp(wp.xz, dist);
                float3 p = wp + d;
                o.wpos = p;
                o.base = float4(wp.xz, dist, d.y);
                o.pos = mul(UNITY_MATRIX_VP, float4(p, 1.0));
                o.screen = ComputeScreenPos(o.pos);
                o.eye = -mul(UNITY_MATRIX_V, float4(p, 1.0)).z;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 p = i.base.xy;
                float dist = i.base.z;
                float height = i.base.w;
                float3 N; float jac;
                GerstnerNormal(p, dist, N, jac);

                float2 uv1 = i.wpos.xz / 13.0 + _WaveTime * float2(0.021, 0.013);
                float2 uv2 = i.wpos.xz / 37.0 - _WaveTime * float2(0.010, 0.019);
                float3 n1 = tex2D(_NormalTex, uv1).xyz * 2.0 - 1.0;
                float3 n2 = tex2D(_NormalTex, uv2).xyz * 2.0 - 1.0;
                float ds = lerp(0.55, 0.1, saturate(dist / 450.0)) * (0.8 + _StormG * 0.7);
                N = normalize(N + float3(n1.x + n2.x, 0, n1.y + n2.y) * ds);

                float3 V = normalize(_WorldSpaceCameraPos - i.wpos);
                float ndv = saturate(dot(N, V));
                float fres = 0.02 + 0.98 * pow(1.0 - ndv, 5.0);
                float3 R = reflect(-V, N);
                R.y = abs(R.y);
                float3 refl = SkyCol(R);
                float3 L = _SunDirG.xyz;
                float day = _DayLightG;

                float3 body = lerp(_DeepColor.rgb, _MidColor.rgb, saturate(height * 0.35 + 0.45)) * day;
                float3 H2 = normalize(L + N * 0.5);
                float sss = pow(saturate(dot(V, -H2)), 4.0) * saturate(height * 0.45 + 0.35) * 1.6;
                sss += saturate(height * 0.3) * 0.15;
                float3 col = body + _SSSColor.rgb * sss * _SunColorG.rgb * 0.5 * _SunVisG;
                col = lerp(col, refl, fres);

                float3 Hh = normalize(L + V);
                float ndh = saturate(dot(N, Hh));
                float spec = pow(ndh, 1100.0) * 16.0 + pow(ndh, 130.0) * 0.45;
                col += _SunColorG.rgb * spec * _SunVisG * saturate(L.y * 4.0 + 0.15);

                float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screen)));
                float wd = max(0.0, sceneZ - i.eye);
                float shallow = exp(-wd * 0.2);
                col = lerp(col, _ShallowColor.rgb * day * (0.65 + 0.35 * ndv), shallow * 0.75 * (1.0 - fres));
                float alpha = saturate(wd * 0.6 + 0.08);

                float fn = tex2D(_NoiseTex, i.wpos.xz / 9.0 + _WaveTime * 0.01).g;
                float fn2 = tex2D(_NoiseTex, i.wpos.xz / 23.0 - _WaveTime * 0.008).b;
                float foamNoise = saturate(fn * 1.2 + fn2 * 0.5 - 0.25);
                float crest = saturate((0.9 - jac) * (2.0 + _StormG * 1.1)) * saturate(height * 0.5 + 0.25);
                float shore = saturate(1.0 - wd / (1.3 + 0.6 * sin(_WaveTime * 0.8 + i.wpos.x * 0.05 + i.wpos.z * 0.03)));
                float wk = Wake(i.wpos.xz, _ShipA, _ShipSpeeds.x) + Wake(i.wpos.xz, _ShipB, _ShipSpeeds.y) + Wake(i.wpos.xz, _ShipC, _ShipSpeeds.z);
                float foam = saturate(crest * 1.1 + shore * 0.55 + wk * 0.95);
                foam *= smoothstep(0.38, 0.75, foamNoise + foam * 0.3);
                float3 foamCol = _FoamColor.rgb * (0.3 * day + 0.42 * _SunColorG.rgb * saturate(dot(N, L) * 0.5 + 0.5) * _SunVisG + 0.12);
                col = lerp(col, foamCol, saturate(foam));
                alpha = saturate(alpha + foam);
                alpha = max(alpha, saturate((dist - 300.0) / 200.0));

                UNITY_APPLY_FOG(i.fogCoord, col);
                return float4(col, alpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
