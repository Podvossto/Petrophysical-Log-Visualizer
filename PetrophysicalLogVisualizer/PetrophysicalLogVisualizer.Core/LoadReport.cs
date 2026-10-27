using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PetrophysicalLogVisualizer.Core
{
    /// <summary>load-report.csv: one row per file, so a reviewer can see every problem without opening the file.</summary>
    public static class LoadReport
    {
        public static string ToCsv(IList<LasFileResult> results)
        {
            var sb = new StringBuilder();
            sb.Append("file,status,bytes,rows,curves,header_wrap,header_null,issue_count,issues,ms\r\n");
            foreach (LasFileResult r in results)
            {
                LasHeader h = r.Header;
                Field(sb, System.IO.Path.GetFileName(r.Path)).Append(',');
                sb.Append(Status(r)).Append(',');
                sb.Append(r.Bytes).Append(',');
                sb.Append(r.Data == null ? 0 : r.Data.Rows).Append(',');
                sb.Append(h == null ? 0 : h.Curves.Count).Append(',');
                sb.Append(h == null || h.Wrap == null ? "" : (h.Wrap.Value ? "YES" : "NO")).Append(',');
                sb.Append(h == null || h.NullValue == null ? "" : h.NullValue.Value.ToString(CultureInfo.InvariantCulture)).Append(',');
                sb.Append(r.Issues.Count).Append(',');
                Field(sb, string.Join(" | ", r.Issues)).Append(',');
                sb.Append(r.ElapsedMs).Append("\r\n");
            }
            return sb.ToString();
        }

        private static string Status(LasFileResult r)
        {
            if (r.Failed) return "FAILED";
            return r.Issues.Count > 0 ? "WARN" : "OK";
        }

        // quote a field when it holds a comma, quote or line break
        private static StringBuilder Field(StringBuilder sb, string text)
        {
            if (text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return sb.Append(text);
            return sb.Append('"').Append(text.Replace("\"", "\"\"")).Append('"');
        }
    }
}
