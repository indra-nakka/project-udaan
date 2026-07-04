# ✅ Verification — Definition of Done

**Role:** The single canonical checklist for "is this task actually done?" Referenced by [[bfs-dfs]], [[session-template]], and the Rules of the Road in [[UDAAN_ROUTER]]. Do not duplicate this list elsewhere — link here.

## Every task
- [ ] Code **compiles** with no Editor errors or new warnings.
- [ ] **Manual Play Mode** check passes for the changed behaviour (if runtime-relevant).
- [ ] No rule in [[invariants]] was violated (or an ADR supersedes it).
- [ ] Any new/changed `NetworkVariable` or Core variable is reflected in [[glossary]].
- [ ] Actions logged in the session file; **one line appended to [[changelog]]**.
- [ ] [[tasks/active]] updated (completed items moved to [[tasks/done]]).

## If the change touches multiplayer
- [ ] Tested with **host + at least one client** (not just host-alone).
- [ ] Server-authority guards present (`if (!IsServer) return;`) on state mutation.
- [ ] No `Instantiate`/`Destroy` on networked objects — `Spawn`/`Despawn` used.
- [ ] No rubber-banding / desync observed in a quick two-instance test.

## If the change touches flight, combat, or feel
- [ ] Tuning values sourced from ScriptableObject data, not hardcoded.
- [ ] Quick "does it feel right?" pass; note result in the session log's fun journal.

## If the change touches mobile / performance
- [ ] Object pooling used for any spawned projectile/VFX.
- [ ] Sanity-checked on a **real device** (or flagged in [[assumptions]] if not yet possible).

## If something was uncertain
- [ ] Open assumptions recorded in [[assumptions]] with a confidence level.
- [ ] New footgun discovered? Added to [[gotchas]].
