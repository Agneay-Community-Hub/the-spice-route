using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Land exploration. Stop the ship near a shore and press F to go ashore as Arivan:
/// walk (WASD), jump (SPACE), interact (E), Era Shift (SHIFT), return to the ship (F).
/// </summary>
public class Shore : MonoBehaviour
{
    public static Shore I;
    public bool OnFoot;
    public string ShipPrompt;
    public Transform Body;
    public Site Near;
    public Vector3 Pos => pos;
    public bool BotMode;
    public Vector2 BotMove;
    public bool BotUse;

    Vector3 pos;
    float yaw, vy, phase, moveAmt;
    bool grounded = true;
    Transform legL, legR, armL, armR;
    GameObject boat;
    Vector3 landing; bool canLand;
    float scan;
    bool hinted;
    float useCooldown;

    static Game G => Game.I;

    public static void Create(Transform parent)
    {
        var go = new GameObject("Shore");
        go.transform.SetParent(parent, false);
        I = go.AddComponent<Shore>();
        I.BuildBody();
    }

    Transform Part(string name, Transform parent, Vector3 local, MeshBuilder b, Material m)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = local;
        b.SpawnSelf(name + "_mesh", m, t);
        return t;
    }

    void BuildBody()
    {
        Body = new GameObject("Arivan").transform;
        Body.SetParent(transform, false);
        Color skin = new Color(0.55f, 0.36f, 0.22f), mundu = new Color(0.93f, 0.9f, 0.8f), sash = new Color(0.78f, 0.25f, 0.12f), gold = new Color(0.9f, 0.7f, 0.3f);
        var torso = new MeshBuilder();
        torso.Cylinder(new Vector3(0, 0.9f, 0), new Vector3(0, 1.48f, 0), 0.2f, 0.25f, 9, skin, false, true);
        torso.Cylinder(new Vector3(0, 0.82f, 0), new Vector3(0, 1.0f, 0), 0.25f, 0.22f, 9, sash, true, true);
        torso.Box(new Vector3(0.05f, 1.22f, 0.02f), new Vector3(0.1f, 0.62f, 0.5f), Quaternion.Euler(0, 0, 32f), sash);
        torso.Sphere(new Vector3(0, 1.66f, 0), new Vector3(0.13f, 0.15f, 0.14f), 9, 7, skin);
        torso.Sphere(new Vector3(0, 1.77f, -0.01f), new Vector3(0.17f, 0.1f, 0.17f), 9, 5, mundu);
        torso.Sphere(new Vector3(0.1f, 1.02f, 0.2f), Vector3.one * 0.07f, 7, 5, gold);   // father's bronze compass at his belt
        Part("Torso", Body, Vector3.zero, torso, Mats.Lit);
        var leg = new MeshBuilder();
        leg.Cylinder(new Vector3(0, 0, 0), new Vector3(0, -0.5f, 0), 0.14f, 0.13f, 7, mundu, true, false);
        leg.Cylinder(new Vector3(0, -0.5f, 0), new Vector3(0, -0.9f, 0), 0.075f, 0.06f, 6, skin, false, true);
        legL = Part("LegL", Body, new Vector3(-0.11f, 0.9f, 0), leg, Mats.Lit);
        legR = Part("LegR", Body, new Vector3(0.11f, 0.9f, 0), leg, Mats.Lit);
        var arm = new MeshBuilder();
        arm.Cylinder(new Vector3(0, 0, 0), new Vector3(0, -0.56f, 0), 0.065f, 0.05f, 6, skin, true, true);
        armL = Part("ArmL", Body, new Vector3(-0.3f, 1.44f, 0), arm, Mats.Lit);
        armR = Part("ArmR", Body, new Vector3(0.3f, 1.44f, 0), arm, Mats.Lit);
        Body.localScale = Vector3.one * 1.12f;
        Body.gameObject.SetActive(false);

        var bb = new MeshBuilder();
        Color hull = new Color(0.36f, 0.25f, 0.15f);
        for (int i = 0; i < 6; i++)
        {
            float t0 = i / 6f, t1 = (i + 1) / 6f;
            float w0 = Mathf.Sin(t0 * Mathf.PI) * 0.75f + 0.05f, w1 = Mathf.Sin(t1 * Mathf.PI) * 0.75f + 0.05f;
            float z0 = -2.2f + t0 * 4.4f, z1 = -2.2f + t1 * 4.4f;
            bb.FlatQuad(new Vector3(-w0, 0.55f, z0), new Vector3(-w1, 0.55f, z1), new Vector3(0, 0, z1), new Vector3(0, 0, z0), hull);
            bb.FlatQuad(new Vector3(0, 0, z0), new Vector3(0, 0, z1), new Vector3(w1, 0.55f, z1), new Vector3(w0, 0.55f, z0), hull);
            bb.FlatQuad(new Vector3(-w1, 0.55f, z1), new Vector3(-w0, 0.55f, z0), new Vector3(0, 0, z0), new Vector3(0, 0, z1), hull * 0.8f);
            bb.FlatQuad(new Vector3(0, 0, z1), new Vector3(0, 0, z0), new Vector3(w0, 0.55f, z0), new Vector3(w1, 0.55f, z1), hull * 0.8f);
        }
        bb.Box(new Vector3(0, 0.45f, 0), new Vector3(1.3f, 0.06f, 0.3f), hull * 1.2f);
        boat = bb.SpawnSelf("LandingBoat", Mats.Wood, transform);
        boat.SetActive(false);
    }

    // ------------------------------------------------------------ aboard ship
    void Update()
    {
        if (G == null || G.Player == null) return;
        if (OnFoot)
        {
            // danger or a story scene: get back aboard at once
            if (G.State != Game.GameState.Sailing || G.CombatEnabled || G.Player.Dead)
            {
                bool fight = G.CombatEnabled && G.State == Game.GameState.Sailing;
                Return(false);
                if (fight) HUD.I.Toast("Enemy sails! You run back to the ship.", UIKit.Red);
            }
            return;
        }
        ShipPrompt = null;
        if (G.State != Game.GameState.Sailing || !G.CanControl || G.CombatEnabled || G.Player.Dead) return;
        if (Stealth.I != null && Stealth.I.Active) return;
        scan -= Time.deltaTime;
        if (scan <= 0f) { scan = 0.3f; canLand = G.Player.Speed < 5f && FindLanding(G.Player.Planar, out landing); }
        if (!canLand) return;
        ShipPrompt = "[F]   Go ashore  ·  explore on foot";
        if (!hinted && !G.Tutorial)
        {
            hinted = true;
            HUD.I.Hint("You can go ashore here: press F  ·  shipwrights, taverns and treasure are found on land", 9f);
        }
        if (!G.BotControl && In.Down(Key.F)) GoAshore();
    }

    static bool FindLanding(Vector3 ship, out Vector3 spot)
    {
        spot = Vector3.zero;
        for (float r = 12f; r <= 84f; r += 6f)
        {
            for (int i = 0; i < 20; i++)
            {
                float a = i / 20f * Mathf.PI * 2f;
                float x = ship.x + Mathf.Cos(a) * r, z = ship.z + Mathf.Sin(a) * r;
                float h = World.LandHeight(x, z);
                if (h < 0.7f || h > 9f) continue;
                if (World.LandGradient(x, z).magnitude > 0.6f) continue;
                spot = new Vector3(x, h, z);
                return true;
            }
        }
        return false;
    }

    public bool GoAshore()
    {
        if (OnFoot || !canLand) return false;
        OnFoot = true;
        var p = G.Player;
        p.SailLevel = 0; p.SailAmt = 0; p.RudderInput = 0; p.Speed = 0;
        p.Frozen = true;
        G.AutoSteer = false;
        pos = landing;
        Vector3 inland = World.LandGradient(pos.x, pos.z);
        yaw = inland.sqrMagnitude > 1e-6f ? Mathf.Atan2(inland.x, inland.z) * Mathf.Rad2Deg : p.Heading;
        vy = 0; grounded = true;
        Body.gameObject.SetActive(true);
        Body.position = pos;
        // the rowing boat waits where the land meets the water
        Vector3 toSea = (p.Planar - pos); toSea.y = 0; toSea.Normalize();
        Vector3 bp = pos;
        for (float d = 0; d < 40f; d += 1f) { Vector3 q = pos + toSea * d; if (World.LandHeight(q.x, q.z) < 0.25f) { bp = q; break; } }
        boat.transform.position = new Vector3(bp.x, Mathf.Max(0.1f, World.LandHeight(bp.x, bp.z)), bp.z);
        boat.transform.rotation = Quaternion.LookRotation(toSea.sqrMagnitude > 0.1f ? toSea : Vector3.forward);
        boat.SetActive(true);
        CameraRig.I.Foot(Body, yaw);
        if (HUD.I.KeysText != null) HUD.I.KeysText.text = HUD.FootKeys;
        if (AudioMan.I != null) AudioMan.I.Play("splash_small", 0.7f);
        HUD.I.Banner("ASHORE", "W A S D walk  ·  SPACE jump  ·  E use  ·  F back to the ship");
        HUD.I.Hint("Walk to the light beams  ·  GOLD beams are shops, TEAL beams are discoveries", 10f);
        return true;
    }

    public void Return(bool announce = true)
    {
        if (!OnFoot) return;
        OnFoot = false;
        Near = null;
        Body.gameObject.SetActive(false);
        boat.SetActive(false);
        G.Player.Frozen = false;
        HUD.I.Prompt(null);
        if (HUD.I.KeysText != null) HUD.I.KeysText.text = HUD.ShipKeys;
        if (G.State == Game.GameState.Sailing) CameraRig.I.Gameplay(1.2f);
        if (announce) HUD.I.Toast("Back aboard the Kaveri  ·  press W to raise the sail", UIKit.Teal);
        Progress.Save();
    }

    // ------------------------------------------------------------ on foot
    /// <summary>Called by Game each frame instead of ship input while ashore.</summary>
    public void Tick(float dt)
    {
        bool ctl = G.CanControl;
        Vector2 mv = Vector2.zero;
        bool jump = false, use = false;
        if (ctl && BotMode) { mv = BotMove; use = BotUse; BotUse = false; }
        else if (ctl)
        {
            if (In.Held(Key.W) || In.Held(Key.UpArrow)) mv.y += 1;
            if (In.Held(Key.S) || In.Held(Key.DownArrow)) mv.y -= 1;
            if (In.Held(Key.D) || In.Held(Key.RightArrow)) mv.x += 1;
            if (In.Held(Key.A) || In.Held(Key.LeftArrow)) mv.x -= 1;
            jump = In.Down(Key.Space);
            use = In.Down(Key.E);
            if (In.Down(Key.F)) { Return(); return; }
            if (In.Down(Key.M)) HUD.I.ToggleChart();
            if (In.Down(Key.Tab)) Ledger.I.Open(0);
            if (In.Down(Key.P) || In.Down(Key.Escape)) G.SetPause(true);
        }
        Vector3 dir = Vector3.zero;
        if (mv.sqrMagnitude > 0.01f)
        {
            float cy = BotMode ? 0f : CameraRig.I.FootYaw;
            Quaternion q = Quaternion.Euler(0, cy, 0);
            dir = (q * new Vector3(mv.x, 0, mv.y)).normalized;
        }
        float speed = 8.5f;
        float slope = World.LandGradient(pos.x, pos.z).magnitude;
        if (slope > 0.7f) speed *= 0.6f;
        Vector3 step = dir * speed * dt;
        if (step.sqrMagnitude > 0)
        {
            Vector3 n = pos + step;
            if (!Walkable(n)) { n = pos + new Vector3(step.x, 0, 0); if (!Walkable(n)) { n = pos + new Vector3(0, 0, step.z); if (!Walkable(n)) n = pos; } }
            foreach (var o in World.Obstacles)
            {
                float dx = n.x - o.x, dz = n.z - o.y, rr = o.z + 0.45f;
                float d2 = dx * dx + dz * dz;
                if (d2 < rr * rr && d2 > 1e-4f) { float d = Mathf.Sqrt(d2); n.x = o.x + dx / d * rr; n.z = o.y + dz / d * rr; }
            }
            pos.x = n.x; pos.z = n.z;
            yaw = Mathf.LerpAngle(yaw, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, 1f - Mathf.Exp(-12f * dt));
        }
        float ground = Mathf.Max(World.LandHeight(pos.x, pos.z), 0.05f);
        if (grounded && jump) { vy = 6.5f; grounded = false; if (AudioMan.I != null) AudioMan.I.Play("click", 0.3f); }
        if (!grounded)
        {
            vy -= 18f * dt;
            pos.y += vy * dt;
            if (pos.y <= ground) { pos.y = ground; grounded = true; vy = 0; }
        }
        else pos.y = Mathf.Lerp(pos.y, ground, 1f - Mathf.Exp(-18f * dt));

        // animation
        moveAmt = Mathf.MoveTowards(moveAmt, dir.sqrMagnitude > 0 ? 1f : 0f, dt * 6f);
        phase += dt * 11f * moveAmt;
        float sw = Mathf.Sin(phase) * 38f * moveAmt;
        legL.localRotation = Quaternion.Euler(sw, 0, 0);
        legR.localRotation = Quaternion.Euler(-sw, 0, 0);
        armL.localRotation = Quaternion.Euler(-sw * 0.8f, 0, 6f);
        armR.localRotation = Quaternion.Euler(sw * 0.8f, 0, -6f);
        Body.position = pos + Vector3.up * (Mathf.Abs(Mathf.Sin(phase)) * 0.06f * moveAmt);
        Body.rotation = Quaternion.Euler(0, yaw, 0);

        // sites
        useCooldown -= dt;
        Near = null;
        float best = 1e9f;
        foreach (var s in World.Sites)
        {
            if (!s.Repeat && Progress.IsFound(s.Id)) continue;
            float d = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(s.Pos.x, s.Pos.z));
            if (d < s.Radius && d < best) { best = d; Near = s; }
        }
        if (!ctl) { HUD.I.Prompt(null); return; }
        if (Near != null)
        {
            HUD.I.Prompt(PromptFor(Near));
            if (use && useCooldown <= 0f) { useCooldown = 0.5f; Use(Near); }
        }
        else HUD.I.Prompt("[F]   Return to the ship");
    }

    static bool Walkable(Vector3 p)
    {
        float h = World.LandHeight(p.x, p.z);
        return h > 0.18f;
    }

    static string PromptFor(Site s)
    {
        switch (s.Type)
        {
            case Site.Kind.Shipwright: return "[E]   Shipwright  ·  upgrade your ship";
            case Site.Kind.Tavern: return "[E]   Tavern  ·  hire crew and pay wages";
            case Site.Kind.Inscription: return "[E]   Read the inscription";
            case Site.Kind.Chest: return "[E]   Open the chest";
            case Site.Kind.Herb: return "[E]   Gather " + s.Label.ToLower();
            case Site.Kind.Vault: return EraShift.I.Active ? "[E]   Step through the door of the past" : (EraShift.I.Unlocked ? "A sealed wall. Press SHIFT to see it as it once was" : "A sealed wall. Your compass cannot open it yet");
            default: return "[E]   Talk";
        }
    }

    public void Use(Site s)
    {
        var m = G.Market;
        switch (s.Type)
        {
            case Site.Kind.Shipwright: Ledger.I.Open(2, true); return;
            case Site.Kind.Tavern: Ledger.I.Open(1, false, s.Port ?? ""); return;
            case Site.Kind.Talk: HUD.I.Say(s.Speaker ?? s.Label, s.Text); return;
            case Site.Kind.Vault:
                if (!EraShift.I.Active) { HUD.I.Toast(EraShift.I.Unlocked ? "The wall is solid. Press SHIFT: in the past there was a door here." : "The wall is solid, and the compass is silent.", UIKit.Teal); return; }
                m.Gold += s.Gold;
                Progress.AddPoints(1, "");
                HUD.I.Banner(s.Label.ToUpper(), "+" + s.Gold + " gold  ·  +1 skill point");
                HUD.I.Say("Arivan", "A door that only exists in the past. And behind it, what the old navigators left for whoever could find it.");
                FX.EraPulse(s.Pos);
                break;
            case Site.Kind.Inscription:
                Progress.AddPoints(1, "");
                HUD.I.Banner("INSCRIPTION", "+1 skill point  ·  press TAB to spend it");
                HUD.I.Say(s.Label, s.Text);
                break;
            case Site.Kind.Chest:
                m.Gold += s.Gold;
                HUD.I.Banner(s.Label.ToUpper(), "+" + s.Gold + " gold");
                Progress.Loyalty(2f);
                break;
            case Site.Kind.Herb:
                m.Gold += s.Gold;
                HUD.I.Toast(s.Label + " gathered  ·  worth " + s.Gold + " gold", UIKit.Gold);
                break;
        }
        if (AudioMan.I != null) AudioMan.I.Play("discovery", 0.8f);
        Progress.MarkFound(s.Id);
        if (s.Beam != null) s.Beam.SetActive(false);
        if (s.Type == Site.Kind.Chest || s.Type == Site.Kind.Herb) s.Go.SetActive(false);
    }

    /// <summary>The nearest thing still worth walking to, for the coach line.</summary>
    public Site NearestGoal(out float dist)
    {
        Site best = null; dist = 1e9f;
        foreach (var s in World.Sites)
        {
            if (s.Type == Site.Kind.Talk) continue;
            if (!s.Repeat && Progress.IsFound(s.Id)) continue;
            float d = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(s.Pos.x, s.Pos.z));
            if (d < dist) { dist = d; best = s; }
        }
        return best;
    }
}
