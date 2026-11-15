using UnityEngine;

/// <summary>Procedural prop library. Each function writes geometry into MeshBuilders (batched per material).</summary>
public static class Props
{
    public static readonly Color Laterite = new Color(0.58f, 0.3f, 0.2f);
    public static readonly Color Whitewash = new Color(0.9f, 0.86f, 0.78f);
    public static readonly Color DarkWood = new Color(0.26f, 0.16f, 0.09f);
    public static readonly Color WoodC = new Color(0.47f, 0.31f, 0.18f);
    public static readonly Color Tile = new Color(0.6f, 0.26f, 0.15f);
    public static readonly Color Thatch = new Color(0.6f, 0.49f, 0.3f);
    public static readonly Color Copper = new Color(0.32f, 0.55f, 0.47f);
    public static readonly Color GoldC = new Color(0.95f, 0.72f, 0.3f);
    public static readonly Color StoneC = new Color(0.6f, 0.57f, 0.5f);
    public static readonly Color Sandstone = new Color(0.78f, 0.64f, 0.45f);
    public static readonly Color Mudbrick = new Color(0.74f, 0.56f, 0.38f);
    public static readonly Color Skin = new Color(0.42f, 0.27f, 0.18f);
    public static readonly Color Cloth = new Color(0.92f, 0.89f, 0.8f);

    public static Color Vary(Color c, System.Random r, float amt)
    {
        float k = 1f + ((float)r.NextDouble() * 2f - 1f) * amt;
        return new Color(c.r * k, c.g * k, c.b * k, 1f);
    }

