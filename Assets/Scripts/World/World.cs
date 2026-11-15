using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Builds the whole voyage procedurally and holds gameplay-relevant locations.</summary>
public static partial class World
{
    public static readonly List<Island> Islands = new List<Island>();
    public static readonly List<Vector4> Rocks = new List<Vector4>(); // x, z, radius, top height
    public static readonly Dictionary<string, Vector3> Points = new Dictionary<string, Vector3>();
    public static readonly List<Vector3> EchoStones = new List<Vector3>();
    public static readonly List<Vector3> Channel = new List<Vector3>();
    public static readonly List<GameObject> RouteMarkers = new List<GameObject>();
    public static readonly List<ParticleSystem> EchoMotes = new List<ParticleSystem>();
    public static Transform Root;
    public static float PortCoastX;
    public static Texture2D ChartTex;
    public static Rect ChartRect = new Rect(-2900, -1350, 3700, 2600);

    public static Vector3 P(string n) => Points.TryGetValue(n, out var v) ? v : Vector3.zero;

    public static float LandHeight(float x, float z)
    {
        float h = -60f;
        for (int i = 0; i < Islands.Count; i++)
        {
            var b = Islands[i].Bounds;
            if (x < b.xMin || x > b.xMax || z < b.yMin || z > b.yMax) continue;
            float v = Islands[i].Height(x, z);
            if (v > h) h = v;
        }
        return h;
    }

    public static Vector3 LandGradient(float x, float z)
    {
        float e = 2.5f;
        return new Vector3(LandHeight(x + e, z) - LandHeight(x - e, z), 0, LandHeight(x, z + e) - LandHeight(x, z - e)) / (2f * e);
    }

    static Vector3 Ground(float x, float z) => new Vector3(x, LandHeight(x, z), z);

    // ---------------------------------------------------------------- coasts
    public static float MalabarCoastX(float z) => 262f + 30f * (Geo.N(0, z, 0.0035f, 1) - 0.5f) * 2f + 42f * Mathf.Exp(-(z * z) / (160f * 160f));

    static float MalabarH(float x, float z)
    {
        float land = (x - MalabarCoastX(z)) / 380f;
        float riverZ = 115f + 28f * Mathf.Sin(x * 0.009f);
        float river = (Mathf.Abs(z - riverZ) - (16f + (x - 300f) * -0.01f)) / 380f;
        if (x > MalabarCoastX(z) - 20f) land = Mathf.Min(land, river);
        float h = Geo.Profile(land, 38f, x, z, 2f, 0.05f);
        float inland = x - MalabarCoastX(z);
        h += Geo.S01((inland - 420f) / 700f) * 210f * (0.55f + 0.7f * Geo.N(x, z, 0.0025f, 4));
        return h;
    }

    public static float ArabCoastX(float z) => -2385f + 28f * (Geo.N(0, z, 0.004f, 11) - 0.5f) * 2f - 26f * Mathf.Exp(-((z + 100f) * (z + 100f)) / (130f * 130f));

    static float ArabH(float x, float z)
    {
        float land = (ArabCoastX(z) - x) / 300f;
        float h = Geo.Profile(land, 30f, x, z, 12f, 0.06f);
        float inland = ArabCoastX(z) - x;
        h += Geo.S01((inland - 120f) / 160f) * 26f * Geo.N(x, z, 0.004f, 13);
        h += Geo.S01((inland - 380f) / 400f) * 90f * (0.5f + 0.6f * Geo.N(x, z, 0.003f, 14));
        return h;
    }

    static float EchoH(float x, float z)
    {
        float h = Geo.IslandH(x, z, new Vector2(-1500, -100), 250, 52, 5);
        float dx = x + 1500f, dz = z - 115f;
        float bay = Mathf.Exp(-(dx * dx + dz * dz) / (85f * 85f));
        return Mathf.Lerp(h, Mathf.Min(h, -7f), bay * 1.15f > 1 ? 1 : bay * 1.15f);
    }

    static void AddIsland(string name, Vector2 c, float r, float peak, float seed, Biome b, float cell = 5f)
    {
        var rect = new Rect(c.x - r * 1.75f, c.y - r * 1.75f, r * 3.5f, r * 3.5f);
        Islands.Add(new Island(name, rect, cell, b, seed, (x, z) => Geo.IslandH(x, z, c, r, peak, seed)));
    }

