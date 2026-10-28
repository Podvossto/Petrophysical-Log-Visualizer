using System.IO;
using System.Linq;
using System.Text;
using PetrophysicalLogVisualizer.App;
using Xunit;

namespace PetrophysicalLogVisualizer.Tests
{
    /// <summary>F1 end to end: a folder of awkward files goes through --batch, and every one of them ends up as a row with a reason, never as a crash.</summary>
    public class AbnormalFilesTests : System.IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), "abnormal_" + System.Guid.NewGuid().ToString("N"));
        private string In { get { return Path.Combine(root, "in"); } }
        private string Out { get { return Path.Combine(root, "out"); } }
        private static readonly Encoding Latin1 = Encoding.GetEncoding(28591);

        public AbnormalFilesTests() { Directory.CreateDirectory(Path.Combine(In, "sub")); }
        public void Dispose() { Directory.Delete(root, true); }

        private const string Head =
            "~V\nVERS. 2.0 : v\nWRAP. NO : w\n~W\nSTRT.FT 1 :\nSTOP.FT 5 :\nSTEP.FT 1 :\nNULL. -999.25 :\n~C\nDEPT.FT : d\nGR.GAPI : g\nRT.OHMM : r\n";
        private const string Rows = "1 50 10\n2 60 11\n3 70 12\n4 80 13\n5 90 14\n";

        private void Put(string relative, byte[] bytes) { File.WriteAllBytes(Path.Combine(In, relative), bytes); }
        private void Put(string relative, string text) { Put(relative, Latin1.GetBytes(text)); }

        private string[][] LoadReport()
        {
            return File.ReadAllLines(Path.Combine(Out, "load-report.csv")).Skip(1).Select(l => l.Split(',')).ToArray();
        }

        [Fact]
        public void Awkward_files_each_get_a_row_and_a_reason_and_the_run_does_not_crash()
        {
            Put("01_good.las", Head + "~A\n" + Rows);
            Put("02_empty.las", new byte[0]);
            var noise = new byte[6000]; new System.Random(7).NextBytes(noise); Put("03_binary.las", noise);
            Put("04_header_only.las", Head);
            Put("05_cut_in_curves.las", Head.Substring(0, Head.IndexOf("~C") + 25));
            Put("06_cut_in_data.las", Head + "~A\n1 50 10\n2 60 11\n3 70 1");
            Put("07_utf8_bom.las", new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Latin1.GetBytes(Head + "~A\n" + Rows)).ToArray());
            Put("08_UPPER.LAS", Head + "~A\n" + Rows);
            Put("09_text_depth.las", Head + "~A\nabc 50 10\ndef 60 11\n");
            Put("10_nul_bytes.las", Head + "~A\n1 50 10\n" + new string('\0', 40) + "\n3 70 12\n");
            Put("11_ภาษาไทย #1 (copy).las", Head + "~A\n" + Rows);
            Put("12_not_a_las.txt", Head + "~A\n" + Rows);             // wrong extension: not a candidate
            Put(Path.Combine("sub", "13_inner.las"), Head + "~A\n" + Rows);   // sub-folders are not searched

            int code = BatchRunner.Run(In, Out, new StringWriter());

            Assert.Equal(0, code);
            string[][] rows = LoadReport();
            Assert.Equal(11, rows.Length);                                   // the 11 .las files of the folder, nothing else
            string[] failed = { "02_empty.las", "03_binary.las", "04_header_only.las", "05_cut_in_curves.las", "09_text_depth.las" };
            Assert.Equal(failed.OrderBy(n => n), rows.Where(r => r[1] == "FAILED").Select(r => r[0]).OrderBy(n => n));   // exactly the unreadable ones fail
            Assert.All(rows.Where(r => r[1] == "FAILED"), r => Assert.True(r[8].Length > 0, r[0] + " has no reason"));
            Assert.Equal(rows.Count(r => r[1] != "FAILED"), Directory.GetFiles(Path.Combine(Out, "plots"), "*.png").Length);
            Assert.True(File.Exists(Path.Combine(Out, "plots", "11_ภาษาไทย #1 (copy).png")));
        }

        [Fact]
        public void A_file_with_old_Mac_line_endings_is_reported_as_failed_not_misread()   // known limitation: lines end in CR only
        {
            Put("cr_only.las", (Head + "~A\n" + Rows).Replace("\n", "\r"));
            Assert.Equal(0, BatchRunner.Run(In, Out, new StringWriter()));
            string[][] rows = LoadReport();
            Assert.Equal("FAILED", rows.Single()[1]);
        }

        [Fact]
        public void A_curve_that_is_all_missing_still_loads_and_the_well_plots_without_it()
        {
            Put("all_missing.las", Head + "~A\n1 -999.25 -999.25\n2 -999.25 -999.25\n3 -999.25 -999.25\n");
            Assert.Equal(0, BatchRunner.Run(In, Out, new StringWriter()));
            Assert.Equal("OK", LoadReport().Single()[1]);
            Assert.True(File.Exists(Path.Combine(Out, "plots", "all_missing.png")));
            Assert.Contains("no valid values", File.ReadAllText(Path.Combine(Out, "conditioning-report.csv")));   // the curve choice says why GR and RT were passed over
        }
    }
}
