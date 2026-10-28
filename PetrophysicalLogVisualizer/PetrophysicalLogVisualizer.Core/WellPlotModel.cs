using System;
using System.Collections.Generic;
using System.Globalization;

namespace PetrophysicalLogVisualizer.Core
{
    /// <summary>One curve to draw in a track. X lines up with WellPlotModel.Depth; NaN means "no reading here" and is drawn as a gap, never joined.</summary>
    public sealed class PlotSeries
    {
        public CurveRole Role { get; set; }
        public string Label { get; set; }
        public double[] X { get; set; }
    }

    public sealed class PlotTrack
    {
        public string Title { get; set; }
        /// <summary>Which curve was drawn and on which fixed scale, for the axis label.</summary>
        public string AxisLabel { get; set; }
        public double XMin { get; set; }
        public double XMax { get; set; }
        /// <summary>True when X holds log10 values; XMin and XMax are log10 too.</summary>
        public bool Log { get; set; }
        public List<PlotSeries> Series { get; } = new List<PlotSeries>();
        /// <summary>Text for the track: "No ... curve", or how many values a log scale could not show.</summary>
        public List<string> Messages { get; } = new List<string>();
    }

    /// <summary>What to draw for one well (F7): 3 tracks on one depth axis, fixed standard scales. The drawing itself is in the App project.</summary>
    public sealed class WellPlotModel
    {
        public string Title { get; set; }
        public double[] Depth { get; set; }
        public double DepthMin { get; set; }
        public double DepthMax { get; set; }
        /// <summary>Gamma ray, resistivity, density with neutron porosity.</summary>
        public PlotTrack[] Tracks { get; set; }
    }

    public static class WellPlot
    {
        // Fixed standard scales (D-21): values outside run off the track and are never removed.
        public const double GrMin = 0, GrMax = 150;
        public const double ResMin = 0.2, ResMax = 2000;
        public const double RhobMin = 1.95, RhobMax = 2.95;
        public const double NphiLeft = 0.45, NphiRight = -0.15;   // neutron runs backwards: 0.45 at the left edge

        /// <summary>Null when the file did not load. A well without one of the curves still gets all 3 tracks, the missing one says so.</summary>
        public static WellPlotModel Build(LasFileResult r)
        {
            if (r == null || r.Failed || r.Data == null || r.Roles == null || r.Data.Rows == 0) return null;
            var m = new WellPlotModel
            {
                Title = System.IO.Path.GetFileName(r.Path),
                Depth = r.Data.Depth,
                DepthMin = r.Data.Depth[0],
                DepthMax = r.Data.Depth[r.Data.Rows - 1],
                Tracks = new PlotTrack[3]
            };

            var gr = new PlotTrack { Title = "Gamma ray", XMin = GrMin, XMax = GrMax };
            AddSeries(gr, r, CurveRole.GammaRay, "No gamma ray curve", v => v);
            gr.AxisLabel = Label(r, CurveRole.GammaRay, "GR", GrMin + " - " + GrMax + " GAPI");

            var res = new PlotTrack { Title = "Resistivity", XMin = Math.Log10(ResMin), XMax = Math.Log10(ResMax), Log = true };
            int hidden = AddSeries(res, r, CurveRole.Resistivity, "No resistivity curve", v => v > 0 ? Math.Log10(v) : double.NaN);
            if (hidden > 0) res.Messages.Add(hidden + " values <= 0 not shown (log scale)");
            res.AxisLabel = Label(r, CurveRole.Resistivity, "RES", ResMin + " - " + ResMax + " ohm.m (log)");

            // Both curves share one axis: neutron is mapped onto the density scale so that 0.45 sits at the left edge and -0.15 at the right.
            var den = new PlotTrack { Title = "Density / neutron", XMin = RhobMin, XMax = RhobMax };
            AddSeries(den, r, CurveRole.BulkDensity, "No density curve", v => v);
            AddSeries(den, r, CurveRole.NeutronPorosity, "No neutron porosity curve", NeutronToAxis);
            den.AxisLabel = Label(r, CurveRole.BulkDensity, "RHOB", RhobMin + " - " + RhobMax + " g/cc") + "  |  " +
                            Label(r, CurveRole.NeutronPorosity, "NPHI", NphiLeft.ToString(CultureInfo.InvariantCulture) + " to " + NphiRight.ToString(CultureInfo.InvariantCulture) + " v/v");

            m.Tracks[0] = gr; m.Tracks[1] = res; m.Tracks[2] = den;
            return m;
        }

        /// <summary>Neutron value on the density axis: 0.45 gives 1.95, -0.15 gives 2.95.</summary>
        public static double NeutronToAxis(double nphi)
        {
            return RhobMin + (NphiLeft - nphi) * (RhobMax - RhobMin) / (NphiLeft - NphiRight);
        }

        // Returns how many real values the transform turned into NaN (log of a value <= 0).
        private static int AddSeries(PlotTrack track, LasFileResult r, CurveRole role, string missing, Func<double, double> map)
        {
            RoleChoice c = r.Roles.Roles[(int)role];
            if (c.Curve < 0) { track.Messages.Add(missing); return 0; }
            float[] values = r.Data.Curves[c.Curve];
            var x = new double[r.Data.Rows];
            int dropped = 0;
            for (int i = 0; i < x.Length; i++)
            {
                if (float.IsNaN(values[i])) { x[i] = double.NaN; continue; }
                x[i] = map(values[i]);
                if (double.IsNaN(x[i])) dropped++;
            }
            track.Series.Add(new PlotSeries { Role = role, Label = c.Mnemonic, X = x });
            return dropped;
        }

        private static string Label(LasFileResult r, CurveRole role, string fallback, string scale)
        {
            RoleChoice c = r.Roles.Roles[(int)role];
            return (c.Curve >= 0 ? c.Mnemonic : fallback) + " " + scale;
        }
    }
}
