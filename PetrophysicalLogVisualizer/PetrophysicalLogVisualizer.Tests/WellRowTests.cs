using System.IO;
using PetrophysicalLogVisualizer.App;
using PetrophysicalLogVisualizer.Core;
using Xunit;

namespace PetrophysicalLogVisualizer.Tests
{
    /// <summary>The remark and the size shown next to each well in the list.</summary>
    public class WellRowTests : System.IDisposable
    {
        private readonly string folder = Path.Combine(Path.GetTempPath(), "wellrow_" + System.Guid.NewGuid().ToString("N"));

        public WellRowTests() { Directory.CreateDirectory(folder); }
        public void Dispose() { Directory.Delete(folder, true); }

        private WellRow Row(string curves, string data)
        {
            string path = Path.Combine(folder, "w.las");
            File.WriteAllText(path, "~V\nVERS. 2.0 : v\nWRAP. NO : w\n~W\nSTRT.FT 1 :\nSTOP.FT 3 :\nSTEP.FT 1 :\nNULL. -999.25 :\n~C\nDEPT.FT : d\n" + curves + "~A\n" + data);
            return WellRow.From(LasLoader.Load(path));
        }

        [Fact]
        public void One_of_four_curves_is_said_in_the_note()
        {
            WellRow r = Row("GR.GAPI : g\n", "1 50\n2 60\n3 70\n");
            Assert.Equal(1, r.CurvesFound);
            Assert.Equal("1/4 curves", r.Note);
        }

        [Fact]
        public void A_readable_file_with_none_of_the_four_curves_says_no_curves_though_its_status_is_OK()
        {
            WellRow r = Row("MUD.UNIT : m\n", "1 5\n2 6\n3 7\n");
            Assert.Equal("OK", r.Status);
            Assert.Equal("no curves", r.Note);
        }

        [Fact]
        public void A_file_that_did_not_load_says_unreadable()
        {
            WellRow r = Row("GR.GAPI : g\n", "");
            Assert.Equal("FAILED", r.Status);
            Assert.Equal("unreadable", r.Note);
        }

        [Fact]
        public void Size_is_shown_in_KB_below_one_MB()
        {
            Assert.Equal("1 KB", Row("GR.GAPI : g\n", "1 50\n2 60\n3 70\n").Size);
        }
    }
}
