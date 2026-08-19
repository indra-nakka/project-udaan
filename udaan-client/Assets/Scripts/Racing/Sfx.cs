using UnityEngine;

/// <summary>
/// Programmer-audio: short SFX synthesized in code (no asset files). Clips are generated once and
/// cached, then played at a world position. Replace with authored audio later.
/// Requires an AudioListener in the scene (the player camera has one).
/// </summary>
public static class Sfx
{
    private static AudioClip _bullet, _rocket, _explosion, _hit, _laugh;

    public static void Bullet(Vector3 pos)    => Play(Get(ref _bullet, MakeBullet), pos, 0.22f);
    public static void Rocket(Vector3 pos)    => Play(Get(ref _rocket, MakeRocket), pos, 0.4f);
    public static void Explosion(Vector3 pos) => Play(Get(ref _explosion, MakeExplosion), pos, 0.75f);
    public static void Hit(Vector3 pos)       => Play(Get(ref _hit, MakeHit), pos, 0.35f);
    public static void BossLaugh(Vector3 pos) => Play(Get(ref _laugh, MakeLaugh), pos, 0.9f);   // placeholder → authored clip in #93

    private static AudioClip Get(ref AudioClip c, System.Func<AudioClip> make) { if (c == null) c = make(); return c; }

    // 2D playback (spatialBlend 0) so SFX are equally audible in first- AND third-person views —
    // PlayClipAtPoint is 3D and goes quiet when the TPV camera pulls back from the action.
    private static void Play(AudioClip c, Vector3 pos, float vol)
    {
        if (c == null) return;
        var go = new GameObject("Sfx");
        go.transform.position = pos;
        var src = go.AddComponent<AudioSource>();
        src.clip = c; src.volume = vol; src.spatialBlend = 0f;
        src.Play();
        Object.Destroy(go, c.length + 0.1f);
    }

    private const int SR = 44100;

    private static AudioClip Build(string name, float seconds, System.Func<int, float, float> sample)
    {
        int n = Mathf.Max(1, (int)(SR * seconds));
        var s = new float[n];
        for (int i = 0; i < n; i++) s[i] = sample(i, i / (float)n);
        var clip = AudioClip.Create(name, n, 1, SR, false);
        clip.SetData(s, 0);
        return clip;
    }

    // Snappy zap: noise + a high tone, fast decay.
    private static AudioClip MakeBullet() => Build("bullet", 0.08f, (i, t) =>
    {
        float env = Mathf.Exp(-t * 34f);
        return ((Random.value * 2f - 1f) * 0.45f + Mathf.Sin(2f * Mathf.PI * 880f * (i / (float)SR)) * 0.35f) * env;
    });

    // Whooshy launch: descending tone.
    private static AudioClip MakeRocket() => Build("rocket", 0.35f, (i, t) =>
    {
        float freq = Mathf.Lerp(500f, 120f, t);
        return (Mathf.Sin(2f * Mathf.PI * freq * (i / (float)SR)) * 0.5f + (Random.value * 2f - 1f) * 0.2f) * Mathf.Exp(-t * 4f);
    });

    // Boom: filtered-ish noise with slow decay.
    private static AudioClip MakeExplosion() => Build("boom", 0.55f, (i, t) =>
    {
        float env = Mathf.Exp(-t * 6f);
        float low = Mathf.Sin(2f * Mathf.PI * 70f * (i / (float)SR)) * 0.4f;
        return ((Random.value * 2f - 1f) * 0.7f + low) * env;
    });

    // Tick: short mid tone.
    private static AudioClip MakeHit() => Build("hit", 0.05f, (i, t) =>
        Mathf.Sin(2f * Mathf.PI * 420f * (i / (float)SR)) * 0.6f * Mathf.Exp(-t * 40f));

    // "Mwahaha" — a low descending growl with a chuckle tremolo. Cartoon-evil, not scary.
    private static AudioClip MakeLaugh() => Build("laugh", 1.2f, (i, t) =>
    {
        float baseFreq = Mathf.Lerp(180f, 95f, t);
        float trem = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * 7f * t);          // chuckle wobble
        float tone = Mathf.Sin(2f * Mathf.PI * baseFreq * (i / (float)SR)) * 0.5f;
        float growl = Mathf.Sin(2f * Mathf.PI * baseFreq * 0.5f * (i / (float)SR)) * 0.4f;
        return (tone + growl) * trem * Mathf.Exp(-t * 1.1f);
    });
}
