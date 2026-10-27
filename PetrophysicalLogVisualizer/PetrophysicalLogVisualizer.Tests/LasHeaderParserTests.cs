using System.Globalization;
using PetrophysicalLogVisualizer.Core;
using Xunit;

namespace PetrophysicalLogVisualizer.Tests
{
    public class LasHeaderParserTests
    {
        private const string Tiny =
            "~VERSION INFORMATION\n" +
            " VERS.   2.00 : CWLS LOG ASCII STANDARD\n" +
            " WRAP.   No   : One line per depth step\n" +
            "~WELL INFORMATION\n" +
            " STRT.FT   2000.0 : First depth\n" +
            " STOP.FT   2001.0 : Last depth\n" +
            " STEP.FT      0.5 : Step\n" +
            " NULL.   -999.2500 : Missing value\n" +
            " WELL.   TEST 1 : Well name: with a colon\n" +
            "~CURVE INFORMATION\n" +
            " DEPT.FT      : Depth\n" +
            " GR  .GAPI    : Gamma ray\n" +
            " Bit IN.e     : MM\n" +
            "~A\n" +
            "2000.0 50.0 1.0\n";

        private static LasHeader Parse(string text, out int dataStart)
        {
            return LasHeaderParser.Parse(text.Split('\n'), out dataStart);
        }

        private static double Num(string text)
        {
            return double.Parse(text, CultureInfo.InvariantCulture);
        }

        [Fact]
        public void Tiny_file_header_is_read()
        {
            int dataStart;
            LasHeader h = Parse(Tiny, out dataStart);
            Assert.Equal(2.0, h.Version);
            Assert.False(h.Wrap);
            Assert.Equal(2000.0, h.Strt);
            Assert.Equal(2001.0, h.Stop);
            Assert.Equal(0.5, h.Step);
            Assert.Equal(-999.25, h.NullValue);
            Assert.Equal(14, dataStart);
            Assert.Empty(h.Warnings);
        }

        [Fact]
        public void Curves_keep_mnemonic_unit_and_description()
        {
            int dataStart;
            LasHeader h = Parse(Tiny, out dataStart);
            Assert.Equal(3, h.Curves.Count);
            Assert.Equal("DEPT", h.Curves[0].Mnemonic);
            Assert.Equal("FT", h.Curves[0].Unit);
            Assert.Equal("GR", h.Curves[1].Mnemonic);
            Assert.Equal("GAPI", h.Curves[1].Unit);
            Assert.Equal("Bit IN", h.Curves[2].Mnemonic);   // D-05: a mnemonic with a space is still a curve
        }

        [Fact]
        public void Crlf_and_lf_give_the_same_result()
        {
            int a, b;
            LasHeader lf = Parse(Tiny, out a);
            LasHeader crlf = Parse(Tiny.Replace("\n", "\r\n"), out b);
            Assert.Equal(a, b);
            Assert.Equal(lf.NullValue, crlf.NullValue);
            Assert.Equal(lf.Curves.Count, crlf.Curves.Count);
        }

        [Theory]
        [InlineData("-999.25")]
        [InlineData("-999.2500")]
        [InlineData("-999.250")]
        [InlineData("-.9999")]
        public void Null_spellings_parse_as_numbers(string text)
        {
            int dataStart;
            LasHeader h = Parse("~W\n NULL. " + text + " : x\n~A\n", out dataStart);
            Assert.Equal(Num(text), h.NullValue);
        }

        [Theory]
        [InlineData("2.0")]
        [InlineData("2.00")]
        [InlineData("1.2")]
        public void Version_spellings(string text)
        {
            int dataStart;
            LasHeader h = Parse("~V\n VERS. " + text + " : x\n~A\n", out dataStart);
            Assert.Equal(Num(text), h.Version);
        }

        [Theory]
        [InlineData("NO", false)]
        [InlineData("No", false)]
        [InlineData("yes", true)]
        [InlineData("YES", true)]
        public void Wrap_is_case_insensitive(string text, bool expected)
        {
            int dataStart;
            LasHeader h = Parse("~V\n WRAP. " + text + " : x\n~A\n", out dataStart);
            Assert.Equal(expected, h.Wrap);
        }

        [Fact]
        public void Bad_value_becomes_warning_not_exception()
        {
            int dataStart;
            LasHeader h = Parse("~W\n STEP.FT abc : x\n~A\n", out dataStart);
            Assert.Null(h.Step);
            Assert.Single(h.Warnings);
        }

        [Fact]
        public void Missing_data_section_is_reported()
        {
            int dataStart;
            LasHeader h = Parse("~W\n STRT.FT 1 : x\n", out dataStart);
            Assert.Equal(-1, dataStart);
            Assert.Single(h.Warnings);
        }

        [Fact]
        public void Sections_after_the_first_data_section_are_ignored()
        {
            int dataStart;
            LasHeader h = Parse("~C\n DEPT.FT : d\n~A\n1 2\n~Lithology_Data\n~C\n GR.GAPI : x\n", out dataStart);
            Assert.Single(h.Curves);
        }
    }
}
