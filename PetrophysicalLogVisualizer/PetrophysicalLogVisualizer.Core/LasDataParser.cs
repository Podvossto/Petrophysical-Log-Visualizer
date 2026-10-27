using System;
using System.Globalization;

namespace PetrophysicalLogVisualizer.Core
{
    /// <summary>Reads the ~A lines into columns (D-06 to D-10). Never throws on bad data: problems become warnings.</summary>
    public sealed class LasDataParser
    {
        private readonly int columns;
        private readonly bool wrap;
        private readonly LasData data;
        private double[] depth = new double[0];
        private float[][] curves;
        private int rows;       // finished rows
        private int col;        // next column to fill in the current row
        private int lineNo;     // 1-based line number of the line being read

        private readonly int[] firstBadLine;
        private int shortRows, shortAt, longRows, longAt, skippedRows, skippedAt;

        private LasDataParser(int columns, bool wrap)
        {
            this.columns = columns;
            this.wrap = wrap;
            data = new LasData { BadTokens = new int[columns] };
            firstBadLine = new int[columns];
            curves = new float[columns - 1][];
            for (int c = 0; c < curves.Length; c++) curves[c] = new float[0];
        }

        /// <summary>columns = number of curves in the header, depth included. dataStart = index of the first data line.</summary>
        public static LasData Parse(string[] lines, int dataStart, int columns, bool wrap)
        {
            if (columns < 1 || dataStart < 0)
            {
                var empty = new LasData { Depth = new double[0], Curves = new float[0][], BadTokens = new int[0] };
                empty.Warnings.Add("No data read: no ~A section or no curves in the header");
                return empty;
            }
            bool detected = !wrap && LooksWrapped(lines, dataStart, columns);
            LasData data = new LasDataParser(columns, wrap || detected).Run(lines, dataStart);
            if (detected)
                data.Warnings.Insert(0, "Header says WRAP NO but the values are spread over several lines; read as wrapped");
            return data;
        }

        // D-12: the first row is wrapped when it needs more than one line to reach exactly `columns` values.
        // A row that is merely short (D-09) never adds up to `columns` exactly with the line after it.
        public static bool LooksWrapped(string[] lines, int dataStart, int columns)
        {
            int sum = 0, used = 0;
            for (int i = dataStart; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0) continue;
                if (line[0] == '~' || line == "\u001A") return false;
                sum += CountTokens(line);
                used++;
                if (sum == columns) return used > 1;
                if (sum > columns) return false;
            }
            return false;
        }

        private static int CountTokens(string line)
        {
            int count = 0, pos = 0;
            while (true)
            {
                while (pos < line.Length && IsSeparator(line[pos])) pos++;
                if (pos >= line.Length) return count;
                count++;
                while (pos < line.Length && !IsSeparator(line[pos])) pos++;
            }
        }

        private LasData Run(string[] lines, int dataStart)
        {
            int stoppedAt = 0;
            for (int i = dataStart; i < lines.Length && stoppedAt == 0; i++)
            {
                lineNo = i + 1;
                string line = lines[i];
                int pos = 0;
                while (pos < line.Length && char.IsWhiteSpace(line[pos])) pos++;
                if (pos < line.Length && line[pos] == '~')   // D-11: a new section ends the data
                {
                    data.Warnings.Add("Line " + lineNo + ": another section starts here; the rest of the file is not read");
                    break;
                }
                pos = 0;
                while (true)
                {
                    while (pos < line.Length && IsSeparator(line[pos])) pos++;
                    if (pos >= line.Length) break;
                    int start = pos;
                    while (pos < line.Length && !IsSeparator(line[pos])) pos++;
                    string token = line.Substring(start, pos - start);
                    if (token == "\u001A") { stoppedAt = lineNo; break; }   // D-06: DOS end-of-file marker
                    if (col >= columns) { if (longRows++ == 0) longAt = lineNo; continue; }   // D-09: extra values unused
                    Store(token);
                    if (wrap && col == columns) EndRow();
                }
                if (!wrap && col > 0) EndRow();
            }
            if (col > 0) EndRow();   // a wrapped row cut short by the end of the file
            if (stoppedAt > 0) data.Warnings.Add("Line " + stoppedAt + ": data ended at a Ctrl-Z (0x1A) marker");
            return Finish();
        }

        private static bool IsSeparator(char c)
        {
            return c == ' ' || c == '\t' || c == ',' || c == '\r';
        }

        private void Store(string token)
        {
            if (rows == depth.Length) Grow();
            double d;
            bool ok = double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out d)
                      && !double.IsNaN(d) && !double.IsInfinity(d);
            float f = ok ? (float)d : float.NaN;
            if (ok && float.IsInfinity(f)) { f = float.NaN; ok = false; }   // too large for float

            if (col == 0) depth[rows] = ok ? d : double.NaN;
            else curves[col - 1][rows] = f;

            if (!ok)
            {
                data.BadTokens[col]++;
                if (firstBadLine[col] == 0) firstBadLine[col] = lineNo;
            }
            col++;
        }

        // Closes the current row: missing values become NaN (D-09); a row without a depth is dropped and reported (D-08).
        private void EndRow()
        {
            if (col < columns)
            {
                if (shortRows++ == 0) shortAt = lineNo;
                if (rows == depth.Length) Grow();
                for (int c = col; c < columns; c++)
                {
                    if (c == 0) depth[rows] = double.NaN;
                    else curves[c - 1][rows] = float.NaN;
                }
            }
            col = 0;
            if (double.IsNaN(depth[rows]))
            {
                if (skippedRows++ == 0) skippedAt = lineNo;
                return;
            }
            rows++;
        }

        private void Grow()
        {
            int size = Math.Max(16, depth.Length * 2);
            Array.Resize(ref depth, size);
            for (int c = 0; c < curves.Length; c++) Array.Resize(ref curves[c], size);
        }

        private LasData Finish()
        {
            Array.Resize(ref depth, rows);
            for (int c = 0; c < curves.Length; c++) Array.Resize(ref curves[c], rows);
            data.Depth = depth;
            data.Curves = curves;
            data.Rows = rows;

            if (shortRows > 0)
                data.Warnings.Add(shortRows + " rows have fewer than " + columns + " values (first at line " + shortAt + "); missing values set to NaN");
            if (longRows > 0)
                data.Warnings.Add(longRows + " extra values beyond " + columns + " columns ignored (first at line " + longAt + ")");
            if (skippedRows > 0)
                data.Warnings.Add(skippedRows + " rows skipped because the depth is not a number (first at line " + skippedAt + ")");
            for (int c = 0; c < columns; c++)
                if (data.BadTokens[c] > 0)
                    data.Warnings.Add("Column " + (c + 1) + ": " + data.BadTokens[c] + " values are not numbers, set to NaN (first at line " + firstBadLine[c] + ")");
            return data;
        }
    }
}
