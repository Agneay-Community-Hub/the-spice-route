using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Time of day, weather (storm blend), lightning, rain, fog and all lighting globals.</summary>
public class SkyController : MonoBehaviour
{
    public static SkyController I;

    public enum Preset { Dawn, Day, Storm, Afternoon, Sunset, Dusk, Title }

    struct P
    {
        public Color zenith, horizon, fog, sun, cloudLit, cloudShade, ambSky, ambEq, ambGround;
        public float elev, azim, sunI, sunVis, cloud, fogD, night, day, exposure;
    }

    P cur, from, to;
    float blendT = 1f, blendDur = 1f;
    public Light Sun;
    public float Storm, StormTarget;
    public float Era;
    float flash, nextLightning = 8f;
    ParticleSystem rain;
    LineRenderer bolt;
    float boltTimer;
    Transform cam;

    public static SkyController Create(Transform parent, Transform camera)
    {
        var go = new GameObject("Sky");
        go.transform.SetParent(parent, false);
        var s = go.AddComponent<SkyController>();
        s.cam = camera;
        I = s;
        s.Setup();
        return s;
    }

    static P Make(Preset p)
    {
        var x = new P();
        switch (p)
        {
            case Preset.Title:
            case Preset.Dawn:
                x.zenith = new Color(0.20f, 0.33f, 0.55f); x.horizon = new Color(0.98f, 0.66f, 0.42f); x.fog = new Color(0.86f, 0.62f, 0.46f);
                x.sun = new Color(1f, 0.72f, 0.45f); x.cloudLit = new Color(1f, 0.78f, 0.62f); x.cloudShade = new Color(0.48f, 0.4f, 0.45f);
                x.ambSky = new Color(0.45f, 0.5f, 0.62f); x.ambEq = new Color(0.62f, 0.5f, 0.42f); x.ambGround = new Color(0.22f, 0.2f, 0.18f);
                x.elev = 9f; x.azim = 270f; x.sunI = 1.5f; x.sunVis = 1f; x.cloud = 0.42f; x.fogD = 0.00055f; x.night = 0; x.day = 0.8f; x.exposure = 0.82f;
                break;
            case Preset.Day:
                x.zenith = new Color(0.16f, 0.38f, 0.72f); x.horizon = new Color(0.68f, 0.8f, 0.9f); x.fog = new Color(0.66f, 0.76f, 0.84f);
                x.sun = new Color(1f, 0.93f, 0.8f); x.cloudLit = new Color(1f, 1f, 0.98f); x.cloudShade = new Color(0.6f, 0.66f, 0.74f);
                x.ambSky = new Color(0.5f, 0.6f, 0.75f); x.ambEq = new Color(0.55f, 0.6f, 0.62f); x.ambGround = new Color(0.25f, 0.24f, 0.2f);
                x.elev = 42f; x.azim = 320f; x.sunI = 1.45f; x.sunVis = 1f; x.cloud = 0.38f; x.fogD = 0.0004f; x.night = 0; x.day = 1f; x.exposure = 0.85f;
                break;
            case Preset.Storm:
                x.zenith = new Color(0.12f, 0.14f, 0.17f); x.horizon = new Color(0.3f, 0.34f, 0.37f); x.fog = new Color(0.27f, 0.31f, 0.34f);
                x.sun = new Color(0.6f, 0.65f, 0.72f); x.cloudLit = new Color(0.34f, 0.37f, 0.41f); x.cloudShade = new Color(0.1f, 0.11f, 0.13f);
                x.ambSky = new Color(0.3f, 0.34f, 0.4f); x.ambEq = new Color(0.25f, 0.28f, 0.3f); x.ambGround = new Color(0.12f, 0.12f, 0.12f);
                x.elev = 35f; x.azim = 20f; x.sunI = 0.45f; x.sunVis = 0.05f; x.cloud = 0.92f; x.fogD = 0.0019f; x.night = 0; x.day = 0.45f; x.exposure = 1.1f;
                break;
            case Preset.Afternoon:
                x.zenith = new Color(0.18f, 0.36f, 0.66f); x.horizon = new Color(0.86f, 0.8f, 0.7f); x.fog = new Color(0.8f, 0.76f, 0.68f);
                x.sun = new Color(1f, 0.86f, 0.66f); x.cloudLit = new Color(1f, 0.95f, 0.88f); x.cloudShade = new Color(0.58f, 0.58f, 0.64f);
                x.ambSky = new Color(0.5f, 0.56f, 0.7f); x.ambEq = new Color(0.6f, 0.56f, 0.5f); x.ambGround = new Color(0.25f, 0.22f, 0.18f);
                x.elev = 26f; x.azim = 45f; x.sunI = 1.45f; x.sunVis = 1f; x.cloud = 0.45f; x.fogD = 0.00045f; x.night = 0; x.day = 0.95f; x.exposure = 0.85f;
                break;
            case Preset.Sunset:
                x.zenith = new Color(0.16f, 0.2f, 0.42f); x.horizon = new Color(1f, 0.5f, 0.26f); x.fog = new Color(0.86f, 0.46f, 0.3f);
                x.sun = new Color(1f, 0.55f, 0.25f); x.cloudLit = new Color(1f, 0.6f, 0.4f); x.cloudShade = new Color(0.36f, 0.24f, 0.32f);
                x.ambSky = new Color(0.36f, 0.36f, 0.55f); x.ambEq = new Color(0.62f, 0.4f, 0.32f); x.ambGround = new Color(0.18f, 0.14f, 0.14f);
                x.elev = 5f; x.azim = 85f; x.sunI = 1.55f; x.sunVis = 1f; x.cloud = 0.48f; x.fogD = 0.0006f; x.night = 0.05f; x.day = 0.7f; x.exposure = 0.9f;
                break;
            case Preset.Dusk:
                x.zenith = new Color(0.05f, 0.07f, 0.17f); x.horizon = new Color(0.55f, 0.3f, 0.28f); x.fog = new Color(0.32f, 0.22f, 0.26f);
                x.sun = new Color(0.9f, 0.45f, 0.3f); x.cloudLit = new Color(0.6f, 0.35f, 0.35f); x.cloudShade = new Color(0.12f, 0.1f, 0.16f);
                x.ambSky = new Color(0.22f, 0.25f, 0.42f); x.ambEq = new Color(0.32f, 0.24f, 0.26f); x.ambGround = new Color(0.1f, 0.08f, 0.1f);
                x.elev = 1.5f; x.azim = 92f; x.sunI = 0.75f; x.sunVis = 0.6f; x.cloud = 0.4f; x.fogD = 0.0007f; x.night = 0.7f; x.day = 0.45f; x.exposure = 1.15f;
                break;
        }
        return x;
    }

