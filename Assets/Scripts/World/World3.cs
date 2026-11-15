using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Chapter III — The Great Marketplace: the Red Sea, the Canal of the Pharaohs and Alexandria.</summary>
public static partial class World
{
    public static float Tide;                          // metres above mean sea level (Chapter III)
    public static readonly List<Vector3> SandBars = new List<Vector3>();
    public static readonly List<Vector3> Canal = new List<Vector3>();
    public static GameObject CanalGhosts;

    static readonly Vector2 PharosC = new Vector2(-1250, 1700);
    static readonly Vector2 LakeC = new Vector2(-330, 960);

    static float RedWestX(float z) => -420f + 40f * (Geo.N(0, z, 0.004f, 61) - 0.5f) * 2f;
    static float RedEastX(float z) => 620f + 40f * (Geo.N(0, z, 0.004f, 62) - 0.5f) * 2f;
    static float RedHeadZ(float x) => 480f + 30f * (Geo.N(x, 0, 0.006f, 63) - 0.5f) * 2f;
    public static float MedCoastZ(float x) => 1450f + 40f * (Geo.N(x, 0, 0.004f, 64) - 0.5f) * 2f;

    static float SegDist(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return (a + ab * t - p).magnitude;
    }

    static float WaterSdf3(float x, float z)
    {
        float red = Mathf.Max(Mathf.Max(RedWestX(z) - x, x - RedEastX(z)), z - RedHeadZ(x));
        Vector2 p = new Vector2(x, z);
        float canal = 1e9f;
        for (int i = 0; i < Canal.Count - 1; i++)
            canal = Mathf.Min(canal, SegDist(p, new Vector2(Canal[i].x, Canal[i].z), new Vector2(Canal[i + 1].x, Canal[i + 1].z)));
        canal -= 36f;
        float lake = (p - LakeC).magnitude - 150f;
        float med = MedCoastZ(x) - z;
        return Mathf.Min(Mathf.Min(red, canal), Mathf.Min(lake, med));
    }

    static float EgyptH(float x, float z)
    {
        float d = WaterSdf3(x, z);
        float land = d / 280f;
        float h = Geo.Profile(land, 20f, x, z, 65f, 0.05f);
        h += Geo.S01((d - 200f) / 600f) * 60f * (0.4f + 0.8f * Geo.N(x, z, 0.004f, 66));
        foreach (var b in SandBars)
        {
            float bd = new Vector2(x - b.x, z - b.z).magnitude;
            float k = Mathf.Exp(-(bd / 26f) * (bd / 26f));
            if (h < -0.3f) h = Mathf.Lerp(h, -0.3f, k);
        }
        return h;
    }

    static IEnumerator BuildChapter3(System.Action<float, string> progress)
    {
        ChartRect = new Rect(-2300, -1100, 3700, 3500);
        Canal.Clear(); SandBars.Clear();
        Canal.Add(new Vector3(60, 0, 420));
        Canal.Add(new Vector3(20, 0, 640));
        Canal.Add(new Vector3(-180, 0, 820));
        Canal.Add(new Vector3(LakeC.x, 0, LakeC.y));
        Canal.Add(new Vector3(-520, 0, 1120));
        Canal.Add(new Vector3(-760, 0, 1290));
        Canal.Add(new Vector3(-950, 0, 1520));
        SandBars.Add(Vector3.Lerp(Canal[1], Canal[2], 0.55f));
        SandBars.Add(Vector3.Lerp(Canal[4], Canal[5], 0.5f));

        progress(0.05f, "Raising the deserts of Egypt");
        yield return null;
        Islands.Add(new Island("Egypt", new Rect(-2300, -1100, 3700, 2750), 10f, Biome.Arabian, 65, EgyptH));
        yield return null;
        progress(0.2f, "Raising the isle of Pharos");
        Islands.Add(new Island("Pharos", new Rect(PharosC.x - 200, PharosC.y - 200, 400, 400), 4f, Biome.Rocky, 67, (x, z) => Geo.IslandH(x, z, PharosC, 80f, 12f, 67f)));
        yield return null;
        foreach (var isl in Islands) { isl.BuildMesh(Root, Mats.Terrain); yield return null; }

        progress(0.35f, "Building Berenike");
        BuildBerenike();
        yield return null;
        progress(0.45f, "Digging the Canal of the Pharaohs");
        BuildCanal();
        yield return null;
        progress(0.55f, "Raising Alexandria");
        BuildAlexandria();
        yield return null;
        progress(0.7f, "Drawing the chart");
        BuildChart();
        yield return null;
    }

