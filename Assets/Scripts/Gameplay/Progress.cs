using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CrewMember
{
    public string Name, Origin, Trait, Port;
    public int[] Skill = new int[6];   // stars per station
    public float Loyalty = 55f;
    public int Station = -1;           // -1 resting
    public int Wage = 6, HireCost = 40;
}

/// <summary>
/// The captain's long-term progress: skill trees, ship upgrades and class, crew and their loyalty,
/// and what has been found on land. Kept across chapters and saved between sessions.
/// </summary>
public static class Progress
{
    [Serializable]
    public class Data
    {
        public int Points, Earned;
        public bool[] Skills = new bool[12];
        public int[] Upgrades = new int[5];
        public bool[] ClassOwned = { true, false, false, false, false };
        public int Class;
        public List<CrewMember> Crew = new List<CrewMember>();
        public List<string> Found = new List<string>();
        public int Gold;
        public int Victories, Discoveries;
    }

    public struct Stats
    {
        public float SpeedMul, TurnMul, MaxHP, DamageTaken, VolleyDmgMul, ReloadMul, EraDrainMul, RepairMul, BuyMul, SellMul, RamMul, RamSelfMul, DriftMul, CostMul;
        public int Cargo, Volley;
    }

    public static Data D = NewData();
    public static Stats S;
    public static bool Persist = true;
    const string Key = "spiceroute_progress_v1";

    // ---------------------------------------------------------------- definitions
    public static readonly string[] TreeNames = { "NAVIGATOR", "MERCHANT", "WARRIOR" };
    public static readonly string[] SkillNames =
    {
        "Trim Mastery", "Weather Eye", "Keen Compass", "Pathfinder",
        "Haggler", "Silver Tongue", "Deep Hold", "Guild Contacts",
        "Fletcher", "Quick Hands", "Hardened Hull", "Ram Master",
    };
    public static readonly string[] SkillDesc =
    {
        "Ship sails 6% faster.", "Storm winds push you off course half as much.", "The Era Shift lasts 30% longer.", "Turn 12% faster and sail 4% faster.",
        "Buy goods 8% cheaper.", "Sell goods 8% dearer.", "The hold carries 6 more cargo.", "Shipwrights and crew cost 25% less.",
        "One more archer: +1 arrow in every volley.", "Reload volleys 20% faster.", "Take 15% less damage.", "Ramming hurts enemies 60% more and you 50% less.",
    };
    public static readonly int[] SkillCost = { 1, 2, 2, 3 };

    public static readonly string[] UpgradeNames = { "Hull", "Sails", "Steering Oar", "Hold", "Fire-Arrows" };
    public static readonly string[] UpgradeDesc = { "+20 hull strength per level", "+4% speed per level", "+8% turning per level", "+4 cargo per level", "+10% arrow damage per level" };
    public static readonly int[] UpgradeCost = { 60, 110, 170 };

    public static readonly string[] ClassNames = { "Coastal Trader", "Swift Merchant", "Great Cargo Ship", "War Galley", "Explorer" };
    public static readonly string[] ClassDesc =
    {
        "The Kaveri as your father built her. Balanced in everything.",
        "Fast and nimble, but thin-hulled.  +12% speed, +5% turning, -20 hull, -4 cargo",
        "A floating warehouse.  +14 cargo, +30 hull, -10% speed, -15% turning",
        "Built to fight.  +2 arrows, +50 hull, -10% damage taken, -8 cargo",
        "Made for the unknown.  Era Shift lasts 25% longer, +5% speed, +10% turning, -10 hull, -1 arrow",
    };
    public static readonly int[] ClassCost = { 0, 160, 220, 280, 200 };

    public static readonly string[] StationNames = { "HELM", "SAILS", "LOOKOUT", "ARCHERS", "CARPENTER", "QUARTERMASTER" };
    public static readonly string[] StationDesc = { "turning", "speed", "longer Era Shift", "arrow damage", "faster repairs", "better prices" };

    static CrewMember Make(string name, string origin, string port, string trait, int hire, int wage, params int[] skill)
    {
        return new CrewMember { Name = name, Origin = origin, Port = port, Trait = trait, HireCost = hire, Wage = wage, Skill = skill };
    }

