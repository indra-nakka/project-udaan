# Engineering Rubric: Execution Modes & Stop Search Logic

This document governs how tasks are explored and executed. The AI agent must evaluate the nature of the current assignment and explicitly choose either **Breadth-First Search (BFS)** or **Depth-First Search (DFS)** execution mode.

---

## 🌐 1. Breadth-First Search (BFS) Mode
*Use this for discovery, high-level structural design, workspace initialization, and multi-system integration.*

### Core Objective
Scan horizontally across systems to map dependencies, build basic placeholders, and unblock execution loops without getting bogged down in implementation details.

### Strict Constraints & Stop Logic
- **Time-Box Limit:** Max 10 minutes of autonomous scanning or file reading per macro-instruction.
- **File Access Cap:** Maximum of **5 files** may be opened sequentially during initial discovery.
- **🚨 Hard Stop Condition:** If the objective requires opening a 6th file to understand a dependency, the agent **MUST STOP**. 
  - Do not keep guessing.
  - Summarize what was learned across the 5 files.
  - Present the known dependency graph to the user and request explicit clearance or clarity before opening more files.

---

## 🕳️ 2. Depth-First Search (DFS) Mode
*Use this for isolated debugging, implementing complex single-file algorithms, multiplayer synchronization loops, and exact code generation.*

### Core Objective
Dive deep into a single specific component or system pipeline to resolve errors or build an isolated feature to 100% technical completion.

### Strict Constraints & Stop Logic
- **Scope Isolation:** Max **2 closely coupled files** (e.g., `TargetHealth.cs` and `ScrapItem.cs`) may be edited in a single execution loop.
- **Anti-Sprawl Rule:** If fixing a bug in File A requires changing code in File B, which then requires changing code in File C, the agent **MUST STOP**. This indicates architectural sprawl.
- **Uncertainty Gate:** If a logical path is ambiguous or relies on a missing dependency, the agent is strictly forbidden from writing "placeholder assumptions." It must log the unknown variables in `context/assumptions.md` with `confidence: low` and return control to the user.

---

## 🛠️ Verification Checklist (Definition of Done)
Before marking any task as complete under either mode, the agent must verify:
1. All changes are logged line-by-line in the active session file.
2. A single tracking line has been appended to `changelog.md`.
3. No breaking changes were introduced to files listed under `architecture/invariants.md`.
