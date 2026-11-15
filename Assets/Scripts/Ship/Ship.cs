using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Arcade-sim sailing ship: square-sail point-of-sail efficiency, steering oar, Gerstner buoyancy,
/// land/reef collisions, fire damage and sinking.
/// </summary>
public class Ship : MonoBehaviour
{
    public ShipParts P;
    public bool IsPlayer;
    public string DisplayName = "Ship";
    public float MaxHP = 100f, HP = 100f;
    public float Heading, Speed, AngVel;
    public int SailLevel;
    public float SailAmt;
    public float Rudder, RudderInput;
    public float MaxSpeed = 15.5f;
    public float MinDrive = 3f;
    public float TurnMul = 1f;
    public float FireDamage = 0.7f;
    public float DamageTaken = 1f;
    public bool LanternOn = true;
    Light lanternPoint;

    public void SetLantern(bool on)
    {
        LanternOn = on;
        if (P != null && P.lanternLight != null) P.lanternLight.gameObject.SetActive(on);
        if (lanternPoint == null && P != null && P.lanternLight != null && IsPlayer)
        {
            lanternPoint = new GameObject("LanternLight").AddComponent<Light>();
            lanternPoint.transform.SetParent(P.lanternLight.parent, false);
            lanternPoint.transform.position = P.lanternLight.GetComponent<Renderer>().bounds.center;
            lanternPoint.type = LightType.Point;
            lanternPoint.color = new Color(1f, 0.65f, 0.3f);
            lanternPoint.range = 22f;
            lanternPoint.shadows = LightShadows.None;
        }
        if (lanternPoint != null) { lanternPoint.enabled = on; lanternPoint.intensity = (SkyController.I != null && SkyController.I.Era < 0.5f) ? 1.6f : 1.6f; }
    }
    public bool Sinking, Dead, Frozen;
    public float LastDamageTime = -99f;
    public float WindEff, WindAngle;
    public System.Action<float> OnDamaged;
    public System.Action OnDeath;
    public int OceanSlot = -1;

    Vector3 planar;
    float pitch, roll, heave, sinkT;
    float collideCooldown, creakTimer = 2f;
    ParticleSystem bowSpray;
    readonly List<(ParticleSystem ps, float t)> fires = new List<(ParticleSystem, float)>();
    float sprayAcc;
    Vector3[] crewBase;
    float[] crewYaw;

    public Vector3 Fwd => new Vector3(Mathf.Sin(Heading * Mathf.Deg2Rad), 0, Mathf.Cos(Heading * Mathf.Deg2Rad));
    public Vector3 Right => new Vector3(Fwd.z, 0, -Fwd.x);
    public Vector3 Planar => planar;
    public Vector3 Velocity => Fwd * Speed;
    public int FireCount => fires.Count;

    public static Ship Create(ShipBuilder.Style style, Transform parent, string name, bool player)
    {
        var parts = ShipBuilder.Build(style, parent);
        var s = parts.root.gameObject.AddComponent<Ship>();
        s.P = parts;
        s.IsPlayer = player;
        s.DisplayName = name;
        s.SetupFx();
        return s;
    }

    void SetupFx()
    {
        var go = new GameObject("BowSpray");
        go.transform.SetParent(P.model, false);
        go.transform.localPosition = new Vector3(0, 0.2f, 9.2f);
    }

    public void Teleport(Vector3 pos, float heading)
    {
        planar = new Vector3(pos.x, 0, pos.z);
        Heading = heading;
        Speed = 0; AngVel = 0; Rudder = 0; RudderInput = 0;
        heave = WaveField.HeightAt(pos) - 0.15f;
        transform.position = new Vector3(planar.x, heave, planar.z);
        transform.rotation = Quaternion.Euler(0, Heading, 0);
    }

    void OnDisable() { if (OceanSlot >= 0 && Ocean.I != null) Ocean.I.ClearShip(OceanSlot); }

    public void Nudge(Vector3 delta) { planar += new Vector3(delta.x, 0, delta.z); }

    public void ResetState(float hp)
    {
        Sinking = false; Dead = false; sinkT = 0;
        MaxHP = Mathf.Max(MaxHP, hp);
        HP = hp;
        ClearFires();
        gameObject.SetActive(true);
    }

    public void ClearFires()
    {
        foreach (var f in fires) FX.StopFire(f.ps);
        fires.Clear();
    }