    static void BuildBerenike()
    {
        var r = new System.Random(1001);
        var solid = NB(); var fol = NB(); var glow = NB(); var wood = NB();
        float cz = -620f, cx = RedWestX(cz);
        Props.Pier(wood, new Vector3(cx - 6f, 0, cz), 90f, 46f, 5f, r);
        float end = cx - 6f + 46f;
        Points["start"] = new Vector3(end - 6f, 0, cz - 9.5f);
        Points["dock_berenike"] = Points["start"];
        for (float x = cx; x < end - 3f; x += 9f) Props.Lantern(wood, glow, new Vector3(x, 1.45f, cz + 2.3f));
        for (int i = 0; i < 8; i++) Props.Cargo(solid, new Vector3(cx + 3f + (float)r.NextDouble() * 32f, 1.45f, cz + (float)(r.NextDouble() * 3 - 1.5)), r);
        var avoid = new List<Vector2>();
        for (int i = 0; i < 80 && avoid.Count < 14; i++)
        {
            float z = cz + (float)(r.NextDouble() * 220 - 110);
            float x = RedWestX(z) - 20f - (float)r.NextDouble() * 110f;
            float h = LandHeight(x, z);
            if (h < 1.2f || LandGradient(x, z).magnitude > 0.3f) continue;
            bool bad = false;
            foreach (var a in avoid) if ((a - new Vector2(x, z)).sqrMagnitude < 14f * 14f) { bad = true; break; }
            if (bad) continue;
            Props.MudHouse(solid, new Vector3(x, h, z), 90f, 6f + (float)r.NextDouble() * 5f, 5f + (float)r.NextDouble() * 4f, 3.4f + (float)r.NextDouble() * 2f, r.NextDouble() < 0.25, r);
            avoid.Add(new Vector2(x, z));
        }
        ScatterPalms(fol, r, 22, () => new Vector2(RedWestX(cz) - 5f - (float)r.NextDouble() * 140f, cz + (float)(r.NextDouble() * 260 - 130)), 0.8f, 20f, avoid, 7f, true);
        for (int i = 0; i < 6; i++) Props.Person(solid, new Vector3(cx + (float)r.NextDouble() * 36f, 1.48f, cz + (float)(r.NextDouble() * 3 - 1.5)), (float)r.NextDouble() * 360f, r, r.NextDouble() < 0.5);
        Flush("Berenike", solid, fol, glow);
        wood.SpawnSelf("Berenike_wood", Mats.Wood, Root);
        Birds.Create(Root, new Vector3(cx, 40, cz), 6, 80f);
    }

    static void BuildCanal()
    {
        var r = new System.Random(1002);
        var stone = NB(); var ghost = NB(); var fol = NB();
        Color lime = new Color(0.82f, 0.76f, 0.62f);
        float acc = 0;
        for (int s = 0; s < Canal.Count - 1; s++)
        {
            Vector3 a = Canal[s], b = Canal[s + 1];
            float len = Vector3.Distance(a, b);
            Vector3 dir = (b - a).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            for (float t = 30f; t < len; t += 85f)
            {
                Vector3 p = Vector3.Lerp(a, b, t / len);
                if ((new Vector2(p.x, p.z) - LakeC).magnitude < 170f) continue;
                for (int k = -1; k <= 1; k += 2)
                {
                    Vector3 q = p + side * k * 50f;
                    q.y = LandHeight(q.x, q.z);
                    if (q.y < 0.5f) continue;
                    // present: a toppled, half-buried obelisk; past: standing obelisk
                    stone.Box(q + new Vector3(0, 0.6f, 0), new Vector3(1.6f, 1.2f, 7f), Quaternion.LookRotation(dir) * Quaternion.Euler(0, 20f * k, 6f), lime * 0.9f);
                    ghost.M = Matrix4x4.TRS(q, Quaternion.LookRotation(dir), Vector3.one);
                    ghost.Box(new Vector3(0, 0.6f, 0), new Vector3(3f, 1.2f, 3f), Color.white);
                    ghost.Box(new Vector3(0, 8f, 0), new Vector3(1.6f, 14f, 1.6f), Color.white);
                    ghost.FlatTri(new Vector3(-0.8f, 15f, -0.8f), new Vector3(0, 16.5f, 0), new Vector3(0.8f, 15f, -0.8f), Color.white, Vector3.back);
                    ghost.FlatTri(new Vector3(-0.8f, 15f, 0.8f), new Vector3(0, 16.5f, 0), new Vector3(0.8f, 15f, 0.8f), Color.white, Vector3.forward);
                    ghost.M = Matrix4x4.identity;
                }
            }
            acc += len;
        }
        // stone embankment fragments near the lake
        for (int i = 0; i < 26; i++)
        {
            float a = (float)r.NextDouble() * Mathf.PI * 2f;
            Vector2 p = LakeC + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (155f + (float)r.NextDouble() * 20f);
            float h = LandHeight(p.x, p.y);
            if (h > -2f && h < 4f) stone.Box(new Vector3(p.x, h + 0.4f, p.y), new Vector3(4f, 1.2f, 2f), Quaternion.Euler(0, a * Mathf.Rad2Deg, 0), lime * 0.8f);
        }
        ScatterPalms(fol, r, 40, () => { var c = Canal[r.Next(Canal.Count - 1)]; var c2 = Canal[Mathf.Min(Canal.Count - 1, Canal.IndexOf(c) + 1)]; Vector3 p = Vector3.Lerp(c, c2, (float)r.NextDouble()); Vector3 sd = Vector3.Cross(Vector3.up, (c2 - c).normalized) * (r.NextDouble() < 0.5 ? -1 : 1) * (45f + (float)r.NextDouble() * 40f); return new Vector2(p.x + sd.x, p.z + sd.z); }, 0.8f, 15f, null, 0, true);
        stone.SpawnSelf("CanalStones", Mats.Stone, Root);
        fol.SpawnSelf("CanalPalms", Mats.Foliage, Root);
        CanalGhosts = ghost.SpawnSelf("CanalObelisks", Mats.GhostGold, Root, false, false);
        var barge = ShipBuilder.Build(ShipBuilder.Style.Ghost, Root, Mats.GhostTeal);
        barge.root.position = new Vector3(LakeC.x + 40f, 0, LakeC.y - 30f);
        barge.root.rotation = Quaternion.Euler(0, 330, 0);
        barge.root.localScale = Vector3.one * 1.3f;
        barge.root.gameObject.AddComponent<Floater>().Init(330, 1.3f);
        Points["canal_mouth"] = Canal[0];
        Points["canal_exit"] = Canal[Canal.Count - 1];
        Points["canal_restart"] = Canal[0] + new Vector3(0, 0, -80f);
    }

