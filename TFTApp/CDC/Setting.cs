using CDC.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace CDC.Configuration
{
    public class Setting : IXmlSerializable
    {
        public Setting() 
        {
            Settings = new Dictionary<string, Setting>();
        }

        public void Add(string name, string value, string description = "")
        {
            Setting setting = new Setting() { Name = name, Value = value, Description = description };
            Add(setting);
        }

        public void Add(Setting setting)
        {
            if (Settings.ContainsKey(setting.Name))
                Settings[setting.Name] = setting;
            else
                Settings.Add(setting.Name, setting);
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
            if (!string.Equals(Name, p.Name) ||
                !string.Equals(Value, p.Value) ||
                !string.Equals(Description, p.Description) ||
                Settings.Count != p.Settings.Count)
                return false;

            var pSetting = p.Settings.Values.GetEnumerator();
            pSetting.MoveNext();
            foreach(var s in Settings.Values)
            {
                if (!s.Equals(pSetting.Current)) return false;
                pSetting.MoveNext();
            }

            return true;
        }

        public override int GetHashCode()
        {
            return (Name + Value + Description).GetHashCode();
        }

        public string Value { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
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

        [ExcludeFromCodeCoverage]
        public void Load(string filename)
        {
            try
            {
                if (File.Exists(filename))
                {
                    using (Stream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
                    {
                        XmlReaderSettings settings = new XmlReaderSettings()
                        {
                            IgnoreWhitespace = true
                        };
                        XmlReader reader = XmlReader.Create(fs);
                        ReadXml(reader);
                        fs.Close();
                    }
                }
            }
            catch(DuplicateSettingException de)
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
            catch(Exception ex)
            {
                var e = new FileLoadException("Error while loading configuration file '" + filename + "'.", ex);
                Debug.Assert(false, e.Message + Environment.NewLine + ex.Message + (ex.InnerException != null ? Environment.NewLine + ex.InnerException.Message : ""));
                throw e;
            }
        }

        public void Save(string filename)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Setting));
            using (Stream fs = new FileStream(filename, FileMode.Create))
            {
                XmlWriterSettings settings = new XmlWriterSettings()
                {
                    Indent = true,
                    Encoding = Encoding.UTF8
                };
                XmlWriter writer = XmlWriter.Create(fs, settings);
                serializer.Serialize(writer, this);
                writer.Close();
                fs.Close();
            }
        }

        public void NewSave(string filename)
        {
            using (Stream fs = new FileStream(filename, FileMode.Create))
            {
                XmlWriterSettings settings = new XmlWriterSettings()
                {
                    Indent = true,
                    Encoding = Encoding.UTF8
                };
                XmlWriter writer = XmlWriter.Create(fs, settings);
                Serialize(writer, this);
                writer.Close();
                fs.Close();
            }
        }

        private static void Serialize(XmlWriter w, Setting s)
        {
            w.WriteStartElement("Setting");
            w.WriteAttributeString("name", s.Name);
            w.WriteAttributeString("value", s.Value);
            if( !string.IsNullOrEmpty(s.Description) ) w.WriteElementString("Description", s.Description);
            foreach (var subSetting in s.Settings.Values)
                Serialize(w, subSetting);
            w.WriteEndElement();
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

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            reader.MoveToContent();
            string name = reader.GetAttribute("name");
            string value = reader.GetAttribute("value");
            if (value == null) value = "";
            string typeCheck = reader.GetAttribute("typeCheck");
            string description = null;

            if (string.IsNullOrEmpty(Name))
                Name = name;

            if (string.IsNullOrEmpty(Value))
                Value = value;
            else if (Value != value)
                throw new DuplicateSettingException(name);

            if( !reader.IsEmptyElement )
            {
                bool bFirstTime = true;
                while (reader.Read())
                {
                    if( bFirstTime )
                        if( reader.NodeType == XmlNodeType.Element && reader.Name == "Description" )
                        {
                            if (!reader.IsEmptyElement)
                            {
                                reader.MoveToContent();
                                description = reader.ReadElementContentAsString();
                                if (string.IsNullOrEmpty(Description))
                                    Description = description;
                                else if (Description != description)
                                    throw new DuplicateSettingException(name);
                            }
                            bFirstTime = false;
                        }
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "Setting")
                    {
                        string aname = reader.GetAttribute("name");
                        if (!string.IsNullOrEmpty(aname))
                        {
                            if (Settings.Keys.Contains(aname))
                            {
                                Settings[aname].ReadXml(reader);
                            }
                            else
                            {
                                Setting S = new Setting();
                                S.ReadXml(reader);
                                Settings[aname] = S;
                            }
                        }
                        bFirstTime = false;
                    }
                    else if( reader.NodeType != XmlNodeType.Whitespace)
                        break;
                }
            }
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("value", Value);            
            if (!string.IsNullOrEmpty(Description))
                writer.WriteElementString("Description", Description);

            foreach(var name in Settings)
            {
                writer.WriteStartElement("Setting");
                name.Value.WriteXml(writer);
                writer.WriteEndElement();
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
