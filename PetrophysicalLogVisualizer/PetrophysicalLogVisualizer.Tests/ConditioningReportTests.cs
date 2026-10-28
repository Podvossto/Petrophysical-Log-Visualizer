using System.Collections.Generic;
using System.IO;
using System.Linq;
using PetrophysicalLogVisualizer.Core;
using Xunit;

namespace PetrophysicalLogVisualizer.Tests
{
    public class ConditioningReportTests : System.IDisposable
    {
        private readonly string folder = Path.Combine(Path.GetTempPath(), "condrep_" + System.Guid.NewGuid().ToString("N"));

        public ConditioningReportTests() { Directory.CreateDirectory(folder); }
        public void Dispose() { Directory.Delete(folder, true); }

        private static string Las(string vers, string rows)
        {
            return "~V\nVERS. " + vers + " : v\nWRAP. NO : w\n~W\nSTRT.FT 1 :\nSTOP.FT 3 :\nSTEP.FT 1 :\nNULL. -999.25 :\n" +
                   "~C\nDEPT.FT : d\nGR.GAPI : g\nNPHI.PERC : n\n~A\n" + rows;
        }

        private LasFileResult Load(string name, string text)
        {
            string path = Path.Combine(folder, name);
            File.WriteAllText(path, text);
            return LasLoader.Load(path);
        }

        private static List<ConditioningEntry> Where(LasFileResult r, string category)
        {
            return ConditioningReport.Entries(r).Where(e => e.Category == category).ToList();
        }

        [Fact]
        public void Missing_values_are_one_row_per_curve_and_kind_with_the_count()
        {
            LasFileResult r = Load("a.las", Las("2.0", "1 -999.25 10\n2 -999.25 -999\n3 50 20\n"));
            List<ConditioningEntry> rows = Where(r, "missing-value");
            Assert.Equal(2, rows.Count(e => e.Curve != ""));
            ConditioningEntry nulls = rows.First(e => e.Curve == "GR");
            Assert.Equal(2, nulls.Count);
            Assert.Contains("declared NULL", nulls.Detail);
            ConditioningEntry marker = rows.First(e => e.Curve == "NPHI");
            Assert.Equal(1, marker.Count);
            Assert.Contains("-999", marker.Detail);
        }

        [Fact]
        public void A_converted_curve_gets_a_unit_conversion_row_that_names_the_curve()
        {
            LasFileResult r = Load("b.las", Las("2.0", "1 50 25\n2 51 26\n3 52 27\n"));
            ConditioningEntry e = Where(r, "unit-conversion").Single();
            Assert.Equal("NPHI", e.Curve);
            Assert.Equal(3, e.Count);
            Assert.Contains("divided by 100", e.Detail);
        }

        [Fact]
        public void Depth_changes_and_curve_choices_are_reported()
        {
            LasFileResult r = Load("c.las", Las("2.0", "3 52 27\n2 51 26\n2 99 98\n1 50 25\n"));
            Assert.Contains(Where(r, "depth"), e => e.Detail.Contains("descending"));
            Assert.Contains(Where(r, "depth"), e => e.Detail.Contains("repeat an earlier depth"));
            Assert.Equal(4, Where(r, "curve-choice").Count(e => e.Detail.Length > 0));   // one line per role
        }

        [Fact]
        public void Version_3_is_reported_as_best_effort()
        {
            LasFileResult r = Load("d.las", Las("3.0", "1 50 25\n2 51 26\n"));
            Assert.Single(Where(r, "format"));
            Assert.Empty(Where(Load("e.las", Las("2.0", "1 50 25\n2 51 26\n")), "format"));
        }

        [Fact]
        public void Clean_file_has_no_change_rows()
        {
            LasFileResult r = Load("f.las", Las("2.0", "1 50 0.25\n2 51 0.26\n3 52 0.27\n").Replace("NPHI.PERC", "NPHI.V/V"));
            Assert.Empty(Where(r, "missing-value"));
            Assert.Empty(Where(r, "unit-conversion"));
            Assert.Empty(Where(r, "depth"));
        }

