using CDC.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security;

namespace CDC.Configuration
{
    public class AppConfiguration : Setting
    {
        static public AppConfiguration AppConfig { get; } = new AppConfiguration();
        private AppConfiguration()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);

            if (Directory.Exists(path.Substring(0, path.LastIndexOf('/') + 1) + "Config"))
            {
                Load(path.Substring(0, path.LastIndexOf('/') + 1) + "Config", "*.config.json");
            }
            if (Directory.Exists(path.Substring(0, path.LastIndexOf('/') + 1)))
            {
                Load(path.Substring(0, path.LastIndexOf('/') + 1), "*.config.json");
            }
        }        
    }
}
