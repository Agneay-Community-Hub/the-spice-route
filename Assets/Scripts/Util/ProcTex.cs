using UnityEngine;

/// <summary>All textures and UI sprites are generated at runtime.</summary>
public static class ProcTex
{
    static int[] perm;

    static void InitPerm()
    {
        if (perm != null) return;
        perm = new int[512];
        var p = new int[256];
        for (int i = 0; i < 256; i++) p[i] = i;
        var r = new System.Random(1337);
        for (int i = 255; i > 0; i--) { int j = r.Next(i + 1); int t = p[i]; p[i] = p[j]; p[j] = t; }
        for (int i = 0; i < 512; i++) perm[i] = p[i & 255];
    }

    static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

    static float Grad(int h, float x, float y)
    {
        switch (h & 7)
        {
            case 0: return x + y;
            case 1: return -x + y;
            case 2: return x - y;
            case 3: return -x - y;
            case 4: return x;
            case 5: return -x;
            case 6: return y;
            default: return -y;
        }
    }

    static int Wrap(int a, int p) { int m = a % p; return m < 0 ? m + p : m; }

    /// <summary>Tileable gradient noise in roughly [-1,1].</summary>
    public static float Perlin(float x, float y, int period)
    {
        InitPerm();
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
        float xf = x - xi, yf = y - yi;
        int x0 = Wrap(xi, period) & 255, x1 = Wrap(xi + 1, period) & 255;
        int y0 = Wrap(yi, period) & 255, y1 = Wrap(yi + 1, period) & 255;
        float u = Fade(xf), v = Fade(yf);
        int aa = perm[perm[x0] + y0], ab = perm[perm[x0] + y1], ba = perm[perm[x1] + y0], bb = perm[perm[x1] + y1];
        float l1 = Mathf.Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
        float l2 = Mathf.Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);
        return Mathf.Lerp(l1, l2, v);
    }

    public static float Fbm(float x, float y, int period, int oct)
    {
        float s = 0, a = 0.5f, norm = 0;
        int p = period;
        for (int o = 0; o < oct; o++)
        {
            s += a * Perlin(x, y, p);
            norm += a;
            x *= 2; y *= 2; p *= 2; a *= 0.5f;
        }
        return s / norm;
    }

    static float C01(float x) => Mathf.Clamp01(x);

    public static Texture2D Noise(int size = 256)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                float r = C01((Fbm(u * 4, v * 4, 4, 5) * 0.5f + 0.5f - 0.5f) * 1.7f + 0.5f);
                float g = C01((Fbm(u * 8 + 3, v * 8 + 1, 8, 4) * 0.5f + 0.5f - 0.5f) * 1.6f + 0.5f);
                float b = C01((Fbm(u * 16 + 7, v * 16 + 5, 16, 3) * 0.5f + 0.5f - 0.5f) * 1.6f + 0.5f);
                float a = C01(1f - Mathf.Abs(Fbm(u * 6 + 11, v * 6 + 2, 6, 4)) * 1.8f);
                px[y * size + x] = new Color32((byte)(r * 255), (byte)(g * 255), (byte)(b * 255), (byte)(a * 255));
            }
        tex.SetPixels32(px);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 4;
        tex.Apply(true, true);
        return tex;
    }

    public static Texture2D WaterNormal(int size = 256)
    {
        var h = new float[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                h[y * size + x] = Fbm(u * 8, v * 8, 8, 4) + 0.4f * Fbm(u * 16 + 0.3f, v * 16 + 0.7f, 16, 3);
            }
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
        var px = new Color32[size * size];
        float s = 7f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = h[y * size + (x + 1) % size] - h[y * size + (x - 1 + size) % size];
                float dy = h[((y + 1) % size) * size + x] - h[((y - 1 + size) % size) * size + x];
                Vector3 n = new Vector3(-dx * s, -dy * s, 1f).normalized;
                px[y * size + x] = new Color32((byte)((n.x * 0.5f + 0.5f) * 255), (byte)((n.y * 0.5f + 0.5f) * 255), (byte)((n.z * 0.5f + 0.5f) * 255), 255);
            }
        tex.SetPixels32(px);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 4;
        tex.Apply(true, true);
        return tex;
    }

    public enum SailStyle { Kaveri, Raider, Merchant, Pennant }

    public static Texture2D SailTex(SailStyle style)
    {
        int W = 256, H = 256;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, true, false);
        var px = new Color[W * H];
        Color cream = new Color(0.86f, 0.79f, 0.64f);
        Color ochre = new Color(0.78f, 0.42f, 0.17f);
        Color charcoal = new Color(0.13f, 0.11f, 0.11f);
        Color crimson = new Color(0.45f, 0.06f, 0.05f);
        Color white = new Color(0.9f, 0.88f, 0.82f);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = (float)x / W, v = (float)y / H;
                int band = (int)(v * 7f);
                Color c;
                switch (style)
                {
                    case SailStyle.Raider: c = (band % 3 == 1) ? crimson : charcoal; break;
                    case SailStyle.Merchant: c = (band % 2 == 1) ? Color.Lerp(white, new Color(0.6f, 0.62f, 0.55f), 0.25f) : white; break;
                    case SailStyle.Pennant: c = v > 0.5f ? new Color(0.85f, 0.55f, 0.12f) : new Color(0.62f, 0.1f, 0.06f); break;
                    default: c = (band % 2 == 1) ? ochre : cream; break;
                }
                float fv = v * 7f - band;
                if (fv < 0.03f || fv > 0.97f) c *= 0.8f;
                if (Mathf.Repeat(u * 9f, 1f) < 0.02f) c *= 0.86f;

                float cx = u - 0.5f, cy = v - 0.52f;
                float r = Mathf.Sqrt(cx * cx + cy * cy);
                if (style == SailStyle.Kaveri)
                {
                    Color gold = new Color(0.98f, 0.76f, 0.3f);
                    Color deep = new Color(0.55f, 0.12f, 0.06f);
                    float ang = Mathf.Atan2(cy, cx);
                    float ray = Mathf.Abs(Mathf.Sin(ang * 8f));
                    if (r < 0.11f) c = Color.Lerp(gold, deep, Mathf.SmoothStep(0.07f, 0.11f, r) * 0.6f);
                    else if (r < 0.125f) c = deep;
                    else if (r < 0.13f + ray * 0.1f && r < 0.24f) c = Color.Lerp(c, gold, 0.85f);
                    if (r < 0.05f) c = deep;
                    if (r < 0.03f) c = gold;
                }
                else if (style == SailStyle.Raider)
                {
                    float ex = cx / 0.3f;
                    float lid = 0.14f * (1f - ex * ex);
                    if (Mathf.Abs(ex) < 1f && Mathf.Abs(cy) < lid)
                    {
                        c = new Color(0.72f, 0.66f, 0.55f);
                        if (r < 0.085f) c = new Color(0.62f, 0.05f, 0.03f);
                        if (r < 0.04f) c = new Color(0.02f, 0.02f, 0.02f);
                    }
                    else if (Mathf.Abs(ex) < 1.05f && Mathf.Abs(cy) < lid + 0.012f) c = new Color(0.55f, 0.48f, 0.38f);
                }
                float n = Fbm(u * 6, v * 6, 6, 4) * 0.5f + 0.5f;
                float edge = Mathf.Min(Mathf.Min(u, 1 - u), Mathf.Min(v, 1 - v));
                c *= (0.82f + 0.24f * n) * (0.8f + 0.2f * Mathf.Clamp01(edge * 12f));
                c.a = 1;
                px[y * W + x] = c;
            }
        tex.SetPixels(px);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 4;
        tex.Apply(true, true);
        return tex;
    }

    public static Texture2D Parchment(int size = 512)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, false);
        var px = new Color[size * size];
        Color baseC = new Color(0.86f, 0.77f, 0.58f);
        Color stain = new Color(0.62f, 0.47f, 0.28f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                float n = Fbm(u * 4, v * 4, 4, 5) * 0.5f + 0.5f;
                float n2 = Fbm(u * 16 + 3, v * 16 + 9, 16, 3) * 0.5f + 0.5f;
                float edge = Mathf.Min(Mathf.Min(u, 1 - u), Mathf.Min(v, 1 - v));
                float burn = 1f - Mathf.Clamp01(edge * 9f + (n - 0.5f) * 0.6f);
                Color c = Color.Lerp(baseC, stain, Mathf.Clamp01((n - 0.45f) * 1.6f) * 0.55f + burn * 0.75f);
                c *= 0.9f + 0.12f * n2;
                c.a = 1;
                px[y * size + x] = c;
            }
        tex.SetPixels(px);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply(true, true);
        return tex;
    }

    // ---------- particle textures ----------
    public static Texture2D SoftDot(int s = 64)
    {
        return Make(s, s, (u, v) =>
        {
            float dx = u - 0.5f, dy = v - 0.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
            float a = Mathf.Exp(-d * d * 4.5f) * Mathf.Clamp01(1 - d);
            return new Color(1, 1, 1, a);
        });
    }

    public static Texture2D Smoke(int s = 128)
    {
        return Make(s, s, (u, v) =>
        {
            float dx = u - 0.5f, dy = v - 0.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
            float n = Fbm(u * 4, v * 4, 4, 4) * 0.5f + 0.5f;
            float a = Mathf.Clamp01((1f - d) * 1.6f) * Mathf.Clamp01(n * 1.6f - 0.25f);
            float sh = 0.75f + 0.25f * (1 - v);
            return new Color(sh, sh, sh, a * a);
        });
    }

    public static Texture2D Streak()
    {
        return Make(8, 64, (u, v) =>
        {
            float a = Mathf.Sin(u * Mathf.PI) * Mathf.Pow(Mathf.Sin(v * Mathf.PI), 0.7f);
            return new Color(1, 1, 1, a);
        });
    }

    public static Texture2D Flame(int s = 64)
    {
        return Make(s, s, (u, v) =>
        {
            float dx = (u - 0.5f) * 2f;
            float dy = (v - 0.35f) * 2f;
            float w = Mathf.Lerp(1f, 0.15f, Mathf.Clamp01(v));
            float d = Mathf.Sqrt((dx / w) * (dx / w) + dy * dy * 0.6f);
            float a = Mathf.Clamp01(1 - d);
            a = a * a;
            return new Color(1, 0.9f + 0.1f * a, 0.7f + 0.3f * a, a);
        });
    }

    static Texture2D Make(int w, int h, System.Func<float, float, Color> f, bool linear = false)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true, linear);
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = f((x + 0.5f) / w, (y + 0.5f) / h);
        tex.SetPixels(px);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply(true, true);
        return tex;
    }

    // ---------- UI sprites ----------
    public static Sprite PanelFill, PanelBorder, Circle, Ring, RingThin, Diamond, Glow, Bar, Arrow, ShipIcon, Square, FadeV;

    static float SdRoundBox(float px, float py, float hx, float hy, float r)
    {
        float qx = Mathf.Abs(px) - hx + r, qy = Mathf.Abs(py) - hy + r;
        float ox = Mathf.Max(qx, 0), oy = Mathf.Max(qy, 0);
        return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
    }

    static Sprite SpriteFrom(int w, int h, System.Func<float, float, float> alpha, float border = 0)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float a = Mathf.Clamp01(alpha(x + 0.5f, y + 0.5f));
                px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        tex.SetPixels32(px);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }

    public static void InitSprites()
    {
        if (PanelFill != null) return;
        int S = 64; float R = 14;
        PanelFill = SpriteFrom(S, S, (x, y) => 0.5f - SdRoundBox(x - S / 2f, y - S / 2f, S / 2f - 1, S / 2f - 1, R), 20);
        PanelBorder = SpriteFrom(S, S, (x, y) =>
        {
            float d = SdRoundBox(x - S / 2f, y - S / 2f, S / 2f - 1, S / 2f - 1, R);
            return Mathf.Clamp01(0.5f - d) - Mathf.Clamp01(0.5f - (d + 2f));
        }, 20);
        int C = 128;
        Circle = SpriteFrom(C, C, (x, y) => C / 2f - 1 - Mathf.Sqrt((x - C / 2f) * (x - C / 2f) + (y - C / 2f) * (y - C / 2f)));
        Ring = SpriteFrom(C, C, (x, y) =>
        {
            float d = Mathf.Sqrt((x - C / 2f) * (x - C / 2f) + (y - C / 2f) * (y - C / 2f));
            return 5f - Mathf.Abs(d - (C / 2f - 7f));
        });
        RingThin = SpriteFrom(C, C, (x, y) =>
        {
            float d = Mathf.Sqrt((x - C / 2f) * (x - C / 2f) + (y - C / 2f) * (y - C / 2f));
            return 1.6f - Mathf.Abs(d - (C / 2f - 3f));
        });
        Glow = SpriteFrom(C, C, (x, y) =>
        {
            float d = Mathf.Sqrt((x - C / 2f) * (x - C / 2f) + (y - C / 2f) * (y - C / 2f)) / (C / 2f);
            float a = Mathf.Clamp01(1 - d);
            return a * a;
        });
        Diamond = SpriteFrom(64, 64, (x, y) => (26f - (Mathf.Abs(x - 32) + Mathf.Abs(y - 32))) * 0.7f);
        Arrow = SpriteFrom(64, 64, (x, y) =>
        {
            float u = (x - 32) / 32f, v = (y - 32) / 32f;
            float head = (v > 0.05f) ? (0.55f * (1 - (v - 0.05f) / 0.9f) - Mathf.Abs(u)) * 40f : -1;
            float shaft = (v <= 0.1f && v > -0.9f) ? (0.13f - Mathf.Abs(u)) * 40f : -1;
            return Mathf.Max(head, shaft);
        });
        ShipIcon = SpriteFrom(64, 64, (x, y) =>
        {
            float u = (x - 32) / 32f, v = (y - 32) / 32f;
            float w = 0.32f * Mathf.Sqrt(Mathf.Clamp01(1 - v * v)) * (v > 0 ? Mathf.Lerp(1, 0.2f, v) : 1f);
            return (w - Mathf.Abs(u)) * 50f * (Mathf.Abs(v) < 0.92f ? 1 : 0);
        });
        Square = SpriteFrom(8, 8, (x, y) => 1f);
        Bar = SpriteFrom(256, 8, (x, y) => Mathf.Sin(x / 256f * Mathf.PI));
        FadeV = SpriteFrom(8, 256, (x, y) => y / 256f);
    }
}
