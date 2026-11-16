using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// SHIFT: the bronze compass lets Arivan perceive the sea as it once was — ghost harbours,
/// hidden channels and echoes of the missing expedition.
/// </summary>
public class EraShift : MonoBehaviour
{
    public static EraShift I;
    public bool Unlocked;
    public bool Active;
    public float Energy = 1f;
    public float Amount;
    public float DrainRate = 1f / 10f, RechargeRate = 1f / 11f;
    public int Activations;
    float wave;

    public static EraShift Create(Transform parent)
    {
        var go = new GameObject("EraShift");
        go.transform.SetParent(parent, false);
        I = go.AddComponent<EraShift>();
        return I;
    }

    public void Toggle()
    {
        if (!Unlocked)
        {
            HUD.I.Toast("Your father's compass is cold and silent... for now.", UIKit.Parch);
            return;
        }
        if (Active) Deactivate();
        else if (Energy > 0.12f) Activate();
        else HUD.I.Toast("The compass needs time to gather the past.", UIKit.Teal);
    }

    void Activate()
    {
        Active = true;
        Activations++;
        wave = 1f;
        if (Game.I.Player != null) FX.EraPulse(Game.I.Player.transform.position);
        AudioMan.I.Play("shift_on", 0.9f);
        if (CameraRig.I != null) CameraRig.I.Shake(0.25f);
    }

    public void Deactivate()
    {
        if (!Active) return;
        Active = false;
        wave = 0.6f;
        AudioMan.I.Play("shift_off", 0.7f);
    }

    public void ForceOff()
    {
        Active = false;
        Amount = 0;
        Shader.SetGlobalFloat("_EraShiftG", 0);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (Game.I != null && Game.I.CanControl && (In.Down(Key.LeftShift) || In.Down(Key.RightShift))) Toggle();
        if (Active)
        {
            Energy -= DrainRate * Progress.S.EraDrainMul * dt;
            if (Energy <= 0f) { Energy = 0f; Deactivate(); HUD.I.Toast("The echo fades...", UIKit.Teal); }
        }
        else Energy = Mathf.Min(1f, Energy + RechargeRate * dt);
        Amount = Mathf.MoveTowards(Amount, Active ? 1f : 0f, dt * (Active ? 1.4f : 2f));
        wave = Mathf.MoveTowards(wave, 0f, dt * 0.7f);
        float s = Amount * Amount * (3f - 2f * Amount);
        Shader.SetGlobalFloat("_EraShiftG", s);
        if (PostFX.I != null) { PostFX.I.Era = s; PostFX.I.EraWave = wave; }
        if (SkyController.I != null) SkyController.I.Era = s;
        foreach (var m in World.EchoMotes)
        {
            if (m == null) continue;
            var em = m.emission;
            em.rateOverTime = s * 30f;
        }
    }
}
