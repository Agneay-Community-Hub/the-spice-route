using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Capture mode (run the player with -capture [-out dir]): renders scripted cinematic shots for the
/// trailer at a locked 30 fps into 1920x1080 frames, plus 4K stills for thumbnails/covers, and
/// exports the synthesised music and key sound effects as WAV files for the trailer soundtrack.
/// </summary>
public class Director : MonoBehaviour
{
    string outDir, frameDir, stillDir;
    RenderTexture rt, rt4k;
    Texture2D tex, tex4k;
    Camera cam;
    int frame;
    readonly List<string> timeline = new List<string>();

    static Game G => Game.I;
    static Ship P => Game.I.Player;

    void Start()
    {
        var args = Environment.GetCommandLineArgs();
        int oi = Array.IndexOf(args, "-out");
        outDir = oi >= 0 && oi + 1 < args.Length ? args[oi + 1] : Path.Combine(Application.persistentDataPath, "capture");
        frameDir = Path.Combine(outDir, "frames");
        stillDir = Path.Combine(outDir, "stills");
        Directory.CreateDirectory(frameDir);
        Directory.CreateDirectory(stillDir);
        cam = CameraRig.I.Cam;
        CameraRig.I.enabled = false;
        HUD.I.gameObject.SetActive(false);
        rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
        rt4k = new RenderTexture(3840, 2160, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 2 };
        tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        tex4k = new Texture2D(3840, 2160, TextureFormat.RGB24, false);
        QualitySettings.shadowDistance = 320f;
        G.State = Game.GameState.Cutscene;
        StartCoroutine(Run());
    }

