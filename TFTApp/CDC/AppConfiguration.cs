using System;
using System.IO;
using System.Reflection;

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
                Load(path.Substring(0, path.LastIndexOf('/') + 1) + "Config", "*.config.xml");
            }
            if (Directory.Exists(path.Substring(0, path.LastIndexOf('/') + 1)))
            {
                Load(path.Substring(0, path.LastIndexOf('/') + 1), "*.config.xml");
            }
        }        
    }
}
