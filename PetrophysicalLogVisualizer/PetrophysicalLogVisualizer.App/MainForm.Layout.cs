using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PetrophysicalLogVisualizer.App
{
    /// <summary>The look of the window only: which control sits where. The behaviour is in MainForm.cs.</summary>
    public sealed partial class MainForm
    {
        private static readonly Color Page = Color.FromArgb(240, 243, 248), Line = Color.FromArgb(220, 225, 232);
        private static readonly Color Accent = Color.FromArgb(37, 99, 235), Muted = Color.FromArgb(100, 110, 125);
        internal static readonly Color OkColor = Color.FromArgb(22, 163, 74), WarnColor = Color.FromArgb(234, 136, 12), FailColor = Color.FromArgb(220, 38, 38);
        private const int SideWidth = 400;   // left and right columns have the same width

        // toolbar and status bar
        private readonly Button openButton = ToolButton("Open Folder", true);
        private readonly Button exportButton = ToolButton("Export all", false);
        private readonly ProgressBar progress = new ProgressBar { Width = 220, Height = 18, Margin = new Padding(16, 11, 0, 0), Visible = false };
        private readonly Label status = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Text = "Open a folder with LAS files." };
        private readonly Label statusRight = new Label { Dock = DockStyle.Right, Width = 420, TextAlign = ContentAlignment.MiddleRight, ForeColor = Muted };

        // left: well list
        private readonly RadioButton allTab = Tab("Wells", true), issuesTab = Tab("Issues", false);
        private readonly TextBox search = new TextBox { Dock = DockStyle.Top };
        private readonly ListView list = new ListView { View = View.Details, FullRowSelect = true, HideSelection = false, BorderStyle = BorderStyle.None, HeaderStyle = ColumnHeaderStyle.Nonclickable, ShowItemToolTips = true };
        private readonly Label okCount = Counter(OkColor), warnCount = Counter(WarnColor), failCount = Counter(FailColor);

        // centre: header, available curves, plot
        private readonly Label nameLabel = new Label { Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 15f, FontStyle.Bold), Margin = new Padding(0, 0, 10, 0) };
        private readonly Label badge = new Label { AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.White, Padding = new Padding(8, 3, 8, 3), Margin = new Padding(0, 0, 4, 0), Visible = false };
        private readonly Label depthValue = Value(), rowsValue = Value(), stepValue = Value(), noteValue = Value();
        private readonly Label[,] curveCells = new Label[4, 3];   // per role: mark + name, curve, unit
        private readonly PictureBox picture = new PictureBox { SizeMode = PictureBoxSizeMode.CenterImage, BackColor = Color.White };

        // right: conditioning report
        private readonly ListView summary = new ListView { View = View.Details, FullRowSelect = true, BorderStyle = BorderStyle.None, HeaderStyle = ColumnHeaderStyle.Nonclickable };
        private readonly Label notes = new Label { BackColor = Color.FromArgb(235, 242, 255), Padding = new Padding(8), ForeColor = Color.FromArgb(30, 64, 140) };
        private readonly TextBox details = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.None, BackColor = Color.White };

        private void BuildLayout()
        {
            Text = "Petrophysical Log Visualizer";
            Font = new Font("Segoe UI", 9f);
            BackColor = Page;
            ClientSize = new Size(1400, 860);
            MinimumSize = new Size(1200, 700);
            StartPosition = FormStartPosition.CenterScreen;

            list.Columns.Add("Well (file)", 130);
            list.Columns.Add("Size", 58, HorizontalAlignment.Right);
            list.Columns.Add("Status", 76);
            list.Columns.Add("Note", 92);
            summary.Columns.Add("Change", 150);
            summary.Columns.Add("Count", 60, HorizontalAlignment.Right);
            summary.Columns.Add("Warn", 50, HorizontalAlignment.Right);
            list.Resize += (s, e) => FitColumns(list, 0);
            summary.Resize += (s, e) => FitColumns(summary, 0);
            search.HandleCreated += (s, e) => SendMessage(search.Handle, 0x1501, (IntPtr)1, "Search well name...");   // grey hint text

            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, BackColor = Color.White, Padding = new Padding(10, 6, 10, 0) };
            toolbar.Controls.AddRange(new Control[] { openButton, exportButton, progress });
            var bar = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = Color.White, Padding = new Padding(12, 0, 12, 0) };
            bar.Controls.Add(status);
            bar.Controls.Add(statusRight);

            Panel left = Card(null, BuildWellsBody());
            left.Dock = DockStyle.Left; left.Width = SideWidth;
            Panel right = Card("Conditioning report", BuildReportBody());
            right.Dock = DockStyle.Right; right.Width = SideWidth;

            var topRow = Grid(2, 1);
            topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            topRow.Controls.Add(Card(null, BuildHeaderBody()));
            topRow.Controls.Add(Card("Available curves", BuildCurvesBody()));
            var centre = Grid(1, 2);
            centre.RowStyles.Add(new RowStyle(SizeType.Absolute, 168));
            centre.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            centre.Controls.Add(topRow);
            centre.Controls.Add(Card("Log plot", picture));

            Controls.Add(centre);
            Controls.Add(new Splitter { Dock = DockStyle.Right, Width = 2, BackColor = Page });   // drag to resize the right column
            Controls.Add(right);
            Controls.Add(new Splitter { Dock = DockStyle.Left, Width = 2, BackColor = Page });    // drag to resize the well list
            Controls.Add(left);
            Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Line });
            Controls.Add(toolbar);
            Controls.Add(bar);
        }

        private Control BuildWellsBody()
        {
            var tabs = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, WrapContents = false };
            tabs.Controls.Add(allTab);
            tabs.Controls.Add(issuesTab);
            var counters = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 30, Padding = new Padding(0, 6, 0, 0) };
            counters.Controls.AddRange(new Control[] { okCount, warnCount, failCount });
            var body = new Panel();
            body.Controls.Add(Fill(list));
            body.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 8 });
            body.Controls.Add(search);
            body.Controls.Add(tabs);
            body.Controls.Add(counters);
            return body;
        }

        private Control BuildHeaderBody()
        {
            var nameRow = Grid(2, 1);   // a long name is cut with "..." so the badge always stays visible
            nameRow.Dock = DockStyle.Top;
            nameRow.Height = 40;
            nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            nameRow.Controls.Add(nameLabel);
            nameRow.Controls.Add(badge);
            var facts = Grid(2, 4);
            facts.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            facts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            AddFact(facts, "Depth range", depthValue);
            AddFact(facts, "Rows", rowsValue);
            AddFact(facts, "Step", stepValue);
            AddFact(facts, "Note", noteValue);
            var body = new Panel();
            body.Controls.Add(Fill(facts));
            body.Controls.Add(nameRow);
            return body;
        }

        private Control BuildCurvesBody()
        {
            var grid = Grid(3, 4);
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54));
            for (int r = 0; r < 4; r++)
            {
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
                for (int c = 0; c < 3; c++)
                {
                    curveCells[r, c] = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, ForeColor = c == 2 ? Muted : Color.Black };
                    grid.Controls.Add(curveCells[r, c], c, r);
                }
            }
            return grid;
        }

        private Control BuildReportBody()
        {
            var grid = Grid(1, 6);
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            AddSection(grid, "Changes summary", summary, SizeType.Absolute, 120);
            AddSection(grid, "Notes", notes, SizeType.Absolute, 92);
            AddSection(grid, "Details", details, SizeType.Percent, 100);
            return grid;
        }

        // A white card with a thin border and the same gap around every card; title may be null.
        private static Panel Card(string title, Control body)
        {
            var outer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6), BackColor = Page };
            var border = new Panel { Dock = DockStyle.Fill, BackColor = Line, Padding = new Padding(1) };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12, 10, 12, 10) };
            inner.Controls.Add(Fill(body));
            if (title != null) inner.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 28, Font = new Font("Segoe UI", 11f, FontStyle.Bold) });
            border.Controls.Add(inner);
            outer.Controls.Add(border);
            return outer;
        }

        private static void AddSection(TableLayoutPanel grid, string title, Control body, SizeType type, float size)
        {
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            grid.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) });
            grid.RowStyles.Add(new RowStyle(type, size));
            grid.Controls.Add(Fill(body));
        }

        private static void AddFact(TableLayoutPanel grid, string caption, Label value)
        {
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            grid.Controls.Add(new Label { Text = caption, Dock = DockStyle.Fill, ForeColor = Muted, TextAlign = ContentAlignment.MiddleLeft });
            grid.Controls.Add(value);
        }

        // The column at index fill takes whatever width the others leave, so the list never scrolls sideways.
        private static void FitColumns(ListView l, int fill)
        {
            int rest = 0;
            for (int i = 0; i < l.Columns.Count; i++) if (i != fill) rest += l.Columns[i].Width;
            l.Columns[fill].Width = Math.Max(60, l.ClientSize.Width - rest);
        }

        private static TableLayoutPanel Grid(int columns, int rows)
        {
            return new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = columns, RowCount = rows, Margin = Padding.Empty, Padding = Padding.Empty };
        }

        private static Control Fill(Control c) { c.Dock = DockStyle.Fill; return c; }

        private static Label Value() { return new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true }; }

        private static Label Counter(Color color) { return new Label { AutoSize = true, ForeColor = color, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Margin = new Padding(0, 0, 18, 0) }; }

        private static Button ToolButton(string text, bool primary)
        {
            var b = new Button { Text = text, AutoSize = true, Height = 30, FlatStyle = FlatStyle.Flat, Padding = new Padding(10, 0, 10, 0), Margin = new Padding(0, 0, 8, 0), Cursor = Cursors.Hand };
            b.BackColor = primary ? Accent : Color.White;
            b.ForeColor = primary ? Color.White : Color.Black;
            b.FlatAppearance.BorderColor = primary ? Accent : Line;
            return b;
        }

        // "Wells" and "Issues" look like tabs; they are two radio buttons, so exactly one is on.
        private static RadioButton Tab(string text, bool on)
        {
            var t = new RadioButton { Text = text, Checked = on, Appearance = Appearance.Button, AutoSize = true, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 6, 0), Padding = new Padding(6, 2, 6, 2), Cursor = Cursors.Hand };
            t.FlatAppearance.BorderSize = 0;
            t.FlatAppearance.CheckedBackColor = Color.FromArgb(225, 235, 255);
            t.CheckedChanged += (s, e) => t.Font = new Font("Segoe UI", 9f, t.Checked ? FontStyle.Bold : FontStyle.Regular);
            return t;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, string lParam);
    }
}
