using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeaconDef
{
    public Vector3 Pos;
    public float YawCenter, Sweep, Speed, Phase, Range, HalfAngle;
    public Transform Cone;
    public float Yaw;
    public bool Spin;
    public float Pitch = 4f;
}

public class PatrolDef
{
    public List<Vector3> Path = new List<Vector3>();
    public float Speed, Radius, T;
    public Transform Boat;
}

/// <summary>Chapter II — The Gate of Tears: Ras al-Mira, the fog coast, Aden, Perim and the Red Sea mouth.</summary>
public static partial class World
{
    public static readonly List<BeaconDef> Beacons = new List<BeaconDef>();
    public static readonly List<PatrolDef> Patrols = new List<PatrolDef>();
    public static readonly List<Vector3> FogChannel = new List<Vector3>();
    public static GameObject FogLamps, GhostBattle, Wreck;
    static Mesh coneMesh, ringMesh2;

    // ---------------------------------------------------------------- coasts
    public static float ArabZ2(float x)
    {
        float z = 255f + 35f * (Geo.N(x, 0, 0.004f, 31) - 0.5f) * 2f;
        z += Geo.S01((-1880f - x) / 380f) * 950f;   // coast turns north into the Red Sea
        return z;
    }

    static float ArabH2(float x, float z)
    {
        float land = (z - ArabZ2(x)) / 320f;
        float h = Geo.Profile(land, 40f, x, z, 21f, 0.05f);
        float inland = z - ArabZ2(x);
        h += Geo.S01((inland - 220f) / 500f) * 170f * (0.5f + 0.7f * Geo.N(x, z, 0.003f, 22));
        return h;
    }

    public static float AfricaZ2(float x)
    {
        return -1050f + Geo.S01((-x - 850f) / 650f) * 900f + 22f * (Geo.N(x, 0, 0.005f, 41) - 0.5f) * 2f;
    }

    static float AfricaH2(float x, float z)
    {
        float land = (AfricaZ2(x) - z) / 300f;
        float h = Geo.Profile(land, 45f, x, z, 42f, 0.05f);
        float inland = AfricaZ2(x) - z;
        h += Geo.S01((inland - 200f) / 500f) * 120f * (0.5f + 0.6f * Geo.N(x, z, 0.003f, 43));
        return h;
    }

    static readonly Vector2 AdenC = new Vector2(-300, 110);

    static float AdenH(float x, float z)
    {
        float baseH = Geo.IslandH(x, z, AdenC, 235f, 18f, 23f);
        float dx = x - AdenC.x, dz = z - AdenC.y;
        float d = Mathf.Sqrt(dx * dx + dz * dz) / 235f;
        float ring = Mathf.Exp(-((d - 0.56f) / 0.15f) * ((d - 0.56f) / 0.15f)) * 135f * (0.65f + 0.6f * Geo.N(x, z, 0.02f, 24));
        float onLand = Geo.S01((baseH + 1f) / 6f);
        // gap in the ring facing east, where the town and harbour sit
        float ang = Mathf.Atan2(dz, dx) * Mathf.Rad2Deg;
        float gap = Mathf.Exp(-(ang * ang) / (32f * 32f));
        return baseH + ring * onLand * (1f - gap * 0.85f);
    }

