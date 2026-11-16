using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Automated full playthrough used for testing (run the player with -autoplay [-tutorial] [-shots dir]).
/// Plays every story beat like a sensible player, logs progress, captures screenshots and reports
/// any beat where it gets stuck or dies.
/// </summary>
public class Autoplay : MonoBehaviour
{
    public static Autoplay I;
    bool tutorial, ended, marketBusy;
    bool landTest, landDone, landBusy;
    bool gifs; int burstLeft, burstWait; string burstName;
    readonly HashSet<string> burstDone = new HashSet<string>();

    void Burst(string name, float camYaw = 9999f, float camPitch = 12f, float camDist = 34f)
    {
        if (!gifs || burstLeft > 0 || burstWait > 0 || burstDone.Contains(name)) return;
        burstDone.Add(name);
        burstName = name;
        if (camYaw < 9000f) CameraRig.I.LookFrom(G.Player.Heading + camYaw, camPitch, camDist);
        Directory.CreateDirectory(Path.Combine(shotDir, "gif_" + name));
        burstWait = 12;
        Log("GIF burst: " + name);
    }

    bool BurstTick()
    {
        if (burstWait > 0)
        {
            burstWait--;
            Time.timeScale = 1f;
            if (burstWait == 3) ScreenCapture.CaptureScreenshot(Path.Combine(shotDir, "still_" + burstName + ".png"), 2);
            if (burstWait == 0) burstLeft = 66;
            return true;
        }
        if (burstLeft > 0)
        {
            Time.captureFramerate = 20;
            Time.timeScale = 1f;
            ScreenCapture.CaptureScreenshot(Path.Combine(shotDir, "gif_" + burstName, (66 - burstLeft).ToString("000") + ".png"));
            burstLeft--;
            if (burstLeft == 0) Time.captureFramerate = 0;
            return true;
        }
        return false;
    }

    void BurstTriggers(int s)
    {
        if (G.State != Game.GameState.Sailing || HUD.I.ModalOpen) return;
        var foe = G.CurrentEnemy;
        float fd = foe != null ? Vector3.Distance(foe.Planar, G.Player.Planar) : 9999f;
        bool era = EraShift.I.Active;
        float fog = SkyController.I.FogMul;
        bool onFoot = Shore.I != null && Shore.I.OnFoot;
        if (onFoot) return;
        if (G.Chapter == 2)
        {
            if (s == 2 && fog > 4f && era) Burst("fog_beacons");
            if (s == 4 && Stealth.I != null && Stealth.I.Active && stepTime > 30f) Burst("stealth");
            if (s == 6 && G.CombatEnabled && fd < 110f) Burst("flagship");
        }
        else if (G.Chapter == 3)
        {
            if (s == 2 && stepTime > 35f) Burst("canal");
            if (s == 4 && stepTime > 25f) Burst("pharos_night");
            if (s == 5 && G.CombatEnabled && fd < 120f) Burst("escape");
        }
        else if (G.Chapter == 4)
        {
            if (s == 2 && stepTime > 14f && fog < 1.3f) Burst("fleet", 155f, 9f, 42f);
            if (s == 2 && fog > 3.5f && era) Burst("storm_wall");
            if (s == 3 && !string.IsNullOrEmpty(HUD.I.Reading) && era) Burst("archive");
            if (s == 4 && G.CombatEnabled && fd < 170f) Burst("last_map", 40f, 14f, 46f);
        }
    }
    string shotDir, logPath;
    int lastStep = -2;
    float stepTime, shotTimer, realStart;
    int shotIndex;
    readonly List<string> issues = new List<string>();
    float hudMissing;
    readonly float[] limits = { 150, 150, 420, 420, 360, 360, 600, 480, 360 };
    readonly float[] limits2 = { 150, 150, 420, 300, 480, 300, 480 };
    readonly float[] limits3 = { 150, 360, 480, 300, 480, 420 };
    readonly float[] limits4 = { 150, 300, 600, 300, 600, 300 };

    static Game G => Game.I;

