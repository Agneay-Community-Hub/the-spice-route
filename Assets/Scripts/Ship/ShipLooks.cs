using UnityEngine;

/// <summary>Shows the player's ship class and installed upgrades on the model itself.</summary>
public static class ShipLooks
{
    static int lastKey = -1;

    public static void Apply(Ship ship)
    {
        if (ship == null || ship.P == null || ship.P.model == null) return;
        var d = Progress.D;
        int key = d.Class + 10 * (d.Upgrades[0] + 4 * (d.Upgrades[1] + 4 * (d.Upgrades[2] + 4 * (d.Upgrades[3] + 4 * d.Upgrades[4]))));
        var old = ship.P.model.Find("Upgrades");
        if (key == lastKey && old != null) return;
        lastKey = key;
        if (old != null) Object.Destroy(old.gameObject);

        Vector3[] scales = { Vector3.one, new Vector3(0.86f, 0.95f, 1.05f), new Vector3(1.26f, 1.1f, 1.08f), new Vector3(1.1f, 1.05f, 1.06f), new Vector3(0.95f, 1f, 1.02f) };
        ship.P.model.localScale = scales[Mathf.Clamp(d.Class, 0, 4)];

        var root = new GameObject("Upgrades").transform;
        root.SetParent(ship.P.model, false);
        var wood = new MeshBuilder(); var metal = new MeshBuilder(); var glow = new MeshBuilder(); var cloth = new MeshBuilder();
        var r = new System.Random(5);
        Color bronze = new Color(0.72f, 0.5f, 0.22f);

        // hull: bronze-bossed shields along the rails
        for (int i = 0; i < d.Upgrades[0] * 3; i++)
        {
            float t = 0.3f + 0.4f * (i + 0.5f) / (d.Upgrades[0] * 3);
            float z = -10f + t * 20f;
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 c = new Vector3(s * (ShipBuilder.HalfBeam(t) + 0.08f), ShipBuilder.Sheer(t) - 0.25f, z);
                metal.Cylinder(c, c + Vector3.right * s * 0.1f, 0.42f, 0.42f, 10, new Color(0.5f, 0.16f, 0.1f), true, true);
                metal.Sphere(c + Vector3.right * s * 0.1f, Vector3.one * 0.14f, 6, 4, bronze);
            }
        }
        // sails: a foresail that grows with each level
        if (d.Upgrades[1] > 0 || d.Class == 4)
        {
            float h = 5f + 2f * d.Upgrades[1] + (d.Class == 4 ? 2f : 0f);
            Vector3 foot = new Vector3(0, ShipBuilder.DeckY(0.92f) + 0.6f, 8.6f), mast = new Vector3(0, ShipBuilder.DeckY(0.6f) + h + 2f, 2.4f), tack = new Vector3(0, ShipBuilder.DeckY(0.7f) + 1.2f, 3.2f);
            Color sc = d.Class == 4 ? new Color(0.55f, 0.85f, 0.8f) : new Color(0.93f, 0.88f, 0.74f);
            cloth.FlatTri(foot, mast, tack, sc);
            cloth.FlatTri(foot, tack, mast, sc);
            wood.Cylinder(foot, mast, 0.04f, 0.04f, 4, new Color(0.3f, 0.22f, 0.14f));
        }
        // steering oar: a second, bronze-banded oar
        if (d.Upgrades[2] > 0)
        {
            Vector3 a = new Vector3(-1.5f, ShipBuilder.DeckY(0.08f) + 1.4f, -8.2f), b = new Vector3(-1.9f, -1.6f, -9.6f);
            wood.Cylinder(a, b, 0.09f, 0.09f, 6, new Color(0.35f, 0.24f, 0.14f));
            wood.Box(b + Vector3.up * 0.5f, new Vector3(0.08f, 1.6f, 0.5f + 0.12f * d.Upgrades[2]), new Color(0.35f, 0.24f, 0.14f));
            for (int i = 0; i < d.Upgrades[2]; i++) metal.Cylinder(Vector3.Lerp(a, b, 0.2f + i * 0.18f), Vector3.Lerp(a, b, 0.24f + i * 0.18f), 0.12f, 0.12f, 6, bronze);
        }
        // hold: more cargo lashed on deck
        for (int i = 0; i < d.Upgrades[3] * 3 + (d.Class == 2 ? 6 : 0); i++)
        {
            float t = 0.36f + 0.2f * (float)r.NextDouble();
            Vector3 p = new Vector3(((float)r.NextDouble() - 0.5f) * 2.6f, ShipBuilder.DeckY(t), -10f + t * 20f);
            Props.Cargo(wood, p, r);
        }
        // fire-arrows: braziers at the rails
        for (int i = 0; i < d.Upgrades[4] + (d.Class == 3 ? 1 : 0); i++)
        {
            float t = 0.34f + 0.14f * i;
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 c = new Vector3(s * (ShipBuilder.HalfBeam(t) - 0.35f), ShipBuilder.DeckY(t), -10f + t * 20f);
                metal.Cylinder(c, c + Vector3.up * 0.9f, 0.07f, 0.07f, 5, bronze * 0.7f);
                metal.Cylinder(c + Vector3.up * 0.9f, c + Vector3.up * 1.1f, 0.12f, 0.26f, 8, bronze, true, false);
                glow.Sphere(c + Vector3.up * 1.16f, new Vector3(0.2f, 0.16f, 0.2f), 6, 4, Color.white);
            }
        }
        // war galley: a bronze ram at the bow
        if (d.Class == 3)
        {
            Vector3 bow = new Vector3(0, -0.2f, 9.6f);
            metal.Cylinder(bow, bow + new Vector3(0, 0.1f, 2.6f), 0.5f, 0.06f, 8, bronze, true, true);
            metal.Box(bow + new Vector3(0, 0.05f, 1f), new Vector3(1.5f, 0.12f, 1.6f), bronze * 0.85f);
        }
        // explorer: a teal masthead lamp
        if (d.Class == 4)
        {
            Vector3 top = new Vector3(0, ShipBuilder.DeckY(0.6f) + 13.2f, 1.8f);
            glow.Sphere(top, Vector3.one * 0.3f, 8, 6, new Color(0.4f, 1f, 0.9f));
        }
        // swift merchant: long racing pennants
        if (d.Class == 1)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 a = new Vector3(s * 1.2f, ShipBuilder.Sheer(0.05f) + 2.2f, -9.4f);
                wood.Cylinder(a + Vector3.down * 2.2f, a, 0.04f, 0.03f, 4, new Color(0.3f, 0.22f, 0.14f));
                cloth.FlatTri(a, a + new Vector3(0, -0.5f, 0), a + new Vector3(0, -0.2f, -2.4f), new Color(0.85f, 0.2f, 0.12f));
                cloth.FlatTri(a, a + new Vector3(0, -0.2f, -2.4f), a + new Vector3(0, -0.5f, 0), new Color(0.85f, 0.2f, 0.12f));
            }
        }
        wood.SpawnSelf("U_wood", Mats.Wood, root);
        metal.SpawnSelf("U_metal", Mats.Lit, root);
        cloth.SpawnSelf("U_cloth", Mats.Lit, root);
        glow.SpawnSelf("U_glow", Mats.Lantern, root, false, false);
    }
}