    static P Lerp(P a, P b, float t)
    {
        var x = new P();
        x.zenith = Color.Lerp(a.zenith, b.zenith, t); x.horizon = Color.Lerp(a.horizon, b.horizon, t); x.fog = Color.Lerp(a.fog, b.fog, t);
        x.sun = Color.Lerp(a.sun, b.sun, t); x.cloudLit = Color.Lerp(a.cloudLit, b.cloudLit, t); x.cloudShade = Color.Lerp(a.cloudShade, b.cloudShade, t);
        x.ambSky = Color.Lerp(a.ambSky, b.ambSky, t); x.ambEq = Color.Lerp(a.ambEq, b.ambEq, t); x.ambGround = Color.Lerp(a.ambGround, b.ambGround, t);
        x.elev = Mathf.Lerp(a.elev, b.elev, t); x.azim = Mathf.LerpAngle(a.azim, b.azim, t); x.sunI = Mathf.Lerp(a.sunI, b.sunI, t);
        x.sunVis = Mathf.Lerp(a.sunVis, b.sunVis, t); x.cloud = Mathf.Lerp(a.cloud, b.cloud, t); x.fogD = Mathf.Lerp(a.fogD, b.fogD, t);
        x.night = Mathf.Lerp(a.night, b.night, t); x.day = Mathf.Lerp(a.day, b.day, t); x.exposure = Mathf.Lerp(a.exposure, b.exposure, t);
        return x;
    }