    /// <summary>Everyone who can be hired, by the port whose tavern they wait in.</summary>
    public static List<CrewMember> Pool()
    {
        return new List<CrewMember>
        {
            //                                                            helm sails look arch carp quar
            Make("Unni", "Muziris", "muziris", "Cheerful shipwright's apprentice. Fixes anything.", 35, 5, 0, 1, 0, 0, 3, 0),
            Make("Meenakshi", "the Western Ghats", "muziris", "A hunter from the hills. She never misses.", 50, 7, 0, 0, 2, 3, 0, 0),
            Make("Kannan", "Kollam", "muziris", "Steady old boatswain. Good at everything, best at nothing.", 40, 6, 2, 2, 1, 1, 1, 1),
            Make("Salim", "Arabia", "ras", "A star-reader who knows the old sea roads.", 50, 7, 2, 0, 3, 0, 0, 1),
            Make("Asha", "Azania", "aden", "A trader who knows the price of everything.", 55, 8, 0, 0, 1, 1, 0, 3),
            Make("Kleon", "Greece", "berenike", "Loud, brave and quick up the mast.", 45, 6, 0, 3, 0, 2, 0, 0),
            Make("Nefer", "Egypt", "alex", "A temple scribe with sharp eyes and a sharper tongue.", 60, 8, 0, 0, 3, 0, 0, 2),
        };
    }

    static Data NewData()
    {
        var d = new Data();
        var p = Make("Paachu", "Muziris", "", "Gruff old helmsman. He sailed with your father.", 0, 6, 3, 2, 1, 0, 1, 0);
        p.Loyalty = 85f;
        p.Station = 0;
        d.Crew.Add(p);
        return d;
    }

    // ---------------------------------------------------------------- save / load
    public static void Load()
    {
        D = NewData();
        if (!Persist) return;
        try
        {
            string js = PlayerPrefs.GetString(Key, "");
            if (!string.IsNullOrEmpty(js))
            {
                var d = JsonUtility.FromJson<Data>(js);
                if (d != null && d.Skills != null && d.Skills.Length == 12 && d.Upgrades != null && d.Upgrades.Length == 5 && d.ClassOwned != null && d.ClassOwned.Length == 5 && d.Crew != null && d.Crew.Count > 0)
                {
                    foreach (var c in d.Crew) if (c.Skill == null || c.Skill.Length != 6) c.Skill = new int[6];
                    if (d.Found == null) d.Found = new List<string>();
                    D = d;
                }
            }
        }
        catch (Exception) { D = NewData(); }
    }

    public static void Save()
    {
        if (Game.I != null && Game.I.Market != null) D.Gold = Game.I.Market.Gold;
        if (!Persist) return;
        try { PlayerPrefs.SetString(Key, JsonUtility.ToJson(D)); PlayerPrefs.Save(); } catch (Exception) { }
    }

    public static void ResetAll()
    {
        D = NewData();
        Save();
        Apply();
    }

    /// <summary>Starting a later chapter directly still gives the skill points a player would have earned.</summary>
    public static void EnsureBaseline(int chapter)
    {
        int want = (chapter - 1) * 6;
        if (D.Earned < want) { D.Points += want - D.Earned; D.Earned = want; }
    }

    // ---------------------------------------------------------------- queries
    public static bool Has(int skill) => D.Skills[skill];
    public static bool IsFound(string id) => D.Found.Contains(id);

    public static bool CanLearn(int skill)
    {
        if (D.Skills[skill]) return false;
        int tier = skill % 4;
        if (tier > 0 && !D.Skills[skill - 1]) return false;
        return D.Points >= SkillCost[tier];
    }

    public static bool Learn(int skill)
    {
        if (!CanLearn(skill)) return false;
        D.Points -= SkillCost[skill % 4];
        D.Skills[skill] = true;
        Apply(); Save();
        return true;
    }

    public static int Price(int baseCost) => Mathf.RoundToInt(baseCost * S.CostMul);

    public static bool BuyUpgrade(int u)
    {
        var m = Game.I.Market;
        if (D.Upgrades[u] >= 3) return false;
        int cost = Price(UpgradeCost[D.Upgrades[u]]);
        if (m.Gold < cost) return false;
        m.Gold -= cost;
        D.Upgrades[u]++;
        Apply(); Save();
        return true;
    }