    void Start()
    {
        I = this;
        var args = System.Environment.GetCommandLineArgs();
        tutorial = System.Array.IndexOf(args, "-tutorial") >= 0;
        landTest = System.Array.IndexOf(args, "-landtest") >= 0;
        gifs = System.Array.IndexOf(args, "-gifs") >= 0;
        int si = System.Array.IndexOf(args, "-shots");
        shotDir = si >= 0 && si + 1 < args.Length ? args[si + 1] : Path.Combine(Application.persistentDataPath, "autoplay");
        Directory.CreateDirectory(shotDir);
        logPath = Path.Combine(shotDir, "autoplay.log");
        File.WriteAllText(logPath, "");
        realStart = Time.realtimeSinceStartup;
        G.BotControl = true;
        StartCoroutine(Begin());
    }

    void Log(string s)
    {
        string line = "[AUTOPLAY " + (Time.realtimeSinceStartup - realStart).ToString("0") + "s / game " + G.PlayTime.ToString("0") + "s] " + s;
        Debug.Log(line);
        File.AppendAllText(logPath, line + "\n");
    }

    void Shot(string name)
    {
        string p = Path.Combine(shotDir, (shotIndex++).ToString("00") + "_" + name + ".png");
        ScreenCapture.CaptureScreenshot(p, 2);
    }

    IEnumerator Begin()
    {
        yield return new WaitForSecondsRealtime(3f);
        Shot("title");
        yield return new WaitForSecondsRealtime(0.5f);
        Log("Starting " + (G.Chapter >= 2 ? "CHAPTER " + G.Chapter : tutorial ? "TUTORIAL VOYAGE" : "normal voyage"));
        G.StartFromBot(tutorial);
    }

    void Update()
    {
        if (G == null || ended) return;
        Time.timeScale = HUD.I.MarketOpen ? 1f : (G.State == Game.GameState.Sailing ? 2.5f : 2f);
        BurstTick();

        if (HUD.I.GameOverOpen)
        {
            issues.Add("Died during step " + Story.I.Step);
            Log("DIED at step " + Story.I.Step + " — respawning at checkpoint");
            HUD.I.ShowGameOver(false);
            G.Respawn();
            return;
        }
        if (HUD.I.MarketOpen && !marketBusy) StartCoroutine(Trade());
        if (landTest && !landDone && !landBusy && G.State == Game.GameState.Sailing && Story.I.Step == 1 && !HUD.I.ModalOpen) StartCoroutine(LandTest());
        if (HUD.I.ChoiceOpen && !marketBusy) StartCoroutine(Choose());

        int s = Story.I.Step;
        if (s != lastStep && G.State != Game.GameState.Title)
        {
            Log("Step " + s + " begins   hull " + Mathf.RoundToInt(G.Player.HP) + "   gold " + G.Market.Gold + "   cargo " + G.Market.Cargo);
            lastStep = s;
            foreach (int sk in new[] { 8, 9, 10, 0, 11, 1, 2, 3, 4, 5, 6, 7 }) if (Progress.Learn(sk)) Log("Learned skill: " + Progress.SkillNames[sk]);
            stepTime = 0;
        }
        stepTime += Time.deltaTime;
        BurstTriggers(s);
        var lim = G.Chapter == 4 ? limits4 : G.Chapter == 3 ? limits3 : G.Chapter == 2 ? limits2 : limits;
        if (s >= 0 && s < lim.Length && stepTime > lim[s] && G.State == Game.GameState.Sailing)
        {
            issues.Add("STUCK in step " + s + " (objective: " + Objective() + ")");
            Log("STUCK in step " + s + " after " + lim[s] + "s; objective '" + Objective() + "'. Skipping ahead.");
            Shot("stuck_step" + s);
            stepTime = 0;
            G.DevJump((s + 1).ToString());
        }

        if (G.State == Game.GameState.Sailing && !HUD.I.ModalOpen && !HUD.I.HudShowing) hudMissing += Time.unscaledDeltaTime; else hudMissing = 0;
        if (hudMissing > 4f) { issues.Add("HUD not visible while sailing (step " + s + ")"); Log("HUD NOT VISIBLE at step " + s); hudMissing = -9999f; }
        shotTimer += Time.unscaledDeltaTime;
        if (shotTimer > 25f && G.State == Game.GameState.Sailing) { shotTimer = 0; Shot("step" + s); }

        if (G.State == Game.GameState.Ending && !ended)
        {
            ended = true;
            StartCoroutine(Finish());
        }
    }

    string Objective()
    {
        var t = HUD.I.transform.Find("HUD/Objective/Text");
        return t != null ? t.GetComponent<UnityEngine.UI.Text>().text : "?";
    }