    void Setup()
    {
        var sunGo = new GameObject("Sun");
        sunGo.transform.SetParent(transform, false);
        Sun = sunGo.AddComponent<Light>();
        Sun.type = LightType.Directional;
        Sun.shadows = LightShadows.Soft;
        Sun.shadowStrength = 0.85f;
        Sun.shadowBias = 0.05f;
        Sun.shadowNormalBias = 0.4f;
        RenderSettings.sun = Sun;
        RenderSettings.skybox = Mats.Sky;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;

        cur = Make(Preset.Title);
        from = to = cur;

        // Rain
        var rgo = new GameObject("Rain");
        rgo.transform.SetParent(transform, false);
        rain = rgo.AddComponent<ParticleSystem>();
        rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = rain.main;
        main.loop = true;
        main.startLifetime = 1.1f;
        main.startSpeed = 0f;
        main.startSize3D = true;
        main.startSizeX = 0.05f; main.startSizeY = 1.6f; main.startSizeZ = 1f;
        main.startColor = new Color(0.75f, 0.8f, 0.9f, 0.55f);
        main.maxParticles = 4000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;
        var vel = rain.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-3f);
        vel.y = new ParticleSystem.MinMaxCurve(-38f);
        vel.z = new ParticleSystem.MinMaxCurve(-2f);
        var sh = rain.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(70, 1, 70);
        var em = rain.emission;
        em.rateOverTime = 0;
        var rr = rgo.GetComponent<ParticleSystemRenderer>();
        rr.sharedMaterial = Mats.Rain;
        rr.renderMode = ParticleSystemRenderMode.Stretch;
        rr.velocityScale = 0.02f;
        rr.lengthScale = 2.5f;
        rr.shadowCastingMode = ShadowCastingMode.Off;
        rain.Play();

        // Lightning bolt
        var bgo = new GameObject("Bolt");
        bgo.transform.SetParent(transform, false);
        bolt = bgo.AddComponent<LineRenderer>();
        bolt.sharedMaterial = Mats.Line;
        bolt.positionCount = 14;
        bolt.widthMultiplier = 2.5f;
        bolt.startColor = new Color(0.8f, 0.85f, 1f, 1f);
        bolt.endColor = new Color(0.6f, 0.7f, 1f, 0.6f);
        bolt.enabled = false;
        bolt.shadowCastingMode = ShadowCastingMode.Off;