    // ---------------------------------------------------------------- build
    public static IEnumerator Build(Transform parent, System.Action<float, string> progress, int chapter = 1)
    {
        Root = new GameObject("World").transform;
        Root.SetParent(parent, false);
        Islands.Clear(); Rocks.Clear(); Points.Clear(); EchoStones.Clear(); Channel.Clear(); RouteMarkers.Clear(); EchoMotes.Clear();
        Beacons.Clear(); Patrols.Clear(); FogChannel.Clear();
        Tide = 0f; SandBars.Clear(); Canal.Clear();
        if (chapter == 2) { yield return BuildChapter2(progress); yield break; }
        if (chapter == 3) { yield return BuildChapter3(progress); yield break; }
        ChartRect = new Rect(-2900, -1350, 3700, 2600);

        progress(0.05f, "Raising the Malabar coast");
        yield return null;
        Islands.Add(new Island("Malabar", new Rect(110, -1350, 1500, 2700), 8f, Biome.Malabar, 2, MalabarH));
        yield return null;
        progress(0.12f, "Shaping the Arabian shore");
        Islands.Add(new Island("Arabia", new Rect(-3350, -1350, 1150, 2500), 8f, Biome.Arabian, 12, ArabH));
        yield return null;
        progress(0.18f, "Scattering islands");
        AddIsland("Cape", new Vector2(-380, 430), 75, 26, 3, Biome.Rocky, 4f);
        AddIsland("Kadal", new Vector2(-820, -230), 230, 78, 4, Biome.Tropical);
        Islands.Add(new Island("Echo", new Rect(-1500 - 440, -100 - 440, 880, 880), 5f, Biome.Tropical, 5, EchoH));
        AddIsland("Islet1", new Vector2(-150, -560), 85, 22, 6, Biome.Tropical);
        AddIsland("Islet2", new Vector2(-1180, 540), 110, 30, 7, Biome.Tropical);
        AddIsland("Islet3", new Vector2(-560, 800), 95, 25, 8, Biome.Rocky);
        AddIsland("Islet4", new Vector2(-2000, 760), 105, 28, 9, Biome.Arabian);
        AddIsland("Islet5", new Vector2(-1950, -950), 90, 20, 10, Biome.Rocky);
        AddIsland("Islet6", new Vector2(60, -1080), 80, 18, 15, Biome.Tropical);
        yield return null;

        foreach (var isl in Islands)
        {
            isl.BuildMesh(Root, Mats.Terrain);
            yield return null;
        }

        PortCoastX = MalabarCoastX(0);
        progress(0.3f, "Building Muziris");
        yield return null;
        BuildMuziris();
        yield return null;
        progress(0.42f, "Lighting the Cape of Lanterns");
        BuildCape();
        BuildKadal();
        yield return null;
        progress(0.5f, "Remembering the Isle of Echoes");
        BuildEcho();
        yield return null;
        progress(0.58f, "Sharpening the Shoals of Thorns");
        BuildShoals();
        yield return null;
        progress(0.64f, "Rebuilding Ras al-Mira");
        BuildOutpost();
        yield return null;
        BuildIslets();
        yield return null;
        progress(0.7f, "Drawing the chart");
        BuildChart();
        yield return null;
    }

    static MeshBuilder NB() => new MeshBuilder();

    static void Flush(string name, MeshBuilder solid, MeshBuilder fol, MeshBuilder glow, MeshBuilder stone = null)
    {
        solid?.SpawnSelf(name + "_solid", Mats.Lit, Root);
        fol?.SpawnSelf(name + "_foliage", Mats.Foliage, Root);
        glow?.SpawnSelf(name + "_glow", Mats.Lantern, Root, false, false);
        stone?.SpawnSelf(name + "_stone", Mats.Stone, Root);
    }

    static void ScatterPalms(MeshBuilder fol, System.Random r, int count, System.Func<Vector2> sampler, float minH, float maxH, List<Vector2> avoid = null, float avoidR = 0, bool date = false)
    {
        int placed = 0, tries = 0;
        while (placed < count && tries < count * 20)
        {
            tries++;
            Vector2 p = sampler();
            float h = LandHeight(p.x, p.y);
            if (h < minH || h > maxH) continue;
            if (LandGradient(p.x, p.y).magnitude > 0.55f) continue;
            if (avoid != null)
            {
                bool bad = false;
                foreach (var a in avoid) if ((a - p).sqrMagnitude < avoidR * avoidR) { bad = true; break; }
                if (bad) continue;
            }
            float ph = date ? (float)(8 + r.NextDouble() * 4) : (float)(10 + r.NextDouble() * 7);
            Props.Palm(fol, new Vector3(p.x, h - 0.2f, p.y), ph, (float)(0.05 + r.NextDouble() * 0.25), (float)(r.NextDouble() * 360), r, date);
            placed++;
        }
    }

