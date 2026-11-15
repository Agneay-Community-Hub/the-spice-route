using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>The Malabar Expedition — a scripted voyage in nine beats, each one a checkpoint.</summary>
public partial class Story : MonoBehaviour
{
    public static Story I;
    public int Step;
    public bool RaiderSunk;
    public int Stones;
    public bool WindShifted, PackageReady;
    readonly bool[] stoneDone = new bool[3];
    GhostShip ghost;

    static Game G => Game.I;
    static HUD H => HUD.I;
    static CameraRig Cam => CameraRig.I;

    void Awake() { I = this; }

    public void Begin(int step, bool restart)
    {
        StopAllCoroutines();
        StartCoroutine(Run(step, restart));
    }

    IEnumerator Run(int step, bool restart)
    {
        int last = G.Chapter == 3 ? 5 : G.Chapter == 2 ? 6 : 8;
        for (int s = step; s <= last; s++)
        {
            Step = s;
            G.Checkpoint = s;
            yield return G.Chapter == 3 ? Beat3(s, restart && s == step) : G.Chapter == 2 ? Beat2(s, restart && s == step) : Beat(s, restart && s == step);
        }
        yield return G.Chapter == 3 ? Ending3() : G.Chapter == 2 ? Ending2() : Ending();
    }

    IEnumerator Beat(int s, bool restart)
    {
        switch (s)
        {
            case 0: return Port(restart);
            case 1: return MarketBeat(restart);
            case 2: return Cape(restart);
            case 3: return StormBeat(restart);
            case 4: return RaiderBeat(restart);
            case 5: return ToEcho(restart);
            case 6: return Echo(restart);
            case 7: return Shoals(restart);
            default: return Outpost(restart);
        }
    }

    // ------------------------------------------------------------------ helpers
    static void Say(string sp, string t) => H.Say(sp, t);

    static IEnumerator SayWait(string sp, string t)
    {
        H.Say(sp, t);
        yield return null;
        while (!H.DialogueIdle) yield return null;
    }

    static IEnumerator WaitTalk()
    {
        while (!H.DialogueIdle) yield return null;
    }

