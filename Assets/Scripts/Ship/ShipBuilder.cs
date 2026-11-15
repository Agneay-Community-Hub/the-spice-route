using System.Collections.Generic;
using UnityEngine;

public class ShipParts
{
    public Transform root, model, yard, sail, oar, flag;
    public Material sailMat, flagMat;
    public List<Transform> crew = new List<Transform>();
    public List<Vector3> firePoints = new List<Vector3>();
    public Transform lanternLight;
    public List<Renderer> renderers = new List<Renderer>();
}

/// <summary>Builds an Indian Ocean stitched-plank trading ship entirely from code.</summary>
public static class ShipBuilder
{
    public enum Style { Kaveri, Raider, Merchant, Ghost }

    const float L = 20f, B = 5.6f;

    static float Sq(float x) => x * x;

    public static float HalfBeam(float t)
    {
        float w;
        if (t > 0.5f) { float u = (t - 0.5f) / 0.5f; w = B * 0.5f * Mathf.Pow(Mathf.Max(0f, Mathf.Cos(u * Mathf.PI * 0.5f)), 0.75f); }
        else { float u = (0.5f - t) / 0.5f; w = B * 0.5f * Mathf.Pow(Mathf.Max(0f, Mathf.Cos(u * Mathf.PI * 0.5f)), 0.5f); }
        return Mathf.Max(0.05f, w);
    }

    public static float Sheer(float t) => 0.9f + 1.6f * Sq(Mathf.Max(0, (t - 0.62f) / 0.38f)) + 1.0f * Sq(Mathf.Max(0, (0.32f - t) / 0.32f));
    static float Keel(float t) => -1.5f + 1.3f * Mathf.Pow(Mathf.Abs(t - 0.5f) * 2f, 3f);
    public static float DeckY(float t) => Sheer(t) - 0.55f;
    static float Z(float t) => -L * 0.5f + t * L;

    static Vector3 HullPt(float t, float f, int side)
    {
        float w = HalfBeam(t) * Mathf.Pow(Mathf.Max(0f, Mathf.Cos(f * Mathf.PI * 0.5f)), 0.8f);
        float y = Mathf.Lerp(Sheer(t), Keel(t), 1f - Mathf.Cos(f * Mathf.PI * 0.5f));
        return new Vector3(side * w, y, Z(t));
    }

