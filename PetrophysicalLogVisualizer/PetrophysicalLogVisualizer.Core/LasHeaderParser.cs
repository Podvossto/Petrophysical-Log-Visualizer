using System;
using System.Globalization;

namespace PetrophysicalLogVisualizer.Core
{
    /// <summary>Reads the ~V ~W ~C lines up to the first data section (D-03, D-04, D-05, D-11).</summary>
    public static class LasHeaderParser
    {
        /// <summary>dataStart = index of the first line after ~A, or -1 when there is no data section.</summary>
        public static LasHeader Parse(string[] lines, out int dataStart)
        {
            var header = new LasHeader();
            dataStart = -1;
            char section = 'X';
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                if (line[0] == '~')
                {
                    section = SectionOf(line);
                    if (section == 'A') { dataStart = i + 1; break; }   // later sections are ignored (D-11)
                    continue;
                }
                if (section == 'V' || section == 'W' || section == 'C')
                    ParseLine(line, section, i + 1, header);
            }
            if (dataStart < 0) header.Warnings.Add("No ~A data section found");
            return header;
        }

        // LAS 2.0 names are ~V ~W ~C ~P ~A; LAS 3.0 spells them out (~Curve, ~Ascii, ~Log_Data).
        private static char SectionOf(string line)
        {
            string name = line.Substring(1).TrimStart().ToUpperInvariant();
            if (name.StartsWith("A") || name.StartsWith("LOG_DATA")) return 'A';
            if (name.StartsWith("V")) return 'V';
            if (name.StartsWith("W")) return 'W';
            if (name.StartsWith("C") && !name.StartsWith("COMMENT")) return 'C';
            return 'X';
        }

        // MNEM.UNIT  VALUE : DESCRIPTION  - mnemonic ends at the first '.', description starts after the last ':' (D-05)
        private static void ParseLine(string line, char section, int lineNo, LasHeader header)
        {
            int dot = line.IndexOf('.');
            if (dot < 0)
            {
                header.Warnings.Add("Line " + lineNo + ": header line has no '.', skipped: " + line);
                return;
            }
            int colon = line.LastIndexOf(':');
            if (colon < dot) colon = line.Length;

            string mnemonic = line.Substring(0, dot).Trim();
            string rest = line.Substring(dot + 1, colon - dot - 1);
            string description = colon < line.Length ? line.Substring(colon + 1).Trim() : "";

            int unitEnd = 0;
            while (unitEnd < rest.Length && !char.IsWhiteSpace(rest[unitEnd])) unitEnd++;
            string unit = rest.Substring(0, unitEnd);
            string value = rest.Substring(unitEnd).Trim();

            if (section == 'C')
            {
                header.Curves.Add(new CurveInfo(mnemonic, unit, description));
                return;
            }
            switch (mnemonic.ToUpperInvariant())
            {
                case "VERS": header.Version = ReadNumber(value, mnemonic, lineNo, header); break;
                case "STRT": header.Strt = ReadNumber(value, mnemonic, lineNo, header); break;
                case "STOP": header.Stop = ReadNumber(value, mnemonic, lineNo, header); break;
                case "STEP": header.Step = ReadNumber(value, mnemonic, lineNo, header); break;
                case "NULL": header.NullValue = ReadNumber(value, mnemonic, lineNo, header); break;
                case "WRAP": header.Wrap = ReadYesNo(value, lineNo, header); break;
            }
        }

        private static double? ReadNumber(string text, string mnemonic, int lineNo, LasHeader header)
        {
            double d;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return d;
            header.Warnings.Add("Line " + lineNo + ": cannot read " + mnemonic + " value '" + text + "'");
            return null;
        }

        private static bool? ReadYesNo(string text, int lineNo, LasHeader header)
        {
            if (text.Equals("YES", StringComparison.OrdinalIgnoreCase)) return true;
            if (text.Equals("NO", StringComparison.OrdinalIgnoreCase)) return false;
            header.Warnings.Add("Line " + lineNo + ": cannot read WRAP value '" + text + "'");
            return null;
        }
    }
}
