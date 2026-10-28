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

            if (args.Length == 0)
            {
                Application.EnableVisualStyles();
                Application.Run(new MainForm());
                return 0;
            }

            Console.WriteLine("Usage: PetrophysicalLogVisualizer.App.exe [--batch <inFolder> <outFolder>]");
            Console.WriteLine("With no arguments the window opens.");
            return 2;
        }
    }
}
