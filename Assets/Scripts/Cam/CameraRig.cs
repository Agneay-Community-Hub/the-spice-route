using UnityEngine;

public class CameraRig : MonoBehaviour
{
    public static CameraRig I;
    public Camera Cam;
    public Ship Target;
    public enum Mode { Orbit, Helm, Cinematic, CinOrbit, Foot }
    public Transform FootTarget;
    float footYaw, footPitch = 14f, footDist = 7f;
    public float FootYaw => footYaw;

    public void Foot(Transform target, float yawDeg)
    {
        fromPos = transform.position; fromRot = transform.rotation; fromFov = Cam.fieldOfView;
        blend = 0f; blendSpeed = 1f / 1.2f;
        FootTarget = target; footYaw = yawDeg; footPitch = 14f;
        CurMode = Mode.Foot;
        pos = Vector3.zero;
    }
    public Mode CurMode = Mode.Cinematic;
    public bool Aiming;
    public bool InputEnabled = true;
    public float Sensitivity = 0.12f;

    float yaw = -90f, pitch = 12f, dist = 34f, targetDist = 34f;
    float helmYaw, helmPitch;
    float idle;
    float trauma;
    Vector3 pos;
    Quaternion rot = Quaternion.identity;
    float fov = 55f;
    float blend = 1f, blendSpeed = 1f;
    Vector3 fromPos; Quaternion fromRot; float fromFov;

    Vector3 cinPos = new Vector3(0, 30, -100), cinLook = Vector3.zero; float cinFov = 50f;
    Vector3 cinVel;
    Vector3 orbCenter; float orbR, orbH, orbSpeed, orbAng;

    public float Yaw => CurMode == Mode.Helm && Target != null ? Target.Heading + helmYaw : transform.eulerAngles.y;

    public static CameraRig Create()
    {
        var go = new GameObject("MainCamera");
        go.tag = "MainCamera";
        var cam = go.AddComponent<Camera>();
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 6000f;
        cam.fieldOfView = 55f;
        cam.allowHDR = true;
        cam.allowMSAA = true;
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.depthTextureMode |= DepthTextureMode.Depth;
        go.AddComponent<AudioListener>();
        I = go.AddComponent<CameraRig>();
        I.Cam = cam;
        return I;
    }

    /// <summary>Swings the chase camera to a given angle (used for media capture).</summary>
    public void LookFrom(float yawDeg, float pitchDeg, float distance) { yaw = yawDeg; pitch = pitchDeg; targetDist = dist = distance; idle = -6f; }

    public void Shake(float amt) => trauma = Mathf.Clamp01(trauma + amt);

    void BeginBlend(float seconds)
    {
        fromPos = transform.position; fromRot = transform.rotation; fromFov = Cam.fieldOfView;
        blend = 0f;
        blendSpeed = 1f / Mathf.Max(0.01f, seconds);
    }

    public void Gameplay(float blendTime = 1.2f)
    {
        if (CurMode == Mode.Orbit) return;
        BeginBlend(blendTime);
        CurMode = Mode.Orbit;
        if (Target != null) yaw = Target.Heading;
        pitch = 12f;
    }

    public void ToggleHelm()
    {
        BeginBlend(0.6f);
        CurMode = CurMode == Mode.Helm ? Mode.Orbit : Mode.Helm;
        helmYaw = 0; helmPitch = 0;
        if (CurMode == Mode.Orbit && Target != null) yaw = Target.Heading;
    }

    public void Shot(Vector3 p, Vector3 look, float fovDeg = 50f, float blendTime = 2f)
    {
        BeginBlend(blendTime);
        CurMode = Mode.Cinematic;
        cinPos = p; cinLook = look; cinFov = fovDeg;
        cinVel = Vector3.zero;
    }

    public void Drift(Vector3 velocity) => cinVel = velocity;

    public void Orbit(Vector3 center, float radius, float height, float speedDeg, float startAngle, float blendTime = 2f, float fovDeg = 50f)
    {
        BeginBlend(blendTime);
        CurMode = Mode.CinOrbit;
        orbCenter = center; orbR = radius; orbH = height; orbSpeed = speedDeg; orbAng = startAngle; cinFov = fovDeg;
    }

    void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        Vector2 md = InputEnabled && Cursor.lockState == CursorLockMode.Locked ? In.MouseDelta * Sensitivity : Vector2.zero;
        Vector3 desiredPos = transform.position;
        Quaternion desiredRot = transform.rotation;
        float desiredFov = 55f;

