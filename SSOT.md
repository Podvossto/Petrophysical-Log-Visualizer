# Petrophysical Log Visualizer — SSOT.md (Single Source of Truth)

> Update this file as the FINAL step of every task — a stale file = task not done
> If this file contradicts reality → trust reality and fix this file immediately
> Last updated: 2026-10-28 (submitted)

## Current status (cap 10 lines)
- All tasks T1–T18 done and pushed (origin/main = 453c891). 157 tests pass. The owner sent the submission to help@petrores.net on 2026-10-28 (deadline 20:00 ICT)
- Left: nothing. If the reviewers report a problem, fix it as a new task
- Data: 203 LAS files received (brief says 50, 192 different + 11 byte-identical copies) → every file in `data/` is processed; plots and reports for all 203 are delivered
- Last full runs (Release, 203 files, 1019.4 MB): `--batch` 28.6–43.0 s (23.7–35.6 MB/s), peak working set 589–796 MB; largest file alone (116.3 MB) 2.3 s and 754 MB (6.5x its size); window scan 24–26 s, 144 OK · 59 WARN · 0 FAILED
- Stack decided: net48 · WinForms · ScottPlot 4.1 · xUnit (see PROJECT.md)
- Plan: 13 steps in PROJECT.md → Development plan; each task below names its step

## Roadmap / Progress
> Status flow: TODO → DOING → REVIEW (waiting for ok commit) → DONE, or BLOCKED (state the reason)