    public static ShipParts Build(Style style, Transform parent, Material ghostMat = null)
    {
        var parts = new ShipParts();
        var root = new GameObject(style + "Ship").transform;
        root.SetParent(parent, false);
        parts.root = root;
        var model = new GameObject("Model").transform;
        model.SetParent(root, false);
        parts.model = model;
        bool ghost = style == Style.Ghost;
        var rnd = new System.Random((int)style * 977 + 13);

        Color plankA, plankB, stripe, tar, deckA, deckB, hutC;
        switch (style)
        {
            case Style.Raider:
                plankA = new Color(0.14f, 0.11f, 0.1f); plankB = new Color(0.11f, 0.09f, 0.08f); stripe = new Color(0.45f, 0.06f, 0.05f);
                tar = new Color(0.06f, 0.05f, 0.05f); deckA = new Color(0.3f, 0.24f, 0.18f); deckB = new Color(0.26f, 0.2f, 0.15f); hutC = new Color(0.18f, 0.15f, 0.12f);
                break;
            case Style.Merchant:
                plankA = new Color(0.42f, 0.3f, 0.19f); plankB = new Color(0.38f, 0.27f, 0.17f); stripe = new Color(0.2f, 0.32f, 0.38f);
                tar = new Color(0.1f, 0.08f, 0.07f); deckA = new Color(0.55f, 0.42f, 0.28f); deckB = new Color(0.5f, 0.38f, 0.25f); hutC = new Color(0.55f, 0.46f, 0.3f);
                break;
            default:
                plankA = new Color(0.38f, 0.23f, 0.13f); plankB = new Color(0.33f, 0.2f, 0.11f); stripe = new Color(0.58f, 0.16f, 0.08f);
                tar = new Color(0.1f, 0.08f, 0.07f); deckA = new Color(0.56f, 0.41f, 0.26f); deckB = new Color(0.5f, 0.36f, 0.23f); hutC = new Color(0.5f, 0.4f, 0.24f);
                break;
        }

        // ---------- hull ----------
        var hull = new MeshBuilder();
        int ns = 28, K = 8;
        for (int side = -1; side <= 1; side += 2)
            for (int j = 0; j < K; j++)
            {
                float f0 = (float)j / K, f1 = (float)(j + 1) / K;
                Color band = j == 0 ? stripe : (j % 2 == 0 ? plankA : plankB);
                int start = hull.Count;
                for (int i = 0; i <= ns; i++)
                {
                    float t = (float)i / ns;
                    Vector3 p0 = HullPt(t, f0, side), p1 = HullPt(t, f1, side);
                    Vector3 nrm = new Vector3(side * Mathf.Cos(f0 * Mathf.PI * 0.5f), -Mathf.Sin(f0 * Mathf.PI * 0.5f), 0).normalized;
                    Color c0 = p0.y < -0.3f ? tar : band, c1 = p1.y < -0.3f ? tar : band;
                    hull.Vert(p0, nrm, c0);
                    hull.Vert(p1, nrm, c1);
                }
                for (int i = 0; i < ns; i++)
                {
                    int a = start + i * 2;
                    hull.QuadAuto(a, a + 2, a + 3, a + 1);
                }
            }
        // inner bulwark
        for (int side = -1; side <= 1; side += 2)
        {
            int start = hull.Count;
            for (int i = 0; i <= ns; i++)
            {
                float t = (float)i / ns;
                Vector3 top = HullPt(t, 0, side);
                Vector3 bot = new Vector3(top.x * 0.93f, DeckY(t) - 0.05f, top.z);
                Vector3 nrm = new Vector3(-side, 0, 0);
                hull.Vert(top + new Vector3(-side * 0.06f, 0, 0), nrm, plankB * 0.7f);
                hull.Vert(bot, nrm, plankB * 0.6f);
            }
            for (int i = 0; i < ns; i++)
            {
                int a = start + i * 2;
                hull.QuadAuto(a, a + 2, a + 3, a + 1);
            }
        }
        var hullGo = hull.SpawnSelf("Hull", ghost ? ghostMat : Mats.Wood, model, !ghost, !ghost, true);
        parts.renderers.Add(hullGo.GetComponent<Renderer>());

        // ---------- deck & fittings ----------
        var det = new MeshBuilder();
        int planks = 6;
        for (int p = 0; p < planks; p++)
        {
            int start = det.Count;
            for (int i = 2; i <= ns - 2; i++)
            {
                float t = (float)i / ns;
                float w = HalfBeam(t) * 0.94f;
                float x0 = -w + 2f * w * p / planks, x1 = -w + 2f * w * (p + 1) / planks;
                Color c = p % 2 == 0 ? deckA : deckB;
                det.Vert(new Vector3(x0, DeckY(t), Z(t)), Vector3.up, c);
                det.Vert(new Vector3(x1, DeckY(t), Z(t)), Vector3.up, c);
            }
            int cnt = (det.Count - start) / 2;
            for (int i = 0; i < cnt - 1; i++)
            {
                int a = start + i * 2;
                det.QuadAuto(a, a + 2, a + 3, a + 1);
            }
        }
        // gunwale rails
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < ns; i++)
            {
                Vector3 a = HullPt((float)i / ns, 0, side), b = HullPt((float)(i + 1) / ns, 0, side);
                det.Cylinder(a + Vector3.up * 0.05f, b + Vector3.up * 0.05f, 0.09f, 0.09f, 4, Props.DarkWood);
            }
        // stem & stern posts
        Vector3 bowTop = HullPt(1f, 0, 1); bowTop.x = 0;
        Vector3 prev = bowTop;
        for (int i = 1; i <= 5; i++)
        {
            float u = i / 5f;
            Vector3 p = bowTop + new Vector3(0, u * 1.8f, Mathf.Sin(u * 1.4f) * 0.9f);
            det.Cylinder(prev, p, 0.2f - u * 0.06f, 0.2f - u * 0.08f, 6, Props.DarkWood);
            prev = p;
        }
        if (style == Style.Raider) det.Cylinder(prev, prev + new Vector3(0, 0.4f, 0.8f), 0.2f, 0.02f, 6, new Color(0.65f, 0.45f, 0.2f));
        else
        {
            det.Sphere(prev + new Vector3(0, 0.1f, 0.25f), new Vector3(0.22f, 0.2f, 0.35f), 6, 5, Props.GoldC);
            det.Cylinder(prev + new Vector3(0, 0.1f, 0.5f), prev + new Vector3(0, -0.05f, 0.95f), 0.14f, 0.02f, 6, Props.GoldC);
        }
        Vector3 sternTop = HullPt(0f, 0, 1); sternTop.x = 0;
        det.Cylinder(sternTop, sternTop + new Vector3(0, 1.2f, -0.6f), 0.18f, 0.1f, 6, Props.DarkWood);
        // oculus eyes
        if (style != Style.Merchant)
            for (int side = -1; side <= 1; side += 2)
            {
                float t = 0.86f;
                Vector3 c = HullPt(t, 0.28f, side) + new Vector3(side * 0.04f, 0, 0);
                Vector3 n = new Vector3(side, 0, 0.45f).normalized;
                det.M = Matrix4x4.TRS(c, Quaternion.LookRotation(n), Vector3.one);
                Ellipse(det, 0.42f, 0.24f, 0.0f, style == Style.Raider ? new Color(0.75f, 0.68f, 0.55f) : new Color(0.92f, 0.9f, 0.84f));
                Ellipse(det, 0.15f, 0.15f, 0.02f, style == Style.Raider ? new Color(0.6f, 0.05f, 0.03f) : new Color(0.06f, 0.05f, 0.05f));
                det.M = Matrix4x4.identity;
            }
        if (style == Style.Raider)
        {
            Vector3 ram = HullPt(0.98f, 0.75f, 1); ram.x = 0;
            det.Cylinder(ram, ram + new Vector3(0, 0.1f, 2.2f), 0.45f, 0.03f, 8, new Color(0.55f, 0.4f, 0.18f));
        }
        // mast
        float mastZ = 1.5f;
        float mastT = (mastZ + L * 0.5f) / L;
        det.Cylinder(new Vector3(0, DeckY(mastT), mastZ), new Vector3(0, 13.6f, mastZ), 0.24f, 0.15f, 8, Props.WoodC * 0.85f);
        det.Box(new Vector3(0, DeckY(mastT) + 0.3f, mastZ), new Vector3(0.9f, 0.6f, 0.9f), Props.DarkWood);
        // rigging
        Color rope = new Color(0.32f, 0.26f, 0.18f);
        Vector3 mastTop = new Vector3(0, 13.3f, mastZ);
        det.Cylinder(mastTop, new Vector3(0, Sheer(0.97f) + 0.4f, Z(0.97f)), 0.035f, 0.035f, 4, rope);
        for (int side = -1; side <= 1; side += 2)
        {
            det.Cylinder(mastTop, HullPt(0.14f, 0, side) + Vector3.up * 0.1f, 0.03f, 0.03f, 4, rope);
            for (int k = -1; k <= 1; k += 2)
            {
                float t = mastT + k * 0.045f;
                det.Cylinder(new Vector3(side * 0.15f, 12.6f, mastZ), HullPt(t, 0, side) + Vector3.up * 0.1f, 0.03f, 0.03f, 4, rope);
            }
        }
        // stern hut (thatched barrel roof, Kerala kettuvallam style)
        float hz0 = Z(0.12f), hz1 = Z(0.31f);
        float hutBaseY = DeckY(0.22f);
        float hr = Mathf.Min(HalfBeam(0.18f), HalfBeam(0.3f)) * 0.82f;
        int arcN = 12;
        var hut = new MeshBuilder();
        for (int pass = 0; pass < 2; pass++)
        {
            int start = hut.Count;
            for (int i = 0; i <= arcN; i++)
            {
                float a = (float)i / arcN * Mathf.PI;
                Vector3 off = new Vector3(Mathf.Cos(a) * hr, Mathf.Sin(a) * hr * 1.15f, 0);
                Vector3 nrm = pass == 0 ? new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) : -new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                Color c = (i % 2 == 0 ? hutC : hutC * 0.88f) * (pass == 0 ? 1f : 0.55f);
                hut.Vert(new Vector3(0, hutBaseY + 0.6f, hz0 - 0.4f) + off * (pass == 0 ? 1f : 0.97f), nrm, c);
                hut.Vert(new Vector3(0, hutBaseY + 0.6f, hz1 + 0.4f) + off * (pass == 0 ? 1f : 0.97f), nrm, c);
            }
            for (int i = 0; i < arcN; i++)
            {
                int a = start + i * 2;
                hut.QuadAuto(a, a + 2, a + 3, a + 1);
            }
        }
        for (int i = 0; i < arcN; i++)
        {
            float a0 = (float)i / arcN * Mathf.PI, a1 = (float)(i + 1) / arcN * Mathf.PI;
            Vector3 c0 = new Vector3(0, hutBaseY + 0.6f, hz0);
            hut.FlatTri(c0, c0 + new Vector3(Mathf.Cos(a0) * hr, Mathf.Sin(a0) * hr * 1.15f, 0), c0 + new Vector3(Mathf.Cos(a1) * hr, Mathf.Sin(a1) * hr * 1.15f, 0), hutC * 0.7f, Vector3.back);
        }
        for (int side = -1; side <= 1; side += 2)
            for (int k = 0; k < 3; k++)
            {
                float z = Mathf.Lerp(hz0, hz1, k / 2f);
                hut.Cylinder(new Vector3(side * hr, hutBaseY, z), new Vector3(side * hr, hutBaseY + 0.65f, z), 0.08f, 0.08f, 4, Props.DarkWood);
            }
        det.Append(hut);
        // cargo
        for (int i = 0; i < 9; i++)
        {
            float t = 0.38f + (i / 9f) * 0.1f + (i >= 5 ? 0.18f : 0);
            float x = (i % 3 - 1) * 1.1f;
            Props.Cargo(det, new Vector3(x, DeckY(t), Z(t)), rnd);
        }
        // stern lantern pole
        Vector3 lp = new Vector3(0, DeckY(0.06f), Z(0.06f));
        det.Cylinder(lp, lp + Vector3.up * 2.4f, 0.06f, 0.05f, 5, Props.DarkWood);
        var detGo = det.SpawnSelf("Fittings", ghost ? ghostMat : Mats.Lit, model, !ghost, !ghost);
        parts.renderers.Add(detGo.GetComponent<Renderer>());
        var lantern = new MeshBuilder();
        lantern.Box(lp + new Vector3(0, 2.25f, 0.2f), new Vector3(0.28f, 0.38f, 0.28f), Props.GoldC);
        var lgo = lantern.SpawnSelf("Lantern", ghost ? ghostMat : Mats.Lantern, model, false, false);
        parts.lanternLight = lgo.transform;

        // ---------- steering oar ----------
        var oarPivot = new GameObject("OarPivot").transform;
        oarPivot.SetParent(model, false);
        oarPivot.localPosition = new Vector3(HalfBeam(0.14f) + 0.25f, Sheer(0.14f) + 0.2f, Z(0.14f));
        var oar = new MeshBuilder();
        oar.Cylinder(Vector3.zero, new Vector3(0.35f, -3.6f, -2.6f), 0.12f, 0.1f, 6, Props.WoodC);
        oar.Box(new Vector3(0.38f, -3.6f, -2.75f), new Vector3(0.1f, 1.6f, 0.9f), Quaternion.Euler(-36, 0, 0), Props.WoodC * 0.9f);
        oar.Cylinder(Vector3.zero, new Vector3(-1.4f, 0.4f, 0.6f), 0.07f, 0.07f, 5, Props.DarkWood);
        oar.SpawnSelf("Oar", ghost ? ghostMat : Mats.Wood, oarPivot, !ghost, !ghost);
        parts.oar = oarPivot;

        // ---------- yard + sail ----------
        var yardPivot = new GameObject("Yard").transform;
        yardPivot.SetParent(model, false);
        yardPivot.localPosition = new Vector3(0, 12.4f, mastZ + 0.3f);
        var yard = new MeshBuilder();
        yard.Cylinder(new Vector3(-6.9f, 0, 0), new Vector3(0, 0.15f, 0), 0.07f, 0.15f, 6, Props.WoodC * 0.8f);
        yard.Cylinder(new Vector3(0, 0.15f, 0), new Vector3(6.9f, 0, 0), 0.15f, 0.07f, 6, Props.WoodC * 0.8f);
        yard.SpawnSelf("YardSpar", ghost ? ghostMat : Mats.Wood, yardPivot, !ghost, !ghost);
        parts.yard = yardPivot;

        var sailT = new GameObject("Sail").transform;
        sailT.SetParent(yardPivot, false);
        sailT.localPosition = new Vector3(0, -0.15f, 0.22f);
        var sail = new MeshBuilder();
        int cols = 12, rows = 10;
        float sw = 12.4f, sh = 8.6f;
        for (int face = 0; face < 2; face++)
        {
            int start = sail.Count;
            Vector3 n = face == 0 ? Vector3.forward : Vector3.back;
            for (int j = 0; j <= rows; j++)
                for (int i = 0; i <= cols; i++)
                {
                    float u = (float)i / cols, v = 1f - (float)j / rows;
                    float x = (u - 0.5f) * sw * (1f - (1f - v) * 0.08f);
                    float y = -(1f - v) * sh;
                    sail.Vert(new Vector3(x, y, 0), n, Color.white, new Vector2(u, v));
                }
            for (int j = 0; j < rows; j++)
                for (int i = 0; i < cols; i++)
                {
                    int a = start + j * (cols + 1) + i;
                    int b = a + cols + 1;
                    sail.QuadAuto(a, a + 1, b + 1, b);
                }
        }
        Material sailMat;
        if (ghost) sailMat = ghostMat;
        else
        {
            sailMat = new Material(style == Style.Raider ? Mats.SailRaider : style == Style.Merchant ? Mats.SailMerchant : Mats.SailKaveri);
        }
        var sailGo = sail.SpawnSelf("SailCloth", sailMat, sailT, !ghost, !ghost);
        parts.sail = sailT;
        parts.sailMat = sailMat;
        parts.renderers.Add(sailGo.GetComponent<Renderer>());

        // pennant
        var flagT = new GameObject("Pennant").transform;
        flagT.SetParent(model, false);
        flagT.localPosition = new Vector3(0, 13.6f, mastZ);
        var flag = new MeshBuilder();
        for (int face = 0; face < 2; face++)
        {
            int start = flag.Count;
            Vector3 n = face == 0 ? Vector3.right : Vector3.left;
            for (int i = 0; i <= 8; i++)
            {
                float u = i / 8f;
                float hh = Mathf.Lerp(0.7f, 0.05f, u);
                flag.Vert(new Vector3(0, 0, -u * 4.5f), n, Color.white, new Vector2(u, 1));
                flag.Vert(new Vector3(0, -hh, -u * 4.5f), n, Color.white, new Vector2(u, 0));
            }
            for (int i = 0; i < 8; i++) { int a = start + i * 2; flag.QuadAuto(a, a + 2, a + 3, a + 1); }
        }
        Material flagMat = ghost ? ghostMat : (style == Style.Raider ? new Material(Mats.SailRaider) : new Material(Mats.Pennant));
        if (!ghost) { flagMat.SetFloat("_Billow", 0f); flagMat.SetFloat("_Flutter", 0f); }
        flag.SpawnSelf("PennantCloth", flagMat, flagT, false, !ghost);
        parts.flag = flagT;
        parts.flagMat = flagMat;

        // ---------- crew ----------
        int crewN = style == Style.Raider ? 9 : style == Style.Merchant ? 4 : 6;
        for (int i = 0; i < crewN; i++)
        {
            float t;
            float x;
            if (i == 0) { t = 0.13f; x = HalfBeam(0.13f) * 0.55f; }
            else if (i == 1) { t = 0.9f; x = 0; }
            else { t = 0.36f + (float)rnd.NextDouble() * 0.45f; x = ((float)rnd.NextDouble() * 2f - 1f) * HalfBeam(t) * 0.7f; }
            var cb = new MeshBuilder();
            Color cloth = style == Style.Raider ? Props.Vary(new Color(0.15f, 0.12f, 0.12f), rnd, 0.2f) : Props.Vary(Props.Cloth, rnd, 0.08f);
            Props.Person(cb, Vector3.zero, 0, rnd, style == Style.Raider || rnd.NextDouble() < 0.4, cloth);
            var cgo = cb.SpawnSelf("Crew", ghost ? ghostMat : Mats.Lit, model, !ghost, !ghost);
            cgo.transform.localPosition = new Vector3(x, DeckY(t), Z(t));
            cgo.transform.localRotation = Quaternion.Euler(0, i == 0 ? 0 : (float)rnd.NextDouble() * 360f, 0);
            parts.crew.Add(cgo.transform);
        }

        // fire points along the hull
        for (int i = 0; i < 12; i++)
        {
            float t = 0.15f + (float)rnd.NextDouble() * 0.7f;
            int side = rnd.NextDouble() < 0.5 ? -1 : 1;
            parts.firePoints.Add(i % 3 == 0 ? new Vector3(((float)rnd.NextDouble() - 0.5f) * 2f, DeckY(t) + 0.2f, Z(t)) : HullPt(t, (float)rnd.NextDouble() * 0.3f, side));
        }
        return parts;
    }

    static void Ellipse(MeshBuilder b, float rx, float ry, float z, Color c)
    {
        int n = 14;
        int ci = b.Vert(new Vector3(0, 0, z), Vector3.forward, c);
        int first = b.Count;
        for (int i = 0; i <= n; i++)
        {
            float a = (float)i / n * Mathf.PI * 2f;
            b.Vert(new Vector3(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry, z), Vector3.forward, c);
        }
        for (int i = 0; i < n; i++) b.TriAuto(ci, first + i, first + i + 1);
    }
}
