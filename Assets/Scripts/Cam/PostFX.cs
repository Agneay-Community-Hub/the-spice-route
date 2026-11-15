using UnityEngine;

/// <summary>Bloom, ACES tonemapping, grading, vignette and the Era Shift memory effect.</summary>
[RequireComponent(typeof(Camera))]
public class PostFX : MonoBehaviour
{
    public static PostFX I;
    Material mat;
    public bool Enabled = true;
    public bool HighQuality = true;
    public float Era, EraWave, Damage, Fade, FlashExtra;
    public float BloomIntensity = 0.55f;
    public float Saturation = 1.12f, Contrast = 1.06f;
    public Color Tint = new Color(1.02f, 1f, 0.97f);
    readonly RenderTexture[] mips = new RenderTexture[8];

    void Awake()
    {
        I = this;
        var s = Shader.Find("Hidden/SR/PostFX");
        if (s != null) mat = new Material(s);
        else Debug.LogError("[SpiceRoute] PostFX shader missing");
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (mat == null || !Enabled)
        {
            Graphics.Blit(src, dst);
            return;
        }
        float exposure = SkyController.I != null ? SkyController.I.Exposure : 1f;
        float flash = (SkyController.I != null ? SkyController.I.Flash : 0f) + FlashExtra;
        mat.SetFloat("_Threshold", 1.25f);
        mat.SetFloat("_Knee", 0.6f);
        mat.SetFloat("_BloomIntensity", BloomIntensity * (1f + Era * 0.6f));
        mat.SetFloat("_Exposure", exposure * (1f + Era * 0.15f));
        mat.SetFloat("_Saturation", Saturation);
        mat.SetFloat("_Contrast", Contrast);
        mat.SetFloat("_Vignette", 0.32f + Era * 0.25f + Damage * 0.3f);
        mat.SetFloat("_EraAmt", Era);
        mat.SetFloat("_EraWave", EraWave);
        mat.SetFloat("_EraTime", Time.time);
        mat.SetFloat("_Damage", Damage);
        mat.SetFloat("_Flash", flash);
        mat.SetFloat("_Fade", Fade);
        mat.SetFloat("_Grain", HighQuality ? 0.018f : 0f);
        mat.SetVector("_Tint", new Vector4(Tint.r, Tint.g, Tint.b, 1));

        int w = src.width / 2, h = src.height / 2;
        var fmt = src.format;
        int levels = HighQuality ? 6 : 4;
        int n = 0;
        RenderTexture cur = RenderTexture.GetTemporary(w, h, 0, fmt);
        Graphics.Blit(src, cur, mat, 0);
        mips[n++] = cur;
        for (int i = 1; i < levels; i++)
        {
            w /= 2; h /= 2;
            if (w < 4 || h < 4) break;
            var next = RenderTexture.GetTemporary(w, h, 0, fmt);
            Graphics.Blit(cur, next, mat, 1);
            mips[n++] = next;
            cur = next;
        }
        for (int i = n - 1; i > 0; i--) Graphics.Blit(mips[i], mips[i - 1], mat, 2);
        mat.SetTexture("_BloomTex", mips[0]);
        Graphics.Blit(src, dst, mat, 3);
        for (int i = 0; i < n; i++) { RenderTexture.ReleaseTemporary(mips[i]); mips[i] = null; }
    }
}
