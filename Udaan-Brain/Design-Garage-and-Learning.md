# Udaan — Garage, Components & Learning (design)

_Session 065. The defining pivot: Udaan is a **build-and-play drone game for ages 5–10** (learn + play). The **garage is the game's brain and its teacher** — every visible part is a real drone concept with a trade-off a child can see and feel. NFS-style customization meets Kerbal-lite honesty in a kid-friendly wrapper._

## Audience & tone

- **Ages 5–10, learn + play.** Stealth STEM: kids learn how drones work by *doing*, never via lectures or quizzes.
- **Forgiving & encouraging.** No "death" — a crash is *"oops, let's rebuild!"* Every screen is warm, bright, and safe.
- **Dead-simple controls.** Big tap targets, strong aim-assist (already built), short sessions, generous help.
- **Safe content.** No violence, no scary themes, positive messaging. No real-money pressure (earn parts by playing); privacy-conscious (COPPA/age-appropriate) — flag for production.
- A friendly **guide character** ("build-buddy") gives short, cheerful nudges in kid words. Skippable, never naggy.

## The core loop

**Play a mission → earn parts/bolts → go to the garage → upgrade (and *learn* from the trade-off) → fly better → play the next mission.** The garage is the hub between missions (NFS structure).

## Modular components (the learning engine)

The drone is a **composition of swappable parts**. Each slot has tiers/variants, a **visible mesh**, **stats**, and teaches **one real concept via a trade-off**:

| Slot | Upgrading it… | …costs you | Concept taught | You SEE it | You FEEL it |
|------|---------------|-----------|----------------|-----------|-------------|
| **Frame / chassis** | tougher, more mount slots | heavier, slower | structure & weight | bulkier body | sluggish if too heavy |
| **Props** | bigger/steeper = more lift | slower spin-up, noisier | thrust / aerodynamics | larger blades | climbs/hovers better |
| **Motors** | more power | drains battery faster, heat | energy conversion | bigger bells | zippier but shorter flights |
| **Battery** | longer flight time | heavier (sags, tilts) | energy storage trade-off | chunkier pack | flies longer but wobblier |
| **Wiring / flight computer** | stability & features (auto-level, hover-hold) | — (reliability tiers) | control systems | neater electronics | steadier, easier to fly |
| **Remote / controller** | range & precision | — | signal & control | fancier handset | tighter response, farther range |
| **Tool / blaster mount** | the playful weapon or tool (see `Design-Weapons-and-Tools.md`) | payload weight | payload vs performance | mounted gadget | trade fun for agility |

**Trade-offs = the lesson.** Every upgrade helps one thing and costs another, so kids build *intuition*, not memorized facts. A simple 5-bar readout — **Flight time · Lift · Speed · Toughness · Control** — updates live as they swap parts, so they *see* the effect before a one-tap **Test Flight** lets them *feel* it.

## Garage UX (NFS, kid-simplified)

- Drone on a **turntable**, rotatable with a finger. Bright workshop, tactile.
- **Tap a part → see variants → swap.** Big cards, drag-and-drop, instant visual change on the model.
- Live **stat bars** animate on change; the guide pops a one-line "why."
- **Test Flight** button: quick free-fly to feel the new build before committing.
- Earn **bolts/parts** from missions; parts unlock by progress, not payment.

## Progression & economy (kid-safe)

- Currency = **bolts** (earned by playing). Parts unlock via mission milestones + bolts.
- The **drone tier ladder** (T0 junk → T5 advanced, see `Art-Direction.md`) is the visible sum of upgraded parts — the progress bar. A kid literally rebuilds a garbage-bin quad into a sleek machine, part by part.
- No lootboxes, no pay-to-win, no timers. (If ever monetized: cosmetic part-skins + a parent-gated pack, per market research.)

## Physics: real-ish but forgiving

- Trade-offs must be **perceptible** (a too-heavy build is visibly sluggish) so learning is felt — but **never frustrating**. Crashes bounce/"oops," then rebuild. Aim-assist and stability options scale with the flight-computer part (older kids can turn assists down).

## Open questions

- Exact bolt economy / pacing per age band.
- How many variants per slot at launch (start ~3 per slot?).
- Guide character design + voice (text-only first; VO deferred with audio).
- Which real concepts to name explicitly vs leave purely felt (lean felt for 5–7, more named for 8–10).
