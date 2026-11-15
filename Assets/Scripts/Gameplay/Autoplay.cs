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
    string shotDir, logPath;
    int lastStep = -2;
    float stepTime, shotTimer, realStart;
    int shotIndex;
    readonly List<string> issues = new List<string>();
    float hudMissing;
    readonly float[] limits = { 150, 150, 420, 420, 360, 360, 600, 480, 360 };

    static Game G => Game.I;

    void Start()
    {
        I = this;
        var args = System.Environment.GetCommandLineArgs();
        tutorial = System.Array.IndexOf(args, "-tutorial") >= 0;
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
        Log("Starting " + (tutorial ? "TUTORIAL VOYAGE" : "normal voyage"));
        G.StartFromBot(tutorial);
    }

    void Update()
    {
        if (G == null || ended) return;
        Time.timeScale = HUD.I.MarketOpen ? 1f : (G.State == Game.GameState.Sailing ? 2.5f : 2f);

        if (HUD.I.GameOverOpen)
        {
            issues.Add("Died during step " + Story.I.Step);
            Log("DIED at step " + Story.I.Step + " — respawning at checkpoint");
            HUD.I.ShowGameOver(false);
            G.Respawn();
            return;
        }
        if (HUD.I.MarketOpen && !marketBusy) StartCoroutine(Trade());

        int s = Story.I.Step;
        if (s != lastStep && G.State != Game.GameState.Title)
        {
            Log("Step " + s + " begins   hull " + Mathf.RoundToInt(G.Player.HP) + "   gold " + G.Market.Gold + "   cargo " + G.Market.Cargo);
            lastStep = s;
            stepTime = 0;
        }
        stepTime += Time.deltaTime;
        if (s >= 0 && s < limits.Length && stepTime > limits[s] && G.State == Game.GameState.Sailing)
        {
            issues.Add("STUCK in step " + s + " (objective: " + Objective() + ")");
            Log("STUCK in step " + s + " after " + limits[s] + "s; objective '" + Objective() + "'. Skipping ahead.");
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
            while (m.Buy(1)) { if (m.Gold < 80) break; }
            while (m.Buy(0)) { }
            while (m.Buy(1)) { }
            Log("Bought " + m.Items[0].Held + " pepper, " + m.Items[1].Held + " cardamom, " + m.Items[2].Held + " cinnamon; gold left " + m.Gold);
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

    IEnumerator Finish()
    {
        Log("WIN — reached the ending.");
        yield return new WaitForSecondsRealtime(4f);
        Shot("ending_cinematic");
        yield return new WaitForSecondsRealtime(14f);
        Shot("ending_screen");
        int profit = Mathf.RoundToInt(G.Market.Earned - G.Market.Spent);
        Log("RESULT  time " + G.PlayTime.ToString("0") + "s  profit " + profit + "  stones " + Story.I.Stones + "  raider " + (Story.I.RaiderSunk ? "sunk" : "outrun") + "  hull " + Mathf.RoundToInt(G.Player.HP));
        Log(issues.Count == 0 ? "NO ISSUES" : "ISSUES:\n  " + string.Join("\n  ", issues));
        yield return new WaitForSecondsRealtime(1f);
        Application.Quit();
    }

    /// <summary>Called by Game every frame while sailing: sets sail, rudder, era shift, fires, interacts.</summary>
    public void Drive(float dt)
    {
        var p = G.Player;
        int s = Story.I.Step;
        int sail = 3;
        float rud = 0;
        var era = EraShift.I;
        Vector3? m = HUD.I.MarkerTarget;
        var raider = G.Raider;

        if (G.CombatEnabled && raider != null && raider.gameObject.activeInHierarchy && !raider.Dead)
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
            if ((s == 1 || s == 8) && dist < 90f) sail = dist < 45f ? 0 : 1;
            if (s == 3) sail = 2;
            if (s == 6)
            {
                if (dist < 120f) sail = 1;
                if (dist < 75f) sail = 0;
            }
            if (s == 7) sail = 2;
        }
        else sail = 0;

        if (s == 6 && era.Unlocked)
        {
            bool reading = !string.IsNullOrEmpty(HUD.I.Reading);
            if (era.Activations == 0) era.Toggle();
            else if (reading && !era.Active && era.Energy > 0.15f) era.Toggle();
            else if (!reading && era.Active && era.Energy < 0.9f) era.Deactivate();
        }
        if (s == 7 && !era.Active && era.Energy > 0.7f) era.Toggle();

        if (G.InInteractRange && p.Speed < 6f) G.TryInteract();

        p.SailLevel = sail;
        p.RudderInput = rud;
    }
}
