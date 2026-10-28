using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using PetrophysicalLogVisualizer.Core;

namespace PetrophysicalLogVisualizer.App
{
    /// <summary>
    /// F8: Open Folder, the well list (search, Wells/Issues), the plot and the changes of the clicked well, Export all.
    /// Loading, drawing and exporting run on a background thread; the list keeps light summaries only (WellRow).
    /// </summary>
    public sealed partial class MainForm : Form
    {
        private readonly List<WellRow> rows = new List<WellRow>();   // every file; the list shows the ones that pass the filter
        private readonly Timer redraw = new Timer { Interval = 250 };  // redraw the plot at the new size once resizing stops
        private WellPlotModel shown;   // the plot of the clicked well, kept so it can be redrawn at a new size
        private string folder;
        private bool busy;
        private int scanId;   // a newer scan makes an older one stop
        private int plotId;   // a newer click or size makes an older drawing stop

        public MainForm()
        {
            BuildLayout();
            allTab.Font = new Font(Font, FontStyle.Bold);
            openButton.Click += (s, e) => OpenFolder();
            exportButton.Click += (s, e) => ExportAll();
            exportButton.Enabled = false;
            search.TextChanged += (s, e) => ApplyFilter();
            issuesTab.CheckedChanged += (s, e) => ApplyFilter();
            list.SelectedIndexChanged += (s, e) => ShowSelected();
            picture.Resize += (s, e) => { redraw.Stop(); redraw.Start(); };
            redraw.Tick += (s, e) => { redraw.Stop(); Redraw(); };
            RefreshCounters();
            ShowView(new WellView());
        }

        private void OpenFolder()
        {
            string path = FolderPicker.Pick(this, "Folder with .las files", folder);
            if (path != null) Scan(path);
        }

        private async void Scan(string path)
        {
            int id = ++scanId;
            list.Items.Clear();
            rows.Clear();
            RefreshCounters();
            folder = path;
            string[] files;
            try { files = LasLoader.FindFiles(path); }
            catch (Exception ex) { status.Text = "Cannot read folder: " + ex.Message; return; }
            if (files.Length == 0) { status.Text = "No .las files in " + path; return; }

            SetBusy(true, files.Length);
            status.Text = "Loading 0 / " + files.Length;
            Dictionary<string, string> copies = await Task.Run(() => FileDuplicates.Find(files));
            for (int i = 0; i < files.Length && id == scanId; i++)
            {
                string file = files[i];
                WellRow row = await Task.Run(() =>
                {
                    LasFileResult r = LasLoader.Load(file);
                    string original;
                    if (copies.TryGetValue(file, out original)) BatchRunner.MarkCopy(r, original);
                    WellRow w = WellRow.From(r);
                    if (r.Data != null) r.Data.ReleaseValues();   // keep the summary, not the numbers
                    return w;
                });
                if (id != scanId) return;
                rows.Add(row);
                RefreshCounters();
                if (Passes(row)) list.Items.Add(ToItem(row));
                progress.Value = i + 1;
                status.Text = "Loading " + (i + 1) + " / " + files.Length + "   " + path;
            }
            if (id != scanId) return;
            SetBusy(false, 0);
            status.Text = "Ready   " + path;
        }

        private void SetBusy(bool value, int total)
        {
            busy = value;
            openButton.Enabled = !value;
            exportButton.Enabled = !value && folder != null;
            progress.Visible = value;
            progress.Maximum = Math.Max(1, total);
            progress.Value = 0;
        }

        private void RefreshCounters()
        {
            int ok = 0, warn = 0, failed = 0;
            foreach (WellRow w in rows) { if (w.Status == "OK") ok++; else if (w.Status == "WARN") warn++; else failed++; }
            okCount.Text = "● " + ok + " OK";
            warnCount.Text = "● " + warn + " WARN";
            failCount.Text = "● " + failed + " FAILED";
            allTab.Text = "Wells (" + rows.Count + ")";
            issuesTab.Text = "Issues (" + (rows.Count - ok) + ")";
            statusRight.Text = rows.Count == 0 ? "" : rows.Count + " wells loaded   |   " + (rows.Count - ok) + " wells with issues";
        }

        private bool Passes(WellRow w)
        {
            if (issuesTab.Checked && w.Status == "OK" && w.DuplicateOf == null) return false;
            return search.Text.Length == 0 || w.FileName.IndexOf(search.Text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void ApplyFilter()
        {
            if (busy) return;   // the list is being filled by the scan; the filter applies to the rows still to come
            list.BeginUpdate();
            list.Items.Clear();
            foreach (WellRow w in rows) if (Passes(w)) list.Items.Add(ToItem(w));
            list.EndUpdate();
        }

        private static ListViewItem ToItem(WellRow w)
        {
            var item = new ListViewItem(w.FileName + (w.DuplicateOf != null ? "  (copy)" : "")) { Tag = w, UseItemStyleForSubItems = false };
            item.SubItems.Add(w.Size);
            string mark = w.Status == "OK" ? "✓ " : w.Status == "WARN" ? "⚠ " : "✕ ";
            ListViewItem.ListViewSubItem state = item.SubItems.Add(mark + w.Status);
            state.ForeColor = w.Status == "OK" ? OkColor : w.Status == "WARN" ? WarnColor : FailColor;
            item.SubItems.Add(w.Note).ForeColor = Muted;
            return item;
        }

        // Same work as --batch. BatchRunner prints "[i/n] ..." per file; a small writer turns those lines into progress.
        private async void ExportAll()
        {
            string output = FolderPicker.Pick(this, "Folder for the PNGs and reports", folder);
            if (output == null) return;
            string input = folder;
            SetBusy(true, rows.Count);
            status.Text = "Exporting...";
            var writer = new ProgressWriter((done, total) => BeginInvoke((Action)(() =>
            {
                progress.Maximum = total;
                progress.Value = Math.Min(done, total);
                status.Text = "Exporting " + done + " / " + total;
            })));
            int code = await Task.Run(() => BatchRunner.Run(input, output, writer));
            SetBusy(false, 0);
            status.Text = code == 0 ? "Export finished: " + output : "Export failed: input folder not found";
        }
    }
}
