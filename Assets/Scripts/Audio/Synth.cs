using UnityEngine;

/// <summary>
/// Offline synthesiser. Every sound and all music is rendered from code at load time.
/// Music is inspired by Kerala traditions: tanpura drone, bansuri-like flute with gamaka,
/// mizhavu/chenda percussion and ilathalam cymbals.
/// </summary>
public static class Synth
{
    public const int SR = 32000;
    static System.Random rng = new System.Random(7);
    static float[] sine;
    static bool wrap;

    static void Init()
    {
        if (sine != null) return;
        sine = new float[4096];
        for (int i = 0; i < 4096; i++) sine[i] = Mathf.Sin(i / 4096f * Mathf.PI * 2f);
    }

    static float S(float phase) { return sine[(int)(phase * 4096f) & 4095]; }
    static float Rnd() => (float)rng.NextDouble() * 2f - 1f;
    static float Coef(float fc) => 1f - Mathf.Exp(-2f * Mathf.PI * fc / SR);
    static float[] Buf(float sec) => new float[Mathf.CeilToInt(sec * SR)];

    static void Add(float[] b, int i, float v)
    {
        if (i < 0) return;
        if (i >= b.Length) { if (!wrap) return; i %= b.Length; }
        b[i] += v;
    }

    public static AudioClip Clip(string name, float[] data)
    {
        var c = AudioClip.Create(name, data.Length, 1, SR, false);
        c.SetData(data, 0);
        return c;
    }

    static void Normalize(float[] b, float peak)
    {
        float m = 1e-6f;
        for (int i = 0; i < b.Length; i++) m = Mathf.Max(m, Mathf.Abs(b[i]));
        float k = peak / m;
        for (int i = 0; i < b.Length; i++) b[i] *= k;
    }

    static void SoftLimit(float[] b, float drive)
    {
        for (int i = 0; i < b.Length; i++) b[i] = (float)System.Math.Tanh(b[i] * drive) / drive;
    }

    static float[] LoopFade(float[] b, int fade)
    {
        int n = b.Length - fade;
        var o = new float[n];
        for (int i = 0; i < n; i++) o[i] = b[i];
        for (int i = 0; i < fade; i++)
        {
            float t = (float)i / fade;
            o[i] = b[i] * Mathf.Sqrt(t) + b[n + i] * Mathf.Sqrt(1f - t);
        }
        return o;
    }

    class SVF
    {
        float low, band, f, q;
        public void Set(float fc, float res) { f = 2f * Mathf.Sin(Mathf.PI * Mathf.Min(fc, SR / 6.5f) / SR); q = 1f / Mathf.Max(0.5f, res); }
        public float LP(float x) { low += f * band; float hp = x - low - q * band; band += f * hp; return low; }
        public float BP(float x) { low += f * band; float hp = x - low - q * band; band += f * hp; return band; }
        public float HP(float x) { low += f * band; float hp = x - low - q * band; band += f * hp; return hp; }
    }

    static void Reverb(float[] b, float wet, float room = 0.82f, bool loop = false)
    {
        int[] cd = { 1129, 1173, 1081, 1031, 1277, 1361 };
        int[] ad = { 163, 403, 127 };
        var combs = new float[cd.Length][];
        var cidx = new int[cd.Length];
        var cl = new float[cd.Length];
        for (int i = 0; i < cd.Length; i++) combs[i] = new float[cd[i]];
        var aps = new float[ad.Length][];
        var aidx = new int[ad.Length];
        for (int i = 0; i < ad.Length; i++) aps[i] = new float[ad[i]];
        var outb = new float[b.Length];
        int passes = loop ? 2 : 1;
        for (int pass = 0; pass < passes; pass++)
            for (int n = 0; n < b.Length; n++)
            {
                float x = b[n] * 0.12f;
                float y = 0;
                for (int c = 0; c < cd.Length; c++)
                {
                    float o = combs[c][cidx[c]];
                    cl[c] = o * 0.75f + cl[c] * 0.25f;
                    combs[c][cidx[c]] = x + cl[c] * room;
                    cidx[c] = (cidx[c] + 1) % cd[c];
                    y += o;
                }
                for (int a = 0; a < ad.Length; a++)
                {
                    float o = aps[a][aidx[a]];
                    float outp = -y * 0.5f + o;
                    aps[a][aidx[a]] = y + o * 0.5f;
                    y = outp;
                    aidx[a] = (aidx[a] + 1) % ad[a];
                }
                if (pass == passes - 1) outb[n] = y;
            }
        for (int n = 0; n < b.Length; n++) b[n] = b[n] * (1f - wet * 0.3f) + outb[n] * wet;
    }

