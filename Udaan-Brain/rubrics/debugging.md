# 🐞 Debugging Playbook

**Role:** The *process* to follow when something is broken — so debugging stays disciplined (DFS) instead of sprawling. Pairs with [[gotchas]] (known traps) and [[bfs-dfs]] (execution modes).

## 1. Orient (≤5 min)
- Reproduce reliably. Write down the exact steps and the expected vs actual result.
- Check **[[gotchas]] first** — this bug may already be catalogued.
- Skim the last 1–2 [[changelog]] lines and the latest `🪵 Session Logs/` entry: *what changed most recently?* New bugs usually live in new diffs.

## 2. Isolate (DFS mode)
- Enter **DFS** ([[bfs-dfs]]): max 2 closely-coupled files per loop.
- Binary-search the cause: disable/log halves of the suspect path. Add greppable `Debug.Log($"...")` markers; remove them after.
- For **netcode** bugs: run **host + client** and confirm which side is wrong. Ask: is this state server- or client-authoritative? Is the guard (`IsServer`/`IsOwner`) correct? Was it `Spawn`/`Despawn`'d?
- For **physics/flight**: pause and inspect `Rigidbody` (velocity, drag, isKinematic), and whether hover override is stuck.
- For **mobile-only** bugs: profile on device; suspect pooling, memory, or thermal throttling.

## 3. Anti-Sprawl Stop
- If fixing File A forces changes in B, which forces C → **STOP**. That's architectural sprawl; surface it to the user and consider an ADR, don't grind. (Rule from [[bfs-dfs]].)

## 4. Resolve & Record
- Fix the root cause, not the symptom.
- Run the [[verification]] checklist.
- **Add the lesson to [[gotchas]]** (Symptom → Cause → Fix) so it's a one-time bug.
- If the fix relied on an unproven belief, log it in [[assumptions]].

## Unity quick-reference
- **Profiler / Frame Debugger** for perf and draw calls.
- **Play Mode** + `Debug.Break()` to freeze on a condition.
- Console: enable **"Error Pause"** and full stack traces.
- Network desync: log the same variable on server and owner each tick; compare.
