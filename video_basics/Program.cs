using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using C_sawapan_media;

namespace testmediasmall
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainWindowForm());
          //  Application.Run(new RGBHistogram());

            MediaIO.UnInitialize();
        }

    }
}
