using CDC.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using Newtonsoft.Json;

namespace CDC.Configuration
{
    public class Setting
    {
        public Setting()
        {
            Settings = new Dictionary<string, Setting>();
        }

        public void Add(string name, string value)
        {
            Setting setting = new Setting() { Value = value };
            Add(name, setting);
        }

        public void Add(string Name, Setting setting)
        {
            if (Settings.ContainsKey(Name))
                Settings[Name] = setting;
            else
                Settings.Add(Name, setting);
        }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
                return false;

            // If parameter cannot be cast to Point return false.
            Setting p = obj as Setting;
            if (p == null)
                return false;

            return Equals(p);
        }

        public bool Equals(Setting p)
        {
            // If parameter is null return false:
            if (p == null)
                return false;

            // Return true if the fields match:
            if (!string.Equals(Value, p.Value) ||
                Settings.Count != p.Settings.Count)
                return false;

            var pSetting = p.Settings.Values.GetEnumerator();
            pSetting.MoveNext();
            foreach (var s in Settings.Values)
            {
                if (!s.Equals(pSetting.Current)) return false;
                pSetting.MoveNext();
            }

            return true;
        }

        public string Value { get; set; }
        public Dictionary<string, Setting> Settings { get; set; }

        [ExcludeFromCodeCoverage]
        public void Load(string directory, string searchPattern)
        {
            ILogger Logger = CDCLogger.Log;

            IEnumerable<string> filenames = null;
            try
            {
                filenames = Directory.EnumerateFiles(directory, searchPattern);
            }
            catch (ArgumentNullException ex)
            {
                Logger.Warning(ex, "Directory or SearchPattern for finding configuration files is null. <" + directory + "/" + searchPattern + ">");
                throw ex;
            }
            catch (ArgumentException ex)
            {
                Logger.Warning(ex, "Directory or SearchPattern for finding configuration files is invalid. <" + directory + "/" + searchPattern + ">");
                throw ex;
            }
            catch (DirectoryNotFoundException ex)
            {
                Logger.Warning(ex, "Directory for finding configuration files is invalid. <" + directory + "/" + searchPattern + ">");
                throw ex;
            }
            catch (PathTooLongException ex)
            {
                Logger.Warning(ex, "Directory passed to Load exists as a file. <" + directory + "/" + searchPattern + ">");
                throw ex;
            }
            catch (IOException ex)
            {
                Logger.Warning(ex, "Directory passed to Load exists as a file. <" + directory + "/" + searchPattern + ">");
                throw ex;
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Warning(ex, "A permission problem exists while searching <" + directory + "/" + searchPattern + ">");
                throw ex;
            }
            catch (SecurityException ex)
            {
                Logger.Warning(ex, "A permission problem exists while searching <" + directory + "/" + searchPattern + ">");
                throw ex;
            }

            foreach (var filename in filenames)
            {
                try
                {
                    Load(filename);
                }
                catch (DuplicateSettingException de)
                {
                    throw new FileLoadException("Could not load configuration file <" + filename + "> due to 'DuplicateSettingException'.  " + de.Message, filename, de);
                }
            }
        }

        private void MergeSettings(Setting s)
        {
            foreach(var setting in s.Settings)
            {
                if (Settings.TryGetValue(setting.Key, out Setting existing))
                {
                    if (existing.Value != setting.Value.Value)
                        throw new DuplicateSettingException(setting.Key);
                    existing.MergeSettings(setting.Value);
                }
                else
                {
                    Settings.Add(setting.Key, setting.Value);
                }
            }
        }

        [ExcludeFromCodeCoverage]
        public void Load(string filename)
        {
            try
            {
                if (File.Exists(filename))
                {
                    using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs, Encoding.Default))
                    {
                        JsonTextReader reader = new JsonTextReader(sr);
                        JsonSerializer serializer = new JsonSerializer();
                        var setting = (Setting)serializer.Deserialize<Setting>(reader);
                        MergeSettings(setting);
                        reader.Close();
                        sr.Close();
                        fs.Close();
                    }
                }
            }
            catch (DuplicateSettingException de)
            {
                var e = new FileLoadException("Error while loading configuration file '" + filename + "'.", de);
                Debug.Assert(false, e.Message + Environment.NewLine + de.Message + (de.InnerException != null ? Environment.NewLine + de.InnerException.Message : ""));
                throw e;
            }
            catch (SettingTypeCheckException se)
            {
                var e = new FileLoadException("Error while loading configuration file '" + filename + "'.", se);
                Debug.Assert(false, e.Message + Environment.NewLine + se.Message + (se.InnerException != null ? Environment.NewLine + se.InnerException.Message : ""));
                throw e;
            }
            catch (Exception ex)
            {
                var e = new FileLoadException("Error while loading configuration file '" + filename + "'.", ex);
                Debug.Assert(false, e.Message + Environment.NewLine + ex.Message + (ex.InnerException != null ? Environment.NewLine + ex.InnerException.Message : ""));
                throw e;
            }
        }

        public void Save(string filename)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Formatting = Formatting.Indented;

            using (var fs = new FileStream(filename, FileMode.Create))
            using (var sw = new StreamWriter(fs, Encoding.Default))
            using (var jw = new JsonTextWriter(sw))
            {
                serializer.Serialize(jw, this);
                sw.Close();
                fs.Close();
            }
        }

        /// <summary>
        /// Gets a configuration setting Node.  Use the Configuration Editor to modify configuration for safety.
        /// Example: string name = Asteria.Configuration.Get("DataSourceManager.DataSource.1").Value;
        /// </summary>
        /// <param name="key">The path to the Configuration Node.</param>
        /// <returns>Asteria.Configuration.Node</returns>
        public bool TryGetSetting(string key, out Setting outNode, bool AssertExists = true)
        {
            string[] path = key.Split('.');
            Setting current = this;
            foreach (var p in path)
            {
                if (current.Settings.Keys.Contains(p))
                    current = current.Settings[p];
                else
                {
                    current = null;
                    break;
                }
            }
            outNode = current;
            bool bExists = outNode != null;

            if (AssertExists)
                Trace.Assert(bExists, key + "Setting \"" + key + "\" not found in Configuration.");

            return bExists;
        }

        public bool TryGetValue(string settingName, out string value)
        {
            if (Settings.TryGetValue(settingName, out Setting setting))
            {
                value = setting.Value;
                return true;
            }
            else
            {
                value = "";
                return false;
            }
        }

        public bool TryGetValue(string settingName, out bool value)
        {
            if (Settings.TryGetValue(settingName, out Setting setting))
                return Boolean.TryParse(setting.Value, out value);
            else
            {
                value = false;
                return false;
            }
        }

        public bool TryGetValue(string settingName, out int value)
        {
            if (Settings.TryGetValue(settingName, out Setting setting))
                return int.TryParse(setting.Value, out value);
            else
            {
                value = int.MinValue;
                return false;
            }
        }
    }

    public class SettingTypeCheckException : Exception
    {
        public SettingTypeCheckException( string name, string value, string type )
        {
            Message = "Setting Type Check for <" + name + ">.Value = '" + value + "' as (" + type + ") failed.";
        }

        public override string Message { get; }
    }

    public class MissingSetting : Exception
    {
        public MissingSetting( string name )
        {
            Message = "Setting <" + name + "> missing.";
        }

        public override string Message { get; }
    }

    public class DuplicateSettingException : Exception
    {
        public DuplicateSettingException( string name )
        {
            Message = "Duplicate setting <" + name + "> found. Don't do this...";
        }

        public override string Message { get; }
    }
}
