using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Asteria.Utilities;
using Asteria.Configuration;
using Asteria.Logging;
using static Asteria.Logging.AsteriaLogger;

namespace addresses
{
    public class Database
    {
        public DataTable _Data { get; } = new DataTable();

        private DataTable _rawTFTData;
        private DataTable _rawPSData;
        private DataTable _rawSpecialData;
        private DataTable _rawNoShows;
        private DataTable _rawBanned;

        ObservableString _currentOperation = ObservableString.Get("CurrentOperation");
        private string _OutputDir, _InputDir;
        private string _TFT_Filename, _PS_Filename, _Special_Filename, _Banned_Filename, _NoShowRaw_Filename;
        private List<string> _NoShow_Filenames;

        Dictionary<string, int> _NoShowPhones, _NoShowEmails;
        HashSet<string> _BannedPhones, _BannedEmails;
        List<int> _NoShowRawList;

        public Database()
        {
            AppConfiguration.AppConfig.TryGetSetting("Data.InputDir", out Setting indir);
            _InputDir = indir.Val;

            if (!Directory.Exists(_InputDir))
                throw new Exception("Input directory not found.");

            AppConfiguration.AppConfig.TryGetSetting("Data.OutputDir", out Setting outdir);
            _OutputDir = outdir.Val;
            Directory.CreateDirectory(_OutputDir);

            AppConfiguration.AppConfig.TryGetSetting("Data.TFTFilename", out Setting tftname);
            _TFT_Filename = tftname.Val;

            AppConfiguration.AppConfig.TryGetSetting("Data.PSFilename", out Setting psname);
            _PS_Filename = psname.Val;

            AppConfiguration.AppConfig.TryGetSetting("Data.SpecialFilename", out Setting specialname);
            _Special_Filename = specialname.Val;

            AppConfiguration.AppConfig.TryGetSetting("Data.NoShowFilename", out Setting noShowName);
            _NoShow_Filenames = new List<string>();
            foreach (var s in noShowName.Settings)
                _NoShow_Filenames.Add(s.Value.Val);

            AppConfiguration.AppConfig.TryGetSetting("Data.BannedFilename", out Setting BannedName);
            _Banned_Filename = BannedName.Val;

            AppConfiguration.AppConfig.TryGetSetting("Data.NoShowRaw", out Setting NoShowRaw);
            _NoShowRaw_Filename = NoShowRaw.Val;

            Clear();
        }

        public void Initialize()
        {
            try
            {
                Log.Write(LogLevel.Info, "Initializing Database");

                ReadNoShows();

                ReadBanned();

                ReadTFTRawData();
                ReadPSRawData();
                ReadSpecialRawData();

                Merge();

                ComputeBreakOut();

                WriteBook1();
                WriteBook2();
                WriteBook3();
                WriteBook4();
                WriteSpecial();

                WriteProjectSmileInvitations();
                WriteTFTEmails();

                ProcessNoShowsRaw();

                CheckDuplicates();

                ComputeNoShows();
            }
            catch (Exception e)
            {
                Log.WriteException(LogLevel.Fatal, e, "Exception while creating database.");
                return;
            }
        }

        bool ProcessNoShowsRaw()
        {
            Log.Write(LogLevel.Info, "Processing NoShowsRaw");

            _NoShowRawList = new List<int>();

            try
            {
                int lineNumber = 0;
                string filename = _InputDir + "/" + _NoShowRaw_Filename;
                if (!File.Exists(filename))
                {
                    Log.Write(LogLevel.Warn, "NoShowsRaw: '" + _NoShowRaw_Filename + "' not found in '" + _InputDir + "' - Skipping");
                    return false;
                }

                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.Default))
                {
                    String line;
                    while (sr.Peek() > 0)
                    {
                        line = sr.ReadLine();
                        if (string.IsNullOrEmpty(line)) break;

                        lineNumber++;
                        _NoShowRawList.Add(Int32.Parse(line));
                    }
                }
            }
            catch (Exception e)
            {
                Log.WriteException(LogLevel.Fatal, e, "Exception encountered while processing NoShowsRaw");
                return false;
            }

            if (_NoShowRawList.Count == 0)
                return false;

            List<string> output = new List<string>();
            output.Add("CONTACT LAST,CONTACT FIRST,PHONE,EMAIL");

            foreach (var id in _NoShowRawList)
            {
                DataRow row = _Data.Rows.Find(id);
                if (row != null)
                {
                    output.Add($"{row[ColumnName.ContactLast]},{row[ColumnName.ContactFirst]},{row[ColumnName.Phone]},{row[ColumnName.Email]}");
                }
            }

            File.WriteAllLines(_InputDir + "/ProcessedNoShows.csv", output);

            Log.Write(LogLevel.Info, "Processing NoShowRaw DB - Done");

