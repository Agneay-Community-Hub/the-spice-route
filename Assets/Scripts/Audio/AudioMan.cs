using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Plays the synthesised clips: pooled one-shots, ambience beds and cross-faded music layers.</summary>
public class AudioMan : MonoBehaviour
{
    public static AudioMan I;
    readonly Dictionary<string, List<AudioClip>> clips = new Dictionary<string, List<AudioClip>>();
    readonly List<AudioSource> pool = new List<AudioSource>();
    AudioSource ocean, wind, rain, fire;
    readonly AudioSource[] music = new AudioSource[4];
    readonly float[] musicTarget = new float[4];
    public enum Track { Calm = 0, Combat = 1, Mystery = 2, Storm = 3, None = -1 }
    public Track Current = Track.None;
    public float MasterVolume = 0.85f, MusicVolume = 0.55f;
    public bool Ready;
    public float Duck = 1f;
    float duckTarget = 1f;

    public static AudioMan Create(Transform parent)
    {
        var go = new GameObject("Audio");
        go.transform.SetParent(parent, false);
        I = go.AddComponent<AudioMan>();
        return I;
    }

    void AddClip(string name, float[] data)
    {
        if (!clips.TryGetValue(name, out var l)) { l = new List<AudioClip>(); clips[name] = l; }
        l.Add(Synth.Clip(name, data));
    }

    public IEnumerator Generate(System.Action<float, string> progress)
    {
        Synth.Prepare();
        var steps = new List<(string, System.Func<float[]>)>
        {
            ("ocean", Synth.Ocean), ("wind", Synth.WindLoop), ("rain", Synth.RainLoop), ("fire", Synth.FireLoop),
            ("creak", Synth.Creak), ("creak", Synth.Creak), ("creak", Synth.Creak),
            ("thunder", Synth.Thunder), ("thunder", Synth.Thunder),
            ("volley", Synth.Volley), ("thunk", Synth.Thunk), ("splash_small", Synth.SplashSmall),
            ("crash", Synth.Crash), ("sink", Synth.Sink), ("click", Synth.Click), ("coin", Synth.Coin),
            ("gong", Synth.Gong), ("shift_on", Synth.ShiftOn), ("shift_off", Synth.ShiftOff),
            ("discovery", Synth.Discovery), ("conch", Synth.ConchHorn), ("stinger", Synth.Stinger), ("victory", Synth.Victory),
            ("music_calm", Synth.MusicCalm), ("music_combat", Synth.MusicCombat), ("music_mystery", Synth.MusicMystery), ("music_storm", Synth.MusicStorm),
        };
        for (int i = 0; i < steps.Count; i++)
        {
            progress((float)i / steps.Count, steps[i].Item1.StartsWith("music") ? "Composing the voyage" : "Tuning the sea");
            yield return null;
            AddClip(steps[i].Item1, steps[i].Item2());
        }
        ocean = Loop("ocean", 0.0f);
        wind = Loop("wind", 0.0f);
        rain = Loop("rain", 0.0f);
        fire = Loop("fire", 0.0f);
        string[] tracks = { "music_calm", "music_combat", "music_mystery", "music_storm" };
        for (int i = 0; i < 4; i++) music[i] = Loop(tracks[i], 0f);
        Ready = true;
    }

    AudioSource Loop(string name, float vol)
    {
        var src = gameObject.AddComponent<AudioSource>();
        src.clip = Get(name);
        src.loop = true;
        src.volume = vol;
        src.spatialBlend = 0;
        src.playOnAwake = false;
        src.Play();
        return src;
    }

    public AudioClip GetClip(string name) => Get(name);

    AudioClip Get(string name)
    {
        if (!clips.TryGetValue(name, out var l) || l.Count == 0) return null;
        return l[Random.Range(0, l.Count)];
    }

    AudioSource Free()
    {
        foreach (var s in pool) if (!s.isPlaying) return s;
        if (pool.Count >= 24) return pool[Random.Range(0, pool.Count)];
        var go = new GameObject("sfx");
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 20f;
        src.maxDistance = 450f;
        src.dopplerLevel = 0;
        pool.Add(src);
        return src;
    }

    public void Play(string name, float vol = 1f, float pitch = 1f)
    {
        if (!Ready) return;
        var c = Get(name);
        if (c == null) return;
        var s = Free();
        s.transform.localPosition = Vector3.zero;
        s.spatialBlend = 0;
        s.clip = c;
        s.volume = vol * MasterVolume;
        s.pitch = pitch;
        s.Play();
    }

    public void PlayAt(string name, Vector3 pos, float vol = 1f, float pitch = 1f)
    {
        if (!Ready) return;
        var c = Get(name);
        if (c == null) return;
        var s = Free();
        s.transform.position = pos;
        s.spatialBlend = 0.85f;
        s.clip = c;
        s.volume = vol * MasterVolume;
        s.pitch = pitch;
        s.Play();
    }

    public void PlayDelayed(string name, float delay, float vol, float pitch) => StartCoroutine(Delayed(name, delay, vol, pitch));

    IEnumerator Delayed(string name, float delay, float vol, float pitch)
    {
        yield return new WaitForSeconds(delay);
        Play(name, vol, pitch);
    }

    public void SetMusic(Track t)
    {
        Current = t;
        for (int i = 0; i < 4; i++) musicTarget[i] = (int)t == i ? 1f : 0f;
    }

    public void DuckMusic(float amount) => duckTarget = amount;

    void Update()
    {
        if (!Ready) return;
        float dt = Time.unscaledDeltaTime;
        Duck = Mathf.MoveTowards(Duck, duckTarget, dt * 0.8f);
        for (int i = 0; i < 4; i++)
        {
            float target = musicTarget[i] * MusicVolume * MasterVolume * Duck;
            float speed = target > music[i].volume ? 0.25f : 0.35f;
            music[i].volume = Mathf.MoveTowards(music[i].volume, target, dt * speed);
        }
        float storm = SkyController.I != null ? SkyController.I.Storm : 0f;
        float ws = Mathf.Clamp01(Wind.Effective / 1.6f);
        ocean.volume = MasterVolume * (0.32f + storm * 0.3f);
        ocean.pitch = 1f - storm * 0.12f;
        wind.volume = MasterVolume * (0.08f + ws * 0.18f + storm * 0.35f);
        wind.pitch = 0.9f + ws * 0.2f + storm * 0.15f;
        rain.volume = MasterVolume * Mathf.Clamp01((storm - 0.3f) / 0.7f) * 0.55f;
        float fires = 0;
        if (Game.I != null && Game.I.Player != null) fires = Mathf.Clamp01(Game.I.Player.FireCount / 3f);
        fire.volume = Mathf.MoveTowards(fire.volume, fires * 0.5f * MasterVolume, dt);
    }
}