    public static bool BuyOrSelectClass(int c)
    {
        var m = Game.I.Market;
        if (!D.ClassOwned[c])
        {
            int cost = Price(ClassCost[c]);
            if (m.Gold < cost) return false;
            m.Gold -= cost;
            D.ClassOwned[c] = true;
        }
        D.Class = c;
        Apply(); Save();
        return true;
    }

    public static bool Hire(CrewMember c)
    {
        var m = Game.I.Market;
        int cost = Price(c.HireCost);
        if (D.Crew.Count >= 6 || m.Gold < cost) return false;
        m.Gold -= cost;
        c.Loyalty = 55f;
        c.Station = -1;
        for (int s = 0; s < 6; s++) if (StationHolder(s) == null && c.Skill[s] > 0 && (c.Station < 0 || c.Skill[s] > c.Skill[c.Station])) c.Station = s;
        D.Crew.Add(c);
        Apply(); Save();
        return true;
    }

    public static List<CrewMember> Candidates(string port)
    {
        var list = new List<CrewMember>();
        foreach (var c in Pool())
        {
            if (c.Port != port) continue;
            bool have = false;
            foreach (var k in D.Crew) if (k.Name == c.Name) have = true;
            if (!have) list.Add(c);
        }
        return list;
    }

    public static CrewMember StationHolder(int station)
    {
        foreach (var c in D.Crew) if (c.Station == station) return c;
        return null;
    }

    /// <summary>Moves a crew member to their next free station (or to rest).</summary>
    public static void CycleStation(CrewMember c)
    {
        for (int i = 0; i < 7; i++)
        {
            c.Station = c.Station >= 5 ? -1 : c.Station + 1;
            if (c.Station < 0 || StationHolder2(c.Station, c) == null) break;
        }
        Apply(); Save();
    }

    static CrewMember StationHolder2(int station, CrewMember except)
    {
        foreach (var c in D.Crew) if (c != except && c.Station == station) return c;
        return null;
    }

    public static int WageBill()
    {
        int w = 0;
        foreach (var c in D.Crew) w += c.Wage;
        return w;
    }

    public static bool PayWages()
    {
        var m = Game.I.Market;
        int w = WageBill();
        if (m.Gold < w) return false;
        m.Gold -= w;
        Loyalty(18f);
        Save();
        return true;
    }

    public static bool Feast()
    {
        var m = Game.I.Market;
        if (m.Gold < 30) return false;
        m.Gold -= 30;
        Loyalty(10f);
        Save();
        return true;
    }

    public static float AverageLoyalty()
    {
        if (D.Crew.Count == 0) return 0;
        float s = 0;
        foreach (var c in D.Crew) s += c.Loyalty;
        return s / D.Crew.Count;
    }

    public static void Loyalty(float delta)
    {
        foreach (var c in D.Crew) c.Loyalty = Mathf.Clamp(c.Loyalty + delta, 0f, 100f);
        Apply();
    }

    static float Bonus(int station)
    {
        var c = StationHolder(station);
        if (c == null) return 0f;
        return c.Skill[station] * Mathf.Lerp(0.3f, 1.1f, c.Loyalty / 100f);
    }

    // ---------------------------------------------------------------- events
    public static void AddPoints(int n, string why)
    {
        D.Points += n; D.Earned += n;
        if (HUD.I != null && !string.IsNullOrEmpty(why)) HUD.I.Toast("+" + n + " SKILL POINT  ·  " + why + "  ·  press TAB", UIKit.Teal);
    }

    /// <summary>Called when a story beat is completed: a skill point, and wages fall due.</summary>
    public static void OnBeatDone()
    {
        D.Points++; D.Earned++;
        Loyalty(-2f);
        // a crew member who has lost all faith leaves at the next chance
        for (int i = D.Crew.Count - 1; i >= 0; i--)
        {
            var c = D.Crew[i];
            if (c.Name != "Paachu" && c.Loyalty <= 4f)
            {
                D.Crew.RemoveAt(i);
                if (HUD.I != null) HUD.I.Toast(c.Name + " has left the ship. Unpaid crews do not stay.", UIKit.Red);
            }
        }
        Apply(); Save();
    }