    public static float Efficiency(float angle)
    {
        float a = Mathf.Abs(angle);
        if (a < 60f) return Mathf.Lerp(0.96f, 1f, a / 60f);
        if (a < 100f) return Mathf.Lerp(1f, 0.74f, (a - 60f) / 40f);
        if (a < 135f) return Mathf.Lerp(0.74f, 0.18f, (a - 100f) / 35f);
        return Mathf.Lerp(0.18f, 0.0f, (a - 135f) / 45f);
    }

    public static string PointOfSail(float angle)
    {
        float a = Mathf.Abs(angle);
        if (a < 30f) return "RUNNING";
        if (a < 75f) return "BROAD REACH";
        if (a < 105f) return "BEAM REACH";
        if (a < 140f) return "CLOSE HAULED";
        return "IN IRONS";
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        if (!Frozen) Simulate(dt);
        Visuals(dt);
        UpdateFires(dt);
        if (OceanSlot >= 0 && Ocean.I != null)
        {
            if (Sinking && sinkT > 5f) Ocean.I.ClearShip(OceanSlot);
            else Ocean.I.SetShip(OceanSlot, planar, Fwd, Speed);
        }
    }

    void Simulate(float dt)
    {
        if (Sinking)
        {
            Speed = Mathf.Lerp(Speed, 0, dt * 0.5f);
            planar += Fwd * Speed * dt;
            return;
        }
        float target = SailLevel / 3f;
        SailAmt = Mathf.MoveTowards(SailAmt, target, dt * 0.55f);
        WindAngle = Vector3.SignedAngle(Fwd, Wind.Dir3, Vector3.up);
        WindEff = Efficiency(WindAngle);
        float ws = Mathf.Clamp(Wind.Effective, 0.3f, 1.5f);
        float drive = MaxSpeed * SailAmt * WindEff * ws;
        if (SailLevel > 0) drive = Mathf.Max(drive, MinDrive * SailAmt);
        float k = drive > Speed ? 0.32f : 0.45f;
        Speed = Mathf.Lerp(Speed, drive, 1f - Mathf.Exp(-k * dt));

        Rudder = Mathf.MoveTowards(Rudder, RudderInput, dt * 2.4f);
        float turnMax = Mathf.Lerp(9f, 22f, Mathf.Clamp01(Speed / 7f)) * TurnMul;
        AngVel = Mathf.Lerp(AngVel, Rudder * turnMax, 1f - Mathf.Exp(-2.2f * dt));
        Heading = Mathf.Repeat(Heading + AngVel * dt, 360f);

        Vector3 drift = Wind.Dir3 * Mathf.Max(0f, Wind.Effective - 1.05f) * 1.6f;
        planar += (Fwd * Speed + drift) * dt;

        collideCooldown -= dt;
        Collide(dt);
    }

    void Collide(float dt)
    {
        Vector3 f = Fwd, r = Right;
        Vector3[] probes = { planar + f * 9.5f, planar + f * 5f + r * 2f, planar + f * 5f - r * 2f, planar + r * 2.6f, planar - r * 2.6f, planar - f * 8.5f };
        float worst = -99f; Vector3 worstP = planar;
        foreach (var p in probes)
        {
            float h = World.LandHeight(p.x, p.z);
            if (h > worst) { worst = h; worstP = p; }
        }
        const float draft = -1.4f;
        if (worst > draft)
        {
            Vector3 g = World.LandGradient(worstP.x, worstP.z);
            Vector3 push = g.sqrMagnitude > 1e-6f ? -g.normalized : -(worstP - planar).normalized;
            push.y = 0;
            planar += push * Mathf.Clamp((worst - draft) * 0.8f + 0.4f, 0.3f, 3f);
            Impact(worstP, Mathf.Abs(Speed), true);
        }
        // reefs
        for (int i = 0; i < World.Rocks.Count; i++)
        {
            Vector4 rk = World.Rocks[i];
            float dx = rk.x - planar.x, dz = rk.y - planar.z;
            if (dx * dx + dz * dz > 900f) continue;
            if (rk.w < draft) continue;
            for (int j = 0; j < probes.Length; j++)
            {
                Vector3 p = probes[j];
                float ex = p.x - rk.x, ez = p.z - rk.y;
                float d = Mathf.Sqrt(ex * ex + ez * ez);
                if (d < rk.z + 0.6f)
                {
                    Vector3 away = new Vector3(ex, 0, ez) / Mathf.Max(0.01f, d);
                    planar += away * (rk.z + 0.6f - d + 0.3f);
                    Impact(p, Mathf.Abs(Speed) + 2f, true);
                    break;
                }
            }
        }
    }

