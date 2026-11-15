using UnityEngine;

/// <summary>Shared runtime materials.</summary>
public static class Mats
{
    public static Texture2D Noise, WaterNormal, SoftDot, SmokeTex, StreakTex, FlameTex, Parchment;
    public static Material Lit, Terrain, Foliage, Wood, Stone, Metal, Ocean, Sky;
    public static Material Add, AddSoft, Alpha, AlphaUnlit, Rain, FlameMat, SmokeMat, Line;
    public static Material Lantern, FireGlow, GhostGold, GhostTeal, GhostRoute, GhostShip;
    public static Material SailKaveri, SailRaider, SailMerchant, Pennant;

    static Shader S(string n)
    {
        var s = Shader.Find(n);
        if (s == null) Debug.LogError("[SpiceRoute] Missing shader: " + n);
        return s;
    }

    public static void SetLinear(Material m, string prop, Color c, float intensity = 1f)
    {
        Color l = c.linear * intensity;
        m.SetVector(prop, new Vector4(l.r, l.g, l.b, c.a));
    }

    public static void Init()
    {
        Noise = ProcTex.Noise();
        WaterNormal = ProcTex.WaterNormal();
        SoftDot = ProcTex.SoftDot();
        SmokeTex = ProcTex.Smoke();
        StreakTex = ProcTex.Streak();
        FlameTex = ProcTex.Flame();
        Parchment = ProcTex.Parchment();

        var lit = S("SR/Lit");
        Lit = MakeLit(lit, 0.32f, 0.3f, 0.12f);
        Terrain = MakeLit(lit, 0.16f, 0.45f, 0.08f);
        Wood = MakeLit(lit, 0.9f, 0.3f, 0.18f);
        Stone = MakeLit(lit, 0.45f, 0.5f, 0.1f);
        Metal = MakeLit(lit, 0.5f, 0.2f, 0.55f);
        Metal.SetFloat("_Metal", 0.8f);
        Foliage = MakeLit(lit, 0.6f, 0.25f, 0.18f);
        Foliage.SetFloat("_Sway", 1f);

        Lantern = MakeLit(lit, 0.3f, 0f, 0.3f);
        SetLinear(Lantern, "_Color", new Color(1f, 0.8f, 0.5f));
        SetLinear(Lantern, "_Emission", new Color(1f, 0.62f, 0.25f), 2.2f);
        FireGlow = MakeLit(lit, 0.3f, 0f, 0.1f);
        SetLinear(FireGlow, "_Emission", new Color(1f, 0.5f, 0.15f), 6f);

        Ocean = new Material(S("SR/Ocean"));
        Ocean.SetTexture("_NormalTex", WaterNormal);
        Ocean.SetTexture("_NoiseTex", Noise);

        Sky = new Material(S("SR/Sky"));
        Sky.SetTexture("_NoiseTex", Noise);

        var add = S("SR/ParticleAdd");
        Add = new Material(add); Add.SetTexture("_MainTex", SoftDot);
        AddSoft = new Material(add); AddSoft.SetTexture("_MainTex", SoftDot); AddSoft.SetFloat("_Intensity", 2.5f);
        FlameMat = new Material(add); FlameMat.SetTexture("_MainTex", FlameTex); FlameMat.SetFloat("_Intensity", 3.2f);
        Line = new Material(add); Line.SetTexture("_MainTex", Texture2D.whiteTexture); Line.SetFloat("_Intensity", 2f);
        Rain = new Material(add); Rain.SetTexture("_MainTex", StreakTex); Rain.SetFloat("_Intensity", 0.55f);

        var alpha = S("SR/ParticleAlpha");
        Alpha = new Material(alpha); Alpha.SetTexture("_MainTex", SoftDot);
        AlphaUnlit = new Material(alpha); AlphaUnlit.SetTexture("_MainTex", SoftDot); AlphaUnlit.SetFloat("_Lit", 0f);
        SmokeMat = new Material(alpha); SmokeMat.SetTexture("_MainTex", SmokeTex);

        GhostGold = MakeGhost(new Color(1f, 0.72f, 0.32f), 1.5f, 0f);
        GhostTeal = MakeGhost(new Color(0.35f, 0.95f, 0.9f), 1.4f, 0f);
        GhostRoute = MakeGhost(new Color(1f, 0.78f, 0.38f), 2.2f, 0f);
        GhostShip = MakeGhost(new Color(0.55f, 0.95f, 1f), 1.6f, 0f);

        var sail = S("SR/Sail");
        SailKaveri = MakeSail(sail, ProcTex.SailTex(ProcTex.SailStyle.Kaveri));
        SailRaider = MakeSail(sail, ProcTex.SailTex(ProcTex.SailStyle.Raider));
        SailMerchant = MakeSail(sail, ProcTex.SailTex(ProcTex.SailStyle.Merchant));
        Pennant = MakeSail(sail, ProcTex.SailTex(ProcTex.SailStyle.Pennant));
        Pennant.SetFloat("_Billow", 0.1f);
        Pennant.SetFloat("_Flutter", 3f);
    }

    static Material MakeLit(Shader s, float detailScale, float detailStrength, float gloss)
    {
        var m = new Material(s);
        m.SetTexture("_DetailTex", Noise);
        m.SetFloat("_DetailScale", detailScale);
        m.SetFloat("_DetailStrength", detailStrength);
        m.SetFloat("_Gloss", gloss);
        return m;
    }

    static Material MakeSail(Shader s, Texture2D t)
    {
        var m = new Material(s);
        m.SetTexture("_MainTex", t);
        return m;
    }

    public static Material MakeGhost(Color c, float intensity, float baseVis)
    {
        var m = new Material(S("SR/Ghost"));
        SetLinear(m, "_Color", c);
        m.SetFloat("_Intensity", intensity);
        m.SetFloat("_Base", baseVis);
        m.SetFloat("_Vis", 1f);
        m.SetTexture("_NoiseTex", Noise);
        return m;
    }

    public static Material Emissive(Color c, float intensity)
    {
        var m = new Material(Lantern);
        SetLinear(m, "_Emission", c, intensity);
        return m;
    }
}
