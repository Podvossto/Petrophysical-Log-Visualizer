using System;
using System.IO;
using System.Text;

namespace PetrophysicalLogVisualizer.App
{
    /// <summary>A TextWriter for BatchRunner's log: a line starting "[3/203] ..." calls onProgress(3, 203); other lines are dropped.</summary>
    public sealed class ProgressWriter : TextWriter
    {
        private readonly Action<int, int> onProgress;
        public ProgressWriter(Action<int, int> onProgress) { this.onProgress = onProgress; }
        public override Encoding Encoding { get { return Encoding.UTF8; } }

        public override void WriteLine(string line)
        {
            if (line == null || line.Length < 5 || line[0] != '[') return;
            int slash = line.IndexOf('/'), close = line.IndexOf(']');
            int done, total;
            if (slash > 0 && close > slash && int.TryParse(line.Substring(1, slash - 1), out done) && int.TryParse(line.Substring(slash + 1, close - slash - 1), out total))
                onProgress(done, total);
        }
    }
}
