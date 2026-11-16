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

    public Ship Player, Raider, Flagship;
    public EnemyAI RaiderAI, FlagshipAI;
    [System.NonSerialized] public Ship[] Allies;
    [System.NonSerialized] public AllyAI[] AllyAIs;
    public static int PendingChapter;
    public int Chapter = 1;

    public Ship CurrentEnemy
    {
        get
        {
            Ship best = null; float bd = 1e9f;
            foreach (var e in new[] { Raider, Flagship })
            {
                if (e == null || !e.gameObject.activeInHierarchy || e.Dead) continue;
                float d = Player != null ? Vector3.Distance(e.Planar, Player.Planar) : 0f;
                if (e == Flagship) d -= 60f;
                if (d < bd) { bd = d; best = e; }
            }
            return best;
        }
    }

    public void LoadChapter(int c)
    {
        PendingChapter = c;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
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
    float ReloadTime => 2.3f * Progress.S.ReloadMul;
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
        {
            var a0 = System.Environment.GetCommandLineArgs();
            Progress.Persist = System.Array.IndexOf(a0, "-autoplay") < 0 && System.Array.IndexOf(a0, "-capture") < 0;
            Progress.Load();
            Progress.Apply();
        }
        WaveField.Push(0, 0.55f);
        var bootArgs = System.Environment.GetCommandLineArgs();
        int ci = System.Array.IndexOf(bootArgs, "-chapter");
        if (ci >= 0 && ci + 1 < bootArgs.Length && (bootArgs[ci + 1] == "2" || bootArgs[ci + 1] == "3" || bootArgs[ci + 1] == "4")) PendingChapter = int.Parse(bootArgs[ci + 1]);
        Chapter = PendingChapter >= 2 && PendingChapter <= 4 ? PendingChapter : 1;
        yield return World.Build(transform, (p, s) => HUD.I.SetLoading(0.05f + p * 0.7f, s), Chapter);
        World.BuildSites(Chapter);

        Player = Ship.Create(ShipBuilder.Style.Kaveri, transform, "Kaveri", true);
        Player.OceanSlot = 0;
        Player.Teleport(World.P("start"), Chapter == 3 ? 90f : Chapter == 2 ? 180f : 270f);
        Player.OnDamaged += a => { damageFlash = Mathf.Min(1f, damageFlash + a / 18f); CameraRig.I.Shake(Mathf.Clamp01(a / 25f)); };
        Player.OnDeath += () => { Progress.OnDefeat(); StartCoroutine(PlayerLost()); };
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

        if (Chapter == 2)
        {
            Market.UseIncenseGoods();
            Market.Gold = 220;
            Flagship = Ship.Create(ShipBuilder.Style.Raider, transform, "Black Compass", false);
            Flagship.transform.localScale = Vector3.one * 1.35f;
            Flagship.OceanSlot = 2;
            Flagship.MaxSpeed = 13f;
            Flagship.MinDrive = 6f;
            Flagship.FireDamage = 1.0f;
            FlagshipAI = Flagship.gameObject.AddComponent<EnemyAI>();
            FlagshipAI.Me = Flagship;
            FlagshipAI.Target = Player;
            FlagshipAI.VolleyCount = 7;
            FlagshipAI.VolleyDamage = 1.9f;
            FlagshipAI.FireInterval = 4.3f;
            FlagshipAI.Accuracy = 9f;
            Combat.I.Ships.Add(Flagship);
            Flagship.gameObject.SetActive(false);
            Stealth.Create(transform);
        }
        if (Chapter == 3)
        {
            Market.UseAlexGoods();
            Market.Gold = 120;
            Flagship = Ship.Create(ShipBuilder.Style.Raider, transform, "Raider II", false);
            Flagship.OceanSlot = 2;
            Flagship.MaxSpeed = 14f;
            Flagship.MinDrive = 6.5f;
            Flagship.FireDamage = 1.2f;
            FlagshipAI = Flagship.gameObject.AddComponent<EnemyAI>();
            FlagshipAI.Me = Flagship;
            FlagshipAI.Target = Player;
            FlagshipAI.VolleyDamage = 2.0f;
            FlagshipAI.Accuracy = 8.5f;
            Combat.I.Ships.Add(Flagship);
            Flagship.gameObject.SetActive(false);
            Stealth.Create(transform);
        }

        if (Chapter == 4)
        {
            Flagship = Ship.Create(ShipBuilder.Style.Raider, transform, "The Last Map", false);
            Flagship.transform.localScale = Vector3.one * 1.5f;
            Flagship.OceanSlot = 2;
            Flagship.MaxSpeed = 13f;
            Flagship.MinDrive = 6f;
            Flagship.FireDamage = 0.9f;
            Flagship.Team = 1;
            FlagshipAI = Flagship.gameObject.AddComponent<EnemyAI>();
            FlagshipAI.Me = Flagship;
            FlagshipAI.Target = Player;
            FlagshipAI.VolleyCount = 8;
            FlagshipAI.VolleyDamage = 2.0f;
            FlagshipAI.FireInterval = 4.4f;
            FlagshipAI.Accuracy = 9f;
            Combat.I.Ships.Add(Flagship);
            Flagship.gameObject.SetActive(false);

            string[] names = { "Thirayar", "Mayil" };
            Allies = new Ship[2];
            AllyAIs = new AllyAI[2];
            for (int i = 0; i < 2; i++)
            {
                var a = Ship.Create(ShipBuilder.Style.Merchant, transform, names[i], false);
                a.Team = 0;
                a.MaxHP = a.HP = 160f;
                a.MaxSpeed = 17.5f;
                a.MinDrive = 7f;
                a.DamageTaken = 0.35f;
                a.FireDamage = 0.5f;
                a.IgnoreReefs = true;
                var ai = a.gameObject.AddComponent<AllyAI>();
                ai.Me = a;
                ai.FollowDist = 55f + i * 50f;
                ai.Side = i == 0 ? 1f : -1f;
                Combat.I.Ships.Add(a);
                Allies[i] = a;
                AllyAIs[i] = ai;
            }
        }
        Raider.Team = 1;
        if (Flagship != null) Flagship.Team = 1;
        Raider.OnDeath += Progress.OnVictory;
        if (Flagship != null) Flagship.OnDeath += Progress.OnVictory;
        if (Market.Gold < Progress.D.Gold) Market.Gold = Progress.D.Gold;
        Progress.EnsureBaseline(Chapter);
        Progress.Apply();
        Player.HP = Player.MaxHP;
        Ledger.Create();
        Shore.Create(transform);

        rig.Target = Player;
        HUD.I.InitChart();
        Tutor.Create();
        HUD.I.SetLoading(0.76f, "Tuning the sea");
        yield return AudioMan.I.Generate((p, s) => HUD.I.SetLoading(0.76f + p * 0.24f, s));
        HUD.I.SetLoading(1f, "Ready");
        yield return null;
        var args = System.Environment.GetCommandLineArgs();
        if (System.Array.IndexOf(args, "-capture") >= 0) { HUD.I.HideLoading(); gameObject.AddComponent<Director>(); yield break; }
        if (Chapter == 2) StartChapter2(); else if (Chapter == 3) StartChapter3(); else if (Chapter == 4) StartChapter4(); else Title();
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
        Progress.Apply();
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

    void StartChapter4()
    {
        State = GameState.Title;
        HUD.I.HideLoading();
        HUD.I.ShowTitle(false);
        StartCoroutine(Prologue4());
    }

    IEnumerator Prologue4()
    {
        State = GameState.Cutscene;
        HUD.I.Letterbox(true);
        SkyController.I.Go(SkyController.Preset.Dawn, 0f);
        BaseTrack = AudioMan.Track.Mystery;
        Vector3 dock = World.P("dock_muziris");
        CameraRig.I.Shot(dock + new Vector3(-180, 30, 120), dock + new Vector3(60, 10, 0), 40, 0.01f);
        CameraRig.I.Drift(new Vector3(4f, -0.3f, -2f));
        var cards = new[]
        {
            new[] { "CHAPTER IV", "ACROSS SEVEN SEAS" },
            new[] { "MUZIRIS  \u00b7  HOMECOMING", "Raghavan is home at last. But the chart from the Vellimeen points south,\nbeyond Kadal, beyond the edge of every known map." },
            new[] { "THE STORM WALL", "A storm that has never ended guards the way to the Sunken Archive,\nwhere the first navigators wrote down every route of the sea." },
            new[] { "", "Whoever holds the Archive holds the sea." },
        };
        yield return HUD.I.PlayCards(cards);
        Story.Begin(0, false);
    }

    void StartChapter3()
    {
        State = GameState.Title;
        HUD.I.HideLoading();
        HUD.I.ShowTitle(false);
        StartCoroutine(Prologue3());
    }

    IEnumerator Prologue3()
    {
        State = GameState.Cutscene;
        HUD.I.Letterbox(true);
        SkyController.I.Go(SkyController.Preset.Dawn, 0f);
        BaseTrack = AudioMan.Track.Mystery;
        Vector3 dock = World.P("dock_berenike");
        CameraRig.I.Shot(dock + new Vector3(180, 35, 120), dock + new Vector3(-30, 6, -10), 42, 0.01f);
        CameraRig.I.Drift(new Vector3(-3f, -0.2f, -2f));
        var cards = new[]
        {
            new[] { "CHAPTER III", "THE GREAT MARKETPLACE" },
            new[] { "BERENIKE  ·  THE RED SEA", "The ledger of the Black Compass named one prisoner:\nRaghavan of Muziris, sent north to Alexandria." },
            new[] { "ALEXANDRIA", "The greatest port in the world, crowned by the Pharos lighthouse,\nand the place where the Hidden Cartographers keep their secrets." },
            new[] { "", "The only fast way there was lost to the sand long ago." },
        };
        yield return HUD.I.PlayCards(cards);
        Story.Begin(0, false);
    }

    void StartChapter2()
    {
        State = GameState.Title;
        HUD.I.HideLoading();
        HUD.I.ShowTitle(false);
        StartCoroutine(Prologue2());
    }

    IEnumerator Prologue2()
    {
        State = GameState.Cutscene;
        HUD.I.Letterbox(true);
        SkyController.I.Go(SkyController.Preset.Dawn, 0f);
        BaseTrack = AudioMan.Track.Mystery;
        Vector3 dock = World.P("dock_ras");
        CameraRig.I.Shot(dock + new Vector3(160, 30, -150), dock + new Vector3(-40, 8, 20), 42, 0.01f);
        CameraRig.I.Drift(new Vector3(-3f, -0.2f, 2f));
        var cards = new[]
        {
            new[] { "CHAPTER II", "THE GATE OF TEARS" },
            new[] { "RAS AL-MIRA  ·  THE MORNING AFTER", "Raghavan's chart shows a passage beyond the edge of every known map.\nIts first leg runs west, through Aden, and through the Gate of Tears." },
            new[] { "THE GATE OF TEARS", "A narrow strait between Arabia and Azania,\nchained shut by the ships and watchfires of the Hidden Cartographers." },
            new[] { "", "Some say no ship slips through the Gate unseen." },
        };
        yield return HUD.I.PlayCards(cards);
        Story.Begin(0, false);
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
        if (Shore.I != null) Shore.I.Return(false);
        Player.ResetState(Player.MaxHP);
        Player.SailLevel = 0;
        ClearEnemies();
        CombatEnabled = false;
        HUD.I.EnemyTarget = null;
        HUD.I.ClearDialogue();
        ClearInteraction();
        if (Stealth.I != null) Stealth.I.ResetState();
        Story.Begin(Checkpoint, true);
        yield return new WaitForSecondsRealtime(0.4f);
        HUD.I.FadeTo(0f, 1f);
    }

    public void Restart()
    {
        LoadChapter(Chapter);
    }

    public void ClearEnemies()
    {
        foreach (var e in new[] { Raider, Flagship })
            if (e != null) { e.ClearFires(); e.gameObject.SetActive(false); }
        if (RaiderAI != null) RaiderAI.Active = false;
        if (FlagshipAI != null) FlagshipAI.Active = false;
    }

    public void SpawnFlagship(Vector3 pos, float heading, float hp = 240f)
    {
        Flagship.gameObject.SetActive(true);
        Flagship.ResetState(hp);
        Flagship.MaxHP = hp;
        Flagship.HP = hp;
        Flagship.Teleport(pos, heading);
        Flagship.SailLevel = 3;
        Flagship.SailAmt = 1f;
        FlagshipAI.Active = true;
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
        if (Shore.I != null) Shore.I.Return(false);
        Player.ResetState(Player.MaxHP);
        ClearEnemies();
        if (Stealth.I != null) Stealth.I.ResetState();
        CombatEnabled = false;
        HUD.I.EnemyTarget = null;
        HUD.I.ClearDialogue();
        ClearInteraction();
        EraShift.I.ForceOff();
        Story.Begin(Mathf.Clamp(s, 0, Chapter == 1 ? 8 : Chapter == 2 ? 6 : 5), true);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        float storm = SkyController.I != null ? SkyController.I.Storm : 0f;
        float era = EraShift.I != null ? EraShift.I.Amount : 0f;
        WaveField.Push(Time.time, Mathf.Lerp(0.55f, 1.65f, storm) * Mathf.Lerp(1f, 0.55f, era));
        Wind.Tick(dt, Time.time);
        Shader.SetGlobalFloat("_TideG", World.Tide);
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
        if (Shore.I != null && Shore.I.OnFoot) { Shore.I.Tick(dt); ShoreWarning = false; HUD.I.ShowReticle = false; }
        else if (CanControl && BotControl && Autoplay.I != null) { Autoplay.I.Drive(dt); BotCombatAndInteract(dt); }
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
        if (World.LandHeight(p.x, p.z) > -3f + World.Tide) return true;
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
            if (World.LandHeight(p.x, p.z) > -1.6f + World.Tide) return true;
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
        if (State != GameState.Title || Chapter >= 2) return;
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
        Combat.I.FireVolley(Player, aim, vel, Progress.S.Volley, 5f, 5.5f * Progress.S.VolleyDmgMul);
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
        if (In.Down(Key.M)) HUD.I.ToggleChart();
        if (In.Down(Key.Tab) && Ledger.I != null) { Player.RudderInput = 0; Ledger.I.Open(0); return; }
        if (In.Down(Key.P) || In.Down(Key.Escape)) SetPause(true);
        if (In.Down(Key.L))
        {
            Player.SetLantern(!Player.LanternOn);
            HUD.I.Toast(Player.LanternOn ? "LANTERN LIT  ·  you can be seen from afar" : "LANTERN DOUSED  ·  you are harder to see", Player.LanternOn ? UIKit.Gold : UIKit.Teal);
        }

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
            else HUD.I.Prompt(Shore.I != null ? Shore.I.ShipPrompt : null);
        }
        else HUD.I.Prompt(Shore.I != null ? Shore.I.ShipPrompt : null);

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
                    Combat.I.FireVolley(Player, aim, targetVel, Progress.S.Volley, aiming ? 3.5f : 5f, 5.5f * Progress.S.VolleyDmgMul);
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
        var foe = CurrentEnemy;
        if (foe != null)
        {
            Vector3 rp = foe.Planar;
            float rd = Vector3.Distance(rp, pp);
            Vector3 camFlat = new Vector3(ray.direction.x, 0, ray.direction.z);
            float ang = Vector3.Angle(camFlat, rp - new Vector3(ray.origin.x, 0, ray.origin.z));
            if (rd < 150f && (ang < (aiming ? 10f : 28f)))
            {
                targetVel = foe.Velocity;
                return rp + Vector3.up * 1f;
            }
        }
        return p;
    }

    void ShipContacts(float dt)
    {
        ramCooldown -= dt;
        Contact(Raider, 11f);
        Contact(Flagship, Chapter == 4 ? 16f : 14f);
        if (Allies != null && Allies.Length == 2)
        {
            foreach (var a in Allies) Separate(Player, a, 15f);
            Separate(Allies[0], Allies[1], 15f);
            foreach (var a in Allies) { Separate(a, Raider, 13f); Separate(a, Flagship, 16f); }
        }
    }

    static void Separate(Ship a, Ship b, float reach)
    {
        if (a == null || b == null || !a.gameObject.activeInHierarchy || !b.gameObject.activeInHierarchy || a.Dead || b.Dead) return;
        Vector3 d = b.Planar - a.Planar;
        float dist = d.magnitude;
        if (dist >= reach || dist < 0.01f) return;
        Vector3 n = d / dist;
        float push = (reach - dist) * 0.5f;
        if (!a.IsPlayer) a.Nudge(-n * push); else a.Nudge(-n * push * 0.3f);
        b.Nudge(n * push * (a.IsPlayer ? 1.7f : 1f));
    }

    void Contact(Ship Raider, float reach)
    {
        if (Raider == null || !Raider.gameObject.activeInHierarchy || Raider.Dead || Player.Dead) return;
        Vector3 d = Raider.Planar - Player.Planar;
        float dist = d.magnitude;
        if (dist < reach && dist > 0.01f)
        {
            Vector3 n = d / dist;
            float push = (reach - dist) * 0.5f;
            Player.Nudge(-n * push);
            Raider.Nudge(n * push);
            float closing = Vector3.Dot(Player.Velocity - Raider.Velocity, n);
            if (closing > 2.5f && ramCooldown <= 0)
            {
                ramCooldown = 1.2f;
                bool playerRams = Vector3.Angle(Player.Fwd, n) < 40f;
                bool raiderRams = Vector3.Angle(Raider.Fwd, -n) < 40f;
                Vector3 hit = Player.Planar + n * 5f + Vector3.up * 1.5f;
                Raider.Damage(closing * (playerRams ? 6f * Progress.S.RamMul : 2f), hit, false);
                Player.Damage(closing * (raiderRams ? 3.5f : 1.2f * Progress.S.RamSelfMul), hit, false);
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
