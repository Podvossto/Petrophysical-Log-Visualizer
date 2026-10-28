using System.IO;
using System.Linq;
using PetrophysicalLogVisualizer.App;
using Xunit;

namespace PetrophysicalLogVisualizer.Tests
{
    public class BatchRunnerTests : System.IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), "batch_" + System.Guid.NewGuid().ToString("N"));
        private string In { get { return Path.Combine(root, "in"); } }
        private string Out { get { return Path.Combine(root, "out"); } }

        public BatchRunnerTests() { Directory.CreateDirectory(In); }
        public void Dispose() { Directory.Delete(root, true); }

        private static string Las(string rows)
        {
            return "~V\nVERS. 2.0 : v\nWRAP. NO : w\n~W\nSTRT.FT 1 :\nSTOP.FT 3 :\nSTEP.FT 1 :\nNULL. -999.25 :\n" +
                   "~C\nDEPT.FT : d\nGR.GAPI : g\n~A\n" + rows;
        }

        private void Put(string name, string text) { File.WriteAllText(Path.Combine(In, name), text); }

        [Fact]
        public void A_folder_with_good_descending_and_unreadable_files_gives_all_outputs_and_does_not_crash()
        {
            Put("good.las", Las("1 50\n2 60\n3 70\n"));
            Put("desc.LAS", Las("3 70\n2 60\n1 50\n"));
            Put("bad.las", "");
            var log = new StringWriter();

            int code = BatchRunner.Run(In, Out, log);

            Assert.Equal(0, code);
            string[] load = File.ReadAllLines(Path.Combine(Out, "load-report.csv"));
            Assert.Equal(4, load.Length);                                   // header + 3 files
            Assert.Single(load, l => l.StartsWith("bad.las,FAILED,"));
            Assert.Equal(new[] { "desc.png", "good.png" }, Directory.GetFiles(Path.Combine(Out, "plots")).Select(Path.GetFileName).OrderBy(n => n).ToArray());
            Assert.Contains("good.las", File.ReadAllText(Path.Combine(Out, "conditioning-report.csv")));
            Assert.Contains("loaded 2, failed 1", File.ReadAllText(Path.Combine(Out, "summary.md")));
            Assert.Contains("Files: 3 (loaded 2, failed 1); plots written: 2", log.ToString());
        }

        [Fact]
        public void A_byte_identical_copy_is_flagged_in_the_reports_but_still_processed_and_plotted()
        {
            Put("a_well.las", Las("1 50\n2 60\n3 70\n"));
            Put("b_well_copy.las", Las("1 50\n2 60\n3 70\n"));
            Put("c_other.las", Las("1 55\n2 65\n3 75\n"));
            var log = new StringWriter();

            Assert.Equal(0, BatchRunner.Run(In, Out, log));

            string[] load = File.ReadAllLines(Path.Combine(Out, "load-report.csv"));
            Assert.Single(load, l => l.Contains("Byte-identical copy of a_well.las"));
            Assert.Contains(load, l => l.StartsWith("b_well_copy.las,WARN,"));
            Assert.Contains(load, l => l.StartsWith("a_well.las,OK,"));                 // the original is not flagged
            Assert.Equal(3, Directory.GetFiles(Path.Combine(Out, "plots"), "*.png").Length);   // nothing is skipped
            string summary = File.ReadAllText(Path.Combine(Out, "summary.md"));
            Assert.Contains("2 files are different; 1 are byte-identical copies", summary);
            Assert.Contains("duplicate-file", File.ReadAllText(Path.Combine(Out, "conditioning-report.csv")));
            Assert.Contains("2 files are different; 1 are byte-identical copies", log.ToString());
        }

        [Fact]
        public void A_missing_input_folder_is_a_message_and_a_non_zero_code()
        {
            var log = new StringWriter();
            int code = BatchRunner.Run(Path.Combine(root, "nothing"), Out, log);
            Assert.Equal(2, code);
            Assert.Contains("not found", log.ToString());
            Assert.False(Directory.Exists(Out));
        }

        [Fact]
        public void An_empty_folder_gives_empty_reports()
        {
            var log = new StringWriter();
            Assert.Equal(0, BatchRunner.Run(In, Out, log));
            Assert.Single(File.ReadAllLines(Path.Combine(Out, "load-report.csv")));   // header only
            Assert.Contains("Files: 0", File.ReadAllText(Path.Combine(Out, "summary.md")));
        }

        [Fact]
        public void A_loaded_file_that_cannot_be_drawn_is_reported_in_the_load_report()
        {
            Put("good.las", Las("1 50\n2 60\n"));
            Directory.CreateDirectory(Out);
            File.WriteAllText(Path.Combine(Out, "plots"), "a file where the plots folder should be");   // forces the plot to fail
            var log = new StringWriter();
            Assert.Equal(0, BatchRunner.Run(In, Out, log));
            string load = File.ReadAllText(Path.Combine(Out, "load-report.csv"));
            Assert.Contains("good.las,WARN,", load);
            Assert.Contains("plot failed", load);
        }
    }
}
