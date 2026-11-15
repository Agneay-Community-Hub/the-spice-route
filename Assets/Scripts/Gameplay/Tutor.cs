using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tutorial Voyage: "Captain's Lessons" panel that teaches every control, recommends the best action,
/// tracks each task, and live-coaches the player (steering, sail, wind, combat, Era Shift) to the win.
/// </summary>
public class Tutor : MonoBehaviour
{
    public static Tutor I;

    class Lesson
    {
        public int Step;
        public string Title, Body, Best, Task;
        public Func<bool> Done;
        public Func<string> Progress;
    }

    readonly List<Lesson> lessons = new List<Lesson>();
    int idx;
    float doneTimer;
    bool showingDone;

    RectTransform panel;
    CanvasGroup group;
    Text header, title, body, best, task, coach;
    Image box, tick;

    // tracked actions
    float lookAmount, onCourseTime, goodWindTime, lessonTime;
    int helmToggles, chartCycles;
    bool lastChart, lastHelm;
    bool fired, broadsideSeen;

    static Game G => Game.I;

    public static Tutor Create()
    {
        var rt = UIKit.Rect(HUD.I.transform, "Tutor");
        UIKit.Stretch(rt);
        rt.SetSiblingIndex(1);
        I = rt.gameObject.AddComponent<Tutor>();
        I.Build();
        I.Define();
        return I;
    }

