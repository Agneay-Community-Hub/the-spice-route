using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Chapter II — The Gate of Tears.</summary>
public partial class Story
{
    public bool Unseen = true, FlagshipSunk;
    bool spottedHandled;

    IEnumerator Beat2(int s, bool restart)
    {
        switch (s)
        {
            case 0: return Port2(restart);
            case 1: return Market2(restart);
            case 2: return Fog2(restart);
            case 3: return Aden2(restart);
            case 4: return Night2(restart);
            case 5: return Wreck2(restart);
            default: return Boss2(restart);
        }
    }

    static Vector3 AheadOnPath(List<Vector3> path, Vector3 pos, float ahead)
    {
        float best = 1e9f, bestS = 0f, acc = 0f;
        Vector2 p = new Vector2(pos.x, pos.z);
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector2 a = new Vector2(path[i].x, path[i].z), b = new Vector2(path[i + 1].x, path[i + 1].z);
            Vector2 ab = b - a;
            float len = ab.magnitude;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / (len * len));
            float d = (a + ab * t - p).magnitude;
            if (d < best) { best = d; bestS = acc + t * len; }
            acc += len;
        }
        float target = Mathf.Min(acc, bestS + ahead);
        acc = 0f;
        for (int i = 0; i < path.Count - 1; i++)
        {
            float len = Vector3.Distance(path[i], path[i + 1]);
            if (acc + len >= target) return Vector3.Lerp(path[i], path[i + 1], (target - acc) / len);
            acc += len;
        }
        return path[path.Count - 1];
    }

    // ------------------------------------------------------------------ 0. Ras al-Mira
    IEnumerator Port2(bool restart)
    {
        G.State = Game.GameState.Cutscene;
        G.BaseTrack = AudioMan.Track.Calm;
        H.SetHudVisible(false);
        H.Letterbox(true);
        Environment(SkyController.Preset.Dawn, 0, -100, 0.95f, 0.1f, restart ? 0 : 2f);
        SkyController.I.FogMul = 1f;
        G.PlaceShip(World.P("start"), 180f);
        G.Player.Frozen = true;
        G.Player.SetLantern(true);
        EraShift.I.Unlocked = true;
        Vector3 dock = World.P("dock_ras");
        Cam.Shot(dock + new Vector3(55, 9, -70), dock + new Vector3(-8, 5, 10), 42, restart ? 0.01f : 2.5f);
        Cam.Drift(new Vector3(-1.2f, 0.1f, 0.4f));
        yield return new WaitForSeconds(1f);
        yield return SayWait("Yusuf", "Your father's chart has more than one mark, Arivan. The first lies west: Aden, and the strait beyond it.");
        yield return SayWait("Arivan", "The Gate of Tears.");
        yield return SayWait("Yusuf", "The Cartographers chain it with ships and watchfires. Few slip through. Find Zahra, the harbour scribe of Aden. She keeps their secrets better than they do.");
        Cam.Shot(G.Player.transform.position + new Vector3(-14, 12, 24), G.Player.transform.position + new Vector3(0, 6, -4), 46, 2.5f);
        Cam.Drift(new Vector3(0.6f, 0.1f, -0.3f));
        yield return SayWait("Paachu", "And fill the hold with frankincense. Aden burns it in every temple, and a trader draws fewer eyes than a captain on a quest.");
        Gameplay();
    }

    // ------------------------------------------------------------------ 1. trade
    IEnumerator Market2(bool restart)
    {
        if (restart) { Environment(SkyController.Preset.Dawn, 0, -100, 0.95f, 0.1f, 0); G.PlaceShip(World.P("start"), 180f); Gameplay(); }
        G.Market.SetRasAlMira2();
        Vector3 market = World.P("ras_market");
        Objective("Buy frankincense and myrrh at the Ras al-Mira market", market, "Market pier  ·  ");
        H.Hint("Click the screen to take the helm  ·  Press E beside the pier to trade", 9f);
        bool done = false;
        G.SetInteraction("[E]   Trade at the Ras al-Mira market", market, 45f, () => G.OpenMarket(() => done = true));
        while (!done)
        {
            if (Dist(World.P("dock_ras")) > 230f) { Say("Paachu", "An empty hold all the way to Aden? Brave. Or foolish."); break; }
            yield return null;
        }
        G.ClearInteraction();
        if (G.Market.Cargo > 0) Say("Paachu", G.Market.Cargo + " bundles of incense aboard. The whole ship smells like a temple.");
        yield return null;
    }

    // ------------------------------------------------------------------ 2. the fog coast
    IEnumerator Fog2(bool restart)
    {
        if (restart) { Environment(SkyController.Preset.Dawn, 0, -100, 1f, 0.1f, 0); G.PlaceShip(World.P("start") + new Vector3(0, 0, -50), 235f); Gameplay(); }
        SkyController.I.Go(SkyController.Preset.Day, 70f);
        Wind.ShiftTo(-95f, 1f, 8f);
        var era = EraShift.I;
        era.Unlocked = true;
        Vector3 aden = World.P("dock_aden");
        Objective("Sail west along the coast to Aden", World.P("fog_entry"), "Fog bank  ·  ");
        H.Hint("W raise sail  ·  A / D steer  ·  T auto-steer  ·  M chart", 8f);
        bool entered = false, cleared = false;
        foreach (var m in new[] { World.FogLamps }) if (m != null) m.GetComponent<Renderer>().sharedMaterial.SetFloat("_Base", 0.08f);
        while (true)
        {
            Vector3 pp = G.Player.transform.position;
            float x = pp.x;
            float k = Geo.S01((700f - x) / 90f) * Geo.S01((x - 10f) / 70f);
            if (!entered) k *= Geo.S01((800f - x) / 100f);
            SkyController.I.FogMul = Mathf.Lerp(SkyController.I.FogMul, 1f + 11f * k, Time.deltaTime * 1.5f);
            if (!entered && x < 690f)
            {
                entered = true;
                era.DrainRate = 1f / 30f;
                era.RechargeRate = 1f / 6f;
                H.Banner("WEATHER SHIFT", "A sea fog rolls in over the reefs");
                Say("Paachu", "Fog, thick as wool. And this coast is all teeth. Reefs everywhere.");
                Say("Arivan", "The old harbour masters lit beacons through here. The compass will remember them.");
                H.Hint("Press SHIFT: the Era Shift shows the ancient beacons that mark the safe channel through the reefs", 12f);
            }
            if (entered && !cleared)
            {
                Objective("Follow the ancient beacons through the fog  (SHIFT shows them)", AheadOnPath(World.FogChannel, pp, 110f), "Beacon  ·  ");
                if (x < 40f)
                {
                    cleared = true;
                    era.DrainRate = 1f / 10f;
                    era.RechargeRate = 1f / 11f;
                    Say("Paachu", "Clear water! And there: the crater of Aden.");
                }
            }
            if (cleared)
            {
                Objective("Sail into the harbour of Aden", aden, "Aden  ·  ");
                if (Dist(aden) < 160f) break;
            }
            yield return null;
        }
        SkyController.I.FogMul = 1f;
    }

    // ------------------------------------------------------------------ 3. Aden
    IEnumerator Aden2(bool restart)
    {
        if (restart) { Environment(SkyController.Preset.Day, 0, -95, 1f, 0.1f, 0); G.PlaceShip(World.P("aden_approach"), 270f); Gameplay(); }
        SkyController.I.FogMul = 1f;
        SkyController.I.Go(SkyController.Preset.Afternoon, 40f);
        H.Banner("ADEN", "The gateway of the Red Sea");
        H.KnownPlaces.Add("aden");
        Vector3 dock = World.P("dock_aden");
        Objective("Bring the Kaveri alongside the Aden pier", dock, "Pier  ·  ");
        while (Dist(dock) > 70f) yield return null;
        G.Market.SetAden();
        bool done = false;
        Vector3 market = World.P("aden_market");
        Say("Zahra", "Incense from Ras al-Mira? Then you are welcome in Aden, captain. Come, the temple merchants are waiting.");
        G.SetInteraction("[E]   Trade at the Aden market", market, 50f, () => G.OpenMarket(() => done = true));
        Objective("Sell your cargo at the Aden market", market, "Market  ·  ");
        while (!done) yield return null;
        G.ClearInteraction();
        G.State = Game.GameState.Cutscene;
        G.Player.SailLevel = 0;
        H.SetHudVisible(false);
        H.Letterbox(true);
        Vector3 pp = G.Player.transform.position;
        Cam.Shot(pp + new Vector3(30, 8, 26), pp + new Vector3(-6, 4, -2), 40, 2f);
        Cam.Drift(new Vector3(-0.8f, 0, 0.3f));
        yield return SayWait("Zahra", "Raghavan's son. You have his eyes, and his habit of arriving where you are not wanted.");
        yield return SayWait("Zahra", "Seven years ago the Cartographers' flagship, the Black Compass, caught your father in the Gate of Tears. His ship burned there.");
        yield return SayWait("Arivan", "And him?");
        yield return SayWait("Zahra", "That, the sea may tell you better than I can. Go tonight. The moon is thin. Douse your lantern and keep out of the watchfires' light.");
        H.FadeTo(1f, 1.2f);
        yield return new WaitForSeconds(1.4f);
    }

    // ------------------------------------------------------------------ 4. the night run
    IEnumerator Night2(bool restart)
    {
        Environment(SkyController.Preset.Night, 0, -95, 0.9f, 0.08f, 0);
        SkyController.I.FogMul = 1f;
        G.BaseTrack = AudioMan.Track.Storm;
        G.PlaceShip(World.P("stealth_start"), 270f);
        G.Player.SetLantern(true);
        G.ClearEnemies();
        var st = Stealth.I;
        st.ResetState();
        spottedHandled = false;
        st.OnSpotted = () => StartCoroutine(Spotted());
        H.FadeTo(0f, 1.5f);
        H.Banner("NIGHTFALL", "The Gate of Tears lies ahead");
        Gameplay();
        H.ShowStealth = true;
        st.Active = true;
        yield return new WaitForSeconds(1f);
        Say("Paachu", "Watchfires on Perim, and patrol boats with lanterns. They sweep the water like searching eyes.");
        Say("Arivan", "Then we go dark. Douse the lantern, reef the sail, and pass between the beams.");
        Objective("Slip through the Gate of Tears unseen", World.P("gate_target"), "Beyond the Gate  ·  ");
        H.Hint("Press L to douse your lantern  ·  stay out of the orange beams and lantern pools  ·  slower sails are quieter", 14f);
        Vector3 target = World.P("gate_target");
        while (true)
        {
            Vector3 pp = G.Player.transform.position;
            if (pp.x < target.x + 20f || Dist(target) < 70f) break;
            yield return null;
        }
        st.Active = false;
        H.ShowStealth = false;
        if (Unseen && !st.Spotted)
        {
            AudioMan.I.Play("victory", 0.6f);
            H.Banner("UNSEEN", "You slipped through the Gate of Tears");
            Say("Paachu", "Not a shout, not a horn. Your father would have laughed.");
        }
        else Say("Paachu", "We're through the Gate! Now find what the compass is pulling toward.");
        yield return WaitTalk();
    }

    IEnumerator Spotted()
    {
        if (spottedHandled) yield break;
        spottedHandled = true;
        Unseen = false;
        AudioMan.I.Play("conch", 1f);
        H.Banner("SPOTTED!", "A Cartographer raider is coming for you");
        G.BaseTrack = AudioMan.Track.Combat;
        Say("Paachu", "They've seen us! Raider closing. Fight her, or run for the far side of the Gate!");
        Vector3 pp = G.Player.Planar;
        Vector3 spawn = pp + new Vector3(-260f, 0, 0);
        if (World.LandHeight(spawn.x, spawn.z) > -6f) spawn = pp + new Vector3(220f, 0, 0);
        G.SpawnRaider(spawn, Mathf.Atan2(pp.x - spawn.x, pp.z - spawn.z) * Mathf.Rad2Deg);
        G.RaiderAI.VolleyDamage = G.Chapter == 3 ? 1.9f : 2.6f;
        G.CombatEnabled = true;
        H.EnemyTarget = G.Raider;
        H.EnemyName = "CARTOGRAPHER RAIDER";
        yield return null;
    }

    // ------------------------------------------------------------------ 5. echo of the Vellimeen
    IEnumerator Wreck2(bool restart)
    {
        if (restart)
        {
            Environment(SkyController.Preset.Night, 0, -95, 0.9f, 0.08f, 0);
            G.PlaceShip(World.P("gate_target") + new Vector3(30, 0, -20), 290f);
            G.ClearEnemies();
            Gameplay();
        }
        var era = EraShift.I;
        era.Unlocked = true;
        Vector3 wreck = World.P("wreck");
        Say("Arivan", "The compass is pulling. Hard. Toward that broken mast.");
        float progress = 0;
        bool done = false;
        while (!done)
        {
            float d = Dist(wreck);
            Objective("Find the echo of the Vellimeen  —  Era Shift beside the wreck", wreck, "Wreck  ·  ");
            G.EchoNearby = d < 170f;
            if (d < 95f)
            {
                if (era.Active)
                {
                    progress += Time.deltaTime / 3f;
                    H.Reading = "Reading the echo...  " + Mathf.RoundToInt(Mathf.Clamp01(progress) * 100f) + "%";
                    if (progress >= 1f) done = true;
                }
                else { H.Reading = "Press SHIFT to read the echo"; progress = Mathf.Max(0, progress - Time.deltaTime * 0.3f); }
            }
            else { H.Reading = ""; progress = Mathf.Max(0, progress - Time.deltaTime); }
            yield return null;
        }
        H.Reading = "";
        G.EchoNearby = false;
        Stones = 1;
        AudioMan.I.Play("discovery", 1f);
        FX.EraPulse(wreck);
        era.Energy = 1f;
        H.Banner("ECHO OF THE VELLIMEEN", "The night your father's ship burned");
        H.ClearDialogue();
        Say("Raghavan (echo)", "They have the ship, but not the chart. Yusuf's boat is away into the dark with it. Run, old friend, run!");
        Say("Raghavan (echo)", "Arivan, if this reaches you: they are not killing me. They need what I know. They are taking me north. To Alexandria.");
        Say("Arivan", "He's alive... he might still be alive.");
        yield return WaitTalk();
    }

    // ------------------------------------------------------------------ 6. the Black Compass
    IEnumerator Boss2(bool restart)
    {
        if (restart)
        {
            Environment(SkyController.Preset.Night, 0, -95, 0.9f, 0.08f, 0);
            G.PlaceShip(World.P("wreck") + new Vector3(-30, 0, 60), 300f);
            G.ClearEnemies();
            Gameplay();
        }
        SkyController.I.Go(SkyController.Preset.Dawn, 20f);
        yield return new WaitForSeconds(2f);
        Vector3 pp = G.Player.Planar;
        Vector3 bs = World.P("boss_spawn");
        G.SpawnFlagship(bs, Mathf.Atan2(pp.x - bs.x, pp.z - bs.z) * Mathf.Rad2Deg);
        AudioMan.I.Play("conch", 1f);
        yield return new WaitForSeconds(1.2f);
        H.Banner("THE BLACK COMPASS", "Flagship of the Hidden Cartographers");
        G.BaseTrack = AudioMan.Track.Combat;
        Say("Paachu", "Black sails at dawn. That's her. That's the ship that took your father.");
        Say("Arivan", "Then it ends here. All hands! Fire-arrows to both rails!");
        G.CombatEnabled = true;
        H.EnemyTarget = G.Flagship;
        H.EnemyName = "THE BLACK COMPASS";
        H.Hint("Get beside her and LEFT CLICK to fire  ·  ram her at full sail  ·  your hull repairs after 8 s without hits", 12f);
        bool escortCalled = G.Raider != null && G.Raider.gameObject.activeInHierarchy && !G.Raider.Dead;
        while (!G.Flagship.Dead)
        {
            Objective("Sink the Black Compass", G.Flagship.transform.position, "Black Compass  ·  ");
            if (!escortCalled && G.Flagship.HP < G.Flagship.MaxHP * 0.5f)
            {
                escortCalled = true;
                Vector3 me = G.Player.Planar, es = World.P("escort_spawn");
                if (Vector3.Distance(me, es) < 200f) es = me + (me - G.Flagship.Planar).normalized * 320f;
                if (World.LandHeight(es.x, es.z) > -6f) es = World.P("boss_spawn");
                G.SpawnRaider(es, Mathf.Atan2(me.x - es.x, me.z - es.z) * Mathf.Rad2Deg);
                G.Raider.MaxHP = G.Raider.HP = 80f;
                G.RaiderAI.VolleyDamage = 2.0f;
                AudioMan.I.Play("conch", 0.8f);
                H.Toast("An escort raider joins the fight!", UIKit.Red);
                Say("Paachu", "She's signalling for help! Another raider. Finish the flagship first!");
            }
            if (!G.Flagship.gameObject.activeInHierarchy) break;
            yield return null;
        }
        FlagshipSunk = true;
        RaiderSunk = G.Raider != null && G.Raider.Dead;
        H.EnemyTarget = null;
        G.BaseTrack = AudioMan.Track.Mystery;
        AudioMan.I.Play("victory", 1f);
        H.Banner("THE BLACK COMPASS SINKS", "The Gate of Tears is open");
        if (G.RaiderAI != null) G.RaiderAI.Active = false;
        Say("Paachu", "She's going down! Look, they're abandoning her. The captain's chest is floating free!");
        yield return new WaitForSeconds(4f);
        G.CombatEnabled = false;
        yield return WaitTalk();
    }

    // ------------------------------------------------------------------ ending
    IEnumerator Ending2()
    {
        G.State = Game.GameState.Cutscene;
        H.SetHudVisible(false);
        H.Letterbox(true);
        H.ClearDialogue();
        G.Player.SailLevel = 0;
        G.BaseTrack = AudioMan.Track.Mystery;
        Cam.Orbit(G.Player.transform.position, 36f, 10f, 5f, 120f, 3f, 42f);
        yield return new WaitForSeconds(1f);
        yield return SayWait("Paachu", "Ledgers. Every ship they ever seized, every captain they ever took.");
        yield return SayWait("Arivan", "Vellimeen. Captured at the Gate. Raghavan of Muziris, prisoner. Sent north, to Alexandria.");
        yield return SayWait("Arivan", "He's alive, Paachu. Set a course north.");
        G.State = Game.GameState.Ending;
        G.Player.Frozen = true;
        string text = "In the captain's chest of the Black Compass, Arivan found a ledger of every ship the Hidden Cartographers had ever seized.\n\nThe Vellimeen was there. So was a name: Raghavan of Muziris. Prisoner. Sent north, to Alexandria.\n\nHis father is alive.\n\nThe Red Sea lies open. The Great Marketplace awaits.";
        float t = G.PlayTime;
        int profit = Mathf.RoundToInt(G.Market.Earned - G.Market.Spent);
        string stats = "VOYAGE TIME        " + Mathf.FloorToInt(t / 60f) + "m " + Mathf.FloorToInt(t % 60f).ToString("00") + "s\n" +
                       "TRADE PROFIT        " + (profit >= 0 ? "+" : "") + profit + " gold\n" +
                       "THE GATE              " + (Unseen ? "Passed unseen" : "Spotted") + "\n" +
                       "THE BLACK COMPASS  " + (FlagshipSunk ? "Sunk" : "Escaped") + "\n" +
                       "HULL                    " + Mathf.RoundToInt(G.Player.HP / G.Player.MaxHP * 100f) + "%\n\n" +
                       "<color=#7fe8e0>TO BE CONTINUED IN CHAPTER III: THE GREAT MARKETPLACE</color>";
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        yield return H.PlayEnding(text, stats, "CONTINUE TO CHAPTER III", () => G.LoadChapter(3));
    }
}