    // ---------------------------------------------------------------- Muziris
    static void BuildMuziris()
    {
        var r = new System.Random(101);
        var solid = NB(); var fol = NB(); var glow = NB(); var wood = NB();
        float cx = PortCoastX;
        var avoid = new List<Vector2>();

        // Piers
        float pierLen = 52f;
        Props.Pier(wood, new Vector3(cx + 6f, 0, 0), -90f, pierLen, 5f, r);
        Props.Pier(wood, new Vector3(MalabarCoastX(-75) + 6f, 0, -75), -90f, 40f, 4f, r);
        Props.Pier(wood, new Vector3(MalabarCoastX(62) + 6f, 0, 62), -90f, 36f, 4f, r);
        float pierEndX = cx + 6f - pierLen;
        Points["dock_muziris"] = new Vector3(pierEndX + 8f, 0, -9.5f);
        Points["start"] = new Vector3(pierEndX + 6f, 0, -9.5f);
        Points["market"] = new Vector3(pierEndX + 14f, 1.4f, 0);
        for (float x = pierEndX + 3f; x < cx; x += 9f)
        {
            Props.Lantern(wood, glow, new Vector3(x, 1.45f, 2.3f));
            Props.Lantern(wood, glow, new Vector3(x + 4.5f, 1.45f, -2.3f));
        }
        for (int i = 0; i < 14; i++) Props.Cargo(solid, new Vector3(pierEndX + 6 + (float)r.NextDouble() * 30f, 1.45f, (float)(r.NextDouble() * 3 - 1.5)), r);

        // market along the waterfront
        Color[] awnings = { new Color(0.75f, 0.2f, 0.12f), new Color(0.9f, 0.65f, 0.15f), new Color(0.2f, 0.45f, 0.4f), new Color(0.85f, 0.82f, 0.7f), new Color(0.5f, 0.18f, 0.35f) };
        for (int i = 0; i < 10; i++)
        {
            float z = -38f + i * 8.2f;
            float x = MalabarCoastX(z) + 16f;
            Props.Stall(solid, Ground(x, z), 90f + (float)(r.NextDouble() * 20 - 10), awnings[r.Next(awnings.Length)], r);
            avoid.Add(new Vector2(x, z));
        }
        // people
        for (int i = 0; i < 26; i++)
        {
            Vector3 p;
            if (i < 9) p = new Vector3(pierEndX + 6 + (float)r.NextDouble() * 40f, 1.48f, (float)(r.NextDouble() * 3.4 - 1.7));
            else { float z = (float)(r.NextDouble() * 90 - 45); p = Ground(MalabarCoastX(z) + 8f + (float)r.NextDouble() * 18f, z); }
            Props.Person(solid, p, (float)(r.NextDouble() * 360), r, r.NextDouble() < 0.3);
        }

        // godowns
        for (int i = 0; i < 4; i++)
        {
            float z = -110f + i * 70f;
            if (Mathf.Abs(z) < 25f) z += 40f;
            float x = MalabarCoastX(z) + 32f;
            if (LandHeight(x, z) < 1.2f) continue;
            Props.Godown(solid, Ground(x, z), 90f, 14f, 8f, r);
            avoid.Add(new Vector2(x, z));
        }

        // houses
        int houses = 0, tries = 0;
        while (houses < 34 && tries < 600)
        {
            tries++;
            float z = (float)(r.NextDouble() * 640 - 320);
            float x = MalabarCoastX(z) + 50f + (float)r.NextDouble() * 230f;
            float h = LandHeight(x, z);
            if (h < 1.5f || h > 30f || LandGradient(x, z).magnitude > 0.25f) continue;
            bool bad = false;
            foreach (var a in avoid) if ((a - new Vector2(x, z)).sqrMagnitude < 18f * 18f) { bad = true; break; }
            if (bad) continue;
            float w = 6f + (float)r.NextDouble() * 5f, d = 5f + (float)r.NextDouble() * 3f;
            Props.KeralaHouse(solid, new Vector3(x, h - 0.6f, z), 90f + (float)(r.NextDouble() * 40 - 20), w, d, 2.6f + (float)r.NextDouble() * 0.6f, r, r.NextDouble() < 0.35);
            avoid.Add(new Vector2(x, z));
            houses++;
        }

        // temple
        {
            float z = -165f, x = MalabarCoastX(z) + 95f;
            Props.Temple(solid, glow, Ground(x, z) - Vector3.up * 0.4f, -90f);
            avoid.Add(new Vector2(x, z)); avoid.Add(new Vector2(x - 13f, z));
        }

        // palms (coastal band + inland)
        ScatterPalms(fol, r, 230, () =>
        {
            float z = (float)(r.NextDouble() * 2400 - 1200);
            return new Vector2(MalabarCoastX(z) + 4f + (float)(r.NextDouble() * r.NextDouble()) * 420f, z);
        }, 0.9f, 60f, avoid, 9f);
        for (int i = 0; i < 40; i++)
        {
            float z = (float)(r.NextDouble() * 1600 - 800);
            float x = MalabarCoastX(z) + 60f + (float)r.NextDouble() * 400f;
            float h = LandHeight(x, z);
            if (h > 2f) Props.Bush(fol, new Vector3(x, h, z), 1.5f + (float)r.NextDouble() * 2f, r, new Color(0.18f, 0.36f, 0.1f));
        }

        Flush("Muziris", solid, fol, glow);
        wood.SpawnSelf("Muziris_wood", Mats.Wood, Root);

        // docked merchant ships & small boats
        var m1 = ShipBuilder.Build(ShipBuilder.Style.Merchant, Root);
        m1.root.position = new Vector3(MalabarCoastX(-75) + 6f - 34f, 0, -82f);
        m1.root.rotation = Quaternion.Euler(0, -90, 0);
        m1.root.gameObject.AddComponent<Floater>().Init(-90f, 1.0f, 0.2f);
        var m2 = ShipBuilder.Build(ShipBuilder.Style.Merchant, Root);
        m2.root.position = new Vector3(MalabarCoastX(62) + 6f - 30f, 0, 69f);
        m2.root.rotation = Quaternion.Euler(0, -95, 0);
        m2.root.gameObject.AddComponent<Floater>().Init(-95f, 1.0f, 0.2f);
        m2.root.localScale = Vector3.one * 0.85f;
        m1.sail.localScale = new Vector3(1, 0.18f, 1);
        m2.sail.localScale = new Vector3(1, 0.18f, 1);
        for (int i = 0; i < 7; i++)
        {
            var bb = NB();
            Props.SmallBoat(bb, 6f + (float)r.NextDouble() * 2f, Props.Vary(Props.DarkWood, r, 0.2f));
            if (i % 2 == 0) Props.Person(bb, new Vector3(0, 0.1f, 0.5f), 0, r);
            var go = bb.SpawnSelf("Vallam", Mats.Wood, Root);
            float z = (float)(r.NextDouble() * 260 - 130);
            float x = MalabarCoastX(z) - 25f - (float)r.NextDouble() * 70f;
            if (Mathf.Abs(z) < 16) z += 30;
            go.transform.position = new Vector3(x, 0, z);
            go.AddComponent<Floater>().Init((float)(r.NextDouble() * 360), 0.3f, 0.02f);
        }
        Birds.Create(Root, new Vector3(cx - 20, 30, 0), 14, 70f);
    }

