# Udaan — Playful Weapons & Tools (design)

_Session 065 (tone updated 066). For ages 5–10, "weapons" are **toy, non-lethal gadgets** a kid would bolt onto a drone — foam darts, water balloons, bubbles, paint, **sticky slime**. No guns; no human or animal harm — ever. Damage is only to **toys and property**. Defeat can be **PG cartoon "gore"** — Wile E. Coyote / Cartoon Network level: hit bots belch smoke, sputter, and **spiral comically down**. Fun over realism. Mounts are visible and progress with the drone tier._

## Principles

- **No human or animal harm — ever.** Targets are only enemy toy-bots and (limited) property. That's what keeps PG-cartoon destruction totally fine.
- **PG cartoon defeat (not too gentle, not too real).** A beaten bot doesn't die a violent death and doesn't just politely power off either — it goes **Wile E. Coyote**: sputter → puff of smoke → comedic **spiral/tumble down** → harmless *clunk*/*poof*. Bits may rattle off. Funny, satisfying, bloodless.
- **Property damage is limited and consequenced.** Only "toy"/prop targets take damage, and with *relevant* light consequences (a knocked-over stall you might have to avoid, etc.) — enough to matter, never enough to feel real or scary.
- **Slightly believable, not realistic** (user's steer): the *gadget* can be whimsical as long as the *quad* sells the realism.
- **Visible on the drone** and swappable in the garage (the tool/blaster mount slot).

## The arsenal (progresses with the drone tier)

| Tier feel | Gadget | Effect | VFX / SFX |
|-----------|--------|--------|-----------|
| T0 junk | **Rubber-band flicker / foam-dart popper** | knocks a bot back a little | *thwip*, foam dart arcs |
| early | **Water squirter** | soaks a bot → it sputters & droops | splash, drips |
| mid | **Paint-splat blaster** | marks bots with color; enough splats → powered down | colorful splat decals |
| mid | **Bubble cannon** | traps a bot in a floaty bubble briefly | shimmer, pop |
| later | **Sticky-slime launcher** | globs a bot, gums its rotors → it sputters & spirals | green splat, drips |
| T5 advanced | **"Sparkle beam"** | gentle energy sparkle → bot powers off with a twinkle | soft glow, chimes |

Each is a garage-swappable mount; **payload weight is a trade-off** (a big bubble cannon = more fun, less agile — ties into the learning system).

## Non-combat tools (for the mixed missions)

Because combat is only one mode (delivery, rescue, races, photo), the mount also holds **tools**:

- **Grabber claw / delivery magnet** — pick up and drop cargo (delivery, rescue).
- **Net launcher** — scoop up runaway critters/bots (rescue).
- **Spotlight** — light dark areas (exploration).
- **Camera** — line up shots (photo missions).

Same slot, same "swap in the garage" flow — the drone becomes a multi-tool, not just a blaster.

## Feel & feedback (kid-friendly juice)

- Big, colorful, exaggerated VFX; bouncy SFX; screen-friendly (no harsh flashes).
- Clear "you did it!" feedback on every hit (sparkles, happy sound), and celebratory, not gory, on bot power-down.
- Generous aim-assist (existing soft-lock) so little kids land hits and feel great.

## Implementation hooks

- Reuses `DroneWeapon` (projectile pooling) — projectiles become foam/water/bubble/paint/snowball/sparkle with playful materials + `Vfx`/`Sfx` variants.
- Enemy "defeat" swaps the death-pop for a **power-down** state (float down / deactivate / turn friendly) — a gentler `TargetHealth` death path for the kid tone.
- Mount is a visible child mesh on the drone, set by the garage's tool slot; progresses per tier.
- Weapon/tool stats (rate, "knockback," payload weight) live in the component system.

## Art notes (per tier mount)

Model a mount on each player tier (see `build_models.py`): T0 = taped foam popper (tan tube + red band); mid tiers = clip-on colorful blaster; T5 = flush glowing emitter. Keep it small so the quad still reads as the hero.

## Mount progression ladder — grounded reference (session 066 research)

Real drone-payload mounts get more advanced along a clear axis: **more enclosed, fewer visible fasteners/wires, tighter seams, palette shifts** (mismatched hobby colors → bold matte print → branded molded grey/white → matte tactical black + carbon). *Exposed/naked reads cheap; a sealed sphere/turret reads elite.* We keep each real mechanism's silhouette and swap the payload for a non-lethal toy.

| Tier | Toy reskin | Real-world basis | Mount & look cues |
|------|-----------|------------------|-------------------|
| **1** | taped **foam-popper** | crude DIY (Nerf-on-drone) | belly, off-center; bare colored servo + white horn, black zip-ties (cut ends), tape, dangling dart; mismatched bright colors |
| **2** | **balloon/snowball dropper** | DIY hook-latch dropper | belly hook with a bent-wire pin; payload swings below; one printed bracket = a notch tidier |
| **3** | **water/paint blaster** | maker 3D-printed | chin-mount printed cylinder/box, visible servo + nozzle, bold single-color matte print + silver screw dots, tucked wire |
| **4** | **grabber claw / rescue net** | maker gripper/net (delivery & rescue) | belly-centered 2–4-jaw geared claw (scalloped jaws) or folded net pod; matte print, exposed pivots |
| **5** | **bubble turret / spotlight / delivery magnet / camera** | commercial (DJI/Wing/Zipline) | flush-docked **sealed molded pod**, branded grey/white, round lens or conical emitter, thin winch line + hook/magnet; **no visible wires** |
| **6** | **sparkle-emitter gimbal "eye"** | advanced turret ball (Skydio/EO-IR) | chin/belly **enclosed sphere in a yoke**, matte tactical shell, one glossy tinted lens + emitter aperture, gold-connector/gasket accents = top tier |

**Non-combat tools across tiers** (delivery/rescue/photo missions): **grabber** T4 printed claw → T5 clean molded gripper · **net** T4 folded pod → T6 sealed cone canister · **spotlight** T3 printed LED pod → T5 sealed round-lens searchlight · **delivery** T5 thin winch + hook/magnet (Wing-style perforated "pill" hook reads great for kids) · **camera** T3 printed 1-axis tilt → T5 molded gimbal ball → T6 multi-lens "eye".

_Cross-cutting modeling rule: each rung up = more enclosed + fewer visible fasteners + tighter seams + the palette shift above. Sources compiled session 066 (Instructables/Hackaday DIY droppers & Nerf builds; Cults3D/Thingiverse printed release & claw mechanisms; DJI Agras/Zenmuse, Wing/Zipline/Amazon delivery; Skydio X10 / EO-IR turrets)._