    static void BuildAlexandria()
    {
        var r = new System.Random(1003);
        var solid = NB(); var fol = NB(); var glow = NB(); var wood = NB(); var stone = NB();
        Color lime = new Color(0.86f, 0.82f, 0.72f);
        // the Great Harbour pier
        float px = -1120f, pz = MedCoastZ(px);
        Props.Pier(wood, new Vector3(px, 0, pz - 6f), 0f, 48f, 5f, r);
        float endZ = pz - 6f + 48f;
        Points["dock_alex"] = new Vector3(px + 9.5f, 0, endZ - 6f);
        Points["alex_market"] = new Vector3(px, 1.4f, endZ - 14f);
        Points["alex_approach"] = new Vector3(px + 40f, 0, endZ + 90f);
        for (float z = pz; z < endZ - 3f; z += 9f) Props.Lantern(wood, glow, new Vector3(px + 2.3f, 1.45f, z));
        for (int i = 0; i < 10; i++) Props.Cargo(solid, new Vector3(px + (float)(r.NextDouble() * 3 - 1.5), 1.45f, pz + (float)r.NextDouble() * 34f), r);
        // city: limestone houses, colonnades, temple
        var avoid = new List<Vector2>();
        for (int i = 0; i < 220 && avoid.Count < 46; i++)
        {
            float x = -1700f + (float)r.NextDouble() * 760f;
            float z = MedCoastZ(x) - 25f - (float)r.NextDouble() * 200f;
            float h = LandHeight(x, z);
            if (h < 1.2f || LandGradient(x, z).magnitude > 0.3f) continue;
            bool bad = false;
            foreach (var a in avoid) if ((a - new Vector2(x, z)).sqrMagnitude < 15f * 15f) { bad = true; break; }
            if (bad) continue;
            Props.MudHouse(r.NextDouble() < 0.6 ? stone : solid, new Vector3(x, h, z), (float)r.Next(4) * 90f, 6f + (float)r.NextDouble() * 6f, 6f + (float)r.NextDouble() * 5f, 4f + (float)r.NextDouble() * 5f, false, r);
            avoid.Add(new Vector2(x, z));
        }
        // colonnaded avenue along the waterfront
        for (int i = 0; i < 24; i++)
        {
            float x = -1400f + i * 14f;
            Vector3 g = Ground(x, MedCoastZ(x) - 18f);
            if (g.y < 0.8f) continue;
            Props.Column(stone, g, 8f, 0.5f, false, r, lime);
        }
        // the temple
        Vector3 tc = Ground(-1250f, MedCoastZ(-1250f) - 120f);
        stone.Box(tc + Vector3.up * 0.8f, new Vector3(36f, 1.6f, 22f), lime * 0.95f);
        for (int i = 0; i < 8; i++) for (int k = -1; k <= 1; k += 2)
            Props.Column(stone, tc + new Vector3(-15f + i * 4.3f, 1.6f, k * 9f), 10f, 0.6f, false, r, lime);
        stone.Box(tc + new Vector3(0, 12.2f, 0), new Vector3(37f, 1.2f, 23f), lime);
        stone.FlatTri(tc + new Vector3(-18.5f, 12.8f, 11.5f), tc + new Vector3(0, 17f, 11.5f), tc + new Vector3(18.5f, 12.8f, 11.5f), lime, Vector3.forward);
        stone.FlatTri(tc + new Vector3(-18.5f, 12.8f, -11.5f), tc + new Vector3(0, 17f, -11.5f), tc + new Vector3(18.5f, 12.8f, -11.5f), lime, Vector3.back);
        stone.FlatQuad(tc + new Vector3(-18.5f, 12.8f, -11.5f), tc + new Vector3(0, 17f, -11.5f), tc + new Vector3(0, 17f, 11.5f), tc + new Vector3(-18.5f, 12.8f, 11.5f), Props.Tile, new Vector3(-1, 1, 0));
        stone.FlatQuad(tc + new Vector3(18.5f, 12.8f, -11.5f), tc + new Vector3(0, 17f, -11.5f), tc + new Vector3(0, 17f, 11.5f), tc + new Vector3(18.5f, 12.8f, 11.5f), Props.Tile, new Vector3(1, 1, 0));
        // market stalls along the pier root
        Color[] aw = { new Color(0.2f, 0.35f, 0.6f), new Color(0.85f, 0.75f, 0.5f), new Color(0.6f, 0.15f, 0.12f) };
        for (int i = 0; i < 8; i++)
        {
            float x = px - 40f + i * 11f;
            Vector3 g = Ground(x, MedCoastZ(x) - 10f);
            if (g.y > 0.6f) Props.Stall(solid, g, 180f, aw[i % 3], r);
        }
        for (int i = 0; i < 24; i++)
        {
            float x = px - 60f + (float)r.NextDouble() * 120f;
            Vector3 g = i < 6 ? new Vector3(px + (float)(r.NextDouble() * 3 - 1.5), 1.48f, pz + (float)r.NextDouble() * 34f) : Ground(x, MedCoastZ(x) - 6f - (float)r.NextDouble() * 20f);
            Props.Person(solid, g, (float)r.NextDouble() * 360f, r, r.NextDouble() < 0.3, r.NextDouble() < 0.5 ? new Color(0.88f, 0.85f, 0.78f) : (Color?)null);
        }
        Props.Person(solid, new Vector3(px + 0.5f, 1.48f, endZ - 16f), 0f, r, false, new Color(0.55f, 0.15f, 0.12f));
        ScatterPalms(fol, r, 30, () => new Vector2(-1700f + (float)r.NextDouble() * 800f, MedCoastZ(-1300) - 30f - (float)r.NextDouble() * 220f), 0.8f, 20f, avoid, 7f, true);

        // the Heptastadion mole: from the coast to Pharos island
        Vector3 m0 = new Vector3(-1330, 0, MedCoastZ(-1330) - 5f), m1 = new Vector3(-1290, 0, PharosC.y - 65f);
        float ml = Vector3.Distance(m0, m1);
        for (float t = 0; t <= ml; t += 6f)
        {
            Vector3 q = Vector3.Lerp(m0, m1, t / ml);
            stone.Box(q + Vector3.up * 0.5f, new Vector3(16f, 6f, 6.5f), Quaternion.LookRotation(m1 - m0), lime * (0.82f + 0.08f * (float)r.NextDouble()));
            if (Mathf.Repeat(t, 18f) < 6f) Rocks.Add(new Vector4(q.x, q.z, 10f, 2f));
        }
        // the Pharos: square base, octagonal middle, round lantern
        Vector3 ph = Ground(PharosC.x, PharosC.y);
        stone.Box(ph + new Vector3(0, 1f, 0), new Vector3(40f, 2f, 40f), lime * 0.9f);
        stone.Box(ph + new Vector3(0, 18f, 0), new Vector3(26f, 32f, 26f), lime);
        stone.Cylinder(ph + new Vector3(0, 34f, 0), ph + new Vector3(0, 52f, 0), 10f, 9f, 8, lime * 0.97f, false, true);
        stone.Cylinder(ph + new Vector3(0, 52f, 0), ph + new Vector3(0, 60f, 0), 6f, 5.5f, 14, lime * 0.95f, false, true);
        for (int i = 0; i < 8; i++)
        {
            float a = i / 8f * Mathf.PI * 2f;
            stone.Cylinder(ph + new Vector3(Mathf.Cos(a) * 4.5f, 60f, Mathf.Sin(a) * 4.5f), ph + new Vector3(Mathf.Cos(a) * 4.5f, 66f, Mathf.Sin(a) * 4.5f), 0.4f, 0.35f, 6, lime * 0.8f);
        }
        stone.Cylinder(ph + new Vector3(0, 66f, 0), ph + new Vector3(0, 69f, 0), 5.5f, 1f, 12, Props.GoldC * 0.8f, true, false);
        stone.Sphere(ph + new Vector3(0, 70f, 0), Vector3.one * 1.2f, 8, 6, Props.GoldC);
        Points["pharos_fire"] = ph + new Vector3(0, 62f, 0);
        Flush("Alexandria", solid, fol, glow, stone);
        wood.SpawnSelf("Alex_wood", Mats.Wood, Root);
        FX.LoopingFire(Root, ph + new Vector3(0, 61f, 0), 3.5f);
        var pb = new BeaconDef { Pos = ph + new Vector3(0, 62f, 0), Speed = 16f, Range = 420f, HalfAngle = 9f, Spin = true, Pitch = 11f };
        AddBeaconVisual(pb);
        Beacons.Add(pb);

        // moored ships in the harbour, and the prison galley
        for (int i = 0; i < 3; i++)
        {
            var m = ShipBuilder.Build(ShipBuilder.Style.Merchant, Root);
            m.root.position = new Vector3(px - 30f - i * 28f, 0, endZ - 20f);
            m.root.rotation = Quaternion.Euler(0, 0, 0);
            m.root.gameObject.AddComponent<Floater>().Init(0, 1f, 0.2f);
            m.sail.localScale = new Vector3(1, 0.18f, 1);
        }
        var prison = ShipBuilder.Build(ShipBuilder.Style.Raider, Root);
        Vector3 pp = new Vector3(-1060f, 0, PharosC.y - 60f);
        prison.root.position = pp;
        prison.root.rotation = Quaternion.Euler(0, 100, 0);
        prison.root.localScale = Vector3.one * 1.15f;
        prison.root.gameObject.AddComponent<Floater>().Init(100, 1.15f, 0.2f);
        prison.sail.localScale = new Vector3(1, 0.12f, 1);
        Points["prison"] = pp;
        Points["night_start"] = new Vector3(-1820, 0, 1640);
        Points["escape_target"] = new Vector3(-760, 0, 2250);
        Points["raider_harbour"] = new Vector3(-1000, 0, 1560);
        Points["raider_north"] = new Vector3(-1450, 0, 1960);
        AddPatrol(new[] { new Vector3(-1450, 0, 1830), new Vector3(-1050, 0, 1840) }, 4.5f, 55f);
        AddPatrol(new[] { new Vector3(-1060, 0, 1560), new Vector3(-1060, 0, 1780) }, 4f, 50f);
        Birds.Create(Root, new Vector3(-1250, 70, 1600), 10, 120f);
    }