    public void Impact(Vector3 point, float speed, bool slow)
    {
        if (collideCooldown > 0f) { if (slow) Speed *= 0.9f; return; }
        collideCooldown = 0.7f;
        bool gentle = IsPlayer && Game.I != null && Game.I.Tutorial;
        if (gentle && speed > 1.2f)
        {
            FX.Splash(new Vector3(point.x, 0.2f, point.z), 1.5f);
            if (AudioMan.I != null) AudioMan.I.PlayAt("thunk", point, 0.6f, 0.7f);
            if (HUD.I != null) HUD.I.Toast("Bump! Land is a wall for ships. Steer back to open water.", UIKit.Gold);
            if (CameraRig.I != null) CameraRig.I.Shake(0.15f);
        }
        else if (speed > 2.2f)
        {
            float dmg = (speed - 1.8f) * 3.2f;
            Damage(dmg, new Vector3(point.x, 0.8f, point.z), false);
            FX.Impact(new Vector3(point.x, 0.8f, point.z), 1.2f);
            FX.Splash(new Vector3(point.x, 0.2f, point.z), 2f);
            if (AudioMan.I != null) AudioMan.I.PlayAt("crash", point, Mathf.Clamp01(speed / 10f) + 0.3f);
            if (IsPlayer && CameraRig.I != null) CameraRig.I.Shake(Mathf.Clamp01(speed / 12f) + 0.2f);
            if (IsPlayer && HUD.I != null) HUD.I.Toast("The hull scrapes the rocks!", UIKit.Red);
        }
        if (slow) Speed = -Mathf.Abs(Speed) * 0.25f;
    }

    public void Damage(float amount, Vector3 worldPoint, bool fire)
    {
        if (Dead) return;
        amount *= DamageTaken;
        HP -= amount;
        LastDamageTime = Time.time;
        OnDamaged?.Invoke(amount);
        if (fire && fires.Count < 5 && Random.value < 0.28f) AddFire();
        if (HP <= 0f)
        {
            HP = 0f;
            Die();
        }
    }

    void AddFire()
    {
        if (P.firePoints.Count == 0) return;
        Vector3 lp = P.firePoints[Random.Range(0, P.firePoints.Count)];
        var ps = FX.AttachFire(P.model, lp, Random.Range(0.8f, 1.2f));
        fires.Add((ps, Random.Range(6f, 10f)));
    }

    void UpdateFires(float dt)
    {
        float stormDouse = SkyController.I != null ? SkyController.I.Storm : 0f;
        for (int i = fires.Count - 1; i >= 0; i--)
        {
            var f = fires[i];
            f.t -= dt * (1f + stormDouse * 2f);
            if (!Dead) HP -= FireDamage * DamageTaken * dt;
            if (Random.value < dt * 2f) FX.Embers(f.ps.transform.position, 2);
            if (f.t <= 0f && !Sinking) { FX.StopFire(f.ps); fires.RemoveAt(i); }
            else fires[i] = f;
        }
        if (!Dead && HP <= 0f) { HP = 0; Die(); }
        if (IsPlayer && !Dead && Time.time - LastDamageTime > 8f && HP < MaxHP && fires.Count == 0)
            HP = Mathf.Min(MaxHP, HP + 1.6f * dt);
    }

    void Die()
    {
        if (Dead) return;
        Dead = true;
        Sinking = true;
        sinkT = 0;
        SailLevel = 0;
        for (int i = 0; i < 3; i++) AddFire();
        FX.BigSmoke(transform.position + Vector3.up * 3f, 6);
        FX.Impact(transform.position + Vector3.up * 2f, 3f);
        if (AudioMan.I != null) { AudioMan.I.PlayAt("crash", transform.position, 1f, 0.7f); AudioMan.I.PlayAt("sink", transform.position, 1f); }
        OnDeath?.Invoke();
    }

