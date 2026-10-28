using PetrophysicalLogVisualizer.Core;
using Xunit;

namespace PetrophysicalLogVisualizer.Tests
{
    public class DepthUnitsTests
    {
        private static LasHeader Header(string unit)
        {
            var h = new LasHeader { Strt = 100, Stop = 200, Step = 0.5 };
            h.Curves.Add(new CurveInfo("DEPT", unit, ""));
            return h;
        }

        private static LasData Data(params double[] depth)
        {
            return new LasData { Depth = depth, Curves = new float[0][], Rows = depth.Length };
        }

        [Theory]
        [InlineData("F")]
        [InlineData("FT")]
        [InlineData("ft")]
        [InlineData("Ft")]
        public void Feet_spellings_are_left_alone(string unit)   // D-13
        {
            LasData d = Data(1000, 1000.5);
            DepthUnitResult r = DepthUnits.Apply(d, Header(unit));
            Assert.Equal(new[] { 1000.0, 1000.5 }, d.Depth);
            Assert.Empty(r.Notes);
            Assert.Empty(r.Warnings);
        }

        [Fact]
        public void Metres_are_converted_to_feet_and_the_header_numbers_follow()   // D-13, from the spec only
        {
            LasData d = Data(304.8, 609.6);
            LasHeader h = Header("M");
            DepthUnitResult r = DepthUnits.Apply(d, h);
            Assert.Equal(1000.0, d.Depth[0], 6);
            Assert.Equal(2000.0, d.Depth[1], 6);
            Assert.Equal(100 / 0.3048, h.Strt.Value, 6);
            Assert.Equal(0.5 / 0.3048, h.Step.Value, 6);
            Assert.Single(r.Notes);
            Assert.Empty(r.Warnings);
        }

        [Fact]
        public void Missing_or_unknown_unit_is_a_warning_and_the_depth_stays()
        {
            LasData d = Data(5, 6);
            Assert.Single(DepthUnits.Apply(d, Header("")).Warnings);
            Assert.Single(DepthUnits.Apply(d, Header("XYZ")).Warnings);
            Assert.Equal(new[] { 5.0, 6.0 }, d.Depth);
        }

        [Fact]
        public void Header_without_curves_does_not_throw()
        {
            DepthUnitResult r = DepthUnits.Apply(Data(1), new LasHeader());
            Assert.Single(r.Warnings);
        }
    }
}
