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
                string title = m.Title + "   (depth " + m.DepthMin + " - " + m.DepthMax + " ft)";
                Font titleFont = font;
                float titleWidth = g.MeasureString(title, font).Width;
                if (titleWidth > width - 20)   // narrow window: shrink the title so it is not cut off
                    titleFont = new Font("Segoe UI", Math.Max(8f, font.Size * (width - 20) / titleWidth), FontStyle.Bold);
                g.DrawString(title, titleFont, Brushes.Black, 10, 8);
                if (titleFont != font) titleFont.Dispose();
                for (int t = 0; t < m.Tracks.Length; t++)
                {
                    var plt = new Plot();
                    Draw(plt, m, m.Tracks[t], t == 0, trackWidth, height - HeaderHeight - PlotTop - PlotBottom);
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
            float available = trackWidth - PlotLeft - PlotRight;
            float size = 10;
            while (size > 7 && LegendWidth(g, track, size) > available) size -= 1;   // narrow window: smaller legend, so no name is cut off
            using (var font = new Font("Segoe UI", size, FontStyle.Regular))
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

        private static float LegendWidth(Graphics g, PlotTrack track, float size)
        {
            float total = 0;
            using (var font = new Font("Segoe UI", size, FontStyle.Regular))
                for (int i = 0; i < track.Series.Count; i++)
                    total += 26 + 6 + g.MeasureString(track.Series[i].Label, font).Width + (i > 0 ? 22 : 0);
            return total;
        }

        private static void Draw(Plot plt, WellPlotModel m, PlotTrack track, bool first, int trackWidth, int plotHeight)
        {
            foreach (PlotSeries s in track.Series)
            {
                ScatterPlot line = plt.AddScatterLines(s.X, m.Depth, ColorOf(s.Role), 1.5f, label: s.Label);
                line.OnNaN = ScatterPlot.NanBehavior.Gap;   // no reading: leave a gap, never join the points
            }

            double top = m.DepthMin, bottom = m.DepthMax > m.DepthMin ? m.DepthMax : m.DepthMin + 1;
            plt.YAxis.IsReverse = true;   // depth increases downward
            plt.SetAxisLimits(track.XMin, track.XMax, top, bottom);
            plt.YAxis.ManualTickSpacing(DepthStep(bottom - top, plotHeight));
            plt.Layout(left: PlotLeft, right: PlotRight, bottom: PlotBottom, top: PlotTop);

            if (track.Log)
            {
                plt.XAxis.MinorLogScale(true);
                plt.XAxis.TickLabelFormat(v => Math.Pow(10, v).ToString("0.###"));
            }

            plt.Title(track.Title, size: 14);
            plt.XAxis.Label(track.AxisLabel, size: 10);
            if (first) plt.YAxis.Label("Depth (ft)");

            // 13 pt in the 1200 px PNG; smaller in a narrow window track so "No ... curve" is not cut off
            float noteSize = Math.Min(13f, Math.Max(7f, (trackWidth - PlotLeft - PlotRight) / 15f));
            for (int i = 0; i < track.Messages.Count; i++)
            {
                double y = top + (bottom - top) * (0.03 + 0.04 * i);   // near the top, on a white box, so it never hides the data
                Text note = plt.AddText(track.Messages[i], (track.XMin + track.XMax) / 2, y, noteSize, Color.DimGray);
                note.Alignment = Alignment.MiddleCenter;
                note.BackgroundFill = true;
                note.BackgroundColor = Color.White;
            }
        }

        // About one depth label per 28 px of the plot area, so a small window and the big PNG are equally readable. The step is 1, 2 or 5 x 10^n ft.
        public static double DepthStep(double range, int plotHeight)
        {
            double raw = range * 28.0 / Math.Max(plotHeight, 100);
            double power = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            foreach (double k in new[] { 5.0, 2.0, 1.0 })
                if (k * power <= raw) return k * power;
            return power;
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