            return true;
        }

        public void Penalize(DataRow r1)
        {
            int NS = 0;
            if (_NoShowPhones.Keys.Contains((string)r1[ColumnName.Phone]))
                NS = _NoShowPhones[(string)r1[ColumnName.Phone]]; // get the penalized value
            if (!(r1[ColumnName.Email] is System.DBNull) &&
                 _NoShowEmails.Keys.Contains((string)r1[ColumnName.Email]))
                NS = _NoShowEmails[(string)r1[ColumnName.Email]];

            if (NS > 0)
            {
                if (r1[ColumnName.Penalty] is System.DBNull)
                    r1[ColumnName.Penalty] = NS;
                else
                    r1[ColumnName.Penalty] = NS + (int)r1[ColumnName.Penalty];
            }

            if (_BannedPhones.Contains((string)r1[ColumnName.Phone]))
                r1[ColumnName.Status] = "Banned";

            if (_BannedEmails.Contains((string)r1[ColumnName.Email]))
                r1[ColumnName.Status] = "Banned";
        }

        private bool ReadBanned()
        {
            Log.Write(LogLevel.Info, "Reading Banned");
            var raw = _rawBanned = new DataTable();
            try
            {
                int lineNumber = 0;
                string filename = _InputDir + "/" + _Banned_Filename;
                if (!File.Exists(filename))
                {
                    Log.Write(LogLevel.Warn, "Banned DB: '" + _Banned_Filename + "' not found in '" + _InputDir + "' - Skipping");
                    return false;
                }

                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.Default))
                {
                    String line = sr.ReadLine();
                    lineNumber++;
                    String[] header = SplitCSV(line);
                    for (int i = 0; i < header.Length; i++)
                    {
                        string columnName = header[i] = header[i].Trim(new char[] { '"', '\\', ',' });
                        raw.Columns.Add(columnName);
                    }
                    raw.PrimaryKey = new DataColumn[1] { raw.Columns[ColumnName.ControlNumber] };

                    bool go = true;
                    do
                    {
                        line = sr.ReadLine();
                        string[] tagInfo = SplitCSV(line);

                        if (tagInfo.Length != header.Length)
                        {
                            Log.Write(LogLevel.Warn, "Line " + lineNumber + " - Length wrong: " + line);
                            go = false;
                        }
                        else
                        {
                            DataRow row = raw.NewRow();
                            for (int ii = 0; ii < header.Length; ii++)
                            {
                                string tag = tagInfo[ii].Trim(new char[] { '"', '\\', ',' });
                                row[header[ii]] = Pretty(tag);
                            }

                            CorrectPhone(row);
                            raw.Rows.Add(row);
                        }
                        if (sr.EndOfStream)
                            go = false;
                    } while (go);
                }
            }
            catch (Exception e)
            {
                Log.WriteException(LogLevel.Warn, e, "Reading Banned DB - Failed");
                return false;
            }

            // construct the data to search
            _BannedPhones = new HashSet<string>();
            foreach (DataRow r in _rawBanned.Rows)
                _BannedPhones.Add((string)r[ColumnName.Phone]);

            _BannedEmails = new HashSet<string>();
            foreach (DataRow r in _rawBanned.Rows)
            {
                if (!(r[ColumnName.Email] is System.DBNull))
                    _BannedEmails.Add((string)r[ColumnName.Email]);
            }

            Log.Write(LogLevel.Info, "Reading Banned DB - Done");
            return true;
        }

        private bool ReadNoShows()
        {
            Log.Write(LogLevel.Info, "Reading NoShows");
            var raw = _rawNoShows = new DataTable();
            int index = 0;
            foreach (var fn in _NoShow_Filenames)
            {
                try
                {
                    int lineNumber = 0;
                    string filename = _InputDir + "/" + fn;
                    if (!File.Exists(filename))
                    {
                        Log.Write(LogLevel.Warn, "NoShows DB: '" + fn + "' not found in '" + _InputDir + "' - Skipping");
                        continue;
                    }

                    HashSet<string> _uniqueLines = new HashSet<string>();

                    using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs, Encoding.Default))
                    {
                        String line = sr.ReadLine();
                        lineNumber++;
                        String[] header = SplitCSV(line);
                        if (raw.Columns.Count == 0)
                        {
                            for (int i = 0; i < header.Length; i++)
                            {
                                string columnName = header[i] = header[i].Trim(new char[] { '"', '\\', ',' });
                                raw.Columns.Add(columnName);
                            }
                            raw.Columns.Add(ColumnName.ControlNumber);
                            raw.Columns.Add(ColumnName.Phone2);
                            raw.PrimaryKey = new DataColumn[1] { raw.Columns[ColumnName.ControlNumber] };
                        }

                        bool go = true;
                        string prevPhone = string.Empty;
                        do
                        {
                            line = sr.ReadLine();
                            if (!_uniqueLines.Add(line)) continue;

                            string[] tagInfo = SplitCSV(line);

                            if (tagInfo.Length != header.Length)
                            {
                                Log.Write(LogLevel.Warn, "Line " + lineNumber + " - Length wrong: " + line);
                                go = false;
                            }
                            else
                            {
                                DataRow row = raw.NewRow();
                                for (int ii = 0; ii < header.Length; ii++)
                                {
                                    string tag = tagInfo[ii].Trim(new char[] { '"', '\\', ',' });
                                    row[header[ii]] = Pretty(tag);
                                }

                                if (string.Compare(prevPhone, (string)row[ColumnName.Phone]) != 0)
                                {
                                    row[ColumnName.ControlNumber] = index.ToString();
                                    index++;
                                    CorrectPhone(row);
                                    raw.Rows.Add(row);
                                    prevPhone = (string)row[ColumnName.Phone];
                                }
                            }
                            if (sr.EndOfStream)
                                go = false;
                        } while (go);
                    }
                }
                catch (Exception e)
                {
                    Log.WriteException(LogLevel.Warn, e, "Reading NoShow DB - Failed");
                    return false;
                }

                // construct the data to search
                _NoShowPhones = new Dictionary<string, int>();
                foreach (DataRow r in _rawNoShows.Rows)
                {
                    string key = (string)r[ColumnName.Phone];
                    if (string.IsNullOrEmpty(key)) continue;

                    if (_NoShowPhones.ContainsKey(key))
                        _NoShowPhones[key]++;
                    else
                        _NoShowPhones.Add(key, 1);
                }

                _NoShowEmails = new Dictionary<string, int>();
                foreach (DataRow r in _rawNoShows.Rows)
                {
                    if (!(r[ColumnName.Email] is System.DBNull))
                    {
                        string key = (string)r[ColumnName.Email];
                        if (string.IsNullOrEmpty(key)) continue;

                        if (_NoShowEmails.ContainsKey(key))
                            _NoShowEmails[key] += 10;
                        else
                            _NoShowEmails.Add(key, 10);
                    }
                }
            }

            Log.Write(LogLevel.Info, "Reading NoShow DB - Done");
            return true;
        }

        private void ComputeNoShows()
        {
            int lineNumber = 0;
            int totalKidsNoShow = 0;
            int totalKidsNoShowPS = 0;
            int totalKidsNoShowTFT = 0;
            int totalRegistrations = _Data.Rows.Count;
            int totalRegNoShowPS = 0;
            int totalRegNoShowTFT = 0;

            string filename = _InputDir + "/NoShows-Raw.csv";
            string outFile = _OutputDir + "/NoShows-Processed.csv";

            if (!File.Exists(filename))
            {
                Log.Write(LogLevel.Warn, "Missing <" + filename + ">.  After the event, this file is needed to produce event statistics.");
                return;
            }

            using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs, Encoding.Default))
            using (var of = new StreamWriter(outFile))
            {
                of.WriteLine("CONTACT LAST,CONTACT FIRST,PHONE,EMAIL");
                do
                {
                    String cn = sr.ReadLine();
                    lineNumber++;
                    if (cn.Length > 0)
                    {
                        DataRow row = _Data.Rows.Find(cn);
                        if (row == null)
                        {
                            Log.Write(LogLevel.Warn, "cn = '" + cn + "' - Does not exist.");
                            continue;
                        }
                        row[ColumnName.pDup] = "N";
                        of.WriteLine((string)row[ColumnName.ContactLast] + ',' +
                            (string)row[ColumnName.ContactFirst] + ',' +
                            (string)row[ColumnName.Phone] + ',' +
                            (string)row[ColumnName.Email]);
                        int kids = int.Parse((string)row[ColumnName.Total]);
                        totalKidsNoShow += kids;
                        if (String.Compare("Project Smile", (string)row[ColumnName.Organization]) == 0)
                        {
                            totalKidsNoShowPS += kids;
                            totalRegNoShowPS++;
                        }
                        else
                        {
                            totalKidsNoShowTFT += kids;
                            totalRegNoShowTFT++;
                        }
                    }
                } while (!sr.EndOfStream);
            }

            StreamWriter sw = new StreamWriter(_OutputDir + "/EventStatistics.csv");
            sw.WriteLine("Total Kids," + _totalKids);
            sw.WriteLine("Total PS Kids," + _totalKidsPS);
            sw.WriteLine("Total TFT Kids," + _totalKidsTFT);
            sw.WriteLine("NoShows Kids," + totalKidsNoShow);
            sw.WriteLine("PS NoShows Kids," + totalKidsNoShowPS + " (" + (100f * totalKidsNoShowPS / _totalKidsPS).ToString("00.0") + "%)");
            sw.WriteLine("TFT NoShows Kids," + totalKidsNoShowTFT + " (" + (100f * totalKidsNoShowTFT / _totalKidsTFT).ToString("00.0") + "%)");
            sw.WriteLine("Total Kids Served," + (_totalKids - totalKidsNoShowPS - totalKidsNoShowTFT));
            sw.WriteLine();
            sw.WriteLine("Total Num Regs," + totalRegistrations);
            sw.WriteLine("Total PS Regs," + _totalRegPS);
            sw.WriteLine("Total TFT Regs," + _totalRegTFT);
            sw.WriteLine("Total NoShow PS," + totalRegNoShowPS + " (" + (100f * totalRegNoShowPS / _totalRegPS).ToString("00.0") + "%)");
            sw.WriteLine("Total NoShow TFT," + totalRegNoShowTFT + " (" + (100f * totalRegNoShowTFT / _totalRegTFT).ToString("00.0") + "%)");
            sw.WriteLine("Total Families Served," + (totalRegistrations - totalRegNoShowPS - totalRegNoShowTFT));
            sw.Close();
        }

        private void Clear()
        {
            Log.Write(LogLevel.Info, "Initializing Database");
            _Data.Clear();
            foreach (var colName in ColumnName.Array)
                _Data.Columns.Add(colName);

            _Data.PrimaryKey = new DataColumn[1] { _Data.Columns[ColumnName.ControlNumber] };
        }

        static Regex csvSplit = new Regex("(?:^|,)(\"(?:[^\"]+|\"\")*\"|[^,]*)", RegexOptions.Compiled);

        public static string[] SplitCSV(string input)
        {
            List<string> list = new List<string>();
            string curr = null;
            foreach (Match match in csvSplit.Matches(input))
            {
                curr = match.Value;
                if (0 == curr.Length)
                    list.Add("");

                list.Add(curr.TrimStart(','));
            }

            return list.ToArray<string>();
        }

        private bool ReadSpecialRawData()
        {
            Log.Write(LogLevel.Info, "Reading Special DB");
            var raw = _rawSpecialData = new DataTable();
            try
            {
                int lineNumber = 0;
                string filename = _InputDir + "/" + _Special_Filename;
                if (!File.Exists(filename))
                {
                    Log.Write(LogLevel.Warn, "Special DB: '" + _Special_Filename + "' not found in '" + _InputDir + "' - Skipping");
                    return false;
                }

                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.Default))
                {
                    String line = sr.ReadLine();
                    lineNumber++;
                    String[] header = SplitCSV(line);
                    for (int i = 0; i < header.Length; i++)
                    {
                        string columnName = header[i] = header[i].Trim(new char[] { '"', '\\', ',' });
                        raw.Columns.Add(columnName);
                    }
                    raw.Columns.Add(ColumnName.Total);
                    raw.Columns.Add(ColumnName.Status);
                    raw.Columns.Add(ColumnName.Penalty);
                    raw.PrimaryKey = new DataColumn[1] { raw.Columns[ColumnName.ControlNumber] };

                    bool go = true;
                    do
                    {
                        line = sr.ReadLine();
                        string[] tagInfo = SplitCSV(line);

                        if (tagInfo.Length != header.Length)
                        {
                            Log.Write(LogLevel.Warn, "Line " + lineNumber + " - Length wrong: " + line);
                            go = false;
                        }
                        else
                        {
                            DataRow row = raw.NewRow();
                            for (int ii = 0; ii < header.Length; ii++)
                            {
                                string tag = tagInfo[ii].Trim(new char[] { '"', '\\', ',' });
                                row[header[ii]] = Pretty(tag);
                            }
                            row[ColumnName.State] = "Texas";
                            //row[ColumnName.Organization] = "ECSL";
                            row[ColumnName.Status] = "Special";
                            int total = 0;
                            if (!string.IsNullOrEmpty((string)row[ColumnName.ChildAge1])) total++;
                            if (!string.IsNullOrEmpty((string)row[ColumnName.ChildAge2])) total++;
                            if (!string.IsNullOrEmpty((string)row[ColumnName.ChildAge3])) total++;
                            if (!string.IsNullOrEmpty((string)row[ColumnName.ChildAge4])) total++;
                            if (!string.IsNullOrEmpty((string)row[ColumnName.ChildAge5])) total++;
                            if (!string.IsNullOrEmpty((string)row[ColumnName.ChildAge6])) total++;
                            if (!string.IsNullOrEmpty((string)row[ColumnName.ChildAge7])) total++;
                            if (!string.IsNullOrEmpty((string)row[ColumnName.ChildAge8])) total++;
                            if (!string.IsNullOrEmpty((string)row[ColumnName.ChildAge9])) total++;
                            if (!string.IsNullOrEmpty((string)row[ColumnName.ChildAge10])) total++;
                            row[ColumnName.Total] = total;
                            Penalize(row);

                            raw.Rows.Add(row);
                        }
                        if (sr.EndOfStream)
                            go = false;
                    } while (go);
                }
            }
            catch (Exception e)
            {
                Log.WriteException(LogLevel.Warn, e, "Reading Special DB - Failed");
                return false;
            }
            Log.Write(LogLevel.Info, "Reading Special DB - Done");
            return true;
        }

        private bool ReadTFTRawData()
        {
            Log.Write(LogLevel.Info, "Reading Toys-for-Tots Input");
            _rawTFTData = new DataTable();
            int lineNumber = 0;
            try
            {
                string filename = _InputDir + "/" + _TFT_Filename;
                if (!File.Exists(filename))
                {
                    Log.Write(LogLevel.Warn, "TFT DB: '" + _TFT_Filename + "' not found in '" + _InputDir + "' - Skipping");
                    return false;
                }

                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.Default))
                {
                    // Read the header row
                    String line = sr.ReadLine();
                    lineNumber++;
                    String[] header = SplitCSV(line);
                    for (int i = 0; i < header.Length; i++)
                    {
                        string columnName = header[i] = header[i].Trim(new char[] { '"', '\\', ',', ' ' });
                        _rawTFTData.Columns.Add(columnName);
                    }
                    _rawTFTData.Columns.Add(ColumnName.Organization);
                    _rawTFTData.Columns.Add(ColumnName.TimeSlot);
                    _rawTFTData.Columns.Add(ColumnName.TimeSlotIndex);
                    _rawTFTData.Columns.Add(ColumnName.Total);
                    _rawTFTData.Columns.Add(ColumnName.Address2);
                    _rawTFTData.Columns.Add(ColumnName.Penalty);
                    _rawTFTData.PrimaryKey = new DataColumn[1] { _rawTFTData.Columns[ColumnName.ControlNumber] };

                    bool go = true;
                    do
                    {
                        line = sr.ReadLine();
                        lineNumber++;
                        string[] tagInfo = SplitCSV(line);

                        if (tagInfo.Length != header.Length)
                        {
                            Log.Write(LogLevel.Warn, "Line " + lineNumber + " - Length wrong: " + line);
                            go = false;
                        }
                        else
                        {
                            DataRow row = _rawTFTData.NewRow();
                            for (int ii = 0; ii < header.Length; ii++)
                            {
                                string tag = tagInfo[ii].Trim(new char[] { '"', '\\', ',' });
                                row[header[ii]] = Pretty(tag);
                            }
                            if (string.Compare("pending", (string)row[ColumnName.Status], true) == 0 ||
                                string.Compare("verify", (string)row[ColumnName.Status], true) == 0)
                            {
                                row[ColumnName.State] = "Texas";
                                row[ColumnName.TimeSlot] = string.Empty;
                                row[ColumnName.TimeSlotIndex] = string.Empty;
                                row[ColumnName.Organization] = "ToysForTots";
                                int total = 0;
                                if (!string.IsNullOrEmpty((string)row[ColumnName.ChildAge1])) total++;
                                if (!string.IsNullOrEmpty((string)row[ColumnName.ChildAge2])) total++;
                                if (!string.IsNullOrEmpty((string)row[ColumnName.ChildAge3])) total++;
                                if (!string.IsNullOrEmpty((string)row[ColumnName.ChildAge4])) total++;
                                if (!string.IsNullOrEmpty((string)row[ColumnName.ChildAge5])) total++;
                                if (!string.IsNullOrEmpty((string)row[ColumnName.ChildAge6])) total++;
                                if (!string.IsNullOrEmpty((string)row[ColumnName.ChildAge7])) total++;
                                if (!string.IsNullOrEmpty((string)row[ColumnName.ChildAge8])) total++;
                                if (!string.IsNullOrEmpty((string)row[ColumnName.ChildAge9])) total++;
                                if (!string.IsNullOrEmpty((string)row[ColumnName.ChildAge10])) total++;
                                row[ColumnName.Total] = total;
                                Penalize(row);

                                _rawTFTData.Rows.Add(row);
                            }
                        }
                        if (sr.EndOfStream)
                            go = false;
                    } while (go);
                }
            }
            catch (Exception e)
            {
                Log.WriteException(LogLevel.Warn, e, "Reading TFT DB - Failed");
                return false;
            }
            Log.Write(LogLevel.Info, "Reading Toys-for-Tots Input - Done - " + lineNumber + " successfully parsed.");
            return true;
        }

        private bool ReadPSRawData()
        {
            Log.Write(LogLevel.Info, "Reading ProjectSmile DB");
            _rawPSData = new DataTable();
            int lineNumber = 0;
            try
            {
                string filename = _InputDir + "/" + _PS_Filename;
                if (!File.Exists(filename))
                {
                    Log.Write(LogLevel.Warn, "Project Smile DB: '" + _PS_Filename + "' not found in '" + _InputDir + "' - Skipping");
                    return false;
                }

                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.Default))
                {
                    String line = sr.ReadLine();
                    lineNumber++;
                    String[] header = SplitCSV(line);
                    for (int i = 0; i < header.Length; i++)
                    {
                        string columnName = header[i] = header[i].Trim(new char[] { '"', '\\', ',' });
                        _rawPSData.Columns.Add(columnName);
                    }
                    #region Add Missing Rows
                    //rawPSData.Columns.Add(ColumnName.ControlNumber);
                    _rawPSData.Columns.Add(ColumnName.Organization);
                    _rawPSData.Columns.Add(ColumnName.Phone2);
                    _rawPSData.Columns.Add(ColumnName.TimeSlot);
                    _rawPSData.Columns.Add(ColumnName.TimeSlotIndex);
                    _rawPSData.Columns.Add(ColumnName.ChildAge1);
                    _rawPSData.Columns.Add(ColumnName.ChildAge2);
                    _rawPSData.Columns.Add(ColumnName.ChildAge3);
                    _rawPSData.Columns.Add(ColumnName.ChildAge4);
                    _rawPSData.Columns.Add(ColumnName.ChildAge5);
                    _rawPSData.Columns.Add(ColumnName.ChildAge6);
                    _rawPSData.Columns.Add(ColumnName.ChildAge7);
                    _rawPSData.Columns.Add(ColumnName.ChildAge8);
                    _rawPSData.Columns.Add(ColumnName.ChildAge9);
                    _rawPSData.Columns.Add(ColumnName.ChildAge10);
                    _rawPSData.Columns.Add(ColumnName.ChildFirst1);
                    _rawPSData.Columns.Add(ColumnName.ChildFirst2);
                    _rawPSData.Columns.Add(ColumnName.ChildFirst3);
                    _rawPSData.Columns.Add(ColumnName.ChildFirst4);
                    _rawPSData.Columns.Add(ColumnName.ChildFirst5);
                    _rawPSData.Columns.Add(ColumnName.ChildFirst6);
                    _rawPSData.Columns.Add(ColumnName.ChildFirst7);
                    _rawPSData.Columns.Add(ColumnName.ChildFirst8);
                    _rawPSData.Columns.Add(ColumnName.ChildFirst9);
                    _rawPSData.Columns.Add(ColumnName.ChildFirst10);
                    _rawPSData.Columns.Add(ColumnName.ChildLast1);
                    _rawPSData.Columns.Add(ColumnName.ChildLast2);
                    _rawPSData.Columns.Add(ColumnName.ChildLast3);
                    _rawPSData.Columns.Add(ColumnName.ChildLast4);
                    _rawPSData.Columns.Add(ColumnName.ChildLast5);
                    _rawPSData.Columns.Add(ColumnName.ChildLast6);
                    _rawPSData.Columns.Add(ColumnName.ChildLast7);
                    _rawPSData.Columns.Add(ColumnName.ChildLast8);
                    _rawPSData.Columns.Add(ColumnName.ChildLast9);
                    _rawPSData.Columns.Add(ColumnName.ChildLast10);
                    _rawPSData.Columns.Add(ColumnName.ChildGender1);
                    _rawPSData.Columns.Add(ColumnName.ChildGender2);
                    _rawPSData.Columns.Add(ColumnName.ChildGender3);
                    _rawPSData.Columns.Add(ColumnName.ChildGender4);
                    _rawPSData.Columns.Add(ColumnName.ChildGender5);
                    _rawPSData.Columns.Add(ColumnName.ChildGender6);
                    _rawPSData.Columns.Add(ColumnName.ChildGender7);
                    _rawPSData.Columns.Add(ColumnName.ChildGender8);
                    _rawPSData.Columns.Add(ColumnName.ChildGender9);
                    _rawPSData.Columns.Add(ColumnName.ChildGender10);
                    _rawPSData.Columns.Add(ColumnName.Total);
                    _rawPSData.Columns.Add(ColumnName.Penalty);
                    #endregion
                    _rawPSData.PrimaryKey = new DataColumn[1] { _rawPSData.Columns[ColumnName.ControlNumber] };

                    bool go = true;
                    string lastFamilyID = string.Empty;
                    DataRow lastRow = null;
                    int totalKidsInFamily = 0;
                    do
                    {
                        line = sr.ReadLine();
                        lineNumber++;
                        string[] tagInfo = SplitCSV(line);

                        if (tagInfo.Length != header.Length)
                            go = false;
                        else
                        {
                            DataRow row = _rawPSData.NewRow();
                            for (int ii = 0; ii < header.Length; ii++)
                            {
                                string tag = tagInfo[ii].Trim(new char[] { '"', '\\', ',' });
                                row[header[ii]] = Pretty(tag);
                            }
                            row[ColumnName.State] = "Texas";
                            row[ColumnName.Organization] = "Project Smile";
                            row[ColumnName.Phone2] = string.Empty;

                            // correct the age (rounding down) to check to see if child is too old
                            string agekey = "CHILDAGE";
                            if (float.TryParse((string)row[agekey], out float age))
                            {
                                int nAge = (int)age;
                                // Correct the age
                                row[agekey] = nAge.ToString();

                                // Check age
                                if (nAge > 17)
                                {
                                    Log.Write(LogLevel.Warn, "Project Smile - [" + row[ColumnName.ControlNumber] + "] " + row[ColumnName.ContactFirst] + " " + row[ColumnName.ContactLast]
                                         + " child '" + (string)row["CHILDNAME"] + "' is above the age limit.");
                                    continue;
                                }
                            }

                            if (string.Compare("pending", (string)row[ColumnName.Status], true) != 0) continue;

                            if (int.Parse((string)row[ColumnName.FamilyID]) > 0)
                            {
                                if (lastRow != null && string.Compare(lastFamilyID, (string)row[ColumnName.FamilyID]) == 0) // merge into last
                                {
                                    totalKidsInFamily++;
                                    if (totalKidsInFamily > 10)
                                        Log.Write(LogLevel.Info, $"Project Smile - [{row[ColumnName.FamilyID]}] has {totalKidsInFamily} kids!");
                                    else
                                    {
                                        lastRow[ColumnName.ChildAge + totalKidsInFamily] = row[ColumnName.ChildAge];
                                        lastRow[ColumnName.ChildFirst + totalKidsInFamily] = row[ColumnName.ChildFirst];
                                        lastRow[ColumnName.ChildLast + totalKidsInFamily] = row[ColumnName.ChildLast];
                                        lastRow[ColumnName.ChildGender + totalKidsInFamily] = row[ColumnName.ChildGender];
                                        lastRow[ColumnName.Total] = totalKidsInFamily;
                                    }
                                }
                                else
                                {
                                    totalKidsInFamily = 1;
                                    lastFamilyID = (string)row[ColumnName.FamilyID];
                                    lastRow = row;
                                    row[ColumnName.ControlNumber] = row[ColumnName.FamilyID];
                                    row[ColumnName.ChildAge1] = row[ColumnName.ChildAge];
                                    row[ColumnName.ChildFirst1] = row[ColumnName.ChildFirst];
                                    row[ColumnName.ChildLast1] = row[ColumnName.ChildLast];
                                    row[ColumnName.ChildGender1] = row[ColumnName.ChildGender];
                                    row[ColumnName.Total] = totalKidsInFamily;
                                    Penalize(row);

                                    if (string.Compare("Pending", (string)row[ColumnName.Status]) == 0)
                                        _rawPSData.Rows.Add(row);
                                }
                            }
                        }
                        if (sr.EndOfStream)
                            go = false;
                    } while (go);
                }
            }
            catch (Exception e)
            {
                Log.WriteException(LogLevel.Warn, e, "Reading ProjectSmile DB - Failed");
                return false;
            }
            Log.Write(LogLevel.Info, "Reading ProjectSmile DB - Done - " + lineNumber + " Parsed");
            return true;
        }

        public void WriteSpecial()
        {
            Log.Write(LogLevel.Info, "Writing Special");
            var Entries = from myRow in _Data.AsEnumerable()
                          where (myRow.Field<string>(ColumnName.Organization).Contains("Late"))
                          orderby myRow.Field<string>(ColumnName.ControlNumber) ascending, myRow.Field<string>(ColumnName.ContactLast)
                          select myRow;

            int page = 1;
            foreach (var e in Entries)
            {
                e[ColumnName.BookNumber] = "Late";
                e[ColumnName.PageNumber] = page++;
            }
            WriteCSV("LateOut.csv", Entries, false);
        }

        public void WriteBook1()
        {
            Log.Write(LogLevel.Info, "Writing Book1");
            var Entries = from myRow in _Data.AsEnumerable()
                          where (myRow.Field<string>(ColumnName.ContactLast).CompareTo(_strBreak1End) <= 0)
                          orderby myRow.Field<string>(ColumnName.TimeSlotIndex) ascending, myRow.Field<string>(ColumnName.ContactLast)
                          select myRow;

            int page = 1;
            foreach (var e in Entries)
            {
                e[ColumnName.BookNumber] = "1";
                e[ColumnName.PageNumber] = page++;
            }
            WriteCSV("RegistrationBook.csv", Entries, false);
        }

        public void WriteBook2()
        {
            Log.Write(LogLevel.Info, "Writing Book2");
            var Entries = from myRow in _Data.AsEnumerable()
                          where (myRow.Field<string>(ColumnName.ContactLast).CompareTo(_strBreak2End) <= 0 &&
                                 myRow.Field<string>(ColumnName.ContactLast).CompareTo(_strBreak2Begin) >= 0)
                          orderby myRow.Field<string>(ColumnName.TimeSlotIndex) ascending, myRow.Field<string>(ColumnName.ContactLast)
                          select myRow;

            int page = 1;
            foreach (var e in Entries)
            {
                e[ColumnName.BookNumber] = "2";
                e[ColumnName.PageNumber] = page++;
            }
            WriteCSV("RegistrationBook.csv", Entries, true);
        }

        public void WriteBook3()
        {
            Log.Write(LogLevel.Info, "Writing Book3");
            var Entries = from myRow in _Data.AsEnumerable()
                          where (myRow.Field<string>(ColumnName.ContactLast).CompareTo(_strBreak3End) <= 0 &&
                                 myRow.Field<string>(ColumnName.ContactLast).CompareTo(_strBreak3Begin) >= 0)
                          orderby myRow.Field<string>(ColumnName.TimeSlotIndex) ascending, myRow.Field<string>(ColumnName.ContactLast)
                          select myRow;

            int page = 1;
            foreach (var e in Entries)
            {
                e[ColumnName.BookNumber] = "3";
                e[ColumnName.PageNumber] = page++;
            }
            WriteCSV("RegistrationBook.csv", Entries, true);
        }

        public void WriteBook4()
        {
            Log.Write(LogLevel.Info, "Writing Book4");
            var Entries = from myRow in _Data.AsEnumerable()
                          where (myRow.Field<string>(ColumnName.ContactLast).CompareTo(_strBreak4Begin) >= 0)
                          orderby myRow.Field<string>(ColumnName.TimeSlotIndex) ascending, myRow.Field<string>(ColumnName.ContactLast)
                          select myRow;

            int page = 1;
            foreach (var e in Entries)
            {
                e[ColumnName.BookNumber] = "4";
                e[ColumnName.PageNumber] = page++;
            }
            WriteCSV("RegistrationBook.csv", Entries, true);
        }

        public void WriteTFTEmails()
        {
            Log.Write(LogLevel.Info, "Writing Email Invitation List");
            var Entries = from myRow in _Data.AsEnumerable()
                          where (!string.IsNullOrEmpty(myRow.Field<string>(ColumnName.Email)))
                          orderby myRow.Field<string>(ColumnName.ContactLast)
                          select myRow;

            WriteCSV("EmailList.csv", Entries, false);
        }

        public void WriteProjectSmileInvitations()
        {
            Log.Write(LogLevel.Info, "Writing Printed Invitations List");
            var PSEntries = from myRow in _Data.AsEnumerable()
                            where (string.IsNullOrEmpty(myRow.Field<string>(ColumnName.Email)))
                            orderby myRow.Field<string>(ColumnName.ContactLast)
                            select myRow;

            WriteCSV("PrintedInvitationsList.csv", PSEntries, false);
        }

        private string Pretty(string txt)
        {
            string[] words = txt.ToLower().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string ret = "";
            bool bFirst = true;
            foreach (var w in words)
            {
                if (!bFirst)
                    ret += ' ';
                else
                    bFirst = false;
                ret += char.ToUpper(w[0]) + w.Substring(1);
            }

            return ret;
        }

        private void MergeCopyFromRow(DataRow row, DataRow rawRow, string name)
        {
            row[name] = rawRow[name];
        }

        public void Merge()
        {
            Log.Write(LogLevel.Info, "Merging");
            string[] timeSlot = { "8:00AM-10:00AM", "10:00AM-Noon", "1:00PM-3:00PM", "3:00PM-5:00PM" };

            // Merge PS data
            int index = 0;
            foreach (DataRow rawRow in _rawPSData.Rows)
            {
                DataRow row = _Data.NewRow();
                foreach (DataColumn col in _Data.Columns)
                {
                    if (_rawPSData.Columns.Contains(col.ColumnName))
                        row[col.ColumnName] = rawRow[col.ColumnName];
                }
                GenerateRegistrationData(row);
                CorrectPhone(row);

                // This will randomly distribute Project Smile families
                if (row[ColumnName.Penalty] is System.DBNull)
                {
                    int timeSlotIndex = (index++) % 4;
                    row[ColumnName.TimeSlotIndex] = timeSlotIndex;
                    row[ColumnName.TimeSlot] = timeSlot[timeSlotIndex];
                }
                else
                {
                    row[ColumnName.TimeSlotIndex] = 3;
                    row[ColumnName.TimeSlot] = timeSlot[3];
                }

                _Data.Rows.Add(row);
            }

            // Merge TFT data
            index = 0;
            int totalRows = _rawTFTData.Rows.Count + 1;
            foreach (DataRow rawRow in _rawTFTData.Rows)
            {
                DataRow row = _Data.NewRow();
                foreach (DataColumn col in _Data.Columns)
                {
                    if (_rawTFTData.Columns.Contains(col.ColumnName))
                        row[col.ColumnName] = rawRow[col.ColumnName];

                }
                GenerateRegistrationData(row);
                CorrectPhone(row);

                // This will put TFT families in the order of entry
                // This will randomly distribute Project Smile families
                if (row[ColumnName.Penalty] is System.DBNull)
                {
                    int timeSlotIndex = (int)((double)(index++) / (double)(totalRows / 4.0f));
                    row[ColumnName.TimeSlotIndex] = timeSlotIndex;
                    row[ColumnName.TimeSlot] = timeSlot[timeSlotIndex];
                }
                else
                {
                    row[ColumnName.TimeSlotIndex] = 3;
                    row[ColumnName.TimeSlot] = timeSlot[3];
                }

                _Data.Rows.Add(row);
            }

            // Merge Special data
            index = 0;
            foreach (DataRow rawRow in _rawSpecialData.Rows)
            {
                DataRow row = _Data.NewRow();
                foreach (DataColumn col in _Data.Columns)
                {
                    if (_rawSpecialData.Columns.Contains(col.ColumnName))
                        row[col.ColumnName] = rawRow[col.ColumnName];

                }
                GenerateRegistrationData(row);
                CorrectPhone(row);

                // Random placement in first 3 timeslots to account for penalized accounts
                int timeSlotIndex = (index++) % 3;
                row[ColumnName.TimeSlotIndex] = timeSlotIndex;
                row[ColumnName.TimeSlot] = timeSlot[timeSlotIndex];

                _Data.Rows.Add(row);
            }

            Log.Write(LogLevel.Info, "Merging - Done");
        }

        private void CorrectPhone(DataRow row)
        {
            string phoneString(string input)
            {
                var result = Regex.Replace(input, @"\D", "");
                if (result.Length == 10)
                    return result.Substring(0, 3) + '-' +
                        result.Substring(3, 3) + '-' +
                        result.Substring(6, 4);
                return input;
            }

            row[ColumnName.Phone] = phoneString((string)row[ColumnName.Phone]);
            if (row.Table.Columns.Contains(ColumnName.Phone2) &&
                !(row[ColumnName.Phone2] is System.DBNull))
                row[ColumnName.Phone2] = phoneString((string)row[ColumnName.Phone2]);
        }

        private void GenerateRegistrationData(DataRow r)
        {
            r[ColumnName.ProperName] = r[ColumnName.ContactLast] + ", " + r[ColumnName.ContactFirst];
            r[ColumnName.Kids] = string.Empty;
            ParseChildren(r);
        }

        private void ParseChildren(DataRow r)
        {
            for (int i = 1; i <= 10; i++)
            {
                int age = -1;
                string colNameAge = "ChildAge" + i;
                string colNameFirstName = "ChildFirst" + i;
                if (!(r[colNameAge] is System.DBNull) &&
                    !(r[colNameFirstName] is System.DBNull) &&
                    int.TryParse((string)r[colNameAge], out age) &&
                    age < 18)
                {
                    string firstName = Pretty(((string)r[colNameFirstName]).Trim());
                    string prefix = "Blue     Blue     Green   .        ";
                    if (age == 17)
                        prefix = ".        .        .       GftCrd   ";

                    r["R" + (i - 1).ToString()] = prefix + age + ": " + firstName;
                    bool bFirst = string.IsNullOrEmpty(((string)r[ColumnName.Kids]));
                    r[ColumnName.Kids] = r[ColumnName.Kids] + (bFirst ? "" : ", ") + firstName;
                }
            }
        }

        #region DBRead_Write
        //public void ReadDB()
        //{
        //    if (false == File.Exists("Regsitrations.db"))
        //        return;

        //    using (StreamReader sr = new StreamReader("Regsitrations.db"))
        //    {
        //        String line = sr.ReadLine();
        //        if (line == null)
        //            return;

        //        for (int i = 0; i < ColumnName.Array.Length; i++)
        //        {
        //            Data.Columns.Add(ColumnName.Array[i]);
        //        }

        //        String[] header = Regex.Split(line, @"\|");

        //        bool go = true;
        //        do
        //        {
        //            line = sr.ReadLine();
        //            string[] tagInfo = Regex.Split(line, @"\|");

        //            if (tagInfo.Length != header.Length)
        //                go = false;
        //            else
        //            {
        //                DataRow row = Data.NewRow();
        //                for (int ii = 0; ii < header.Length; ii++)
        //                {
        //                    row[header[ii]] = tagInfo[ii];
        //                }
        //                if ((string)row[ColumnName.CorrectedAddress] == "ERROR")
        //                    row[ColumnName.CorrectedAddress] = "Pending";

        //                // Add the total column
        //                int total = 0;
        //                total += Int32.Parse((string)row["Boys 0-3"]);
        //                total += Int32.Parse((string)row["Boys 4-6"]);
        //                total += Int32.Parse((string)row["Boys 7-10"]);
        //                total += Int32.Parse((string)row["Boys 11-14"]);
        //                total += Int32.Parse((string)row["Boys 15-17"]);
        //                total += Int32.Parse((string)row["Girls 0-3"]);
        //                total += Int32.Parse((string)row["Girls 4-6"]);
        //                total += Int32.Parse((string)row["Girls 7-10"]);
        //                total += Int32.Parse((string)row["Girls 11-14"]);
        //                total += Int32.Parse((string)row["Girls 15-17"]);
        //                row[ColumnName.Total] = total.ToString();

        //                Data.Rows.Add(row);
        //            }

        //            if (sr.EndOfStream)
        //                go = false;
        //        } while (go);
        //    }
        //    Data.PrimaryKey = new DataColumn[1] { Data.Columns[ColumnName.ControlNumber] };
        //}

        //public void WriteDB()
        //{
        //    StreamWriter swOut = new StreamWriter("Regsitrations.db");

        //    // write headings
        //    bool bFirst = true;
        //    foreach(DataColumn s in Data.Columns)
        //    {
        //        if (bFirst)
        //            bFirst = false;
        //        else
        //            swOut.Write("|");
        //        swOut.Write(s.ColumnName);
        //    }
        //    swOut.WriteLine();

        //    foreach (DataRow row in Data.Rows)
        //    {
        //        bFirst = true;
        //        foreach (DataColumn s in Data.Columns)
        //        {
        //            if (bFirst)
        //                bFirst = false;
        //            else
        //                swOut.Write("|");

        //            swOut.Write(row[s] == null ? "" : row[s].ToString());
        //        }
        //        swOut.WriteLine();
        //    }
        //    swOut.Close();
        //}
        #endregion
        public void WriteCSV(string filename, OrderedEnumerableRowCollection<DataRow> data, bool bAppend)
        {
            StreamWriter swOut = new StreamWriter(_OutputDir + '/' + filename, bAppend);

            if (!bAppend)
            {
                // write headings
                bool bFirst = true;
                foreach (DataColumn s in _Data.Columns)
                {
                    if (bFirst)
                    {
                        bFirst = false;
                        swOut.Write("\"");
                    }
                    else
                        swOut.Write("\",\"");
                    swOut.Write(s.ColumnName);
                }
                swOut.WriteLine("\"");
            }

            foreach (var row in data)
            {
                bool bFirst = true;
                foreach (DataColumn s in _Data.Columns)
                {
                    if (bFirst)
                    {
                        bFirst = false;
                        swOut.Write("\"");
                    }
                    else
                        swOut.Write("\",\"");

                    swOut.Write(row[s] == null ? "" : row[s].ToString());
                }
                swOut.WriteLine("\"");
            }
            swOut.Close();
        }

        public string _strBreak1End = string.Empty;
        public string _strBreak2Begin = string.Empty;
        public string _strBreak2End = string.Empty;
        public string _strBreak3Begin = string.Empty;
        public string _strBreak3End = string.Empty;
        public string _strBreak4Begin = string.Empty;
        public int _totalBoys_0_2 = 0,
                _totalBoys_3_6 = 0,
                _totalBoys_7_11 = 0,
                _totalBoys_12_16 = 0,
                _totalBoys_17 = 0,
                _totalGirls_0_2 = 0,
                _totalGirls_3_6 = 0,
                _totalGirls_7_11 = 0,
                _totalGirls_12_16 = 0,
                _totalGirls_17 = 0,
                _totalKids = 0,
                _totalKidsPS = 0,
                _totalKidsTFT = 0,
                _totalRegPS = 0,
                _totalRegTFT = 0;

        public void ComputeBreakOut()
        {
            int CountKids(DataRow e, Gender gender, int ageBegin, int ageEnd)
            {
                int count = 0;
                for (int i = 1; i <= 10; i++)
                {
                    string colNameAge = "ChildAge" + i;
                    string colNameGender = "ChildGender" + i;
                    if (!(e[colNameAge] is System.DBNull) &&
                        !(e[colNameGender] is System.DBNull) &&
                        ((string)e[colNameGender]).Length > 0)
                    {
                        char firstChar = ((string)e[colNameGender])[0];
                        if ((gender == Gender.Male && firstChar == 'M') ||
                            (gender == Gender.Female && firstChar == 'F'))
                        {
                            int age = -1;
                            if (int.TryParse((string)e[colNameAge], out age))
                            {
                                if (age >= ageBegin && age <= ageEnd)
                                    count++;
                            }
                        }
                    }
                }
                return count;
            }

            Log.Write(LogLevel.Info, "Computing BreakOut");
            var entries = from myRow in _Data.AsEnumerable()
                          orderby myRow.Field<string>(ColumnName.ContactLast)
                          select myRow;
            // Compute Totals
            _totalKids = 0;
            _totalBoys_0_2 = 0;
            _totalBoys_3_6 = 0;
            _totalBoys_7_11 = 0;
            _totalBoys_12_16 = 0;
            _totalBoys_17 = 0;
            _totalGirls_0_2 = 0;
            _totalGirls_3_6 = 0;
            _totalGirls_7_11 = 0;
            _totalGirls_12_16 = 0;
            _totalGirls_17 = 0;
            _totalKids = 0;
            _totalKidsPS = 0;
            _totalKidsTFT = 0;
            _totalRegPS = 0;
            _totalRegTFT = 0;
            foreach (var e in entries)
            {
                if (e.Field<string>(ColumnName.Organization) == "Project Smile")
                {
                    _totalKidsPS += Int32.Parse(e.Field<string>(ColumnName.Total));
                    _totalRegPS++;
                }
                else
                {
                    _totalKidsTFT += Int32.Parse(e.Field<string>(ColumnName.Total));
                    _totalRegTFT++;
                }

                _totalBoys_0_2 += CountKids(e, Gender.Male, 0, 2);
                _totalBoys_3_6 += CountKids(e, Gender.Male, 3, 6);
                _totalBoys_7_11 += CountKids(e, Gender.Male, 7, 11);
                _totalBoys_12_16 += CountKids(e, Gender.Male, 12, 16);
                _totalBoys_17 += CountKids(e, Gender.Male, 17, 17);
                _totalGirls_0_2 += CountKids(e, Gender.Female, 0, 2);
                _totalGirls_3_6 += CountKids(e, Gender.Female, 3, 6);
                _totalGirls_7_11 += CountKids(e, Gender.Female, 7, 11);
                _totalGirls_12_16 += CountKids(e, Gender.Female, 12, 16);
                _totalGirls_17 += CountKids(e, Gender.Female, 17, 17);
            }
            _totalKids += _totalBoys_0_2 + _totalBoys_3_6 + _totalBoys_7_11 + _totalBoys_12_16 + _totalBoys_17 +
                _totalGirls_0_2 + _totalGirls_3_6 + _totalGirls_7_11 + _totalGirls_12_16 + _totalGirls_17;

            StreamWriter sw = new StreamWriter(_OutputDir + "/RegistrationStatistics.csv");

            sw.WriteLine("Total_Registrations," + _Data.Rows.Count);
            sw.WriteLine("Total Accepted," + entries.Count());
            sw.WriteLine("Total Accepted PS," + _totalRegPS);
            sw.WriteLine("Total Accepted TFT," + _totalRegTFT);
            _currentOperation.Value =
                "Gender,'0-2,'3-6,'7-11,'12-16,'17" + Environment.NewLine +
                "Boys," + _totalBoys_0_2 + ',' + _totalBoys_3_6 + ',' + _totalBoys_7_11 + ',' + _totalBoys_12_16 + ',' + _totalBoys_17 + Environment.NewLine +
                "Girls," + _totalGirls_0_2 + ',' + _totalGirls_3_6 + ',' + _totalGirls_7_11 + ',' + _totalGirls_12_16 + ',' + _totalGirls_17 + Environment.NewLine +
                "Total," + (_totalGirls_0_2 + _totalBoys_0_2) + ',' + (_totalGirls_3_6 + _totalBoys_3_6) + ',' + (_totalGirls_7_11 + _totalBoys_7_11) + ',' +
                            (_totalGirls_12_16 + _totalBoys_12_16) + ',' + (_totalGirls_17 + _totalBoys_17);

            sw.WriteLine(_currentOperation.Value);
            sw.WriteLine("Total Kids," + _totalKids);
            sw.WriteLine("Total Kids PS," + _totalKidsPS);
            sw.WriteLine("Total Kids TFT," + _totalKidsTFT);

            sw.Close();

            int n = 0;
            int state = 0;
            _strBreak2Begin = _strBreak1End = _strBreak2End = _strBreak3Begin = _strBreak3End = _strBreak4Begin = string.Empty;
            foreach (var e in entries)
            {
                n += Int32.Parse(e.Field<string>(ColumnName.Total));
                string strLastName = (string)e[ColumnName.ContactLast];
                switch (state)
                {
                    case 0:
                        if (state == 0 && n > _totalKids / 4)
                        {
                            _strBreak1End = strLastName;
                            state++;
                        }
                        break;
                    case 1:
                        if (string.IsNullOrEmpty(_strBreak2Begin) &&
                            _strBreak1End.CompareTo(strLastName) != 0)
                        {
                            _strBreak2Begin = strLastName;
                            state++;
                        }
                        break;
                    case 2:
                        if (n > 2 * _totalKids / 4)
                        {
                            _strBreak2End = strLastName;
                            state++;
                        }
                        break;
                    case 3:
                        if (string.IsNullOrEmpty(_strBreak3Begin) &&
                            _strBreak2End.CompareTo(strLastName) != 0)
                        {
                            _strBreak3Begin = strLastName;
                            state++;
                        }
                        break;
                    case 4:
                        if (n > 3 * _totalKids / 4)
                        {
                            _strBreak3End = strLastName;
                            state++;
                        }
                        break;
                    case 5:
                        if (string.IsNullOrEmpty(_strBreak4Begin) &&
                            _strBreak3End.CompareTo(strLastName) != 0)
                        {
                            _strBreak4Begin = strLastName;
                            state++;
                        }
                        break;
                    case 6:
                        StringBuilder sb = new StringBuilder();
                        sb.Append("     " + "A - " + _strBreak1End + Environment.NewLine);
                        sb.Append("     " + _strBreak2Begin + " - " + _strBreak2End + Environment.NewLine);
                        sb.Append("     " + _strBreak3Begin + " - " + _strBreak3End + Environment.NewLine);
                        sb.Append("     " + _strBreak4Begin + " - Zzz");

                        Directory.CreateDirectory(_OutputDir);
                        StreamWriter swOut = new StreamWriter(_OutputDir + "/Breakout.txt");
                        swOut.WriteLine(sb.ToString());
                        swOut.Close();
                        Log.Write(LogLevel.Info, sb.ToString());
                        System.Threading.Thread.Sleep(250);
                        Log.Write(LogLevel.Info, "Computing BreakOut - Done");
                        System.Threading.Thread.Sleep(250);
                        return;
                }
            }
        }

        public void CheckDuplicates()
        {
            FindSimilarChildrenLists();
            CheckForDuplicatePhones();
        }



        private Dictionary<string, Dictionary<string, string>> dictControlNumberProperties = new Dictionary<string, Dictionary<string, string>>();
        private void CheckForDuplicatePhones()
        {
            Log.Write(LogLevel.Info, "Checking for Similar Entries");
            System.Threading.Thread.Sleep(100);
            string FirstNumberInString(string s)
            {
                string numString = string.Empty;
                foreach (char c in s)
                {
                    if (char.IsNumber(c))
                        numString += c;
                    else
                        break;
                }
                return numString;
            }
            string StringAfterNumber(string s)
            {
                int index = 0;
                foreach (char c in s)
                {
                    if (char.IsNumber(c))
                        index++;
                    else break;
                }
                return s.Substring(index).Trim();
            }

            int currentProgress = -1;
            int progress = 0;
            Log.Write(LogLevel.Info, "Checking for Similar Children");

            // construct the data to search
            foreach (DataRow r1 in this._Data.Rows)
            {
                progress++;
                if (currentProgress != (int)((double)progress / (double)_Data.Rows.Count * 10f))
                {
                    currentProgress = (int)((double)progress / (double)_Data.Rows.Count * 10f);
                    Log.Write(LogLevel.Info, currentProgress.ToString());
                    System.Threading.Thread.Sleep(100);
                }
                string cn = (string)r1[ColumnName.ControlNumber];
                if (!dictControlNumberProperties.ContainsKey(cn))
                    dictControlNumberProperties[cn] = new Dictionary<string, string>();
                var td = dictControlNumberProperties[cn];
                string address;
                if (!(r1[ColumnName.Address2] is System.DBNull))
                    address = (string)r1[ColumnName.Address] + (string)r1[ColumnName.Address2];
                else
                    address = (string)r1[ColumnName.Address];
                td["streetNum"] = FirstNumberInString(address);
                td["streetName"] = Pretty(StringAfterNumber(address));
                td["phone"] = (string)r1[ColumnName.Phone];
                td["email"] = (string)r1[ColumnName.Email];
                td["nameComposite"] = (((string)r1[ColumnName.ContactFirst]) + "---").Substring(0, 3).ToUpper() +
                                      (((string)r1[ColumnName.ContactLast]) + "---").Substring(0, 3).ToUpper();
                td["SimilarEntryIssues"] = string.Empty;
                td["SimilarEntryList"] = string.Empty;
            }

            progress = 0;
            currentProgress = -1;
            foreach (var tuple1 in dictControlNumberProperties)
            {
                progress++;
                if (currentProgress != (int)((double)progress / (double)dictControlNumberToChildrenIndex.Count * 10f))
                {
                    currentProgress = (int)((double)progress / (double)dictControlNumberToChildrenIndex.Count * 10f);
                    Log.Write(LogLevel.Info, currentProgress.ToString());
                }
                int t1 = int.Parse(tuple1.Key);
                foreach (var tuple2 in dictControlNumberProperties)
                {
                    int t2 = int.Parse(tuple2.Key);
                    if (t1 >= t2) continue;

                    if (tuple1.Value["streetNum"].Length > 0 && tuple1.Value["streetNum"] == tuple2.Value["streetNum"] &&
                        tuple1.Value["streetName"].Length > 5 && tuple1.Value["streetName"] == tuple2.Value["streetName"] ||
                        tuple1.Value["phone"].Length > 5 && tuple1.Value["phone"] == tuple2.Value["phone"] ||
                        tuple1.Value["email"].Length > 5 && tuple1.Value["email"] == tuple2.Value["email"])
                    {
                        HashSet<int> merged = new HashSet<int>();
                        foreach (var val in dictControlNumberToChildrenIndex[tuple1.Key]) merged.Add(val);
                        foreach (var val in dictControlNumberToChildrenIndex[tuple2.Key]) merged.Add(val);

                        if (merged.Count < dictControlNumberToChildrenIndex[tuple1.Key].Length + dictControlNumberToChildrenIndex[tuple2.Key].Length)
                        {
                            dictControlNumberProperties[tuple1.Key]["SimilarEntryIssues"] += tuple2.Key +
                                "(" + dictControlNumberProperties[tuple2.Key]["index"] + "),";
                            dictControlNumberProperties[tuple1.Key]["SimilarEntryList"] += tuple2.Key + ",";
                        }
                    }
                }
            }

            {
                StreamWriter sw = new StreamWriter(_OutputDir + "/SimilarEntryIssues.dat");
                foreach (var tuple in dictControlNumberProperties)
                {
                    if (!string.IsNullOrEmpty(tuple.Value["SimilarEntryIssues"]))
                        sw.WriteLine(tuple.Key + "(" + tuple.Value["index"] + ") : " + tuple.Value["SimilarEntryIssues"]);
                }
                sw.Close();
            }

            {
                StreamWriter sw = new StreamWriter(_OutputDir + "/SimilarEntryIssuesReport.txt");
                string strHeading = "------------------------------------------------------------------------------";
                foreach (var tuple in dictControlNumberProperties)
                {
                    if (!string.IsNullOrEmpty(tuple.Value["SimilarEntryIssues"]))
                    {
                        sw.WriteLine(strHeading);
                        WriteEntry(sw, _Data, tuple.Key);
                        sw.WriteLine("                                                                is SIMILAR TO");
                        WriteList(sw, _Data, tuple.Value["SimilarEntryList"]);
                    }
                }
                sw.Close();
            }
        }

        private void WriteList(StreamWriter sw, DataTable data, string v)
        {
            string[] list = SplitCSV(v);
            foreach (var key in list)
            {
                if (!string.IsNullOrEmpty(key))
                {
                    sw.WriteLine("   - - - - - - -");
                    WriteEntry(sw, data, key);
                }
            }
        }

        private void WriteEntry(StreamWriter sw, DataTable data, string key)
        {
            var row = data.Rows.Find(key);
            if (row == null)
                throw new NoNullAllowedException("Found null row...");
            WriteFamily(sw, row);
        }

        private void WriteFamily(StreamWriter sw, DataRow row)
        {
            string cn = S(row[ColumnName.ControlNumber]);
            int nCN = int.Parse(cn);
            if (nCN < 50000)
                sw.WriteLine("Project Smile Family - " + S(row[ColumnName.Volunteer]));
            else if (nCN > 999990)
                sw.WriteLine("Special Family");
            else
                sw.WriteLine("TFT Entry");
            sw.WriteLine(S(row[ColumnName.ControlNumber]) + "   " + S(row[ColumnName.ContactLast]) + ", " + S(row[ColumnName.ContactFirst]));
            if (!(row[ColumnName.Address2] is System.DBNull))
            {
                sw.WriteLine(S(row[ColumnName.Address]) + " " + S(row[ColumnName.Address2]) +
                    ", " + S(row[ColumnName.City]) + "  " + S(row[ColumnName.Zip]));
            }
            else
                sw.WriteLine(S(row[ColumnName.Address]) + ", " + S(row[ColumnName.City]) + "  " + S(row[ColumnName.Zip]));
            sw.WriteLine(S(row[ColumnName.Phone]) + " - " + S(row[ColumnName.Email]));
            for (int i = 1; i <= 10; i++)
            {
                if (!(row["ChildFirst" + i] is System.DBNull))
                {
                    if (S(row["ChildFirst" + i]).Length > 0)
                        sw.WriteLine(S(row["ChildFirst" + i]) + " " + S(row["ChildLast" + i]) + " " + S(row["ChildAge" + i]) + " (" +
                            S(row["ChildGender" + i]) + ")");
                }
            }

        }

        private string S(object v)
        {
            if (v is System.DBNull) return string.Empty;
            return (string)v;
        }



        #region SimilarChildrenListFinder
        private Dictionary<string, int[]> dictControlNumberToChildrenIndex = new Dictionary<string, int[]>();

        HashSet<int> _tmpHash = new HashSet<int>();
        public void FindSimilarChildrenLists()
        {
            bool AreNumbersUnique(int[] nameIndexes)
            {
                _tmpHash.Clear();
                foreach (var n in nameIndexes)
                    _tmpHash.Add(n);
                return (_tmpHash.Count == nameIndexes.Length);
            }

            int currentProgress = -1;
            int progress = 0;
            Log.Write(LogLevel.Info, "Checking for Similar Children");
            List<string> CNsWithChildrenNamingIssues = new List<string>();
            foreach (DataRow r in this._Data.Rows)
            {
                progress++;
                if (currentProgress != (int)((double)progress / (double)_Data.Rows.Count * 10f))
                {
                    currentProgress = (int)((double)progress / (double)_Data.Rows.Count * 10f);
                    Log.Write(LogLevel.Info, $"Checking for Similar Children - step {currentProgress} of 10.");
                }
                (int[] nameIndexes, int numChildren) = GetChildren(r);
                string cn = (string)r[ColumnName.ControlNumber];
                if (!AreNumbersUnique(nameIndexes))
                    CNsWithChildrenNamingIssues.Add(cn);
                dictControlNumberToChildrenIndex[cn] = nameIndexes;
                dictControlNumberProperties[cn] = new Dictionary<string, string>();
                dictControlNumberProperties[cn]["ChildrenDupsIssue"] = string.Empty;
                dictControlNumberProperties[cn]["ChildrenDupsList"] = string.Empty;
                dictControlNumberProperties[cn]["index"] = (string)r[ColumnName.BookNumber] + ',' + (string)r[ColumnName.PageNumber];
            }
            Log.Write(LogLevel.Info, " - Clearing Dups List ");
            foreach (var tuple1 in dictControlNumberToChildrenIndex)
            {
                if (!dictControlNumberProperties.ContainsKey(tuple1.Key))
                    dictControlNumberProperties[tuple1.Key] = new Dictionary<string, string>();
            }
            File.WriteAllLines(_OutputDir + "/ChildNamingIssues.dat", CNsWithChildrenNamingIssues);

            progress = 0;
            currentProgress = -1;
            // for each submission (given by ControlNumber) check to see how many other submissions are similar.
            foreach (var tuple1 in dictControlNumberToChildrenIndex)
            {
                int t1 = int.Parse(tuple1.Key);
                progress++;
                if (currentProgress != (int)((double)progress / (double)dictControlNumberToChildrenIndex.Count * 10f))
                {
                    currentProgress = (int)((double)progress / (double)dictControlNumberToChildrenIndex.Count * 10f);
                    Log.Write(LogLevel.Info, currentProgress.ToString());
                }
                // are there at least 2 children
                if (tuple1.Value.Length > 2)
                {
                    // examine all other submissions
                    foreach (var tuple2 in dictControlNumberToChildrenIndex)
                    {
                        int t2 = int.Parse(tuple2.Key);
                        if (tuple2.Value.Length < 3 ||
                            t1 == t2) continue;

                        // merge the two submissions into one list called 'merged'
                        HashSet<int> merged = new HashSet<int>();
                        foreach (var val in tuple1.Value) merged.Add(val);
                        foreach (var val in tuple2.Value) merged.Add(val);

                        // if the length of merged is less than 70% of the combined length of both together, then there are a significant number of duplicates
                        if (merged.Count < .7 * (tuple1.Value.Length + tuple2.Value.Length))
                        {
                            dictControlNumberProperties[tuple1.Key]["ChildrenDupsIssue"] += tuple2.Key + '(' + dictControlNumberProperties[tuple2.Key]["index"] + "),";
                            dictControlNumberProperties[tuple1.Key]["ChildrenDupsList"] += tuple2.Key + ",";
                        }
                    }
                }
            }

            StreamWriter sw2 = new StreamWriter(_OutputDir + "/ChildrenDupsIssues.dat");
            foreach (var tuple in dictControlNumberProperties)
            {
                if (!string.IsNullOrEmpty(tuple.Value["ChildrenDupsIssue"]))
                    sw2.WriteLine(tuple.Key + '(' + tuple.Value["index"] + ") : " + tuple.Value["ChildrenDupsIssue"]);
            }
            sw2.Close();

            {
                StreamWriter sw = new StreamWriter(_OutputDir + "/ChildrenDupsIssuesReport.txt");
                string strHeading = "------------------------------------------------------------------------------";
                foreach (var tuple in dictControlNumberProperties)
                {
                    if (!string.IsNullOrEmpty(tuple.Value["ChildrenDupsIssue"]))
                    {
                        sw.WriteLine(strHeading);
                        WriteEntry(sw, _Data, tuple.Key);
                        sw.WriteLine("                                                  may have duplicate children");
                        WriteList(sw, _Data, tuple.Value["ChildrenDupsList"]);
                    }
                }
                sw.Close();
            }
        }

        private Dictionary<string, int> dictNameToIndex = new Dictionary<string, int>();
        private Dictionary<int, string> dictIndexToName = new Dictionary<int, string>();
        private int countChildren = 0;

        private (int[], int) GetChildren(DataRow r)
        {
            char[] sep = { ',' };
            List<string> childrenNames = new List<string>();
            string[] colNames = { ColumnName.ChildFirst1,
                                    ColumnName.ChildFirst2,
                                    ColumnName.ChildFirst3,
                                    ColumnName.ChildFirst4,
                                    ColumnName.ChildFirst5,
                                    ColumnName.ChildFirst6,
                                    ColumnName.ChildFirst7,
                                    ColumnName.ChildFirst8,
                                    ColumnName.ChildFirst9,
                                    ColumnName.ChildFirst10,};
            foreach (var colName in colNames)
            {
                if (!(r[colName] is System.DBNull))
                {
                    string name = ((string)r[colName]);
                    if (!string.IsNullOrEmpty(name))
                        childrenNames.Add(name);
                }
            }

            int[] childrenIndexes = new int[childrenNames.Count];
            int index = 0;

            foreach (var name in childrenNames)
            {
                if (dictNameToIndex.TryGetValue(name, out int nameIndex))
                {
                    childrenIndexes[index] = nameIndex;
                    index++;
                }
                else
                {
                    int newIndex = countChildren++;
                    dictNameToIndex.Add(name, newIndex);
                    dictIndexToName.Add(newIndex, name);

                    childrenIndexes[index] = newIndex;
                    index++;
                }
            }

            return (childrenIndexes, (int)childrenNames.Count);
        }
        #endregion
    }
}