    // ------------------------------------------------------------------ instruments
    static void Flute(float[] b, float t0, float dur, float f0, float f1, float amp, bool gamaka)
    {
        int start = (int)(t0 * SR);
        int n = (int)((dur + 0.3f) * SR);
        float ph = 0, breathLp = 0, a = Coef(2500f);
        float slide = Mathf.Min(0.09f, dur * 0.3f);
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Clamp01(t / 0.07f) * (t > dur ? Mathf.Exp(-(t - dur) / 0.09f) : 1f);
            float f = t < slide ? Mathf.Lerp(f0, f1, Mathf.SmoothStep(0, 1, t / slide)) : f1;
            f *= 1f + 0.0065f * Mathf.Sin(2f * Mathf.PI * 5.3f * t) * Mathf.Clamp01((t - 0.18f) * 3f);
            if (gamaka && t > dur * 0.35f && t < dur * 0.92f) f *= 1f + 0.045f * Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 3.6f * (t - dur * 0.35f)));
            ph += f / SR; ph -= (int)ph;
            float s = S(ph) + 0.22f * S(ph * 2f) + 0.07f * S(ph * 3f);
            breathLp += a * (Rnd() - breathLp);
            float br = breathLp * (0.16f + 0.3f * Mathf.Exp(-t * 18f));
            Add(b, start + i, (s * 0.8f + br) * env * amp);
        }
    }

    static void Tanpura(float[] b, float t0, float f, float amp, float dur)
    {
        int start = (int)(t0 * SR);
        int n = (int)(dur * SR);
        const int H = 10;
        var ph = new float[H];
        var ha = new float[H];
        var mod = new float[H];
        for (int h = 0; h < H; h++) ha[h] = 1f / Mathf.Pow(h + 1, 0.85f);
        float tau = dur * 0.45f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            if ((i & 63) == 0)
                for (int h = 0; h < H; h++) mod[h] = (0.5f + 0.5f * S(t * (0.6f + (h + 1) * 0.13f) + h * 0.17f)) * ha[h];
            float env = Mathf.Clamp01(t * 100f) * Mathf.Exp(-t / tau);
            float s = 0;
            for (int h = 0; h < H; h++)
            {
                ph[h] += f * (h + 1) * (1f + 0.0004f * (h + 1)) / SR;
                if (ph[h] >= 1f) ph[h] -= 1f;
                s += S(ph[h]) * mod[h];
            }
            Add(b, start + i, s * env * amp);
        }
    }

    static void Pluck(float[] b, float t0, float f, float amp, float dur, float bright = 0.5f)
    {
        int N = Mathf.Max(2, (int)(SR / f));
        var ring = new float[N];
        for (int i = 0; i < N; i++) ring[i] = Rnd();
        float lp = 0;
        for (int i = 0; i < N; i++) { lp += (ring[i] - lp) * bright; ring[i] = lp; }
        int start = (int)(t0 * SR);
        int n = (int)(dur * SR);
        int idx = 0;
        for (int i = 0; i < n; i++)
        {
            float x = ring[idx];
            int nx = (idx + 1) % N;
            ring[idx] = (x + ring[nx]) * 0.5f * 0.997f;
            idx = nx;
            float env = i > n - 800 ? (n - i) / 800f : 1f;
            Add(b, start + i, (float)System.Math.Tanh(x * 1.6f) * amp * env);
        }
    }

    static void Drum(float[] b, float t0, float f0, float f1, float pdecay, float decay, float amp, float noise, float ndecay, float nfc)
    {
        int start = (int)(t0 * SR);
        int n = (int)(decay * 6f * SR);
        float ph = 0;
        var svf = new SVF(); svf.Set(nfc, 1.5f);
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            float f = f1 + (f0 - f1) * Mathf.Exp(-t / pdecay);
            ph += f / SR; ph -= (int)ph;
            float tone = S(ph) * Mathf.Exp(-t / decay);
            float nz = svf.BP(Rnd()) * Mathf.Exp(-t / ndecay) * noise;
            Add(b, start + i, (tone + nz) * amp);
        }
    }

    static readonly float[] metalRatios = { 1f, 1.47f, 1.98f, 2.44f, 3.11f, 3.98f };

    static void Metal(float[] b, float t0, float f, float amp, float decay)
    {
        int start = (int)(t0 * SR);
        int n = (int)(decay * 5f * SR);
        var ph = new float[metalRatios.Length];
        var hp = new SVF(); hp.Set(5000f, 0.8f);
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            float s = 0;
            for (int k = 0; k < metalRatios.Length; k++)
            {
                ph[k] += f * metalRatios[k] / SR; ph[k] -= (int)ph[k];
                s += (ph[k] < 0.5f ? 1f : -1f) * (1f / (k + 1.5f));
            }
            s = s * 0.5f + hp.HP(Rnd()) * 0.6f;
            Add(b, start + i, s * Mathf.Exp(-t / decay) * Mathf.Clamp01(t / 0.002f) * amp);
        }
    }

    static void Bell(float[] b, float t0, float f, float amp, float decay, float ratio = 1.4f, float index = 2.5f)
    {
        int start = (int)(t0 * SR);
        int n = (int)(decay * 5f * SR);
        float pc = 0, pm = 0;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Exp(-t / decay) * Mathf.Clamp01(t / 0.003f);
            pm += f * ratio / SR; pm -= (int)pm;
            pc += f / SR; pc -= (int)pc;
            float mod = S(pm) * index * Mathf.Exp(-t / (decay * 0.4f));
            float ph = pc + mod * 0.159f;
            ph -= Mathf.Floor(ph);
            Add(b, start + i, S(ph) * env * amp);
        }
    }

    static void Pad(float[] b, float t0, float dur, float[] freqs, float amp, float cutoff, float attack, float release, bool choir = false)
    {
        int start = (int)(t0 * SR);
        int n = (int)((dur + release) * SR);
        int V = freqs.Length * 3;
        var ph = new float[V];
        for (int i = 0; i < V; i++) ph[i] = (float)rng.NextDouble();
        var f1 = new SVF(); var f2 = new SVF();
        if (choir) { f1.Set(720f, 4f); f2.Set(1150f, 5f); } else { f1.Set(cutoff, 0.8f); }
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Clamp01(t / attack) * (t > dur ? Mathf.Exp(-(t - dur) / (release * 0.35f)) : 1f);
            float s = 0;
            for (int v = 0; v < V; v++)
            {
                float det = (v % 3 - 1) * 0.0035f;
                ph[v] += freqs[v / 3] * (1f + det) * (1f + 0.002f * S(t * 0.3f + v * 0.1f)) / SR;
                ph[v] -= (int)ph[v];
                s += ph[v] * 2f - 1f;
            }
            s /= V;
            float o = choir ? f1.BP(s) * 1.2f + f2.BP(s) * 0.8f : f1.LP(s);
            Add(b, start + i, o * env * amp);
        }
    }

    static void Conch(float[] b, float t0, float dur, float f, float amp)
    {
        int start = (int)(t0 * SR);
        int n = (int)((dur + 0.4f) * SR);
        float ph = 0, br = 0;
        var form = new SVF(); form.Set(900f, 2.5f);
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Clamp01(t / 0.25f) * (t > dur ? Mathf.Exp(-(t - dur) / 0.12f) : 1f);
            float ff = f * (0.93f + 0.07f * Mathf.Clamp01(t / 0.35f)) * (1f + 0.004f * Mathf.Sin(t * 2f * Mathf.PI * 4.5f));
            ph += ff / SR; ph -= (int)ph;
            float s = (float)System.Math.Tanh(S(ph) * 3.5f) + 0.3f * S(ph * 2f);
            br += (Rnd() - br) * 0.2f;
            s = s * 0.7f + form.BP(s) * 0.6f + br * 0.08f;
            Add(b, start + i, s * env * amp);
        }
    }

    static void Noise(float[] b, float t0, float dur, float amp, float fcA, float fcB, float res, float attack, float decay)
    {
        int start = (int)(t0 * SR);
        int n = (int)(dur * SR);
        var f = new SVF();
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR, u = t / dur;
            if (i % 32 == 0) f.Set(Mathf.Lerp(fcA, fcB, u), res);
            float env = Mathf.Clamp01(t / attack) * Mathf.Exp(-t / decay);
            Add(b, start + i, f.BP(Rnd()) * env * amp);
        }
    }

    // ------------------------------------------------------------------ sound effects
    public static float[] Ocean()
    {
        wrap = false;
        var b = Buf(12f);
        float brown = 0, lp1 = 0, lp2 = 0, a1 = Coef(500f), a2 = Coef(1600f);
        for (int i = 0; i < b.Length; i++)
        {
            float t = (float)i / SR;
            float w = Rnd();
            brown = (brown + 0.02f * w) / 1.02f;
            lp1 += a1 * (w - lp1);
            lp2 += a2 * (w - lp2);
            float swell = 0.5f + 0.5f * Mathf.Sin(t * 2f * Mathf.PI / 5.7f) * Mathf.Sin(t * 2f * Mathf.PI / 3.3f + 1.3f);
            float wash = Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * 2f * Mathf.PI / 4.1f + 0.4f)), 6f);
            b[i] = brown * 6f * (0.55f + 0.45f * swell) + lp1 * 0.5f * swell + (lp2 - lp1) * 0.35f * wash;
        }
        Normalize(b, 0.8f);
        return LoopFade(b, SR * 2);
    }

    public static float[] WindLoop()
    {
        wrap = false;
        var b = Buf(12f);
        var f = new SVF(); var w2 = new SVF();
        for (int i = 0; i < b.Length; i++)
        {
            float t = (float)i / SR;
            if (i % 64 == 0)
            {
                f.Set(380f + 260f * Mathf.PerlinNoise(t * 0.25f, 1f) + 120f * Mathf.Sin(t * 0.7f), 1.2f);
                w2.Set(950f + 500f * Mathf.PerlinNoise(t * 0.4f, 5f), 9f);
            }
            float gust = 0.4f + 0.6f * Mathf.PerlinNoise(t * 0.35f, 9f);
            float x = Rnd();
            b[i] = (f.BP(x) * 1.0f + w2.BP(x) * 0.18f) * gust;
        }
        Normalize(b, 0.8f);
        return LoopFade(b, SR * 2);
    }

    public static float[] RainLoop()
    {
        wrap = false;
        var b = Buf(8f);
        var hp = new SVF(); hp.Set(2500f, 0.7f);
        var lp = new SVF(); lp.Set(900f, 0.7f);
        for (int i = 0; i < b.Length; i++)
        {
            float x = Rnd();
            b[i] = hp.HP(x) * 0.25f + lp.LP(x) * 0.25f;
        }
        for (int k = 0; k < 900; k++)
        {
            int p = rng.Next(b.Length - 400);
            float a = (float)rng.NextDouble() * 0.6f;
            float fr = 2000f + (float)rng.NextDouble() * 3000f;
            for (int i = 0; i < 300; i++) b[p + i] += Mathf.Sin(i * fr / SR * 6.283f) * Mathf.Exp(-i / 40f) * a;
        }
        Normalize(b, 0.7f);
        return LoopFade(b, SR);
    }

    public static float[] FireLoop()
    {
        wrap = false;
        var b = Buf(5f);
        var lp = new SVF(); lp.Set(400f, 0.7f);
        for (int i = 0; i < b.Length; i++) b[i] = lp.LP(Rnd()) * 0.5f;
        for (int k = 0; k < 260; k++)
        {
            int p = rng.Next(b.Length - 200);
            float a = (float)rng.NextDouble();
            for (int i = 0; i < 120; i++) b[p + i] += Rnd() * Mathf.Exp(-i / 15f) * a;
        }
        Normalize(b, 0.7f);
        return LoopFade(b, SR / 2);
    }

    public static float[] Creak()
    {
        wrap = false;
        var b = Buf(1.4f);
        var res = new SVF(); res.Set(380f + (float)rng.NextDouble() * 250f, 12f);
        float ph = 0;
        for (int i = 0; i < b.Length; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Sin(Mathf.Clamp01(t / 1.3f) * Mathf.PI);
            float rate = 35f + 90f * env + 20f * Mathf.Sin(t * 13f);
            ph += rate / SR;
            float pulse = 0;
            if (ph >= 1f) { ph -= 1f; pulse = 1f; }
            b[i] = res.BP(pulse * 2f + Rnd() * 0.02f) * env;
        }
        Normalize(b, 0.7f);
        return b;
    }

    public static float[] Thunder()
    {
        wrap = false;
        var b = Buf(6f);
        float brown = 0;
        var lp = new SVF(); lp.Set(140f, 0.8f);
        var hp = new SVF(); hp.Set(1800f, 0.7f);
        float burst = 0;
        for (int i = 0; i < b.Length; i++)
        {
            float t = (float)i / SR;
            float w = Rnd();
            brown = (brown + 0.03f * w) / 1.03f;
            if (rng.NextDouble() < 0.0004) burst = 1f;
            burst *= 0.9997f;
            float env = Mathf.Exp(-t / 2.2f) * (0.5f + burst);
            float crack = hp.HP(w) * Mathf.Exp(-t / 0.12f) * 0.7f;
            b[i] = lp.LP(brown * 8f) * env * 2f + crack;
        }
        Normalize(b, 0.95f);
        return b;
    }

    public static float[] Volley()
    {
        wrap = false;
        var b = Buf(0.9f);
        for (int k = 0; k < 6; k++)
        {
            float t0 = k * 0.05f + (float)rng.NextDouble() * 0.03f;
            Pluck(b, t0, 150f + (float)rng.NextDouble() * 80f, 0.25f, 0.25f, 0.6f);
            Noise(b, t0, 0.5f, 0.5f, 2600f, 700f, 1.5f, 0.01f, 0.18f);
        }
        Normalize(b, 0.85f);
        return b;
    }

    public static float[] Thunk()
    {
        wrap = false;
        var b = Buf(0.5f);
        Drum(b, 0, 180f, 70f, 0.02f, 0.07f, 0.9f, 0.6f, 0.02f, 1600f);
        Noise(b, 0.0f, 0.2f, 0.4f, 3000f, 1200f, 2f, 0.001f, 0.03f);
        Normalize(b, 0.85f);
        return b;
    }

    public static float[] SplashSmall()
    {
        wrap = false;
        var b = Buf(0.7f);
        Noise(b, 0, 0.7f, 1f, 3500f, 700f, 0.9f, 0.004f, 0.15f);
        for (int k = 0; k < 4; k++)
        {
            float t0 = 0.05f + (float)rng.NextDouble() * 0.3f;
            int s0 = (int)(t0 * SR);
            float f0 = 500f + (float)rng.NextDouble() * 600f;
            float ph = 0;
            for (int i = 0; i < SR / 20; i++)
            {
                float t = (float)i / SR;
                ph += f0 * (1f + t * 30f) / SR;
                Add(b, s0 + i, Mathf.Sin(ph * 6.283f) * Mathf.Exp(-t / 0.015f) * 0.25f);
            }
        }
        Normalize(b, 0.8f);
        return b;
    }

    public static float[] Crash()
    {
        wrap = false;
        var b = Buf(1.6f);
        Drum(b, 0, 120f, 45f, 0.05f, 0.25f, 1f, 0.8f, 0.08f, 900f);
        Noise(b, 0.0f, 1.2f, 0.7f, 2500f, 400f, 1.2f, 0.002f, 0.3f);
        for (int k = 0; k < 8; k++) Noise(b, 0.05f + k * 0.07f, 0.1f, 0.3f, 3000f, 2000f, 3f, 0.001f, 0.02f);
        var cr = Creak();
        for (int i = 0; i < cr.Length && i < b.Length; i++) b[i] += cr[i] * 0.4f;
        Normalize(b, 0.95f);
        return b;
    }

    public static float[] Sink()
    {
        wrap = false;
        var b = Buf(4.5f);
        float brown = 0;
        var lp = new SVF(); lp.Set(200f, 0.8f);
        for (int i = 0; i < b.Length; i++)
        {
            float t = (float)i / SR;
            brown = (brown + 0.02f * Rnd()) / 1.02f;
            b[i] = lp.LP(brown * 10f) * Mathf.Exp(-t / 2f);
        }
        for (int k = 0; k < 40; k++)
        {
            float t0 = (float)rng.NextDouble() * 3.5f;
            int s0 = (int)(t0 * SR);
            float f0 = 200f + (float)rng.NextDouble() * 500f;
            float ph = 0;
            for (int i = 0; i < SR / 12; i++)
            {
                float t = (float)i / SR;
                ph += f0 * (1f + t * 12f) / SR;
                Add(b, s0 + i, Mathf.Sin(ph * 6.283f) * Mathf.Exp(-t / 0.03f) * 0.2f);
            }
        }
        Normalize(b, 0.9f);
        return b;
    }

    public static float[] Click()
    {
        wrap = false;
        var b = Buf(0.12f);
        Bell(b, 0, 1450f, 0.5f, 0.025f, 2f, 1.5f);
        Normalize(b, 0.5f);
        return b;
    }

    public static float[] Coin()
    {
        wrap = false;
        var b = Buf(0.9f);
        Bell(b, 0, 2100f, 0.5f, 0.12f, 1.41f, 1.2f);
        Bell(b, 0.07f, 2650f, 0.45f, 0.15f, 1.41f, 1.2f);
        Bell(b, 0.13f, 3150f, 0.3f, 0.12f, 1.41f, 1f);
        Normalize(b, 0.6f);
        return b;
    }

    public static float[] Gong()
    {
        wrap = false;
        var b = Buf(3f);
        Bell(b, 0, 196f, 0.7f, 0.9f, 1.4f, 3f);
        Bell(b, 0, 392f * 1.01f, 0.3f, 0.6f, 1.4f, 2f);
        Drum(b, 0, 90f, 60f, 0.1f, 0.4f, 0.4f, 0f, 0.1f, 500f);
        Reverb(b, 0.4f);
        Normalize(b, 0.8f);
        return b;
    }

    public static float[] ShiftOn()
    {
        wrap = false;
        var b = Buf(3.2f);
        Noise(b, 0, 1.4f, 0.6f, 200f, 3200f, 2f, 0.9f, 0.8f);
        Drum(b, 0.0f, 70f, 40f, 0.2f, 0.8f, 0.7f, 0, 0.1f, 500f);
        float[] notes = { 587.3f, 740f, 880f, 1108.7f, 1174.7f };
        for (int i = 0; i < notes.Length; i++) Bell(b, 0.25f + i * 0.09f, notes[i], 0.22f, 0.7f, 3.5f, 1.4f);
        Reverb(b, 0.55f, 0.86f);
        Normalize(b, 0.8f);
        return b;
    }

    public static float[] ShiftOff()
    {
        wrap = false;
        var b = Buf(1.8f);
        Noise(b, 0, 1.0f, 0.5f, 2600f, 180f, 2f, 0.05f, 0.4f);
        Bell(b, 0.05f, 880f, 0.2f, 0.4f, 3.5f, 1.2f);
        Bell(b, 0.15f, 587f, 0.2f, 0.5f, 3.5f, 1.2f);
        Reverb(b, 0.45f);
        Normalize(b, 0.6f);
        return b;
    }

    public static float[] Discovery()
    {
        wrap = false;
        var b = Buf(4f);
        float[] notes = { 293.66f, 311.13f, 369.99f, 440f, 587.33f, 739.99f, 880f };
        for (int i = 0; i < notes.Length; i++) Bell(b, i * 0.13f, notes[i] * 2f, 0.3f, 0.9f, 3.5f, 1.6f);
        Pad(b, 0, 2.2f, new[] { 146.83f, 220f, 293.66f }, 0.4f, 1200f, 0.4f, 1.4f, true);
        Reverb(b, 0.6f, 0.86f);
        Normalize(b, 0.8f);
        return b;
    }

    public static float[] ConchHorn()
    {
        wrap = false;
        var b = Buf(4f);
        Conch(b, 0, 1.0f, 220f, 0.7f);
        Conch(b, 1.3f, 2.0f, 220f, 0.8f);
        Reverb(b, 0.35f);
        Normalize(b, 0.9f);
        return b;
    }

    public static float[] Stinger()
    {
        wrap = false;
        var b = Buf(4f);
        Drum(b, 0, 90f, 50f, 0.08f, 0.6f, 1f, 0.3f, 0.05f, 600f);
        Drum(b, 0.35f, 90f, 50f, 0.08f, 0.6f, 0.8f, 0.3f, 0.05f, 600f);
        Pad(b, 0, 2.4f, new[] { 73.42f, 110f, 146.83f, 155.56f }, 0.6f, 700f, 0.05f, 1.2f);
        Metal(b, 0, 520f, 0.25f, 0.5f);
        Reverb(b, 0.4f);
        Normalize(b, 0.9f);
        return b;
    }

    public static float[] Victory()
    {
        wrap = false;
        var b = Buf(4f);
        float[] ch = { 293.66f, 369.99f, 440f, 587.33f };
        for (int i = 0; i < ch.Length; i++) Pluck(b, i * 0.06f, ch[i], 0.35f, 2.5f, 0.6f);
        Bell(b, 0.3f, 1174.7f, 0.25f, 0.9f, 3.5f, 1.2f);
        Pad(b, 0, 2f, new[] { 146.83f, 220f, 293.66f }, 0.35f, 1500f, 0.2f, 1.5f);
        Reverb(b, 0.45f);
        Normalize(b, 0.8f);
        return b;
    }

    // ------------------------------------------------------------------ music
    static float Hz(float semisFromD3) => 146.83f * Mathf.Pow(2f, semisFromD3 / 12f);

    /// <summary>"Malabar Dawn" — D Mohanam, 84 bpm, tanpura + flute + mizhavu.</summary>
    public static float[] MusicCalm()
    {
        Init();
        float bpm = 84f, beat = 60f / bpm;
        int beats = 32;
        var b = Buf(beats * beat);
        wrap = true;
        float[] tp = { Hz(-5), Hz(0), Hz(0), Hz(-12) };
        for (int i = 0; i < beats; i++) Tanpura(b, i * beat, tp[i % 4], 0.11f, 3.6f);
        Pad(b, 0, beats * beat - 2f, new[] { Hz(-12), Hz(-5), Hz(0) }, 0.16f, 900f, 3f, 2f);
        int[] mohanam = { 0, 2, 4, 7, 9 };
        System.Func<int, float> deg = d =>
        {
            int oct = Mathf.FloorToInt(d / 5f);
            int idx = ((d % 5) + 5) % 5;
            return Hz(12 + mohanam[idx] + 12 * oct);
        };
        int[,] mel = {
            {3,4},{4,2},{5,2},{4,4},{3,4},
            {2,6},{1,2},{2,4},{3,4},
            {4,4},{5,4},{6,4},{5,2},{4,2},
            {3,12},{-99,4},
            {2,2},{3,2},{4,4},{3,2},{2,2},{1,4},
            {0,4},{1,2},{2,2},{3,8},
            {4,4},{3,2},{2,2},{1,4},{2,4},
            {0,12},{-99,4}
        };
        float t = 0; float prevF = deg(3);
        for (int i = 0; i < mel.GetLength(0); i++)
        {
            int d = mel[i, 0];
            float len = mel[i, 1] * beat / 4f;
            if (d != -99)
            {
                float f = deg(d);
                Flute(b, t, len * 0.95f, prevF, f, 0.2f, len >= beat * 1.5f);
                prevF = f;
            }
            t += len;
        }
        float[] pat = { 0, 1.5f, 2, 3, 3.5f, 4, 5.5f, 6, 7, 7.5f };
        for (int c = 0; c < beats / 8; c++)
            for (int k = 0; k < pat.Length; k++)
            {
                float tt = (c * 8 + pat[k]) * beat;
                bool low = k % 3 == 0;
                if (low) Drum(b, tt, 140f, 85f, 0.03f, 0.16f, 0.28f, 0.2f, 0.02f, 900f);
                else Drum(b, tt, 320f, 260f, 0.01f, 0.05f, 0.12f, 0.6f, 0.015f, 2400f);
            }
        for (int i = 0; i < beats; i += 2) Pluck(b, i * beat + beat * 0.5f, Hz(new[] { 0, 7, 12, 7 }[(i / 2) % 4]), 0.08f, 2.2f, 0.4f);
        Reverb(b, 0.32f, 0.84f, true);
        SoftLimit(b, 1.4f);
        Normalize(b, 0.75f);
        wrap = false;
        return b;
    }

    /// <summary>"Black Sails" — chenda, thavil, ilathalam, low strings and conch. 132 bpm.</summary>
    public static float[] MusicCombat()
    {
        Init();
        float bpm = 132f, beat = 60f / bpm;
        int beats = 32;
        var b = Buf(beats * beat);
        wrap = true;
        int sixteenths = beats * 4;
        bool[] acc = { true, false, false, true, false, false, true, false, true, false, false, true, false, false, true, false };
        for (int i = 0; i < sixteenths; i++)
        {
            float tt = i * beat / 4f;
            bool a = acc[i % 16];
            bool roll = (i % 64) >= 56;
            float amp = a ? 0.35f : (roll ? 0.28f : 0.12f);
            Drum(b, tt, 420f, 300f, 0.008f, 0.06f, amp, 0.9f, 0.02f, 2800f);
        }
        for (int bar = 0; bar < beats / 4; bar++)
        {
            float t0 = bar * 4 * beat;
            Drum(b, t0, 85f, 48f, 0.05f, 0.32f, 0.7f, 0.2f, 0.03f, 500f);
            Drum(b, t0 + 1.5f * beat, 85f, 48f, 0.05f, 0.25f, 0.45f, 0.2f, 0.03f, 500f);
            Drum(b, t0 + 2f * beat, 85f, 48f, 0.05f, 0.32f, 0.65f, 0.2f, 0.03f, 500f);
            if (bar % 2 == 1) Drum(b, t0 + 3.5f * beat, 85f, 48f, 0.05f, 0.25f, 0.5f, 0.2f, 0.03f, 500f);
            for (int k = 0; k < 4; k++) Metal(b, t0 + k * beat, 640f, k == 3 ? 0.16f : 0.1f, k == 3 ? 0.4f : 0.12f);
        }
        int[] ost = { 0, 0, 1, 0, 3, 0, 1, -2 };
        for (int i = 0; i < beats * 2; i++)
        {
            float f = Hz(-12 + ost[i % 8]);
            Pad(b, i * beat / 2f, beat / 2f * 0.8f, new[] { f, f * 2f }, 0.22f, 600f, 0.01f, 0.08f);
        }
        Pad(b, 0, beats * beat - 1f, new[] { Hz(-24), Hz(-17) }, 0.15f, 300f, 1f, 1f);
        Conch(b, 0, 1.6f, Hz(12), 0.12f);
        Conch(b, 16 * beat, 1.6f, Hz(13), 0.12f);
        Reverb(b, 0.15f, 0.78f, true);
        SoftLimit(b, 1.6f);
        Normalize(b, 0.8f);
        wrap = false;
        return b;
    }

    /// <summary>"Echoes" — Mayamalavagowla bells and choir over a low drone. 64 bpm.</summary>
    public static float[] MusicMystery()
    {
        Init();
        float bpm = 64f, beat = 60f / bpm;
        int beats = 16;
        var b = Buf(beats * beat);
        wrap = true;
        float[][] chords = {
            new[] { Hz(0), Hz(7), Hz(16) },
            new[] { Hz(1), Hz(8), Hz(17) },
            new[] { Hz(0), Hz(7), Hz(16) },
            new[] { Hz(-1), Hz(5), Hz(8) },
        };
        for (int c = 0; c < 4; c++)
        {
            Pad(b, c * 4 * beat, 4 * beat, chords[c], 0.28f, 1000f, 1.2f, 2f, true);
            for (int k = 0; k < 8; k++)
            {
                float f = chords[c][k % 3] * (k % 2 == 0 ? 2f : 4f);
                Bell(b, c * 4 * beat + k * beat / 2f, f, 0.07f + 0.04f * (k % 3 == 0 ? 1 : 0), 1.1f, 3.5f, 1.3f);
            }
            Noise(b, c * 4 * beat + 2.5f * beat, 1.5f * beat, 0.08f, 300f, 2500f, 2f, 1.4f * beat, 10f);
        }
        Pad(b, 0, beats * beat - 1f, new[] { Hz(-12), Hz(-24) }, 0.18f, 250f, 1f, 1f);
        for (int i = 0; i < beats; i += 2) Drum(b, i * beat, 55f, 40f, 0.1f, 0.5f, 0.25f, 0f, 0.1f, 400f);
        Reverb(b, 0.6f, 0.88f, true);
        SoftLimit(b, 1.3f);
        Normalize(b, 0.7f);
        wrap = false;
        return b;
    }

    /// <summary>"Monsoon" — storm tension: low drums, swelling strings.</summary>
    public static float[] MusicStorm()
    {
        Init();
        float bpm = 100f, beat = 60f / bpm;
        int beats = 32;
        var b = Buf(beats * beat);
        wrap = true;
        for (int i = 0; i < beats; i++)
        {
            float amp = (i % 4 == 0) ? 0.6f : (i % 2 == 0 ? 0.35f : 0.2f);
            Drum(b, i * beat, 70f, 42f, 0.06f, 0.4f, amp, 0.25f, 0.04f, 400f);
            if (i % 8 == 7) for (int k = 0; k < 4; k++) Drum(b, i * beat + k * beat / 4f, 300f, 230f, 0.01f, 0.06f, 0.15f + k * 0.04f, 0.8f, 0.02f, 2400f);
        }
        float[][] ch = { new[] { Hz(-12), Hz(-5), Hz(3) }, new[] { Hz(-14), Hz(-7), Hz(1) }, new[] { Hz(-12), Hz(-5), Hz(3) }, new[] { Hz(-11), Hz(-4), Hz(5) } };
        for (int c = 0; c < 4; c++) Pad(b, c * 8 * beat, 8 * beat, ch[c], 0.3f, 700f, 2f, 1.5f);
        for (int i = 0; i < 4; i++) Flute(b, i * 8 * beat + 4 * beat, 3f * beat, Hz(15), Hz(i % 2 == 0 ? 13 : 12), 0.08f, true);
        Reverb(b, 0.3f, 0.85f, true);
        SoftLimit(b, 1.5f);
        Normalize(b, 0.75f);
        wrap = false;
        return b;
    }

    public static void Prepare() => Init();
}
