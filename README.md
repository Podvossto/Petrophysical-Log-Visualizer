# Petrophysical Log Visualizer — LAS well-log loader, conditioner and plotter

Loads every LAS file in a folder, cleans the data for interpretation, reports every change, and plots a three-track log per well (C#, .NET Framework 4.8, WinForms).

## 1. Build and run
<!-- fill in Phase G -->
- Requirements: Windows 10/11, Visual Studio 2026 with the ".NET desktop development" workload (.NET Framework 4.8 targeting pack)
- Build: open `PetrophysicalLogVisualizer.sln` → Build Solution
- Run (window): F5 → **Open folder** → select the LAS folder
- Run (headless): `PetrophysicalLogVisualizer.App.exe --batch <inFolder> <outFolder>`
- Tests: Test Explorer → Run All

## 2. Design
<!-- pipeline + project split + data model + why; fill as decisions are made -->
`File Discovery → LAS Parsing → Validation & Inspection → Data Conditioning → Data Model → Plot Rendering → Conditioning Report → Export`

## 3. Data-conditioning decisions
<!-- one short paragraph per rule: what it does, why, evidence (from docs/DATA_NOTES.md) -->
- Missing values:
- Depth order and sampling:
- Units:
- Curve names:

## 4. Performance and resource use
<!-- numbers from the final real run only: files, MB, total time, MB/s, peak memory, machine -->

## 5. Known limitations
<!-- what is deliberately not handled, and what would change for thousands of wells -->

## 6. How I used AI
<!-- what AI did, what I did, how every piece was verified (tests / real files / app) -->

## Project documents
| File | What's inside |
|---|---|
| **PROJECT.md** | scope, requirements + acceptance criteria, architecture, development plan |
| **docs/DATA_NOTES.md** | what the real files look like and the rule each finding led to |
| **AGENTS.md** | the working rules I gave the AI assistant |