    // ---------------------------------------------------------------- build
    static IEnumerator BuildChapter2(System.Action<float, string> progress)
    {
        ChartRect = new Rect(-2900, -1350, 4200, 2600);
        progress(0.05f, "Raising the coast of Arabia");
        yield return null;
        Islands.Add(new Island("Arabia2", new Rect(-2900, 60, 4300, 1300), 9f, Biome.Arabian, 21, ArabH2));
        yield return null;
        progress(0.15f, "Raising the coast of Azania");
        Islands.Add(new Island("Africa2", new Rect(-2900, -1400, 2400, 1350), 9f, Biome.Arabian, 41, AfricaH2));
        yield return null;
        progress(0.22f, "Building the crater of Aden");
        Islands.Add(new Island("Aden", new Rect(AdenC.x - 420, AdenC.y - 420, 840, 840), 5f, Biome.Rocky, 23, AdenH));
        Islands.Add(new Island("Perim", new Rect(-1700 - 160, 40 - 160, 320, 320), 4f, Biome.Rocky, 51, (x, z) => Geo.IslandH(x, z, new Vector2(-1700, 40), 72f, 24f, 51f)));
        Islands.Add(new Island("Islet2a", new Rect(-950 - 200, -420 - 200, 400, 400), 5f, Biome.Arabian, 52, (x, z) => Geo.IslandH(x, z, new Vector2(-950, -420), 100f, 26f, 52f)));
        Islands.Add(new Island("Islet2b", new Rect(-2450 - 200, -40 - 200, 400, 400), 5f, Biome.Rocky, 53, (x, z) => Geo.IslandH(x, z, new Vector2(-2450, -40), 90f, 30f, 53f)));
        yield return null;
        foreach (var isl in Islands) { isl.BuildMesh(Root, Mats.Terrain); yield return null; }

        progress(0.35f, "Lighting Ras al-Mira");
        BuildRasAlMira2();
        yield return null;
        progress(0.45f, "Rolling in the sea fog");
        BuildFogCoast();
        yield return null;
        progress(0.52f, "Building Aden");
        BuildAden();
        yield return null;
        progress(0.6f, "Kindling the watchfires of Perim");
        BuildGate();
        yield return null;
        progress(0.66f, "Sinking the Vellimeen");
        BuildWreck();
        yield return null;
        progress(0.7f, "Drawing the chart");
        BuildChart();
        yield return null;
    }

    static void BuildRasAlMira2()
    {
        var r = new System.Random(901);
        var solid = NB(); var fol = NB(); var glow = NB(); var wood = NB();
        float cx = 900f, cz = ArabZ2(cx);
        Props.Pier(wood, new Vector3(cx, 0, cz + 6f), 180f, 48f, 5f, r);
        float endZ = cz + 6f - 48f;
        Points["dock_ras"] = new Vector3(cx + 9.5f, 0, endZ + 6f);
        Points["start"] = new Vector3(cx + 9.5f, 0, endZ + 8f);
        Points["ras_market"] = new Vector3(cx, 1.4f, endZ + 14f);
        for (float z = endZ + 3f; z < cz; z += 10f) Props.Lantern(wood, glow, new Vector3(cx + 2.3f, 1.45f, z));
        for (int i = 0; i < 10; i++) Props.Cargo(solid, new Vector3(cx + (float)(r.NextDouble() * 3 - 1.5), 1.45f, endZ + 4f + (float)r.NextDouble() * 30f), r);
        var avoid = new List<Vector2>();
        for (int i = 0; i < 18; i++)
        {
            float x = cx + (float)(r.NextDouble() * 240 - 120);
            float z = ArabZ2(x) + 25f + (float)r.NextDouble() * 110f;
            float h = LandHeight(x, z);
            if (h < 1.2f || LandGradient(x, z).magnitude > 0.3f) continue;
            bool bad = false;
            foreach (var a in avoid) if ((a - new Vector2(x, z)).sqrMagnitude < 14f * 14f) { bad = true; break; }
            if (bad) continue;
            Props.MudHouse(solid, new Vector3(x, h, z), 180f + (float)(r.NextDouble() * 20 - 10), 6f + (float)r.NextDouble() * 6f, 5f + (float)r.NextDouble() * 4f, 3.2f + (float)r.NextDouble() * 2.5f, r.NextDouble() < 0.3, r);
            avoid.Add(new Vector2(x, z));
        }
        Vector3 tower = Ground(cx + 55f, cz + 40f);
        Props.Watchtower(solid, tower, 15f, r);
        ScatterPalms(fol, r, 30, () => new Vector2(cx + (float)(r.NextDouble() * 300 - 150), ArabZ2(cx) + 8f + (float)r.NextDouble() * 150f), 0.8f, 20f, avoid, 7f, true);
        Props.Person(solid, new Vector3(cx - 0.5f, 1.48f, endZ + 16f), 180f, r, true, new Color(0.35f, 0.28f, 0.45f));
        for (int i = 0; i < 8; i++) Props.Person(solid, new Vector3(cx + (float)(r.NextDouble() * 3 - 1.5), 1.48f, endZ + 6f + (float)r.NextDouble() * 34f), (float)r.NextDouble() * 360f, r, r.NextDouble() < 0.6);
        Flush("Ras2", solid, fol, glow);
        wood.SpawnSelf("Ras2_wood", Mats.Wood, Root);
        Birds.Create(Root, new Vector3(cx, 40, cz), 6, 80f);
    }

