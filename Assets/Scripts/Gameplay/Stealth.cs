using UnityEngine;

/// <summary>
/// Chapter II night run through the Gate of Tears. Watchtower beams sweep the channels and patrol
/// boats carry lantern pools. Being inside either fills the detection meter — faster with your own
/// lantern lit or at full sail. A full meter raises the alarm.
/// </summary>
public class Stealth : MonoBehaviour
{
    public static Stealth I;
    public bool Active;
    public float Detection;
    public bool Spotted;
    public bool InLight;
    public string Source = "";
    public System.Action OnSpotted;

    public static Stealth Create(Transform parent)
    {
        var go = new GameObject("Stealth");
        go.transform.SetParent(parent, false);
        I = go.AddComponent<Stealth>();
        return I;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        float t = Time.time;
        foreach (var b in World.Beacons)
        {
            b.Yaw = b.Spin ? Mathf.Repeat(b.Phase + t * b.Speed, 360f) : b.YawCenter + Mathf.Sin(t * b.Speed * Mathf.Deg2Rad * 2.2f + b.Phase) * b.Sweep;
            if (b.Cone != null)
            {
                b.Cone.rotation = Quaternion.Euler(b.Pitch, b.Yaw, 0);
                bool show = SkyController.I == null || SkyController.I.IsNight;
                if (b.Cone.gameObject.activeSelf != show) b.Cone.gameObject.SetActive(show);
            }
        }
        foreach (var p in World.Patrols)
        {
            if (p.Boat == null || p.Path.Count < 2) continue;
            float len = Vector3.Distance(p.Path[0], p.Path[1]);
            p.T += dt * p.Speed / Mathf.Max(1f, len);
            float u = Mathf.PingPong(p.T, 1f);
            Vector3 pos = Vector3.Lerp(p.Path[0], p.Path[1], u);
            bool forward = Mathf.Repeat(p.T, 2f) < 1f;
            Vector3 dir = (forward ? p.Path[1] - p.Path[0] : p.Path[0] - p.Path[1]).normalized;
            pos.y = WaveField.HeightAt(pos) - 0.05f;
            p.Boat.position = pos;
            p.Boat.rotation = Quaternion.Slerp(p.Boat.rotation, Quaternion.LookRotation(dir), dt * 2f);
        }

        var G = Game.I;
        if (!Active || G == null || G.Player == null || G.State != Game.GameState.Sailing || Spotted) { InLight = false; return; }
        var pl = G.Player;
        Vector3 pp = pl.Planar;
        bool lit = pl.LanternOn;
        float speedK = 1f + Mathf.Clamp01(pl.Speed / 12f) * 0.8f;
        InLight = false;
        Source = "";
        foreach (var b in World.Beacons)
        {
            Vector3 to = pp - new Vector3(b.Pos.x, 0, b.Pos.z);
            float d = to.magnitude;
            if (d > b.Range) continue;
            Vector3 f = Quaternion.Euler(0, b.Yaw, 0) * Vector3.forward;
            if (Vector3.Angle(f, to) < b.HalfAngle + 3f) { InLight = true; Source = "watchtower beam"; }
        }
        foreach (var p in World.Patrols)
        {
            if (p.Boat == null) continue;
            Vector3 bp = p.Boat.position; bp.y = 0;
            float r = p.Radius * (lit ? 1.5f : 1f);
            if ((bp - pp).sqrMagnitude < r * r) { InLight = true; Source = "patrol boat"; }
        }
        float lanternGlow = lit ? 0.12f : 0f;
        if (InLight) Detection += dt * (lit ? 0.75f : 0.32f) * speedK;
        else Detection = Mathf.Max(0f, Detection + dt * (lanternGlow * speedK - 0.12f));
        if (Detection >= 1f)
        {
            Detection = 1f;
            Spotted = true;
            OnSpotted?.Invoke();
        }
    }

    public void ResetState()
    {
        Detection = 0;
        Spotted = false;
        InLight = false;
    }
}
