using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Particle effects factory. One-shot effects share pooled systems and use Emit().</summary>
public static class FX
{
    static Transform root;
    static ParticleSystem splash, spray, sparks, smoke, debris, bigSmoke, embers;
    static readonly List<ParticleSystem> firePool = new List<ParticleSystem>();
    static Mesh ringMesh;

    public static void Init(Transform parent)
    {
        firePool.Clear();
        root = new GameObject("FX").transform;
        root.SetParent(parent, false);

        splash = Make("Splash", Mats.AlphaUnlit, 600, ps =>
        {
            var m = ps.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.4f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.8f);
            m.startColor = new Color(0.92f, 0.95f, 1f, 0.85f);
            m.gravityModifier = 1.4f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 22f; sh.radius = 0.8f;
            SizeGrow(ps, 0.6f, 2.2f);
            FadeOut(ps, new Color(1, 1, 1));
        });
        spray = Make("Spray", Mats.AlphaUnlit, 1500, ps =>
        {
            var m = ps.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.4f);
            m.startColor = new Color(0.95f, 0.97f, 1f, 0.55f);
            m.gravityModifier = 0.9f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 40f; sh.radius = 0.5f;
            SizeGrow(ps, 0.7f, 2.5f);
            FadeOut(ps, new Color(1, 1, 1));
        });
        sparks = Make("Sparks", Mats.AddSoft, 600, ps =>
        {
            var m = ps.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(3f, 9f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.35f);
            m.startColor = new Color(1f, 0.6f, 0.2f, 1f);
            m.gravityModifier = 0.8f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.3f;
            FadeOut(ps, new Color(1f, 0.7f, 0.3f));
        });
        embers = Make("Embers", Mats.AddSoft, 800, ps =>
        {
            var m = ps.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.5f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            m.startColor = new Color(1f, 0.55f, 0.15f, 1f);
            m.gravityModifier = -0.15f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 1.5f;
            FadeOut(ps, new Color(1f, 0.6f, 0.2f));
        });
        smoke = Make("Smoke", Mats.SmokeMat, 500, ps =>
        {
            var m = ps.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
            m.startSize = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            m.startColor = new Color(0.25f, 0.23f, 0.22f, 0.6f);
            m.gravityModifier = -0.08f;
            m.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2f);
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 1f;
            SizeGrow(ps, 0.6f, 2.6f);
            FadeOut(ps, new Color(0.4f, 0.38f, 0.36f));
        });
        bigSmoke = Make("BigSmoke", Mats.SmokeMat, 400, ps =>
        {
            var m = ps.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            m.startSize = new ParticleSystem.MinMaxCurve(4f, 8f);
            m.startColor = new Color(0.12f, 0.11f, 0.1f, 0.7f);
            m.gravityModifier = -0.05f;
            m.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2f);
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 3f;
            SizeGrow(ps, 0.5f, 3f);
            FadeOut(ps, new Color(0.3f, 0.28f, 0.26f));
        });
        debris = Make("Debris", Mats.Alpha, 300, ps =>
        {
            var m = ps.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            m.startColor = new Color(0.35f, 0.22f, 0.12f, 1f);
            m.gravityModifier = 2f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = 0.6f;
        });
    }

    static void SizeGrow(ParticleSystem ps, float a, float b)
    {
        var sz = ps.sizeOverLifetime;
        sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, a, 1, b));
    }

    static void FadeOut(ParticleSystem ps, Color c)
    {
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(c, 1) },
                  new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0, 1) });
        col.color = new ParticleSystem.MinMaxGradient(g);
    }

    static ParticleSystem Make(string name, Material mat, int max, System.Action<ParticleSystem> cfg, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent ?? root, false);
        go.transform.rotation = Quaternion.Euler(-90, 0, 0);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var m = ps.main;
        m.loop = false;
        m.playOnAwake = false;
        m.maxParticles = max;
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission;
        em.rateOverTime = 0;
        cfg(ps);
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.sortMode = ParticleSystemSortMode.Distance;
        return ps;
    }

    static void Emit(ParticleSystem ps, Vector3 p, int count)
    {
        if (ps == null || count <= 0) return;
        var ep = new ParticleSystem.EmitParams { position = p, applyShapeToPosition = true };
        ps.Emit(ep, count);
    }

    public static void Splash(Vector3 p, float scale = 1f) => Emit(splash, p, Mathf.RoundToInt(10 * scale));
    public static void Spray(Vector3 p, int n) => Emit(spray, p, n);
    public static void Sparks(Vector3 p, int n = 14) => Emit(sparks, p, n);
    public static void Smoke(Vector3 p, int n = 3) => Emit(smoke, p, n);
    public static void BigSmoke(Vector3 p, int n = 4) => Emit(bigSmoke, p, n);
    public static void Debris(Vector3 p, int n = 10) => Emit(debris, p, n);
    public static void Embers(Vector3 p, int n = 6) => Emit(embers, p, n);

    public static void Impact(Vector3 p, float scale = 1f)
    {
        Sparks(p, Mathf.RoundToInt(16 * scale));
        Debris(p, Mathf.RoundToInt(8 * scale));
        Smoke(p, 2);
    }

    /// <summary>Burning fire attached to a transform. Returns the system; stop with StopFire.</summary>
    public static ParticleSystem AttachFire(Transform parent, Vector3 localPos, float size = 1f)
    {
        ParticleSystem ps = null;
        for (int i = 0; i < firePool.Count; i++) if (!firePool[i].gameObject.activeSelf) { ps = firePool[i]; break; }
        if (ps == null)
        {
            ps = Make("Fire", Mats.FlameMat, 120, s =>
            {
                var m = s.main;
                m.loop = true;
                m.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.9f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.2f);
                m.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
                m.startColor = new Color(1f, 0.62f, 0.25f, 1f);
                m.startRotation = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
                var sh = s.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12f; sh.radius = 0.5f;
                var em = s.emission; em.rateOverTime = 32;
                var sz = s.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1f, 1, 0.2f));
                var col = s.colorOverLifetime; col.enabled = true;
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.9f, 0.6f), 0), new GradientColorKey(new Color(1f, 0.45f, 0.1f), 0.5f), new GradientColorKey(new Color(0.6f, 0.1f, 0.02f), 1) },
                          new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.15f), new GradientAlphaKey(0, 1) });
                col.color = new ParticleSystem.MinMaxGradient(g);
            });
            var sm = Make("FireSmoke", Mats.SmokeMat, 60, s =>
            {
                var m = s.main;
                m.loop = true;
                m.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3.5f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(2f, 3.5f);
                m.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
                m.startColor = new Color(0.15f, 0.14f, 0.13f, 0.55f);
                m.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2f);
                var sh = s.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 15f; sh.radius = 0.4f;
                var em = s.emission; em.rateOverTime = 7;
                SizeGrow(s, 0.6f, 3f);
                FadeOut(s, new Color(0.35f, 0.33f, 0.3f));
            }, ps.transform);
            sm.transform.localPosition = new Vector3(0, 0, 1.2f);
            firePool.Add(ps);
        }
        ps.gameObject.SetActive(true);
        ps.transform.SetParent(parent, false);
        ps.transform.localPosition = localPos;
        ps.transform.rotation = Quaternion.Euler(-90, 0, 0);
        ps.transform.localScale = Vector3.one * size;
        ps.Clear(true);
        ps.Play(true);
        return ps;
    }

    public static void StopFire(ParticleSystem ps)
    {
        if (ps == null) return;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.transform.SetParent(root, false);
        ps.gameObject.SetActive(false);
    }

    public static ParticleSystem LoopingFire(Transform parent, Vector3 worldPos, float size)
    {
        var ps = AttachFire(parent, worldPos, size);
        firePool.Remove(ps);
        var light = new GameObject("FireLight").AddComponent<Light>();
        light.transform.SetParent(ps.transform, false);
        light.type = LightType.Point;
        light.range = 40f;
        light.intensity = 2.2f;
        light.color = new Color(1f, 0.6f, 0.3f);
        light.shadows = LightShadows.None;
        return ps;
    }

    public static ParticleSystem Motes(Transform parent, Vector3 pos)
    {
        var ps = Make("EchoMotes", Mats.AddSoft, 200, s =>
        {
            var m = s.main;
            m.loop = true;
            m.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.4f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            m.startColor = new Color(0.5f, 1f, 0.95f, 1f);
            m.gravityModifier = -0.04f;
            var sh = s.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 4f;
            var em = s.emission; em.rateOverTime = 0;
            FadeOut(s, new Color(0.4f, 0.9f, 1f));
        }, parent);
        ps.transform.position = pos;
        ps.Play();
        return ps;
    }

    /// <summary>Expanding ring on the sea when an Era Shift is triggered.</summary>
    public static void EraPulse(Vector3 pos)
    {
        if (ringMesh == null)
        {
            var b = new MeshBuilder();
            int n = 64;
            for (int i = 0; i <= n; i++)
            {
                float a = (float)i / n * Mathf.PI * 2f;
                Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                b.Vert(d * 0.94f, Vector3.up, Color.white);
                b.Vert(d * 1f + Vector3.up * 0.04f, d, Color.white);
                b.Vert(d * 1.0f + Vector3.up * 3f, d, Color.white);
            }
            for (int i = 0; i < n; i++)
            {
                int a = i * 3;
                b.Tri(a, a + 3, a + 4); b.Tri(a, a + 4, a + 1);
                b.Tri(a + 1, a + 4, a + 5); b.Tri(a + 1, a + 5, a + 2);
                b.Tri(a, a + 4, a + 3); b.Tri(a, a + 1, a + 4);
                b.Tri(a + 1, a + 5, a + 4); b.Tri(a + 1, a + 2, a + 5);
            }
            ringMesh = b.Build("EraRing");
        }
        var go = MeshBuilder.Spawn("EraPulse", ringMesh, new Material(Mats.GhostTeal), root, false, false);
        go.transform.position = new Vector3(pos.x, 0.3f, pos.z);
        go.AddComponent<PulseRing>();
    }
}

public class PulseRing : MonoBehaviour
{
    float t;
    Material m;
    void Start() { m = GetComponent<Renderer>().material; m.SetFloat("_Base", 1f); }
    void Update()
    {
        t += Time.deltaTime;
        float k = t / 2.2f;
        float r = Mathf.Lerp(2f, 320f, 1f - Mathf.Pow(1f - Mathf.Clamp01(k), 3f));
        transform.localScale = new Vector3(r, 1f + k * 2f, r);
        m.SetFloat("_Vis", Mathf.Clamp01(1f - k) * 1f);
        m.SetFloat("_Intensity", 3f * (1f - k));
        if (k >= 1f) Destroy(gameObject);
    }
}
