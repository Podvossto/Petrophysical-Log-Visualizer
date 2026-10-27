using System.Linq;
using PetrophysicalLogVisualizer.Core;
using Xunit;

namespace PetrophysicalLogVisualizer.Tests
{
    public class DepthConditioningTests
    {
        private static readonly float NaN = float.NaN;

        // Two columns: depth and one curve whose value is depth * 10, so a moved row is easy to spot. "x" = NaN curve value.
        private static LasData Table(params string[] rows)
        {
            return LasDataParser.Parse(rows, 0, 2, false);
        }

        private static LasHeader Header(double? step)
        {
            return new LasHeader { Step = step };
        }

        [Fact]
        public void Ascending_table_is_left_alone()
        {
            LasData d = Table("1 10", "2 20", "3 30");
            DepthResult r = DepthConditioning.Apply(d, Header(1));
            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, d.Depth);
            Assert.False(r.Reversed || r.Sorted);
            Assert.Equal(0, r.DuplicatesDropped);
            Assert.Empty(r.Notes);
            Assert.Empty(r.Warnings);
        }

        [Fact]
        public void Descending_table_is_reversed_as_a_block_and_values_stay_with_their_depth()   // D-14
        {
            LasData d = Table("3 30", "2 20", "1 10");
            DepthResult r = DepthConditioning.Apply(d, Header(-1));
            Assert.True(r.Reversed);
            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, d.Depth);
            Assert.Equal(new[] { 10f, 20f, 30f }, d.Curves[0]);
            Assert.Single(r.Notes);
            Assert.Empty(r.Warnings);   // negative header STEP is the same step
        }

        [Fact]
        public void Jump_back_is_sorted_stably_and_the_moved_rows_are_counted()   // D-14, STATE_AQ_8_RUN2 shape
        {
            LasData d = Table("5 50", "6 60", "7 70", "4 40", "5 51");
            DepthResult r = DepthConditioning.Apply(d, Header(1));
            Assert.True(r.Sorted);
            Assert.Equal(new[] { 4.0, 5.0, 6.0, 7.0 }, d.Depth);
            Assert.Equal(new[] { 40f, 50f, 60f, 70f }, d.Curves[0]);   // the first depth-5 row (50) wins
            Assert.Equal(1, r.DuplicatesDropped);
            Assert.Equal(5, r.RowsMoved);   // sorted order 4,5,5,6,7 is rows 3,0,4,1,2: no row stays in place
            Assert.Contains(r.Warnings, w => w.Contains("sorted"));
        }

        [Fact]
        public void Duplicate_depths_keep_the_first_row_and_are_counted()   // D-14, F3
        {
            LasData d = Table("1 10", "2 20", "2 99", "2 98", "3 30");
            DepthResult r = DepthConditioning.Apply(d, Header(1));
            Assert.Equal(3, d.Rows);
            Assert.Equal(new[] { 10f, 20f, 30f }, d.Curves[0]);
            Assert.Equal(2, r.DuplicatesDropped);
            Assert.Equal(3, d.Curves[0].Length);
        }

        [Fact]
        public void Duplicates_in_a_descending_table_also_keep_the_first_row_of_the_file()
        {
            LasData d = Table("3 30", "2 20", "2 99", "1 10");
            DepthResult r = DepthConditioning.Apply(d, Header(-1));
            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, d.Depth);
            Assert.Equal(new[] { 10f, 20f, 30f }, d.Curves[0]);
            Assert.Equal(1, r.DuplicatesDropped);
        }

        [Fact]
        public void NaN_values_travel_with_their_row()
        {
            LasData d = Table("3 x", "2 20", "1 10");
            DepthConditioning.Apply(d, Header(1));
            Assert.Equal(new[] { 10f, 20f, NaN }, d.Curves[0]);
        }

        [Fact]
        public void Wrong_header_step_reports_both_numbers()   // D-15, like 4200340125MAIN
        {
            LasData d = Table("1 0", "1.5 0", "2 0", "2.5 0", "3 0");
            DepthResult r = DepthConditioning.Apply(d, Header(0.75));
            Assert.Equal(0.5, r.RealStep);
            Assert.Single(r.Warnings);
            Assert.Contains("0.75", r.Warnings[0]);
            Assert.Contains("0.5", r.Warnings[0]);
        }

        [Fact]
        public void Zero_header_step_is_a_warning_and_a_missing_one_is_a_note()
        {
            DepthResult zero = DepthConditioning.Apply(Table("1 0", "2 0", "3 0"), Header(0));
            Assert.Single(zero.Warnings);
            DepthResult none = DepthConditioning.Apply(Table("1 0", "2 0", "3 0"), Header(null));
            Assert.Empty(none.Warnings);
            Assert.Single(none.Notes);
            Assert.Equal(1.0, none.RealStep);
        }

        [Fact]
        public void Gaps_are_counted_and_not_filled()
        {
            LasData d = Table("1 10", "2 20", "3 30", "7 70", "8 80", "9 90");
            DepthResult r = DepthConditioning.Apply(d, Header(1));
            Assert.Equal(1, r.Gaps);
            Assert.Equal(4.0, r.LargestGap);
            Assert.Equal(3.0, r.FirstGapDepth);
            Assert.Equal(6, d.Rows);   // no resampling
            Assert.Single(r.Warnings);
        }

        [Fact]
        public void One_row_and_empty_tables_do_not_throw()
        {
            Assert.Null(DepthConditioning.Apply(Table("1 10"), Header(1)).RealStep);
            Assert.Null(DepthConditioning.Apply(Table(), Header(1)).RealStep);
        }

        [Fact]
        public void Loader_reports_depth_problems_in_the_issues_list()
        {
            string path = System.IO.Path.GetTempFileName();
            try
            {
                System.IO.File.WriteAllText(path,
                    "~V\nVERS. 2.0 : v\nWRAP. NO : w\n~W\nSTRT.FT 1 :\nSTOP.FT 3 :\nSTEP.FT 1 :\nNULL. -999.25 :\n" +
                    "~C\nDEPT.FT : d\nGR.GAPI : g\n~A\n3 30\n2 20\n2 21\n1 10\n");
                LasFileResult r = LasLoader.Load(path);
                Assert.False(r.Failed);
                Assert.Equal(new[] { 1.0, 2.0, 3.0 }, r.Data.Depth);
                Assert.True(r.Depth.Reversed);
                Assert.Contains(r.Issues, i => i.Contains("repeat an earlier depth"));
            }
            finally { System.IO.File.Delete(path); }
        }
    }
}
