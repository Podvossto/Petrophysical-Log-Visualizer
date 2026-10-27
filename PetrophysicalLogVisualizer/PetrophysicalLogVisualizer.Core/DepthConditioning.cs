using System;
using System.Collections.Generic;

namespace PetrophysicalLogVisualizer.Core
{
    /// <summary>What was done to the depth column, so the conditioning report can trace every change (F3, F6).</summary>
    public sealed class DepthResult
    {
        public bool Reversed { get; set; }
        public bool Sorted { get; set; }
        public int RowsMoved { get; set; }
        public int DuplicatesDropped { get; set; }

        /// <summary>Most common depth step in the final table; null when there are fewer than 2 rows.</summary>
        public double? RealStep { get; set; }
        public double? HeaderStep { get; set; }
        public int Gaps { get; set; }
        public double LargestGap { get; set; }
        public double FirstGapDepth { get; set; }

        /// <summary>One line per change made to the table, or fact worth stating (no header STEP).</summary>
        public List<string> Notes { get; } = new List<string>();

        /// <summary>Things the owner should look at: jumps, duplicates, a wrong header STEP, gaps.</summary>
        public List<string> Warnings { get; } = new List<string>();
    }

    public static class DepthConditioning
    {
        /// <summary>D-14, D-15: depth ascending, duplicates resolved, real step vs header STEP, gaps. Never resamples. Changes the arrays of data.</summary>
        public static DepthResult Apply(LasData data, LasHeader header)
        {
            var result = new DepthResult { HeaderStep = header.Step };
            double[] depth = data.Depth;
            int n = data.Rows;
            if (n == 0) return result;

            int ups = 0, downs = 0;
            for (int i = 1; i < n; i++)
            {
                if (depth[i] > depth[i - 1]) ups++;
                else if (depth[i] < depth[i - 1]) downs++;
            }

            int[] order = new int[n];
            for (int i = 0; i < n; i++) order[i] = i;
            bool descending = downs > 0 && ups == 0;
            if (downs > 0 && ups > 0)   // jumps: stable sort, equal depths keep their file order
            {
                Array.Sort(order, (a, b) => { int c = depth[a].CompareTo(depth[b]); return c != 0 ? c : a.CompareTo(b); });
                for (int i = 0; i < n; i++) if (order[i] != i) result.RowsMoved++;
                result.Sorted = true;
            }

            // Duplicates: the first one in file order is kept (equal depths sit next to each other here).
            int kept = 0;
            for (int i = 0; i < n; i++)
                if (i == 0 || depth[order[i]] != depth[order[kept - 1]]) order[kept++] = order[i];
            result.DuplicatesDropped = n - kept;
            if (descending) { Array.Reverse(order, 0, kept); result.Reversed = true; }

            if (result.Reversed || result.Sorted || kept < n) Gather(data, order, kept);

            if (result.Reversed) result.Notes.Add("Depth was descending; the rows were put in ascending order");
            if (result.Sorted)
                result.Warnings.Add("Depth is not in one direction; rows sorted by depth, " + result.RowsMoved + " rows moved");
            if (result.DuplicatesDropped > 0)
                result.Warnings.Add(result.DuplicatesDropped + " rows repeat an earlier depth; the first row at each depth was kept");

            Sampling(data, result);
            return result;
        }

        private static void Gather(LasData data, int[] order, int kept)
        {
            var depth = new double[kept];
            for (int i = 0; i < kept; i++) depth[i] = data.Depth[order[i]];
            data.Depth = depth;
            for (int c = 0; c < data.Curves.Length; c++)
            {
                float[] old = data.Curves[c], fresh = new float[kept];
                for (int i = 0; i < kept; i++) fresh[i] = old[order[i]];
                data.Curves[c] = fresh;
            }
            data.Rows = kept;
        }

        // D-15: the header STEP is not trusted; the real step is the most common spacing between rows.
        private static void Sampling(LasData data, DepthResult result)
        {
            int n = data.Rows;
            if (n < 2) return;
            var counts = new Dictionary<double, int>();
            for (int i = 1; i < n; i++)
            {
                double step = Math.Round(data.Depth[i] - data.Depth[i - 1], 4);
                int seen;
                counts.TryGetValue(step, out seen);
                counts[step] = seen + 1;
            }
            double real = 0;
            int best = 0;
            foreach (KeyValuePair<double, int> pair in counts)
                if (pair.Value > best || (pair.Value == best && pair.Key < real)) { best = pair.Value; real = pair.Key; }
            if (real <= 0) return;   // spacing below 0.0001 rounds to 0: nothing sensible to compare
            result.RealStep = real;

            double header = result.HeaderStep.HasValue ? Math.Abs(result.HeaderStep.Value) : 0;
            if (!result.HeaderStep.HasValue)
                result.Notes.Add("Header has no STEP; the real step is " + real);
            else if (header == 0)
                result.Warnings.Add("Header STEP is 0; the real step is " + real);
            else if (Math.Abs(header - real) > 0.01 * real)
                result.Warnings.Add("Header STEP is " + header + " but the real step is " + real);

            for (int i = 1; i < n; i++)
            {
                double gap = data.Depth[i] - data.Depth[i - 1];
                if (gap <= 1.5 * real) continue;
                if (result.Gaps++ == 0) result.FirstGapDepth = data.Depth[i - 1];
                if (gap > result.LargestGap) result.LargestGap = gap;
            }
            if (result.Gaps > 0)
                result.Warnings.Add(result.Gaps + " gaps larger than 1.5 x the step (first after depth " + result.FirstGapDepth + ", largest " + result.LargestGap + "); not filled");
        }
    }
}
