# Project: Udaan (Mobile Arena PvP)

## Read First, Every Session
- [[architecture/overview]] — 1-page system map (Unity NGO, URP, Mobile Touch).
- [[context/conventions]] — Code style, Ghibli/Re-Volt visual rules.
- [[tasks/active]] — Current sprint focus.
- [[sessions/_index]] — Scan the 1-line summaries of the last 5 sessions only.

## Read On-Demand, By Trigger
- Modifying Multiplayer/Netcode? → [[architecture/components/networking]]
- Building UI/Economy? → [[architecture/components/economy]]
- Debugging a crash? → [[rubrics/debugging]], [[context/gotchas]]
- Designing a new system? → [[rubrics/bfs-dfs]], [[decisions/_index]]
- Before proposing any architectural change? → Check [[architecture/invariants]] first.

## Write Every Session (Post-Session Hook)
- Create a new session log in `sessions/YYYY-MM-DD-HHMM.md` using the template [[rubrics/session-template]].
- Append exactly one summary line to `changelog.md` per substantive change. Do not skip this[cite: 3].
- Update `tasks/active.md` (Move completed items to `tasks/done.md`, do not delete).
- Any new architectural choices must become a new ADR in `decisions/`.

## Rules of the Road (Strict Enforcement)
- **Append-Only:** Never delete entries from `changelog.md` or `decisions/`[cite: 3]. Mark old entries with `status: superseded`[cite: 3].
- **Citations:** Cite specific `file:line` numbers when referencing code in your logs, never paraphrase[cite: 3].
- **Stop Search Logic:** Start unknown tasks with 10 minutes of BFS scanning. If you open >5 files looking for an answer, STOP. Switch to BFS or ask the user for guidance[cite: 3].
- **Uncertainty Logging:** If you don't know something, log it in `context/assumptions.md` with `confidence: low`[cite: 3]. Do not guess or hallucinate code[cite: 3].
- **Verification:** Before claiming a feature works, run the verification checklist found in [[rubrics/verification]][cite: 3].
