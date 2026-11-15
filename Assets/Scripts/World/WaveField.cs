using UnityEngine;

/// <summary>
/// Gerstner wave field evaluated identically on CPU (buoyancy) and GPU (ocean shader).
/// </summary>
public static class WaveField
{
    public const int Count = 6;
    static readonly Vector4[] waves = new Vector4[Count];
    public static float Amp = 0.6f;
    public static float Time;

    static readonly Vector4[] baseWaves =
    {
        new Vector4(1.0f, 0.30f, 0.17f, 62f),
        new Vector4(0.8f, -0.6f, 0.15f, 33f),
        new Vector4(0.3f, 1.0f, 0.13f, 19f),
        new Vector4(-0.5f, 0.8f, 0.10f, 9.5f),
        new Vector4(0.9f, 0.5f, 0.08f, 5.2f),
        new Vector4(-0.7f, -0.3f, 0.06f, 3.3f),
    };

    static WaveField()
    {
        for (int i = 0; i < Count; i++)
        {
            var w = baseWaves[i];
            var d = new Vector2(w.x, w.y).normalized;
            waves[i] = new Vector4(d.x, d.y, w.z, w.w);
        }
    }

    public static void Push(float time, float amp)
    {
        Time = time;
        Amp = amp;
        Shader.SetGlobalVectorArray("_Waves", waves);
        Shader.SetGlobalFloat("_WaveTime", time);
        Shader.SetGlobalFloat("_WaveAmp", amp);
    }

    public static Vector3 Displacement(float x, float z)
    {
        Vector3 d = Vector3.zero;
        float horizAmp = Mathf.Min(Amp, 1f);
        for (int i = 0; i < Count; i++)
        {
            Vector4 w = waves[i];
            float k = 6.2831853f / w.w;
            float c = Mathf.Sqrt(9.8f / k);
            float f = k * (w.x * x + w.y * z - c * Time);
            float a = w.z / k;
            float cf = Mathf.Cos(f);
            d.x += w.x * a * horizAmp * cf;
            d.z += w.y * a * horizAmp * cf;
            d.y += a * Amp * Mathf.Sin(f);
        }
        return d;
    }

    public static float HeightAt(float x, float z)
    {
        float px = x, pz = z;
        for (int i = 0; i < 3; i++)
        {
            Vector3 d = Displacement(px, pz);
            px = x - d.x;
            pz = z - d.z;
        }
        return Displacement(px, pz).y + World.Tide;
    }

    public static float HeightAt(Vector3 p) => HeightAt(p.x, p.z);
}
