using System.Linq;
using PetrophysicalLogVisualizer.Core;
using Xunit;

namespace PetrophysicalLogVisualizer.Tests
{
    public class MissingValuesTests
    {
        private static readonly float NaN = float.NaN;

        // Parses real text rows, so spellings like -999.2500 go through the same path as in a file.
        private static LasData Table(params string[] rows)
        {
            return LasDataParser.Parse(rows, 0, 3, false);
        }

        private static LasHeader Header(double? nullValue)
        {
            return new LasHeader { NullValue = nullValue };
        }

        [Fact]
        public void Declared_null_becomes_NaN_and_is_counted_per_curve()
        {
            LasData d = Table("1 -999.25 5", "2 7 -999.25", "3 -999.25 -999.25");
            MissingValueResult r = MissingValues.Apply(d, Header(-999.25));
            Assert.Equal(new[] { NaN, 7f, NaN }, d.Curves[0]);
            Assert.Equal(new[] { 5f, NaN, NaN }, d.Curves[1]);
            Assert.Equal(new[] { 2, 2 }, r.DeclaredNull);
            Assert.Empty(r.Warnings);
            Assert.Single(r.Notes);
        }

        [Fact]
        public void Spellings_of_the_same_number_share_one_bucket()
        {
            LasData d = Table("1 -999.2500 -999.250", "2 -999.25 5");
            MissingValueResult r = MissingValues.Apply(d, Header(-999.25));
            Assert.Equal(3, r.DeclaredNull.Sum());
        }

        [Fact]
        public void Depth_column_is_never_touched()
        {
            LasData d = Table("-999.25 1 2", "2 3 4");
            MissingValues.Apply(d, Header(-999.25));
            Assert.Equal(new[] { -999.25, 2.0 }, d.Depth);
        }

        [Fact]
        public void Null_zero_is_not_applied_and_other_markers_still_are()   // D-04 + D-16, like 4238336177MAIN
        {
            LasData d = Table("1 0.0 -999.25", "2 -999.250 3", "3 22.238 0");
            MissingValueResult r = MissingValues.Apply(d, Header(0));
            Assert.Equal(new[] { 0f, NaN, 22.238f }, d.Curves[0]);   // real zeros survive
            Assert.Equal(new[] { NaN, 3f, 0f }, d.Curves[1]);
            Assert.Equal(0, r.DeclaredNull.Sum());
            Assert.Equal(2, r.Markers.Sum(m => m[0]));               // -999.25 is marker index 0
            Assert.Equal(2, r.Warnings.Count);                       // "NULL is 0" and "undeclared marker"
            Assert.Contains(r.Warnings, w => w.Contains("Declared NULL is 0"));
        }

        [Fact]
        public void No_declared_null_warns_and_still_applies_markers()
        {
            LasData d = Table("1 -999.25 5");
            MissingValueResult r = MissingValues.Apply(d, Header(null));
            Assert.Equal(NaN, d.Curves[0][0]);
            Assert.Contains(r.Warnings, w => w.Contains("No NULL value declared"));
        }

        [Theory]
        [InlineData("-999.25")]
        [InlineData("-999")]
        [InlineData("-9999")]
        [InlineData("-99999")]
        [InlineData("-9999.9999")]   // stored as float -10000: must still be found
        [InlineData("-9999.999")]    // 9981 values in one real file
        [InlineData("-999.2")]       // -999.25 cut short: 29,110 values in one mud log that declares -999.25
        public void Each_known_marker_becomes_NaN_when_not_declared(string marker)
        {
            LasData d = Table("1 " + marker + " 5");
            MissingValueResult r = MissingValues.Apply(d, Header(-777));
            Assert.Equal(NaN, d.Curves[0][0]);
            Assert.Equal(1, r.Markers[0].Sum());
            Assert.Single(r.Warnings);
        }

        [Fact]
        public void The_two_9999_spellings_are_found_and_counted_in_separate_buckets()
        {
            LasData d = Table("1 -9999.9999 -9999.999", "2 -9999.9999 5", "3 -10000 -9999.99");
            MissingValueResult r = MissingValues.Apply(d, Header(-777));
            int four = System.Array.IndexOf(MissingValues.Markers, -9999.9999);
            int three = System.Array.IndexOf(MissingValues.Markers, -9999.999);
            Assert.NotEqual(four, three);
            Assert.Equal(3, r.Markers[0][four] + r.Markers[1][four]);   // -9999.9999 twice, plus -10000 (same float)
            Assert.Equal(1, r.Markers[0][three] + r.Markers[1][three]);
            Assert.Equal(-9999.99f, d.Curves[1][2]);                    // a different number: untouched
        }

        [Fact]
        public void The_cut_short_marker_is_a_different_number_from_the_declared_null()
        {
            LasData d = Table("1 -999.2 -999.25", "2 -999.25 -999.2");
            MissingValueResult r = MissingValues.Apply(d, Header(-999.25));
            int cut = System.Array.IndexOf(MissingValues.Markers, -999.2);
            Assert.Equal(new[] { 1, 1 }, r.DeclaredNull);                       // -999.25 is the declared NULL
            Assert.Equal(1, r.Markers[0][cut]);                                  // -999.2 is counted as a marker, apart from it
            Assert.Equal(1, r.Markers[1][cut]);
            Assert.Equal(new[] { NaN, NaN }, d.Curves[0]);
            Assert.Equal(new[] { NaN, NaN }, d.Curves[1]);
        }

        [Fact]
        public void Real_values_are_never_removed_by_range()   // D-16: no "below -90" rule
        {
            LasData d = Table("1 -270.1804 -60.112", "2 0 524", "3 -90.5 -998.9");
            MissingValueResult r = MissingValues.Apply(d, Header(-999.25));
            Assert.Equal(new[] { -270.1804f, 0f, -90.5f }, d.Curves[0]);
            Assert.Equal(new[] { -60.112f, 524f, -998.9f }, d.Curves[1]);
            Assert.Empty(r.Notes);
        }

        [Fact]
        public void Declared_null_equal_to_a_marker_is_counted_once_as_declared()
        {
            LasData d = Table("1 -999.25 5");
            MissingValueResult r = MissingValues.Apply(d, Header(-999.25));
            Assert.Equal(1, r.DeclaredNull[0]);
            Assert.Equal(0, r.Markers[0].Sum());
        }

        [Fact]
        public void Values_that_were_already_NaN_are_not_counted()
        {
            LasData d = Table("1 (null) -999.25");
            MissingValueResult r = MissingValues.Apply(d, Header(-999.25));
            Assert.Equal(1, r.DeclaredNull.Sum());
        }
    }
}
