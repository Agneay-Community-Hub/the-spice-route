using System.Collections;
using UnityEngine;

/// <summary>Chapter III — The Great Marketplace.</summary>
public partial class Story
{
    public bool FatherFreed, Escaped;
    public int BarsCrossed;
    bool tideOn;
    float tideClock;

    IEnumerator Beat3(int s, bool restart)
    {
        switch (s)
        {
            case 0: return Port3(restart);
            case 1: return RedSea3(restart);
            case 2: return Canal3(restart);
            case 3: return Alex3(restart);
            case 4: return Rescue3(restart);
            default: return Escape3(restart);
        }
    }

    void Update()
    {
        if (G == null || G.Chapter != 3) return;
        if (tideOn)
        {
            tideClock += Time.deltaTime;
            World.Tide = 0.4f + 1.25f * Mathf.Sin(tideClock * Mathf.PI * 2f / 44f - 1.2f);
        }
        else World.Tide = Mathf.MoveTowards(World.Tide, 0f, Time.deltaTime * 0.5f);
    }

    public static bool TideHigh => World.Tide > 1.0f;

    public static float NearestBar(Vector3 p, out Vector3 bar)
    {
        float best = 1e9f; bar = Vector3.zero;
        foreach (var b in World.SandBars)
        {
            float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(b.x, b.z));
            if (d < best) { best = d; bar = b; }
        }
        return best;
    }

    // ------------------------------------------------------------------ 0. Berenike
    IEnumerator Port3(bool restart)
    {
        G.State = Game.GameState.Cutscene;
        G.BaseTrack = AudioMan.Track.Calm;
        H.SetHudVisible(false);
        H.Letterbox(true);
        Environment(SkyController.Preset.Dawn, 0, 0, 1f, 0.1f, restart ? 0 : 2f);
        G.PlaceShip(World.P("start"), 90f);
        G.Player.Frozen = true;
        EraShift.I.Unlocked = true;
        Vector3 pp = G.Player.transform.position;
        Cam.Shot(pp + new Vector3(60, 12, 40), pp + new Vector3(-10, 5, -4), 42, restart ? 0.01f : 2.5f);
        Cam.Drift(new Vector3(-0.8f, 0.1f, -0.6f));
        yield return new WaitForSeconds(1f);
        yield return SayWait("Arivan", "Alexandria. The ledger says he is held there, Paachu. Alive.");
        yield return SayWait("Paachu", "By caravan, that's twelve days across the desert to the Nile. The Cartographers will have moved him by then.");
        yield return SayWait("Arivan", "Not by caravan. By the Canal of the Pharaohs.");
        yield return SayWait("Paachu", "That canal has been sand for a hundred years, boy.");
        Cam.Shot(pp + new Vector3(-20, 8, 26), pp + new Vector3(4, 5, -2), 46, 2.5f);
        yield return SayWait("Arivan", "The sea remembers it. And so does the compass.");
        Gameplay();
    }

    // ------------------------------------------------------------------ 1. north up the Red Sea
    IEnumerator RedSea3(bool restart)
    {
        if (restart) { Environment(SkyController.Preset.Dawn, 0, 0, 1f, 0.1f, 0); G.PlaceShip(World.P("start"), 90f); Gameplay(); }
        SkyController.I.Go(SkyController.Preset.Day, 80f);
        Wind.ShiftTo(-10f, 1.05f, 6f);
        Vector3 mouth = World.P("canal_mouth");
        Objective("Sail north up the Red Sea to where the old canal began", mouth, "Canal mouth  ·  ");
        H.Hint("W raise sail  ·  A / D steer  ·  T auto-steer  ·  M chart", 9f);
        bool shown = false;
        while (Dist(mouth) > 90f)
        {
            if (!shown && Dist(mouth) < 420f)
            {
                shown = true;
                Say("Paachu", "Nothing but dunes ahead. Where's your canal?");
                Say("Arivan", "Watch.");
                H.Hint("Press SHIFT: the Era Shift shows the obelisks that once lined the canal", 10f);
            }
            yield return null;
        }
        H.Banner("THE CANAL OF THE PHARAOHS", "Dug by the old kings, swallowed by the sand");
        Say("Paachu", "By the gods... there's still water in it. Shallow, but water.");
        yield return WaitTalk();
    }

    // ------------------------------------------------------------------ 2. the canal and the tide
    IEnumerator Canal3(bool restart)
    {
        if (restart) { Environment(SkyController.Preset.Day, 0, -10, 1f, 0.1f, 0); G.PlaceShip(World.P("canal_restart"), 0f); Gameplay(); }
        tideOn = true;
        tideClock = 0f;
        float oldDmg = G.Player.DamageTaken;
        G.Player.DamageTaken = 0.25f;
        H.ShowTide = true;
        yield return new WaitForSeconds(1.5f);
        H.Banner("TIDE SHIFT", "The sea breathes in and out of the canal");
        Say("Arivan", "Sandbars. At low tide they'll ground us. At high tide we float right over.");
        H.Hint("Watch the TIDE gauge at the top. Stop before a sandbar (S) and cross only when the tide is HIGH", 14f);
        Vector3 exit = World.P("canal_exit");
        BarsCrossed = 0;
        while (Dist(exit) > 80f)
        {
            Vector3 pp = G.Player.transform.position;
            Objective("Follow the canal north to the sea  (cross sandbars at HIGH tide)", AheadOnPath(World.Canal, pp, 120f), "Canal  ·  ");
            yield return null;
        }
        tideOn = false;
        H.ShowTide = false;
        G.Player.DamageTaken = oldDmg;
        Say("Paachu", "Salt wind! That's the Great Sea. And that tower on the horizon...");
        Say("Arivan", "The Pharos of Alexandria.");
        yield return WaitTalk();
    }

    // ------------------------------------------------------------------ 3. Alexandria
    IEnumerator Alex3(bool restart)
    {
        if (restart) { Environment(SkyController.Preset.Afternoon, 0, 20, 1f, 0.1f, 0); G.PlaceShip(World.P("alex_approach"), 180f); Gameplay(); }
        SkyController.I.Go(SkyController.Preset.Afternoon, 40f);
        H.Banner("ALEXANDRIA", "The Great Marketplace of the world");
        Vector3 dock = World.P("dock_alex");
        Objective("Bring the Kaveri alongside the Alexandria pier", dock, "Pier  ·  ");
        while (Dist(dock) > 70f) yield return null;
        G.Market.SetAlexandria();
        bool done = false;
        Vector3 market = World.P("alex_market");
        Say("Paachu", "Half the world's ships, and all of them selling. Let's sell our pepper and incense first.");
        G.SetInteraction("[E]   Trade at the Great Marketplace", market, 50f, () => G.OpenMarket(() => done = true));
        Objective("Sell your cargo at the Great Marketplace", market, "Market  ·  ");
        while (!done) yield return null;
        G.ClearInteraction();
        Say("Theon", "Looking for someone, Malabar man? In Alexandria, answers cost 400 drachmae.");
        bool paid = false;
        G.SetInteraction("[E]   Pay Theon the harbour clerk (400 gold)", market, 50f, () => paid = true);
        Objective("Pay Theon the harbour clerk for information", market, "Theon  ·  ");
        while (!paid) yield return null;
        G.ClearInteraction();
        int pay = Mathf.Min(400, G.Market.Gold);
        G.Market.Gold -= pay;
        AudioMan.I.Play("coin", 0.8f);
        H.Toast("-" + pay + " gold", UIKit.Gold);
        G.State = Game.GameState.Cutscene;
        G.Player.SailLevel = 0;
        H.SetHudVisible(false);
        H.Letterbox(true);
        Vector3 pp = G.Player.transform.position;
        Cam.Shot(pp + new Vector3(26, 9, -20), pp + new Vector3(-4, 5, 6), 40, 2f);
        Cam.Drift(new Vector3(0.6f, 0, 0.5f));
        yield return SayWait("Theon", "A Malabar navigator, seven years a prisoner. They keep him aboard the Iron Ledger, the galley moored beneath the Pharos.");
        yield return SayWait("Theon", "He maps routes for them in chains. The great light sweeps that water all night. Go dark, and go fast.");
        yield return SayWait("Arivan", "Tonight, then.");
        H.FadeTo(1f, 1.2f);
        yield return new WaitForSeconds(1.4f);
    }

    // ------------------------------------------------------------------ 4. the rescue
    IEnumerator Rescue3(bool restart)
    {
        Environment(SkyController.Preset.Night, 0, 20, 0.9f, 0.08f, 0);
        G.BaseTrack = AudioMan.Track.Storm;
        G.PlaceShip(World.P("night_start"), 60f);
        G.Player.SetLantern(true);
        G.ClearEnemies();
        var st = Stealth.I;
        st.ResetState();
        spottedHandled = false;
        Unseen = true;
        st.OnSpotted = () => StartCoroutine(Spotted());
        H.FadeTo(0f, 1.5f);
        H.Banner("NIGHT IN ALEXANDRIA", "The Pharos watches the harbour");
        Gameplay();
        H.ShowStealth = true;
        st.Active = true;
        yield return new WaitForSeconds(1f);
        Say("Paachu", "That beam turns like a millstone. It sees everything it touches.");
        Say("Arivan", "Then we stay where it isn't. Lantern out. Around the island, to the galley.");
        H.Hint("Press L to douse your lantern  ·  dodge the Pharos beam and the patrol lantern pools  ·  slower sails are quieter", 14f);
        Vector3 prison = World.P("prison");
        bool freed = false;
        G.SetInteraction("[E]   Cut Raghavan free", prison, 60f, () => freed = true);
        while (!freed)
        {
            Objective("Reach the Iron Ledger and free your father  [E]", prison, "Iron Ledger  ·  ");
            yield return null;
        }
        G.ClearInteraction();
        FatherFreed = true;
        st.Active = false;
        H.ShowStealth = false;
        AudioMan.I.Play("discovery", 1f);
        H.Banner("RAGHAVAN", "Seven years. Seven seas.");
        G.State = Game.GameState.Cutscene;
        G.Player.SailLevel = 0;
        H.Letterbox(true);
        Vector3 pp = G.Player.transform.position;
        Cam.Shot(pp + new Vector3(18, 7, -18), pp + new Vector3(0, 4, 2), 38, 2f);
        yield return SayWait("Raghavan", "Arivan? No... the sea is playing tricks on an old man.");
        yield return SayWait("Arivan", "No tricks, father. The compass brought me. Come. We are leaving.");
        H.Letterbox(false);
        G.State = Game.GameState.Sailing;
        Cam.Gameplay(1f);
    }

    // ------------------------------------------------------------------ 5. escape
    IEnumerator Escape3(bool restart)
    {
        if (restart)
        {
            Environment(SkyController.Preset.Night, 0, 20, 0.9f, 0.08f, 0);
            G.PlaceShip(World.P("prison") + new Vector3(40, 0, 30), 30f);
            G.ClearEnemies();
            FatherFreed = true;
            Gameplay();
        }
        AudioMan.I.Play("conch", 1f);
        AudioMan.I.PlayDelayed("conch", 1.6f, 0.7f, 0.9f);
        H.Banner("THE HARBOUR WAKES", "Run for the open sea!");
        G.BaseTrack = AudioMan.Track.Combat;
        Say("Raghavan", "They've seen the galley. Every Cartographer ship in Alexandria will be after us!");
        Say("Paachu", "Then give me full sail and a clear sea. North, out past the island!");
        Vector3 me = G.Player.Planar;
        if (G.Raider == null || !G.Raider.gameObject.activeInHierarchy || G.Raider.Dead)
        {
            Vector3 a = World.P("raider_harbour");
            G.SpawnRaider(a, Mathf.Atan2(me.x - a.x, me.z - a.z) * Mathf.Rad2Deg);
            G.Raider.MaxHP = G.Raider.HP = 100f;
        }
        G.RaiderAI.VolleyDamage = 1.7f;
        Vector3 b = World.P("raider_north");
        G.SpawnFlagship(b, Mathf.Atan2(me.x - b.x, me.z - b.z) * Mathf.Rad2Deg, 100f);
        G.FlagshipAI.VolleyDamage = 1.7f;
        G.CombatEnabled = true;
        H.EnemyTarget = G.CurrentEnemy;
        H.EnemyName = "CARTOGRAPHER RAIDER";
        H.Hint("Full sail north to the open sea  ·  or fight: get them beside you and LEFT CLICK", 12f);
        Vector3 target = World.P("escape_target");
        while (true)
        {
            Objective("Escape the harbour to the open sea", target, "Open sea  ·  ");
            H.EnemyTarget = G.CurrentEnemy;
            bool bothDown = (G.Raider == null || G.Raider.Dead || !G.Raider.gameObject.activeInHierarchy) && (G.Flagship == null || G.Flagship.Dead || !G.Flagship.gameObject.activeInHierarchy);
            if (Dist(target) < 120f || bothDown) break;
            yield return null;
        }
        Escaped = true;
        RaiderSunk = G.Raider != null && G.Raider.Dead;
        FlagshipSunk = G.Flagship != null && G.Flagship.Dead;
        G.CombatEnabled = false;
        H.EnemyTarget = null;
        if (G.RaiderAI != null) G.RaiderAI.Active = false;
        if (G.FlagshipAI != null) G.FlagshipAI.Active = false;
        AudioMan.I.Play("victory", 1f);
        H.Banner("FREE", "Alexandria falls away behind you");
        G.BaseTrack = AudioMan.Track.Calm;
        yield return new WaitForSeconds(2f);
    }

    // ------------------------------------------------------------------ ending
    IEnumerator Ending3()
    {
        G.State = Game.GameState.Cutscene;
        H.SetHudVisible(false);
        H.Letterbox(true);
        H.ClearDialogue();
        G.Player.SailLevel = 1;
        SkyController.I.Go(SkyController.Preset.Dawn, 6f);
        G.BaseTrack = AudioMan.Track.Calm;
        Cam.Orbit(G.Player.transform.position, 34f, 9f, 4f, 200f, 3f, 42f);
        yield return new WaitForSeconds(1.5f);
        yield return SayWait("Raghavan", "You crossed the Gate of Tears. You sailed the old kings' canal. Your mother would not believe it.");
        yield return SayWait("Arivan", "The chart, father. The passage beyond the edge of every map. Is it real?");
        yield return SayWait("Raghavan", "It is real. And now the Cartographers know we will go looking for it.");
        yield return SayWait("Arivan", "Then we had better get there first.");
        G.State = Game.GameState.Ending;
        G.Player.Frozen = true;
        string text = "As dawn broke over the Great Sea, the Kaveri carried two navigators of Muziris away from Alexandria.\n\nFather and son, reunited after seven years.\n\nBetween them lay a bronze compass, a chart of a passage no one had ever sailed, and every Cartographer fleet between here and the edge of the world.\n\nThe sea remembers every journey. Theirs is only beginning.";
        float t = G.PlayTime;
        int profit = Mathf.RoundToInt(G.Market.Earned - G.Market.Spent);
        string stats = "VOYAGE TIME        " + Mathf.FloorToInt(t / 60f) + "m " + Mathf.FloorToInt(t % 60f).ToString("00") + "s\n" +
                       "MARKET SALES        " + Mathf.RoundToInt(G.Market.Earned) + " gold\n" +
                       "THE RESCUE           " + (Unseen ? "Unseen" : "Spotted") + "\n" +
                       "RAIDERS SUNK        " + ((RaiderSunk ? 1 : 0) + (FlagshipSunk ? 1 : 0)) + " / 2\n" +
                       "HULL                    " + Mathf.RoundToInt(G.Player.HP / G.Player.MaxHP * 100f) + "%\n\n" +
                       "<color=#7fe8e0>CONTINUE IN CHAPTER IV: ACROSS SEVEN SEAS</color>";
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        yield return H.PlayEnding(text, stats, "CONTINUE TO CHAPTER IV", () => G.LoadChapter(4));
    }
}
