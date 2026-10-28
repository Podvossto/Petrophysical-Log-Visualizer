using System.Collections.Generic;
using System.Globalization;

namespace PetrophysicalLogVisualizer.Core
{
    /// <summary>What was changed to NaN, per curve, so the conditioning report can trace every edit (F2, F6).</summary>
    public sealed class MissingValueResult
    {
        /// <summary>Per curve: values equal to the NULL declared in the header.</summary>
        public int[] DeclaredNull { get; set; }

        /// <summary>[curve][i]: values equal to MissingValues.Markers[i] that were not the declared NULL.</summary>
        public int[][] Markers { get; set; }

        /// <summary>One line per kind of change, always written when something changed.</summary>
        public List<string> Notes { get; } = new List<string>();

        /// <summary>Things the owner should look at: NULL = 0, no NULL declared, markers the header did not declare.</summary>
        public List<string> Warnings { get; } = new List<string>();
    }

    public static class MissingValues
    {
        /// <summary>D-16: markers seen in the real files besides the declared NULL. Matched as numbers, never by range.</summary>
        public static readonly double[] Markers = { -999.25, -999, -9999, -99999, -9999.9999, -9999.999, -999.2 };

        /// <summary>Curve values only (the depth column is never touched). Changes the arrays in place.</summary>
        public static MissingValueResult Apply(LasData data, LasHeader header)
        {
            int n = data.Curves.Length;
            var result = new MissingValueResult { DeclaredNull = new int[n], Markers = new int[n][] };

            // Compared as float: that is how the values are stored, so -999.2500 and -999.250 meet in one bucket
            // and -9999.9999 (stored as -10000) is still found.
            bool useDeclared = header.NullValue.HasValue && header.NullValue.Value != 0;   // D-04: 0 is a real reading
            float declared = useDeclared ? (float)header.NullValue.Value : 0f;
            var markers = new float[Markers.Length];
            for (int m = 0; m < markers.Length; m++) markers[m] = (float)Markers[m];

            if (!header.NullValue.HasValue)
                result.Warnings.Add("No NULL value declared in the header; only the known markers are treated as missing");
            else if (!useDeclared)
                result.Warnings.Add("Declared NULL is 0; zeros are real readings and were not treated as missing");

            for (int c = 0; c < n; c++)
            {
                result.Markers[c] = new int[Markers.Length];
                float[] values = data.Curves[c];
                for (int r = 0; r < values.Length; r++)
                {
                    float v = values[r];
                    if (float.IsNaN(v)) continue;
                    if (useDeclared && v == declared) { result.DeclaredNull[c]++; values[r] = float.NaN; continue; }
                    for (int m = 0; m < markers.Length; m++)
                    {
                        if (v != markers[m]) continue;
                        result.Markers[c][m]++;
                        values[r] = float.NaN;
                        break;
                    }
                }
            }
            AddNotes(result, header);
            return result;
        }

        private static void AddNotes(MissingValueResult result, LasHeader header)
        {
            int total, curves;
            Sum(result.DeclaredNull, out total, out curves);
            if (total > 0)
                result.Notes.Add(total + " values equal to the declared NULL (" + Text(header.NullValue.Value) + ") set to NaN in " + curves + " curves");

            for (int m = 0; m < Markers.Length; m++)
            {
                var perCurve = new int[result.Markers.Length];
                for (int c = 0; c < perCurve.Length; c++) perCurve[c] = result.Markers[c][m];
                Sum(perCurve, out total, out curves);
                if (total == 0) continue;
                string line = total + " values equal to " + Text(Markers[m]) + " (not the declared NULL) set to NaN in " + curves + " curves";
                result.Notes.Add(line);
                result.Warnings.Add(line);
            }
        }

        private static void Sum(int[] counts, out int total, out int curvesWithAny)
        {
            total = 0;
            curvesWithAny = 0;
            foreach (int k in counts)
            {
                total += k;
                if (k > 0) curvesWithAny++;
            }
        }

        private static string Text(double d)
        {
            return d.ToString(CultureInfo.InvariantCulture);
        }
    }
}
