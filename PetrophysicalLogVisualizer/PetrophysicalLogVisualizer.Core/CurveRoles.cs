using System;
using System.Collections.Generic;

namespace PetrophysicalLogVisualizer.Core
{
    /// <summary>The curve chosen for one role, or Curve = -1 when the well has no usable one (F5).</summary>
    public sealed class RoleChoice
    {
        public CurveRole Role { get; }
        /// <summary>Index into LasData.Curves (the depth column is not in that array).</summary>
        public int Curve { get; set; } = -1;
        public string Mnemonic { get; set; }
        public string OriginalUnit { get; set; }
        public double Divisor { get; set; } = 1;
        /// <summary>The other candidates with the reason each was not chosen.</summary>
        public List<string> Others { get; } = new List<string>();

        public RoleChoice(CurveRole role) { Role = role; }
    }

    public sealed class RoleResult
    {
        /// <summary>Indexed by (int)CurveRole.</summary>
        public RoleChoice[] Roles { get; } = new RoleChoice[4];
        /// <summary>One line per role (choice, conversion, other candidates) for the conditioning report.</summary>
        public List<string> Notes { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
    }

    public static class CurveRoles
    {
        /// <summary>D-17 to D-20: pick GR / RES / RHOB / NPHI by mnemonic and unit, convert the chosen curve in place. Other curves are not touched.</summary>
        public static RoleResult Apply(LasData data, LasHeader header)
        {
            var result = new RoleResult();
            foreach (CurveRole role in Enum.GetValues(typeof(CurveRole)))
                result.Roles[(int)role] = Choose(role, data, header, result);
            return result;
        }

        private static RoleChoice Choose(CurveRole role, LasData data, LasHeader header, RoleResult result)
        {
            var choice = new RoleChoice(role);
            string note = "";
            int count = Math.Min(data.Curves.Length, header.Curves.Count - 1);
            foreach (string alias in CurveRules.Aliases(role))
            {
                for (int c = 0; c < count; c++)
                {
                    CurveInfo info = header.Curves[c + 1];   // header curve 0 is the depth
                    if (!string.Equals(info.Mnemonic.Trim(), alias, StringComparison.OrdinalIgnoreCase)) continue;

                    string unit = info.Unit.Trim();
                    string label = info.Mnemonic.Trim() + " (" + unit + ")";
                    double divisor = 1;
                    string reject = null;
                    if (unit.Length > 0)
                    {
                        double? d = CurveRules.Divisor(role, unit);
                        if (d == null) reject = "unit not accepted";
                        else divisor = d.Value;
                    }
                    if (reject == null && !HasValue(data.Curves[c])) reject = "no valid values";

                    if (reject != null || choice.Curve >= 0)
                    {
                        choice.Others.Add(label + ": " + (reject ?? "lower priority"));
                        continue;
                    }
                    string mismatch = null;
                    if (role == CurveRole.NeutronPorosity && divisor == 100)   // D-17 safety net, the other way round
                    {
                        double median = Median(data.Curves[c]);
                        if (median < 1)
                        {
                            divisor = 1;
                            mismatch = "labelled " + unit + " but its median is " + Math.Round(median, 3) + "; the values look like fractions, so they were not divided by 100";
                            result.Warnings.Add(role + ": " + info.Mnemonic.Trim() + " is " + mismatch);
                        }
                    }
                    choice.Curve = c;
                    choice.Mnemonic = info.Mnemonic.Trim();
                    choice.OriginalUnit = unit;
                    choice.Divisor = divisor;
                    note = label + " chosen";
                    if (unit.Length == 0) note += "; unit is empty, taken as " + CurveRules.Standard(role);
                    if (divisor != 1) note += "; values divided by " + divisor + " to get " + CurveRules.Standard(role);
                    if (mismatch != null) note += "; " + mismatch;
                }
            }

            string name = role.ToString();
            if (choice.Curve < 0) note = "no usable curve";
            else
            {
                Convert(data.Curves[choice.Curve], choice.Divisor);
                if (role == CurveRole.NeutronPorosity && choice.Divisor == 1)
                {
                    double median = Median(data.Curves[choice.Curve]);   // D-17 safety net
                    if (median > 1)
                        result.Warnings.Add(name + ": " + choice.Mnemonic + " is read as a fraction but its median is " + Math.Round(median, 3) + "; it may be percent");
                }
            }
            if (choice.Others.Count > 0) note += "; other candidates: " + string.Join(", ", choice.Others);
            result.Notes.Add(name + ": " + note);
            return choice;
        }

        private static bool HasValue(float[] values)
        {
            for (int i = 0; i < values.Length; i++)
                if (!float.IsNaN(values[i])) return true;
            return false;
        }

        // Divide, not multiply: 25 / 100 is exactly 0.25 in float, 25 * 0.01 is not.
        private static void Convert(float[] values, double divisor)
        {
            if (divisor == 1) return;
            float d = (float)divisor;
            for (int i = 0; i < values.Length; i++) values[i] /= d;   // NaN stays NaN
        }

        private static double Median(float[] values)
        {
            var copy = new List<float>(values.Length);
            for (int i = 0; i < values.Length; i++)
                if (!float.IsNaN(values[i])) copy.Add(values[i]);
            if (copy.Count == 0) return 0;
            copy.Sort();
            return copy[copy.Count / 2];
        }
    }
}
