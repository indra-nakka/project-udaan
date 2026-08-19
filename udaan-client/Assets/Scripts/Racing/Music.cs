using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Programmer background music: three looping moods (Menu / Battle / Boss) synthesized in code — no
/// asset files. Each loop is built lazily the first time its mood plays, cached, then crossfaded on a
/// single persistent 2D AudioSource (sits under the SFX). Survives scene reloads (DontDestroyOnLoad).
///
/// ─── HOW TO TWEAK THE MUSIC ────────────────────────────────────────────────
/// Everything is in the three <see cref="Song"/> objects below (MENU / BATTLE / BOSS). Edit a field,
/// save, press Play. Fields:
///   bpm          tempo (higher = faster / busier).
///   rootHz       musical key. 130.81=C3, 146.83=D3, 164.81=E3, 110=A2 (lower = darker).
///   scale        the notes allowed, as semitones from the root (major-pentatonic vs minor, etc.).
///   progression  one chord ROOT (semitones from key) per bar — the chord changes.
///   melody       one scale-DEGREE per beat for the whole loop (index into scale); -1 = a rest/silence.
///   melodyOctave semitones to lift the melody (12 = one octave up).
///   bassPerBar   bass hits per bar (1 = held, 4 = a pulse).
///   pad          sustained background chord volume (0 = off; warmth/atmosphere).
///   melodyVol / bassVol   layer volumes.
///   decay        note shape: high = plucky/short, low = sustained/pad-like.
///   vibrato      pitch wobble depth (0 = sterile, ~0.2 = expressive).
///   tri / minor / drone   waveform (triangle vs sine), minor pad-chord, and a low sustained boss drone.
/// Add notes to `melody` (must stay length = 4 × number-of-chords) to write a new tune.
/// ────────────────────────────────────────────────────────────────────────────
/// </summary>
public class Music : MonoBehaviour
{
    public enum Mood { None, Menu, Battle, Boss }

    // ── TUNABLE SONGS ────────────────────────────────────────────────────────
    private class Song
    {
        public float bpm, rootHz;
        public int[] scale, progression, melody;
        public int melodyOctave, bassPerBar;
        public float pad, melodyVol, bassVol, decay, vibrato;
        public bool tri, minor, drone;
    }

    // Calm, welcoming. Major pentatonic, slow, gentle.
    private static readonly Song MENU = new Song {
        bpm = 84f, rootHz = 130.81f,                 // C3
        scale = new[] { 0, 2, 4, 7, 9 },             // major pentatonic
        progression = new[] { 0, 9, 5, 7 },          // I  vi IV V
        melody = new[] { 0, 2, 4, 2,  1, 3, 2, -1,  4, 3, 2, 0,  1, -1, 0, -1 },
        melodyOctave = 12, bassPerBar = 1,
        pad = 0.10f, melodyVol = 0.20f, bassVol = 0.30f, decay = 3.0f, vibrato = 0.06f,
        tri = false, minor = false, drone = false,
    };

    // FAST & upbeat flight theme — bright, busy melody, a driving 2-per-bar bass. Kept warm (sub-octave
    // in the synth) + an 8-bar loop so it's lively without being grating.
    private static readonly Song BATTLE = new Song {
        bpm = 136f, rootHz = 146.83f,                // D
        scale = new[] { 0, 2, 4, 7, 9 },             // major pentatonic (happy)
        progression = new[] { 0, 7, 9, 5, 0, 7, 5, 7 },   // 8 bars
        melody = new[] {
            0, 2, 4, 2,  4, 3, 2, 4,   3, 1, 2, 3,  4, 2, 0, -1,
            2, 4, 3, 1,  3, 4, 2, 3,   1, 2, 4, 2,  3, 1, 0, -1 },
        melodyOctave = 12, bassPerBar = 2,           // driving bass
        pad = 0.10f, melodyVol = 0.18f, bassVol = 0.24f, decay = 2.6f, vibrato = 0.05f,
        tri = true, minor = false, drone = false,
    };

    // FAST & intense boss theme — driving minor with a low drone. Busy and tense, still warm-toned.
    private static readonly Song BOSS = new Song {
        bpm = 138f, rootHz = 110.00f,                // A2
        scale = new[] { 0, 2, 3, 5, 7, 8, 10 },      // natural minor
        progression = new[] { 0, 8, 5, 7, 0, 8, 7, 5 },   // 8 bars
        melody = new[] {
            0, 3, 5, 3,  6, 5, 3, 0,   5, 6, 5, 3,  2, 3, 5, -1,
            0, 3, 5, 6,  5, 3, 2, 3,   5, 3, 2, 0,  3, 2, 0, -1 },
        melodyOctave = 12, bassPerBar = 2,
        pad = 0.10f, melodyVol = 0.19f, bassVol = 0.26f, decay = 2.6f, vibrato = 0.05f,
        tri = true, minor = true, drone = true,
    };

    // ──────────────────────────────────────────────────────────────────────────
    private static Music _inst;
    private AudioSource _src;
    private Mood _current = Mood.None;
    private Coroutine _fade;
    private readonly Dictionary<Mood, AudioClip> _clips = new Dictionary<Mood, AudioClip>();

    private const int SR = 44100;
    private const float BgmVolume = 0.24f;   // sits well under the SFX — background, not foreground

    public static void Menu()   => Ensure().Cue(Mood.Menu);
    public static void Battle() => Ensure().Cue(Mood.Battle);
    public static void Boss()   => Ensure().Cue(Mood.Boss);
    public static void Stop()   { if (_inst != null) _inst.Cue(Mood.None); }

