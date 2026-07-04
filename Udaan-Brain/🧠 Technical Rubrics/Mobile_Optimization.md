# 🧠 Mobile Optimization

**Role:** Mobile performance rules of thumb. Hard requirements are in [[invariants]]; this is the "how" reference. Target: **60fps on a 3-year-old mid-range Android.**

## Rendering
- **URP**, mobile-tuned (the `Mobile_RPAsset` / `Mobile_Renderer`). No post-FX that aren't mobile-validated.
- Bake lighting for static meshes; avoid realtime shadows where possible.
- Reduce **draw calls**: atlas textures, share materials, use GPU instancing, add LODs.
- Keep overdraw low (watch transparent VFX stacking).

## Memory & GC
- **Minimize garbage collection during gameplay** — cache references, avoid per-frame allocations (no LINQ / `new` / string concat in `Update`).
- **Object pooling is mandatory** for projectiles, VFX, audio one-shots ([[invariants]], [[gotchas]]).

## UI
- Keep Canvas redraws minimal: split static vs dynamic canvases so a changing element doesn't dirty the whole canvas.
- TextMeshPro for text; avoid frequent layout rebuilds.

## Network (mobile data)
- Budget bandwidth; send deltas, sensible tick rate, quantize transform data.

## Validate on device
- Profile with the **Unity Profiler on real hardware** — the Editor lies about thermal, memory, and GC.
- Run **thermal-throttle tests** (sustained ~10+ min) and a battery-drain pass before declaring perf "done".
