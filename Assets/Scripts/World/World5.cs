using System.Collections.Generic;
using UnityEngine;

/// <summary>Something to find or use on land.</summary>
public class Site
{
    public enum Kind { Shipwright, Tavern, Inscription, Chest, Herb, Vault, Talk }
    public Kind Type;
    public string Id, Label, Text, Port, Speaker;
    public Vector3 Pos;
    public int Gold;
    public float Radius = 6f;
    public GameObject Go, Beam;
    public bool Repeat => Type == Kind.Shipwright || Type == Kind.Tavern || Type == Kind.Talk;
}

/// <summary>Land sites for on-foot exploration: shipwrights, taverns, inscriptions, chests, herbs, vaults and people.</summary>
public static partial class World
{
    public static readonly List<Site> Sites = new List<Site>();
    public static readonly List<Vector3> Obstacles = new List<Vector3>();   // x, z, radius in (x, y, z)
    static Material beamGold, beamTeal;
    static int siteN;
    static int siteChapter;

    static Vector3? Spot(float x, float z, float minH = 1.2f, float maxH = 34f)
    {
        for (float rr = 0f; rr <= 90f; rr += 5f)
        {
            int n = rr < 1f ? 1 : Mathf.CeilToInt(rr * 0.8f);
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f + rr * 0.37f;
                float px = x + Mathf.Cos(a) * rr, pz = z + Mathf.Sin(a) * rr;
                float h = LandHeight(px, pz);
                if (h < minH || h > maxH) continue;
                if (LandGradient(px, pz).magnitude > 0.45f) continue;
                bool bad = false;
                foreach (var o in Obstacles) if ((o.x - px) * (o.x - px) + (o.y - pz) * (o.y - pz) < (o.z + 4f) * (o.z + 4f)) { bad = true; break; }
                if (!bad) foreach (var s in Sites) if ((s.Pos.x - px) * (s.Pos.x - px) + (s.Pos.z - pz) * (s.Pos.z - pz) < 144f) { bad = true; break; }
                if (bad) continue;
                return new Vector3(px, h, pz);
            }
        }
        return null;
    }

    static Site Add(Site.Kind type, string label, float x, float z, string text = "", int gold = 0, string port = null, string speaker = null)
    {
        siteN++;
        var p = Spot(x, z);
        if (!p.HasValue) return null;
        var s = new Site { Type = type, Label = label, Pos = p.Value, Text = text, Gold = gold, Port = port, Speaker = speaker, Id = "c" + siteChapter + "_" + siteN };
        Sites.Add(s);
        BuildSite(s);
        return s;
    }

    static void BuildSite(Site s)
    {
        if (beamGold == null) { beamGold = Mats.Emissive(new Color(1f, 0.75f, 0.3f), 2.2f); beamTeal = Mats.Emissive(new Color(0.35f, 1f, 0.9f), 2.2f); }
        var go = new GameObject("Site_" + s.Type + "_" + s.Id);
        go.transform.SetParent(Root, false);
        go.transform.position = s.Pos;
        s.Go = go;
        var r = new System.Random(s.Id.GetHashCode());
        var solid = NB(); var stone = NB(); var wood = NB(); var glow = NB(); var fol = NB(); var ghost = NB();
        Vector3 o = Vector3.zero;
        Color timber = new Color(0.42f, 0.29f, 0.17f), bronze = new Color(0.72f, 0.5f, 0.22f);
        switch (s.Type)
        {
            case Site.Kind.Shipwright:
                s.Radius = 9f;
                wood.Box(o + new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0.5f, 9f), timber);
                for (int i = 0; i < 7; i++)
                {
                    float z = -3.6f + i * 1.2f, w = 1.7f * Mathf.Sin((i + 0.6f) / 7.2f * Mathf.PI);
                    wood.Cylinder(o + new Vector3(0, 0.7f, z), o + new Vector3(w, 2.3f, z), 0.09f, 0.07f, 5, timber * 1.15f);
                    wood.Cylinder(o + new Vector3(0, 0.7f, z), o + new Vector3(-w, 2.3f, z), 0.09f, 0.07f, 5, timber * 1.15f);
                }
                for (int k = -1; k <= 1; k += 2) { wood.Box(o + new Vector3(k * 2.2f, 0.9f, -3f), new Vector3(0.25f, 1.8f, 0.25f), timber * 0.8f); wood.Box(o + new Vector3(k * 2.2f, 0.9f, 3f), new Vector3(0.25f, 1.8f, 0.25f), timber * 0.8f); }
                Props.Cargo(wood, o + new Vector3(3.6f, 0, 1f), r);
                Props.Person(solid, o + new Vector3(3.2f, 0, -1.5f), -90f, r, false);
                break;
            case Site.Kind.Tavern:
                s.Radius = 8f;
                for (int k = -1; k <= 1; k += 2) for (int j = -1; j <= 1; j += 2) wood.Box(o + new Vector3(k * 3f, 1.5f, j * 2.4f), new Vector3(0.22f, 3f, 0.22f), timber);
                wood.Box(o + new Vector3(0, 3.1f, 0), new Vector3(7.2f, 0.16f, 5.8f), Quaternion.Euler(0, 0, 6f), new Color(0.62f, 0.2f, 0.12f));
                wood.Box(o + new Vector3(0, 0.9f, -1.4f), new Vector3(4.6f, 0.2f, 1f), timber * 1.1f);
                for (int i = 0; i < 4; i++) wood.Cylinder(o + new Vector3(-1.8f + i * 1.2f, 0, 0.6f), o + new Vector3(-1.8f + i * 1.2f, 0.55f, 0.6f), 0.28f, 0.28f, 7, timber * 0.9f, false, true);
                Props.Lantern(wood, glow, o + new Vector3(3f, 0, 2.4f));
                Props.Lantern(wood, glow, o + new Vector3(-3f, 0, 2.4f));
                Props.Person(solid, o + new Vector3(0, 0, -2.3f), 0f, r, true);
                Props.Person(solid, o + new Vector3(-1.8f, 0.55f, 0.6f), 180f, r, false);
                break;
            case Site.Kind.Inscription:
                Props.Stele(stone, o, (float)r.NextDouble() * 360f);
                break;
            case Site.Kind.Chest:
                s.Radius = 4.5f;
                wood.Box(o + new Vector3(0, 0.35f, 0), new Vector3(1.1f, 0.7f, 0.7f), timber);
                wood.Cylinder(o + new Vector3(-0.55f, 0.7f, 0), o + new Vector3(0.55f, 0.7f, 0), 0.35f, 0.35f, 8, timber * 1.1f, true, true);
                solid.Box(o + new Vector3(0.3f, 0.5f, 0), new Vector3(0.08f, 1.02f, 0.74f), bronze);
                solid.Box(o + new Vector3(-0.3f, 0.5f, 0), new Vector3(0.08f, 1.02f, 0.74f), bronze);
                break;
            case Site.Kind.Herb:
                s.Radius = 4.5f;
                Props.Bush(fol, o, 1.3f, r, new Color(0.2f, 0.5f, 0.18f));
                for (int i = 0; i < 5; i++) glow.Sphere(o + new Vector3((float)r.NextDouble() * 1.4f - 0.7f, 0.6f + (float)r.NextDouble() * 0.7f, (float)r.NextDouble() * 1.4f - 0.7f), Vector3.one * 0.07f, 5, 4, new Color(0.7f, 1f, 0.4f));
                break;
            case Site.Kind.Vault:
                s.Radius = 7f;
                Props.Column(stone, o + new Vector3(-1.6f, 0, 0), 4.2f, 0.45f, false, r, new Color(0.5f, 0.48f, 0.44f));
                Props.Column(stone, o + new Vector3(1.6f, 0, 0), 4.2f, 0.45f, false, r, new Color(0.5f, 0.48f, 0.44f));
                stone.Box(o + new Vector3(0, 4.45f, 0), new Vector3(4.4f, 0.5f, 1.1f), new Color(0.46f, 0.44f, 0.4f));
                stone.Box(o + new Vector3(0, 0.15f, 0.9f), new Vector3(3f, 0.3f, 1.4f), new Color(0.42f, 0.4f, 0.37f));
                ghost.Box(o + new Vector3(0, 2.1f, 0), new Vector3(2.7f, 4.2f, 0.2f), Color.white);
                ghost.Sphere(o + new Vector3(0, 2.3f, 0.2f), Vector3.one * 0.5f, 10, 8, Color.white);
                break;
            case Site.Kind.Talk:
                s.Radius = 4.5f;
                Props.Person(solid, o, (float)r.NextDouble() * 360f, r, r.NextDouble() < 0.5, new Color(0.75f, 0.3f, 0.2f));
                break;
        }
        solid.SpawnSelf("solid", Mats.Lit, go.transform);
        stone.SpawnSelf("stone", Mats.Stone, go.transform);
        wood.SpawnSelf("wood", Mats.Wood, go.transform);
        fol.SpawnSelf("fol", Mats.Foliage, go.transform);
        glow.SpawnSelf("glow", Mats.Lantern, go.transform, false, false);
        ghost.SpawnSelf("ghost", Mats.GhostTeal, go.transform, false, false);
        if (s.Type != Site.Kind.Talk)
        {
            var beam = NB();
            bool shop = s.Type == Site.Kind.Shipwright || s.Type == Site.Kind.Tavern;
            beam.Cylinder(new Vector3(0, 3f, 0), new Vector3(0, shop ? 46f : 34f, 0), shop ? 0.22f : 0.14f, 0.03f, 6, Color.white);
            s.Beam = beam.SpawnSelf("Beam", shop ? beamGold : beamTeal, go.transform, false, false);
            if (!s.Repeat && Progress.IsFound(s.Id)) s.Beam.SetActive(false);
        }
        if (s.Type == Site.Kind.Shipwright || s.Type == Site.Kind.Tavern || s.Type == Site.Kind.Vault) Obstacles.Add(new Vector3(s.Pos.x, s.Pos.z, s.Type == Site.Kind.Vault ? 0f : 1.2f));
    }

    static readonly string[] Lore =
    {
        "\"Here the first pepper was weighed for a ship from the west. May the scales stay honest.\"",
        "\"When the wind turns in the month of rains, sail west. When it turns again, come home.\"",
        "\"The sea is a road with no stones. Only memory marks the way.\"",
        "\"We lit lamps on the reefs so that strangers would live. A drowned stranger buys no pepper.\"",
        "\"Seven seas, said the old ones. But it is one water, and it remembers.\"",
        "\"The navigator Raghavan passed here, and asked the stones for a road no chart records.\"",
        "\"Beware the men who draw maps and show them to no one.\"",
        "\"Trade is a promise made across water. Break it, and the wind will know.\"",
    };

    /// <summary>Places every land site for a chapter. Call after the world is built.</summary>
    public static void BuildSites(int chapter)
    {
        Sites.Clear();
        siteN = 0;
        siteChapter = chapter;
        int lore = chapter * 3;
        string L() => Lore[(lore++) % Lore.Length];
        if (chapter == 1 || chapter == 4)
        {
            float cx = PortCoastX;
            Add(Site.Kind.Shipwright, "Muziris shipwright", MalabarCoastX(-92) + 14f, -92);
            Add(Site.Kind.Tavern, "The Pepper Vine tavern", MalabarCoastX(84) + 16f, 84, "", 0, "muziris");
            Add(Site.Kind.Talk, "Pepper merchant", cx + 24f, 14, chapter == 1 ? "Pepper is cheap here and worth three times as much across the western sea. Fill your hold before you sail." : "The whole city is talking about you, captain. They say you sailed the old kings' canal!", 0, null, "Pepper Merchant");
            Add(Site.Kind.Talk, "Old sailor", cx + 30f, -40, "Want my advice? Visit the shipwright for a stronger hull, and keep your crew paid at the tavern. An unpaid crew is a slow crew.", 0, null, "Old Sailor");
            Add(Site.Kind.Inscription, "Founding stone of Muziris", cx + 120f, -30, L());
            Add(Site.Kind.Herb, "Wild cardamom", cx + 95f, 150, "", 18);
            Add(Site.Kind.Chest, "Lighthouse keeper's chest", -380, 430, "", 60);
            Add(Site.Kind.Inscription, "Kadal standing stone", -790, -150, L());
            Add(Site.Kind.Chest, "Smuggler's cache", -900, -260, "", 75);
            Add(Site.Kind.Herb, "Wild pepper vine", -760, -300, "", 20);
            Add(Site.Kind.Vault, "Vault of Echoes", -1560, -60, "", 130);
            Add(Site.Kind.Inscription, "Stone of the first navigators", -1440, -160, L());
            Add(Site.Kind.Chest, "Castaway's chest", -150, -560, "", 50);
            Add(Site.Kind.Herb, "Wild cinnamon", -1180, 540, "", 22);
            Add(Site.Kind.Chest, "Wrecker's hoard", -560, 800, "", 65);
            Add(Site.Kind.Inscription, "Wind-worn stele", -2000, 760, L());
            Add(Site.Kind.Chest, "Sunken trader's strongbox", -1950, -950, "", 80);
            Add(Site.Kind.Herb, "Wild ginger", 60, -1080, "", 18);
            if (Points.ContainsKey("dock_outpost"))
            {
                Vector3 d = Points["dock_outpost"];
                Add(Site.Kind.Chest, "Abandoned strongbox", d.x - 50f, d.z + 30f, "", 70);
                Add(Site.Kind.Inscription, "Outpost marker stone", d.x - 60f, d.z - 30f, L());
            }
            if (chapter == 4)
            {
                Add(Site.Kind.Vault, "Vault of the Archive", -760, -2090, "", 200);
                Add(Site.Kind.Inscription, "The Archive's first wall", -690, -2200, "\"We wrote the sea down so that no one would be lost. Whoever reads this: do not let it be owned.\"");
            }
        }
        else if (chapter == 2)
        {
            Vector3 ras = P("dock_ras"), mk = P("aden_market");
            Add(Site.Kind.Shipwright, "Ras al-Mira shipwright", ras.x + 30f, ras.z + 50f);
            Add(Site.Kind.Tavern, "The Dhow's Rest", ras.x - 35f, ras.z + 60f, "", 0, "ras");
            Add(Site.Kind.Inscription, "Incense road marker", ras.x, ras.z + 130f, L());
            Add(Site.Kind.Shipwright, "Aden shipwright", mk.x + 40f, mk.z + 25f);
            Add(Site.Kind.Tavern, "The Crater Lamp", mk.x - 40f, mk.z + 25f, "", 0, "aden");
            Add(Site.Kind.Talk, "Temple merchant", mk.x + 8f, mk.z + 14f, "The Gate of Tears is watched at night. Douse your lantern with L and keep out of the beams.", 0, null, "Temple Merchant");
            Add(Site.Kind.Inscription, "Crater wall inscription", mk.x, mk.z + 80f, L());
            Add(Site.Kind.Chest, "Watchman's stash", -1700, 40, "", 85);
            Add(Site.Kind.Chest, "Pearl diver's box", -950, -420, "", 70);
            Add(Site.Kind.Vault, "Vault of the Strait", -2450, -40, "", 150);
            Add(Site.Kind.Herb, "Myrrh tree", 420, 200, "", 24);
        }
        else if (chapter == 3)
        {
            Vector3 ber = P("dock_berenike"), mk = P("alex_market");
            Add(Site.Kind.Shipwright, "Berenike shipwright", ber.x - 40f, ber.z + 40f);
            Add(Site.Kind.Tavern, "The Red Sea Rest", ber.x - 40f, ber.z - 40f, "", 0, "berenike");
            Add(Site.Kind.Inscription, "Pharaoh's canal stele", -80, 760, "\"I, the king, ordered this canal dug from the river to the sea, that ships might sail from Egypt to the east.\"");
            Add(Site.Kind.Herb, "Papyrus and lotus", -330, 960, "", 22);
            Add(Site.Kind.Shipwright, "Alexandria shipyard", mk.x + 45f, mk.z - 25f);
            Add(Site.Kind.Tavern, "The Lighthouse Cup", mk.x - 45f, mk.z - 25f, "", 0, "alex");
            Add(Site.Kind.Talk, "Greek scholar", mk.x + 8f, mk.z - 14f, "They say the Cartographers keep a prisoner on the galley past the island. Go at night, and stay out of the lighthouse beam.", 0, null, "Greek Scholar");
            Add(Site.Kind.Inscription, "Library fragment", mk.x, mk.z - 80f, L());
            Add(Site.Kind.Vault, "Vault of the Pharos", PharosC.x, PharosC.y, "", 160);
            Add(Site.Kind.Chest, "Desert caravan cache", 300, 300, "", 75);
        }
    }
}