    // ---------------------------------------------------------------- Cape
    static void BuildCape()
    {
        var r = new System.Random(202);
        var stone = NB(); var glow = NB(); var fol = NB();
        Vector3 top = Ground(-380, 430);
        Props.Lighthouse(stone, glow, top - Vector3.up * 0.5f, 18f, Props.Sandstone, false, r);
        ScatterPalms(fol, r, 14, () => new Vector2(-380 + (float)(r.NextDouble() * 140 - 70), 430 + (float)(r.NextDouble() * 140 - 70)), 1f, 18f);
        for (int i = 0; i < 18; i++)
        {
            float a = (float)r.NextDouble() * Mathf.PI * 2f;
            float rad = 70f + (float)r.NextDouble() * 30f;
            Vector3 p = new Vector3(-380 + Mathf.Cos(a) * rad, 0, 430 + Mathf.Sin(a) * rad);
            p.y = Mathf.Max(LandHeight(p.x, p.z), -3f);
            stone.Rock(p, new Vector3(3, 2.5f, 3) * (0.7f + (float)r.NextDouble()), r.Next(), new Color(0.42f, 0.4f, 0.36f), new Color(0.3f, 0.28f, 0.26f));
        }
        Flush("Cape", null, fol, glow, stone);
        FX.LoopingFire(Root, top + Vector3.up * 19.2f, 2.2f);
        Points["cape"] = new Vector3(-330, 0, 320);
        Points["cape_light"] = top + Vector3.up * 19f;
    }

    // ---------------------------------------------------------------- Kadal
    static void BuildKadal()
    {
        var r = new System.Random(303);
        var fol = NB(); var solid = NB(); var stone = NB();
        Vector2 c = new Vector2(-820, -230);
        ScatterPalms(fol, r, 70, () => c + Random2(r) * 260f, 0.8f, 40f);
        for (int i = 0; i < 30; i++)
        {
            Vector2 p = c + Random2(r) * 230f;
            float h = LandHeight(p.x, p.y);
            if (h > 3f) Props.Bush(fol, new Vector3(p.x, h, p.y), 2f + (float)r.NextDouble() * 2.5f, r, new Color(0.14f, 0.32f, 0.09f));
        }
        // fishing hamlet on the south shore
        for (int i = 0; i < 4; i++)
        {
            float x = -860f + i * 22f;
            float z = FindCoastZ(x, -230f, -1f) + 18f;
            float h = LandHeight(x, z);
            if (h < 1f) continue;
            Props.KeralaHouse(solid, new Vector3(x, h - 0.6f, z), 180f + (float)(r.NextDouble() * 20 - 10), 5f, 4f, 2.2f, r, true);
        }
        for (int i = 0; i < 20; i++)
        {
            float a = (float)r.NextDouble() * Mathf.PI * 2f;
            Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (220f + (float)r.NextDouble() * 40f);
            float h = LandHeight(p.x, p.y);
            if (h > -4f && h < 4f) stone.Rock(new Vector3(p.x, h, p.y), new Vector3(3, 2, 3) * (0.6f + (float)r.NextDouble()), r.Next(), new Color(0.4f, 0.37f, 0.32f), new Color(0.28f, 0.26f, 0.24f));
        }
        Flush("Kadal", solid, fol, null, stone);
        Points["kadal_shelter"] = new Vector3(-830, 0, FindCoastZ(-830f, -230f, -1f) - 60f);
        Points["raider_spawn"] = new Vector3(-1250, 0, -760);
        Birds.Create(Root, new Vector3(-820, 45, -230), 8, 90f);
    }

    static Vector2 Random2(System.Random r)
    {
        float a = (float)r.NextDouble() * Mathf.PI * 2f;
        float d = Mathf.Sqrt((float)r.NextDouble());
        return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d;
    }

    /// <summary>Walk from a land point along z (dir sign) until reaching water.</summary>
    static float FindCoastZ(float x, float z0, float dir)
    {
        for (float z = z0; Mathf.Abs(z - z0) < 600f; z += dir * 2f)
            if (LandHeight(x, z) < 0.5f) return z;
        return z0;
    }

