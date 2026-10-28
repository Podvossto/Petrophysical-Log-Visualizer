using System.IO;
using System.Linq;
using PetrophysicalLogVisualizer.Core;
using Xunit;

namespace PetrophysicalLogVisualizer.Tests
{
    public class WellPlotModelTests : System.IDisposable
    {
        private readonly string folder = Path.Combine(Path.GetTempPath(), "wellplot_" + System.Guid.NewGuid().ToString("N"));

        public WellPlotModelTests() { Directory.CreateDirectory(folder); }
        public void Dispose() { Directory.Delete(folder, true); }

        // columns: "MNEM.UNIT" pairs after the depth; rows: depth followed by one value per column.
        private LasFileResult Load(string[] columns, params string[] rows)
        {
            string header = "~V\nVERS. 2.0 : v\nWRAP. NO : w\n~W\nSTRT.FT 1 :\nSTOP.FT 9 :\nSTEP.FT 1 :\nNULL. -999.25 :\n~C\nDEPT.FT : d\n" +
                            string.Join("", columns.Select(c => c + " : x\n")) + "~A\n";
            string path = Path.Combine(folder, "well.las");
            File.WriteAllText(path, header + string.Join("\n", rows) + "\n");
            return LasLoader.Load(path);
        }

        private static readonly string[] All = { "GR.GAPI", "RT.OHMM", "RHOB.G/CC", "NPHI.V/V" };

        [Fact]
        public void A_file_that_did_not_load_gives_no_model()
        {
            string path = Path.Combine(folder, "empty.las");
            File.WriteAllBytes(path, new byte[0]);
            Assert.Null(WellPlot.Build(LasLoader.Load(path)));
            Assert.Null(WellPlot.Build(null));
        }

        [Fact]
        public void Three_tracks_with_fixed_standard_scales_and_the_depth_range()
        {
            WellPlotModel m = WellPlot.Build(Load(All, "100 50 10 2.5 0.2", "101 60 20 2.6 0.1", "102 70 30 2.7 0.05"));
            Assert.Equal(3, m.Tracks.Length);
            Assert.Equal(new[] { 0.0, 150.0 }, new[] { m.Tracks[0].XMin, m.Tracks[0].XMax });
            Assert.Equal(new[] { 1.95, 2.95 }, new[] { m.Tracks[2].XMin, m.Tracks[2].XMax });
            Assert.True(m.Tracks[1].Log);
            Assert.Equal(System.Math.Log10(0.2), m.Tracks[1].XMin, 9);
            Assert.Equal(System.Math.Log10(2000), m.Tracks[1].XMax, 9);
            Assert.Equal(100.0, m.DepthMin);
            Assert.Equal(102.0, m.DepthMax);
            Assert.Equal("well.las", m.Title);
        }

        [Fact]
        public void Gamma_values_are_drawn_as_they_are_and_NaN_stays_a_gap()
        {
            WellPlotModel m = WellPlot.Build(Load(All, "100 50 10 2.5 0.2", "101 -999.25 20 2.6 0.1", "102 70 30 2.7 0.05"));
            double[] x = m.Tracks[0].Series.Single().X;
            Assert.Equal(50.0, x[0]);
            Assert.True(double.IsNaN(x[1]));
            Assert.Equal(70.0, x[2]);
            Assert.Equal(m.Depth.Length, x.Length);
        }

        [Fact]
        public void Resistivity_is_drawn_as_log10_and_values_at_or_below_zero_are_counted_not_drawn()
        {
            WellPlotModel m = WellPlot.Build(Load(All, "100 50 100 2.5 0.2", "101 50 0 2.5 0.2", "102 50 -3 2.5 0.2"));
            PlotTrack t = m.Tracks[1];
            double[] x = t.Series.Single().X;
            Assert.Equal(2.0, x[0], 9);
            Assert.True(double.IsNaN(x[1]) && double.IsNaN(x[2]));
            Assert.Contains("2 values <= 0 not shown", t.Messages.Single());
        }

        [Fact]
        public void Neutron_is_mapped_onto_the_density_axis_backwards()
        {
            Assert.Equal(1.95, WellPlot.NeutronToAxis(0.45), 9);
            Assert.Equal(2.95, WellPlot.NeutronToAxis(-0.15), 9);
            Assert.Equal(2.45, WellPlot.NeutronToAxis(0.15), 9);
            WellPlotModel m = WellPlot.Build(Load(All, "100 50 10 2.5 0.15", "101 50 10 2.5 0.45"));
            PlotSeries n = m.Tracks[2].Series.Single(s => s.Role == CurveRole.NeutronPorosity);
            Assert.Equal(new[] { 2.45, 1.95 }, n.X.Select(v => System.Math.Round(v, 6)).ToArray());   // curves are float, so 0.15 is not exact
        }

        [Fact]
        public void Percent_neutron_is_drawn_after_the_conversion_to_a_fraction()
        {
            WellPlotModel m = WellPlot.Build(Load(new[] { "GR.GAPI", "NPHI.PERC" }, "100 50 15", "101 50 45"));
            PlotSeries n = m.Tracks[2].Series.Single(s => s.Role == CurveRole.NeutronPorosity);
            Assert.Equal(2.45, n.X[0], 6);
            Assert.Equal(1.95, n.X[1], 6);
        }

        [Fact]
        public void Missing_curves_say_so_and_the_other_tracks_still_plot()
        {
            WellPlotModel m = WellPlot.Build(Load(new[] { "GR.GAPI", "RHOB.G/CC" }, "100 50 2.5", "101 60 2.6"));
            Assert.Equal(3, m.Tracks.Length);
            Assert.Single(m.Tracks[0].Series);
            Assert.Empty(m.Tracks[1].Series);
            Assert.Equal("No resistivity curve", m.Tracks[1].Messages.Single());
            Assert.Single(m.Tracks[2].Series);
            Assert.Equal("No neutron porosity curve", m.Tracks[2].Messages.Single());
        }

        [Fact]
        public void A_well_with_none_of_the_four_curves_still_builds()
        {
            WellPlotModel m = WellPlot.Build(Load(new[] { "SP.MV" }, "100 5", "101 6"));
            Assert.All(m.Tracks, t => Assert.Empty(t.Series));
            Assert.Equal(4, m.Tracks.Sum(t => t.Messages.Count));   // gamma, resistivity, density, neutron
        }

        [Fact]
        public void Out_of_scale_values_are_kept_so_they_run_off_the_track()   // D-21
        {
            WellPlotModel m = WellPlot.Build(Load(All, "100 711.95 10 -60.112 0.2", "101 50 20 4.6 0.1"));
            Assert.Equal(711.95, m.Tracks[0].Series.Single().X[0], 2);
            double[] rhob = m.Tracks[2].Series.Single(s => s.Role == CurveRole.BulkDensity).X;
            Assert.Equal(-60.112, rhob[0], 3);
            Assert.Equal(4.6, rhob[1], 3);
        }
    }
}
