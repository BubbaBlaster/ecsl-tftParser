using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TFT
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Agora.Logging.AgoraLogger.SetVerbosity("Info");
            "Starting".LogHeading();

            Agora.Forms.Invoker.Configure();
            Application.Run(new Form1());
        }
    }
}
