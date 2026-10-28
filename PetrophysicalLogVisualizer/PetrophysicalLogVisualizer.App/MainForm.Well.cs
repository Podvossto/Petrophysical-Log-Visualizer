using System;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using PetrophysicalLogVisualizer.Core;

namespace PetrophysicalLogVisualizer.App
{
    /// <summary>The clicked well: reload it, draw it at the size of the plot area, fill the header and the report card.</summary>
    public sealed partial class MainForm
    {
        // Reload the file (the list keeps no numbers) on a background thread; the plot is drawn at the exact size of its area, so it is sharp and fits.
        private async void ShowSelected()
        {
            if (list.SelectedItems.Count == 0) return;
            var row = (WellRow)list.SelectedItems[0].Tag;
            int id = ++plotId;
            ShowHeader(row, "loading...");
            Size size = PlotSize();
            WellPlotModel model = null;
            WellView view = null;
            Bitmap bitmap = null;
            string error = null;
            await Task.Run(() =>
            {
                try
                {
                    LasFileResult r = LasLoader.Load(row.Path);
                    if (row.DuplicateOf != null) BatchRunner.MarkCopy(r, row.DuplicateOf);   // same flag as in the scan and the reports
                    model = WellPlot.Build(r);
                    view = WellView.Build(r, model);
                    if (model == null) error = "Not plotted: the file did not load";
                    else bitmap = WellPlotRenderer.Render(model, size.Width, size.Height);
                }
                catch (Exception ex) { error = "Plot failed: " + ex.GetType().Name + ": " + ex.Message; }
            });
            if (id != plotId) { if (bitmap != null) bitmap.Dispose(); return; }
            shown = model;
            SetPicture(bitmap);
            ShowHeader(row, error);
            ShowView(view ?? new WellView { Notes = error ?? "" });
        }

        // After a resize: draw the same well again at the new size (the numbers are still in 'shown').
        private async void Redraw()
        {
            WellPlotModel model = shown;
            if (model == null) return;
            int id = ++plotId;
            Size size = PlotSize();
            Bitmap bitmap = null;
            await Task.Run(() =>
            {
                try { bitmap = WellPlotRenderer.Render(model, size.Width, size.Height); }
                catch (Exception) { bitmap = null; }   // keep the old picture; the next click draws again
            });
            if (id != plotId || bitmap == null) { if (bitmap != null) bitmap.Dispose(); return; }
            SetPicture(bitmap);
        }

        private Size PlotSize()
        {
            return new Size(Math.Max(360, picture.ClientSize.Width), Math.Max(360, picture.ClientSize.Height));
        }

        private void SetPicture(Bitmap bitmap)
        {
            Image old = picture.Image;
            picture.Image = bitmap;
            if (old != null) old.Dispose();
        }

        private void ShowHeader(WellRow w, string extra)
        {
            var inv = CultureInfo.InvariantCulture;
            nameLabel.Text = System.IO.Path.GetFileNameWithoutExtension(w.FileName);
            badge.Text = w.Status;
            badge.BackColor = w.Status == "OK" ? OkColor : w.Status == "WARN" ? WarnColor : FailColor;
            badge.Visible = true;
            depthValue.Text = w.TopDepth == null ? "no data" : w.TopDepth.Value.ToString("F1", inv) + " - " + w.BaseDepth.Value.ToString("F1", inv) + " ft";
            rowsValue.Text = w.Rows.ToString("N0", inv);
            stepValue.Text = w.Step == null ? "-" : w.Step.Value.ToString("0.####", inv) + " ft";
            string note = w.DuplicateOf != null ? "Byte-identical copy of " + w.DuplicateOf : w.IssueCount > 0 ? w.IssueCount + " issue(s), see the report" : "No issues";
            noteValue.Text = extra ?? note;
        }

        private void ShowView(WellView v)
        {
            for (int r = 0; r < 4; r++)
            {
                string[] line = r < v.Curves.Count ? v.Curves[r] : null;
                bool missing = line == null || line[1] == "(missing)";
                curveCells[r, 0].Text = (line == null ? "" : missing ? "✕  " : "✓  ") + (line == null ? "" : line[0]);
                curveCells[r, 0].ForeColor = line == null ? Color.Black : missing ? WarnColor : Color.Black;
                curveCells[r, 1].Text = line == null ? "" : line[1];
                curveCells[r, 1].ForeColor = missing ? WarnColor : Color.Black;
                curveCells[r, 2].Text = line == null ? "" : line[2];
            }

            summary.BeginUpdate();
            summary.Items.Clear();
            foreach (string[] line in v.Summary)
            {
                var item = new System.Windows.Forms.ListViewItem(line) { UseItemStyleForSubItems = false };
                if (line[2].Length > 0) item.SubItems[2].ForeColor = WarnColor;
                summary.Items.Add(item);
            }
            summary.EndUpdate();
            notes.Text = v.Notes.Length > 0 ? v.Notes : v.Curves.Count > 0 ? "All four curves were found." : "Click a well to see its curves and changes.";
            details.Text = v.Details;
        }
    }
}
