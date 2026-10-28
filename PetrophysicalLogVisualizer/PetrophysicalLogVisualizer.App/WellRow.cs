using System.IO;
using PetrophysicalLogVisualizer.Core;

namespace PetrophysicalLogVisualizer.App
{
    /// <summary>The light summary of one file kept for the well list. No curve values: the file is reloaded when it is clicked.</summary>
    public sealed class WellRow
    {
        public string Path { get; private set; }
        public string FileName { get; private set; }
        public string Status { get; private set; }      // OK, WARN or FAILED, same rule as the load report
        public int Rows { get; private set; }
        public double? TopDepth { get; private set; }   // ft, after conditioning (ascending)
        public double? BaseDepth { get; private set; }
        public double? Step { get; private set; }       // measured step when known, else the header STEP
        public int IssueCount { get; private set; }
        public string DuplicateOf { get; private set; }
        public long Bytes { get; private set; }
        public int CurvesFound { get; private set; }   // how many of GR / RES / RHOB / NPHI were chosen (0 to 4)

        public static WellRow From(LasFileResult r)
        {
            var row = new WellRow
            {
                Path = r.Path,
                FileName = System.IO.Path.GetFileName(r.Path),
                Status = r.Failed ? "FAILED" : r.Issues.Count > 0 ? "WARN" : "OK",
                IssueCount = r.Issues.Count,
                Bytes = r.Bytes,
                DuplicateOf = r.DuplicateOf,
            };
            if (r.Data != null && r.Data.Rows > 0 && r.Data.Depth != null)
            {
                row.Rows = r.Data.Rows;
                row.TopDepth = r.Data.Depth[0];
                row.BaseDepth = r.Data.Depth[r.Data.Rows - 1];
                row.Step = r.Depth != null && r.Depth.RealStep != null ? r.Depth.RealStep : r.Header.Step;
            }
            if (r.Roles != null)
                foreach (RoleChoice c in r.Roles.Roles) if (c != null && c.Curve >= 0) row.CurvesFound++;
            return row;
        }

        /// <summary>"640 KB" or "12.3 MB".</summary>
        public string Size
        {
            get { return Bytes < 1048576 ? (Bytes + 1023) / 1024 + " KB" : (Bytes / 1048576.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " MB"; }
        }

        /// <summary>Short remark for the list: what a reader should know before clicking. Empty when there is nothing to say.</summary>
        public string Note
        {
            get
            {
                if (Status == "FAILED") return "unreadable";
                if (CurvesFound == 0) return "no curves";
                if (CurvesFound < 4) return CurvesFound + "/4 curves";
                return IssueCount > 0 ? IssueCount + " issue" + (IssueCount > 1 ? "s" : "") : "";
            }
        }
    }
}