        switch (CurMode)
        {
            case Mode.Orbit:
                if (Target == null) break;
                {
                    if (md.sqrMagnitude > 0.01f) idle = 0; else idle += dt;
                    yaw += md.x;
                    pitch = Mathf.Clamp(pitch - md.y, -4f, 62f);
                    if (InputEnabled) targetDist = Mathf.Clamp(targetDist - In.Scroll * 4f, 16f, 70f);
                    if (idle > (Game.I != null && Game.I.Tutorial ? 1.2f : 2.5f) && Target.Speed > 2f && !Aiming)
                        yaw = Mathf.LerpAngle(yaw, Target.Heading, dt * 0.45f);
                    float d = Aiming ? 24f : targetDist;
                    dist = Mathf.Lerp(dist, d, 1f - Mathf.Exp(-4f * dt));
                    Quaternion q = Quaternion.Euler(pitch, yaw, 0);
                    Vector3 pivot = Target.transform.position + Vector3.up * 6.5f;
                    if (Aiming) pivot += q * Vector3.right * 6f + Vector3.up * 2f;
                    Vector3 p = pivot + q * new Vector3(0, 0, -dist);
                    float wh = WaveField.HeightAt(p) + 2f;
                    if (p.y < wh) p.y = wh;
                    float lh = World.LandHeight(p.x, p.z) + 3f;
                    if (p.y < lh) p.y = lh;
                    desiredPos = p;
                    desiredRot = Quaternion.LookRotation(pivot + (Aiming ? q * Vector3.forward * 30f : Vector3.zero) - p);
                    desiredFov = Aiming ? 42f : 55f + Mathf.Clamp01((Target.Speed - 8f) / 8f) * 6f;
                }
                break;
            case Mode.Helm:
                if (Target == null) break;
                {
                    helmYaw = Mathf.Clamp(helmYaw + md.x, -140f, 140f);
                    helmPitch = Mathf.Clamp(helmPitch - md.y, -30f, 35f);
                    if (md.sqrMagnitude < 0.01f) idle += dt; else idle = 0;
                    if (idle > 3f) { helmYaw = Mathf.Lerp(helmYaw, 0, dt * 0.8f); helmPitch = Mathf.Lerp(helmPitch, 0, dt * 0.8f); }
                    Transform t = Target.transform;
                    desiredPos = t.TransformPoint(new Vector3(0.6f, ShipBuilder.DeckY(0.2f) + 3.6f, -5.6f));
                    Vector3 e = t.eulerAngles;
                    desiredRot = Quaternion.Euler(e.x * 0.6f + helmPitch, e.y + helmYaw, e.z * 0.6f);
                    desiredFov = 62f;
                }
                break;
            case Mode.Foot:
                if (FootTarget == null) break;
                {
                    footYaw += md.x;
                    footPitch = Mathf.Clamp(footPitch - md.y, -12f, 65f);
                    if (InputEnabled) footDist = Mathf.Clamp(footDist - In.Scroll * 1.2f, 3.5f, 16f);
                    Quaternion q = Quaternion.Euler(footPitch, footYaw, 0);
                    Vector3 pivot = FootTarget.position + Vector3.up * 1.9f;
                    Vector3 p = pivot + q * new Vector3(0, 0, -footDist);
                    float lh = Mathf.Max(World.LandHeight(p.x, p.z), WaveField.HeightAt(p)) + 0.6f;
                    if (p.y < lh) p.y = lh;
                    desiredPos = p;
                    desiredRot = Quaternion.LookRotation(pivot - p);
                    desiredFov = 58f;
                }
                break;
            case Mode.Cinematic:
                cinPos += cinVel * dt;
                cinLook += cinVel * dt;
                desiredPos = cinPos;
                desiredRot = Quaternion.LookRotation(cinLook - cinPos);
                desiredFov = cinFov;
                break;
            case Mode.CinOrbit:
                orbAng += orbSpeed * dt;
                desiredPos = orbCenter + new Vector3(Mathf.Sin(orbAng * Mathf.Deg2Rad) * orbR, orbH, Mathf.Cos(orbAng * Mathf.Deg2Rad) * orbR);
                desiredRot = Quaternion.LookRotation(orbCenter + Vector3.up * 4f - desiredPos);
                desiredFov = cinFov;
                break;
        }

        if (CurMode == Mode.Orbit || CurMode == Mode.Foot)
        {
            pos = Vector3.Lerp(pos == Vector3.zero ? desiredPos : pos, desiredPos, 1f - Mathf.Exp(-10f * dt));
            rot = desiredRot;
        }
        else { pos = desiredPos; rot = desiredRot; }
        fov = Mathf.Lerp(fov, desiredFov, 1f - Mathf.Exp(-4f * dt));

        Vector3 finalPos = pos; Quaternion finalRot = rot; float finalFov = fov;
        if (blend < 1f)
        {
            blend = Mathf.Min(1f, blend + dt * blendSpeed);
            float s = blend * blend * (3f - 2f * blend);
            finalPos = Vector3.Lerp(fromPos, pos, s);
            finalRot = Quaternion.Slerp(fromRot, rot, s);
            finalFov = Mathf.Lerp(fromFov, fov, s);
        }

        trauma = Mathf.Max(0, trauma - dt * 0.9f);
        float sh = trauma * trauma;
        float t2 = Time.time * 22f;
        Vector3 shake = new Vector3(Mathf.PerlinNoise(t2, 0) - 0.5f, Mathf.PerlinNoise(0, t2) - 0.5f, 0) * sh * 1.6f;
        Quaternion shakeR = Quaternion.Euler(shake.y * 4f, shake.x * 4f, (Mathf.PerlinNoise(t2, t2) - 0.5f) * sh * 6f);
        transform.position = finalPos + finalRot * shake;
        transform.rotation = finalRot * shakeR;
        Cam.fieldOfView = finalFov;
    }
}