    public static void OnVictory()
    {
        D.Victories++;
        D.Points++; D.Earned++;
        Loyalty(6f);
        Save();
    }

    public static void OnDefeat() { Loyalty(-6f); Save(); }

    public static void MarkFound(string id)
    {
        if (!D.Found.Contains(id)) { D.Found.Add(id); D.Discoveries++; }
        Save();
    }

    // ---------------------------------------------------------------- stats
    public static void Apply()
    {
        var s = new Stats { SpeedMul = 1f, TurnMul = 1f, MaxHP = 100f, DamageTaken = 1f, VolleyDmgMul = 1f, ReloadMul = 1f, EraDrainMul = 1f, RepairMul = 1f, BuyMul = 1f, SellMul = 1f, RamMul = 1f, RamSelfMul = 1f, DriftMul = 1f, CostMul = 1f, Cargo = 20, Volley = 7 };
        // skills
        if (Has(0)) s.SpeedMul *= 1.06f;
        if (Has(1)) s.DriftMul = 0.5f;
        if (Has(2)) s.EraDrainMul *= 1f / 1.3f;
        if (Has(3)) { s.TurnMul *= 1.12f; s.SpeedMul *= 1.04f; }
        if (Has(4)) s.BuyMul *= 0.92f;
        if (Has(5)) s.SellMul *= 1.08f;
        if (Has(6)) s.Cargo += 6;
        if (Has(7)) s.CostMul = 0.75f;
        if (Has(8)) s.Volley += 1;
        if (Has(9)) s.ReloadMul *= 0.8f;
        if (Has(10)) s.DamageTaken *= 0.85f;
        if (Has(11)) { s.RamMul = 1.6f; s.RamSelfMul = 0.5f; }
        // upgrades
        s.MaxHP += 20f * D.Upgrades[0];
        s.SpeedMul *= 1f + 0.04f * D.Upgrades[1];
        s.TurnMul *= 1f + 0.08f * D.Upgrades[2];
        s.Cargo += 4 * D.Upgrades[3];
        s.VolleyDmgMul *= 1f + 0.10f * D.Upgrades[4];
        // class
        switch (D.Class)
        {
            case 1: s.SpeedMul *= 1.12f; s.TurnMul *= 1.05f; s.MaxHP -= 20f; s.Cargo -= 4; break;
            case 2: s.Cargo += 14; s.MaxHP += 30f; s.SpeedMul *= 0.9f; s.TurnMul *= 0.85f; break;
            case 3: s.Volley += 2; s.MaxHP += 50f; s.DamageTaken *= 0.9f; s.Cargo -= 8; break;
            case 4: s.EraDrainMul *= 0.8f; s.SpeedMul *= 1.05f; s.TurnMul *= 1.1f; s.MaxHP -= 10f; s.Volley -= 1; break;
        }
        // crew at their stations (stronger when loyal)
        s.TurnMul *= 1f + 0.02f * Bonus(0);
        s.SpeedMul *= 1f + 0.012f * Bonus(1);
        s.EraDrainMul *= 1f - 0.06f * Bonus(2);
        s.VolleyDmgMul *= 1f + 0.05f * Bonus(3);
        s.RepairMul *= 1f + 0.25f * Bonus(4);
        float q = 0.025f * Bonus(5);
        s.BuyMul *= 1f - q; s.SellMul *= 1f + q;
        S = s;

        var g = Game.I;
        if (g == null || g.Player == null) return;
        var p = g.Player;
        float hpFrac = p.MaxHP > 0 ? p.HP / p.MaxHP : 1f;
        p.MaxHP = s.MaxHP;
        if (!p.Dead) p.HP = Mathf.Max(1f, s.MaxHP * hpFrac);
        p.MaxSpeed = 15.5f * (g.Tutorial ? 1.3f : 1f) * s.SpeedMul;
        p.TurnMul = s.TurnMul;
        p.DamageTaken = (g.Tutorial ? 0.45f : 1f) * s.DamageTaken;
        p.DriftMul = s.DriftMul;
        g.Market.Capacity = Mathf.Max(g.Market.Cargo, s.Cargo);
        ShipLooks.Apply(p);
    }
}
