using System;
using System.Drawing;
using PetrophysicalLogVisualizer.Core;
using ScottPlot;
using ScottPlot.Plottable;

namespace PetrophysicalLogVisualizer.App
{
    /// <summary>Draws a WellPlotModel with ScottPlot: 3 tracks side by side on one depth axis, increasing downward. No decisions here, only drawing.</summary>
    public static class WellPlotRenderer
    {
        private const int HeaderHeight = 40;
        private const int PlotLeft = 70, PlotRight = 20, PlotTop = 64, PlotBottom = 60;   // same in every track so the depth lines up; the strip under the title holds the legend

        public static Bitmap Render(WellPlotModel m, int width, int height)
        {
            var result = new Bitmap(width, height);
            int trackWidth = width / m.Tracks.Length;
            using (Graphics g = Graphics.FromImage(result))
            using (var font = new Font("Segoe UI", 14, FontStyle.Bold))
            {
                g.Clear(Color.White);
                g.DrawString(m.Title + "   (depth " + m.DepthMin + " - " + m.DepthMax + " ft)", font, Brushes.Black, 10, 8);
                for (int t = 0; t < m.Tracks.Length; t++)
                {
                    var plt = new Plot();
                    Draw(plt, m, m.Tracks[t], t == 0);
                    using (Bitmap body = plt.Render(trackWidth, height - HeaderHeight))
                        g.DrawImage(body, t * trackWidth, HeaderHeight);
                    DrawLegend(g, m.Tracks[t], t * trackWidth, trackWidth);
                }
            }
            return result;
        }

        // One coloured line and the curve name per series, centred between the title and the plot area.
        private static void DrawLegend(Graphics g, PlotTrack track, int left, int trackWidth)
        {
            if (track.Series.Count == 0) return;
            using (var font = new Font("Segoe UI", 10, FontStyle.Regular))
            {
                const int lineLength = 26, gap = 6, spacing = 22;
                var widths = new float[track.Series.Count];
                float total = 0;
                for (int i = 0; i < widths.Length; i++)
                {
                    widths[i] = lineLength + gap + g.MeasureString(track.Series[i].Label, font).Width;
                    total += widths[i] + (i > 0 ? spacing : 0);
                }
                float x = left + PlotLeft + (trackWidth - PlotLeft - PlotRight - total) / 2;
                int y = HeaderHeight + PlotTop - 20;
                for (int i = 0; i < widths.Length; i++)
                {
                    using (var pen = new Pen(ColorOf(track.Series[i].Role), 3))
                        g.DrawLine(pen, x, y + 9, x + lineLength, y + 9);
                    g.DrawString(track.Series[i].Label, font, Brushes.Black, x + lineLength + gap, y);
                    x += widths[i] + spacing;
                }
            }
        }

        private static void Draw(Plot plt, WellPlotModel m, PlotTrack track, bool first)
        {
            foreach (PlotSeries s in track.Series)
            {
                ScatterPlot line = plt.AddScatterLines(s.X, m.Depth, ColorOf(s.Role), 1.5f, label: s.Label);
                line.OnNaN = ScatterPlot.NanBehavior.Gap;   // no reading: leave a gap, never join the points
            }

            double top = m.DepthMin, bottom = m.DepthMax > m.DepthMin ? m.DepthMax : m.DepthMin + 1;
            plt.YAxis.IsReverse = true;   // depth increases downward
            plt.SetAxisLimits(track.XMin, track.XMax, top, bottom);
            plt.Layout(left: PlotLeft, right: PlotRight, bottom: PlotBottom, top: PlotTop);

            if (track.Log)
            {
                plt.XAxis.MinorLogScale(true);
                plt.XAxis.TickLabelFormat(v => Math.Pow(10, v).ToString("0.###"));
            }

            plt.Title(track.Title, size: 14);
            plt.XAxis.Label(track.AxisLabel, size: 10);
            if (first) plt.YAxis.Label("Depth (ft)");

            for (int i = 0; i < track.Messages.Count; i++)
            {
                double y = top + (bottom - top) * (0.03 + 0.04 * i);   // near the top, on a white box, so it never hides the data
                Text note = plt.AddText(track.Messages[i], (track.XMin + track.XMax) / 2, y, 13, Color.DimGray);
                note.Alignment = Alignment.MiddleCenter;
                note.BackgroundFill = true;
                note.BackgroundColor = Color.White;
            }
        }

        private static Color ColorOf(CurveRole role)
        {
            switch (role)
            {
                case CurveRole.GammaRay: return Color.FromArgb(0x2E, 0x7D, 0x32);
                case CurveRole.Resistivity: return Color.FromArgb(0x7B, 0x1F, 0xA2);
                case CurveRole.BulkDensity: return Color.FromArgb(0xC6, 0x28, 0x28);
                default: return Color.FromArgb(0x15, 0x65, 0xC0);
            }
        }
    }
}
