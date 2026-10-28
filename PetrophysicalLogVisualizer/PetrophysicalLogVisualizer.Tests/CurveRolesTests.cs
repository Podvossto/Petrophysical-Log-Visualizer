using System.Linq;
using PetrophysicalLogVisualizer.Core;
using Xunit;

namespace PetrophysicalLogVisualizer.Tests
{
    public class CurveRolesTests
    {
        private static readonly float NaN = float.NaN;

        // curves: "MNEMONIC|UNIT" pairs; values: one array per curve, same order. The depth column is added here.
        private static RoleResult Run(string[] curves, params float[][] values)
        {
            var header = new LasHeader();
            header.Curves.Add(new CurveInfo("DEPT", "FT", ""));
            foreach (string c in curves)
            {
                string[] p = c.Split('|');
                header.Curves.Add(new CurveInfo(p[0], p[1], ""));
            }
            var data = new LasData { Depth = new double[values[0].Length], Curves = values, Rows = values[0].Length };
            data.BadTokens = new int[curves.Length + 1];
            lastData = data;
            return CurveRoles.Apply(data, header);
        }

        private static LasData lastData;

        private static RoleChoice Pick(RoleResult r, CurveRole role) { return r.Roles[(int)role]; }

        [Fact]
        public void Alias_is_matched_by_mnemonic_and_the_original_name_is_kept()
        {
            RoleResult r = Run(new[] { "SP|MV", "HGR|GAPI" }, new[] { 1f }, new[] { 50f });
            RoleChoice gr = Pick(r, CurveRole.GammaRay);
            Assert.Equal(1, gr.Curve);
            Assert.Equal("HGR", gr.Mnemonic);
        }

        [Fact]
        public void Mnemonic_and_unit_are_compared_ignoring_case()   // real files: gAPI, api, ohmm, g/cc
        {
            RoleResult r = Run(new[] { "gr|gAPI", "rt|ohmm", "rhob|g/cc" }, new[] { 1f }, new[] { 2f }, new[] { 2.5f });
            Assert.Equal(0, Pick(r, CurveRole.GammaRay).Curve);
            Assert.Equal(1, Pick(r, CurveRole.Resistivity).Curve);
            Assert.Equal(2, Pick(r, CurveRole.BulkDensity).Curve);
        }

        [Fact]
        public void Two_gamma_candidates_give_one_chosen_and_the_other_listed()   // D-19
        {
            RoleResult r = Run(new[] { "GR_EDTC|GAPI", "GR|GAPI" }, new[] { 1f }, new[] { 2f });
            RoleChoice gr = Pick(r, CurveRole.GammaRay);
            Assert.Equal("GR", gr.Mnemonic);               // GR comes first in the priority list
            Assert.Single(gr.Others);
            Assert.Contains("GR_EDTC", gr.Others[0]);
            Assert.Contains("other candidates", r.Notes.First(n => n.StartsWith("GammaRay")));
        }

        [Fact]
        public void Gamma_alias_with_a_wrong_unit_is_not_chosen()   // a real file has GAMMA with unit "u"
        {
            RoleResult r = Run(new[] { "GAMMA|u" }, new[] { 1f });
            Assert.Equal(-1, Pick(r, CurveRole.GammaRay).Curve);
            Assert.Contains("unit not accepted", Pick(r, CurveRole.GammaRay).Others[0]);
        }

        [Fact]
        public void Percent_neutron_is_divided_by_100()   // D-17
        {
            RoleResult r = Run(new[] { "NPHI|PERC" }, new[] { 25f, NaN, 12.5f });
            Assert.Equal(new[] { 0.25f, NaN, 0.125f }, lastData.Curves[0]);
            Assert.Equal(100, Pick(r, CurveRole.NeutronPorosity).Divisor);
            Assert.Contains("divided by 100", r.Notes.First(n => n.StartsWith("NeutronPorosity")));
        }

        [Theory]
        [InlineData("CFCF")]
        [InlineData("V/V")]
        [InlineData("FRAC")]
        [InlineData("decp")]
        public void Fraction_neutron_is_kept_as_it_is(string unit)   // D-17: CFCF is a fraction in the real data
        {
            RoleResult r = Run(new[] { "TNPH|" + unit }, new[] { 0.18f, 0.2f });
            Assert.Equal(new[] { 0.18f, 0.2f }, lastData.Curves[0]);
            Assert.Equal(1, Pick(r, CurveRole.NeutronPorosity).Divisor);
            Assert.Empty(r.Warnings);
        }