    static float R(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

    static void Push(MeshBuilder b, Vector3 pos, float yaw) => b.M = Matrix4x4.TRS(pos, Quaternion.Euler(0, yaw, 0), Vector3.one);
    static void Pop(MeshBuilder b) => b.M = Matrix4x4.identity;

    // ------------------------------------------------------------------ vegetation
    public static void Palm(MeshBuilder fol, Vector3 basePos, float h, float lean, float yaw, System.Random r, bool date = false)
    {
        Vector3 ld = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
        int seg = 7;
        Color c1 = date ? new Color(0.5f, 0.4f, 0.28f) : new Color(0.46f, 0.38f, 0.29f);
        Color c2 = c1 * 0.82f; c2.a = 1;
        float s = h / 12f;
        System.Func<float, Vector3> P = t => basePos + Vector3.up * h * t + ld * (lean * h * t * t);
        for (int i = 0; i < seg; i++)
        {
            float t0 = (float)i / seg, t1 = (float)(i + 1) / seg;
            fol.Sway = t0 * 0.3f;
            fol.Cylinder(P(t0) - Vector3.up * (i == 0 ? 0.5f : 0f), P(t1), Mathf.Lerp(0.34f, 0.19f, t0) * s * (i == 0 ? 1.3f : 1f), Mathf.Lerp(0.34f, 0.19f, t1) * s, 6, i % 2 == 0 ? c1 : c2);
        }
        Vector3 top = P(1f);
        int nf = date ? 14 : 11;
        Color baseCol = date ? new Color(0.36f, 0.45f, 0.22f) : new Color(0.17f, 0.38f, 0.1f);
        Color tipCol = date ? new Color(0.52f, 0.55f, 0.3f) : new Color(0.42f, 0.5f, 0.16f);
        float a0 = R(r, 0, 360);
        for (int f = 0; f < nf; f++)
        {
            float ang = a0 + f * 360f / nf + R(r, -12, 12);
            float len = (date ? 3.4f : 4.8f) * s * R(r, 0.85f, 1.15f);
            float droop = (date ? 0.7f : 1.0f) * R(r, 0.8f, 1.25f);
            Vector3 dir = Quaternion.Euler(0, ang, 0) * Vector3.forward;
            Frond(fol, top, dir, len, droop, Vary(baseCol, r, 0.15f), Vary(tipCol, r, 0.12f));
        }
        if (!date)
        {
            fol.Sway = 0.3f;
            for (int k = 0; k < 4; k++)
            {
                Vector3 off = Quaternion.Euler(0, k * 90 + a0, 0) * Vector3.forward * 0.3f * s;
                fol.Sphere(top + off - Vector3.up * 0.35f * s, Vector3.one * 0.2f * s, 5, 4, new Color(0.33f, 0.36f, 0.14f));
            }
        }
        fol.Sway = 0;
    }

    static void Frond(MeshBuilder b, Vector3 start, Vector3 dir, float len, float droop, Color baseCol, Color tipCol)
    {
        int n = 6;
        Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
        Vector3 pc = start, pl = start, pr = start;
        for (int i = 1; i <= n; i++)
        {
            float s = (float)i / n;
            Vector3 c = start + dir * len * s + Vector3.up * (len * (0.42f * s - droop * 0.72f * s * s));
            float w = Mathf.Sin(Mathf.PI * Mathf.Min(1f, s * 1.1f)) * len * 0.2f + 0.04f;
            Vector3 l = c + side * w - Vector3.up * w * 0.4f;
            Vector3 rr = c - side * w - Vector3.up * w * 0.4f;
            Color col = Color.Lerp(baseCol, tipCol, s);
            b.Sway = 0.3f + 0.7f * s;
            b.Card(pl, l, c, pc, col);
            b.Card(pc, c, rr, pr, col * 0.92f);
            pc = c; pl = l; pr = rr;
        }
        b.Sway = 0;
    }

    public static void Bush(MeshBuilder fol, Vector3 p, float size, System.Random r, Color c)
    {
        fol.Sway = 0.25f;
        for (int i = 0; i < 3; i++)
        {
            Vector3 o = new Vector3(R(r, -0.6f, 0.6f), R(r, 0.2f, 0.6f), R(r, -0.6f, 0.6f)) * size;
            fol.Sphere(p + o, new Vector3(1f, 0.7f, 1f) * size * R(r, 0.6f, 1f), 6, 4, Vary(c, r, 0.15f), Vary(c * 0.7f, r, 0.1f));
        }
        fol.Sway = 0;
    }

    // ------------------------------------------------------------------ Kerala architecture
    public static void KeralaHouse(MeshBuilder b, Vector3 pos, float yaw, float w, float d, float wallH, System.Random r, bool wooden)
    {
        Push(b, pos, yaw);
        Color lat = Vary(Laterite, r, 0.08f);
        Color wall = wooden ? Vary(WoodC, r, 0.1f) : Vary(Whitewash, r, 0.04f);
        Color tile = Vary(Tile, r, 0.1f);
        b.Box(new Vector3(0, 0.3f, 0.4f), new Vector3(w + 1.0f, 1.2f, d + 1.8f), lat);
        b.Box(new Vector3(0, 0.9f + wallH / 2f, 0), new Vector3(w, wallH, d), wall);
        b.Box(new Vector3(0, 0.9f + 1.05f, d / 2f + 0.03f), new Vector3(1.1f, 2.1f, 0.08f), DarkWood);
        b.Box(new Vector3(-w * 0.3f, 0.9f + wallH * 0.6f, d / 2f + 0.03f), new Vector3(0.8f, 0.8f, 0.08f), DarkWood);
        b.Box(new Vector3(w * 0.3f, 0.9f + wallH * 0.6f, d / 2f + 0.03f), new Vector3(0.8f, 0.8f, 0.08f), DarkWood);
        for (int i = 0; i < 4; i++)
        {
            float x = Mathf.Lerp(-w / 2f + 0.3f, w / 2f - 0.3f, i / 3f);
            b.Cylinder(new Vector3(x, 0.9f, d / 2f + 1.1f), new Vector3(x, 0.9f + wallH, d / 2f + 1.1f), 0.13f, 0.11f, 6, DarkWood);
        }
        float eave = 0.9f + wallH;
        float o = 1.3f;
        float hx = w / 2f + o, hz = d / 2f + o + 0.4f;
        float zc = 0.4f;
        float roofH = (w + d) * 0.27f;
        float rl = Mathf.Max(0.3f, (w - d) * 0.5f);
        Vector3 e1 = new Vector3(-hx, eave, zc - hz), e2 = new Vector3(hx, eave, zc - hz), e3 = new Vector3(hx, eave, zc + hz), e4 = new Vector3(-hx, eave, zc + hz);
        Vector3 r1 = new Vector3(-rl, eave + roofH, zc), r2 = new Vector3(rl, eave + roofH, zc);
        Vector3 centre = new Vector3(0, eave, zc);
        b.FlatQuad(e4, r1, r2, e3, tile, new Vector3(0, 1, 1));
        b.FlatQuad(e1, r1, r2, e2, Vary(tile, r, 0.05f) * 0.9f, new Vector3(0, 1, -1));
        b.FlatTri(e1, r1, e4, tile * 0.95f, new Vector3(-1, 1, 0));
        b.FlatTri(e2, r2, e3, tile * 0.95f, new Vector3(1, 1, 0));
        b.Box(new Vector3(0, eave - 0.05f, zc), new Vector3(hx * 2f, 0.1f, hz * 2f), DarkWood);
        // gable (mukhappu) at ridge ends
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 top = new Vector3(s * (rl + 0.05f), eave + roofH + 0.15f, zc);
            Vector3 a = new Vector3(s * (rl + 0.55f), eave + roofH - 0.85f, zc - 0.75f);
            Vector3 c = new Vector3(s * (rl + 0.55f), eave + roofH - 0.85f, zc + 0.75f);
            b.FlatTri(top, a, c, DarkWood, new Vector3(s, 0, 0));
            b.FlatTri(top, a, c, DarkWood, new Vector3(-s, 0, 0));
        }
        b.Cylinder(r1 + Vector3.up * 0.1f, r2 + Vector3.up * 0.1f, 0.12f, 0.12f, 5, tile * 0.7f);
        Pop(b);
    }