    static float Dist(Vector3 p)
    {
        Vector3 a = G.Player.transform.position;
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(p.x, p.z));
    }

    static void Objective(string text, Vector3? target, string label = "")
    {
        H.SetObjective(text);
        H.MarkerTarget = target;
        H.MarkerLabel = label;
    }

    static void Environment(SkyController.Preset p, float storm, float windAng, float windStr, float gust, float blend)
    {
        SkyController.I.Go(p, blend);
        SkyController.I.SetStorm(storm);
        if (blend <= 0.01f) SkyController.I.Storm = storm;
        Wind.Set(windAng, windStr);
        Wind.GustAmount = gust;
    }

    static void Gameplay()
    {
        H.Letterbox(false);
        H.SetHudVisible(true);
        Cam.Gameplay(1.6f);
        G.Player.Frozen = false;
        G.State = Game.GameState.Sailing;
    }

    // ------------------------------------------------------------------ beats
    IEnumerator Port(bool restart)
    {
        G.State = Game.GameState.Cutscene;
        G.BaseTrack = AudioMan.Track.Calm;
        H.SetHudVisible(false);
        H.Letterbox(true);
        Environment(SkyController.Preset.Dawn, 0, -75, 0.95f, 0.1f, restart ? 0 : 3f);
        G.PlaceShip(World.P("start"), 270f);
        G.Player.Frozen = true;
        Vector3 dock = World.P("dock_muziris");
        Cam.Shot(dock + new Vector3(-75, 10, -60), dock + new Vector3(15, 6, 4), 42, restart ? 0.01f : 3f);
        Cam.Drift(new Vector3(0.4f, 0.1f, 1.6f));
        yield return new WaitForSeconds(1.5f);
        yield return SayWait("Devaki", "Arivan! Thank the gods. I feared you had already sailed.");
        Cam.Shot(dock + new Vector3(-26, 5, 15), dock + new Vector3(6, 4, -3), 38, 2.5f);
        Cam.Drift(new Vector3(0.9f, 0, 0.2f));
        yield return SayWait("Devaki", "This package must reach Ras al-Mira, across the western sea, before the monsoon shifts. Do not open it. And do not let the Cartographers take it.");
        yield return SayWait("Arivan", "Ras al-Mira? That outpost has been empty for years.");
        yield return SayWait("Devaki", "Not empty. Waiting. Your father knew that.");
        Cam.Shot(G.Player.transform.position + new Vector3(8, 14, -26), G.Player.transform.position + new Vector3(0, 6, 4), 46, 2.5f);
        Cam.Drift(new Vector3(-0.6f, 0.2f, 0.6f));
        yield return SayWait("Paachu", "Fill the hold before we go, boy. Pepper is cheap here and worth silver in the west.");
        Gameplay();
    }

    IEnumerator MarketBeat(bool restart)
    {
        if (restart) { Environment(SkyController.Preset.Dawn, 0, -75, 0.95f, 0.1f, 0); G.PlaceShip(World.P("start"), 270f); Gameplay(); }
        G.Market.SetMuziris();
        Vector3 market = World.P("market");
        Objective("Trade spices at the Muziris market", market, "Market pier  ·  ");
        H.Hint("Click the screen to take the helm  ·  Press E beside the pier to trade", 9f);
        bool done = false;
        G.SetInteraction("[E]   Trade at the Muziris market", market, 45f, () => G.OpenMarket(() => done = true));
        while (!done)
        {
            if (Dist(World.P("dock_muziris")) > 230f)
            {
                Say("Paachu", "Sailing with an empty hold? Your choice, captain. The west will not pay us for sea air.");
                break;
            }
            yield return null;
        }
        G.ClearInteraction();
        if (G.Market.Cargo > 0) Say("Paachu", G.Market.Cargo + " sacks stowed. She sits lower in the water now. A good sign.");
        yield return null;
    }

    IEnumerator Cape(bool restart)
    {
        if (restart) { Environment(SkyController.Preset.Dawn, 0, -75, 0.95f, 0.1f, 0); G.PlaceShip(World.P("start") + new Vector3(-30, 0, 0), 270f); Gameplay(); }
        WindShifted = false;
        SkyController.I.Go(SkyController.Preset.Day, 90f);
        Vector3 cape = World.P("cape");
        Objective("Sail west to the Cape of Lanterns", cape, "Cape of Lanterns  ·  ");
        yield return WaitTalk();
        Say("Paachu", "Raise the sail. W gives her more canvas, S takes it in. A and D work the steering oar.");
        H.Hint("W  raise sail     ·     A / D  steer     ·     mouse  look     ·     Q  helm view", 10f);
        float t0 = Time.time;
        while (Dist(World.P("dock_muziris")) < 270f && Dist(cape) > 420f) yield return null;
        H.Banner("THE WIND SHIFTS", "The monsoon is turning  ·  keep the wind behind your sail");
        Wind.ShiftTo(-150f, 1.05f, 7f);
        WindShifted = true;
        yield return new WaitForSeconds(1.8f);
        Say("Paachu", "Feel that? The monsoon is shifting. A square sail loves the wind at her back.");
        Say("Paachu", "Watch the dial. The teal arrow is the wind. Keep it behind us or on our beam, never on our nose.");
        H.Hint("Steer so the wind comes from behind or from the side  ·  sail efficiency is shown beside the dial", 10f);
        while (Dist(cape) > 150f) yield return null;
    }

    IEnumerator StormBeat(bool restart)
    {
        Vector3 shelter = World.P("kadal_shelter");
        if (restart)
        {
            Environment(SkyController.Preset.Day, 0.6f, -150, 1.2f, 0.25f, 0);
            Vector3 p = World.P("cape") + new Vector3(30, 0, -60);
            G.PlaceShip(p, Mathf.Atan2(shelter.x - p.x, shelter.z - p.z) * Mathf.Rad2Deg);
            G.Player.SailLevel = 2;
            Gameplay();
        }
        H.Banner("WEATHER SHIFT", "A monsoon squall sweeps in from the open sea");
        SkyController.I.SetStorm(1f);
        G.BaseTrack = AudioMan.Track.Storm;
        Wind.ShiftTo(-155f, 1.5f, 10f);
        Wind.GustAmount = 0.35f;
        yield return new WaitForSeconds(2f);
        Say("Paachu", "Storm rising! We will never round the cape in this. She'll tear the sail clean off the yard!");
        Say("Arivan", "Then we change course. Kadal island, to the south. We shelter in its lee.");
        Objective("COURSE SHIFT  —  Shelter in the lee of Kadal Island", shelter, "Kadal  ·  ");
        H.Hint("Storm winds shove you downwind  ·  lower the sail (S) if she grows hard to handle", 9f);
        while (Dist(shelter) > 130f) yield return null;
        SkyController.I.SetStorm(0f);
        SkyController.I.Go(SkyController.Preset.Afternoon, 30f);
        Wind.ShiftTo(-100f, 1.0f, 25f);
        Wind.GustAmount = 0.12f;
        G.BaseTrack = AudioMan.Track.Calm;
        Say("Paachu", "We made it. The Kaveri is tougher than she looks. Like her captain.");
        yield return WaitTalk();
    }

    IEnumerator RaiderBeat(bool restart)
    {
        if (restart)
        {
            Environment(SkyController.Preset.Afternoon, 0, -100, 1f, 0.12f, 0);
            G.PlaceShip(World.P("kadal_shelter"), 200f);
            Gameplay();
        }
        Objective("", null);
        yield return new WaitForSeconds(3f);
        Vector3 pp = G.Player.transform.position;
        Vector3 dir = (World.P("raider_spawn") - pp); dir.y = 0; dir.Normalize();
        Vector3 spawn = pp + dir * 430f;
        if (World.LandHeight(spawn.x, spawn.z) > -6f) spawn = World.P("raider_spawn");
        G.SpawnRaider(spawn, Mathf.Atan2(pp.x - spawn.x, pp.z - spawn.z) * Mathf.Rad2Deg);
        AudioMan.I.Play("conch", 1f);
        yield return new WaitForSeconds(1.2f);
        H.Banner("BLACK SAILS", "The Hidden Cartographers have found you");
        G.BaseTrack = AudioMan.Track.Combat;
        Say("Paachu", "Black sails on the horizon! Cartographers. They have come for the package.");
        Say("Arivan", "Archers to the rails! Light the arrows!");
        G.CombatEnabled = true;
        H.EnemyTarget = G.Raider;
        Objective("Sink the Cartographer raider  —  or outrun it", G.Raider.transform.position, "Raider  ·  ");
        H.Hint("Turn broadside and LEFT CLICK to loose fire-arrows  ·  hold RIGHT CLICK to aim  ·  ram her at full sail", 12f);
        float escape = 0;
        while (true)
        {
            H.MarkerTarget = G.Raider.transform.position;
            if (G.Raider.Dead) { RaiderSunk = true; break; }
            if (Dist(G.Raider.transform.position) > 560f) escape += Time.deltaTime; else escape = 0;
            if (escape > 6f) break;
            yield return null;
        }
        H.EnemyTarget = null;
        G.BaseTrack = AudioMan.Track.Calm;
        if (RaiderSunk)
        {
            AudioMan.I.Play("victory", 0.9f);
            H.Banner("RAIDER SUNK", "+80 gold salvaged from the wreckage");
            G.Market.Gold += 80;
            Say("Paachu", "She's going under! Ha! Let the Cartographers chart the bottom of the sea.");
            Say("Arivan", "Look, in the wreckage. A torn chart, marked with my father's sign. They were hunting him too.");
        }
        else
        {
            G.RaiderAI.Active = false;
            G.Raider.SailLevel = 0;
            Say("Paachu", "We've lost them in the swell. They will be back. Cartographers always come back.");
        }
        yield return new WaitForSeconds(2f);
        G.CombatEnabled = false;
        yield return WaitTalk();
    }

    IEnumerator ToEcho(bool restart)
    {
        if (restart) { Environment(SkyController.Preset.Afternoon, 0, -100, 1f, 0.12f, 0); G.PlaceShip(World.P("kadal_shelter"), 270f); Gameplay(); }
        if (G.Raider.gameObject.activeSelf && !G.Raider.Dead) G.Raider.gameObject.SetActive(false);
        SkyController.I.Go(SkyController.Preset.Sunset, 110f);
        H.KnownPlaces.Add("echo");
        Say("Arivan", "The island on my father's chart. It should lie just west of here.");
        Objective("Sail to the Isle of Echoes, marked on your father's chart", World.P("echo_arrive"), "Isle of Echoes  ·  ");
        H.Hint("Press M to open your father's chart", 7f);
        while (Dist(World.P("echo_arrive")) > 190f && Dist(new Vector3(-1500, 0, -100)) > 420f) yield return null;
    }

    IEnumerator Echo(bool restart)
    {
        if (restart) { Environment(SkyController.Preset.Sunset, 0, -100, 1f, 0.12f, 0); G.PlaceShip(World.P("echo_arrive") + new Vector3(60, 0, 0), 260f); Gameplay(); }
        var era = EraShift.I;
        era.Unlocked = true;
        era.Energy = 1f;
        AudioMan.I.Play("discovery", 0.9f);
        H.Banner("ERA SHIFT", "Your father's compass is awakening");
        Say("Arivan", "Father's compass... it's warm. It's humming. It has never done that before.");
        Objective("Press SHIFT to use your father's compass", null);
        H.Hint("Press SHIFT to perform an ERA SHIFT and see the island as it once was", 14f);
        int a0 = era.Activations;
        float wait = 0;
        while (era.Activations == a0)
        {
            wait += Time.deltaTime;
            if (wait > 16f) { H.Hint("Press SHIFT to perform an ERA SHIFT", 8f); wait = 0; }
            yield return null;
        }
        yield return new WaitForSeconds(1.6f);
        H.ClearDialogue();
        Say("Paachu", "By the gods... a harbour. A lighthouse. A whole city! Where did it come from?");
        Say("Arivan", "Not where. When. This is the island as it was, long ago. The compass remembers.");
        Say("Arivan", "Those stones glow like the compass. Father left something here.");
        string[] fragments =
        {
            "Arivan. If you hear this, the compass has found you. It remembers what the sea remembers. Every route ever sailed is still out here, beneath the waves.",
            "The Cartographers want to own the sea. Chart every passage, tax every hull, close every route they cannot control. Whoever holds the map holds the world.",
            "I found the old passage, the one the ancients sailed. I gave the chart to the only people I trusted. Follow the Vellimeen through the thorns, son. Follow the light.",
        };
        float progress = 0;
        while (Stones < 3)
        {
            int best = -1; float bd = 1e9f;
            for (int i = 0; i < 3; i++)
            {
                if (stoneDone[i]) continue;
                float d = Dist(World.EchoStones[i]);
                if (d < bd) { bd = d; best = i; }
            }
            Vector3 sp = World.EchoStones[best];
            Objective("Find the echo stones  (" + Stones + " / 3)  —  Era Shift near each glowing stone", sp, "Echo stone  ·  ");
            G.EchoNearby = bd < 170f;
            if (bd < 95f)
            {
                if (era.Active)
                {
                    progress += Time.deltaTime / 2.6f;
                    H.Reading = "Reading the echo...  " + Mathf.RoundToInt(Mathf.Clamp01(progress) * 100f) + "%";
                    if (progress >= 1f)
                    {
                        stoneDone[best] = true;
                        Stones++;
                        progress = 0;
                        H.Reading = "";
                        AudioMan.I.Play("discovery", 0.9f);
                        FX.EraPulse(sp);
                        H.Toast("Echo stone  " + Stones + " / 3", UIKit.Teal);
                        H.ClearDialogue();
                        Say("Raghavan (echo)", fragments[Stones - 1]);
                        era.Energy = Mathf.Max(era.Energy, 0.6f);
                    }
                }
                else { progress = Mathf.Max(0, progress - Time.deltaTime * 0.3f); H.Reading = "Press SHIFT to read the echo stone"; }
            }
            else { H.Reading = ""; progress = Mathf.Max(0, progress - Time.deltaTime); }
            yield return null;
        }
        G.EchoNearby = false;
        H.Reading = "";
        yield return WaitTalk();
    }

    IEnumerator Shoals(bool restart)
    {
        if (restart)
        {
            Environment(SkyController.Preset.Sunset, 0, -95, 1f, 0.1f, 0);
            G.PlaceShip(World.Channel[0] + new Vector3(110, 0, 20), 255f);
            EraShift.I.Unlocked = true;
            Gameplay();
        }
        SkyController.I.Go(SkyController.Preset.Dusk, 160f);
        Wind.ShiftTo(-95f, 1f, 10f);
        var era = EraShift.I;
        era.DrainRate = 1f / 26f;
        era.RechargeRate = 1f / 7f;
        H.KnownPlaces.Add("shoals");
        H.KnownPlaces.Add("outpost");
        foreach (var m in World.RouteMarkers) m.GetComponent<Renderer>().sharedMaterial.SetFloat("_Base", 0.14f);
        if (ghost != null) Destroy(ghost.gameObject);
        ghost = GhostShip.Create(World.Channel);
        H.Banner("ECHO OF THE VELLIMEEN", "Follow your father's ship through the Shoals of Thorns");
        AudioMan.I.Play("shift_on", 0.6f);
        Say("Paachu", "The Vellimeen! Your father's ship. She's sailing into the shoals!");
        Say("Paachu", "Hidden reefs all through here. Keep to her wake and the lanterns, or we will tear our belly open.");
        H.Hint("ERA SHIFT reveals the Vellimeen and the ancient channel lanterns  ·  hidden reefs lurk just beneath the surface", 12f);
        while (!(ghost.Finished && Dist(World.P("dock_outpost")) < 280f))
        {
            Objective("Follow the Vellimeen through the Shoals of Thorns", ghost.transform.position, "Vellimeen  ·  ");
            yield return null;
        }
        era.DrainRate = 1f / 10f;
        era.RechargeRate = 1f / 11f;
        Say("Arivan", "She's gone. Into the harbour... Ras al-Mira.");
        yield return WaitTalk();
    }

    IEnumerator Outpost(bool restart)
    {
        if (restart)
        {
            Environment(SkyController.Preset.Dusk, 0, -95, 1f, 0.1f, 0);
            G.PlaceShip(World.P("outpost_approach") + new Vector3(80, 0, 0), 270f);
            EraShift.I.Unlocked = true;
            Gameplay();
        }
        SkyController.I.Go(SkyController.Preset.Dusk, 30f);
        H.Banner("RAS AL-MIRA", "The forgotten outpost");
        Vector3 dock = World.P("dock_outpost");
        Objective("Bring the Kaveri alongside the Ras al-Mira pier", dock, "Pier  ·  ");
        while (Dist(dock) > 70f) yield return null;
        Say("Yusuf", "A Malabar sail, after all these years. And that compass... You are Raghavan's son.");
        Say("Yusuf", "I am Yusuf, the last trader of Ras al-Mira. Come. Sell what you carry. The storm has made spices precious.");
        G.Market.SetOutpost();
        bool done = false;
        Vector3 market = World.P("outpost_market");
        G.SetInteraction("[E]   Trade with Yusuf", market, 50f, () => G.OpenMarket(() => done = true));
        Objective("Trade your cargo with Yusuf", market, "Yusuf  ·  ");
        while (!done) yield return null;
        G.ClearInteraction();
        PackageReady = true;
        yield return WaitTalk();
        Say("Yusuf", "Now. The package. Devaki kept her promise, and so did your father. Open it, Arivan. It was always meant for you.");
        bool opened = false;
        G.SetInteraction("[E]   Open the sealed package", market, 50f, () => opened = true);
        Objective("Open the sealed package", market, "");
        while (!opened) yield return null;
        G.ClearInteraction();
    }

    IEnumerator Ending()
    {
        G.State = Game.GameState.Cutscene;
        H.SetHudVisible(false);
        H.Letterbox(true);
        H.ClearDialogue();
        G.Player.SailLevel = 0;
        G.BaseTrack = AudioMan.Track.Mystery;
        Vector3 dock = World.P("dock_outpost");
        Cam.Orbit(G.Player.transform.position, 34f, 9f, 5f, 210f, 3f, 42f);
        AudioMan.I.Play("discovery", 1f);
        yield return new WaitForSeconds(1f);
        yield return SayWait("Arivan", "It's a chart. But these waters... no merchant has ever marked them.");
        yield return SayWait("Yusuf", "The lost passage. The route the ancients sailed beyond the edge of every map. Your father died to keep it from the Cartographers.");
        yield return SayWait("Arivan", "Then I will finish his voyage.");
        G.State = Game.GameState.Ending;
        G.Player.Frozen = true;
        string text = "The sealed package held no gold and no spice.\n\nIt held a chart: a passage beyond the edge of every known map, the route Raghavan sailed and the Hidden Cartographers would kill to own.\n\nThe monsoon has shifted. So has Arivan's course.\n\nThe sea remembers every journey.";
        float t = G.PlayTime;
        int profit = Mathf.RoundToInt(G.Market.Earned - G.Market.Spent);
        string stats = (G.Tutorial ? "<color=#7fe8e0>TUTORIAL VOYAGE COMPLETE  ·  you won!</color>\n" : "") + "VOYAGE TIME        " + Mathf.FloorToInt(t / 60f) + "m " + Mathf.FloorToInt(t % 60f).ToString("00") + "s\n" +
                       "TRADE PROFIT        " + (profit >= 0 ? "+" : "") + profit + " gold\n" +
                       "ECHO STONES        " + Stones + " / 3\n" +
                       "THE RAIDER           " + (RaiderSunk ? "Sunk" : "Outrun") + "\n" +
                       "HULL                    " + Mathf.RoundToInt(G.Player.HP / G.Player.MaxHP * 100f) + "%\n\n" +
                       "<color=#7fe8e0>TO BE CONTINUED IN CHAPTER II</color>";
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        yield return H.PlayEnding(text, stats, "CONTINUE TO CHAPTER II", () => G.LoadChapter(2));
    }
}