    IEnumerator Trade()
    {
        marketBusy = true;
        yield return new WaitForSecondsRealtime(1f);
        Shot("market_open");
        var m = G.Market;
        if (m.CanBuy)
        {
            var order = new List<int> { 0, 1, 2 };
            order.Sort((a, b) => (m.Items[b].WestPrice - m.BuyCost(b)).CompareTo(m.Items[a].WestPrice - m.BuyCost(a)));
            foreach (int i in order) { while (m.Buy(i)) { if (m.Gold < 60 && i == order[0]) break; } }
            foreach (int i in order) while (m.Buy(i)) { }
            Log("Bought " + m.Items[0].Held + " " + m.Items[0].Name + ", " + m.Items[1].Held + " " + m.Items[1].Name + ", " + m.Items[2].Held + " " + m.Items[2].Name + "; gold left " + m.Gold);
        }
        else
        {
            int before = m.Gold;
            for (int i = 0; i < m.Items.Count; i++) while (m.Sell(i)) { }
            Log("Sold all cargo for " + (m.Gold - before) + " gold");
        }
        yield return new WaitForSecondsRealtime(0.6f);
        HUD.I.CloseMarket();
        yield return new WaitForSecondsRealtime(1f);
        marketBusy = false;
    }

    IEnumerator Choose()
    {
        marketBusy = true;
        yield return new WaitForSecondsRealtime(1.5f);
        Shot("choice");
        int pick = 2;
        var args = System.Environment.GetCommandLineArgs();
        int ci = System.Array.IndexOf(args, "-choice");
        if (ci >= 0 && ci + 1 < args.Length) int.TryParse(args[ci + 1], out pick);
        Log("Choosing ending " + pick + ": " + Story.ChoiceTitles[Mathf.Clamp(pick, 0, 3)]);
        HUD.I.PickChoice(Mathf.Clamp(pick, 0, 3));
        yield return new WaitForSecondsRealtime(1f);
        marketBusy = false;
    }

    IEnumerator WalkTo(Site site, string what)
    {
        float t = 0;
        var sh = Shore.I;
        if (what == "tavern") { burstDone.Remove("x"); if (gifs && !burstDone.Contains("ashore")) { burstDone.Add("ashore"); burstName = "ashore"; Directory.CreateDirectory(Path.Combine(shotDir, "gif_ashore")); burstWait = 12; } }
        while (sh.OnFoot && t < 90f)
        {
            Vector3 d = site.Pos - sh.Pos; d.y = 0;
            if (d.magnitude < site.Radius * 0.6f) break;
            sh.BotMove = new Vector2(d.x, d.z).normalized;
            t += Time.deltaTime;
            yield return null;
        }
        sh.BotMove = Vector2.zero;
        Vector3 e = site.Pos - sh.Pos; e.y = 0;
        if (e.magnitude > site.Radius) { issues.Add("LAND: could not walk to " + what + " (" + e.magnitude.ToString("0") + " m short)"); Log("LAND could not reach " + what); }
        else Log("LAND reached " + what);
        yield return new WaitForSecondsRealtime(0.5f);
    }

    Site FindSite(Site.Kind k)
    {
        Site best = null; float bd = 1e9f;
        foreach (var s in World.Sites)
        {
            if (s.Type != k) continue;
            float d = Vector3.Distance(s.Pos, Shore.I.Pos);
            if (d < bd) { bd = d; best = s; }
        }
        return best;
    }

