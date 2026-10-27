using System.Collections.Generic;

namespace PetrophysicalLogVisualizer.Core
{
    /// <summary>The ~A table as columns: depth = double[], each curve = float[] (no object per row).</summary>
    public sealed class LasData
    {
        public double[] Depth { get; set; }

        /// <summary>One array per curve, depth column excluded; every array has Rows items.</summary>
        public float[][] Curves { get; set; }

        public int Rows { get; set; }

        /// <summary>Per column (depth first): how many tokens were not numbers and became NaN.</summary>
        public int[] BadTokens { get; set; }

        public List<string> Warnings { get; } = new List<string>();
    }
}
