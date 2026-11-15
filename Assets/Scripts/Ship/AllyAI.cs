using System.Collections.Generic;
using UnityEngine;

/// <summary>Guild ally: sails in the Kaveri's wake (following her exact track), and joins any fight.</summary>
public class AllyAI : MonoBehaviour
{
    public Ship Me;
    public bool Active;
    public bool Engage = true;
    public float FollowDist = 50f;
    public float Side = 1f;
    public float Waiting = -1f;  // >=0: hold position at WaitPos with this heading
    public Vector3 WaitPos;
    float fireTimer = 3f;

    static readonly List<Vector3> trail = new List<Vector3>();
    static float trailFrame = -1;

    public static void ResetTrail() { trail.Clear(); }

    /// <summary>Lays a straight wake behind the player so followers have a track before she moves.</summary>
    public static void SeedTrail(Vector3 pos, Vector3 fwd)
    {
        trail.Clear();
        for (float d = 150f; d > 0f; d -= 6f) trail.Add(pos - fwd * d);
    }

    static void RecordTrail()
    {
        if (trailFrame == Time.frameCount) return;
        trailFrame = Time.frameCount;
        var p = Game.I != null ? Game.I.Player : null;
        if (p == null) return;
        if (trail.Count == 0 || (trail[trail.Count - 1] - p.Planar).sqrMagnitude > 36f) trail.Add(p.Planar);
        if (trail.Count > 400) trail.RemoveAt(0);
    }

    Vector3 TrailPoint(float back)
    {
        var p = Game.I.Player;
        Vector3 prev = p.Planar;
        float acc = 0;
        for (int i = trail.Count - 1; i >= 0; i--)
        {
            float d = Vector3.Distance(prev, trail[i]);
            if (acc + d >= back) return Vector3.Lerp(prev, trail[i], (back - acc) / Mathf.Max(0.01f, d));
            acc += d; prev = trail[i];
        }
        return prev;
    }

    void Update()
    {
        if (Me == null || Me.Dead) return;
        RecordTrail();
        var g = Game.I;
        if (!Active || g == null || g.Player == null) { Me.RudderInput = 0; Me.SailLevel = 0; return; }
        float dt = Time.deltaTime;
        if (Waiting >= 0f)
        {
            Vector3 dw = WaitPos - Me.Planar;
            if (dw.magnitude > 15f) Steer(Mathf.Atan2(dw.x, dw.z) * Mathf.Rad2Deg, dw.magnitude > 40f ? 2 : 1);
            else Steer(Waiting, 0);
            return;
        }
        Ship foe = Engage && g.CombatEnabled ? g.CurrentEnemy : null;
        if (foe != null && Vector3.Distance(foe.Planar, g.Player.Planar) > 420f) foe = null;
        if (foe != null)
        {
            Vector3 to = foe.Planar - Me.Planar;
            float dist = to.magnitude;
            Vector3 station = foe.Planar + foe.Right * Side * 52f - foe.Fwd * 10f;
            Vector3 ds = station - Me.Planar;
            float desired = ds.magnitude < 20f ? foe.Heading : Mathf.Atan2(ds.x, ds.z) * Mathf.Rad2Deg;
            Steer(desired, dist > 70f ? 3 : 2);
            fireTimer -= dt;
            if (fireTimer <= 0 && dist < 130f && dist > 16f && Combat.Broadside(Me, foe.Planar, 55f))
            {
                Combat.I.FireVolley(Me, foe.Planar + Vector3.up, foe.Velocity, 6, 6f * Mathf.Lerp(0.7f, 1.3f, dist / 130f), 3.4f);
                fireTimer = Random.Range(4.6f, 6.2f);
            }
            return;
        }
        // follow the wake
        Vector3 tgt = TrailPoint(FollowDist) + g.Player.Right * Side * 0f;
        Vector3 d = tgt - Me.Planar;
        float dd = d.magnitude;
        float behind = Vector3.Distance(Me.Planar, g.Player.Planar);
        int sail = dd > 60f ? 3 : dd > 22f ? 2 : (g.Player.Speed > 3f ? 1 : 0);
        if (behind < FollowDist * 0.55f) sail = Mathf.Min(sail, g.Player.Speed > 4f ? 1 : 0);
        float hdg = dd > 6f ? Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg : g.Player.Heading;
        // far behind (lost the trail): jump the gap quietly out of sight
        if (behind > 650f && !OnScreen()) { Me.Teleport(TrailPoint(FollowDist), g.Player.Heading); Me.SailLevel = 2; return; }
        Steer(hdg, sail);
    }

    bool OnScreen()
    {
        var cam = CameraRig.I != null ? CameraRig.I.Cam : null;
        if (cam == null) return false;
        Vector3 v = cam.WorldToViewportPoint(Me.transform.position);
        return v.z > 0 && v.x > -0.1f && v.x < 1.1f && v.y > -0.1f && v.y < 1.1f;
    }

    void Steer(float desired, int sail)
    {
        Vector3 ahead = Me.Planar + Me.Fwd * 40f;
        if (World.LandHeight(ahead.x, ahead.z) > -3f && World.PassageDistance(Me.Planar) > 60f)
        {
            Vector3 gr = World.LandGradient(ahead.x, ahead.z);
            if (gr.sqrMagnitude > 1e-6f) desired = Mathf.Atan2(-gr.x, -gr.z) * Mathf.Rad2Deg;
        }
        float err = Mathf.DeltaAngle(Me.Heading, desired);
        Me.RudderInput = Mathf.Clamp(err / 22f, -1f, 1f);
        Me.SailLevel = sail;
    }
}
