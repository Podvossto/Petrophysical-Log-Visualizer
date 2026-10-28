using System.Collections.Generic;
using System.Text;
using PetrophysicalLogVisualizer.Core;

namespace PetrophysicalLogVisualizer.App
{
    /// <summary>Texts for the right-hand panel of one clicked well: the 4 roles, the changes per category, the plot notes and the full list.</summary>
    public sealed class WellView
    {
        public List<string[]> Curves { get; } = new List<string[]>();    // role, curve, unit
        public List<string[]> Summary { get; } = new List<string[]>();   // category, count, warnings
        public string Notes { get; set; } = "";
        public string Details { get; set; } = "";

        private static readonly string[] RoleNames = { "Gamma ray", "Resistivity", "Bulk density", "Neutron porosity" };

        /// <summary>model may be null (file did not load). Never throws for a failed file: it shows the issues instead.</summary>
        public static WellView Build(LasFileResult r, WellPlotModel model)
        {
            var v = new WellView();
            if (r.Failed || r.Roles == null)
            {
                var failed = new StringBuilder("FAILED - this file could not be read:\r\n");
                foreach (string issue in r.Issues) failed.Append("  ").Append(issue).Append("\r\n");
                v.Details = failed.ToString();
                v.Notes = "No plot: the file did not load.";
                return v;
            }

            foreach (CurveRole role in System.Enum.GetValues(typeof(CurveRole)))
            {
                RoleChoice c = r.Roles.Roles[(int)role];
                v.Curves.Add(new[] { RoleNames[(int)role], c.Curve >= 0 ? c.Mnemonic : "(missing)", c.Curve >= 0 ? CurveRules.Standard(role) : "" });
            }

            List<ConditioningEntry> entries = ConditioningReport.Entries(r);
            v.AddSummary(entries);
            v.Details = Describe(entries);
            if (model != null)
            {
                var notes = new StringBuilder();
                foreach (PlotTrack t in model.Tracks)
                    foreach (string m in t.Messages) notes.Append(m).Append("\r\n");
                v.Notes = notes.ToString();
            }
            return v;
        }

        // one row per category: number of values changed when the entries carry counts, else the number of entries
        private void AddSummary(List<ConditioningEntry> entries)
        {
            var order = new List<string>();
            var total = new Dictionary<string, long>();
            var warns = new Dictionary<string, int>();
            foreach (ConditioningEntry e in entries)
            {
                if (!total.ContainsKey(e.Category)) { order.Add(e.Category); total[e.Category] = 0; warns[e.Category] = 0; }
                total[e.Category] += e.Count >= 0 ? e.Count : 1;
                if (e.Level == "warn") warns[e.Category]++;
            }
            foreach (string category in order)
                Summary.Add(new[] { Pretty(category), total[category].ToString(), warns[category] > 0 ? warns[category].ToString() : "" });
        }

        // "missing-value" -> "Missing value"
        private static string Pretty(string category)
        {
            string text = category.Replace('-', ' ');
            return text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
        }

        // warnings first, then notes (the curve-choice notes name the chosen curve and the other candidates)
        private static string Describe(List<ConditioningEntry> entries)
        {
            var sb = new StringBuilder();
            foreach (string level in new[] { "warn", "note" })
            {
                bool header = false;
                foreach (ConditioningEntry e in entries)
                {
                    if (e.Level != level) continue;
                    if (!header) { sb.Append(level == "warn" ? "WARNINGS" : "CHANGES AND CHOICES").Append("\r\n"); header = true; }
                    sb.Append(e.Category);
                    if (e.Curve.Length > 0) sb.Append(" / ").Append(e.Curve);
                    if (e.Count >= 0) sb.Append(" (").Append(e.Count).Append(")");
                    sb.Append(":\r\n  ").Append(e.Detail).Append("\r\n");
                }
                if (header) sb.Append("\r\n");
            }
            if (sb.Length == 0) sb.Append("Nothing was changed and nothing needs attention.");
            return sb.ToString();
        }
    }
}
