using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace PetrophysicalLogVisualizer.Core
{
    /// <summary>Everything the program knows about one file after loading. A bad file is a result with Failed = true, never an exception.</summary>
    public sealed class LasFileResult
    {
        public string Path { get; set; }
        public long Bytes { get; set; }
        public long ElapsedMs { get; set; }
        public LasHeader Header { get; set; }
        public LasData Data { get; set; }
        public MissingValueResult Missing { get; set; }
        public DepthUnitResult DepthUnit { get; set; }
        public DepthResult Depth { get; set; }
        public RoleResult Roles { get; set; }
        public bool Failed { get; set; }

        /// <summary>Header warnings, then data warnings, then loader messages, in that order.</summary>
        public List<string> Issues { get; } = new List<string>();
    }

    public static class LasLoader
    {
        /// <summary>D-01: .las and .LAS alike. Sub-folders are not searched.</summary>
        public static string[] FindFiles(string folder)
        {
            var found = new List<string>();
            foreach (string path in Directory.GetFiles(folder))
                if (string.Equals(System.IO.Path.GetExtension(path), ".las", StringComparison.OrdinalIgnoreCase))
                    found.Add(path);
            found.Sort(StringComparer.OrdinalIgnoreCase);
            return found.ToArray();
        }

        public static LasFileResult Load(string path)
        {
            var result = new LasFileResult { Path = path };
            var timer = Stopwatch.StartNew();
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                result.Bytes = bytes.Length;
                // D-02: single-byte text, so no byte is lost and non-ASCII header text never throws
                string text = Encoding.GetEncoding(28591).GetString(bytes);
                bytes = null;
                string[] lines = text.Split('\n');   // once per file, not per line; '\r' is a separator in both parsers
                text = null;

                int dataStart;
                result.Header = LasHeaderParser.Parse(lines, out dataStart);
                result.Issues.AddRange(result.Header.Warnings);

                bool wrap = result.Header.Wrap == true;
                result.Data = LasDataParser.Parse(lines, dataStart, result.Header.Curves.Count, wrap);
                result.Issues.AddRange(result.Data.Warnings);

                if (result.Data.Rows == 0)
                {
                    result.Failed = true;
                    result.Issues.Add("No data rows could be read");
                }
                else
                {
                    result.Missing = MissingValues.Apply(result.Data, result.Header);
                    result.Issues.AddRange(result.Missing.Warnings);
                    result.DepthUnit = DepthUnits.Apply(result.Data, result.Header);   // before the step check: both must be in feet
                    result.Issues.AddRange(result.DepthUnit.Warnings);
                    result.Depth = DepthConditioning.Apply(result.Data, result.Header);
                    result.Issues.AddRange(result.Depth.Warnings);
                    result.Roles = CurveRoles.Apply(result.Data, result.Header);
                    result.Issues.AddRange(result.Roles.Warnings);
                }
            }
            catch (Exception ex)
            {
                result.Failed = true;
                result.Issues.Add("Load failed: " + ex.GetType().Name + ": " + ex.Message);
            }
            result.ElapsedMs = timer.ElapsedMilliseconds;
            return result;
        }
    }
}