        Apply();
    }

    public void Go(Preset p, float seconds)
    {
        from = cur;
        to = Make(p);
        blendDur = Mathf.Max(0.01f, seconds);
        blendT = 0f;
        if (seconds <= 0.01f) { cur = to; blendT = 1f; }
    }

    public void SetStorm(float target) => StormTarget = target;

    void Update()
    {
        float dt = Time.deltaTime;
        if (blendT < 1f)
        {
            blendT = Mathf.Min(1f, blendT + dt / blendDur);
            float s = blendT * blendT * (3f - 2f * blendT);
            cur = Lerp(from, to, s);
        }
        Storm = Mathf.MoveTowards(Storm, StormTarget, dt * 0.08f);

        // lightning
        flash = Mathf.Max(0, flash - dt * 4f);
        if (boltTimer > 0) { boltTimer -= dt; if (boltTimer <= 0) bolt.enabled = false; }
        if (Storm > 0.6f)
        {
            nextLightning -= dt;
            if (nextLightning <= 0)
            {
                nextLightning = Random.Range(3.5f, 9f);
                Strike();
            }
        }

        // rain follows camera
        if (cam != null)
        {
            rain.transform.position = cam.position + Vector3.up * 22f + cam.forward * 12f;
        }
        var em = rain.emission;
        em.rateOverTime = Mathf.Clamp01((Storm - 0.3f) / 0.7f) * 3200f;

        Apply();
    }

    void Strike()
    {
        flash = Random.Range(0.6f, 1f);
        if (cam == null) return;
        Vector3 dir = Quaternion.Euler(0, Random.Range(-70f, 70f), 0) * Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        float dist = Random.Range(250f, 800f);
        Vector3 ground = cam.position + dir * dist;
        ground.y = 0;
        Vector3 top = ground + new Vector3(Random.Range(-60, 60), 420f, Random.Range(-60, 60));
        for (int i = 0; i < bolt.positionCount; i++)
        {
            float t = (float)i / (bolt.positionCount - 1);
            Vector3 p = Vector3.Lerp(top, ground, t);
            if (i > 0 && i < bolt.positionCount - 1) p += new Vector3(Random.Range(-18f, 18f), 0, Random.Range(-18f, 18f));
            bolt.SetPosition(i, p);
        }
        bolt.widthMultiplier = Random.Range(1.5f, 3.5f);
        bolt.enabled = true;
        boltTimer = 0.18f;
        if (AudioMan.I != null) AudioMan.I.PlayDelayed("thunder", dist / 340f * 0.5f, Mathf.Lerp(1f, 0.55f, dist / 800f), Random.Range(0.85f, 1.1f));
    }

    public float Flash => flash;

    void Apply()
    {
        P p = cur;
        // storm overlay
        if (Storm > 0.001f)
        {
            P st = Make(Preset.Storm);
            st.elev = p.elev; st.azim = p.azim;
            p = Lerp(p, st, Storm);
        }
        Quaternion rot = Quaternion.Euler(p.elev, p.azim, 0);
        Sun.transform.rotation = rot;
        Vector3 toSun = -(rot * Vector3.forward);
        float fl = flash;
        Sun.color = p.sun;
        Sun.intensity = p.sunI * Mathf.Clamp01(p.elev / 4f + 0.35f) + fl * 1.5f;
        Sun.shadowStrength = Mathf.Lerp(0.85f, 0.35f, Storm);

        RenderSettings.ambientSkyColor = p.ambSky * (1f + fl);
        RenderSettings.ambientEquatorColor = p.ambEq * (1f + fl);
        RenderSettings.ambientGroundColor = p.ambGround;
        RenderSettings.fogColor = p.fog;
        RenderSettings.fogDensity = p.fogD;

        Vec("_SunDirG", toSun);
        Col("_SunColorG", p.sun, 1.6f);
        Col("_SkyZenithG", p.zenith);
        Col("_SkyHorizonG", p.horizon);
        Col("_FogColorG", p.fog);
        Col("_CloudLitG", p.cloudLit);
        Col("_CloudShadeG", p.cloudShade);
        Col("_AmbientG", Color.Lerp(p.ambSky, p.ambEq, 0.5f));
        Shader.SetGlobalFloat("_SunVisG", p.sunVis);
        Shader.SetGlobalFloat("_CloudCoverG", p.cloud);
        Shader.SetGlobalFloat("_NightG", p.night);
        Shader.SetGlobalFloat("_FlashG", fl);
        Shader.SetGlobalFloat("_StormG", Storm);
        Shader.SetGlobalFloat("_DayLightG", p.day);
        Shader.SetGlobalFloat("_WetnessG", Mathf.Clamp01(Storm * 1.3f));
        Exposure = p.exposure;
    }

    public float Exposure { get; private set; } = 1f;

    static void Col(string n, Color c, float mul = 1f)
    {
        Color l = c.linear * mul;
        Shader.SetGlobalVector(n, new Vector4(l.r, l.g, l.b, 1));
    }

    static void Vec(string n, Vector3 v) => Shader.SetGlobalVector(n, new Vector4(v.x, v.y, v.z, 0));
}