    static Vector3 FindCoast(Vector2 centre, float angDeg, float maxR)
    {
        Vector2 d = new Vector2(Mathf.Sin(angDeg * Mathf.Deg2Rad), Mathf.Cos(angDeg * Mathf.Deg2Rad));
        for (float rr = 20f; rr < maxR; rr += 2f)
        {
            Vector2 p = centre + d * rr;
            if (LandHeight(p.x, p.y) < 0.6f) return new Vector3(p.x, 0, p.y);
        }
        Vector2 q = centre + d * maxR;
        return new Vector3(q.x, 0, q.y);
    }

    // ---------------------------------------------------------------- Isle of Echoes
    static void BuildEcho()
    {
        var r = new System.Random(404);
        var fol = NB(); var stone = NB(); var ghost = NB(); var ghostTeal = NB();
        Vector2 c = new Vector2(-1500, -100);
        ScatterPalms(fol, r, 55, () => c + Random2(r) * 280f, 0.8f, 35f);
        for (int i = 0; i < 26; i++)
        {
            Vector2 p = c + Random2(r) * 240f;
            float h = LandHeight(p.x, p.y);
            if (h > 3f) Props.Bush(fol, new Vector3(p.x, h, p.y), 2f + (float)r.NextDouble() * 2f, r, new Color(0.16f, 0.33f, 0.1f));
        }
        Color ruin = new Color(0.66f, 0.62f, 0.54f);

        // ruined harbour south of the bay (present) + ghost harbour (past)
        Vector3 bayShore = new Vector3(-1500, 0, FindCoastZ(-1500, -60f, 1f));
        for (int i = 0; i < 9; i++)
        {
            float x = -1530 + i * 7.5f;
            float z = bayShore.z - 10f;
            Vector3 g = Ground(x, z);
            if (g.y < 0.5f) continue;
            Props.Column(stone, g, 7f, 0.55f, r.NextDouble() < 0.7, r, ruin);
            Props.Column(ghost, g, 7f, 0.55f, false, r, Color.white);
        }
        Props.RuinWall(stone, Ground(-1545, bayShore.z - 22f), Ground(-1455, bayShore.z - 22f), 3f, 1.2f, r, ruin);
        ghost.Box(new Vector3(-1500, LandHeight(-1500, bayShore.z - 22f) + 2.5f, bayShore.z - 22f), new Vector3(90, 5, 1.2f), Color.white);
        // ghost pier & moored ships
        Props.Pier(ghost, new Vector3(-1500, 0, bayShore.z - 4f), 0f, 55f, 6f, r);
        Points["echo_bay"] = new Vector3(-1500, 0, bayShore.z + 60f);

        // lighthouse on the east headland
        Vector3 lhPos = Ground(-1418, 125);
        if (lhPos.y < 2f) lhPos = Ground(-1430, 90);
        Props.Lighthouse(stone, null, lhPos - Vector3.up * 0.6f, 24f, ruin, true, r);
        Props.Lighthouse(ghost, ghost, lhPos - Vector3.up * 0.6f, 24f, Color.white, false, r);
        Points["echo_lighthouse"] = lhPos + Vector3.up * 26f;

        // echo stones
        float[] angs = { 120f, 215f, 300f };
        foreach (float a in angs)
        {
            Vector3 coast = FindCoast(c, a, 420f);
            Vector3 inward = (new Vector3(c.x, 0, c.y) - coast).normalized;
            Vector3 p = coast + inward * 12f;
            p.y = LandHeight(p.x, p.z);
            float yaw = Mathf.Atan2(-inward.x, -inward.z) * Mathf.Rad2Deg;
            Props.Stele(stone, p - Vector3.up * 0.3f, yaw);
            ghostTeal.M = Matrix4x4.TRS(p - Vector3.up * 0.3f, Quaternion.Euler(0, yaw, 0), Vector3.one * 1.04f);
            ghostTeal.Box(new Vector3(0, 2.4f, 0), new Vector3(1.6f, 3.9f, 0.6f), Color.white);
            ghostTeal.Cylinder(new Vector3(0, 0, 0), new Vector3(0, 40f, 0), 0.6f, 0.05f, 8, Color.white);
            ghostTeal.M = Matrix4x4.identity;
            EchoStones.Add(p);
            for (int k = 0; k < 4; k++)
            {
                Vector3 cp = p + Quaternion.Euler(0, k * 90 + 45, 0) * Vector3.forward * 4.5f;
                Props.Column(stone, Ground(cp.x, cp.z), 4f, 0.4f, true, r, ruin);
            }
            EchoMotes.Add(FX.Motes(Root, p + Vector3.up * 3f));
        }

        stone.SpawnSelf("Echo_stone", Mats.Stone, Root);
        fol.SpawnSelf("Echo_foliage", Mats.Foliage, Root);
        ghost.SpawnSelf("Echo_ghost", Mats.GhostGold, Root, false, false);
        ghostTeal.SpawnSelf("Echo_ghostStones", Mats.GhostTeal, Root, false, false);

        // ghost ships moored in the bay
        for (int i = 0; i < 2; i++)
        {
            var gs = ShipBuilder.Build(ShipBuilder.Style.Ghost, Root, Mats.GhostGold);
            gs.root.position = new Vector3(-1500 + (i == 0 ? -14f : 14f), 0, bayShore.z + 30f + i * 8f);
            gs.root.rotation = Quaternion.Euler(0, i == 0 ? 5 : -8, 0);
            gs.root.gameObject.AddComponent<Floater>().Init(i == 0 ? 5 : -8, 1f);
        }
        Points["echo_arrive"] = new Vector3(-1250, 0, -40);
        Birds.Create(Root, new Vector3(-1500, 50, -100), 8, 110f);
    }

