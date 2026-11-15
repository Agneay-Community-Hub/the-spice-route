using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Fire-arrow volleys: ballistic projectiles with trails, hull/sail hit tests and splashes.</summary>
public class Combat : MonoBehaviour
{
    public static Combat I;

    class Arrow
    {
        public Transform tr;
        public TrailRenderer trail;
        public Vector3 vel;
        public Ship owner;
        public float life, delay, dmg;
        public bool active;
    }

    readonly List<Arrow> pool = new List<Arrow>();
    public readonly List<Ship> Ships = new List<Ship>();
    Mesh arrowMesh;
    static readonly Vector3 G = new Vector3(0, -9.8f, 0);

    public static Combat Create(Transform parent)
    {
        var go = new GameObject("Combat");
        go.transform.SetParent(parent, false);
        I = go.AddComponent<Combat>();
        var b = new MeshBuilder();
        b.Box(new Vector3(0, 0, 0), new Vector3(0.06f, 0.06f, 1.3f), new Color(0.3f, 0.2f, 0.12f));
        b.Box(new Vector3(0, 0, 0.7f), new Vector3(0.16f, 0.16f, 0.22f), new Color(1f, 0.6f, 0.2f));
        I.arrowMesh = b.Build("Arrow");
        return I;
    }

    Arrow Get()
    {
        foreach (var a in pool) if (!a.active) return a;
        var go = MeshBuilder.Spawn("Arrow", arrowMesh, Mats.FireGlow, transform, false, false);
        var tr = go.AddComponent<TrailRenderer>();
        tr.sharedMaterial = Mats.Line;
        tr.time = 0.28f;
        tr.widthMultiplier = 0.28f;
        tr.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.8f, 0.4f), 0), new GradientColorKey(new Color(1f, 0.35f, 0.05f), 1) },
                  new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
        tr.colorGradient = g;
        tr.shadowCastingMode = ShadowCastingMode.Off;
        tr.minVertexDistance = 0.4f;
        var a2 = new Arrow { tr = go.transform, trail = tr };
        go.SetActive(false);
        pool.Add(a2);
        return a2;
    }

    /// <summary>Fires a volley from the side of 'from' facing target. Returns false if target is not broadside.</summary>
    public void FireVolley(Ship from, Vector3 target, Vector3 targetVel, int count, float spread, float dmg)
    {
        Vector3 to = target - from.Planar;
        bool starboard = Vector3.Dot(to, from.Right) > 0;
        for (int i = 0; i < count; i++)
        {
            var a = Get();
            Vector3 start = from.transform.TransformPoint(new Vector3((starboard ? 1 : -1) * 2.7f, 1.9f, Mathf.Lerp(-5.5f, 5.5f, (i + 0.5f) / count)));
            Vector3 aim = target + Random.insideUnitSphere * spread;
            float dist = Vector3.Distance(new Vector3(start.x, 0, start.z), new Vector3(aim.x, 0, aim.z));
            float T = Mathf.Clamp(dist / 48f, 0.5f, 3.2f);
            aim += targetVel * T;
            aim.y = WaveField.HeightAt(aim) + 1.6f + Random.Range(0f, 2.5f);
            a.vel = (aim - start) / T - 0.5f * G * T;
            a.tr.position = start;
            a.tr.rotation = Quaternion.LookRotation(a.vel);
            a.owner = from;
            a.life = 0;
            a.delay = i * 0.06f + Random.Range(0f, 0.05f);
            a.dmg = dmg;
            a.active = true;
            a.tr.gameObject.SetActive(false);
        }
        if (AudioMan.I != null) AudioMan.I.PlayAt("volley", from.transform.position, from.IsPlayer ? 0.9f : 0.7f, Random.Range(0.92f, 1.08f));
        FX.Sparks(from.transform.TransformPoint(new Vector3((starboard ? 1 : -1) * 2.8f, 2f, 0)), 10);
    }

    public static bool Broadside(Ship from, Vector3 target, float maxOff = 58f)
    {
        Vector3 to = target - from.Planar; to.y = 0;
        float ang = Vector3.Angle(from.Right, to);
        return ang < maxOff || ang > 180f - maxOff;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        foreach (var a in pool)
        {
            if (!a.active) continue;
            if (a.delay > 0)
            {
                a.delay -= dt;
                if (a.delay <= 0)
                {
                    a.tr.gameObject.SetActive(true);
                    a.trail.Clear();
                }
                else continue;
            }
            a.vel += G * dt;
            Vector3 p = a.tr.position + a.vel * dt;
            a.tr.position = p;
            a.tr.rotation = Quaternion.LookRotation(a.vel);
            a.life += dt;
            bool done = false;
            foreach (var s in Ships)
            {
                if (s == null || s == a.owner || s.Dead || !s.gameObject.activeInHierarchy || (a.owner != null && s.Team == a.owner.Team)) continue;
                Vector3 lp = s.transform.InverseTransformPoint(p);
                if (Mathf.Abs(lp.z) > 11f || lp.y < -1.5f || lp.y > 13f) continue;
                float hw = ShipBuilder.HalfBeam(Mathf.Clamp01((lp.z + 10f) / 20f)) + 0.3f;
                bool hullHit = lp.y < ShipBuilder.Sheer(Mathf.Clamp01((lp.z + 10f) / 20f)) + 1.8f && Mathf.Abs(lp.x) < hw;
                bool sailHit = lp.y > 3.5f && Mathf.Abs(lp.x) < 6.4f && Mathf.Abs(lp.z - 1.8f) < 1.2f;
                if (hullHit || sailHit)
                {
                    s.Damage(hullHit ? a.dmg : a.dmg * 0.4f, p, hullHit);
                    FX.Impact(p, 0.6f);
                    if (AudioMan.I != null) AudioMan.I.PlayAt("thunk", p, 0.8f, Random.Range(0.85f, 1.2f));
                    done = true;
                    break;
                }
            }
            if (!done && p.y < WaveField.HeightAt(p))
            {
                FX.Splash(p, 0.6f);
                if (Random.value < 0.4f && AudioMan.I != null) AudioMan.I.PlayAt("splash_small", p, 0.5f, Random.Range(0.9f, 1.3f));
                done = true;
            }
            if (done || a.life > 6f)
            {
                a.active = false;
                a.tr.gameObject.SetActive(false);
            }
        }
    }
}
