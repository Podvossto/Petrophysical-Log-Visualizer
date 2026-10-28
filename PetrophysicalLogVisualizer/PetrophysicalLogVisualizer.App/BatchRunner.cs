using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using PetrophysicalLogVisualizer.Core;

namespace PetrophysicalLogVisualizer.App
{
    /// <summary>
    /// --batch &lt;inFolder&gt; &lt;outFolder&gt;: every LAS file in the folder is loaded, conditioned and plotted, one file at a time,
    /// then the reports are written. No window. A bad file is a line in the reports, never a crash.
    /// Output: outFolder\plots\*.png, load-report.csv, conditioning-report.csv, summary.md
    /// </summary>
    public static class BatchRunner
    {
        /// <summary>0 when the run finished (even if some files could not be read); 2 when the input folder is not usable.</summary>
        public static int Run(string inFolder, string outFolder, TextWriter log)
        {
            if (!Directory.Exists(inFolder))
            {
                log.WriteLine("Input folder not found: " + inFolder);
                return 2;
            }

            var timer = Stopwatch.StartNew();
            string[] files = LasLoader.FindFiles(inFolder);
            string plots = Path.Combine(outFolder, "plots");
            Directory.CreateDirectory(outFolder);
            Dictionary<string, string> copies = FileDuplicates.Find(files);   // byte-identical files: flagged in the reports, still processed
            var results = new List<LasFileResult>();
            int png = 0, plotErrors = 0;
            long bytes = 0;

            for (int i = 0; i < files.Length; i++)
            {
                LasFileResult r = LasLoader.Load(files[i]);
                bytes += r.Bytes;
                string original;
                if (copies.TryGetValue(files[i], out original))
                {
                    r.DuplicateOf = Path.GetFileName(original);
                    r.Issues.Add("Byte-identical copy of " + r.DuplicateOf);
                }
                PlotExportResult plot = PlotExporter.Save(r, plots);
                if (plot.Png != null) png++;
                else if (!r.Failed) { plotErrors++; r.Issues.Add(plot.Error); }   // loaded but not drawn: the load report must say so
                if (r.Data != null) r.Data.ReleaseValues();                       // keep one file's values in memory, not all of them
                results.Add(r);
                log.WriteLine("[" + (i + 1) + "/" + files.Length + "] " + Path.GetFileName(files[i]) + ": " + (r.Failed ? "FAILED" : r.Issues.Count > 0 ? "WARN" : "OK") + (plot.Png != null ? ", plot written" : ", no plot"));
            }

            var utf8 = new UTF8Encoding(false);
            File.WriteAllText(Path.Combine(outFolder, "load-report.csv"), LoadReport.ToCsv(results), utf8);
            File.WriteAllText(Path.Combine(outFolder, "conditioning-report.csv"), ConditioningReport.ToCsv(results), utf8);
            File.WriteAllText(Path.Combine(outFolder, "summary.md"), ConditioningReport.ToSummary(results), utf8);

            int failed = 0;
            foreach (LasFileResult r in results) if (r.Failed) failed++;
            double seconds = timer.Elapsed.TotalSeconds;
            log.WriteLine("Files: " + files.Length + " (loaded " + (files.Length - failed) + ", failed " + failed + "); plots written: " + png + (plotErrors > 0 ? " (" + plotErrors + " loaded files could not be drawn)" : ""));
            if (copies.Count > 0) log.WriteLine((files.Length - copies.Count) + " files are different; " + copies.Count + " are byte-identical copies (flagged, not skipped)");
            log.WriteLine("Time: " + seconds.ToString("F1") + " s; " + (bytes / 1048576.0).ToString("F1") + " MB read (" + (bytes / 1048576.0 / Math.Max(seconds, 0.001)).ToString("F1") + " MB/s); peak working set: " + (Process.GetCurrentProcess().PeakWorkingSet64 / 1048576) + " MB");
            log.WriteLine("Reports and plots are in: " + outFolder);
            return 0;
        }
    }
}
