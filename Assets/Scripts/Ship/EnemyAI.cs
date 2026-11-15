using UnityEngine;

/// <summary>Raider captain: intercept, hold a broadside station, loose volleys, ram when close.</summary>
public class EnemyAI : MonoBehaviour
{
    public Ship Me, Target;
    public bool Active;
    public float Accuracy = 7f;
    public int VolleyCount = 6;
    public float VolleyDamage = 2.6f;
    public float FireInterval = 4.4f;
    public bool BothSides;
    float fireTimer = 4f;
    float sideSign = 1f, sideTimer;

    void Update()
    {
        if (Me == null) return;
        float dt = Time.deltaTime;
        if (!Active || Me.Dead || Target == null || Target.Dead)
        {
            Me.RudderInput = 0;
            if (Me.Dead) return;
            Me.SailLevel = Active ? 1 : Me.SailLevel;
            return;
        }
        Vector3 to = Target.Planar - Me.Planar;
        float dist = to.magnitude;
        sideTimer -= dt;
        if (sideTimer <= 0)
        {
            sideTimer = Random.Range(10f, 18f);
            sideSign = Vector3.Dot(-to, Target.Right) > 0 ? 1f : -1f;
        }
        Vector3 station = Target.Planar + Target.Right * sideSign * 48f + Target.Fwd * 6f;
        Vector3 d2 = station - Me.Planar;
        float desired;
        if (dist > 160f)
        {
            Vector3 lead = Target.Planar + Target.Velocity * Mathf.Clamp(dist / 14f, 0, 8f);
            Vector3 dl = lead - Me.Planar;
            desired = Mathf.Atan2(dl.x, dl.z) * Mathf.Rad2Deg;
        }
        else if (d2.magnitude < 22f) desired = Target.Heading;
        else desired = Mathf.Atan2(d2.x, d2.z) * Mathf.Rad2Deg;

        // steer away from land
        Vector3 ahead = Me.Planar + Me.Fwd * 45f;
        if (World.LandHeight(ahead.x, ahead.z) > -4f)
        {
            Vector3 g = World.LandGradient(ahead.x, ahead.z);
            if (g.sqrMagnitude > 1e-6f) desired = Mathf.Atan2(-g.x, -g.z) * Mathf.Rad2Deg;
        }
        float err = Mathf.DeltaAngle(Me.Heading, desired);
        Me.RudderInput = Mathf.Clamp(err / 22f, -1f, 1f);
        Me.SailLevel = dist > 80f ? 3 : (Target.Speed > 6f ? 3 : 2);

        fireTimer -= dt;
        if (fireTimer <= 0 && dist < 135f && dist > 18f && Combat.Broadside(Me, Target.Planar, 55f))
        {
            Combat.I.FireVolley(Me, Target.Planar + Vector3.up, Target.Velocity, VolleyCount, Accuracy * Mathf.Lerp(0.6f, 1.3f, dist / 135f), VolleyDamage);
            fireTimer = FireInterval * Random.Range(0.82f, 1.18f);
        }
    }
}