    void Visuals(float dt)
    {
        Vector3 f = Fwd, r = Right;
        float hb = WaveField.HeightAt(planar + f * 8.5f), hs = WaveField.HeightAt(planar - f * 8.5f);
        float hp = WaveField.HeightAt(planar - r * 2.6f), hr = WaveField.HeightAt(planar + r * 2.6f);
        float hc = WaveField.HeightAt(planar);
        float y = (hb + hs + hp + hr + hc * 2f) / 6f;
        float tPitch = Mathf.Atan2(hb - hs, 17f) * Mathf.Rad2Deg;
        float tRoll = Mathf.Atan2(hr - hp, 5.2f) * Mathf.Rad2Deg;
        float heel = -Mathf.Sin(WindAngle * Mathf.Deg2Rad) * SailAmt * WindEff * Mathf.Clamp(Wind.Effective, 0.5f, 1.6f) * 5.5f + AngVel * 0.3f;
        pitch = Mathf.Lerp(pitch, tPitch, 1f - Mathf.Exp(-3f * dt));
        roll = Mathf.Lerp(roll, tRoll + heel, 1f - Mathf.Exp(-2.5f * dt));
        heave = Mathf.Lerp(heave, y - 0.12f, 1f - Mathf.Exp(-4f * dt));
        float extraPitch = 0, extraRoll = 0, sink = 0;
        if (Sinking)
        {
            sinkT += dt;
            sink = sinkT * sinkT * 0.12f;
            extraPitch = Mathf.Min(25f, sinkT * 3f);
            extraRoll = Mathf.Min(35f, sinkT * 4.5f);
            if (sinkT < 6f && Random.value < dt * 3f) FX.BigSmoke(transform.position + Vector3.up * 4f, 1);
            if (sinkT > 14f) gameObject.SetActive(false);
        }
        transform.position = new Vector3(planar.x, heave - sink, planar.z);
        transform.rotation = Quaternion.Euler(-pitch + extraPitch, Heading, roll + extraRoll);

        if (P.yard != null)
        {
            float trim = Mathf.Clamp(WindAngle * 0.45f, -48f, 48f);
            P.yard.localRotation = Quaternion.Slerp(P.yard.localRotation, Quaternion.Euler(0, trim, 0), 1f - Mathf.Exp(-1.5f * dt));
        }
        if (P.sail != null)
        {
            float sc = Mathf.Lerp(0.1f, 1f, SailAmt);
            P.sail.localScale = new Vector3(1f, sc, 1f);
            if (P.sailMat != null && P.sailMat.HasProperty("_Billow"))
            {
                float storm = SkyController.I != null ? SkyController.I.Storm : 0;
                float backwind = Mathf.Abs(WindAngle) > 140f ? -0.4f : 1f;
                P.sailMat.SetFloat("_Billow", SailAmt * (WindEff * 1.5f + 0.15f) * Mathf.Clamp(Wind.Effective, 0.5f, 1.4f) * backwind);
                P.sailMat.SetFloat("_Flutter", (1f - WindEff) * SailAmt * 1.6f + storm * 0.7f + 0.05f);
            }
        }
        if (P.flag != null)
        {
            float flagYaw = Vector3.SignedAngle(Fwd, Wind.Dir3, Vector3.up) + 180f;
            P.flag.localRotation = Quaternion.Euler(0, flagYaw, 0);
            if (P.flagMat != null && P.flagMat.HasProperty("_Flutter")) P.flagMat.SetFloat("_Flutter", 2.5f + Wind.Effective);
        }
        if (P.oar != null) P.oar.localRotation = Quaternion.Euler(0, -Rudder * 28f, 0);
        if (crewBase == null)
        {
            crewBase = new Vector3[P.crew.Count];
            crewYaw = new float[P.crew.Count];
            for (int i = 0; i < P.crew.Count; i++) { crewBase[i] = P.crew[i].localPosition; crewYaw[i] = P.crew[i].localEulerAngles.y; }
        }
        for (int i = 0; i < P.crew.Count; i++)
        {
            var c = P.crew[i];
            float bob = Mathf.Sin(Time.time * (1.3f + i * 0.17f) + i) * 0.03f;
            c.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 0.8f + i) * 3f, crewYaw[i] + Mathf.Sin(Time.time * 0.25f + i) * 20f * (i == 0 ? 0.2f : 1f), -roll * 0.4f);
            c.localPosition = crewBase[i] + Vector3.up * bob;
        }

        // bow spray & creaks
        if (!Sinking && Speed > 3f)
        {
            sprayAcc += dt * (Speed - 2f) * 2.2f * (1f + (SkyController.I != null ? SkyController.I.Storm : 0));
            while (sprayAcc > 1f)
            {
                sprayAcc -= 1f;
                Vector3 bow = transform.TransformPoint(new Vector3(Random.Range(-1.2f, 1.2f), 0.3f, 9f));
                bow.y = WaveField.HeightAt(bow) + 0.3f;
                FX.Spray(bow, 1);
            }
        }
        if (IsPlayer)
        {
            creakTimer -= dt;
            if (creakTimer <= 0)
            {
                creakTimer = Random.Range(3f, 7f) / (1f + (SkyController.I != null ? SkyController.I.Storm * 1.5f : 0));
                if (AudioMan.I != null) AudioMan.I.PlayAt("creak", transform.position + Vector3.up * 2f, Random.Range(0.25f, 0.5f), Random.Range(0.85f, 1.15f));
            }
        }
    }
}