    public static void Godown(MeshBuilder b, Vector3 pos, float yaw, float w, float d, System.Random r)
    {
        Push(b, pos, yaw);
        b.Box(new Vector3(0, 0.4f, 0), new Vector3(w + 0.6f, 0.8f, d + 0.6f), Vary(Laterite, r, 0.08f));
        b.Box(new Vector3(0, 2.4f, 0), new Vector3(w, 3.2f, d), Vary(WoodC, r, 0.1f));
        for (int i = 0; i < 3; i++)
            b.Box(new Vector3(Mathf.Lerp(-w * 0.35f, w * 0.35f, i / 2f), 1.9f, d / 2f + 0.04f), new Vector3(1.6f, 2.2f, 0.08f), DarkWood);
        float eave = 4.0f, hz = d / 2f + 1.1f, hx = w / 2f + 0.8f, rh = d * 0.45f;
        Color tile = Vary(Tile, r, 0.08f);
        b.FlatQuad(new Vector3(-hx, eave, hz), new Vector3(-hx + 0.5f, eave + rh, 0), new Vector3(hx - 0.5f, eave + rh, 0), new Vector3(hx, eave, hz), tile, new Vector3(0, 1, 1));
        b.FlatQuad(new Vector3(-hx, eave, -hz), new Vector3(-hx + 0.5f, eave + rh, 0), new Vector3(hx - 0.5f, eave + rh, 0), new Vector3(hx, eave, -hz), tile * 0.9f, new Vector3(0, 1, -1));
        b.FlatTri(new Vector3(-hx, eave, -hz), new Vector3(-hx + 0.5f, eave + rh, 0), new Vector3(-hx, eave, hz), DarkWood, Vector3.left);
        b.FlatTri(new Vector3(hx, eave, -hz), new Vector3(hx - 0.5f, eave + rh, 0), new Vector3(hx, eave, hz), DarkWood, Vector3.right);
        b.Box(new Vector3(0, eave - 0.05f, 0), new Vector3(hx * 2, 0.1f, hz * 2), DarkWood);
        Pop(b);
    }

