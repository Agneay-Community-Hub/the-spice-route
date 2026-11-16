using System.Collections.Generic;
using UnityEngine;

/// <summary>Chapter IV — beyond the edge of the map: the Storm Wall and the Sunken Archive.</summary>
public static partial class World
{
    public static readonly List<Vector3> Passage = new List<Vector3>();
    public static GameObject PassageLamps, ArchiveGhost;
    static readonly Vector2 ArchiveC = new Vector2(-760, -2180);

    static void BuildEdge()
    {
        var r = new System.Random(1401);
        Passage.Clear();
        Passage.Add(new Vector3(-830, 0, -760));
        Passage.Add(new Vector3(-760, 0, -1120));
        Passage.Add(new Vector3(-900, 0, -1400));
        Passage.Add(new Vector3(-720, 0, -1660));
        Passage.Add(new Vector3(-790, 0, -1880));

        var isl = new Island("Archive", new Rect(ArchiveC.x - 400, ArchiveC.y - 400, 800, 800), 5f, Biome.Rocky, 71, (x, z) => Geo.IslandH(x, z, ArchiveC, 220f, 40f, 71f));
        Islands.Add(isl);
        isl.BuildMesh(Root, Mats.Terrain);

        // the reef field of the Storm Wall
        var stone = NB();
        int placed = 0;
        for (int i = 0; i < 4000 && placed < 230; i++)
        {
            float x = -1180f + (float)r.NextDouble() * 760f;
            float z = -1760f + (float)r.NextDouble() * 890f;
            if (DistToPolyline(Passage, x, z) < 36f) continue;
            if (LandHeight(x, z) > -3f) continue;
            float rad = 3f + (float)r.NextDouble() * 7f;
            bool submerged = r.NextDouble() < 0.4;
            float top = submerged ? -0.7f - (float)r.NextDouble() * 0.5f : 1.5f + (float)r.NextDouble() * 9f;
            bool close = false;
            foreach (var k in Rocks) if ((new Vector2(k.x, k.y) - new Vector2(x, z)).sqrMagnitude < (k.z + rad) * (k.z + rad) * 0.6f) { close = true; break; }
            if (close) continue;
            Rocks.Add(new Vector4(x, z, rad, top));
            float hgt = top + 10f;
            stone.Rock(new Vector3(x, top - hgt * 0.5f, z), new Vector3(rad, hgt * 0.5f + 0.5f, rad * (0.7f + (float)r.NextDouble() * 0.5f)), r.Next(), new Color(0.24f, 0.24f, 0.25f), new Color(0.14f, 0.14f, 0.15f));
            placed++;
        }

        // ancient passage lamps
        var lamps = NB();
        for (int s = 0; s < Passage.Count - 1; s++)
        {
            Vector3 a = Passage[s], b = Passage[s + 1];
            float len = Vector3.Distance(a, b);
            Vector3 side = Vector3.Cross(Vector3.up, (b - a).normalized);
            for (float t = 0; t < len; t += 36f)
            {
                Vector3 p = Vector3.Lerp(a, b, t / len);
                for (int k = -1; k <= 1; k += 2)
                {
                    Vector3 q = p + side * k * 24f;
                    lamps.Cylinder(q + Vector3.down * 2f, q + Vector3.up * 3f, 0.4f, 0.32f, 6, Color.white);
                    lamps.Sphere(q + Vector3.up * 3.5f, Vector3.one * 0.7f, 8, 6, Color.white);
                    lamps.Cylinder(q + Vector3.up * 3.5f, q + Vector3.up * 50f, 0.3f, 0.02f, 6, Color.white);
                }
            }
        }
        PassageLamps = lamps.SpawnSelf("PassageLamps", Mats.GhostRoute, Root, false, false);

        // the drowned archive: colonnades and halls half under water, a whole ghost city in the past
        var ghost = NB();
        Color basalt = new Color(0.36f, 0.36f, 0.4f);
        Vector3 north = FindCoast(ArchiveC, 0f, 420f);
        Vector3 inward = (new Vector3(ArchiveC.x, 0, ArchiveC.y) - north).normalized;
        Vector3 hall = north + inward * 30f;
        hall.y = LandHeight(hall.x, hall.z);
        Points["archive_echo"] = north + inward * 10f;
        Points["archive_entry"] = north - inward * 40f;
        for (int i = 0; i < 14; i++)
        {
            float a = i / 14f * Mathf.PI * 2f;
            Vector3 c = hall + new Vector3(Mathf.Cos(a) * 26f, 0, Mathf.Sin(a) * 26f);
            c.y = LandHeight(c.x, c.z);
            Props.Column(stone, c, 12f, 0.9f, r.NextDouble() < 0.6, r, basalt);
            Props.Column(ghost, c, 12f, 0.9f, false, r, Color.white);
        }
        Props.Stele(stone, hall, 0f);
        ghost.Cylinder(hall + Vector3.up * 13f, hall + Vector3.up * 22f, 30f, 2f, 20, Color.white, true, false);
        for (int i = 0; i < 26; i++)
        {
            float a = (float)r.NextDouble() * Mathf.PI * 2f;
            float d = 60f + (float)r.NextDouble() * 160f;
            Vector3 p = new Vector3(ArchiveC.x + Mathf.Cos(a) * d, 0, ArchiveC.y + Mathf.Sin(a) * d);
            p.y = LandHeight(p.x, p.z);
            float w = 8f + (float)r.NextDouble() * 10f, hh = 6f + (float)r.NextDouble() * 18f;
            ghost.Box(p + Vector3.up * hh * 0.5f, new Vector3(w, hh, w * 0.8f), Quaternion.Euler(0, (float)r.NextDouble() * 90f, 0), Color.white);
            if (p.y > -6f && r.NextDouble() < 0.5)
                stone.Box(p + Vector3.up * 1.2f, new Vector3(w, 2.4f + (float)r.NextDouble() * 3f, w * 0.8f), Quaternion.Euler(0, (float)r.NextDouble() * 90f, (float)r.NextDouble() * 8f), basalt * 0.9f);
        }
        // a great ghost lighthouse at the summit
        Vector3 summit = Ground(ArchiveC.x, ArchiveC.y);
        Props.Lighthouse(ghost, ghost, summit, 36f, Color.white, false, r);
        stone.SpawnSelf("StormReefs", Mats.Stone, Root);
        ArchiveGhost = ghost.SpawnSelf("ArchiveGhostCity", Mats.GhostTeal, Root, false, false);
        EchoMotes.Add(FX.Motes(Root, Points["archive_echo"] + Vector3.up * 3f));
        var beam = NB();
        beam.Cylinder(Points["archive_echo"], Points["archive_echo"] + Vector3.up * 60f, 0.8f, 0.05f, 8, Color.white);
        beam.SpawnSelf("ArchiveBeam", Mats.GhostTeal, Root, false, false);

        Points["storm_entry"] = Passage[0];
        Points["fleet_rendezvous"] = new Vector3(-330, 0, 300);
        Points["final_enemy_a"] = new Vector3(-380, 0, -2330);
        Points["final_enemy_b"] = new Vector3(-1160, 0, -2380);
    }

    public static float PassageDistance(Vector3 p) => Passage.Count > 1 ? DistToPolyline(Passage, p.x, p.z) : 1e9f;
}
