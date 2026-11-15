using System;
using UnityEngine;

public enum Biome { Malabar, Tropical, Rocky, Arabian }

/// <summary>A heightfield landmass with CPU height queries for collision.</summary>
public class Island
{
    public string Name;
    public Rect Bounds;
    public float Cell;
    public int NX, NZ;
    public float[] H;
    public Biome Biome;
    public float Seed;

    public Island(string name, Rect bounds, float cell, Biome biome, float seed, Func<float, float, float> fn)
    {
        Name = name; Bounds = bounds; Cell = cell; Biome = biome; Seed = seed;
        NX = Mathf.CeilToInt(bounds.width / cell);
        NZ = Mathf.CeilToInt(bounds.height / cell);
        H = new float[(NX + 1) * (NZ + 1)];
        for (int j = 0; j <= NZ; j++)
            for (int i = 0; i <= NX; i++)
                H[j * (NX + 1) + i] = fn(bounds.xMin + i * cell, bounds.yMin + j * cell);
    }

    public float Height(float x, float z)
    {
        if (x < Bounds.xMin || x > Bounds.xMax || z < Bounds.yMin || z > Bounds.yMax) return -60f;
        float fx = (x - Bounds.xMin) / Cell, fz = (z - Bounds.yMin) / Cell;
        int ix = Mathf.Clamp((int)fx, 0, NX - 1), iz = Mathf.Clamp((int)fz, 0, NZ - 1);
        float tx = fx - ix, tz = fz - iz;
        int w = NX + 1;
        float a = H[iz * w + ix], b = H[iz * w + ix + 1], c = H[(iz + 1) * w + ix], d = H[(iz + 1) * w + ix + 1];
        return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), tz);
    }

    Color Paint(float h, float slope, float n, float n2)
    {
        Color sandWet, sand, grass, jungle, rock, under;
        switch (Biome)
        {
            case Biome.Arabian:
                under = new Color(0.62f, 0.52f, 0.36f); sandWet = new Color(0.66f, 0.52f, 0.34f); sand = new Color(0.84f, 0.7f, 0.48f);
                grass = new Color(0.72f, 0.56f, 0.36f); jungle = new Color(0.55f, 0.5f, 0.3f); rock = new Color(0.62f, 0.44f, 0.3f);
                break;
            case Biome.Rocky:
                under = new Color(0.5f, 0.48f, 0.4f); sandWet = new Color(0.55f, 0.5f, 0.42f); sand = new Color(0.72f, 0.66f, 0.54f);
                grass = new Color(0.3f, 0.4f, 0.18f); jungle = new Color(0.24f, 0.32f, 0.15f); rock = new Color(0.42f, 0.4f, 0.37f);
                break;
            default:
                under = new Color(0.66f, 0.6f, 0.44f); sandWet = new Color(0.66f, 0.58f, 0.42f); sand = new Color(0.88f, 0.8f, 0.6f);
                grass = new Color(0.27f, 0.45f, 0.13f); jungle = new Color(0.12f, 0.28f, 0.08f); rock = new Color(0.4f, 0.35f, 0.29f);
                if (Biome == Biome.Malabar) { grass = new Color(0.25f, 0.44f, 0.12f); rock = new Color(0.52f, 0.3f, 0.2f); }
                break;
        }
        Color c;
        if (h < -1.2f) c = Color.Lerp(under, under * 0.55f, Mathf.Clamp01(-h / 22f));
        else if (h < 0.4f) c = sandWet;
        else if (h < 2.4f + n * 1.5f) c = Color.Lerp(sandWet, sand, Mathf.Clamp01((h - 0.4f) / 0.8f));
        else
        {
            float gj = Mathf.Clamp01((n2 - 0.35f) * 2.5f + (h - 15f) / 50f);
            c = Color.Lerp(grass, jungle, gj);
            if (Biome == Biome.Arabian) c = Color.Lerp(sand, grass, Mathf.Clamp01((h - 4f) / 20f) * 0.6f + n2 * 0.4f);
        }
        float rk = Mathf.Clamp01((slope - 0.55f) * 3.5f);
        if (h > 1.5f) c = Color.Lerp(c, rock * (0.85f + n * 0.3f), rk);
        c.a = 1;
        return c;
    }

    public GameObject BuildMesh(Transform parent, Material mat)
    {
        var mb = new MeshBuilder();
        int w = NX + 1;
        int[] idx = new int[(NX + 1) * (NZ + 1)];
        for (int i = 0; i < idx.Length; i++) idx[i] = -1;
        bool[] use = new bool[idx.Length];
        for (int j = 0; j < NZ; j++)
            for (int i = 0; i < NX; i++)
            {
                float mx = Mathf.Max(Mathf.Max(H[j * w + i], H[j * w + i + 1]), Mathf.Max(H[(j + 1) * w + i], H[(j + 1) * w + i + 1]));
                if (mx > -26f) { use[j * w + i] = use[j * w + i + 1] = use[(j + 1) * w + i] = use[(j + 1) * w + i + 1] = true; }
            }
        for (int j = 0; j <= NZ; j++)
            for (int i = 0; i <= NX; i++)
            {
                int k = j * w + i;
                if (!use[k]) continue;
                float x = Bounds.xMin + i * Cell, z = Bounds.yMin + j * Cell;
                float hL = H[j * w + Mathf.Max(0, i - 1)], hR = H[j * w + Mathf.Min(NX, i + 1)];
                float hD = H[Mathf.Max(0, j - 1) * w + i], hU = H[Mathf.Min(NZ, j + 1) * w + i];
                Vector3 n = new Vector3(hL - hR, 2f * Cell, hD - hU).normalized;
                float slope = 1f - n.y;
                float nz1 = Mathf.PerlinNoise(x * 0.05f + Seed * 3f, z * 0.05f);
                float nz2 = Mathf.PerlinNoise(x * 0.012f + Seed * 5f, z * 0.012f + 9f);
                idx[k] = mb.Vert(new Vector3(x, H[k], z), n, Paint(H[k], slope * 3f, nz1, nz2));
            }
        for (int j = 0; j < NZ; j++)
            for (int i = 0; i < NX; i++)
            {
                int a = idx[j * w + i], b = idx[j * w + i + 1], c = idx[(j + 1) * w + i], d = idx[(j + 1) * w + i + 1];
                if (a < 0 || b < 0 || c < 0 || d < 0) continue;
                float mx = Mathf.Max(Mathf.Max(H[j * w + i], H[j * w + i + 1]), Mathf.Max(H[(j + 1) * w + i], H[(j + 1) * w + i + 1]));
                if (mx <= -26f) continue;
                // clockwise from above: a(i,j) -> c(i,j+1) -> d(i+1,j+1)
                mb.Tri(a, c, d);
                mb.Tri(a, d, b);
            }
        return mb.SpawnSelf(Name, mat, parent, true, true);
    }

    public Vector3 Gradient(float x, float z)
    {
        float e = 2f;
        return new Vector3(Height(x + e, z) - Height(x - e, z), 0, Height(x, z + e) - Height(x, z - e)) / (2f * e);
    }
}

