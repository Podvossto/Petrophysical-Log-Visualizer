using System.IO;
using PetrophysicalLogVisualizer.App;
using PetrophysicalLogVisualizer.Core;
using Xunit;

namespace PetrophysicalLogVisualizer.Tests
{
    public class PlotExporterTests : System.IDisposable
    {
        private readonly string folder = Path.Combine(Path.GetTempPath(), "plotexp_" + System.Guid.NewGuid().ToString("N"));

        public PlotExporterTests() { Directory.CreateDirectory(folder); }
        public void Dispose() { Directory.Delete(folder, true); }

        private LasFileResult Load(string name, string text)
        {
            string path = Path.Combine(folder, name);
            File.WriteAllText(path, text);
            return LasLoader.Load(path);
        }

        private const string Good =
            "~V\nVERS. 2.0 : v\nWRAP. NO : w\n~W\nSTRT.FT 1 :\nSTOP.FT 3 :\nSTEP.FT 1 :\nNULL. -999.25 :\n" +
            "~C\nDEPT.FT : d\nGR.GAPI : g\n~A\n1 50\n2 60\n3 70\n";

        private static int BigEndian(byte[] b, int at) { return (b[at] << 24) | (b[at + 1] << 16) | (b[at + 2] << 8) | b[at + 3]; }

        [Fact]
        public void A_loaded_well_gives_a_png_named_after_the_file_with_the_expected_size()
        {
            string output = Path.Combine(folder, "plots");
            PlotExportResult r = PlotExporter.Save(Load("well_1.las", Good), output);
            Assert.Null(r.Error);
            Assert.Equal(Path.Combine(output, "well_1.png"), r.Png);
            byte[] bytes = File.ReadAllBytes(r.Png);
            Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, new[] { bytes[0], bytes[1], bytes[2], bytes[3] });   // PNG signature
            Assert.Equal(PlotExporter.Width, BigEndian(bytes, 16));    // IHDR width
            Assert.Equal(PlotExporter.Height, BigEndian(bytes, 20));   // IHDR height
        }

        [Fact]
        public void A_file_that_did_not_load_gives_no_png_and_a_reason()
        {
            string output = Path.Combine(folder, "plots");
            PlotExportResult r = PlotExporter.Save(Load("bad.las", ""), output);
            Assert.Null(r.Png);
            Assert.Contains("did not load", r.Error);
            Assert.False(File.Exists(Path.Combine(output, "bad.png")));
        }

        [Fact]
        public void An_output_folder_that_cannot_be_used_is_an_error_not_an_exception()
        {
            string blocker = Path.Combine(folder, "blocker");
            File.WriteAllText(blocker, "a file where the output folder should be");
            PlotExportResult r = PlotExporter.Save(Load("w.las", Good), blocker);
            Assert.Null(r.Png);
            Assert.StartsWith("plot failed:", r.Error);
        }

        [Fact]
        public void Saving_again_into_the_same_folder_replaces_the_png()
        {
            string output = Path.Combine(folder, "plots");
            LasFileResult well = Load("again.las", Good);
            PlotExporter.Save(well, output);
            PlotExportResult second = PlotExporter.Save(well, output);
            Assert.Null(second.Error);
            Assert.Single(Directory.GetFiles(output));
        }
    }
}
