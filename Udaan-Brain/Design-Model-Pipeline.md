# Udaan — Blender → Unity Model Pipeline (standard)

_Session 066. We'll import a LOT of models, so this locks the translation so every model comes in **upright at the right scale** with no per-axis fiddling. If you ever find yourself typing a non-uniform scale like `(11.6, 217, 4.2)`, something below is misconfigured — that number is the symptom of an axis + unit mismatch, not a real size._

## Why it broke

- **Axes differ:** Blender is **Z-up**, Unity is **Y-up**. A raw FBX imports rotated (often −90° X), so a model "lies down" or faces the wrong way.
- **Units/scale:** if the exporter or importer disagree on unit scale, the model comes in 100× too big/small.
- When both are wrong, you end up **stretching per-axis** to force it into shape — which is what produced the `(11.6, 217, 4.2)` scale. One uniform slider can't undo a rotation, hence the pain.

## The fix (two sides, both automated)

**1. Export (Blender) — `build_models.py`** now exports with:
- `bake_space_transform=True` + `axis_forward='-Z'`, `axis_up='Y'` — bakes Blender's Z-up into Unity's Y-up **into the mesh**, so it lands upright.
- `apply_unit_scale=True`, `apply_scale_options='FBX_SCALE_ALL'` — 1 Blender metre = 1 Unity metre.
- `transform_apply(rotation, scale)` before export — the object leaves with a clean identity transform.

**2. Import (Unity) — `Assets/Editor/UdaanModelPostprocessor.cs`** runs automatically on any FBX under `Assets/Art/Models/` and sets:
- `useFileScale = true`, `globalScale = 1` — trust the FBX's real units.
- `bakeAxisConversion = **false**` — orientation is **already baked by the export**, so we do NOT bake again (doing both re-rotates the model). If a model ever imports lying-down/rotated, flip this one flag to `true`.
- no cameras/lights, standard materials, imported normals.

**Result:** drop a model in `Assets/Art/Models/`, and it imports **upright at 1:1**. Skinning then needs only a **single uniform scale**, close to 1.

> Untested-in-this-session caveat: the exact axis handling (export-bake vs import-bake) can be finicky per Unity version. If the first reimport looks rotated, the one-line fix is `bakeAxisConversion = true` in the postprocessor (and it'll be right from then on). Either way it's **one setting, once — never a non-uniform scale.**

## The standard (follow for every model)

1. **Build in metres at real size** in Blender (drone ≈ 1–2 m, boss ≈ 4 m). Don't build tiny/huge.
2. **One joined mesh**, origin centred (`build_models.py` does `join` + `origin_set BOUNDS`).
3. **Nose/forward = +Y in Blender** (our convention). After conversion the nose maps to Unity **+Z (forward)**. If a specific model's nose still faces wrong, that's a **yaw nudge**, not a scale hack (see below).
4. **Export** via `build-models.bat` → `Assets/Art/Models/*.fbx`.
5. **Reimport** so the postprocessor runs (re-running the bat reimports; or right-click ▸ Reimport). ⚠️ **Existing models imported before this change must be reimported once.**
6. **Use it:** uniform scale only. Player skin via "Udaan ▸ Skin Player Drone"; enemy/ally/boss via `FactionVisuals` (DemoFlow inspector).

## Orientation nudges (when needed)

Up-axis is handled automatically. A model's **nose facing the wrong way** (yaw) is a per-model rotation, exposed so you never re-export for it:
- **Faction skins:** `DemoFlow` inspector → `enemyEuler / allyEuler / bossEuler` (e.g. `(0,180,0)` to spin the nose around).
- **Player prefab:** rotate the `Skin` child in the prefab.

## Size convention

- Because import is now 1:1, a model built at ~1.5 m reads at ~1.5 m in Unity. The drone skin scale sits near **0.3–0.6**; the boss a bit larger.
- Note: `FactionVisuals.BossScale` is **relative to the boss's 2.2× entity scale**, so it looks ~2.2× its raw number — start ~0.3.

## For the future (many models)

The rule is simply: **build at metre scale in Blender with nose +Y → export → drop in `Art/Models` → it's upright and 1:1.** No manual import settings, no non-uniform scale. When we bring in authored/city models, they follow the same path (and the postprocessor already covers the whole folder).