    public static void Temple(MeshBuilder b, MeshBuilder glow, Vector3 pos, float yaw)
    {
        Push(b, pos, yaw);
        b.Cylinder(new Vector3(0, -1f, 0), new Vector3(0, 1.0f, 0), 6.2f, 6.0f, 20, Laterite, false, true);
        b.Cylinder(new Vector3(0, 1.0f, 0), new Vector3(0, 4.6f, 0), 4.6f, 4.6f, 20, Whitewash);
        for (int i = 0; i < 12; i++)
        {
            float a = i / 12f * Mathf.PI * 2f;
            Vector3 p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 5.4f;
            b.Cylinder(p + Vector3.up * 1f, p + Vector3.up * 4.6f, 0.18f, 0.15f, 6, DarkWood);
        }
        b.Cylinder(new Vector3(0, 4.6f, 0), new Vector3(0, 7.2f, 0), 7.2f, 3.4f, 20, Copper, true, false);
        b.Cylinder(new Vector3(0, 7.2f, 0), new Vector3(0, 7.8f, 0), 3.4f, 3.4f, 16, Whitewash);
        b.Cylinder(new Vector3(0, 7.8f, 0), new Vector3(0, 10.6f, 0), 4.2f, 0.2f, 20, Copper * 0.9f, true, false);
        b.Sphere(new Vector3(0, 10.9f, 0), Vector3.one * 0.4f, 8, 6, GoldC);
        b.Cylinder(new Vector3(0, 11.1f, 0), new Vector3(0, 12f, 0), 0.25f, 0.02f, 8, GoldC);
        b.Box(new Vector3(0, 2.2f, 4.62f), new Vector3(1.6f, 2.4f, 0.1f), DarkWood);
        // Deepastambham (lamp tower)
        Vector3 lp = new Vector3(0, 0, 13f);
        b.Box(lp + Vector3.up * 0.4f, new Vector3(2.2f, 0.8f, 2.2f), Laterite);
        b.Cylinder(lp + Vector3.up * 0.8f, lp + Vector3.up * 9.5f, 0.35f, 0.18f, 10, GoldC * 0.8f);
        for (int i = 0; i < 9; i++)
        {
            float y = 2.0f + i * 0.85f;
            float rad = Mathf.Lerp(1.1f, 0.45f, i / 8f);
            b.Cylinder(lp + Vector3.up * y, lp + Vector3.up * (y + 0.08f), rad, rad, 12, GoldC * 0.75f, true, true);
            if (glow != null)
                for (int k = 0; k < 8; k++)
                {
                    float a = k / 8f * Mathf.PI * 2f + i;
                    glow.M = b.M;
                    glow.Box(lp + new Vector3(Mathf.Cos(a) * rad, y + 0.15f, Mathf.Sin(a) * rad), Vector3.one * 0.12f, GoldC);
                }
        }
        if (glow != null) glow.M = Matrix4x4.identity;
        Pop(b);
    }

