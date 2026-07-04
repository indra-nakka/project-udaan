---
type: router
status: active
updated: 2026-06-30
tags: [moc, index, start-here]
---

# Project: Udaan (Mobile Arena PvP)

> **Vault note:** files use two naming styles — plain folders (`architecture/`, `rubrics/`, `context/`, `tasks/`) and emoji-prefixed display files (`🏗️ Architecture`, `💰 Economy_Balance`, …). Wikilinks below use **basename only**, which Obsidian resolves regardless of folder or emoji prefix.

## 🧭 Strategy & Planning (read when scoping or prioritizing)
- [[📋 Master Plan]] — at-a-glance milestone roadmap (M1–M6, risk-sequenced).
- [[UDAAN_MASTER_PROJECT_PLAN]] — full execution plan: work breakdown, phasing, timelines, do/avoid.
- [[UDAAN_Analysis_and_Competition]] — honest assessment, competition analysis, brainstorming (the "why").

## Read First, Every Session
- [[🏗️ Architecture]] — 1-page system map (Unity NGO, URP, Mobile Touch).
- [[🎨 Aesthetic Canvas]] — visual rules (Ghibli/Re-Volt), UI/HUD guidelines.
- [[conventions]] — code & commit style.
- [[tasks/active]] — current sprint focus.
- Latest ~5 files in `🪵 Session Logs/` (by date) — scan the 1-line summaries only.

## Read On-Demand, By Trigger
- Modifying Multiplayer/Netcode? → [[Unity_NGO_Patterns]], [[DEC-001_Netcode_NGO]], **[[DEC-003_Network_Stack_Reevaluation]]** (stay/switch)
- Touching gameplay code? → [[code-review]] (open bugs & tech-debt)
- Building UI/Economy? → [[💰 Economy_Balance]], and Phase 1.6 of [[UDAAN_MASTER_PROJECT_PLAN]]
- Mobile performance / optimization? → [[Mobile_Optimization]]
- Backend / API work? → [[📋 API Contract Sandbox]]
- Input / controls / flight feel? → [[controller-map]], [[DEC-002_Classic_Input_Manager]]
- Designing a new system? → [[bfs-dfs]], then the ADRs in `⚖️ Tradeoffs & Decisions/`
- Variable Mapping & Network Scope Registry → [[glossary]]
- Before proposing any architectural change? → [[invariants]] (don't break these)
- Debugging a crash? → [[debugging]], then check [[gotchas]]
- Unsure / acting on an unproven belief? → log it in [[assumptions]]

## Write Every Session (Post-Session Hook)
- Create a new session log in `🪵 Session Logs/YYYY-MM-DD-HHMM.md` using the template [[session-template]].
- Append exactly one summary line to [[changelog]] per substantive change. Do not skip this.
- Update [[tasks/active]] (move completed items to [[tasks/done]], do not delete).
- Any new architectural choice must become a new ADR (`DEC-00N_Name.md`) in `⚖️ Tradeoffs & Decisions/`.
- Any new/changed Core or NetworkVariable → update [[glossary]] the same session.
- Any scope or priority change → reflect it in [[📋 Master Plan]].

## Rules of the Road (Strict Enforcement)
- **Append-Only:** Never delete entries from [[changelog]] or the `⚖️ Tradeoffs & Decisions/` folder. Mark old entries `status: superseded`.
- **Citations:** Cite specific `file:line` numbers when referencing code in your logs, never paraphrase.
- **Stop Search Logic:** Start unknown tasks with ~10 minutes of BFS scanning. If you open >5 files looking for an answer, STOP — switch to BFS or ask the user. (See [[bfs-dfs]].)
- **Uncertainty Logging:** If you don't know something, log it in [[assumptions]] with `confidence: low`. Do not guess or hallucinate code.
- **Verification:** Before claiming a feature works, run the checklist in [[verification]].
- **Glossary discipline:** BEFORE implementing or modifying any script variable, cross-reference [[glossary]]; AFTER creating/modifying/deleting any Core or NetworkVariable, update its Type/Scope/Authority row in [[glossary]].

## 🗂️ File Index (what exists today)
- **Strategy:** `📋 Master Plan`, `UDAAN_MASTER_PROJECT_PLAN`, `UDAAN_Analysis_and_Competition`
- **Architecture:** `🏗️ Architecture`, `architecture/glossary`, `architecture/controller-map`, `architecture/invariants`, `📋 API Contract Sandbox`
- **Design:** `🎨 Aesthetic Canvas`, `💰 Economy_Balance`
- **Context:** `context/conventions`, `context/gotchas`, `context/assumptions`, `context/code-review`
- **Rubrics:** `rubrics/bfs-dfs`, `rubrics/verification`, `rubrics/debugging`, `rubrics/session-template`, `🧠 Technical Rubrics/Unity_NGO_Patterns`, `🧠 Technical Rubrics/Mobile_Optimization`
- **Decisions (ADRs):** `⚖️ Tradeoffs & Decisions/DEC-001_Netcode_NGO`, `DEC-002_Classic_Input_Manager`, `DEC-003_Network_Stack_Reevaluation`
- **Tasks / Log:** `tasks/active`, `tasks/done`, `changelog`, `🪵 Session Logs/`
