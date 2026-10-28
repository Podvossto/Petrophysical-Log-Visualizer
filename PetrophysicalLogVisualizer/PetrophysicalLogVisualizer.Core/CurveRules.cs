using System;

namespace PetrophysicalLogVisualizer.Core
{
    public enum CurveRole { GammaRay, Resistivity, BulkDensity, NeutronPorosity }

    /// <summary>
    /// The only place where a mnemonic or a unit label is written (D-13, D-17 to D-20).
    /// Units are compared ignoring case: the real files write GAPI, gAPI and api for the same thing.
    /// </summary>
    public static class CurveRules
    {
        // Priority order: the first alias that has an accepted unit and some values wins; the rest are listed in the report.
        private static readonly string[] Gamma = { "GR", "GR_EDTC", "HGR", "GRGC", "GRSG", "GAMMA", "SGR", "CGR", "GRTO", "GRKT", "GRGM", "GRS", "HSGR", "HCGR" };
        // Deep readings first, then medium and shallow, then the micro curve (D-20).
        private static readonly string[] Resistivity =
            { "RT_HRLT", "RT", "RT90", "AT90", "DDLL", "LLD", "LLDC", "RLLD", "RILD", "ILD", "RD",
              "ILM", "RILM", "DSLL", "LLS", "LLSC", "RLLS", "RS", "MSFL" };
        private static readonly string[] Density = { "RHOB", "RHOZ", "RHO8", "DEN" };   // DPHI is a porosity, never here (D-18)
        private static readonly string[] Neutron = { "NPHI", "TNPH", "NPOR", "NPL", "CNLS", "CNLSC" };

        public static string[] Aliases(CurveRole role)
        {
            switch (role)
            {
                case CurveRole.GammaRay: return Gamma;
                case CurveRole.Resistivity: return Resistivity;
                case CurveRole.BulkDensity: return Density;
                default: return Neutron;
            }
        }

        public static string Standard(CurveRole role)
        {
            switch (role)
            {
                case CurveRole.GammaRay: return "GAPI";
                case CurveRole.Resistivity: return "ohm.m";
                case CurveRole.BulkDensity: return "g/cc";
                default: return "v/v";
            }
        }

        /// <summary>
        /// Usual range of the median of a whole curve, in the standard unit. Wide on purpose: it only catches a curve that cannot be
        /// what its name says (a gamma ray that is all 0, a density of 0.19 g/cc). Null for neutron (it has its own percent/fraction
        /// checks) and for resistivity (it spans too many decades to say anything).
        /// </summary>
        public static double[] MedianRange(CurveRole role)
        {
            switch (role)
            {
                case CurveRole.GammaRay: return new[] { 1.0, 500.0 };
                case CurveRole.BulkDensity: return new[] { 1.0, 3.6 };
                default: return null;
            }
        }

        /// <summary>Number to divide the values by to reach Standard(role); null when the unit is not accepted for this role (a count rate is not a porosity).</summary>
        public static double? Divisor(CurveRole role, string unit)
        {
            string u = unit.Trim().ToUpperInvariant();
            switch (role)
            {
                case CurveRole.GammaRay:
                    return In(u, "GAPI", "API") ? 1 : (double?)null;
                case CurveRole.Resistivity:
                    return In(u, "OHMM", "OHM-M", "OHM.M") ? 1 : (double?)null;
                case CurveRole.BulkDensity:
                    if (In(u, "G/C3", "G/CC", "G/CM3")) return 1;
                    return In(u, "K/M3", "KG/M3") ? 1000 : (double?)null;
                default:
                    if (In(u, "V/V", "FRAC", "DECP", "DECI", "CFCF", "FT3/FT3")) return 1;   // fractions
                    return In(u, "PERC", "PU", "%") ? 100 : (double?)null;                    // percent
            }
        }

        /// <summary>D-13: feet divisor is 1, metres is 0.3048; null when the label is not a known depth unit.</summary>
        public static double? DepthDivisor(string unit)
        {
            string u = unit.Trim().ToUpperInvariant();
            if (In(u, "F", "FT", "FEET")) return 1;
            return In(u, "M", "METRE", "METRES", "METER", "METERS") ? 0.3048 : (double?)null;
        }

        private static bool In(string value, params string[] list)
        {
            foreach (string item in list)
                if (value == item) return true;
            return false;
        }
    }
}
