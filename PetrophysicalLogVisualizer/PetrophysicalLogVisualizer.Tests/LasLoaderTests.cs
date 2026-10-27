using System;
using System.IO;
using System.Linq;
using System.Text;
using PetrophysicalLogVisualizer.Core;
using Xunit;

namespace PetrophysicalLogVisualizer.Tests
{
    public class LasLoaderTests : IDisposable
    {
        private readonly string folder = Path.Combine(Path.GetTempPath(), "las-test-" + Guid.NewGuid().ToString("N"));

        public LasLoaderTests() { Directory.CreateDirectory(folder); }
        public void Dispose() { Directory.Delete(folder, true); }

        private const string Good =
            "~V\n VERS. 2.0 : v\n WRAP. NO : w\n~W\n NULL. -999.25 : n\n" +
            "~C\n DEPT.FT : Depth\n GR.GAPI : Gamma\n~A\n1 50\n2 51\n";

        private string Write(string name, byte[] bytes)
        {
            string path = Path.Combine(folder, name);
            File.WriteAllBytes(path, bytes);
            return path;
        }

        private string Write(string name, string text)
        {
            return Write(name, Encoding.GetEncoding(28591).GetBytes(text));
        }

        [Fact]
        public void Good_file_loads_without_issues()
        {
            LasFileResult r = LasLoader.Load(Write("a.las", Good));
            Assert.False(r.Failed);
            Assert.Equal(2, r.Data.Rows);
            Assert.Empty(r.Issues);
        }

        [Fact]
        public void Crlf_and_lf_files_give_the_same_result()   // D-02
        {
            LasFileResult lf = LasLoader.Load(Write("lf.las", Good));
            LasFileResult crlf = LasLoader.Load(Write("crlf.las", Good.Replace("\n", "\r\n")));
            Assert.Equal(lf.Data.Depth, crlf.Data.Depth);
            Assert.Equal(lf.Data.Curves[0], crlf.Data.Curves[0]);
            Assert.Empty(crlf.Issues);
        }

        [Fact]
        public void Non_ascii_byte_in_header_does_not_fail_the_load()   // D-02
        {
            byte[] bytes = Encoding.GetEncoding(28591).GetBytes(Good.Replace("Gamma", "Gamma °é"));
            LasFileResult r = LasLoader.Load(Write("latin.las", bytes));
            Assert.False(r.Failed);
            Assert.Equal(2, r.Data.Rows);
        }

        [Fact]
        public void Both_extension_cases_are_found()   // D-01
        {
            Write("a.las", Good);
            Write("b.LAS", Good);
            Write("c.txt", Good);
            string[] found = LasLoader.FindFiles(folder);
            Assert.Equal(new[] { "a.las", "b.LAS" }, found.Select(Path.GetFileName).ToArray());
        }

        [Fact]
        public void Garbage_and_empty_files_fail_softly()
        {
            LasFileResult empty = LasLoader.Load(Write("empty.las", new byte[0]));
            LasFileResult junk = LasLoader.Load(Write("junk.las", new byte[] { 0, 1, 2, 255, 254, 0x1A, 10, 13, 0 }));
            Assert.True(empty.Failed);
            Assert.True(junk.Failed);
            Assert.NotEmpty(empty.Issues);
            Assert.NotEmpty(junk.Issues);
        }

        [Fact]
        public void Missing_file_is_a_failed_result_not_an_exception()
        {
            LasFileResult r = LasLoader.Load(Path.Combine(folder, "nope.las"));
            Assert.True(r.Failed);
            Assert.Contains(r.Issues, i => i.StartsWith("Load failed"));
        }

        [Fact]
        public void Report_has_one_row_per_file_and_quotes_commas()
        {
            LasFileResult ok = LasLoader.Load(Write("ok.las", Good));
            LasFileResult bad = LasLoader.Load(Write("bad,name.las", new byte[0]));
            string[] rows = LoadReport.ToCsv(new[] { ok, bad }).TrimEnd().Split(new[] { "\r\n" }, StringSplitOptions.None);
            Assert.Equal(3, rows.Length);                    // header + 2 files
            Assert.StartsWith("ok.las,OK,", rows[1]);
            Assert.StartsWith("\"bad,name.las\",FAILED,", rows[2]);
        }
    }
}
