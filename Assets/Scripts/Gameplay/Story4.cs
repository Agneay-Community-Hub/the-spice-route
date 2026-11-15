using System.Collections;
using UnityEngine;

/// <summary>Chapter IV — Across Seven Seas. The Guild fleet, the Storm Wall, the Sunken Archive and the final choice.</summary>
public partial class Story
{
    public bool FleetJoined, ArchiveRead, LastMapSunk;
    public int Choice = -1;

    public static readonly string[] ChoiceTitles = { "MERCHANT KING", "GUARDIAN OF THE SEAS", "THE GREAT NAVIGATOR", "THE BROKEN ROUTE" };
    public static readonly string[] ChoiceText =
    {
        "Claim the Archive's routes for Muziris. Your city will rule the spice trade for a hundred years.",
        "Seal the Archive and guard its secret. No one will ever own the sea again, not even you.",
        "Copy every chart and give them freely to every port. The sea will belong to everyone.",
        "Destroy the Archive, and the compass with it. The old routes vanish; new ones must be found.",
    };

    IEnumerator Beat4(int s, bool restart)
    {
        switch (s)
        {
            case 0: return Home4(restart);
            case 1: return Fleet4(restart);
            case 2: return Storm4(restart);
            case 3: return Archive4(restart);
            case 4: return Battle4(restart);
            default: return Choice4(restart);
        }
    }

    static void PlaceAllies(bool following)
    {
        var g = G;
        if (g.Allies == null) return;
        AllyAI.ResetTrail();
        if (following) AllyAI.SeedTrail(g.Player.Planar, g.Player.Fwd);
        for (int i = 0; i < g.Allies.Length; i++)
        {
            var a = g.Allies[i];
            var ai = g.AllyAIs[i];
            a.gameObject.SetActive(true);
            a.ResetState(a.MaxHP);
            a.ClearFires();
            if (following)
            {
                Vector3 p = g.Player.Planar - g.Player.Fwd * ai.FollowDist;
                if (World.LandHeight(p.x, p.z) > -4f) p = g.Player.Planar + g.Player.Right * (i == 0 ? 45f : -45f);
                a.Teleport(p, g.Player.Heading);
                ai.Waiting = -1f;
            }
            else
            {
                Vector3 w = World.P("fleet_rendezvous") + new Vector3(i == 0 ? -45f : 45f, 0, -25f);
                a.Teleport(w, 200f);
                ai.WaitPos = w;
                ai.Waiting = 200f;
            }
            ai.Active = true;
        }
    }

    static int AlliesAfloat()
    {
        int n = 0;
        if (G.Allies != null) foreach (var a in G.Allies) if (a != null && !a.Dead) n++;
        return n;
    }

    static float StormAmount(Vector3 p)
    {
        return Geo.S01((-640f - p.z) / 170f) * Geo.S01((p.z + 1890f) / 140f) * Geo.S01((p.x + 1500f) / 220f) * Geo.S01((-120f - p.x) / 220f);
    }

    // ------------------------------------------------------------------ 0. homecoming
    IEnumerator Home4(bool restart)
    {
        G.State = Game.GameState.Cutscene;
        G.BaseTrack = AudioMan.Track.Calm;
        H.SetHudVisible(false);
        H.Letterbox(true);
        Environment(SkyController.Preset.Dawn, 0, -75, 0.95f, 0.1f, restart ? 0 : 2f);
        G.PlaceShip(World.P("start"), 270f);
        G.Player.Frozen = true;
        EraShift.I.Unlocked = true;
        PlaceAllies(false);
        Vector3 dock = World.P("dock_muziris");
        Cam.Shot(dock + new Vector3(-75, 10, -60), dock + new Vector3(15, 6, 4), 42, 0.01f);
        Cam.Drift(new Vector3(0.4f, 0.1f, 1.6f));
        yield return new WaitForSeconds(1.2f);
        yield return SayWait("Devaki", "Raghavan. Seven years, and the sea gives you back to us.");
        yield return SayWait("Raghavan", "It gave me back a son who sails better than I ever did, Devaki.");
        Cam.Shot(dock + new Vector3(-26, 5, 15), dock + new Vector3(6, 4, -3), 38, 2.5f);
        Cam.Drift(new Vector3(0.9f, 0, 0.2f));
        yield return SayWait("Devaki", "The Merchants' Guild has heard everything. Two of our ships are waiting for you at the Cape of Lanterns.");
        yield return SayWait("Raghavan", "The chart ends south of Kadal, at the Storm Wall. A storm that has never ended. Every captain who tried to cross it was lost.");
        Cam.Shot(G.Player.transform.position + new Vector3(8, 14, -26), G.Player.transform.position + new Vector3(0, 6, 4), 46, 2.5f);
        Cam.Drift(new Vector3(-0.6f, 0.2f, 0.6f));
        yield return SayWait("Arivan", "Every captain without this compass. Beyond the wall lies the Sunken Archive, and the Cartographers are already sailing for it.");
        yield return SayWait("Paachu", "Then let's not keep the Guild waiting. Raise the sail, captain!");
        Gameplay();
    }