    static void BuildFogCoast()
    {
        var r = new System.Random(902);
        FogChannel.Add(new Vector3(640, 0, 170));
        FogChannel.Add(new Vector3(470, 0, 95));
        FogChannel.Add(new Vector3(300, 0, 165));
        FogChannel.Add(new Vector3(140, 0, 100));
        FogChannel.Add(new Vector3(20, 0, 120));
        var stone = NB();
        int placed = 0;
        for (int i = 0; i < 2000 && placed < 150; i++)
        {
            float x = 40f + (float)r.NextDouble() * 600f;
            float z = -60f + (float)r.NextDouble() * 290f;
            if (DistToPolyline(FogChannel, x, z) < 30f) continue;
            if (LandHeight(x, z) > -3f) continue;
            float rad = 3f + (float)r.NextDouble() * 5f;
            bool submerged = r.NextDouble() < 0.5;
            float top = submerged ? -0.7f - (float)r.NextDouble() * 0.5f : 1f + (float)r.NextDouble() * 5f;
            bool close = false;
            foreach (var k in Rocks) if ((new Vector2(k.x, k.y) - new Vector2(x, z)).sqrMagnitude < (k.z + rad) * (k.z + rad) * 0.6f) { close = true; break; }
            if (close) continue;
            Rocks.Add(new Vector4(x, z, rad, top));
            float hgt = top + 8f;
            stone.Rock(new Vector3(x, top - hgt * 0.5f, z), new Vector3(rad, hgt * 0.5f + 0.5f, rad), r.Next(), new Color(0.32f, 0.3f, 0.28f), new Color(0.2f, 0.19f, 0.18f));
            placed++;
        }
        stone.SpawnSelf("FogReefs", Mats.Stone, Root);
        var lamps = NB();
        for (int s = 0; s < FogChannel.Count - 1; s++)
        {
            Vector3 a = FogChannel[s], b = FogChannel[s + 1];
            float len = Vector3.Distance(a, b);
            Vector3 side = Vector3.Cross(Vector3.up, (b - a).normalized);
            for (float t = 0; t < len; t += 32f)
            {
                Vector3 p = Vector3.Lerp(a, b, t / len);
                for (int k = -1; k <= 1; k += 2)
                {
                    Vector3 q = p + side * k * 19f;
                    lamps.Cylinder(q + Vector3.down * 2f, q + Vector3.up * 2.4f, 0.35f, 0.3f, 6, Color.white);
                    lamps.Sphere(q + Vector3.up * 2.8f, Vector3.one * 0.6f, 8, 6, Color.white);
                    lamps.Cylinder(q + Vector3.up * 2.8f, q + Vector3.up * 34f, 0.28f, 0.02f, 6, Color.white);
                }
            }
        }
        FogLamps = lamps.SpawnSelf("FogBeacons", Mats.GhostRoute, Root, false, false);
        Points["fog_entry"] = FogChannel[0];
    }

