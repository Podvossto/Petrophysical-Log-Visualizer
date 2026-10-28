using System.Linq;
using PetrophysicalLogVisualizer.Core;
using Xunit;

namespace PetrophysicalLogVisualizer.Tests
{
    public class LasDataParserTests
    {
        private static readonly float NaN = float.NaN;

        private static LasData Read(int columns, bool wrap, params string[] lines)
        {
            return LasDataParser.Parse(lines, 0, columns, wrap);
        }

        [Theory]
        [InlineData("2000.0 50.5 1.25", "2000.5 51.5 1.5")]          // spaces
        [InlineData("2000.0,50.5,1.25", "2000.5,51.5,1.5")]          // commas
        [InlineData("2000.0\t50.5\t1.25", "2000.5\t51.5\t1.5")]      // tabs
        [InlineData("  2000.0   50.5 ,  1.25 \r", "2000.5 51.5 1.5")] // mixed + CR
        public void Separators_give_the_same_table(string row1, string row2)
        {
            LasData d = Read(3, false, row1, row2);
            Assert.Equal(2, d.Rows);
            Assert.Equal(new[] { 2000.0, 2000.5 }, d.Depth);
            Assert.Equal(new[] { 50.5f, 51.5f }, d.Curves[0]);
            Assert.Equal(new[] { 1.25f, 1.5f }, d.Curves[1]);
            Assert.Empty(d.Warnings);
        }

        [Fact]
        public void Blank_lines_are_ignored()
        {
            LasData d = Read(2, false, "1 10", "", "   ", "2 20");
            Assert.Equal(2, d.Rows);
        }

        [Fact]
        public void Wrapped_data_gives_the_same_table_as_unwrapped()   // D-10
        {
            LasData flat = Read(4, false, "1 10 11 12", "2 20 21 22");
            LasData wrapped = Read(4, true, "1", "10 11", "12", "2 20 21", "22");
            Assert.Equal(flat.Rows, wrapped.Rows);
            Assert.Equal(flat.Depth, wrapped.Depth);
            for (int c = 0; c < 3; c++) Assert.Equal(flat.Curves[c], wrapped.Curves[c]);
            Assert.Empty(wrapped.Warnings);
        }

        [Fact]
        public void Header_says_no_wrap_but_data_is_wrapped_is_read_as_wrapped()   // D-12
        {
            LasData d = Read(4, false, "1", "10 11", "12", "", "2", "20 21", "22");
            Assert.Equal(2, d.Rows);
            Assert.Equal(new[] { 1.0, 2.0 }, d.Depth);
            Assert.Equal(new[] { 12f, 22f }, d.Curves[2]);
            Assert.Single(d.Warnings);
            Assert.Contains("WRAP NO", d.Warnings[0]);
        }

        [Fact]
        public void Short_first_row_is_not_mistaken_for_wrapped_data()   // D-09 vs D-12
        {
            LasData d = Read(3, false, "1 10", "2 20 21");
            Assert.Equal(2, d.Rows);
            Assert.Equal(new[] { NaN, 21f }, d.Curves[1]);
            Assert.DoesNotContain(d.Warnings, w => w.Contains("WRAP NO"));
        }

        [Fact]
        public void Normal_unwrapped_file_gets_no_wrap_warning()
        {
            LasData d = Read(3, false, "1 10 11", "2 20 21");
            Assert.Empty(d.Warnings);
        }

        [Fact]
        public void Wrapped_file_with_many_rows_on_one_line_does_not_overflow()
        {
            LasData d = Read(2, true, "1 10 2 20 3 30 4 40");
            Assert.Equal(4, d.Rows);
            Assert.Equal(new[] { 10f, 20f, 30f, 40f }, d.Curves[0]);
        }

        [Fact]
        public void Text_in_place_of_a_number_becomes_NaN_for_that_cell_only()   // D-07
        {
            LasData d = Read(3, false,
                "1 1.#INF 5", "2 -1.#IND 6", "3 (null) 7", "4 ******** 8", "5 9 10");
            Assert.Equal(5, d.Rows);
            Assert.Equal(new[] { NaN, NaN, NaN, NaN, 9f }, d.Curves[0]);
            Assert.Equal(new[] { 5f, 6f, 7f, 8f, 10f }, d.Curves[1]);   // other curve keeps its values
            Assert.Equal(4, d.BadTokens[1]);
            Assert.Single(d.Warnings);
        }

