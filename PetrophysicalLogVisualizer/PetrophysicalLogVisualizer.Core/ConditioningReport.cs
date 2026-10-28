using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PetrophysicalLogVisualizer.Core
{
    /// <summary>One thing that was changed, chosen or noticed in one file (F6).</summary>
    public sealed class ConditioningEntry
    {
        public string File { get; set; }
        /// <summary>"note" = routine (the declared NULL replaced, a curve chosen); "warn" = look at it (undeclared marker, depth problem, rejected curve).</summary>
        public string Level { get; set; } = "note";
        public string Category { get; set; }
        public string Curve { get; set; } = "";
        /// <summary>Number of values or rows affected; -1 when the entry is a statement without a count.</summary>
        public long Count { get; set; } = -1;
        public string Detail { get; set; }
    }

    /// <summary>conditioning-report.csv (one row per entry) and summary.md (totals over all files). Files that failed to load have no entries; they are in load-report.csv.</summary>
    public static class ConditioningReport
    {
        public static List<ConditioningEntry> Entries(LasFileResult r)
        {
            var list = new List<ConditioningEntry>();
            if (r.Failed || r.Data == null || r.Header == null) return list;
            string file = System.IO.Path.GetFileName(r.Path);
            LasHeader h = r.Header;
            if (r.DuplicateOf != null)
                Add(list, file, "warn", "duplicate-file", "", "Byte-identical copy of " + r.DuplicateOf + "; processed like any other file, so its changes below count twice in the totals", -1);
            if (h.Version.HasValue && h.Version.Value >= 3.0)
                Add(list, file, "note", "format", "", "LAS " + Num(h.Version.Value) + ": read as best effort (first data section only, other sections skipped)", -1);
            foreach (string w in r.Data.Warnings) Add(list, file, "warn", "parse", "", w, -1);

            if (r.DepthUnit != null)
            {
                foreach (string n in r.DepthUnit.Notes) Add(list, file, "note", "depth-unit", "", n, r.Data.Rows);
                foreach (string w in r.DepthUnit.Warnings) Add(list, file, "warn", "depth-unit", "", w, -1);
            }
            if (r.Depth != null)
            {
                foreach (string n in r.Depth.Notes) Add(list, file, "note", "depth", "", n, -1);
                foreach (string w in r.Depth.Warnings) Add(list, file, "warn", "depth", "", w, -1);
            }
            if (r.Missing != null) AddMissing(list, file, r);
            if (r.Roles != null) AddRoles(list, file, r);
            return list;
        }

        private static void Add(List<ConditioningEntry> list, string file, string level, string category, string curve, string detail, long count)
        {
            list.Add(new ConditioningEntry { File = file, Level = level, Category = category, Curve = curve, Detail = detail, Count = count });
        }

        private static void AddMissing(List<ConditioningEntry> list, string file, LasFileResult r)
        {
            MissingValueResult m = r.Missing;
            for (int c = 0; c < m.DeclaredNull.Length; c++)
            {
                string curve = r.Header.Curves[c + 1].Mnemonic.Trim();
                if (m.DeclaredNull[c] > 0)
                    Add(list, file, "note", "missing-value", curve, "values equal to the declared NULL (" + Num(r.Header.NullValue.Value) + ") set to NaN", m.DeclaredNull[c]);
                for (int k = 0; k < MissingValues.Markers.Length; k++)
                    if (m.Markers[c][k] > 0)
                        Add(list, file, "warn", "missing-value", curve, "values equal to " + Num(MissingValues.Markers[k]) + " (not the declared NULL) set to NaN", m.Markers[c][k]);
            }
            // the NULL = 0 and no-NULL warnings; the marker warnings repeat the totals of the rows above
            foreach (string w in m.Warnings)
                if (!m.Notes.Contains(w)) Add(list, file, "warn", "missing-value", "", w, -1);
        }

        private static void AddRoles(List<ConditioningEntry> list, string file, LasFileResult r)
        {
            foreach (string note in r.Roles.Notes) Add(list, file, "note", "curve-choice", "", note, -1);
            foreach (RoleChoice c in r.Roles.Roles)
                if (c.Curve >= 0 && c.Divisor != 1)
                    Add(list, file, "note", "unit-conversion", c.Mnemonic, "divided by " + Num(c.Divisor) + " (" + c.OriginalUnit + " to " + CurveRules.Standard(c.Role) + ")", r.Data.Rows);
            foreach (string w in r.Roles.Warnings) Add(list, file, "warn", "curve-choice", "", w, -1);
        }

        public static string ToCsv(IList<LasFileResult> results)
        {
            var sb = new StringBuilder();
            sb.Append("file,level,category,curve,count,detail\r\n");
            foreach (LasFileResult r in results)
                foreach (ConditioningEntry e in Entries(r))
                {
                    Field(sb, e.File).Append(',').Append(e.Level).Append(',').Append(e.Category).Append(',');
                    Field(sb, e.Curve).Append(',');
                    if (e.Count >= 0) sb.Append(e.Count);
                    sb.Append(',');
                    Field(sb, e.Detail).Append("\r\n");
                }
            return sb.ToString();
        }

        public static string ToSummary(IList<LasFileResult> results)
        {
            int loaded = 0, notes = 0, warns = 0, filesWithWarn = 0, copies = 0;
            var failed = new List<string>();
            var perCategory = new SortedDictionary<string, int>(StringComparer.Ordinal);
            int[] roleFiles = new int[4];
            var chosen = new Dictionary<string, int>[4];
            for (int i = 0; i < 4; i++) chosen[i] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            long cells = 0, nullValues = 0, markerValues = 0, converted = 0, moved = 0, dropped = 0;
            int nullFiles = 0, markerFiles = 0, reversed = 0, sorted = 0, dupFiles = 0, stepWrong = 0, noStep = 0, gapFiles = 0, metres = 0;

            foreach (LasFileResult r in results)
            {
                if (r.DuplicateOf != null) copies++;
                if (r.Failed || r.Data == null)
                {
                    failed.Add(System.IO.Path.GetFileName(r.Path) + ": " + (r.Issues.Count > 0 ? r.Issues[r.Issues.Count - 1] : "failed"));
                    continue;
                }
                loaded++;
                cells += (long)r.Data.Rows * r.Data.Curves.Length;
                var seen = new HashSet<string>();
                bool anyWarn = false;
                foreach (ConditioningEntry e in Entries(r))
                {
                    if (e.Level == "warn") { warns++; anyWarn = true; } else notes++;
                    if (seen.Add(e.Category)) { int n; perCategory.TryGetValue(e.Category, out n); perCategory[e.Category] = n + 1; }
                }
                if (anyWarn) filesWithWarn++;
                long a = Sum(r.Missing.DeclaredNull), b = 0;
                foreach (int[] row in r.Missing.Markers) b += Sum(row);
                nullValues += a; markerValues += b;
                if (a > 0) nullFiles++;
                if (b > 0) markerFiles++;
                if (r.DepthUnit.Divisor != 1) metres++;
                if (r.Depth.Reversed) reversed++;
                if (r.Depth.Sorted) { sorted++; moved += r.Depth.RowsMoved; }
                if (r.Depth.DuplicatesDropped > 0) { dupFiles++; dropped += r.Depth.DuplicatesDropped; }
                if (r.Depth.Gaps > 0) gapFiles++;
                if (!r.Header.Step.HasValue) noStep++;
                foreach (string w in r.Depth.Warnings) if (w.StartsWith("Header STEP is")) { stepWrong++; break; }
                foreach (RoleChoice c in r.Roles.Roles)
                {
                    if (c.Curve < 0) continue;
                    roleFiles[(int)c.Role]++;
                    string key = c.Mnemonic.ToUpperInvariant();
                    int n; chosen[(int)c.Role].TryGetValue(key, out n); chosen[(int)c.Role][key] = n + 1;
                    if (c.Divisor != 1) converted += r.Data.Rows;
                }
            }

            var sb = new StringBuilder();
            sb.Append("# Conditioning summary\n\n");
            sb.Append("Files: " + results.Count + " (loaded " + loaded + ", failed " + failed.Count + ").\n\n");
            if (copies > 0)
                sb.Append((results.Count - copies) + " files are different; " + copies + " are byte-identical copies of an earlier file. Every file is processed, so the totals below count the copies too.\n\n");
            sb.Append("Entries in conditioning-report.csv: " + (notes + warns) + ". `note` = routine (" + notes + "), `warn` = worth a look (" + warns + ", in " + filesWithWarn + " files). Filter the `level` column on `warn` to skip the routine rows.\n\n");
            sb.Append("## Changes made\n\n| Change | Level | Files | Values or rows |\n|---|---|---|---|\n");
            sb.Append("| Values equal to the declared NULL set to NaN | note | " + nullFiles + " | " + nullValues + " |\n");
            sb.Append("| Values equal to a known marker (not declared) set to NaN | warn | " + markerFiles + " | " + markerValues + " |\n");
            sb.Append("| Depth reversed to ascending | note | " + reversed + " | |\n");
            sb.Append("| Depth sorted (jumps) | warn | " + sorted + " | " + moved + " rows moved |\n");
            sb.Append("| Duplicate depths dropped (first kept) | warn | " + dupFiles + " | " + dropped + " rows |\n");
            sb.Append("| Depth converted from metres to feet | note | " + metres + " | |\n");
            sb.Append("| Chosen curve converted to the standard unit | note | | " + converted + " rows |\n\n");
            sb.Append("Values set to NaN by the declared NULL or a marker: " + Percent(nullValues + markerValues, cells) + " of all " + cells + " curve values (" + Percent(nullValues, cells) + " by the declared NULL, which is routine: a tool does not measure the whole well).\n\n");
            sb.Append("## Sampling (reported, never resampled)\n\n");
            sb.Append("- Header STEP differs from the real step: " + stepWrong + " files\n- Header has no STEP: " + noStep + " files\n- Files with gaps larger than 1.5 x the step: " + gapFiles + "\n\n");
            sb.Append("## Curve choice\n\n| Role | Files with a curve | Files without | Chosen curves (files) |\n|---|---|---|---|\n");
            foreach (CurveRole role in Enum.GetValues(typeof(CurveRole)))
                sb.Append("| " + role + " | " + roleFiles[(int)role] + " | " + (loaded - roleFiles[(int)role]) + " | " + Top(chosen[(int)role]) + " |\n");
            sb.Append("\n## Entries by category (files that have at least one)\n\n");
            foreach (var kv in perCategory) sb.Append("- " + kv.Key + ": " + kv.Value + "\n");
            sb.Append("\n## Not loaded\n\n");
            if (failed.Count == 0) sb.Append("None.\n");
            foreach (string f in failed) sb.Append("- " + f + "\n");
            return sb.ToString();
        }

        private static string Percent(long part, long whole)
        {
            return whole == 0 ? "0 %" : (100.0 * part / whole).ToString("F1", CultureInfo.InvariantCulture) + " %";
        }

        private static long Sum(int[] counts)
        {
            long total = 0;
            foreach (int k in counts) total += k;
            return total;
        }

        private static string Top(Dictionary<string, int> counts)
        {
            var list = new List<KeyValuePair<string, int>>(counts);
            list.Sort((x, y) => y.Value != x.Value ? y.Value.CompareTo(x.Value) : string.CompareOrdinal(x.Key, y.Key));
            var parts = new List<string>();
            for (int i = 0; i < list.Count && i < 8; i++) parts.Add(list[i].Key + " " + list[i].Value);
            return parts.Count == 0 ? "-" : string.Join(", ", parts);
        }

        private static string Num(double d)
        {
            return d.ToString(CultureInfo.InvariantCulture);
        }

        private static StringBuilder Field(StringBuilder sb, string text)
        {
            return LoadReport.Field(sb, text);
        }
    }
}