    // ------------------------------------------------------------------ rendering
    void Render(RenderTexture target, Texture2D t, string path, bool jpg)
    {
        var prevTarget = cam.targetTexture;
        cam.targetTexture = target;
        cam.Render();
        // PostFX writes to the camera target; read back the final image
        var prev = RenderTexture.active;
        var resolved = RenderTexture.GetTemporary(target.width, target.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(target, resolved);
        RenderTexture.active = resolved;
        t.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        t.Apply(false);
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(resolved);
        cam.targetTexture = prevTarget;
        File.WriteAllBytes(path, jpg ? t.EncodeToJPG(92) : t.EncodeToPNG());
    }

    IEnumerator Frames(string name, float seconds, Action<float> update, float stillAt = 0.5f)
    {
        int n = Mathf.RoundToInt(seconds * 30f);
        timeline.Add(name + "," + frame + "," + n);
        bool stillDone = false;
        for (int i = 0; i < n; i++)
        {
            float u = (float)i / n;
            update(u);
            yield return new WaitForEndOfFrame();
            Render(rt, tex, Path.Combine(frameDir, frame.ToString("00000") + ".jpg"), true);
            frame++;
            if (!stillDone && u >= stillAt)
            {
                stillDone = true;
                Render(rt4k, tex4k, Path.Combine(stillDir, name + ".png"), false);
            }
        }
    }

    // ------------------------------------------------------------------ helpers
    static void Env(SkyController.Preset p, float storm, float windAng, float windStr)
    {
        SkyController.I.Go(p, 0f);
        SkyController.I.SetStorm(storm);
        SkyController.I.Storm = storm;
        Wind.Set(windAng, windStr);
        Wind.GustAmount = storm > 0.5f ? 0.35f : 0.1f;
    }

    static void Sail(Vector3 pos, float heading, float speed)
    {
        G.PlaceShip(pos, heading);
        P.Frozen = false;
        P.SailLevel = 3;
        P.SailAmt = 1f;
        P.Speed = speed;
        P.RudderInput = 0;
    }

    void Look(Vector3 pos, Vector3 target, float fov)
    {
        cam.transform.position = pos;
        cam.transform.rotation = Quaternion.LookRotation(target - pos);
        cam.fieldOfView = fov;
    }

    Vector3 ShipPt(float x, float y, float z)
    {
        var t = P.transform;
        Vector3 f = P.Fwd, r = P.Right;
        Vector3 p = t.position;
        return new Vector3(p.x, 0, p.z) + r * x + Vector3.up * y + f * z;
    }

    static float Ease(float u) => u * u * (3f - 2f * u);

    // ------------------------------------------------------------------ the shoot
    IEnumerator Run()
    {
        yield return null;
        ExportAudio();
        Time.captureFramerate = 30;
        P.DamageTaken = 0.05f;
        Vector3 dock = World.P("dock_muziris");
        float cx = World.PortCoastX;

        // 1. Muziris at dawn
        Env(SkyController.Preset.Dawn, 0, -75, 0.95f);
        G.PlaceShip(World.P("start"), 270f);
        P.Frozen = true;
        for (int i = 0; i < 20; i++) yield return null;
        yield return Frames("01_muziris_dawn", 6f, u =>
            Look(Vector3.Lerp(dock + new Vector3(-130, 30, -110), dock + new Vector3(-85, 16, -45), Ease(u)), dock + new Vector3(45, 6, 0), 44));

        // 2. Temple and palms
        float tz = -165f, tx = World.MalabarCoastX(tz) + 95f;
        Vector3 temple = new Vector3(tx, World.LandHeight(tx, tz), tz);
        yield return Frames("02_temple", 4.5f, u =>
            Look(temple + new Vector3(-48 + u * 14f, 9, 40 - u * 6f), temple + new Vector3(0, 7, 0), 42));

        // 3. Sailing out at dawn
        Sail(World.P("start") + new Vector3(-90, 0, 0), 268f, 12f);
        for (int i = 0; i < 10; i++) yield return null;
        yield return Frames("03_sail_dawn", 6f, u =>
            Look(ShipPt(-14f + u * 6f, 4f, -24f + u * 4f), ShipPt(0, 7, 12), 50));

        // 4. Wind shift, open sea by day
        Env(SkyController.Preset.Day, 0, -150, 1.1f);
        Sail(new Vector3(-150, 0, 120), 300f, 12f);
        for (int i = 0; i < 10; i++) yield return null;
        yield return Frames("04_open_sea_day", 5f, u =>
            Look(ShipPt(34f, 6f + u * 3f, 18f - u * 20f), ShipPt(0, 6, 0), 46));

        // 5. Storm
        Env(SkyController.Preset.Day, 1f, -155, 1.55f);
        Sail(new Vector3(-420, 0, 200), 205f, 14f);
        for (int i = 0; i < 30; i++) yield return null;
        yield return Frames("05_storm", 7f, u =>
            Look(ShipPt(-9f, 9f, -30f), ShipPt(0, 6, 10), 55));

        // 6. Battle with the raider
        Env(SkyController.Preset.Afternoon, 0, -100, 1f);
        Sail(new Vector3(-1100, 0, -650), 300f, 10f);
        G.SpawnRaider(new Vector3(-1100, 0, -650) + P.Right * 58f + P.Fwd * 6f, 300f);
        G.Raider.MaxHP = G.Raider.HP = 5000f;
        G.Raider.Speed = 10f;
        G.RaiderAI.Active = true;
        G.CombatEnabled = true;
        for (int i = 0; i < 8; i++) G.Raider.Damage(1f, G.Raider.transform.position, true);
        float volley = 0;
        yield return Frames("06_battle", 8f, u =>
        {
            volley -= 1f / 30f;
            if (volley <= 0)
            {
                volley = 1.6f;
                Combat.I.FireVolley(P, G.Raider.Planar + Vector3.up, G.Raider.Velocity, 7, 3f, 0.5f);
            }
            if (u > 0.3f && UnityEngine.Random.value < 0.02f) G.Raider.Damage(1f, G.Raider.transform.position, true);
            Vector3 mid = (P.transform.position + G.Raider.transform.position) * 0.5f;
            Look(ShipPt(-30f + u * 10f, 16f, -52f + u * 8f), mid + Vector3.up * 5f, 48);
        }, 0.55f);
        G.CombatEnabled = false;
        G.RaiderAI.Active = false;
        G.Raider.ClearFires();
        G.Raider.gameObject.SetActive(false);

        // 7. Sunset at the Isle of Echoes
        Env(SkyController.Preset.Sunset, 0, -95, 1f);
        Sail(new Vector3(-1180, 0, -60), 268f, 11f);
        for (int i = 0; i < 10; i++) yield return null;
        yield return Frames("07_sunset_echoes", 6f, u =>
            Look(ShipPt(6f, 5f + u * 2f, -34f - u * 8f), ShipPt(0, 7, 40), 44));

        // 8. Era Shift reveals the ghost harbour
        Vector3 bay = World.P("echo_bay");
        Sail(bay + new Vector3(30, 0, 70), 200f, 4f);
        EraShift.I.Unlocked = true;
        EraShift.I.Energy = 1f;
        EraShift.I.DrainRate = 0.01f;
        bool shifted = false;
        Vector3 lh = World.P("echo_lighthouse");
        yield return Frames("08_era_shift", 7.5f, u =>
        {
            if (!shifted && u > 0.2f) { shifted = true; EraShift.I.Toggle(); }
            Look(bay + new Vector3(95 - u * 25f, 26f, 120f), new Vector3(-1490, 12, 40), 50);
        }, 0.75f);

        // 9. Following the Vellimeen through the shoals
        Env(SkyController.Preset.Dusk, 0, -95, 1f);
        foreach (var mk in World.RouteMarkers) mk.GetComponent<Renderer>().sharedMaterial.SetFloat("_Base", 0.2f);
        var ghost = GhostShip.Create(World.Channel);
        Vector3 c0 = World.Channel[0];
        Sail(c0 + new Vector3(70, 0, 10), 235f, 8f);
        for (int i = 0; i < 90; i++) yield return null;
        yield return Frames("09_vellimeen", 7f, u =>
        {
            Vector3 g = ghost.transform.position;
            Look(ShipPt(-18f, 20f, -40f), Vector3.Lerp(g, P.transform.position, 0.4f) + Vector3.up * 4f, 52);
        }, 0.6f);
        EraShift.I.Deactivate();
        ghost.gameObject.SetActive(false);

        // 10. Ras al-Mira at dusk
        for (int i = 0; i < 40; i++) yield return null;
        Vector3 od = World.P("dock_outpost");
        G.PlaceShip(od, 270f);
        P.Frozen = true;
        yield return Frames("10_ras_al_mira", 5.5f, u =>
        {
            float a = Mathf.Lerp(150f, 115f, u) * Mathf.Deg2Rad;
            Look(od + new Vector3(Mathf.Cos(a) * 70f, 20f, Mathf.Sin(a) * 70f), od + new Vector3(-30, 6, 0), 46);
        });

        // 11. Finale: into the sunset
        Env(SkyController.Preset.Sunset, 0, -90, 1f);
        Sail(new Vector3(-600, 0, 300), 266f, 12f);
        for (int i = 0; i < 10; i++) yield return null;
        yield return Frames("11_finale", 7f, u =>
            Look(ShipPt(-4f, 3.5f + u * 16f, -40f - u * 30f), ShipPt(0, 6 + u * 4f, 60), 46), 0.3f);

        // bonus stills for thumbnails / covers (4K only)
        Env(SkyController.Preset.Sunset, 0, -90, 1f);
        Sail(new Vector3(-600, 0, 300), 250f, 11f);
        for (int i = 0; i < 20; i++) yield return null;
        Look(ShipPt(16f, 3f, 14f), ShipPt(0, 8, -2), 42);
        yield return new WaitForEndOfFrame();
        Render(rt4k, tex4k, Path.Combine(stillDir, "hero_sunset_close.png"), false);
        Env(SkyController.Preset.Dawn, 0, -75, 1f);
        Sail(World.P("start") + new Vector3(-200, 0, -10), 280f, 11f);
        for (int i = 0; i < 20; i++) yield return null;
        Look(ShipPt(14f, 2.5f, 22f), ShipPt(0, 8, 0), 44);
        yield return new WaitForEndOfFrame();
        Render(rt4k, tex4k, Path.Combine(stillDir, "hero_dawn_close.png"), false);

        File.WriteAllLines(Path.Combine(outDir, "timeline.csv"), timeline);
        Debug.Log("[CAPTURE] done: " + frame + " frames");
        Time.captureFramerate = 0;
        yield return new WaitForSecondsRealtime(1f);
        Application.Quit();
    }

    // ------------------------------------------------------------------ audio export
    void ExportAudio()
    {
        string adir = Path.Combine(outDir, "audio");
        Directory.CreateDirectory(adir);
        string[] names = { "music_calm", "music_storm", "music_combat", "music_mystery", "conch", "thunder", "shift_on", "stinger", "discovery", "volley", "crash", "ocean", "gong" };
        foreach (var n in names)
        {
            var clip = AudioMan.I.GetClip(n);
            if (clip == null) continue;
            var data = new float[clip.samples * clip.channels];
            clip.GetData(data, 0);
            WriteWav(Path.Combine(adir, n + ".wav"), data, clip.frequency, clip.channels);
        }
    }

    static void WriteWav(string path, float[] data, int rate, int ch)
    {
        using (var fs = new FileStream(path, FileMode.Create))
        using (var w = new BinaryWriter(fs))
        {
            int bytes = data.Length * 2;
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            w.Write(36 + bytes);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            w.Write(16); w.Write((short)1); w.Write((short)ch); w.Write(rate); w.Write(rate * ch * 2); w.Write((short)(ch * 2)); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            w.Write(bytes);
            foreach (var f in data) w.Write((short)(Mathf.Clamp(f, -1f, 1f) * 32767f));
        }
    }
}