    // ------------------------------------------------------------------ lessons
    void Define()
    {
        Add(1, "Welcome, captain!",
            "This is your ship, the Kaveri. First, CLICK ONCE anywhere on the game picture. After that, just MOVE THE MOUSE (no buttons) to look around. Roll the mouse wheel to move the camera closer or further.",
            "Look all around you. Notice the GOLD DIAMOND floating in the world: it always shows where to go next.",
            "Move the mouse to look around",
            () => lookAmount > 400f, () => Mathf.Min(100, Mathf.RoundToInt(lookAmount / 4f)) + "%");

        Add(1, "Buy spices to sell later",
            "Your ship is parked next to the market pier. Press the E key on your keyboard. A shop window opens. Each click on BUY puts one sack of spice in your ship.",
            "Click BUY on CARDAMOM several times first (it makes the most money), then BUY on PEPPER until you run out of gold. Then click DONE.",
            "Press E, buy spices, click DONE",
            () => Story.I.Step > 1, () => G.Market.Cargo + " sacks bought");

        Add(2, "Make the ship move",
            "The big sail catches the wind and pushes your ship. Press W to open the sail a little. Press W three times for full sail (fastest). Press S to close it again and slow down. The SAIL bars (bottom-left) show how open it is.",
            "Press W three times now. In open sea, full sail is always best.",
            "Press W three times (full sail)",
            () => G.Player.SailLevel >= 3, () => G.Player.SailLevel + " / 3 bars");

        Add(2, "Steer your ship",
            "HOLD the A key to turn left. HOLD the D key to turn right. LET GO to sail straight. Ships turn slowly, like a real boat, so be patient. At the top of the screen is a compass: the BLUE SHIP symbol shows where your ship is pointing, and the GOLD DIAMOND shows where you need to go.",
            "Turn until the BLUE SHIP sits on top of the GOLD DIAMOND, then let go. Use short taps to fine-tune.",
            "Keep the ship pointed at the diamond",
            () => onCourseTime > 3f, () => Mathf.Min(3f, onCourseTime).ToString("0.0") + " / 3 seconds");

        Add(2, "Auto-steer helper",
            "Steering a ship takes practice. If it feels hard, press T: your crew will steer toward the gold diamond for you, and steer around islands and rocks. Press T again to steer yourself. Holding A or D always takes back control.",
            "Turn auto-steer on with T whenever you just want to enjoy the trip. You still control the sail with W and S.",
            "Press T to try auto-steer",
            () => G.AutoSteerUses > 0, null);

        Add(2, "Islands are walls, not roads",
            "You never land on islands; ships stay on the water. Land and rocks act like walls: you bump into them and stop. Light turquoise water means it is getting shallow. A red SHALLOW WATER warning appears just before you would hit something.",
            "When you see the red warning: turn away with A or D, or press S to slow down. Every goal in this game is reached by stopping NEAR it on the water.",
            "Read this, then keep sailing",
            () => lessonTime > 12f, () => Mathf.Max(0, 12 - Mathf.FloorToInt(lessonTime)) + " s");

        Add(2, "Look from the ship (Q)",
            "Press Q to stand on your ship and look from the steering position. Press Q again to go back to the normal view behind the ship.",
            "The normal view (behind the ship) is easiest for steering and fighting.",
            "Press Q twice",
            () => helmToggles >= 2, () => helmToggles + " / 2");

        Add(2, "The map (M)",
            "Press M to open your father's map. The arrow is your ship; the red diamond is where to go. Press M again to close it.",
            "Open the map whenever you feel lost.",
            "Press M to open, M again to close",
            () => chartCycles >= 1, () => chartCycles >= 1 ? "done" : "");

        Add(2, "The wind changes!",
            "Wind pushes the sail, so its direction matters. Bottom-left is the wind circle: the BLUE ARROW shows which way the wind blows, with your ship always pointing up. The % number shows how well you are using the wind.",
            "Keep the number above 70%: the blue arrow should come from BEHIND or the SIDE of your ship. If the wind blows straight at your nose (IN IRONS), turn a little left or right.",
            "Sail above 70% for 5 seconds",
            () => Story.I.WindShifted && goodWindTime > 5f, () => !Story.I.WindShifted ? "keep sailing, the wind will change soon" : Mathf.RoundToInt(G.Player.WindEff * 100) + "%  ·  " + Mathf.Min(5f, goodWindTime).ToString("0.0") + " / 5 s");

        Add(2, "Sail to the lighthouse",
            "Follow the gold diamond to the lighthouse on the Cape of Lanterns. The number under the diamond is how far away it is.",
            "Full sail (W x3), point at the diamond, or press T for auto-steer.",
            "Reach the Cape of Lanterns",
            () => Story.I.Step > 2, null);

        Add(3, "A storm!",
            "Rain, big waves and strong wind. The wind pushes your ship sideways, so it is harder to steer. You must go to the calm side of Kadal Island, where the gold diamond is.",
            "Press S once (2 bars) so the ship is easier to control. Point at the diamond (or press T). Stay away from the coast.",
            "Reach the shelter behind Kadal Island",
            () => Story.I.Step > 3, null);

        Add(4, "Enemy ship: get beside it",
            "A pirate ship with black sails attacks! Its RED diamond is on the compass. Your archers stand along the LEFT and RIGHT sides of your ship, so they can only shoot sideways, not forwards.",
            "Turn your ship so the enemy is on your LEFT or RIGHT side, not in front. The help line below tells you which way to turn.",
            "Put the enemy beside your ship",
            () => broadsideSeen, null);

        Add(4, "Fire arrows!",
            "When the enemy is beside you, CLICK THE LEFT MOUSE BUTTON to shoot burning arrows. The orange ring in the middle of the screen refills when you can shoot again (about 2 seconds). Holding the RIGHT mouse button zooms in for better aim.",
            "Click every time the ring is full while the enemy is beside you. Keep moving: a moving ship is harder to hit. Your ship repairs itself when nothing hits it for 8 seconds. Sailing far away also works: the enemy gives up.",
            "Sink the enemy ship (or escape)",
            () => Story.I.Step > 4, () => G.Raider != null && G.Raider.gameObject.activeInHierarchy && !G.Raider.Dead ? "enemy strength " + Mathf.CeilToInt(G.Raider.HP / G.Raider.MaxHP * 100f) + "%" : "");

        Add(5, "Sail to the Isle of Echoes",
            "Follow the gold diamond west, toward the sunset.",
            "Full sail and point at the diamond, or press T for auto-steer.",
            "Reach the Isle of Echoes",
            () => Story.I.Step > 5, null);

        Add(6, "The magic compass (SHIFT)",
            "Your father's compass can show the past! Press the SHIFT key: the world turns golden and ghost buildings appear. The ring in the bottom-right corner is its power; it empties while the magic is on and refills when it is off.",
            "Press SHIFT now to see it. Press SHIFT again to turn it off and save power.",
            "Press SHIFT",
            () => EraShift.I.Activations > 0, null);

        Add(6, "Find the 3 glowing stones",
            "Three stones on the island shine with a beam of blue light. Sail close to one (the diamond guides you). When you are close, turn the magic ON with SHIFT and wait until the reading reaches 100%.",
            "When you arrive, press S until the ship almost stops, so you do not sail past. Turn the magic OFF while travelling between stones.",
            "Read all 3 stones",
            () => Story.I.Step > 6, () => Story.I.Stones + " / 3 stones");

        Add(7, "Follow the ghost ship",
            "Your father's ghost ship knows the safe path through hidden rocks. Stay close behind it. It waits for you if you are slow.",
            "Use 2 bars of sail (it is just the right speed). Tap SHIFT to see the ghost ship clearly. If the red warning appears, turn back toward the ghost ship.",
            "Follow it to Ras al-Mira",
            () => Story.I.Step > 7, null);

        Add(8, "Sell your spices",
            "Sail slowly to the wooden pier (press S to slow down as you get close). You do not need to touch it: when the message 'Trade with Yusuf' appears, press E.",
            "In the shop, click SELL ALL on every spice. Prices here are three times higher than where you bought them! Then click DONE.",
            "Stop near the pier, press E, sell everything",
            () => (G.Market.Cargo == 0 && !HUD.I.MarketOpen && G.Market.Earned > 0) || Story.I.PackageReady,
            () => G.Market.Cargo + " sacks left");

        Add(8, "Open the package and win!",
            "Press E one more time to open the sealed package you carried all the way from Muziris.",
            "This is the last step. Enjoy the ending!",
            "Press E to open the package",
            () => G.State == Game.GameState.Ending, null);
    }

