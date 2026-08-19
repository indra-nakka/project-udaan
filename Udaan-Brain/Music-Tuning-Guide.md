# Udaan — Music Tuning Guide

All the game music is generated in code (no audio files). You change it by editing **one file**:
`udaan-client/Assets/Scripts/Racing/Music.cs`. At the top are three "songs" — **`MENU`**, **`BATTLE`** (in-game), and **`BOSS`** — each a block of settings you can freely edit.

**To hear a change:** edit a value → save → press **Play** in Unity. (Each mood is built the first time it plays and cached for the session; recompiling after an edit clears the cache, so your change is heard next time that mood starts.)

---

## The knobs (every field, what it does)

| Field | What it controls | Try… |
|-------|------------------|------|
| `bpm` | **Speed.** Higher = faster/more energetic. | 90 = relaxed, 120 = lively, 140+ = fast/upbeat |
| `rootHz` | **Key** (how high/low overall). | 110 = low/dark (A2), 130.81 = C3, 146.83 = D3, 164.81 = E3 |
| `scale` | **The notes allowed**, as semitones from the key. | `{0,2,4,7,9}` = happy (major pentatonic); `{0,2,3,5,7,8,10}` = sad/tense (minor) |
| `progression` | **The chords**, one root per bar (semitones from key). Length = number of bars. | `{0,7,9,5}` = classic I–V–vi–IV |
| `melody` | **The tune** — one note per beat for the whole loop. Each number is an index into `scale`; `-1` = a rest (silence). | see "Write your own tune" below |
| `melodyOctave` | Lifts the melody up. | 12 = one octave up (leave at 12) |
| `bassPerBar` | Bass hits per bar. | 1 = held/calm, 2 = a drive, 4 = a pulse |
| `pad` | Volume of the soft background chord. 0 = off. | 0.10 warm, 0.15 lush, 0 = bare |
| `melodyVol` / `bassVol` | Layer volumes. | melody 0.15–0.22, bass 0.20–0.28 |
| `decay` | Note shape. High = short/plucky (punchy), low = long/sustained (pad-like). | 2.6 punchy, 1.3 smooth |
| `vibrato` | Pitch wobble. 0 = clean, more = "singing". | 0.05 subtle, 0.2 expressive |
| `tri` | Tone. `true` = triangle (brighter), `false` = sine (mellow/warm). | brighter tunes: true |
| `minor` | Makes the pad chords minor (sad). | true for boss/spooky |
| `drone` | Adds a low sustained note (menace). | true for boss |

There's also one global: `BgmVolume` (near the top of the class) = **overall music loudness** vs the sound effects. Lower it if the music is too present.

---

## Recipes

**Faster / more upbeat:** raise `bpm` (e.g. 120 → 140) and fill in the `melody` with fewer `-1` rests. Set `bassPerBar = 2`.

**Calmer / more chill:** lower `bpm`, set `bassPerBar = 1`, raise `pad`, lower `decay` (more sustained), and put more `-1` rests in the melody.

**Happier:** `scale = {0,2,4,7,9}`, `minor = false`. **Sadder / spookier:** `scale = {0,2,3,5,7,8,10}`, `minor = true`, and lower `rootHz`.

**Warmer (less "buzzy"):** set `tri = false` (sine). **Brighter/poppier:** `tri = true`.

**Quieter in the mix:** lower `BgmVolume` (e.g. 0.24 → 0.18).

**Longer, less repetitive loop:** add more chords to `progression` (and extend `melody` to match — see the rule below).

---

## Write your own tune (the `melody` array)

The melody is one note per beat. Each number picks a note from `scale` **by position** (not the semitone — the index):

- With `scale = {0, 2, 4, 7, 9}`: index `0`→first note, `1`→second, … `4`→fifth. `-1` = a rest.
- So `melody = { 0, 2, 4, 2 }` plays note1, note3, note5, note3 — a little rising-then-falling phrase.

**The one rule:** the melody length must equal **4 × (number of chords in `progression`)** — because there are 4 beats per bar. So 4 chords → 16 notes, 8 chords → 32 notes. (If the lengths don't match you'll get an odd loop, not a crash.)

**Worked example** — a bouncy 4-bar phrase (16 notes) in major pentatonic:
```
progression = { 0, 7, 9, 5 },              // 4 chords → 4 bars
melody = { 0,2,4,2,  4,3,2,4,  2,4,3,1,  0,2,-1,-1 }   // 16 notes
```
Numbers going up = melody rises; repeating a number = a held/repeated note; `-1` = a little breath.

---

## Copy-paste presets

**Upbeat & playful (in-game):**
```
bpm = 138, scale = {0,2,4,7,9}, bassPerBar = 2, tri = true,
pad = 0.10, melodyVol = 0.18, decay = 2.6, vibrato = 0.05, minor = false, drone = false
```

**Chill background:**
```
bpm = 96, scale = {0,2,4,7,9}, bassPerBar = 1, tri = false,
pad = 0.15, melodyVol = 0.14, decay = 1.6, vibrato = 0.04, minor = false, drone = false
```

**Tense boss:**
```
bpm = 140, scale = {0,2,3,5,7,8,10}, rootHz = 110, bassPerBar = 2, tri = true,
pad = 0.10, melodyVol = 0.19, decay = 2.6, vibrato = 0.05, minor = true, drone = true
```

That's everything — the three song blocks plus `BgmVolume` give you full control without touching any of the synth code below them.
