using System.Collections.Generic;

namespace PetrophysicalLogVisualizer.Core
{
    public sealed class DepthUnitResult
    {
        /// <summary>The unit label as written in the file; empty when there was none.</summary>
        public string OriginalUnit { get; set; }
        public double Divisor { get; set; } = 1;
        public List<string> Notes { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
    }

    public static class DepthUnits
    {
        /// <summary>D-13: depth is the first column and ends up in feet. STRT, STOP and STEP follow it so the later step check compares like with like.</summary>
        public static DepthUnitResult Apply(LasData data, LasHeader header)
        {
            string unit = header.Curves.Count > 0 ? header.Curves[0].Unit.Trim() : "";
            var result = new DepthUnitResult { OriginalUnit = unit };

            double? divisor = CurveRules.DepthDivisor(unit);
            if (unit.Length == 0)
                result.Warnings.Add("Depth unit is missing; read as feet");
            else if (divisor == null)
                result.Warnings.Add("Depth unit '" + unit + "' is not recognized; read as feet");
            if (divisor == null || divisor == 1) return result;

            result.Divisor = divisor.Value;
            for (int i = 0; i < data.Depth.Length; i++) data.Depth[i] /= result.Divisor;
            if (header.Strt.HasValue) header.Strt /= result.Divisor;
            if (header.Stop.HasValue) header.Stop /= result.Divisor;
            if (header.Step.HasValue) header.Step /= result.Divisor;
            result.Notes.Add("Depth was in metres (" + unit + "); converted to feet by dividing by " + result.Divisor);
            return result;
        }
    }
}
