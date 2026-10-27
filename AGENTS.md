# Agent Rules — Petrophysical Log Visualizer

## Mandatory Working Rules (5 rules — apply to every task, no exceptions)
1. **Same failure twice → change method** — never attempt a third try with the same approach hoping for luck. Change the method/plan explicitly.
2. **Every number in a report must come from a real run in that same round** — never quote numbers from memory/assumptions/a previous session. If not actually run: run it now, or clearly label it "not yet verified".
3. **Same tool failing 10 times in one task → stop and report** — what was tried, where it's stuck, what's blocked. Never grind silently.
4. **Stay in scope** — any task outside the IN list of PROJECT.md must be approved by the project owner first. Never guess and act.
5. **Never commit on your own — always ask** — propose (files + commit message) and wait for the owner's "ok commit" (one reply = one commit). Split bigger work into small tasks: do ONE task → report (files changed + real run output) → wait for ok before the next — never deliver one huge unreviewed batch (no black box).

## Git workflow (assignment mode — a reviewer should read the commit log as a story)
- **1 commit = 1 purpose**, explainable in a single sentence — if you need the word "and", it's still too big: split it.
- Before proposing any commit: **run build/tests until they pass** — every commit must be in a working state (a reviewer can check out any point in history).
- Propose (files + message) → wait for "ok commit" per rule 5 → `git add <named files>` — **never `git add .`** (it mixes several concerns into one blob).
- Message format: `<type>: <what>` + a body explaining why — type is feat/fix/test/docs/refactor/chore. Task IDs (T#) are optional and only used when SSOT.md is committed (see below).
- **Commits carry the owner's name only** — no `Co-Authored-By` or other AI trailers. AI usage is disclosed in the README (or wherever the project requires), not in commit metadata.
- Never: floating "WIP"/"fix"/"update" messages, one commit mixing several concerns, committing a broken build, finishing a task without updating SSOT.md.
- **SSOT.md in git — decide once per project (in Project Rules):** tracked (default) or local-only (git-ignored) when the commit log itself is the deliverable and status edits would add noise to every diff.

## Decision policy — when to ask / when to decide
- **Always ask first (batched into one round — never trickling questions):** choosing/changing the tech stack, adding any new library/dependency however small, changing data structures/schema, deploy target, out-of-scope work — present options with a recommendation + reason, then wait. Never start silently.
- **Decide yourself, but report:** micro-details inside an already-chosen frame (colors/chart type, naming, file layout) — test: reversible in minutes **and** adds no new dependency.
- **If Techstack in PROJECT.md still shows `<...>`** = the owner hasn't decided → ask, never guess on their behalf.
- Tasks in the Progress table already carry an AC → don't re-ask about goals; ask only about "how" when it's undecided.
- **Never create new .md files beyond the 4 scaffold files** — if the work genuinely needs another document, propose it first (filename + what it holds) and wait for approval; such documents live in `docs/` only. No floating scratch notes in the root or the working tree. Pre-approved for this project: `docs/DATA_NOTES.md`.

## Before starting any task
1. Read `SSOT.md` — current status + roadmap (never guess status from memory). In this project SSOT.md is local-only (see Project Rules); if it is absent, the plan is the Development plan in `PROJECT.md`
2. Check scope in `PROJECT.md` (enforced by rule 4)

## Finishing a task (final step before reporting done)
- Update `SSOT.md`: new status + roadmap + one history line + date — no update = task not done

## Router — when to read what
- Understanding the system / tech rationale / the plan → `PROJECT.md`
- What the data actually looks like and which rule came from which evidence → `docs/DATA_NOTES.md`
- Build / run / design / conditioning decisions / limitations / AI usage → `README.md`
- Current status (owner's machine only) → `SSOT.md`

## Project Rules (project-specific — the generic rules above stay unchanged)
From the brief:
- C# only. **The LAS reader is hand-written** — no library that parses LAS. Plotting/charting and general-purpose libraries are allowed.
- Never tune to the sample files: every rule must be a general rule taken from the LAS spec or from evidence in `docs/DATA_NOTES.md`, and must work on files we have never seen.
- A bad file never crashes the program: problems become issues in the load report, never unhandled exceptions.
- The owner must be able to explain and change every line → deliver code in pieces of ~100–150 lines; prefer the simpler version.

Code:
- Column storage: depth = `double[]`, each curve = `float[]`. No object per row, no per-row `Dictionary`.
- Hot path (reading `~A`): no LINQ, no `string.Split` per line; parse numbers with `CultureInfo.InvariantCulture`.
- `PetrophysicalLogVisualizer.Core` references no other project and no UI type.
- **No silent edits:** every conditioning change calls `report.Add(...)`; no value is changed or dropped without an entry.
- Missing values become `NaN` — never delete a whole row because one curve is missing; never interpolate.
- Curve aliases + accepted units live in `CurveRules.cs` only — never hardcode a mnemonic elsewhere.
- Every conditioning rule and every parser fix comes with a test; tests include `NaN` and out-of-scale values like the real data.

Scope guard (ask before adding anything): horizontal zoom, crossover shading, extra tracks, resampling and LAS 3.0 beyond best-effort are OUT. Before any new work ask: "Is it in a MUST, or does it improve resource use?" If neither → defer.

Verification:
- "Done" states how it was verified: unit test / all files in `data/` / tried in the app. Code-only tests do not count as UI verification.
- UI behaviour is committed only after the owner has tried it by hand.

Git (exceptions to the generic workflow):
- **`SSOT.md` is local-only (git-ignored)** — the commit log is part of the deliverable, so status edits stay out of the diffs. The plan reviewers see is the Development plan in `PROJECT.md`.
- `AGENTS.md` and `PROJECT.md` are committed once at the start; change them only when a decision actually changes (its own `docs:` commit).
- `data/`, `output/`, `bin/`, `obj/`, `.vs/`, `SSOT.md` are git-ignored. Generated plots + reports enter git **once**, in a single final commit under `deliverables/`.
- Author = the owner's git identity only (check `git config user.name` / `user.email` before the first commit).