    void Add(int step, string t, string b, string best, string task, Func<bool> done, Func<string> prog)
    {
        lessons.Add(new Lesson { Step = step, Title = t, Body = b, Best = best, Task = task, Done = done, Progress = prog });
    }

    // ------------------------------------------------------------------ UI
    void Build()
    {
        panel = UIKit.Panel(transform, "TutorPanel", new Color(0.03f, 0.04f, 0.06f, 0.82f), new Color(UIKit.Teal.r, UIKit.Teal.g, UIKit.Teal.b, 0.55f));
        UIKit.Place(panel, 0f, 1f, 0f, 1f, 36, -160, 540, 470);
        group = panel.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0;
        header = UIKit.Txt(panel, "Header", "", 15, UIKit.Teal, TextAnchor.UpperLeft, FontStyle.Bold);
        UIKit.Place(header.rectTransform, 0f, 1f, 0f, 1f, 22, -16, 500, 22);
        title = UIKit.Txt(panel, "Title", "", 27, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
        UIKit.Place(title.rectTransform, 0f, 1f, 0f, 1f, 22, -40, 500, 36);
        body = UIKit.Txt(panel, "Body", "", 19, UIKit.Parch, TextAnchor.UpperLeft);
        UIKit.Place(body.rectTransform, 0f, 1f, 0f, 1f, 22, -82, 500, 150);
        best = UIKit.Txt(panel, "Best", "", 19, UIKit.Teal, TextAnchor.UpperLeft, FontStyle.Italic);
        UIKit.Place(best.rectTransform, 0f, 1f, 0f, 1f, 22, -236, 500, 110);
        box = UIKit.Img(panel, "Box", ProcTex.PanelBorder, UIKit.Gold, true);
        UIKit.Place(box.rectTransform, 0f, 0f, 0f, 0f, 22, 62, 26, 26);
        tick = UIKit.Img(panel, "Tick", ProcTex.Circle, new Color(0.45f, 1f, 0.55f));
        UIKit.Place(tick.rectTransform, 0f, 0f, 0f, 0f, 27, 67, 16, 16);
        task = UIKit.Txt(panel, "Task", "", 20, UIKit.Parch, TextAnchor.MiddleLeft, FontStyle.Bold);
        UIKit.Place(task.rectTransform, 0f, 0f, 0f, 0f, 60, 60, 470, 30);
        coach = UIKit.Txt(panel, "Coach", "", 20, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
        UIKit.Place(coach.rectTransform, 0f, 0f, 0f, 0f, 22, 18, 510, 30);
    }

    // ------------------------------------------------------------------ update
    void Update()
    {
        if (G == null || G.Player == null) { group.alpha = 0; return; }
        if (!G.Tutorial)
        {
            group.alpha = 0;
            string c = G.State == Game.GameState.Sailing && !HUD.I.ModalOpen ? Coach() : "";
            HUD.I.NextText.text = string.IsNullOrEmpty(c) ? "" : "NEXT  " + c.TrimStart('>', ' ');
            return;
        }
        HUD.I.NextText.text = "";
        Track();

        bool visible = (G.State == Game.GameState.Sailing) && !HUD.I.ModalOpen;
        group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, Time.unscaledDeltaTime * 4f);
        if (idx >= lessons.Count) { group.alpha = 0; return; }

        // skip lessons belonging to steps the story has already passed
        while (idx < lessons.Count - 1 && lessons[idx].Step < Story.I.Step && !lessons[idx].Done()) idx++;
        var L = lessons[idx];
        bool ready = Story.I.Step >= L.Step;

        header.text = "TUTORIAL VOYAGE   ·   LESSON " + (idx + 1) + " / " + lessons.Count;
        if (!ready)
        {
            title.text = "Keep sailing";
            body.text = "Follow the gold diamond. The goal is written at the top-left. The next lesson starts when you arrive.";
            best.text = "BEST MOVE: full sail (W x3), point the blue ship at the gold diamond, or press T for auto-steer.";
            task.text = "Next: " + L.Title;
            tick.enabled = false;
        }
        else
        {
            title.text = L.Title;
            body.text = L.Body;
            best.text = "BEST MOVE: " + L.Best;
            string prog = L.Progress != null ? L.Progress() : "";
            task.text = L.Task + (string.IsNullOrEmpty(prog) ? "" : "   <color=#9fe8e0>(" + prog + ")</color>");
            if (visible) lessonTime += Time.deltaTime;
            bool done = L.Done();
            tick.enabled = done;
            if (done && !showingDone) { showingDone = true; doneTimer = 1.4f; AudioMan.I.Play("coin", 0.5f, 1.3f); HUD.I.Toast("Lesson complete: " + L.Title, new Color(0.45f, 1f, 0.55f)); }
            if (showingDone)
            {
                doneTimer -= Time.deltaTime;
                if (doneTimer <= 0) { showingDone = false; idx++; ResetCounters(); }
            }
        }
        coach.text = Coach();
    }

    void ResetCounters() { onCourseTime = 0; goodWindTime = 0; lessonTime = 0; }

    void Track()
    {
        if (Cursor.lockState == CursorLockMode.Locked) lookAmount += In.MouseDelta.magnitude;
        bool helm = CameraRig.I.CurMode == CameraRig.Mode.Helm;
        if (helm != lastHelm) { helmToggles++; lastHelm = helm; }
        bool chart = HUD.I.ChartOpen;
        if (lastChart && !chart) chartCycles++;
        lastChart = chart;
        var p = G.Player;
        if (HUD.I.MarkerTarget.HasValue && G.State == Game.GameState.Sailing)
        {
            float err = BearingError();
            if (Mathf.Abs(err) < 15f && p.Speed > 1f) onCourseTime += Time.deltaTime; else onCourseTime = Mathf.Max(0, onCourseTime - Time.deltaTime);
        }
        if (Story.I.WindShifted && p.WindEff > 0.7f && p.SailLevel > 0) goodWindTime += Time.deltaTime;
        if (G.Raider != null && G.Raider.gameObject.activeInHierarchy && !G.Raider.Dead)
        {
            float d = Vector3.Distance(G.Raider.Planar, p.Planar);
            if (d < 140f && Combat.Broadside(p, G.Raider.Planar, 62f)) broadsideSeen = true;
        }
    }

    float BearingError()
    {
        var p = G.Player;
        Vector3 to = HUD.I.MarkerTarget.Value - p.transform.position;
        float b = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
        return Mathf.DeltaAngle(p.Heading, b);
    }

    static float Dist2(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

    /// <summary>The single best thing to do right now, in plain words.</summary>
    string Coach()
    {
        var p = G.Player;
        if (G.State != Game.GameState.Sailing) return "";
        if (Cursor.lockState != CursorLockMode.Locked) return "> Click once on the game picture to start steering";
        if (G.ShoreWarning) return "> LAND AHEAD! Hold A or D to turn away, or press S to slow down";
        if (p.HP < p.MaxHP * 0.35f) return "> Ship badly damaged! Sail away from danger; it repairs itself in 8 s";

        bool ch2 = G.Chapter == 2;
        bool ch3 = G.Chapter == 3;
        if (G.Chapter >= 2 && Stealth.I != null && Stealth.I.Active && !Stealth.I.Spotted)
        {
            if (p.LanternOn) return "> Press L to douse your lantern";
            if (Stealth.I.InLight) return "> You are in the light! Steer out of it (A / D)";
            if (Stealth.I.Detection > 0.4f) return "> They are noticing you: press S to slow down";
        }
        var foe = G.CurrentEnemy;
        if (G.CombatEnabled && foe != null)
        {
            Vector3 to = foe.Planar - p.Planar;
            float d = to.magnitude;
            float side = Vector3.SignedAngle(p.Fwd, to, Vector3.up);
            if (d > 150f) return "> Enemy is " + Mathf.RoundToInt(d) + " m away: sail toward the RED diamond";
            if (!Combat.Broadside(p, foe.Planar, 62f))
            {
                bool ahead = Mathf.Abs(side) < 90f;
                string turn = (side > 0) == ahead ? "HOLD A" : "HOLD D";
                return "> Enemy is " + (ahead ? "in front" : "behind") + ": " + turn + " until it is beside you";
            }
            return G.ReloadReady ? "> Enemy is beside you: CLICK LEFT MOUSE to shoot!" : "> Reloading... keep the enemy beside you";
        }

        if (ch3 && Story.I.Step == 2)
        {
            float bd = Story.NearestBar(p.transform.position, out Vector3 bar);
            Vector3 toBar = bar - p.transform.position; toBar.y = 0;
            bool ahead = Vector3.Dot(toBar, p.Fwd) > 0;
            if (bd < 170f && ahead && !Story.TideHigh) return p.Speed > 1.5f ? "> Sandbar ahead and the tide is LOW: press S to stop and wait" : "> Waiting for HIGH tide... watch the gauge at the top";
            if (bd < 170f && ahead && Story.TideHigh) return "> The tide is HIGH: press W and sail over the sandbar now!";
        }
        if (ch3 && Story.I.Step == 1 && !EraShift.I.Active && Dist2(p.transform.position, World.P("canal_mouth")) < 450f) return "> Press SHIFT to see the obelisks of the old canal";
        if (ch2 && Story.I.Step == 2 && SkyController.I.FogMul > 2f)
            return EraShift.I.Active ? "> Sail between the glowing beacon pairs" : "> Press SHIFT to see the ancient beacons";
        if (ch2 && Story.I.Step == 5)
        {
            if (!string.IsNullOrEmpty(HUD.I.Reading) && !EraShift.I.Active) return "> You are beside the wreck: press SHIFT";
            if (HUD.I.Reading.StartsWith("Reading")) return p.SailLevel > 1 ? "> Reading... press S to slow down" : "> Reading... stay close until 100%";
        }
        if (!ch2 && Story.I.Step == 6 && EraShift.I.Unlocked)
        {
            if (EraShift.I.Activations == 0) return "> Press SHIFT now";
            if (!string.IsNullOrEmpty(HUD.I.Reading) && !EraShift.I.Active) return "> You are close to a stone: press SHIFT";
            if (HUD.I.Reading.StartsWith("Reading") && p.SailLevel > 1) return "> Reading... press S to slow down and stay close";
            if (HUD.I.Reading.StartsWith("Reading")) return "> Reading... stay close until 100%";
            if (EraShift.I.Active && EraShift.I.Energy < 0.5f) return "> Press SHIFT to switch the magic off and save power";
        }
        if (!ch2 && Story.I.Step == 7 && !EraShift.I.Active && EraShift.I.Energy > 0.5f) return "> Tap SHIFT to see the ghost ship";

        if (!HUD.I.MarkerTarget.HasValue) return "";
        float dist = Vector3.Distance(HUD.I.MarkerTarget.Value, p.transform.position);
        if (p.SailLevel == 0 && dist > 60f) return "> Press W to open the sail and start moving";
        bool docking = ch3 ? Story.I.Step == 3 : ch2 ? (Story.I.Step == 1 || Story.I.Step == 3) : (Story.I.Step == 1 || Story.I.Step == 8);
        if (dist < 90f && p.Speed > 4f && docking) return "> Almost there: press S to slow down";
        if (G.AutoSteer) return p.SailLevel < 3 && dist > 150f ? "> Auto-steer is driving. Press W for more speed" : "> Auto-steer is driving. Relax, or press T to steer";
        float err = BearingError();
        if (p.WindEff < 0.3f && p.SailLevel > 0) return "> Wind is in your face: turn a little " + (err > 0 ? "left" : "right") + " (A or D)";
        if (Mathf.Abs(err) > 15f)
        {
            string hint = onCourseTime < 0.1f && lessonTime > 25f ? "  (or press T for auto-steer)" : "";
            return err > 0 ? "> Turn RIGHT: hold D" + hint : "> Turn LEFT: hold A" + hint;
        }
        if (p.SailLevel < 3 && dist > 150f && SkyController.I.Storm < 0.5f) return "> Good, pointing the right way! Press W for more speed";
        if (SkyController.I.Storm > 0.5f && p.SailLevel > 2) return "> Good! In the storm, press S once for easier steering";
        return "> Good, pointing the right way. Let go of A / D";
    }
}
