using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>Bootstrap and top-level game loop. The scene contains only this component.</summary>
public class Game : MonoBehaviour
{
    public static Game I;
    public enum GameState { Loading, Title, Cutscene, Sailing, GameOver, Ending }
    public GameState State = GameState.Loading;

    public Ship Player, Raider;
    public EnemyAI RaiderAI;
    public Market Market = new Market();
    public Story Story;
    public bool CombatEnabled;
    public bool BotControl;
    public bool Tutorial;
    public bool ReloadReady => reload <= 0f;
    public bool HighQuality = true;
    public bool WantsLock;
    public bool EchoNearby;
    public float PlayTime;
    public int Checkpoint;
    public AudioMan.Track BaseTrack = AudioMan.Track.Calm;

    float reload;
    const float ReloadTime = 2.3f;
    float ramCooldown;
    float damageFlash;
    bool skipFire;
    bool paused;

    string interPrompt;
    Vector3 interPos;
    float interRadius;
    System.Action interAction;

    public bool CanControl => State == GameState.Sailing && HUD.I != null && !HUD.I.ModalOpen && !paused;

#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void SR_HideLoader();
#endif
    static void HideHtmlLoader()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SR_HideLoader();
#endif
    }

    void Awake()
    {
        I = this;
        Application.runInBackground = true;
        StartCoroutine(Boot());
    }

    void SetupQuality()
    {
        int top = QualitySettings.names.Length - 1;
        QualitySettings.SetQualityLevel(top, true);
        ApplyQuality();
    }

    void ApplyQuality()
    {
        QualitySettings.shadows = HighQuality ? ShadowQuality.All : ShadowQuality.HardOnly;
        QualitySettings.shadowResolution = HighQuality ? ShadowResolution.High : ShadowResolution.Medium;
        QualitySettings.shadowDistance = HighQuality ? 230f : 110f;
        QualitySettings.shadowCascades = HighQuality ? 2 : 1;
        QualitySettings.shadowProjection = ShadowProjection.StableFit;
        QualitySettings.antiAliasing = HighQuality ? 4 : 0;
        QualitySettings.pixelLightCount = 2;
        QualitySettings.vSyncCount = 0;
        if (CameraRig.I != null) CameraRig.I.Cam.allowMSAA = HighQuality;
        if (PostFX.I != null) PostFX.I.HighQuality = HighQuality;
    }

    public void ToggleQuality()
    {
        HighQuality = !HighQuality;
        ApplyQuality();
    }

    IEnumerator Boot()
    {
        SetupQuality();
        var rig = CameraRig.Create();
        rig.gameObject.AddComponent<PostFX>();
        rig.transform.position = new Vector3(0, 30, -100);
        HUD.Create();
        HUD.I.FadeTo(0f, 0.01f);
        HUD.I.SetLoading(0.02f, "Preparing the voyage");
        yield return null;
        yield return null;
        HideHtmlLoader();
        Mats.Init();
        HUD.I.SetLoading(0.05f, "Mixing pigments");
        yield return null;
        SkyController.Create(transform, rig.transform);
        Ocean.Create(transform, rig.transform);
        FX.Init(transform);
        Combat.Create(transform);
        AudioMan.Create(transform);
        EraShift.Create(transform);
        Story = gameObject.AddComponent<Story>();
        WaveField.Push(0, 0.55f);
        yield return World.Build(transform, (p, s) => HUD.I.SetLoading(0.05f + p * 0.7f, s));

        Player = Ship.Create(ShipBuilder.Style.Kaveri, transform, "Kaveri", true);
        Player.OceanSlot = 0;
        Player.Teleport(World.P("start"), 270f);
        Player.OnDamaged += a => { damageFlash = Mathf.Min(1f, damageFlash + a / 18f); CameraRig.I.Shake(Mathf.Clamp01(a / 25f)); };
        Player.OnDeath += () => StartCoroutine(PlayerLost());
        Combat.I.Ships.Add(Player);

        Raider = Ship.Create(ShipBuilder.Style.Raider, transform, "Raider", false);
        Raider.OceanSlot = 1;
        Raider.MaxHP = Raider.HP = 140f;
        Raider.MinDrive = 6.5f;
        Raider.MaxSpeed = 14.5f;
        Raider.FireDamage = 1.3f;
        RaiderAI = Raider.gameObject.AddComponent<EnemyAI>();
        RaiderAI.Me = Raider;
        RaiderAI.Target = Player;
        Combat.I.Ships.Add(Raider);
        Raider.gameObject.SetActive(false);

        rig.Target = Player;
        HUD.I.InitChart();
        Tutor.Create();
        HUD.I.SetLoading(0.76f, "Tuning the sea");
        yield return AudioMan.I.Generate((p, s) => HUD.I.SetLoading(0.76f + p * 0.24f, s));
        HUD.I.SetLoading(1f, "Ready");
        yield return null;
        var args = System.Environment.GetCommandLineArgs();
        if (System.Array.IndexOf(args, "-capture") >= 0) { HUD.I.HideLoading(); gameObject.AddComponent<Director>(); yield break; }
        Title();
        if (System.Array.IndexOf(args, "-autoplay") >= 0) gameObject.AddComponent<Autoplay>();
    }

    void Title()
    {
        State = GameState.Title;
        SkyController.I.Go(SkyController.Preset.Title, 0f);
        Wind.Set(-75f, 0.95f);
        BaseTrack = AudioMan.Track.Calm;
        Vector3 dock = World.P("dock_muziris");
        CameraRig.I.Orbit(dock + new Vector3(10, 0, 0), 95f, 22f, 2.2f, 230f, 0.01f, 46f);
        HUD.I.HideLoading();
        HUD.I.ShowTitle(true, StartGame, StartTutorial);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void StartTutorial()
    {
        if (State != GameState.Title) return;
        Tutorial = true;
        Player.DamageTaken = 0.45f;
        Player.MaxSpeed = 15.5f * 1.3f;
        EraShift.I.DrainRate = 1f / 16f;
        Market.Gold = 260;
        HUD.I.KnownPlaces.Add("echo");
        StartGame();
    }

    void StartGame()
    {
        if (State != GameState.Title) return;
        HUD.I.ShowTitle(false);
        StartCoroutine(Prologue());
    }

    IEnumerator Prologue()
    {
        State = GameState.Cutscene;
        HUD.I.Letterbox(true);
        Vector3 dock = World.P("dock_muziris");
        CameraRig.I.Shot(dock + new Vector3(-180, 30, 120), dock + new Vector3(60, 10, 0), 40, 3f);
        CameraRig.I.Drift(new Vector3(4f, -0.3f, -2f));
        var cards = new[]
        {
            new[] { "MUZIRIS  ·  THE MALABAR COAST", "Ships from a dozen kingdoms crowd the river mouth,\ntheir holds heavy with pepper, the black gold of the world." },
            new[] { "SEVEN YEARS AGO", "The navigator Raghavan sailed west aboard the Vellimeen,\nin search of a route no chart recorded. He never returned." },
            new[] { "TODAY", "His son Arivan inherited a battered ship named the Kaveri,\nhis father's bronze compass, and a promise to find the truth." },
            new[] { "", "The monsoon is about to shift." },
        };
        if (Tutorial)
        {
            var l = new System.Collections.Generic.List<string[]>(cards);
            l.Add(new[] { "TUTORIAL VOYAGE", "Follow the Captain's Lessons on the left of the screen.\nThey explain every control and the best move at each moment.\nAssists: lighter hull damage, a stronger monsoon, a weaker raider." });
            cards = l.ToArray();
        }
        yield return HUD.I.PlayCards(cards);
        Story.Begin(0, false);
    }

    IEnumerator PlayerLost()
    {
        State = GameState.GameOver;
        CombatEnabled = false;
        HUD.I.ClearDialogue();
        yield return new WaitForSeconds(3.5f);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        HUD.I.ShowGameOver(true);
    }

    public void Respawn()
    {
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        HUD.I.FadeTo(1f, 0.6f);
        yield return new WaitForSecondsRealtime(0.7f);
        EraShift.I.ForceOff();
        Player.ResetState(Player.MaxHP);
        Player.SailLevel = 0;
        if (Raider != null) { Raider.ClearFires(); Raider.gameObject.SetActive(false); RaiderAI.Active = false; }
        CombatEnabled = false;
        HUD.I.EnemyTarget = null;
        HUD.I.ClearDialogue();
        ClearInteraction();
        Story.Begin(Checkpoint, true);
        yield return new WaitForSecondsRealtime(0.4f);
        HUD.I.FadeTo(0f, 1f);
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SetPause(bool on)
    {
        paused = on;
        Time.timeScale = on ? 0f : 1f;
        HUD.I.ShowPause(on);
        if (on) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }

    public void PlaceShip(Vector3 pos, float heading)
    {
        Player.Teleport(pos, heading);
        Player.SailLevel = 0;
        Player.SailAmt = 0;
        CameraRig.I.Target = Player;
    }

    public void SpawnRaider(Vector3 pos, float heading)
    {
        Raider.gameObject.SetActive(true);
        Raider.ResetState(Tutorial ? 90f : 140f);
        float rhp = Tutorial ? 90f : 140f;
        Raider.MaxHP = rhp;
        Raider.HP = rhp;
        RaiderAI.Accuracy = Tutorial ? 10f : 7f;
        Raider.Teleport(pos, heading);
        Raider.SailLevel = 3;
        Raider.SailAmt = 1f;
        RaiderAI.Active = true;
    }

    public void SetInteraction(string prompt, Vector3 pos, float radius, System.Action act)
    {
        interPrompt = prompt; interPos = pos; interRadius = radius; interAction = act;
    }

    public void ClearInteraction() { interAction = null; interPrompt = null; }

    public void OpenMarket(System.Action onClose)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Player.SailLevel = 0;
        HUD.I.ShowMarket(Market, onClose);
    }

    // JS hook for automated testing: unityInstance.SendMessage('Game','DevJump','5')
    public void DevTutorial(string step)
    {
        if (!Tutorial) { State = GameState.Title; StartTutorial(); }
        DevJump(step);
    }

    public void DevJump(string step)
    {
        if (!int.TryParse(step, out int s)) return;
        StopAllCoroutines();
        if (HUD.I.TitleOpen) HUD.I.ShowTitle(false);
        HUD.I.ForceCloseCards();
        HUD.I.HideLoading();
        Player.ResetState(Player.MaxHP);
        if (Raider != null) { Raider.ClearFires(); Raider.gameObject.SetActive(false); RaiderAI.Active = false; }
        CombatEnabled = false;
        HUD.I.EnemyTarget = null;
        HUD.I.ClearDialogue();
        ClearInteraction();
        EraShift.I.ForceOff();
        Story.Begin(Mathf.Clamp(s, 0, 8), true);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        float storm = SkyController.I != null ? SkyController.I.Storm : 0f;
        float era = EraShift.I != null ? EraShift.I.Amount : 0f;
        WaveField.Push(Time.time, Mathf.Lerp(0.55f, 1.65f, storm) * Mathf.Lerp(1f, 0.55f, era));
        Wind.Tick(dt, Time.time);
        if (State == GameState.Loading) return;

        // music
        if (AudioMan.I != null && AudioMan.I.Ready)
        {
            var want = BaseTrack;
            if (EraShift.I.Active && BaseTrack != AudioMan.Track.Combat) want = AudioMan.Track.Mystery;
            if (AudioMan.I.Current != want) AudioMan.I.SetMusic(want);
            AudioMan.I.DuckMusic(HUD.I.DialogueIdle ? 1f : 0.7f);
        }

        if (State == GameState.Sailing && !paused) PlayTime += dt;

        // cursor lock
        WantsLock = State == GameState.Sailing && !HUD.I.ModalOpen && !paused;
        skipFire = false;
        if (!WantsLock && Cursor.lockState == CursorLockMode.Locked) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        if (WantsLock && Cursor.lockState != CursorLockMode.Locked && In.LMBDown)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            skipFire = true;
        }

        // modal keys
        if (HUD.I.ChartOpen && (In.Down(Key.M) || In.Down(Key.Tab) || In.Down(Key.Escape))) { HUD.I.ToggleChart(); return; }
        if (HUD.I.MarketOpen && In.Down(Key.Escape)) { HUD.I.CloseMarket(); return; }
        if (HUD.I.ControlsOpen && In.Down(Key.Escape)) { HUD.I.ShowControls(false); return; }
        if (paused && (In.Down(Key.Escape) || In.Down(Key.P))) { SetPause(false); return; }

        if (Player == null) return;
        if (CanControl && BotControl && Autoplay.I != null) { Autoplay.I.Drive(dt); BotCombatAndInteract(dt); }
        else if (CanControl) PlayerInput(dt);
        else
        {
            ShoreWarning = false;
            Player.RudderInput = 0;
            HUD.I.Prompt(null);
            HUD.I.ShowReticle = false;
            CameraRig.I.Aiming = false;
        }

        ShipContacts(dt);
        damageFlash = Mathf.MoveTowards(damageFlash, 0, dt * 0.8f);
        if (PostFX.I != null)
        {
            float low = Player.Dead ? 0.6f : Mathf.Clamp01(1f - Player.HP / Player.MaxHP * 2.5f) * 0.35f;
            PostFX.I.Damage = Mathf.Max(damageFlash * 0.7f, low);
        }
    }

    public bool AutoSteer;
    public int AutoSteerUses;
    public bool ShoreWarning;

    static bool Blocked(Vector3 p)
    {
        if (World.LandHeight(p.x, p.z) > -3f) return true;
        for (int i = 0; i < World.Rocks.Count; i++)
        {
            var k = World.Rocks[i];
            if (k.w < -1.4f) continue;
            float dx = k.x - p.x, dz = k.y - p.z, rr = k.z + 7f;
            if (dx * dx + dz * dz < rr * rr) return true;
        }
        return false;
    }

    /// <summary>Steer toward a target, picking the nearest clear heading around land and reefs.</summary>
    float AutoRudder(Vector3 target)
    {
        Vector3 pp = Player.Planar;
        Vector3 to = target - pp; to.y = 0;
        float desired = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
        float dist = to.magnitude;
        float[] offs = { 0, -25, 25, -50, 50, -80, 80, -110, 110 };
        float chosen = desired;
        foreach (float o in offs)
        {
            float h = desired + o;
            Vector3 f = new Vector3(Mathf.Sin(h * Mathf.Deg2Rad), 0, Mathf.Cos(h * Mathf.Deg2Rad));
            bool clear = true;
            for (float s = 15f; s <= Mathf.Min(110f, dist + 10f); s += 15f)
                if (Blocked(pp + f * s)) { clear = false; break; }
            if (clear) { chosen = h; break; }
        }
        float err = Mathf.DeltaAngle(Player.Heading, chosen);
        return Mathf.Clamp(err / 20f, -1f, 1f);
    }

    bool DangerAhead()
    {
        if (Player.Speed < 1.5f) return false;
        Vector3 f = Player.Fwd;
        float range = 18f + Player.Speed * 3.5f;
        for (float s = 12f; s <= range; s += 6f)
        {
            Vector3 p = Player.Planar + f * s;
            if (World.LandHeight(p.x, p.z) > -1.6f) return true;
            for (int i = 0; i < World.Rocks.Count; i++)
            {
                var k = World.Rocks[i];
                if (k.w < -1.4f) continue;
                float dx = k.x - p.x, dz = k.y - p.z, rr = k.z + 3f;
                if (dx * dx + dz * dz < rr * rr) return true;
            }
        }
        return false;
    }

    public bool InInteractRange
    {
        get
        {
            if (interAction == null || Player == null) return false;
            Vector3 p = Player.transform.position;
            return Vector2.Distance(new Vector2(p.x, p.z), new Vector2(interPos.x, interPos.z)) < interRadius;
        }
    }

    public bool TryInteract()
    {
        if (!InInteractRange) return false;
        var a = interAction;
        a();
        return true;
    }

    public void StartFromBot(bool tutorial)
    {
        if (tutorial) StartTutorial(); else StartGame();
    }

    void BotCombatAndInteract(float dt)
    {
        ShoreWarning = DangerAhead();
        reload = Mathf.Max(0, reload - dt);
        HUD.I.Prompt(InInteractRange ? interPrompt : null);
        HUD.I.ShowReticle = false;
    }

    public bool BotFire(Vector3 aim, Vector3 vel)
    {
        if (!CombatEnabled || reload > 0 || Player.Dead) return false;
        if (!Combat.Broadside(Player, aim, 62f)) return false;
        Combat.I.FireVolley(Player, aim, vel, 7, 5f, 5.5f);
        reload = ReloadTime;
        return true;
    }

    public float BotRudder(Vector3 target) => AutoRudder(target);

    void PlayerInput(float dt)
    {
        if (In.Down(Key.W) || In.Down(Key.UpArrow))
        {
            if (Player.SailLevel < 3) { Player.SailLevel++; AudioMan.I.PlayAt("creak", Player.transform.position + Vector3.up * 8f, 0.5f, 1.3f); }
        }
        if (In.Down(Key.S) || In.Down(Key.DownArrow))
        {
            if (Player.SailLevel > 0) { Player.SailLevel--; AudioMan.I.PlayAt("creak", Player.transform.position + Vector3.up * 8f, 0.5f, 1.1f); }
        }
        float r = 0;
        if (In.Held(Key.A) || In.Held(Key.LeftArrow)) r -= 1f;
        if (In.Held(Key.D) || In.Held(Key.RightArrow)) r += 1f;
        if (In.Down(Key.T))
        {
            AutoSteer = !AutoSteer;
            AutoSteerUses++;
            HUD.I.Toast(AutoSteer ? "AUTO-STEER ON  ·  the crew steers to the gold diamond" : "AUTO-STEER OFF  ·  you have the helm", UIKit.Teal);
        }
        if (AutoSteer && r == 0 && HUD.I.MarkerTarget.HasValue) r = AutoRudder(HUD.I.MarkerTarget.Value);
        Player.RudderInput = r;
        ShoreWarning = DangerAhead();
        if (In.Down(Key.Q) || In.Down(Key.V))
        {
            CameraRig.I.ToggleHelm();
            HUD.I.Toast(CameraRig.I.CurMode == CameraRig.Mode.Helm ? "PERSPECTIVE SHIFT  ·  helm view" : "PERSPECTIVE SHIFT  ·  chase view", UIKit.Parch);
        }
        if (In.Down(Key.M) || In.Down(Key.Tab)) HUD.I.ToggleChart();
        if (In.Down(Key.P) || In.Down(Key.Escape)) SetPause(true);

        // interaction
        if (interAction != null)
        {
            Vector3 p = Player.transform.position;
            float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(interPos.x, interPos.z));
            if (d < interRadius)
            {
                HUD.I.Prompt(interPrompt);
                if (In.Down(Key.E))
                {
                    var a = interAction;
                    AudioMan.I.Play("click", 0.6f);
                    a();
                }
            }
            else HUD.I.Prompt(null);
        }
        else HUD.I.Prompt(null);

        // combat
        if (CombatEnabled && !Player.Dead)
        {
            reload = Mathf.Max(0, reload - dt);
            bool aiming = In.RMB && Cursor.lockState == CursorLockMode.Locked;
            CameraRig.I.Aiming = aiming;
            Vector3 targetVel = Vector3.zero;
            Vector3 aim = ComputeAim(aiming, ref targetVel);
            HUD.I.ShowReticle = true;
            HUD.I.ReticleWorld = aim;
            HUD.I.ReloadFrac = 1f - reload / ReloadTime;
            if (In.LMBDown && !skipFire && Cursor.lockState == CursorLockMode.Locked)
            {
                if (reload > 0) { }
                else if (!Combat.Broadside(Player, aim, 62f))
                    HUD.I.Hint("Your archers line the rails  ·  turn broadside to the target to loose a volley", 3f);
                else
                {
                    Combat.I.FireVolley(Player, aim, targetVel, 7, aiming ? 3.5f : 5f, 5.5f);
                    reload = ReloadTime;
                    CameraRig.I.Shake(0.12f);
                }
            }
        }
        else { HUD.I.ShowReticle = false; CameraRig.I.Aiming = false; }
    }

    Vector3 ComputeAim(bool aiming, ref Vector3 targetVel)
    {
        var cam = CameraRig.I.Cam;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, aiming ? 0.5f : 0.45f, 0));
        Vector3 pp = Player.Planar;
        Vector3 p;
        if (ray.direction.y < -0.01f) p = ray.origin + ray.direction * (-ray.origin.y / ray.direction.y);
        else p = ray.origin + new Vector3(ray.direction.x, 0, ray.direction.z).normalized * 120f;
        p.y = 0;
        Vector3 off = p - pp;
        float d = Mathf.Clamp(off.magnitude, 25f, 140f);
        p = pp + off.normalized * d;
        if (Raider != null && Raider.gameObject.activeInHierarchy && !Raider.Dead)
        {
            Vector3 rp = Raider.Planar;
            float rd = Vector3.Distance(rp, pp);
            Vector3 camFlat = new Vector3(ray.direction.x, 0, ray.direction.z);
            float ang = Vector3.Angle(camFlat, rp - new Vector3(ray.origin.x, 0, ray.origin.z));
            if (rd < 150f && (ang < (aiming ? 10f : 28f)))
            {
                targetVel = Raider.Velocity;
                return rp + Vector3.up * 1f;
            }
        }
        return p;
    }

    void ShipContacts(float dt)
    {
        ramCooldown -= dt;
        if (Raider == null || !Raider.gameObject.activeInHierarchy || Raider.Dead || Player.Dead) return;
        Vector3 d = Raider.Planar - Player.Planar;
        float dist = d.magnitude;
        if (dist < 11f && dist > 0.01f)
        {
            Vector3 n = d / dist;
            float push = (11f - dist) * 0.5f;
            Player.Nudge(-n * push);
            Raider.Nudge(n * push);
            float closing = Vector3.Dot(Player.Velocity - Raider.Velocity, n);
            if (closing > 2.5f && ramCooldown <= 0)
            {
                ramCooldown = 1.2f;
                bool playerRams = Vector3.Angle(Player.Fwd, n) < 40f;
                bool raiderRams = Vector3.Angle(Raider.Fwd, -n) < 40f;
                Vector3 hit = Player.Planar + n * 5f + Vector3.up * 1.5f;
                Raider.Damage(closing * (playerRams ? 6f : 2f), hit, false);
                Player.Damage(closing * (raiderRams ? 3.5f : 1.2f), hit, false);
                FX.Impact(hit, 2f);
                FX.Splash(hit, 3f);
                AudioMan.I.PlayAt("crash", hit, 1f);
                CameraRig.I.Shake(0.6f);
                if (playerRams) HUD.I.Toast("RAMMED!", UIKit.Gold);
                Player.Speed *= 0.4f;
                Raider.Speed *= 0.3f;
            }
        }
    }
}
