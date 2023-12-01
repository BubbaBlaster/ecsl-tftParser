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
            Agora.SDK.Log.SetLevel(Agora.Logging.LogLevel.Info);
            Agora.Forms.Invoker.Configure();
            var form = new Form1();
            "Starting".LogHeading();
            Application.Run(form);
        }
    }
}