        [Fact]
        public void Count_rate_is_never_chosen_as_neutron_porosity()   // D-17: CNLSC with CPS
        {
            RoleResult r = Run(new[] { "CNLSC|CPS", "NPHI|CFCF" }, new[] { 333f }, new[] { 0.2f });
            RoleChoice n = Pick(r, CurveRole.NeutronPorosity);
            Assert.Equal("NPHI", n.Mnemonic);
            Assert.Equal(new[] { 333f }, lastData.Curves[0]);   // untouched
            Assert.Contains("CNLSC (CPS): unit not accepted", n.Others);
        }

        [Fact]
        public void Only_a_count_rate_leaves_the_role_empty_without_an_exception()
        {
            RoleResult r = Run(new[] { "CNLSC|CPS" }, new[] { 333f });
            Assert.Equal(-1, Pick(r, CurveRole.NeutronPorosity).Curve);
            Assert.Contains("no usable curve", r.Notes.First(n => n.StartsWith("NeutronPorosity")));
        }

        [Fact]
        public void Fraction_with_a_median_above_one_gets_a_warning_and_is_not_converted()   // D-17 safety net
        {
            RoleResult r = Run(new[] { "NPHI|V/V" }, new[] { 18f, 20f, 22f });
            Assert.Single(r.Warnings);
            Assert.Equal(new[] { 18f, 20f, 22f }, lastData.Curves[0]);
        }

        [Theory]
        [InlineData("G/C3", 2.65f, 2.65f)]
        [InlineData("G/CC", 2.65f, 2.65f)]
        [InlineData("G/CM3", 2.65f, 2.65f)]
        [InlineData("K/M3", 2650f, 2.65f)]   // D-18
        public void Density_units_end_up_in_g_per_cc(string unit, float value, float expected)
        {
            RoleResult r = Run(new[] { "RHOZ|" + unit }, new[] { value, NaN });
            Assert.Equal(new[] { expected, NaN }, lastData.Curves[0]);
        }

        [Fact]
        public void DPHI_is_not_chosen_as_bulk_density()   // D-18
        {
            RoleResult r = Run(new[] { "DPHI|CFCF" }, new[] { 0.12f });
            Assert.Equal(-1, Pick(r, CurveRole.BulkDensity).Curve);
        }

        [Theory]
        [InlineData("OHMM")]
        [InlineData("OHM-M")]
        [InlineData("ohm.m")]
        public void Resistivity_unit_spellings_are_accepted(string unit)   // D-20
        {
            RoleResult r = Run(new[] { "LLD|" + unit }, new[] { 20f });
            Assert.Equal(0, Pick(r, CurveRole.Resistivity).Curve);
        }

        [Fact]
        public void Empty_unit_is_accepted_with_a_note()   // D-20, RLLD and RLLS
        {
            RoleResult r = Run(new[] { "RLLD|" }, new[] { 20f });
            Assert.Equal(0, Pick(r, CurveRole.Resistivity).Curve);
            Assert.Contains("unit is empty", r.Notes.First(n => n.StartsWith("Resistivity")));
        }

        [Fact]
        public void Deep_resistivity_wins_over_shallow_and_micro()   // D-20
        {
            RoleResult r = Run(new[] { "MSFL|OHMM", "DSLL|OHMM", "DDLL|OHMM" }, new[] { 1f }, new[] { 2f }, new[] { 3f });
            Assert.Equal("DDLL", Pick(r, CurveRole.Resistivity).Mnemonic);
            Assert.Equal(2, Pick(r, CurveRole.Resistivity).Others.Count);
        }

        [Fact]
        public void Candidate_with_only_NaN_is_skipped_for_the_next_one()
        {
            RoleResult r = Run(new[] { "GR|GAPI", "HGR|GAPI" }, new[] { NaN, NaN }, new[] { 50f, 60f });
            Assert.Equal("HGR", Pick(r, CurveRole.GammaRay).Mnemonic);
            Assert.Contains("no valid values", Pick(r, CurveRole.GammaRay).Others[0]);
        }

        [Fact]
        public void A_well_without_any_of_the_four_curves_gives_four_empty_roles()
        {
            RoleResult r = Run(new[] { "SP|MV" }, new[] { 1f });
            Assert.All(r.Roles, c => Assert.Equal(-1, c.Curve));
            Assert.Equal(4, r.Notes.Count);
        }
    }
}