    // ------------------------------------------------------------------ 1. the fleet
    IEnumerator Fleet4(bool restart)
    {
        if (restart) { Environment(SkyController.Preset.Dawn, 0, -75, 0.95f, 0.1f, 0); G.PlaceShip(World.P("start"), 270f); PlaceAllies(false); Gameplay(); }
        SkyController.I.Go(SkyController.Preset.Day, 60f);
        Vector3 rv = World.P("fleet_rendezvous");
        Objective("Meet the Guild ships at the Cape of Lanterns", rv, "Guild fleet  ·  ");
        H.Hint("W raise sail  ·  A / D steer  ·  T auto-steer to the gold diamond  ·  M chart", 10f);
        while (Dist(rv) > 150f) yield return null;
        FleetJoined = true;
        AllyAI.ResetTrail();
        AllyAI.SeedTrail(G.Player.Planar, G.Player.Fwd);
        foreach (var ai in G.AllyAIs) ai.Waiting = -1f;
        AudioMan.I.Play("conch", 0.9f);
        H.Banner("THE GUILD FLEET", "The Thirayar and the Mayil join the Kaveri");
        Say("Ammu (Thirayar)", "Captain Arivan! The Guild sails with you. Lead, and we will follow your wake.");
        Say("Arivan", "Stay in my wake, exactly in it. Where we are going, a ship's length off course is a ship lost.");
        Say("Raghavan", "South now, past Kadal. When the sky turns black, we are there.");
        Wind.ShiftTo(-150f, 1.05f, 14f);
        yield return WaitTalk();
    }