public static class Geo
{
    public static float S01(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
    public static float N(float x, float z, float f, float seed) => Mathf.PerlinNoise(x * f + seed * 13.1f + 500f, z * f + seed * 7.3f + 500f);

    public static float Profile(float land, float peak, float x, float z, float seed, float beachW = 0.07f)
    {
        float shelf = Mathf.Lerp(-34f, -0.6f, S01((land + 0.45f) / 0.45f));
        float beach = S01(land / beachW) * 2.0f;
        float hill = Mathf.Pow(S01((land - beachW) / (1f - beachW)), 1.25f) * peak * (0.55f + 0.9f * N(x, z, 0.011f, seed + 9));
        float rough = (N(x, z, 0.05f, seed + 3) - 0.5f) * 3f * S01(land / 0.15f);
        return shelf + beach + hill + rough;
    }

    public static float IslandH(float x, float z, Vector2 c, float radius, float peak, float seed)
    {
        float dx = x - c.x, dz = z - c.y;
        float d = Mathf.Sqrt(dx * dx + dz * dz) / radius;
        float warp = (N(x, z, 0.006f, seed) - 0.5f) * 0.55f + (N(x, z, 0.02f, seed + 5) - 0.5f) * 0.15f;
        float land = 1f - (d + warp);
        return Profile(land, peak, x, z, seed);
    }
}
