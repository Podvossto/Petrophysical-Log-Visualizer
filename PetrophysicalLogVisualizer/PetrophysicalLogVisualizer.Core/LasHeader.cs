using System.Collections.Generic;

namespace PetrophysicalLogVisualizer.Core
{
    /// <summary>One curve definition line from the ~C block (D-05).</summary>
    public sealed class CurveInfo
    {
        public string Mnemonic { get; }
        public string Unit { get; }
        public string Description { get; }

        public CurveInfo(string mnemonic, string unit, string description)
        {
            Mnemonic = mnemonic;
            Unit = unit;
            Description = description;
        }
    }

    /// <summary>Values parsed from the LAS header sections (~V, ~W, ~C).</summary>
    public sealed class LasHeader
    {
        public double? Version { get; set; }
        public bool? Wrap { get; set; }
        public double? NullValue { get; set; }
        public double? Strt { get; set; }
        public double? Stop { get; set; }
        public double? Step { get; set; }
        public List<CurveInfo> Curves { get; } = new List<CurveInfo>();
        public List<string> Warnings { get; } = new List<string>();
    }
}