    // ------------------------------------------------------------------ 2. the Storm Wall
    IEnumerator Storm4(bool restart)
    {
        if (restart)
        {
            Environment(SkyController.Preset.Day, 0, -150, 1.05f, 0.12f, 0);
            G.PlaceShip(World.P("fleet_rendezvous") + new Vector3(0, 0, -80), 200f);
            FleetJoined = true;
            PlaceAllies(true);
            Gameplay();
        }
        var era = EraShift.I;
        era.Unlocked = true;
        Vector3 entry = World.P("storm_entry");
        Vector3 end = World.Passage[World.Passage.Count - 1];
        H.KnownPlaces.Add("storm");
        if (World.PassageLamps != null) World.PassageLamps.GetComponent<Renderer>().sharedMaterial.SetFloat("_Base", 0.1f);
        bool entered = false, through = false;
        float pushToast = 0;
        while (!through)
        {
            Vector3 pp = G.Player.transform.position;
            float k = StormAmount(pp);
            SkyController.I.SetStorm(k);
            SkyController.I.FogMul = Mathf.Lerp(SkyController.I.FogMul, 1f + 5f * k, Time.deltaTime * 1.5f);
            Wind.GustAmount = 0.1f + 0.3f * k;
            G.BaseTrack = k > 0.3f ? AudioMan.Track.Storm : AudioMan.Track.Calm;
            pushToast -= Time.deltaTime;
            if (!entered)
            {
                Objective("Sail south past Kadal to the Storm Wall", entry, "Storm Wall  ·  ");
                if (Dist(entry) < 140f || pp.z < -760f)
                {
                    entered = true;
                    era.DrainRate = 1f / 30f;
                    era.RechargeRate = 1f / 6f;
                    H.Banner("THE STORM WALL", "A storm that has never ended");
                    Say("Paachu", "Gods of the sea! Rocks everywhere, and the sky has gone black!");
                    Say("Raghavan", "The first navigators lit a passage through here. The compass remembers it. Shift, Arivan!");
                    H.Hint("Press SHIFT: the Era Shift shows the ancient lamps of the safe passage  ·  sail between them", 12f);
                }
            }
            else
            {
                Objective("Follow the ancient lamps through the Storm Wall  (SHIFT shows them)", AheadOnPath(World.Passage, pp, 110f), "Lamp  ·  ");
                // the wall itself: outside the reef field the storm hurls ships back north
                if (pp.z < -800f && pp.z > -1900f && (pp.x < -1220f || pp.x > -380f))
                {
                    G.Player.Nudge(new Vector3(0, 0, 9f * Time.deltaTime));
                    if (pushToast <= 0f) { pushToast = 6f; H.Toast("The Storm Wall drives you back! Only the ancient passage gets through.", UIKit.Red); }
                }
                if (Dist(end) < 80f || (pp.z < end.z - 20f && Mathf.Abs(pp.x - end.x) < 260f)) through = true;
            }
            yield return null;
        }
        era.DrainRate = 1f / 10f;
        era.RechargeRate = 1f / 11f;
        SkyController.I.SetStorm(0f);
        SkyController.I.FogMul = 1f;
        SkyController.I.Go(SkyController.Preset.Sunset, 25f);
        Wind.ShiftTo(-160f, 0.95f, 10f);
        Wind.GustAmount = 0.1f;
        G.BaseTrack = AudioMan.Track.Mystery;
        AudioMan.I.Play("discovery", 0.8f);
        H.Banner("BEYOND THE EDGE OF THE MAP", AlliesAfloat() == 2 ? "The whole fleet is through" : "The Kaveri is through");
        Say("Paachu", "We're through! Calm water. I have never seen a sky that colour.");
        Say("Arivan", "There. An island that is on no chart at all.");
        yield return WaitTalk();
    }