| ID | Step | Task | AC (done when) | Feature | Status |
|---|---|---|---|---|---|
| T1 | 1, 3 | Scope + rules docs | PROJECT.md, AGENTS.md, README skeleton committed | — | DONE (18726e4) |
| T2 | 2 | Data exploration | `docs/DATA_NOTES.md`: observed facts + rule per finding, from files opened by hand | — | DONE (7eb345b, D-01..D-24 by later commits) |
| T3 | 4 | Solution skeleton | 3 projects, refs Core←App/Tests, ScottPlot in App only, Prefer32Bit off; build 0 errors | — | DONE (b7b46fd) |
| T4 | 5 | Model + header parser | Tiny LAS string test: NULL, STRT/STOP/STEP, curves + units parsed | F1 | DONE (5ad6132) |
| T5 | 5 | Data parser | Tests: space, comma, tab, WRAP YES, bad token → NaN, column count mismatch → warning | F1 | DONE (6a7a6ee; wrap detection c35e784; # lines 8653341) |
| T6 | 5 | Folder loader + load report | All files in `data/`: no crash; `load-report.csv` 1 row/file | F1 | DONE (adc2644) |
| T7 | 6 | Missing values | Tests: -999.25 → NaN counted; NULL=0 not applied; real file checked | F2 | DONE (7f06cb4) |
| T8 | 6 | Depth order + sampling | Tests: descending, duplicates, wrong STEP | F3 | DONE (76f0a26) |
| T9 | 6 | Roles + units | Tests: alias → role; CFCF/CPS not NPHI; 25 % → 0.25; K/M3 → g/cc; M → ft | F4 F5 | DONE (d33edd1; fix 43038a9) |
| T10 | 7 | Conditioning report | CSV + summary.md over all files; 3 wells spot-checked by hand | F6 | DONE (594864c) |
| T11 | 8 | Implement Plotting | Multi-track plot from conditioned data; 5 wells checked by eye | F7 | DONE (ae7aa86) |
| T12 | 8 | PNG export | PNG for every loadable well | F7 | DONE (bdba5ae) |
| T13 | 9 | Batch mode | `--batch <in> <out>` writes PNGs + reports for every file, no window | F1 F6 F7 | DONE (9d11389, a188dab) |
| T14 | 10 | Implement UI (WinForms window) | Owner tried every button; UI responsive during load | F8 | DONE (4d31473) |
| T15 | 11 | Measure | Time, MB/s, peak working set from a real run on the final code | F9 | DONE (measured on 4d31473, numbers in README section 4) |
| T16 | 12 | End-to-end test | Input folder → export on all files + abnormal cases, checked against F1–F9 AC | F1–F9 | DONE (8d2db24, 7f579c7, 54a162b, b87a022) |
| T17 | 13 | README | build/run, design, decisions, performance, limitations, AI use | — | DONE (e012b4d) |
| T18 | 13 | Deliverables + fresh clone | `deliverables/` committed once; SSOT.md tracked; fresh clone builds + runs | — | DONE (e266cc7 deliverables, b8c2f0d SSOT.md tracked; fresh clone checked 2026-10-28) |

## Known issues (accepted for now)
- Memory: a file is read whole as text, then split into lines (754 MB peak for a 116 MB file, 6.5x); streaming parse would fix it but changes both parsers and many tests
- D-12 wrap detection looks only at the first row; a file whose first row is malformed but the rest wrapped would be missed (no such file seen)
- No real file lacks a NULL line, so that warning is covered by a unit test only
- `masmp`: 125 curve lines, 2 start with a dot (empty mnemonic, wrong unit); no test yet for dotted curve lines (see D-10)
- Skipped # lines have no report entry (open question, D-22)
- Plot shows the whole well in one image (about 10 ft per pixel for a 14,000 ft well) with fixed scales; values outside them run off the track and are not marked; in cased sections the curves are junk; a log track hides values <= 0 and says how many. Owner decided to keep it as is
- 4246135949_MAIN.las: its data columns probably do not follow the header order (DEN median 0.19 g/cc); the program only warns (D-23); the -999.2 marker (D-16) rests on one file
- CR-only line endings are reported FAILED, not read
- A file that loads but has none of the four curves has status OK; the window's Note column says `no curves` and the conditioning report says why (a warning in Core was deferred)
- Window: plot is a picture (no zoom), file is reloaded on every click, tried at normal DPI only
- (anything accepted here must also appear in README → Limitations)

## History (append-only, complete; no line cap)
- 2026-10-27: Restart planned; scaffold adapted; project renamed; plan changed to 13 steps
- 2026-10-27: T1 DONE (18726e4); T2 DONE (7eb345b)
- 2026-10-27: T3 DONE — b7b46fd skeleton (+ README .slnx fix b0abe1b), pushed
- 2026-10-27: T4 DONE — 5ad6132 header parser + tests
- 2026-10-27: T5 DONE — 6a7a6ee data parser; T5b DONE — c35e784 wrap detection (D-12)
- 2026-10-27: T6 DONE — adc2644 loader + load report; first real run 203 files: 54.5 s, 18.7 MB/s, peak 843 MB (Debug)
- 2026-10-28: T7 DONE — 7f06cb4 markers → NaN; 6th marker -9999.999 owner-approved after s_f729e898…las (9981 values)
- 2026-10-28: T5c DONE — 8653341 skip # lines in ~A; masmp issues 127 → 1; 61 tests; run: 157 OK / 46 WARN / 0 FAILED
- 2026-10-28: T7 + T5c pushed (adc2644..8653341); SSOT cleaned up (stale status, test counts, push state)
- 2026-10-28: DATA_NOTES draft — D-10 125 curve lines (2 dotted lines), D-22 # lines (masmp 128 -> 1 issues, depth was shifted before), D-16 final (28 files, 6 markers); corrected my earlier "127 -> 1" to same-code "128 -> 1"; not committed
- 2026-10-28: DATA_NOTES committed 049a84b (D-10, D-22, D-16 final); build + 61 tests re-run OK; not pushed
- 2026-10-28: roadmap re-split per owner: new T11 Implement Plotting (step 8) separate from T12 PNG export; later tasks renumbered T13-T18 (UI = T14, Measure = T15); T13 old name 'Window' was ambiguous
- 2026-10-28: 049a84b pushed; T8 REVIEW — DepthConditioning (reverse/sort/dedupe, real step vs header, gaps) wired into LasLoader; 72 tests pass; 203 files: 27 reversed, 1 sorted, 1 file with 200 dups, 2 STEP mismatches, 0 gaps
- 2026-10-28: T8 DONE — 76f0a26 depth order + sampling, pushed
- 2026-10-28: T9 REVIEW — CurveRules/CurveRoles/DepthUnits wired into LasLoader; 103 tests; 203 files: GR in 179, RES in 96, RHOB in 90, NPHI in 74 files; DATA_NOTES D-13/D-17..D-20 updated with the run (D-14 25 -> 27 still pending, owner to decide)
- 2026-10-28: T9 checks done: Deci / ft3-ft3 neutron values confirmed as fractions; the files without GR were all listed, HSGR + HCGR added -> 24 without GR; 103 tests pass
- 2026-10-28: T9 DONE — d33edd1 roles + units (+ DATA_NOTES D-13, D-14, D-15, D-17..D-20 figures), pushed
- 2026-10-28: T10 REVIEW — ConditioningReport.ToCsv/ToSummary; 203 files -> 3995 entries; 3 wells (4238336177MAIN, 4200343170_STATE_AQ_8_RUN2, 4238337564_..triplecmi) recounted from raw text and matched; reports are written by a throwaway tool until batch mode (T13)
- 2026-10-28: T10 adjusted — conditioning-report.csv got a level column (note/warn) and summary.md the NaN share; 203 files: 3880 note, 115 warn (48 files); 113 tests pass; reports rewritten in output/
- 2026-10-28: T10 DONE — 594864c conditioning report (csv with level column + summary.md), pushed
- 2026-10-28: T11 REVIEW — WellPlotModel (Core, 3 tracks, fixed scales, log10 resistivity, neutron mapped on the density axis) + WellPlotRenderer (App, ScottPlot, NaN gaps via OnNaN=Gap, YAxis.IsReverse); 9 model tests; 203 files rendered in memory with 0 exceptions (slowest 162 ms); 5 wells looked at by eye. Found by eye: 39399.LAS NPHI labelled % but fractions -> wrongly divided by 100; fixed with a median check (D-17), 1 file affected of 10
- 2026-10-28: T11 legend added (coloured line + curve name under each track title) after the owner could not tell density from neutron; 203 files re-rendered, see run output; owner decided to keep off-scale values as they are and list it in README limitations
- 2026-10-28: T9 fix committed 43038a9 (not pushed); owner chose to keep the plot as it is; T11 commit still waiting for ok commit
- 2026-10-28: T11 DONE — ae7aa86 plot model + renderer (legend, 3 tracks); with 43038a9 not yet pushed
- 2026-10-28: 43038a9 and ae7aa86 pushed
- 2026-10-28: T12 REVIEW — PlotExporter.Save (App) writes folder\{name}.png, never throws; Tests project now references App (project reference, no new library); 203 files -> 203 PNG (30.2 MB, all reopened at 1200x1500) in output/plots
- 2026-10-28: T12 DONE — bdba5ae PlotExporter, not pushed
- 2026-10-28: bdba5ae pushed
- 2026-10-28: T13 REVIEW — BatchRunner (--batch in out, one file at a time, LasData.ReleaseValues, reports + plots), App OutputType Exe so the console shows progress; real run on data/: 203 loaded, 203 PNG, 34.9 s, 29.2 MB/s, peak working set 966 MB (Debug, not the T15 measure); also rounded the median in the neutron warnings
- 2026-10-28: T13 DONE — 9d11389 median rounding, a188dab --batch mode, pushed
- 2026-10-28: T15 measured on the Release build; wrongly blamed a loader experiment for a slowdown (it was the machine); the experiment (read line by line) gave -3% memory and was reverted
- 2026-10-28: T15 re-run in more detail (no code change): batch 39.3-40.3 s x3 (25.4-26.0 MB/s), peak 736-804 MB; phases over 203 files: raw read 0.6 s, load+condition 23.5-28.8 s (35-43 MB/s, CPU bound, linear in size), draw+save 13.6-19.9 s (67-98 ms per well; one 7 s outlier once, not repeated, cause unknown); peak per file ~6.4-8.3x its size for files over 20 MB (116.3 MB -> 746 MB), 57 MB for a typical 2.8 MB file, 19 MB empty run. Two files hold identical data (4246135952_AQZO... and 4246135952AQZO...)
- 2026-10-28: byte-identical check over data/: 11 groups, 22 files, 1.5 s; not implemented yet
- 2026-10-28: T16 REVIEW — 20 abnormal files through the Release exe: no crash, every .las has a row + reason (6 FAILED, .txt and sub-folder skipped); 203 real files: F1-F7 structure checks pass (loaded 203, 0 not ascending, 0 equal depths, 0 length mismatches, 0 Infinity, 0 markers left, depth all ft); F8 window not built, F9 needs README; 9 implausible chosen curves found (above)
- 2026-10-28: T16 DONE — 8d2db24 test for broken files, 7f579c7 -999.2 marker (D-16), 54a162b warning for implausible gamma/density (D-23); 141 tests; duplicates (11 pairs) and CR-only line endings still open/noted; not pushed
- 2026-10-28: b87a022 duplicates flagged (11 byte-identical copies, D-24); 3 T16 commits + b87a022 pushed; owner sent a UI reference image for T14, evaluation only
- 2026-10-28: T17 started — fresh clone test: VS 2022 MSBuild 17.14 reads the .slnx but neither msbuild -restore nor devenv /Build restores packages.config packages, so a clean clone does not build from the command line; proposed migrating App and Tests to PackageReference; README draft written (not committed); nuget.exe download was blocked by the permission check and not retried
- 2026-10-28: App and Tests moved from packages.config to PackageReference; clean copy (no bin/obj/packages) builds with ONE command msbuild slnx -restore on VS 2022 17.14 (with VisualStudioVersion=17.0; my shell had 18.0 left from VS 2026, which made 2022 look for NuGet 18.0 targets that do not exist) and VS 2026: 0 warnings, 148 tests pass, exe runs, 203 files 30.1 s; README sections 1 and 6 written from the owner's account (Opus 5.5 plans docs, Sonnet 5.5 medium implements, Opus 5.5 for testing); one README sentence about the owner's code walkthrough is marked CONFIRM
- 2026-10-28: 4e9b958 build: PackageReference migration committed (not pushed). README rewritten with tables like PROJECT.md and the owner's background (first C# project, no petrophysics, read up on basic terms and how a log is read, compared stack options); AI table; one sentence still marked CONFIRM; README NOT committed yet. Owner wants T14 (window) before committing README and before T18
- 2026-10-28: 4e9b958 pushed (origin/main = 4e9b958); README still uncommitted on purpose
- 2026-10-28: T14 plan written into this file (handoff section); owner starts it in a new Claude Code session (Sonnet 5.5, medium)
- 2026-10-28: T14a REVIEW — WellRow.cs + MainForm.cs (Open Folder, well list, background load one file at a time, ReleaseValues, progress, status line); Program.Main: no args -> window, --batch untouched; build 0 errors, 148 tests pass; NOT yet tried by hand, not committed
- 2026-10-28: owner decided T14 is ONE commit (one purpose), AGENTS.md unchanged. T14b-d written: click well -> reload + plot on background thread; right panel (warnings first, then changes/curve choices, FAILED shows issues); Export all (BatchRunner + ProgressWriter), search, only-problems filter; build 0 errors, 148 tests pass; NOT tried by hand, not committed
- 2026-10-28: T14 layout pass (owner feedback): plot now fits the whole section (PictureBox Zoom, no scroll); splitters; header card with status badge; OK/WARN/FAILED counters under the list; right panel = Available curves, Changes summary, Notes, Details (WellView.cs); build 0 errors, 148 tests pass; NOT tried by hand, not committed. Owner deferred the no-curve warning (Core) until the UI is done
- 2026-10-28: T14 layout pass 2 (owner: not symmetrical vs reference): 3 columns with equal side widths (330), white bordered cards with equal gaps, header card + Available curves side by side above the plot, search/filter inside the well card, status bar left/right; build 0 errors, 148 tests pass; NOT tried by hand, not committed
- 2026-10-28: T14 layout pass 3: Explorer-style folder picker (FolderPicker.cs, shell COM, falls back to FolderBrowserDialog; no new library); MainForm split into MainForm.cs / .Layout.cs / .Well.cs; plot drawn at the exact size of its area (redrawn 250 ms after a resize); list columns fit (no sideways scroll); Wells/Issues tabs, search hint, counters; renderer note font shrinks in narrow tracks (PNG unchanged, 13 pt). Bug found by screenshot: window did not flag byte-identical copies as WARN like --batch -> BatchRunner.MarkCopy shared; window now 144 OK / 59 WARN / 0 FAILED (same as reports). Tests 148 pass (built to scratch: owner's app locked bin\Debug); checked with off-screen screenshots; owner still to try by hand; not committed
- 2026-10-28: T14 depth axis: ticks were chosen by ScottPlot from the image height (1000 ft in the small window, 500 ft in the PNG); now WellPlotRenderer.DepthStep gives ~1 label per 28 px (1/2/5 x 10^n ft) so window and PNG agree; DepthStepTests (5 cases); 153 tests pass (built to scratch, owner's app locked bin\Debug); window screenshot: 500 ft labels; PNG not re-rendered yet (by the formula 200 ft); not committed
- 2026-10-28: T14 final polish: well list now Well | Size | Status | Note ("no curves", "n/4 curves", "N issues", "unreadable"; WellRow.Note/Size + WellRowTests, 4 tests), file-name tooltips, plot title and legend shrink in a narrow window, side columns 400 px. 157 tests pass (scratch build; owner's app was locked). Owner decisions: UI = one commit; folder picker stays and AI use is disclosed in README; reference image was found by the owner (not part of the brief). Not tried by hand since these changes; not committed
- 2026-10-28: T14 DONE — 4d31473 window (Open Folder, well list, plot, report, Export all), pushed; origin/main = 4d31473; 157 tests pass; README still uncommitted on purpose
- 2026-10-28: T15 RE-MEASURED on final code 4d31473, Release, data/ 203 files 1019.4 MB. --batch 3 runs: 43.0 s / 28.6 s / 31.4 s (23.7 / 35.6 / 32.5 MB/s), peak working set 589 / 796 / 773 MB (run 1 slowest, cause not determined). Largest file alone (116.3 MB): 2.3 s, peak 754 MB (6.5x file size); median file (4238336177MAIN.las 2.7 MB): 0.3 s, 64 MB; empty folder 24 MB. Window (harness process itself ~97 MB at start): scan of 203 files 24.2 s and 25.5 s, working set after scan 441 / 521 MB, peak 896 / 809 MB; click largest well 3.0 s to plot; click median well 0.1 s. README numbers still to be written (README untouched, not committed)
- 2026-10-28: README (uncommitted, owner's rule) updated for the window: 157 tests, window section 1, design, section 4 figures from the T15 re-measure, new limitations (no zoom/table, no-curves files stay OK, reread per click, DPI untested), AI section (window, FolderPicker COM, owner found the reference mock-up); CONFIRM sentence still open; fresh-clone claim in section 1 to be re-verified at T18. Deliverables size check from a full batch run: 203 PNG = 33.9 MB, conditioning-report.csv 491 KB (4011 entries, 132 warn in 59 files), load-report.csv 22 KB, summary.md 2 KB
- 2026-10-28: README rewritten shorter (225 -> ~100 lines) after the owner found it too long; sections kept as the brief asks (build/run, design, conditioning decisions, limitations, AI); performance section states the weak point (memory 6.5x largest file, single thread ~30-40 s/GB); a TODO comment marks the deliverables/ line that is true only after T18; CONFIRM sentence still open; not committed
- 2026-10-28: README Example section added: docs/images/window.png (screenshot of the Release exe, 203 files, well 4246137183_PURE_UNIV_58_1_1_MainPass_020LUP.las; picked among 24 OK/4-curve/non-copy files with the fewest missing values, then by eye) + link to deliverables/plots/<same file>.png (exists only after T18; TODO comment in README). Not committed
- 2026-10-28: README section 6 per owner: removed the 'looked at the plots...' row and the paragraphs on the window mock-up / what I could not judge / how it was checked; CONFIRM sentence replaced by the owner's own wording (understands concepts and structure, not all C# syntax) + the workflow (AGENTS.md, SSOT.md, tests, DATA_NOTES, small commits); Project documents is now a table that lists SSOT.md. One sentence kept: the window mock-up was generated with ChatGPT (visual reference). Open decision: SSOT.md is git-ignored by AGENTS.md rule; owner wants it shipped with T18 -> needs its own docs commit (AGENTS.md + .gitignore + SSOT.md); not done yet. README not committed
- 2026-10-28: owner decided SSOT.md ships with the project (tracked in git, committed last in T18) and wants the history complete, so no line cap: the T14 handoff section was removed, local paths and "LOCAL ONLY" removed, status and known issues brought up to date, AGENTS.md and .gitignore changed for it (own docs commit, not committed yet); D-14 in DATA_NOTES was already corrected to 27 (checked against a full run: 27 files with a descending note)
- 2026-10-28: T18 deliverables e266cc7 — `--batch data deliverables` with the Release build of 4d31473: 203 loaded, 0 failed, 203 PNG (33.9 MB) + load-report.csv + conditioning-report.csv (4011 entries, 132 warn in 59 files) + summary.md; this run took 44.0 s and peaked at 798 MB (a fourth run, slightly above the three in README section 4); not pushed
- 2026-10-28: T17 README e012b4d — build/run, design, decisions, performance (weak point stated), limitations, AI use, an Example section (docs/images/window.png + a plot from deliverables/), image links checked locally; not pushed
- 2026-10-28: SSOT.md tracked in git (this file, AGENTS.md and .gitignore in one docs commit); T14 handoff removed. Next: fresh-clone test, push, check the README images on GitHub with curl, send the submission
- 2026-10-28: T18 DONE and submitted — fresh clone of origin/main (b8c2f0d) into a short path: msbuild -restore OK, 157/157 tests pass, App.exe built; a first clone into a long path failed with "Filename too long" (long plot filenames) -> README warns to clone into a short path (453c891, pushed; also dropped the 192/11 copy count, the plot-choice rationale and the commit-metadata note from README); README images on GitHub return HTTP 200; owner emailed the submission with the repository link