    // ---------------------------------------------------------------- Shoals of Thorns
    static void BuildShoals()
    {
        var r = new System.Random(505);
        float coastAtDock = ArabCoastX(-100f);
        Channel.Add(new Vector3(-1785, 0, -95));
        Channel.Add(new Vector3(-1880, 0, -235));
        Channel.Add(new Vector3(-1990, 0, -70));
        Channel.Add(new Vector3(-2090, 0, -215));
        Channel.Add(new Vector3(-2185, 0, -95));
        Channel.Add(new Vector3(coastAtDock + 70f, 0, -100));

        var stone = NB();
        int placed = 0;
        for (int i = 0; i < 2600 && placed < 230; i++)
        {
            float x = -2250f + (float)r.NextDouble() * 440f;
            float z = -560f + (float)r.NextDouble() * 900f;
            float dc = DistToChannel(x, z);
            if (dc < 32f) continue;
            if (LandHeight(x, z) > -3f) continue;
            float rad = 3f + (float)r.NextDouble() * 6f;
            bool submerged = r.NextDouble() < 0.45;
            float top = submerged ? -0.7f - (float)r.NextDouble() * 0.5f : 1f + (float)r.NextDouble() * 6f;
            bool tooClose = false;
            foreach (var k in Rocks) if ((new Vector2(k.x, k.y) - new Vector2(x, z)).sqrMagnitude < (k.z + rad) * (k.z + rad) * 0.6f) { tooClose = true; break; }
            if (tooClose) continue;
            Rocks.Add(new Vector4(x, z, rad, top));
            float hgt = top + 8f;
            stone.Rock(new Vector3(x, top - hgt * 0.5f, z), new Vector3(rad, hgt * 0.5f + 0.5f, rad * (0.8f + (float)r.NextDouble() * 0.4f)), r.Next(), new Color(0.3f, 0.29f, 0.27f), new Color(0.18f, 0.17f, 0.16f));
            placed++;
        }
        stone.SpawnSelf("Shoals", Mats.Stone, Root);

        // ghost route lamps (visible in the past; faint after revelation)
        var route = NB();
        float acc = 0;
        for (int s = 0; s < Channel.Count - 1; s++)
        {
            Vector3 a = Channel[s], b = Channel[s + 1];
            float len = Vector3.Distance(a, b);
            for (float t = acc; t < len; t += 34f)
            {
                Vector3 p = Vector3.Lerp(a, b, t / len);
                Vector3 side = Vector3.Cross(Vector3.up, (b - a).normalized);
                for (int k = -1; k <= 1; k += 2)
                {
                    Vector3 q = p + side * k * 20f;
                    route.Cylinder(q + Vector3.down * 2f, q + Vector3.up * 2.2f, 0.35f, 0.3f, 6, Color.white);
                    route.Sphere(q + Vector3.up * 2.6f, Vector3.one * 0.55f, 8, 6, Color.white);
                    route.Cylinder(q + Vector3.up * 2.6f, q + Vector3.up * 30f, 0.25f, 0.02f, 6, Color.white);
                }
            }
            acc = 0;
        }
        var rgo = route.SpawnSelf("RouteLamps", Mats.GhostRoute, Root, false, false);
        RouteMarkers.Add(rgo);
        Points["shoals_entry"] = Channel[0];
    }

    public static float DistToChannel(float x, float z)
    {
        float best = 1e9f;
        Vector2 p = new Vector2(x, z);
        for (int i = 0; i < Channel.Count - 1; i++)
        {
            Vector2 a = new Vector2(Channel[i].x, Channel[i].z), b = new Vector2(Channel[i + 1].x, Channel[i + 1].z);
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            best = Mathf.Min(best, (a + ab * t - p).magnitude);
        }
        return best;
    }