    // ------------------------------------------------------------------ 3. the Sunken Archive
    IEnumerator Archive4(bool restart)
    {
        Vector3 echo = World.P("archive_echo");
        if (restart)
        {
            Environment(SkyController.Preset.Sunset, 0, -160, 0.95f, 0.1f, 0);
            Vector3 st = World.Passage[World.Passage.Count - 1];
            G.PlaceShip(st, Mathf.Atan2(echo.x - st.x, echo.z - st.z) * Mathf.Rad2Deg);
            FleetJoined = true;
            PlaceAllies(true);
            Gameplay();
        }
        SkyController.I.FogMul = 1f;
        SkyController.I.SetStorm(0f);
        H.KnownPlaces.Add("archive");
        var era = EraShift.I;
        float progress = 0;
        bool done = false;
        while (!done)
        {
            float d = Dist(echo);
            Objective("Read the echo of the Sunken Archive  —  Era Shift beside the ruins", echo, "Archive  ·  ");
            G.EchoNearby = d < 170f;
            if (d < 100f)
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
        ArchiveRead = true;
        Stones = 1;
        AudioMan.I.Play("discovery", 1f);
        FX.EraPulse(echo);
        era.Energy = 1f;
        H.Banner("THE SUNKEN ARCHIVE", "Every route the sea has ever known");
        H.ClearDialogue();
        Say("The First Navigator (echo)", "We wrote every wind, every current and every star into these walls, so that no sailor would ever be lost again.");
        Say("The First Navigator (echo)", "But maps are power. Men began to fight over the Archive. So we called the storm, and let the sea take our city.");
        Say("Raghavan", "The Cartographers never wanted to find new routes. They wanted to own all of them.");
        yield return WaitTalk();
    }

    // ------------------------------------------------------------------ 4. the Last Map
    IEnumerator Battle4(bool restart)
    {
        if (restart)
        {
            Environment(SkyController.Preset.Sunset, 0, -160, 0.95f, 0.1f, 0);
            Vector3 st = World.P("archive_entry") + new Vector3(0, 0, 60);
            G.PlaceShip(st, 180f);
            G.ClearEnemies();
            FleetJoined = true;
            PlaceAllies(true);
            Gameplay();
        }
        SkyController.I.Go(SkyController.Preset.Sunset, 30f);
        yield return new WaitForSeconds(1.5f);
        Vector3 me = G.Player.Planar;
        Vector3 a = World.P("final_enemy_a"), b = World.P("final_enemy_b");
        Vector3 fs = Vector3.Distance(me, a) > Vector3.Distance(me, b) ? a : b;
        Vector3 rs = fs == a ? b : a;
        G.SpawnFlagship(fs, Mathf.Atan2(me.x - fs.x, me.z - fs.z) * Mathf.Rad2Deg, G.Tutorial ? 260f : 360f);
        G.SpawnRaider(rs, Mathf.Atan2(me.x - rs.x, me.z - rs.z) * Mathf.Rad2Deg);
        AudioMan.I.Play("conch", 1f);
        AudioMan.I.PlayDelayed("conch", 1.4f, 0.8f, 0.85f);
        yield return new WaitForSeconds(1.2f);
        H.Banner("THE LAST MAP", "Flagship of the Grand Cartographer");
        G.BaseTrack = AudioMan.Track.Combat;
        Say("Paachu", "Sails on both sides! They followed us through the storm!");
        Say("Ammu (Thirayar)", "The Guild is with you, captain! Thirayar, Mayil: fire-arrows to the rails!");
        Say("Arivan", "Sink the Last Map and this ends for good!");
        G.CombatEnabled = true;
        H.EnemyTarget = G.Flagship;
        H.EnemyName = "THE LAST MAP";
        H.Hint("Get beside the flagship and LEFT CLICK to fire  ·  your Guild allies fight with you  ·  hull repairs after 8 s without hits", 12f);
        while (G.Flagship != null && !G.Flagship.Dead && G.Flagship.gameObject.activeInHierarchy)
        {
            Objective("Sink the Last Map", G.Flagship.transform.position, "The Last Map  ·  ");
            H.EnemyTarget = G.Flagship;
            H.EnemyName = "THE LAST MAP";
            yield return null;
        }
        LastMapSunk = true;
        FlagshipSunk = true;
        RaiderSunk = G.Raider != null && G.Raider.Dead;
        H.EnemyTarget = null;
        G.CombatEnabled = false;
        if (G.RaiderAI != null) G.RaiderAI.Active = false;
        if (G.FlagshipAI != null) G.FlagshipAI.Active = false;
        AudioMan.I.Play("victory", 1f);
        H.Banner("THE LAST MAP SINKS", "The Hidden Cartographers are finished");
        G.BaseTrack = AudioMan.Track.Mystery;
        if (!RaiderSunk) Say("Paachu", "The last raider is striking her sails. They surrender!");
        Say("Raghavan", "It is over, son. Now the Archive is waiting for you.");
        yield return new WaitForSeconds(3f);
        yield return WaitTalk();
    }

    // ------------------------------------------------------------------ 5. the choice
    IEnumerator Choice4(bool restart)
    {
        Vector3 echo = World.P("archive_echo");
        if (restart)
        {
            Environment(SkyController.Preset.Dusk, 0, -160, 0.95f, 0.1f, 0);
            G.PlaceShip(World.P("archive_entry"), 180f);
            G.ClearEnemies();
            FleetJoined = true;
            PlaceAllies(true);
            Gameplay();
        }
        bool entered = false;
        Objective("Bring the Kaveri to the Archive and go ashore", echo, "Archive  ·  ");
        G.SetInteraction("[E]   Enter the Sunken Archive", echo, 90f, () => entered = true);
        while (!entered) yield return null;
        G.ClearInteraction();
        G.State = Game.GameState.Cutscene;
        G.Player.SailLevel = 0;
        H.SetHudVisible(false);
        H.Letterbox(true);
        H.ClearDialogue();
        EraShift.I.Energy = 1f;
        if (!EraShift.I.Active) EraShift.I.Toggle();
        EraShift.I.DrainRate = 0f;
        Vector3 hall = echo + (new Vector3(-760, 0, -2180) - echo).normalized * 30f;
        Cam.Shot(hall + new Vector3(40, 22, 40), hall + new Vector3(0, 10, 0), 44, 2.5f);
        Cam.Drift(new Vector3(-1.2f, 0.1f, 0.4f));
        yield return SayWait("Arivan", "It is all here. Every route between Muziris and the end of the world.");
        yield return SayWait("Raghavan", "And it answers to whoever holds the compass. The choice is yours, Arivan. Not mine. Not the Guild's.");
        yield return SayWait("Paachu", "Whatever you choose, captain, the sea will remember it.");
        Choice = -1;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        H.ShowChoice("WHAT WILL YOU DO WITH THE ARCHIVE?", ChoiceTitles, ChoiceText, i => Choice = i);
        while (Choice < 0) yield return null;
        EraShift.I.DrainRate = 1f / 10f;
        AudioMan.I.Play("discovery", 1f);
        FX.EraPulse(echo);
        H.Banner(ChoiceTitles[Choice], "");
        yield return new WaitForSeconds(2f);
    }

    // ------------------------------------------------------------------ ending
    IEnumerator Ending4()
    {
        G.State = Game.GameState.Cutscene;
        H.SetHudVisible(false);
        H.Letterbox(true);
        H.ClearDialogue();
        G.Player.SailLevel = 1;
        G.BaseTrack = AudioMan.Track.Calm;
        SkyController.I.Go(SkyController.Preset.Dawn, 6f);
        if (EraShift.I.Active) EraShift.I.Deactivate();
        Cam.Orbit(G.Player.transform.position, 40f, 11f, 4f, 200f, 3f, 42f);
        yield return new WaitForSeconds(1.5f);
        int c = Mathf.Clamp(Choice, 0, 3);
        string[][] lines =
        {
            new[] { "Raghavan", "Muziris will be the richest port on earth. Use that power more kindly than they did, son." },
            new[] { "Raghavan", "Some doors are meant to stay shut. Your mother would be proud of you." },
            new[] { "Raghavan", "Every sailor in every port, never lost again. That was the first navigators' dream. Now it is yours." },
            new[] { "Raghavan", "No compass. No chart. Just the stars and the wind, like the first sailors. I think I like that." },
        };
        yield return SayWait(lines[c][0], lines[c][1]);
        yield return SayWait("Arivan", "Take us home, Paachu.");
        G.State = Game.GameState.Ending;
        G.Player.Frozen = true;
        string[] epilogue =
        {
            "Arivan carried the Archive's charts home to Muziris.\n\nWithin a generation, every ship that crossed the western sea flew the colours of the Malabar coast, and the black gold of pepper made the city richer than any kingdom.\n\nThey called him the Merchant King. Some said it with love. Some, with fear.\n\nThe sea remembers.",
            "Arivan sealed the Archive and let the Storm Wall close behind the fleet.\n\nHe spent the rest of his life sailing between Muziris and Alexandria, quietly turning back every ship that went looking for the passage.\n\nNo one would own the sea again. Not even him.\n\nThe sea remembers.",
            "Arivan copied every chart, and gave them away.\n\nPepper from Muziris, incense from Arabia, glass from Alexandria: a thousand small ships now sailed routes once known to a few. The seven seas became one sea, and it belonged to everyone.\n\nThey called him the Great Navigator.\n\nThe sea remembers.",
            "Arivan broke the bronze compass on the Archive steps, and the sea took the island back.\n\nThe old routes were lost forever. Sailors had to learn the winds and the stars all over again, and they found new routes no one had ever imagined.\n\nSome called it the Broken Route. Others called it freedom.\n\nThe sea remembers.",
        };
        float t = G.PlayTime;
        string stats = "YOUR CHOICE          " + ChoiceTitles[c] + "\n" +
                       "VOYAGE TIME        " + Mathf.FloorToInt(t / 60f) + "m " + Mathf.FloorToInt(t % 60f).ToString("00") + "s\n" +
                       "GUILD FLEET          " + AlliesAfloat() + " / 2 afloat\n" +
                       "THE LAST MAP        " + (LastMapSunk ? "Sunk" : "Escaped") + "\n" +
                       "HULL                    " + Mathf.RoundToInt(G.Player.HP / G.Player.MaxHP * 100f) + "%\n\n" +
                       "<color=#7fe8e0>THE END  ·  THANK YOU FOR SAILING THE SPICE ROUTE</color>";
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        yield return H.PlayEnding(epilogue[c], stats, "MAIN MENU", () => G.LoadChapter(0));
    }
}