    IEnumerator LandTest()
    {
        landBusy = true;
        var sh = Shore.I;
        G.Market.Gold += 200;
        Log("LAND TEST begins: " + World.Sites.Count + " sites in this chapter");
        yield return new WaitForSeconds(1.5f);
        if (!sh.GoAshore()) { issues.Add("LAND: could not go ashore at Muziris"); Log("LAND could not go ashore"); landDone = true; landBusy = false; yield break; }
        sh.BotMode = true;
        yield return new WaitForSecondsRealtime(2f);
        Shot("land_ashore");
        int gold0 = G.Market.Gold;
        var wright = FindSite(Site.Kind.Shipwright);
        if (wright == null) issues.Add("LAND: no shipwright site");
        else
        {
            yield return WalkTo(wright, "shipwright");
            Shot("land_shipwright");
            sh.Use(wright);
            yield return new WaitForSecondsRealtime(1f);
            if (!Ledger.IsOpen) issues.Add("LAND: shipwright did not open the ledger");
            Shot("ledger_ship");
            bool ok = Progress.BuyUpgrade(0);
            Ledger.I.Refresh();
            Log("LAND bought hull upgrade: " + ok + "  hull now " + G.Player.MaxHP + "  gold " + G.Market.Gold);
            if (!ok || G.Player.MaxHP < 119f) issues.Add("LAND: hull upgrade did not apply");
            yield return new WaitForSecondsRealtime(0.8f);
            Shot("ledger_ship_bought");
            yield return new WaitForSecondsRealtime(0.4f);
            Ledger.I.Close();
        }
        var tav = FindSite(Site.Kind.Tavern);
        if (tav == null) issues.Add("LAND: no tavern site");
        else
        {
            yield return WalkTo(tav, "tavern");
            Shot("land_tavern");
            sh.Use(tav);
            yield return new WaitForSecondsRealtime(1f);
            Shot("ledger_crew");
            var cand = Progress.Candidates(tav.Port);
            bool hired = cand.Count > 0 && Progress.Hire(cand[0]);
            Ledger.I.Refresh();
            Log("LAND hired " + (cand.Count > 0 ? cand[0].Name : "nobody") + ": " + hired + "  crew " + Progress.D.Crew.Count);
            if (!hired) issues.Add("LAND: could not hire crew");
            yield return new WaitForSecondsRealtime(0.8f);
            Shot("ledger_crew_hired");
            yield return new WaitForSecondsRealtime(0.4f);
            Ledger.I.Close();
        }
        var ins = FindSite(Site.Kind.Inscription);
        if (ins != null)
        {
            int pts = Progress.D.Points;
            yield return WalkTo(ins, "inscription");
            sh.Use(ins);
            yield return new WaitForSecondsRealtime(1f);
            Shot("land_inscription");
            if (Progress.D.Points <= pts) issues.Add("LAND: inscription gave no skill point");
            Ledger.I.Open(0);
            yield return new WaitForSecondsRealtime(0.6f);
            bool learned = Progress.Learn(0) || Progress.Learn(8) || Progress.Has(0);
            Ledger.I.Refresh();
            Log("LAND learned Trim Mastery: " + learned + "  speed x" + Progress.S.SpeedMul.ToString("0.00"));
            if (!learned) issues.Add("LAND: could not learn a skill");
            yield return new WaitForSecondsRealtime(0.8f);
            Shot("ledger_skills");
            yield return new WaitForSecondsRealtime(0.4f);
            Ledger.I.Close();
        }
        yield return new WaitForSecondsRealtime(0.5f);
        sh.BotMode = false;
        sh.Return();
        yield return new WaitForSeconds(2f);
        if (sh.OnFoot || G.Player.Frozen) issues.Add("LAND: did not return to the ship cleanly");
        Log("LAND TEST done  gold " + gold0 + " -> " + G.Market.Gold);
        landDone = true;
        landBusy = false;
    }

    IEnumerator Finish()
    {
        Log("WIN — reached the ending.");
        yield return new WaitForSecondsRealtime(4f);
        Shot("ending_cinematic");
        yield return new WaitForSecondsRealtime(14f);
        Shot("ending_screen");
        int profit = Mathf.RoundToInt(G.Market.Earned - G.Market.Spent);
        Log("RESULT  chapter " + G.Chapter + "  time " + G.PlayTime.ToString("0") + "s  profit " + profit + "  echoes " + Story.I.Stones + "  raider " + (Story.I.RaiderSunk ? "sunk" : "outrun") + (G.Chapter == 2 ? "  gate " + (Story.I.Unseen ? "unseen" : "spotted") + "  flagship " + (Story.I.FlagshipSunk ? "sunk" : "escaped") : "") + (G.Chapter == 4 ? "  lastmap " + (Story.I.LastMapSunk ? "sunk" : "escaped") + "  allies " + (G.Allies == null ? 0 : System.Array.FindAll(G.Allies, x => x != null && !x.Dead).Length) + "  ending " + Story.I.Choice : "") + "  hull " + Mathf.RoundToInt(G.Player.HP));
        Log(issues.Count == 0 ? "NO ISSUES" : "ISSUES:\n  " + string.Join("\n  ", issues));
        yield return new WaitForSecondsRealtime(1f);
        Application.Quit();
    }