    // ---------------------------------------------------------------- Ras al-Mira
    static void BuildOutpost()
    {
        var r = new System.Random(606);
        var solid = NB(); var fol = NB(); var glow = NB(); var wood = NB(); var ghost = NB();
        float cz = -100f;
        float cx = ArabCoastX(cz);
        Props.Pier(wood, new Vector3(cx - 6f, 0, cz), 90f, 46f, 5f, r);
        float end = cx - 6f + 46f;
        Points["dock_outpost"] = new Vector3(end - 6f, 0, cz - 9.5f);
        Points["outpost_market"] = new Vector3(end - 14f, 1.4f, cz);
        for (float x = cx; x < end - 3f; x += 10f) Props.Lantern(wood, glow, new Vector3(x, 1.45f, cz + 2.3f));
        for (int i = 0; i < 8; i++) Props.Cargo(solid, new Vector3(cx + 4f + (float)r.NextDouble() * 30f, 1.45f, cz + (float)(r.NextDouble() * 3 - 1.5)), r);

        var avoid = new List<Vector2>();
        for (int i = 0; i < 16; i++)
        {
            float z = cz + (float)(r.NextDouble() * 200 - 100);
            float x = ArabCoastX(z) - 25f - (float)r.NextDouble() * 110f;
            float h = LandHeight(x, z);
            if (h < 1.2f || LandGradient(x, z).magnitude > 0.3f) continue;
            bool bad = false;
            foreach (var a in avoid) if ((a - new Vector2(x, z)).sqrMagnitude < 14f * 14f) { bad = true; break; }
            if (bad) continue;
            float w = 6f + (float)r.NextDouble() * 6f, d = 5f + (float)r.NextDouble() * 4f, hh = 3.2f + (float)r.NextDouble() * 2.5f;
            bool ruined = r.NextDouble() < 0.45;
            float yaw = 90f + (float)(r.NextDouble() * 20 - 10);
            Props.MudHouse(solid, new Vector3(x, h, z), yaw, w, d, hh, ruined, r);
            if (ruined) Props.MudHouse(ghost, new Vector3(x, h, z), yaw, w, d, hh, false, r);
            avoid.Add(new Vector2(x, z));
        }
        Vector3 tower = Ground(cx - 40f, cz + 55f);
        Props.Watchtower(solid, tower, 15f, r);
        Props.Lantern(solid, glow, tower + Vector3.up * 15.5f, 1.2f);
        ScatterPalms(fol, r, 30, () => new Vector2(cx - 10f - (float)r.NextDouble() * 160f, cz + (float)(r.NextDouble() * 300 - 150)), 0.8f, 20f, avoid, 7f, true);
        // old Yusuf and a few ghost traders
        Props.Person(solid, new Vector3(end - 16f, 1.48f, cz + 1f), 90f, r, true, new Color(0.35f, 0.28f, 0.45f));
        for (int i = 0; i < 18; i++)
        {
            Vector3 p = i < 6 ? new Vector3(cx + (float)r.NextDouble() * 36f, 1.48f, cz + (float)(r.NextDouble() * 3 - 1.5)) : Ground(cx - 10f - (float)r.NextDouble() * 60f, cz + (float)(r.NextDouble() * 80 - 40));
            Props.Person(ghost, p, (float)(r.NextDouble() * 360), r, r.NextDouble() < 0.6);
        }
        Flush("Outpost", solid, fol, glow);
        wood.SpawnSelf("Outpost_wood", Mats.Wood, Root);
        ghost.SpawnSelf("Outpost_ghost", Mats.GhostGold, Root, false, false);
        var gs = ShipBuilder.Build(ShipBuilder.Style.Ghost, Root, Mats.GhostGold);
        gs.root.position = new Vector3(end - 4f, 0, cz + 12f);
        gs.root.rotation = Quaternion.Euler(0, 92, 0);
        gs.root.gameObject.AddComponent<Floater>().Init(92, 1f);
        Points["outpost_approach"] = new Vector3(end + 60f, 0, cz);
        Birds.Create(Root, new Vector3(cx - 30, 40, cz), 6, 80f);
    }

    static void BuildIslets()
    {
        var r = new System.Random(707);
        var fol = NB(); var stone = NB();
        foreach (var isl in Islands)
        {
            if (!isl.Name.StartsWith("Islet")) continue;
            Vector2 c = isl.Bounds.center;
            float rad = isl.Bounds.width / 3.5f;
            bool desert = isl.Biome == Biome.Arabian;
            if (isl.Biome != Biome.Rocky || r.NextDouble() < 0.5)
                ScatterPalms(fol, r, desert ? 8 : 18, () => c + Random2(r) * rad * 1.1f, 0.8f, 25f, null, 0, desert);
            for (int i = 0; i < 10; i++)
            {
                float a = (float)r.NextDouble() * Mathf.PI * 2f;
                Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad * (0.9f + (float)r.NextDouble() * 0.3f);
                float h = LandHeight(p.x, p.y);
                if (h > -4f && h < 5f) stone.Rock(new Vector3(p.x, h, p.y), new Vector3(2.5f, 2f, 2.5f) * (0.6f + (float)r.NextDouble()), r.Next(), new Color(0.4f, 0.37f, 0.32f), new Color(0.27f, 0.25f, 0.23f));
            }
        }
        fol.SpawnSelf("Islets_foliage", Mats.Foliage, Root);
        stone.SpawnSelf("Islets_stone", Mats.Stone, Root);
    }