        [Fact]
        public void Short_row_keeps_its_depth_and_fills_NaN()   // D-09
        {
            LasData d = Read(4, false, "1 10 11 12", "2 20 21", "3 30 31 32");
            Assert.Equal(3, d.Rows);
            Assert.Equal(new[] { 12f, NaN, 32f }, d.Curves[2]);
            Assert.Single(d.Warnings);
        }

        [Fact]
        public void Long_row_ignores_extra_values_and_warns()   // D-09
        {
            LasData d = Read(3, false, "1 10 11 99", "2 20 21");
            Assert.Equal(2, d.Rows);
            Assert.Equal(new[] { 10f, 20f }, d.Curves[0]);
            Assert.Equal(new[] { 11f, 21f }, d.Curves[1]);
            Assert.Single(d.Warnings);
        }

        [Fact]
        public void Ctrl_Z_line_ends_the_data()   // D-06
        {
            LasData d = Read(2, false, "1 10", "2 20", "\u001A", "3 30");
            Assert.Equal(2, d.Rows);
            Assert.Single(d.Warnings);
        }

        [Fact]
        public void Comment_lines_in_the_data_section_are_skipped()   // legend of curve names, as in the masmp file
        {
            LasData d = Read(2, false, "#   DEPT   GR", "# SP    CALI", "  # indented", "1 10", "# between rows", "2 20");
            Assert.Equal(2, d.Rows);
            Assert.Equal(new[] { 1.0, 2.0 }, d.Depth);
            Assert.Equal(new[] { 10f, 20f }, d.Curves[0]);
            Assert.Equal(0, d.BadTokens.Sum());
            Assert.Empty(d.Warnings);
        }

        [Fact]
        public void Comment_line_inside_a_wrapped_row_does_not_split_it()
        {
            LasData d = Read(3, true, "1 10", "# note", "11", "2 20 21");
            Assert.Equal(2, d.Rows);
            Assert.Equal(new[] { 10f, 20f }, d.Curves[0]);
        }

        [Fact]
        public void Another_section_after_the_data_ends_it_without_garbage_rows()   // D-11
        {
            LasData d = Read(2, false, "1 10", "2 20", "~Parameter", "FOO.X 1 : x", "~Ascii", "3 30");
            Assert.Equal(2, d.Rows);
            Assert.Equal(new[] { 1.0, 2.0 }, d.Depth);
            Assert.Equal(0, d.BadTokens.Sum());
            Assert.Single(d.Warnings);
        }

        [Fact]
        public void Damaged_line_of_NULs_is_skipped_and_reported()   // D-08
        {
            LasData d = Read(2, false, "1 10", "\0\0\0\0", "3 30");
            Assert.Equal(2, d.Rows);
            Assert.Equal(new[] { 1.0, 3.0 }, d.Depth);
            Assert.Contains(d.Warnings, w => w.Contains("skipped"));
        }

        [Fact]
        public void Out_of_scale_values_are_kept()   // D-21
        {
            LasData d = Read(2, false, "1 -60.112", "2 524", "3 711.95");
            Assert.Equal(new[] { -60.112f, 524f, 711.95f }, d.Curves[0]);
            Assert.Empty(d.Warnings);
        }

        [Fact]
        public void Value_too_large_for_float_becomes_NaN()
        {
            LasData d = Read(2, false, "1 1e39");
            Assert.Equal(NaN, d.Curves[0][0]);
            Assert.Equal(1, d.BadTokens[1]);
        }

        [Fact]
        public void No_data_section_gives_empty_table_not_exception()
        {
            LasData d = LasDataParser.Parse(new string[0], -1, 3, false);
            Assert.Equal(0, d.Rows);
            Assert.Single(d.Warnings);
        }
    }
}