    private static Music Ensure()
    {
        if (_inst == null)
        {
            var go = new GameObject("Music");
            DontDestroyOnLoad(go);
            _inst = go.AddComponent<Music>();
            _inst._src = go.AddComponent<AudioSource>();
            _inst._src.loop = true;
            _inst._src.spatialBlend = 0f;      // 2D — audible in FPV and TPV alike
            _inst._src.volume = 0f;
            _inst._src.playOnAwake = false;
        }
        return _inst;
    }

    private void Cue(Mood m)
    {
        if (m == _current) return;
        _current = m;
        if (_fade != null) StopCoroutine(_fade);
        _fade = StartCoroutine(Swap(m));
    }

    private IEnumerator Swap(Mood m)
    {
        float v0 = _src.volume;
        for (float t = 0f; t < 0.4f && _src.volume > 0f; t += Time.unscaledDeltaTime)
        { _src.volume = Mathf.Lerp(v0, 0f, t / 0.4f); yield return null; }
        _src.volume = 0f;

        if (m == Mood.None) { _src.Stop(); yield break; }

        if (!_clips.TryGetValue(m, out var clip) || clip == null)
        { clip = Compose(m); _clips[m] = clip; }        // build once, cache

        _src.clip = clip;
        _src.Play();
        for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime)
        { _src.volume = Mathf.Lerp(0f, BgmVolume, t / 0.6f); yield return null; }
        _src.volume = BgmVolume;
    }

    // ── synthesis ────────────────────────────────────────────────────────────
    private struct Note { public float start, dur, freq, gain, decay, vib; public bool tri; }

    private static float Semi(float rootHz, int semi) => rootHz * Mathf.Pow(2f, semi / 12f);
    private static float Tri(float ph) { float f = ph - Mathf.Floor(ph); return 4f * Mathf.Abs(f - 0.5f) - 1f; }

    private AudioClip Compose(Mood m)
    {
        Song s = m == Mood.Battle ? BATTLE : m == Mood.Boss ? BOSS : MENU;

        float beat = 60f / s.bpm;
        int beatsPerBar = 4, bars = s.progression.Length;
        float loop = beat * beatsPerBar * bars;
        var notes = new List<Note>();

        for (int bar = 0; bar < bars; bar++)
        {
            int chordRoot = s.progression[bar];
            float barT = bar * beatsPerBar * beat;

            // bass
            for (int b = 0; b < s.bassPerBar; b++)
            {
                float step = beatsPerBar * beat / s.bassPerBar;
                notes.Add(new Note {
                    start = barT + b * step, dur = step * 0.92f,
                    freq = Semi(s.rootHz, chordRoot - 12), gain = s.bassVol,
                    decay = s.bassPerBar == 1 ? 1.0f : 4.5f, tri = true });
            }

            // sustained pad (soft triad — warmth / atmosphere)
            if (s.pad > 0f)
            {
                int third = s.minor ? 3 : 4;
                foreach (int semi in new[] { chordRoot, chordRoot + third, chordRoot + 7 })
                    notes.Add(new Note {
                        start = barT, dur = beatsPerBar * beat,
                        freq = Semi(s.rootHz, semi), gain = s.pad, decay = 0.5f, tri = false });
            }

            // low sustained drone (boss menace)
            if (s.drone)
                notes.Add(new Note {
                    start = barT, dur = beatsPerBar * beat,
                    freq = Semi(s.rootHz, chordRoot - 24), gain = 0.16f, decay = 0.35f, tri = true });

            // melody
            for (int b = 0; b < beatsPerBar; b++)
            {
                int deg = s.melody[(bar * beatsPerBar + b) % s.melody.Length];
                if (deg < 0) continue;   // rest
                int semi = s.scale[deg % s.scale.Length] + s.melodyOctave + chordRoot;
                notes.Add(new Note {
                    start = barT + b * beat, dur = beat * 0.85f,
                    freq = Semi(s.rootHz, semi), gain = s.melodyVol,
                    decay = s.decay, vib = s.vibrato, tri = s.tri });
            }
        }

        return Render(m.ToString(), loop, notes);
    }

    private AudioClip Render(string name, float seconds, List<Note> notes)
    {
        int n = Mathf.Max(1, (int)(SR * seconds));
        var buf = new float[n];
        foreach (var nt in notes)
        {
            int s0 = (int)(nt.start * SR);
            int s1 = (int)((nt.start + nt.dur) * SR);
            for (int i = s0; i < s1 && i < n; i++)
            {
                if (i < 0) continue;
                float tt = (i - s0) / (float)SR;
                float env = Mathf.Exp(-tt * nt.decay) * (1f - Mathf.Exp(-tt * 30f)); // soft attack, exp decay
                float arg = 2f * Mathf.PI * nt.freq * (i / (float)SR)
                          + nt.vib * Mathf.Sin(2f * Mathf.PI * 5.5f * tt);            // gentle vibrato
                float w = nt.tri ? Tri(arg / (2f * Mathf.PI)) : Mathf.Sin(arg);
                w += Mathf.Sin(arg * 0.5f) * 0.12f;                                   // warm SUB-octave (no bright buzz)
                buf[i] += w * env * nt.gain;
            }
        }
        for (int i = 0; i < n; i++) buf[i] = Mathf.Clamp(buf[i], -0.95f, 0.95f);      // soft ceiling
        var clip = AudioClip.Create(name, n, 1, SR, false);
        clip.SetData(buf, 0);
        return clip;
    }
}