    static void BuildChart()
    {
        int W = 370, H = 260;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false, false);
        var px = new Color[W * H];
        var land = new float[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float wx = ChartRect.xMin + (x + 0.5f) / W * ChartRect.width;
                float wz = ChartRect.yMin + (y + 0.5f) / H * ChartRect.height;
                land[y * W + x] = LandHeight(wx, wz);
            }
        Color sea = new Color(0.78f, 0.7f, 0.52f), deep = new Color(0.68f, 0.6f, 0.44f), landC = new Color(0.62f, 0.5f, 0.32f), ink = new Color(0.25f, 0.17f, 0.1f);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float h = land[y * W + x];
                bool isLand = h > 0.3f;
                bool edge = false;
                if (isLand)
                    for (int k = 0; k < 4 && !edge; k++)
                    {
                        int xx = x + (k == 0 ? 1 : k == 1 ? -1 : 0), yy = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                        if (xx < 0 || yy < 0 || xx >= W || yy >= H) continue;
                        if (land[yy * W + xx] <= 0.3f) edge = true;
                    }
                Color c = isLand ? Color.Lerp(landC, landC * 0.75f, Mathf.Clamp01(h / 80f)) : Color.Lerp(sea, deep, Mathf.Clamp01(-h / 30f));
                if (!isLand && h > -6f) c = Color.Lerp(c, ink, 0.12f);
                if (edge) c = ink;
                float n = Mathf.PerlinNoise(x * 0.07f, y * 0.07f);
                c *= 0.92f + 0.12f * n;
                c.a = 1;
                px[y * W + x] = c;
            }
        tex.SetPixels(px);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply(false, true);
        ChartTex = tex;
    }
}

/// <summary>Bobs a static object on the waves.</summary>
public class Floater : MonoBehaviour
{
    float yaw, len, depth;
    public void Init(float yawDeg, float lengthScale, float draft = 0.4f) { yaw = yawDeg; len = 9f * lengthScale; depth = draft; }
    void Update()
    {
        Vector3 p = transform.position;
        Vector3 f = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
        Vector3 rgt = new Vector3(f.z, 0, -f.x);
        float hb = WaveField.HeightAt(p + f * len), hs = WaveField.HeightAt(p - f * len);
        float hp = WaveField.HeightAt(p - rgt * 2.5f), hr = WaveField.HeightAt(p + rgt * 2.5f);
        float y = (hb + hs + hp + hr) * 0.25f - depth;
        transform.position = new Vector3(p.x, Mathf.Lerp(p.y, y, Time.deltaTime * 3f), p.z);
        float pitch = Mathf.Atan2(hb - hs, len * 2f) * Mathf.Rad2Deg;
        float roll = Mathf.Atan2(hr - hp, 5f) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(-pitch, yaw, roll), Time.deltaTime * 3f);
    }
}

/// <summary>Seabirds circling a point.</summary>
public class Birds : MonoBehaviour
{
    struct Bird { public Transform t, wl, wr; public float r, h, speed, phase, flap; }
    Bird[] birds;
    Vector3 centre;

    public static void Create(Transform parent, Vector3 centre, int count, float radius)
    {
        var go = new GameObject("Birds");
        go.transform.SetParent(parent, false);
        var b = go.AddComponent<Birds>();
        b.centre = centre;
        var body = new MeshBuilder();
        body.Sphere(Vector3.zero, new Vector3(0.12f, 0.1f, 0.35f), 6, 4, new Color(0.95f, 0.95f, 0.93f));
        var bodyMesh = body.Build("bird");
        var wing = new MeshBuilder();
        wing.Card(new Vector3(0, 0, 0.12f), new Vector3(0.75f, 0.05f, 0.05f), new Vector3(0.75f, 0.05f, -0.12f), new Vector3(0, 0, -0.15f), new Color(0.9f, 0.9f, 0.88f));
        var wingMesh = wing.Build("wing");
        var rnd = new System.Random(count * 31 + (int)centre.x);
        b.birds = new Bird[count];
        for (int i = 0; i < count; i++)
        {
            var bt = MeshBuilder.Spawn("bird", bodyMesh, Mats.Lit, go.transform, false, false).transform;
            var wl = MeshBuilder.Spawn("wl", wingMesh, Mats.Lit, bt, false, false).transform;
            var wr = MeshBuilder.Spawn("wr", wingMesh, Mats.Lit, bt, false, false).transform;
            wr.localScale = new Vector3(-1, 1, 1);
            bt.localScale = Vector3.one * 1.6f;
            b.birds[i] = new Bird { t = bt, wl = wl, wr = wr, r = radius * (0.4f + (float)rnd.NextDouble() * 0.8f), h = (float)rnd.NextDouble() * 25f, speed = 0.12f + (float)rnd.NextDouble() * 0.1f, phase = (float)rnd.NextDouble() * 10f, flap = 5f + (float)rnd.NextDouble() * 3f };
        }
    }

    void Update()
    {
        float t = Time.time;
        for (int i = 0; i < birds.Length; i++)
        {
            var b = birds[i];
            float a = t * b.speed * (i % 2 == 0 ? 1 : -1) + b.phase;
            Vector3 p = centre + new Vector3(Mathf.Cos(a) * b.r, b.h + Mathf.Sin(t * 0.3f + b.phase) * 4f, Mathf.Sin(a) * b.r * 0.7f);
            Vector3 tan = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a) * 0.7f) * (i % 2 == 0 ? 1 : -1);
            b.t.position = p;
            b.t.rotation = Quaternion.LookRotation(tan) * Quaternion.Euler(0, 0, (i % 2 == 0 ? -1 : 1) * 15f);
            float fl = Mathf.Sin(t * b.flap + b.phase) * 35f * (0.5f + 0.5f * Mathf.Sin(t * 0.7f + b.phase));
            b.wl.localRotation = Quaternion.Euler(0, 0, fl);
            b.wr.localRotation = Quaternion.Euler(0, 0, -fl);
        }
    }
}
