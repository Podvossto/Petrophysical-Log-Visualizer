using System;
using System.Windows.Forms;

namespace PetrophysicalLogVisualizer.App
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length == 3 && args[0] == "--batch")
                return BatchRunner.Run(args[1], args[2], Console.Out);

            Console.WriteLine("Usage: PetrophysicalLogVisualizer.App.exe --batch <inFolder> <outFolder>");
            Console.WriteLine("The window is not built yet.");
            return 2;
        }
    }
}
