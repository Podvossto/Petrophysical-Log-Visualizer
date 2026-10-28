using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using PetrophysicalLogVisualizer.Core;

namespace PetrophysicalLogVisualizer.App
{
    /// <summary>What happened to one file: the PNG that was written, or why there is none.</summary>
    public sealed class PlotExportResult
    {
        public string File { get; set; }
        public string Png { get; set; }
        public string Error { get; set; }
    }

    /// <summary>F7: one PNG per loadable well. A file that did not load, or a drawing that fails, is a result with an Error, never an exception.</summary>
    public static class PlotExporter
    {
        public const int Width = 1200, Height = 1500;

        /// <summary>Writes folder\{file name without extension}.png.</summary>
        public static PlotExportResult Save(LasFileResult r, string folder)
        {
            var result = new PlotExportResult { File = Path.GetFileName(r.Path) };
            try
            {
                WellPlotModel model = WellPlot.Build(r);
                if (model == null)
                {
                    result.Error = "not plotted: the file did not load";
                    return result;
                }
                Directory.CreateDirectory(folder);
                string png = Path.Combine(folder, Path.GetFileNameWithoutExtension(r.Path) + ".png");
                using (Bitmap bitmap = WellPlotRenderer.Render(model, Width, Height))
                    bitmap.Save(png, ImageFormat.Png);
                result.Png = png;
            }
            catch (Exception ex)
            {
                result.Error = "plot failed: " + ex.GetType().Name + ": " + ex.Message;
            }
            return result;
        }
    }
}