    public static void Pier(MeshBuilder b, Vector3 start, float yaw, float length, float width, System.Random r, float deckY = 1.4f)
    {
        Push(b, start, yaw);
        int n = Mathf.CeilToInt(length / 0.7f);
        for (int i = 0; i < n; i++)
        {
            float z = i * 0.7f + 0.35f;
            b.Box(new Vector3(0, deckY, z), new Vector3(width, 0.16f, 0.62f), Vary(WoodC, r, 0.12f));
        }
        for (float z = 1f; z < length; z += 4f)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                b.Cylinder(new Vector3(s * width * 0.5f, -6f, z), new Vector3(s * width * 0.5f, deckY + 0.9f, z), 0.2f, 0.18f, 6, DarkWood);
            }
            b.Box(new Vector3(0, deckY - 0.25f, z), new Vector3(width + 0.4f, 0.25f, 0.3f), DarkWood);
        }
        Pop(b);
    }

    public static void Stall(MeshBuilder b, Vector3 pos, float yaw, Color awning, System.Random r)
    {
        Push(b, pos, yaw);
        float w = 3.2f, d = 2.4f;
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0 ? -1 : 1) * w / 2f, z = (i < 2 ? -1 : 1) * d / 2f;
            float hgt = z > 0 ? 2.5f : 2.9f;
            b.Cylinder(new Vector3(x, 0, z), new Vector3(x, hgt, z), 0.07f, 0.07f, 5, DarkWood);
        }
        b.Card(new Vector3(-w / 2f - 0.3f, 2.9f, -d / 2f - 0.2f), new Vector3(-w / 2f - 0.3f, 2.45f, d / 2f + 0.5f), new Vector3(w / 2f + 0.3f, 2.45f, d / 2f + 0.5f), new Vector3(w / 2f + 0.3f, 2.9f, -d / 2f - 0.2f), awning);
        b.Box(new Vector3(0, 0.8f, 0.3f), new Vector3(w - 0.4f, 0.12f, 1.2f), WoodC);
        b.Box(new Vector3(0, 0.4f, 0.3f), new Vector3(w - 0.6f, 0.8f, 1.0f), DarkWood);
        Color[] spices = { new Color(0.12f, 0.1f, 0.08f), new Color(0.36f, 0.48f, 0.2f), new Color(0.5f, 0.28f, 0.12f), new Color(0.85f, 0.6f, 0.08f), new Color(0.65f, 0.12f, 0.06f) };
        for (int i = 0; i < 4; i++)
        {
            Color c = spices[r.Next(spices.Length)];
            b.Sphere(new Vector3(-1.1f + i * 0.73f, 0.9f, 0.3f), new Vector3(0.3f, 0.2f, 0.3f), 7, 4, c);
        }
        for (int i = 0; i < 3; i++)
            b.Sphere(new Vector3(-1.2f + i * 1.1f, 0.35f, -0.8f), new Vector3(0.38f, 0.45f, 0.38f), 6, 5, new Color(0.62f, 0.52f, 0.36f));
        Pop(b);
    }

    public static void Person(MeshBuilder b, Vector3 pos, float yaw, System.Random r, bool turban = false, Color? clothCol = null)
    {
        Push(b, pos, yaw);
        Color cloth = clothCol ?? Vary(Cloth, r, 0.06f);
        Color skin = Vary(Skin, r, 0.15f);
        b.Cylinder(new Vector3(0, 0, 0), new Vector3(0, 0.95f, 0), 0.24f, 0.2f, 7, cloth, true, false);
        bool shirt = r.NextDouble() < 0.35;
        b.Cylinder(new Vector3(0, 0.95f, 0), new Vector3(0, 1.48f, 0), 0.2f, 0.23f, 7, shirt ? Vary(new Color(0.55f, 0.42f, 0.25f), r, 0.2f) : skin, false, true);
        b.Cylinder(new Vector3(0.27f, 1.45f, 0), new Vector3(0.3f, 0.95f, 0.08f), 0.06f, 0.05f, 4, skin);
        b.Cylinder(new Vector3(-0.27f, 1.45f, 0), new Vector3(-0.3f, 0.95f, 0.08f), 0.06f, 0.05f, 4, skin);
        b.Sphere(new Vector3(0, 1.63f, 0), new Vector3(0.12f, 0.14f, 0.13f), 7, 5, skin);
        if (turban) b.Sphere(new Vector3(0, 1.73f, 0), new Vector3(0.15f, 0.09f, 0.15f), 7, 4, Vary(new Color(0.9f, 0.85f, 0.7f), r, 0.1f));
        else b.Sphere(new Vector3(0, 1.69f, -0.02f), new Vector3(0.125f, 0.09f, 0.13f), 6, 4, new Color(0.06f, 0.05f, 0.05f));
        Pop(b);
    }

    public static void SmallBoat(MeshBuilder b, float len, Color hull)
    {
        int n = 8;
        Vector3[] prevL = null, prevR = null;
        for (int i = 0; i <= n; i++)
        {
            float t = (float)i / n;
            float z = (t - 0.5f) * len;
            float w = Mathf.Max(0.04f, Mathf.Sin(t * Mathf.PI)) * len * 0.1f;
            float sheer = 0.5f + Mathf.Pow(Mathf.Abs(t - 0.5f) * 2f, 3f) * 0.6f;
            Vector3[] L = { new Vector3(-w, sheer, z), new Vector3(-w * 0.8f, 0f, z), new Vector3(0, -0.35f, z) };
            Vector3[] Rr = { new Vector3(w, sheer, z), new Vector3(w * 0.8f, 0f, z), new Vector3(0, -0.35f, z) };
            if (prevL != null)
            {
                for (int k = 0; k < 2; k++)
                {
                    b.FlatQuad(prevL[k], L[k], L[k + 1], prevL[k + 1], hull * (k == 0 ? 1f : 0.8f), new Vector3(-1, -k * 0.5f, 0));
                    b.FlatQuad(prevR[k], Rr[k], Rr[k + 1], prevR[k + 1], hull * (k == 0 ? 1f : 0.8f), new Vector3(1, -k * 0.5f, 0));
                    b.FlatQuad(prevL[k], L[k], L[k + 1], prevL[k + 1], hull * 0.5f, new Vector3(1, 0.5f, 0));
                    b.FlatQuad(prevR[k], Rr[k], Rr[k + 1], prevR[k + 1], hull * 0.5f, new Vector3(-1, 0.5f, 0));
                }
            }
            prevL = L; prevR = Rr;
        }
        b.Box(new Vector3(0, 0.2f, 0), new Vector3(len * 0.12f, 0.06f, len * 0.6f), hull * 0.6f);
    }

    // ------------------------------------------------------------------ ruins & stone
    public static void Column(MeshBuilder b, Vector3 pos, float h, float rad, bool broken, System.Random r, Color c)
    {
        b.Box(pos + Vector3.up * 0.25f, new Vector3(rad * 2.8f, 0.5f, rad * 2.8f), c * 0.92f);
        float top = broken ? h * R(r, 0.25f, 0.8f) : h;
        b.Cylinder(pos + Vector3.up * 0.5f, pos + Vector3.up * top, rad, rad * 0.9f, 10, Vary(c, r, 0.06f), false, true);
        if (!broken) b.Box(pos + Vector3.up * (h + 0.25f), new Vector3(rad * 2.6f, 0.5f, rad * 2.6f), c * 0.95f);
        else
        {
            Vector3 drum = pos + new Vector3(R(r, 1.5f, 3f), 0.45f, R(r, -2f, 2f));
            b.Cylinder(drum, drum + Quaternion.Euler(0, R(r, 0, 360), 0) * Vector3.forward * R(r, 1f, 2f) + Vector3.up * 0.1f, rad * 0.95f, rad * 0.9f, 10, c * 0.85f, true, true);
        }
    }

    public static void RuinWall(MeshBuilder b, Vector3 a, Vector3 c, float h, float thick, System.Random r, Color col)
    {
        Vector3 dir = c - a;
        float len = dir.magnitude;
        dir /= len;
        Quaternion q = Quaternion.LookRotation(dir);
        float x = 0;
        while (x < len)
        {
            float bl = Mathf.Min(R(r, 1.2f, 2.4f), len - x);
            float bh = h * R(r, 0.2f, 1f);
            Vector3 p = a + dir * (x + bl / 2f);
            b.Box(p + Vector3.up * bh / 2f, new Vector3(thick, bh, bl * 0.96f), q, Vary(col, r, 0.08f));
            x += bl;
        }
    }

    public static void Stele(MeshBuilder b, Vector3 pos, float yaw)
    {
        Push(b, pos, yaw);
        Color dark = new Color(0.32f, 0.3f, 0.28f);
        b.Box(new Vector3(0, 0.3f, 0), new Vector3(2.4f, 0.6f, 1.4f), StoneC * 0.8f);
        b.Box(new Vector3(0, 2.4f, 0), new Vector3(1.5f, 3.8f, 0.5f), dark);
        b.FlatTri(new Vector3(-0.75f, 4.3f, 0.25f), new Vector3(0, 4.9f, 0.25f), new Vector3(0.75f, 4.3f, 0.25f), dark, Vector3.forward);
        b.FlatTri(new Vector3(-0.75f, 4.3f, -0.25f), new Vector3(0, 4.9f, -0.25f), new Vector3(0.75f, 4.3f, -0.25f), dark, Vector3.back);
        b.FlatQuad(new Vector3(-0.75f, 4.3f, -0.25f), new Vector3(0, 4.9f, -0.25f), new Vector3(0, 4.9f, 0.25f), new Vector3(-0.75f, 4.3f, 0.25f), dark, new Vector3(-1, 1, 0));
        b.FlatQuad(new Vector3(0.75f, 4.3f, -0.25f), new Vector3(0, 4.9f, -0.25f), new Vector3(0, 4.9f, 0.25f), new Vector3(0.75f, 4.3f, 0.25f), dark, new Vector3(1, 1, 0));
        for (int i = 0; i < 5; i++)
            b.Box(new Vector3(0, 1.2f + i * 0.6f, 0.27f), new Vector3(1.1f - (i % 2) * 0.3f, 0.08f, 0.05f), dark * 0.6f);
        Pop(b);
    }

    public static void Lighthouse(MeshBuilder b, MeshBuilder glow, Vector3 pos, float h, Color stone, bool ruined, System.Random r)
    {
        b.Cylinder(pos + Vector3.down * 2f, pos + Vector3.up * 1.2f, 7.5f, 7f, 14, stone * 0.85f, false, true);
        float top = ruined ? h * 0.32f : h;
        int bands = 5;
        for (int i = 0; i < bands; i++)
        {
            float y0 = 1.2f + (top - 1.2f) * i / bands, y1 = 1.2f + (top - 1.2f) * (i + 1) / bands;
            float r0 = Mathf.Lerp(4.6f, 2.8f, (y0 - 1.2f) / h), r1 = Mathf.Lerp(4.6f, 2.8f, (y1 - 1.2f) / h);
            b.Cylinder(pos + Vector3.up * y0, pos + Vector3.up * y1, r0, r1, 14, Vary(stone, r, 0.05f), false, i == bands - 1);
            b.Cylinder(pos + Vector3.up * (y1 - 0.25f), pos + Vector3.up * (y1 + 0.05f), r1 + 0.2f, r1 + 0.2f, 14, stone * 0.8f, true, true);
        }
        if (!ruined)
        {
            b.Cylinder(pos + Vector3.up * h, pos + Vector3.up * (h + 0.5f), 3.6f, 3.6f, 14, stone * 0.8f, true, true);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                Vector3 p = pos + new Vector3(Mathf.Cos(a) * 2.4f, h + 0.5f, Mathf.Sin(a) * 2.4f);
                b.Cylinder(p, p + Vector3.up * 3.2f, 0.18f, 0.16f, 6, stone * 0.7f);
            }
            b.Cylinder(pos + Vector3.up * (h + 3.7f), pos + Vector3.up * (h + 6.2f), 3.2f, 0.2f, 14, Tile * 0.8f, true, false);
            if (glow != null) glow.Cylinder(pos + Vector3.up * (h + 0.6f), pos + Vector3.up * (h + 2.2f), 1.4f, 1.0f, 10, GoldC, true, true);
        }
        else
        {
            for (int i = 0; i < 9; i++)
            {
                Vector3 p = pos + new Vector3(R(r, -9, 9), 0.4f, R(r, -9, 9));
                if (new Vector2(p.x - pos.x, p.z - pos.z).magnitude < 5f) continue;
                b.Box(p, new Vector3(R(r, 0.8f, 1.8f), R(r, 0.5f, 1f), R(r, 0.8f, 1.6f)), Quaternion.Euler(R(r, -15, 15), R(r, 0, 90), R(r, -15, 15)), Vary(stone, r, 0.08f));
            }
        }
    }

    public static void MudHouse(MeshBuilder b, Vector3 pos, float yaw, float w, float d, float h, bool ruined, System.Random r)
    {
        Push(b, pos, yaw);
        Color c = Vary(Mudbrick, r, 0.08f);
        b.Box(new Vector3(0, -0.5f, 0), new Vector3(w + 0.6f, 1f, d + 0.6f), c * 0.85f);
        if (!ruined)
        {
            b.Box(new Vector3(0, h / 2f, 0), new Vector3(w, h, d), c);
            b.Box(new Vector3(0, h + 0.3f, d / 2f - 0.15f), new Vector3(w, 0.6f, 0.3f), c * 0.95f);
            b.Box(new Vector3(0, h + 0.3f, -d / 2f + 0.15f), new Vector3(w, 0.6f, 0.3f), c * 0.95f);
            b.Box(new Vector3(w / 2f - 0.15f, h + 0.3f, 0), new Vector3(0.3f, 0.6f, d), c * 0.95f);
            b.Box(new Vector3(-w / 2f + 0.15f, h + 0.3f, 0), new Vector3(0.3f, 0.6f, d), c * 0.95f);
            for (int i = 0; i < 5; i++)
            {
                float x = Mathf.Lerp(-w / 2f + 0.5f, w / 2f - 0.5f, i / 4f);
                b.Cylinder(new Vector3(x, h - 0.4f, d / 2f - 0.3f), new Vector3(x, h - 0.4f, d / 2f + 0.5f), 0.1f, 0.1f, 5, DarkWood);
            }
        }
        else
        {
            b.Box(new Vector3(-w * 0.25f, h * 0.35f, 0), new Vector3(w * 0.5f, h * 0.7f, d), c);
            b.Box(new Vector3(w * 0.25f, h * 0.2f, -d * 0.25f), new Vector3(w * 0.5f, h * 0.4f, d * 0.5f), c * 0.9f);
            for (int i = 0; i < 6; i++)
                b.Box(new Vector3(R(r, -w, w), 0.2f, R(r, -d, d) + d * 0.6f), Vector3.one * R(r, 0.4f, 0.9f), Quaternion.Euler(R(r, 0, 30), R(r, 0, 90), 0), c * 0.85f);
        }
        b.Box(new Vector3(0, 1.05f, d / 2f + 0.02f), new Vector3(1.1f, 2.1f, 0.06f), DarkWood);
        Pop(b);
    }

    public static void Watchtower(MeshBuilder b, Vector3 pos, float h, System.Random r)
    {
        Color c = Vary(Mudbrick, r, 0.05f);
        b.Cylinder(pos + Vector3.down * 1f, pos + Vector3.up * h, 3.4f, 2.6f, 10, c, false, true);
        for (int i = 0; i < 10; i++)
        {
            float a = i / 10f * Mathf.PI * 2f;
            b.Box(pos + new Vector3(Mathf.Cos(a) * 2.5f, h + 0.45f, Mathf.Sin(a) * 2.5f), new Vector3(0.7f, 0.9f, 0.7f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0), c * 0.95f);
        }
        b.Box(pos + new Vector3(0, h * 0.6f, 2.55f), new Vector3(0.5f, 1.2f, 0.2f), DarkWood);
    }

    public static void Lantern(MeshBuilder b, MeshBuilder glow, Vector3 pos, float h = 2.6f)
    {
        b.Cylinder(pos, pos + Vector3.up * h, 0.07f, 0.06f, 5, DarkWood);
        b.Box(pos + Vector3.up * (h + 0.05f), new Vector3(0.5f, 0.06f, 0.06f), DarkWood);
        glow.Box(pos + new Vector3(0.22f, h - 0.3f, 0), new Vector3(0.2f, 0.3f, 0.2f), GoldC);
    }

    public static void Cargo(MeshBuilder b, Vector3 pos, System.Random r)
    {
        int kind = r.Next(3);
        if (kind == 0) b.Box(pos + Vector3.up * 0.4f, Vector3.one * 0.8f, Quaternion.Euler(0, R(r, 0, 90), 0), Vary(WoodC, r, 0.15f));
        else if (kind == 1) b.Sphere(pos + Vector3.up * 0.35f, new Vector3(0.38f, 0.42f, 0.36f), 6, 5, new Color(0.62f, 0.52f, 0.36f));
        else
        {
            Color terr = Vary(new Color(0.62f, 0.33f, 0.2f), r, 0.1f);
            b.Sphere(pos + Vector3.up * 0.55f, new Vector3(0.32f, 0.45f, 0.32f), 8, 6, terr);
            b.Cylinder(pos + Vector3.up * 0.9f, pos + Vector3.up * 1.2f, 0.11f, 0.13f, 6, terr);
            b.Cylinder(pos + Vector3.up * 0.2f, pos, 0.12f, 0.02f, 6, terr);
        }
    }
}
