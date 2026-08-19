# Udaan — Demo Scope & Definition of Done

_Session 066. The demo = the current single combat-arena level ("Sky Sentinel"), **re-skinned kid-appropriate** (toy weapons, PG-cartoon defeat), as a polished self-contained slice. **Story, the 8 cities, the garage/component system, and mixed mission modes are Phase +1** (after the demo ships). Keep the demo tight._

## Tone lock (applies to the demo)
- Weapons = toys (foam, water balloon, bubble, paint, **sticky slime**). No human/animal harm — only toy-bots & limited property.
- Defeat = **PG cartoon** (Wile E. Coyote): smoke → sputter → **spiral/tumble down** → harmless *poof*. Not gentle, not real.
- Hero name is **player-selectable, default `Ira`** (Prof-Oak style). Game title = **Udaan** (separate).

## Definition of Done — the demo ships when all of these are true

**Front-end & flow**
- [ ] **Start menu** — title, **Play**, **name entry** (default `Ira`), **difficulty select (Easy / Medium / Hard / Pain)**, options (volume, mute), quit. _(#89)_
- [ ] **Pause menu** — Resume, Restart, Quit to menu. _(#90)_
- [ ] **Kid-friendly win / lose + results** — "You did it! ⭐⭐⭐" / "Oops — try again!" (stars, not raw numbers; keep the console scorecard + CSV for us). _(#95)_
- [ ] **Settings persist** (PlayerPrefs): name, difficulty, volume. _(#89)_
- [ ] **Demo onboarding (lite)** — ~20s teach: fly → aim → fire, no-fail. _(from Design-Onboarding)_

**Player drone**
- [ ] **T0 design finalized** in-engine (the DIY quad, materials/colors locked, flies well). _(#91a)_
- [ ] **Mid-game upgrades reflect on the drone** — the Upgrade beat's picks visibly change the model (a bolted-on part / bigger props / new blaster / color), not just stats. _(#91)_

**Enemies & boss**
- [ ] **Enemy bot design** — kid-appropriate hostile bot(s), modeled + reskinned. _(#92a)_
- [ ] **Final boss = purple OCTA-copter** (8 rotors), imposing; **slightly scary weapon system**; **evil-laugh** SFX on entrance/attacks. _(#92)_
- [ ] **Cartoon defeat VFX** — smoke + spiral-down replaces the old "death pop." _(#94)_

**Audio (whole set)**
- [x] **BGM** (menu + gameplay + boss) — procedural `Music.cs`, three crossfaded moods (session-067).
- [x] **Firing / collisions / defeat-poof / boss laugh** — procedural `Sfx.cs` (2D mix, audible in TPV).
- [ ] **Announcer / on-screen announcements** (wave start, "boss incoming!", win/lose) — text + sting still to add (VO deferred, no voice assets).
- [ ] **UI clicks** + a settings panel with master/SFX/music sliders (menu volume ± removed per request; master stays at GameConfig default). _(#93)_

**Difficulty**
- [x] Selector code = **Easy / Medium / Hard / Pain** (multipliers on enemy HP/dmg, Core HP, lives; Pain = 1 life). _Done in code (#70); needs menu wiring (#89)._

## My additions (worth folding in)
- **HUD kid-ification** — bigger, friendlier, less military language (Core→"the Nest"? TBD); clear icons.
- **"Play again" loop** wired to the menu (not just the R key).
- **Boss telegraph** — the octacopter should wind up its scary attacks with a clear tell (fair + dramatic), paired with the laugh.
- **One-screen credits / "made with" splash** (optional, nice for a demo).
- **Performance sanity pass** on a real device if you have one (frame time at peak drone count).
- **A single evocative demo title card** (Udaan logo) even if placeholder.

## Explicitly OUT of the demo (Phase +1)
Story & cutscenes · the 8 Indian-city chapters · the **garage / modular-component system** · mixed mission modes (delivery/rescue/race/photo) · FPV goggle camera mode · multiplayer. _These are designed (see `Design-Story`, `Design-Garage-and-Learning`, `Design-Weapons-and-Tools`) and wait behind the demo._

## Suggested build order
1. **Start + Pause menus** (+ difficulty/name/volume wiring & persistence) — makes it feel like a game. _(#89, #90)_
2. **Cartoon defeat + slime/toy weapon reskin** — locks the tone. _(#94, #86)_
3. **Purple octacopter boss** (model → behavior → evil laugh). _(#92)_
4. **Visible upgrade parts on the drone.** _(#91)_
5. **Full sound set + BGM + announcer.** _(#93)_
6. **Kid-friendly win/lose/results + lite onboarding + polish.** _(#95)_