        [Fact]
        public void Failed_file_has_no_entries_and_is_listed_in_the_summary()
        {
            LasFileResult bad = Load("g.las", "");
            Assert.Empty(ConditioningReport.Entries(bad));
            string summary = ConditioningReport.ToSummary(new[] { bad });
            Assert.Contains("failed 1", summary);
            Assert.Contains("g.las", summary);
        }

        [Fact]
        public void Csv_has_a_header_and_quotes_details_with_commas()
        {
            LasFileResult r = Load("h,x.las", Las("2.0", "1 -999.25 25\n2 51 26\n3 52 27\n"));
            string[] lines = ConditioningReport.ToCsv(new[] { r }).TrimEnd().Split(new[] { "\r\n" }, System.StringSplitOptions.None);
            Assert.Equal("file,level,category,curve,count,detail", lines[0]);
            Assert.All(lines.Skip(1), l => Assert.StartsWith("\"h,x.las\",", l));
            Assert.Contains(lines, l => l.Contains(",note,missing-value,GR,1,"));
        }

        [Fact]
        public void Declared_null_is_a_note_and_an_undeclared_marker_is_a_warning()
        {
            LasFileResult r = Load("k.las", Las("2.0", "1 -999.25 10\n2 50 -999\n3 51 20\n"));
            List<ConditioningEntry> rows = Where(r, "missing-value");
            Assert.Equal("note", rows.Single(e => e.Curve == "GR").Level);
            Assert.Equal("warn", rows.Single(e => e.Curve == "NPHI").Level);
        }

        [Fact]
        public void Summary_separates_routine_rows_from_warnings_and_gives_the_nan_share()
        {
            LasFileResult r = Load("l.las", Las("2.0", "1 -999.25 25\n2 51 26\n3 52 27\n4 -999.25 28\n"));
            string s = ConditioningReport.ToSummary(new[] { r });
            Assert.Contains("`warn` = worth a look (0, in 0 files)", s);
            Assert.Contains("25.0 % of all 8 curve values", s);   // 2 NULL values in 4 rows x 2 curves
        }

        [Fact]
        public void Reports_are_the_same_after_the_values_are_released()   // a batch run keeps only the counts of finished files
        {
            LasFileResult r = Load("m.las", Las("2.0", "3 -999.25 25\n2 51 26\n1 -999 27\n"));
            var one = new[] { r };
            string load = LoadReport.ToCsv(one), csv = ConditioningReport.ToCsv(one), summary = ConditioningReport.ToSummary(one);

            r.Data.ReleaseValues();

            Assert.Equal(load, LoadReport.ToCsv(one));
            Assert.Equal(csv, ConditioningReport.ToCsv(one));
            Assert.Equal(summary, ConditioningReport.ToSummary(one));
            Assert.Empty(r.Data.Depth);
            Assert.Equal(2, r.Data.Curves.Length);
            Assert.Equal(3, r.Data.Rows);
        }

        [Fact]
        public void Summary_adds_up_over_files()
        {
            LasFileResult one = Load("i.las", Las("2.0", "1 -999.25 25\n2 51 26\n3 52 27\n"));
            LasFileResult two = Load("j.las", Las("2.0", "3 -999.25 25\n2 -999.25 26\n1 52 27\n"));
            string s = ConditioningReport.ToSummary(new[] { one, two });
            Assert.Contains("Files: 2 (loaded 2, failed 0)", s);
            Assert.Contains("| Values equal to the declared NULL set to NaN | note | 2 | 3 |", s);
            Assert.Contains("| Depth reversed to ascending | note | 1 | |", s);
            Assert.Contains("| GammaRay | 2 | 0 | GR 2 |", s);
        }
    }
}
