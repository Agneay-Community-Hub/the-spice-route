using UnityEngine;

/// <summary>The monsoon. Angle is the compass bearing the wind blows TOWARD (degrees, 0 = +Z / north).</summary>
public static class Wind
{
    public static float Angle = -75f;
    public static float Strength = 1f;
    public static float Gust;
    static float targetAngle = -75f, targetStrength = 1f, angleSpeed = 20f, strengthSpeed = 0.2f;
    public static float GustAmount = 0.1f;

    public static Vector3 Dir3 => new Vector3(Mathf.Sin(Angle * Mathf.Deg2Rad), 0, Mathf.Cos(Angle * Mathf.Deg2Rad));
    public static float Effective => Strength * (1f + Gust);

    public static void Set(float angle, float strength)
    {
        Angle = targetAngle = angle;
        Strength = targetStrength = strength;
    }

    public static void ShiftTo(float angle, float strength, float seconds)
    {
        targetAngle = angle;
        targetStrength = strength;
        float delta = Mathf.Abs(Mathf.DeltaAngle(Angle, angle));
        angleSpeed = Mathf.Max(1f, delta / Mathf.Max(0.1f, seconds));
        strengthSpeed = Mathf.Max(0.01f, Mathf.Abs(strength - Strength) / Mathf.Max(0.1f, seconds));
    }

    public static bool Shifting => Mathf.Abs(Mathf.DeltaAngle(Angle, targetAngle)) > 0.5f;

    public static void Tick(float dt, float time)
    {
        Angle = Mathf.MoveTowardsAngle(Angle, targetAngle, angleSpeed * dt);
        Strength = Mathf.MoveTowards(Strength, targetStrength, strengthSpeed * dt);
        Gust = (Mathf.PerlinNoise(time * 0.13f, 3.1f) - 0.5f) * 2f * GustAmount;
        float wobble = (Mathf.PerlinNoise(time * 0.05f, 7.7f) - 0.5f) * 8f * (GustAmount * 4f);
        Vector3 d = new Vector3(Mathf.Sin((Angle + wobble) * Mathf.Deg2Rad), 0, Mathf.Cos((Angle + wobble) * Mathf.Deg2Rad));
        Shader.SetGlobalVector("_WindDirG", new Vector4(d.x, d.z, Effective, 0));
    }
}