    /// <summary>Called by Game every frame while sailing: sets sail, rudder, era shift, fires, interacts.</summary>
    public void Drive(float dt)
    {
        var p = G.Player;
        if (landBusy) { p.SailLevel = 0; p.RudderInput = 0; return; }
        int s = Story.I.Step;
        int sail = 3;
        float rud = 0;
        var era = EraShift.I;
        Vector3? m = HUD.I.MarkerTarget;
        var raider = G.CurrentEnemy;
        bool ch2 = G.Chapter == 2;
        bool ch3 = G.Chapter == 3;
        bool ch4 = G.Chapter == 4;
        if (ch4) ch2 = false;
        bool stealth = G.Chapter >= 2 && Stealth.I != null && Stealth.I.Active && !Stealth.I.Spotted;

        if (G.CombatEnabled && raider != null && !stealth)
        {
            Vector3 to = raider.Planar - p.Planar;
            float d = to.magnitude;
            if (d > 110f) rud = G.BotRudder(raider.Planar);
            else
            {
                float b = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                float h1 = b + 90f, h2 = b - 90f;
                float h = Mathf.Abs(Mathf.DeltaAngle(p.Heading, h1)) < Mathf.Abs(Mathf.DeltaAngle(p.Heading, h2)) ? h1 : h2;
                if (d < 50f) h += Mathf.DeltaAngle(b, h) > 0 ? 25f : -25f;
                Vector3 dir = new Vector3(Mathf.Sin(h * Mathf.Deg2Rad), 0, Mathf.Cos(h * Mathf.Deg2Rad));
                rud = G.BotRudder(p.Planar + dir * 90f);
                sail = 2;
            }
            G.BotFire(raider.Planar + Vector3.up, raider.Velocity);
        }
        else if (m.HasValue)
        {
            rud = G.BotRudder(m.Value);
            float dist = Vector3.Distance(new Vector3(m.Value.x, 0, m.Value.z), p.Planar);
            bool docking = ch4 ? (s == 5 || s == 3) : ch3 ? s == 3 : ch2 ? (s == 1 || s == 3) : (s == 1 || s == 8);
            if (docking && dist < 90f) sail = dist < 45f ? 0 : 1;
            if (!ch2 && !ch4 && s == 3) sail = 2;
            if ((!ch2 && !ch4 && s == 6) || (ch2 && s == 5) || (ch4 && s == 3))
            {
                if (dist < 120f) sail = 1;
                if (dist < 75f) sail = 0;
            }
            if (!ch2 && !ch4 && s == 7) sail = 2;
            if (ch4 && s == 2 && SkyController.I.FogMul > 2f) sail = 2;
            if (ch2 && s == 2 && SkyController.I.FogMul > 2f) sail = 2;
            if (ch3 && s == 2)
            {
                sail = 2;
                float bd = Story.NearestBar(p.transform.position, out Vector3 bar);
                Vector3 toBar = bar - p.transform.position; toBar.y = 0;
                if (bd < 110f && Vector3.Dot(toBar, p.Fwd) > 0 && !Story.TideHigh) sail = 0;
            }
            if (stealth)
            {
                if (p.LanternOn) p.SetLantern(false);
                sail = Stealth.I.InLight ? 3 : 2;
            }
            if (ch3 && s == 4 && dist < 80f) sail = dist < 45f ? 0 : 1;
        }
        else sail = 0;

        int readStep = ch4 ? 3 : ch2 ? 5 : 6;
        if (ch4 && s == 2 && SkyController.I.FogMul > 2f && !era.Active && era.Energy > 0.5f) era.Toggle();
        if (ch2 && s == 2 && SkyController.I.FogMul > 2f && !era.Active && era.Energy > 0.5f) era.Toggle();
        if (s == readStep && era.Unlocked)
        {
            bool reading = !string.IsNullOrEmpty(HUD.I.Reading);
            if (era.Activations == 0) era.Toggle();
            else if (reading && !era.Active && era.Energy > 0.15f) era.Toggle();
            else if (!reading && era.Active && era.Energy < 0.9f) era.Deactivate();
        }
        if (!ch2 && !ch4 && s == 7 && !era.Active && era.Energy > 0.7f) era.Toggle();

        if (G.InInteractRange && p.Speed < 6f) G.TryInteract();

        p.SailLevel = sail;
        p.RudderInput = rud;
    }
}