/// <summary>The spectral Vellimeen, leading the player through the reef channel.</summary>
public class GhostShip : MonoBehaviour
{
    ShipParts P;
    List<Vector3> path;
    int seg;
    float heading;
    public bool Finished;
    Vector3 pos;
    Material mat;
    float vis = 0f;

    public static GhostShip Create(List<Vector3> path)
    {
        var parts = ShipBuilder.Build(ShipBuilder.Style.Ghost, World.Root, Mats.GhostShip);
        var g = parts.root.gameObject.AddComponent<GhostShip>();
        g.P = parts;
        g.path = path;
        g.pos = path[0] - (path[1] - path[0]).normalized * 30f;
        g.heading = Mathf.Atan2(path[1].x - path[0].x, path[1].z - path[0].z) * Mathf.Rad2Deg;
        g.mat = Mats.GhostShip;
        g.transform.position = g.pos;
        return g;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        var pl = Game.I.Player;
        float dp = Vector3.Distance(new Vector3(pl.transform.position.x, 0, pl.transform.position.z), pos);
        if (!Finished)
        {
            float speed = dp > 170f ? 0.5f : dp < 70f ? 11f : 8f;
            Vector3 target = path[Mathf.Min(seg + 1, path.Count - 1)];
            Vector3 to = target - pos; to.y = 0;
            if (to.magnitude < 12f)
            {
                seg++;
                if (seg >= path.Count - 1) Finished = true;
            }
            float want = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            heading = Mathf.MoveTowardsAngle(heading, want, dt * 22f);
            Vector3 f = new Vector3(Mathf.Sin(heading * Mathf.Deg2Rad), 0, Mathf.Cos(heading * Mathf.Deg2Rad));
            pos += f * speed * dt;
            vis = Mathf.MoveTowards(vis, 1f, dt * 0.5f);
        }
        else vis = Mathf.MoveTowards(vis, 0f, dt * 0.25f);
        float h = WaveField.HeightAt(pos);
        transform.position = new Vector3(pos.x, h - 0.1f, pos.z);
        transform.rotation = Quaternion.Euler(Mathf.Sin(Time.time * 0.7f) * 2f, heading, Mathf.Sin(Time.time * 0.5f) * 3f);
        mat.SetFloat("_Base", 0.22f * vis);
        mat.SetFloat("_Vis", vis);
        if (P.sail != null) P.sail.localScale = Vector3.one;
        if (Finished && vis <= 0f) gameObject.SetActive(false);
    }
}