    static void AddBeaconVisual(BeaconDef b)
    {
        var mb = new MeshBuilder();
        int n = 18;
        float R = Mathf.Tan(b.HalfAngle * Mathf.Deg2Rad);
        Color c0 = new Color(1f, 0.78f, 0.45f, 0.35f), c1 = new Color(1f, 0.6f, 0.3f, 0f);
        for (int i = 0; i < n; i++)
        {
            float a0 = (float)i / n * Mathf.PI * 2f, a1 = (float)(i + 1) / n * Mathf.PI * 2f;
            Vector3 p0 = new Vector3(Mathf.Cos(a0) * R, Mathf.Sin(a0) * R * 0.45f, 1f), p1 = new Vector3(Mathf.Cos(a1) * R, Mathf.Sin(a1) * R * 0.45f, 1f);
            int a = mb.Vert(Vector3.zero, Vector3.up, c0), bb = mb.Vert(p0, Vector3.up, c1), cc = mb.Vert(p1, Vector3.up, c1);
            mb.Tri(a, bb, cc); mb.Tri(a, cc, bb);
        }
        var go = MeshBuilder.Spawn("PharosBeam", mb.Build("PharosBeam"), Mats.Line, Root, false, false);
        go.transform.position = b.Pos;
        go.transform.localScale = Vector3.one * b.Range;
        b.Cone = go.transform;
    }
}