    static float DistToPolyline(List<Vector3> pts, float x, float z)
    {
        float best = 1e9f;
        Vector2 p = new Vector2(x, z);
        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector2 a = new Vector2(pts[i].x, pts[i].z), b = new Vector2(pts[i + 1].x, pts[i + 1].z);
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            best = Mathf.Min(best, (a + ab * t - p).magnitude);
        }
        return best;
    }

    public static float FogDistance(Vector3 p) => FogChannel.Count > 1 ? DistToPolyline(FogChannel, p.x, p.z) : 1e9f;

    static void BuildAden()
    {
        var r = new System.Random(903);
        var solid = NB(); var fol = NB(); var glow = NB(); var wood = NB(); var stone = NB();
        Vector3 coast = FindCoast(AdenC, 90f, 420f);
        float cz = coast.z;
        Props.Pier(wood, new Vector3(coast.x - 6f, 0, cz), 90f, 46f, 5f, r);
        float end = coast.x - 6f + 46f;
        Points["dock_aden"] = new Vector3(end - 6f, 0, cz - 9.5f);
        Points["aden_market"] = new Vector3(end - 14f, 1.4f, cz);
        for (float x = coast.x; x < end - 3f; x += 9f) Props.Lantern(wood, glow, new Vector3(x, 1.45f, cz + 2.3f));
        for (int i = 0; i < 10; i++) Props.Cargo(solid, new Vector3(coast.x + 3f + (float)r.NextDouble() * 32f, 1.45f, cz + (float)(r.NextDouble() * 3 - 1.5)), r);
        // whitewashed town climbing the crater's eastern gap
        var avoid = new List<Vector2>();
        Color white = new Color(0.9f, 0.86f, 0.78f);
        for (int i = 0; i < 160 && avoid.Count < 34; i++)
        {
            float x = coast.x - 15f - (float)r.NextDouble() * 120f;
            float z = cz + (float)(r.NextDouble() * 160 - 80);
            float h = LandHeight(x, z);
            if (h < 1.2f || h > 45f || LandGradient(x, z).magnitude > 0.45f) continue;
            bool bad = false;
            foreach (var a in avoid) if ((a - new Vector2(x, z)).sqrMagnitude < 12f * 12f) { bad = true; break; }
            if (bad) continue;
            float w = 5f + (float)r.NextDouble() * 5f, d = 5f + (float)r.NextDouble() * 4f, hh = 4f + (float)r.NextDouble() * 6f;
            var hb = r.NextDouble() < 0.6 ? solid : stone;
            Props.MudHouse(hb, new Vector3(x, h, z), 90f + (float)(r.NextDouble() * 20 - 10), w, d, hh, false, r);
            avoid.Add(new Vector2(x, z));
        }
        // sea wall with towers
        Vector3 w0 = Ground(coast.x - 4f, cz - 85f), w1 = Ground(coast.x - 4f, cz + 85f);
        Props.RuinWall(stone, w0, Ground(coast.x - 4f, cz - 12f), 4.5f, 1.6f, r, Props.Sandstone);
        Props.RuinWall(stone, Ground(coast.x - 4f, cz + 12f), w1, 4.5f, 1.6f, r, Props.Sandstone);
        Props.Watchtower(solid, w0, 12f, r);
        Props.Watchtower(solid, w1, 12f, r);
        Props.Lantern(solid, glow, w0 + Vector3.up * 12.5f, 1.2f);
        Props.Lantern(solid, glow, w1 + Vector3.up * 12.5f, 1.2f);
        // the great cisterns: stepped basins on the crater slope
        Vector3 cis = Ground(coast.x - 120f, cz + 20f);
        for (int i = 0; i < 4; i++)
            stone.Box(cis + new Vector3(-i * 7f, i * 2.2f - 1f, 0), new Vector3(12f - i * 1.5f, 2.2f, 16f - i * 2f), Props.StoneC * (0.9f - i * 0.05f));
        // Zahra on the pier
        Props.Person(solid, new Vector3(end - 16f, 1.48f, cz + 1f), 90f, r, false, new Color(0.15f, 0.35f, 0.45f));
        for (int i = 0; i < 14; i++)
            Props.Person(solid, i < 6 ? new Vector3(coast.x + (float)r.NextDouble() * 36f, 1.48f, cz + (float)(r.NextDouble() * 3 - 1.5)) : Ground(coast.x - 8f - (float)r.NextDouble() * 40f, cz + (float)(r.NextDouble() * 60 - 30)), (float)r.NextDouble() * 360f, r, r.NextDouble() < 0.5);
        ScatterPalms(fol, r, 16, () => new Vector2(coast.x - 10f - (float)r.NextDouble() * 80f, cz + (float)(r.NextDouble() * 200 - 100)), 0.8f, 20f, avoid, 6f, true);
        Flush("Aden", solid, fol, glow, stone);
        wood.SpawnSelf("Aden_wood", Mats.Wood, Root);
        for (int i = 0; i < 2; i++)
        {
            var m = ShipBuilder.Build(ShipBuilder.Style.Merchant, Root);
            m.root.position = new Vector3(end - 6f, 0, cz + 14f + i * 14f);
            m.root.rotation = Quaternion.Euler(0, 90, 0);
            m.root.gameObject.AddComponent<Floater>().Init(90f, 1f, 0.2f);
            m.sail.localScale = new Vector3(1, 0.18f, 1);
        }
        Points["aden_approach"] = new Vector3(end + 80f, 0, cz);
        Birds.Create(Root, new Vector3(coast.x - 30f, 50, cz), 8, 90f);
    }

    static void BuildGate()
    {
        var r = new System.Random(904);
        var solid = NB(); var stone = NB();
        Vector2 perim = new Vector2(-1700, 40);
        Vector3 tN = FindCoast(perim, 0f, 200f) + new Vector3(0, 0, -10f);
        Vector3 tS = FindCoast(perim, 180f, 200f) + new Vector3(0, 0, 10f);
        tN.y = LandHeight(tN.x, tN.z); tS.y = LandHeight(tS.x, tS.z);
        Vector3 tA = Ground(-1520f, ArabZ2(-1520f) + 14f);
        Props.Watchtower(solid, tN, 14f, r);
        Props.Watchtower(solid, tS, 14f, r);
        Props.Watchtower(solid, tA, 12f, r);
        for (int i = 0; i < 16; i++)
        {
            float a = (float)r.NextDouble() * Mathf.PI * 2f;
            Vector2 p = perim + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (68f + (float)r.NextDouble() * 20f);
            float h = LandHeight(p.x, p.y);
            if (h > -4f && h < 4f) stone.Rock(new Vector3(p.x, h, p.y), new Vector3(2.5f, 2f, 2.5f) * (0.6f + (float)r.NextDouble()), r.Next(), new Color(0.36f, 0.3f, 0.26f), new Color(0.24f, 0.2f, 0.18f));
        }
        Flush("Gate", solid, null, null, stone);
        AddBeacon(tN + Vector3.up * 15.5f, 0f, 55f, 22f, 0f);
        AddBeacon(tS + Vector3.up * 15.5f, 180f, 55f, 20f, 1.7f);
        AddBeacon(tA + Vector3.up * 13.5f, 200f, 40f, 16f, 3.1f);
        AddPatrol(new[] { new Vector3(-1880, 0, 195), new Vector3(-1560, 0, 205) }, 5f, 55f);
        AddPatrol(new[] { new Vector3(-1560, 0, -95), new Vector3(-1880, 0, -85) }, 4.5f, 55f);
        Points["gate_entry"] = new Vector3(-1380, 0, 170);
        Points["gate_target"] = new Vector3(-2010, 0, 120);
        Points["stealth_start"] = new Vector3(-1150, 0, 120);
    }

    static void AddBeacon(Vector3 pos, float yawCenter, float sweep, float speed, float phase)
    {
        var b = new BeaconDef { Pos = pos, YawCenter = yawCenter, Sweep = sweep, Speed = speed, Phase = phase, Range = 230f, HalfAngle = 13f };
        FX.LoopingFire(Root, pos, 1.6f);
        if (coneMesh == null)
        {
            var mb = new MeshBuilder();
            int n = 18;
            float L = 1f, R = Mathf.Tan(13f * Mathf.Deg2Rad);
            Color c0 = new Color(1f, 0.75f, 0.4f, 0.35f), c1 = new Color(1f, 0.6f, 0.3f, 0f);
            for (int i = 0; i < n; i++)
            {
                float a0 = (float)i / n * Mathf.PI * 2f, a1 = (float)(i + 1) / n * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * R, Mathf.Sin(a0) * R * 0.45f, L), p1 = new Vector3(Mathf.Cos(a1) * R, Mathf.Sin(a1) * R * 0.45f, L);
                int a = mb.Vert(Vector3.zero, Vector3.up, c0), bb = mb.Vert(p0, Vector3.up, c1), cc = mb.Vert(p1, Vector3.up, c1);
                mb.Tri(a, bb, cc); mb.Tri(a, cc, bb);
            }
            coneMesh = mb.Build("BeaconCone");
            coneMesh.bounds = new Bounds(new Vector3(0, 0, 0.5f), new Vector3(1, 1, 1));
        }
        var go = MeshBuilder.Spawn("BeaconCone", coneMesh, Mats.Line, Root, false, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(b.Range, b.Range, b.Range);
        b.Cone = go.transform;
        Beacons.Add(b);
    }

    static void AddPatrol(Vector3[] path, float speed, float radius)
    {
        var p = new PatrolDef { Speed = speed, Radius = radius };
        p.Path.AddRange(path);
        var bb = new MeshBuilder();
        Props.SmallBoat(bb, 9f, new Color(0.12f, 0.1f, 0.09f));
        Props.Person(bb, new Vector3(0, 0.1f, 1.5f), 0, new System.Random(path.Length + (int)radius), true, new Color(0.15f, 0.12f, 0.12f));
        Props.Person(bb, new Vector3(0, 0.1f, -1.5f), 180, new System.Random(7 + (int)speed), true, new Color(0.15f, 0.12f, 0.12f));
        var boat = bb.SpawnSelf("PatrolBoat", Mats.Wood, Root);
        var lamp = new MeshBuilder();
        lamp.Cylinder(new Vector3(0, 0, 3.6f), new Vector3(0, 2.4f, 3.6f), 0.05f, 0.05f, 5, Props.DarkWood);
        lamp.Box(new Vector3(0, 2.5f, 3.6f), Vector3.one * 0.35f, Props.GoldC);
        lamp.SpawnSelf("Lamp", Mats.Lantern, boat.transform, false, false);
        if (ringMesh2 == null)
        {
            var mb = new MeshBuilder();
            int n = 48;
            for (int i = 0; i < n; i++)
            {
                float a0 = (float)i / n * Mathf.PI * 2f, a1 = (float)(i + 1) / n * Mathf.PI * 2f;
                Color ci = new Color(1f, 0.7f, 0.35f, 0.16f), co = new Color(1f, 0.7f, 0.35f, 0f);
                int c = mb.Vert(Vector3.zero, Vector3.up, ci);
                int e0 = mb.Vert(new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), Vector3.up, co);
                int e1 = mb.Vert(new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)), Vector3.up, co);
                mb.Tri(c, e0, e1); mb.Tri(c, e1, e0);
            }
            ringMesh2 = mb.Build("LampPool");
        }
        var pool = MeshBuilder.Spawn("LampPool", ringMesh2, Mats.Line, boat.transform, false, false);
        pool.transform.localPosition = new Vector3(0, 0.6f, 0);
        pool.transform.localScale = new Vector3(radius, 1, radius);
        boat.transform.position = path[0];
        p.Boat = boat.transform;
        Patrols.Add(p);
    }

    static void BuildWreck()
    {
        var r = new System.Random(905);
        Vector3 w = new Vector3(-2090, 0, 165);
        var solid = NB();
        solid.Cylinder(w + new Vector3(0, -6f, 0), w + new Vector3(2.5f, 9f, 1.5f), 0.26f, 0.18f, 8, Props.DarkWood);
        solid.Cylinder(w + new Vector3(1.6f, 6.5f, 1f), w + new Vector3(-3.5f, 5f, 0.4f), 0.12f, 0.1f, 6, Props.DarkWood);
        for (int i = 0; i < 6; i++)
        {
            Vector3 p = w + new Vector3((float)(r.NextDouble() * 16 - 8), 0.1f, (float)(r.NextDouble() * 16 - 8));
            solid.Box(p, new Vector3(0.6f, 0.3f, 2.5f + (float)r.NextDouble() * 3f), Quaternion.Euler(0, (float)r.NextDouble() * 180f, (float)r.NextDouble() * 20f), Props.DarkWood * 0.8f);
        }
        Wreck = solid.SpawnSelf("Wreck", Mats.Wood, Root);
        Points["wreck"] = w;
        EchoStones.Add(w);
        EchoMotes.Add(FX.Motes(Root, w + Vector3.up * 3f));
        var beam = NB();
        beam.Cylinder(w, w + Vector3.up * 45f, 0.7f, 0.05f, 8, Color.white);
        beam.SpawnSelf("WreckBeam", Mats.GhostTeal, Root, false, false);

        // the ghost of the battle seven years ago
        GhostBattle = new GameObject("GhostBattle");
        GhostBattle.transform.SetParent(Root, false);
        var gv = ShipBuilder.Build(ShipBuilder.Style.Ghost, GhostBattle.transform, Mats.GhostShip);
        gv.root.position = w + new Vector3(0, 0, 5f);
        gv.root.rotation = Quaternion.Euler(0, 300, 0);
        gv.root.gameObject.AddComponent<Floater>().Init(300, 1f);
        for (int i = 0; i < 2; i++)
        {
            var gr = ShipBuilder.Build(ShipBuilder.Style.Ghost, GhostBattle.transform, Mats.GhostGold);
            gr.root.position = w + new Vector3(i == 0 ? -26f : 24f, 0, i == 0 ? 18f : -14f);
            gr.root.rotation = Quaternion.Euler(0, i == 0 ? 210 : 30, 0);
            gr.root.gameObject.AddComponent<Floater>().Init(i == 0 ? 210 : 30, 1f);
        }
        Points["boss_spawn"] = new Vector3(-2420, 0, 560);
        Points["escort_spawn"] = new Vector3(-2240, 0, 700);
    }
}
