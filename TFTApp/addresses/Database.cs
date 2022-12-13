using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Agora.Utilities;
using Microsoft.Extensions.Configuration;
using static Agora.SDK;

namespace addresses
{
    public class Database
    {
        public DataTable _Data { get; } = new DataTable();

        private DataTable _rawTFTData;
        private DataTable _rawSpecialData;
        private DataTable _rawNoShows;
        private DataTable _rawBanned;

        ObservableString _currentOperation = ObservableString.Get("CurrentOperation");
        private string OutputDir, InputDir;
        private string TFTFilename, SpecialFilename, BannedFilename, NoShowRawFilename;
        private List<string> NoShowFilenames;

        Dictionary<string, int> _NoShowPhones, _NoShowEmails;
        HashSet<string> _BannedPhones, _BannedEmails;
        List<int> _NoShowRawList;

        public Database()
        {
            InputDir = Config["Data:InputDir"];
            if (!Directory.Exists(InputDir))
                throw new Exception("Input directory not found.");

            OutputDir = Config["Data:OutputDir"];
            Directory.CreateDirectory(OutputDir);

            TFTFilename = Config["Data:TFTFilename"];

            //AppConfiguration.AppConfig.TryGetSetting("Data.PSFilename", out Setting psname);
            //_PS_Filename = psname.Val;

            SpecialFilename = Config["Data:SpecialFilename"];
            NoShowFilenames = Config.GetSection("Data:NoShowFilename").Get<List<string>>();
            BannedFilename = Config["Data:BannedFilename"];
            NoShowRawFilename = Config["Data:NoShowRaw"];

            Clear();
        }

        public void Initialize()
        {
            try
            {
                "Initializing Database".LogInfo();

                ReadNoShows();

                ReadBanned();

                ReadTFTRawData();
                ReadSpecialRawData();

                Merge();

                ComputeBreakOut();

                WriteBook();
                WriteSpecial();
                WriteTFTEmails();

                ProcessNoShowsRaw();

                CheckDuplicates();

                CheckChildrenFirstNamesNotEmpty();

                ComputeNoShows();
            }
            catch (Exception ex)
            {
               "Exception while creating database.".LogException(ex, Agora.Logging.LogLevel.Fatal);
                return;
            }
        }

        private void CheckChildrenFirstNamesNotEmpty()
        {
            "Checking for Empty First Names".LogInfo();

            StreamWriter sw = new StreamWriter(OutputDir + "/FirstNameErrors.dat");

            // construct the data to search
            foreach (DataRow r in this._Data.Rows)
            {
                for (int i = 1; i < 11; i++)
                {
                    string colFirstName = "ChildFirst" + i;
                    string colLastName = "ChildLast" + i;
                    string colAge = "ChildAge" + i;
                    string colGender = "ChildGender" + i;

                    if (!(r[colAge] is System.DBNull) && !string.IsNullOrEmpty((string)r[colAge]) && 
                        r[colFirstName] is System.DBNull)
                    {
                        sw.Write((string)r[ColumnName.Organization] + ',' + (string)r[ColumnName.ControlNumber]);
                        i = 11; // force it out                        
                    }
                }
            }
            sw.Close();
        }

        bool ProcessNoShowsRaw()
        {
            "Processing NoShowsRaw".LogInfo();

            _NoShowRawList = new List<int>();

            try
            {
                int lineNumber = 0;
                string filename = InputDir + "/" + NoShowRawFilename;
                if (!File.Exists(filename))
                {
                    $"NoShowsRaw: '{NoShowRawFilename}' not found in '{InputDir}' - Skipping".LogInfo();
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
            catch (Exception ex)
            {
                "Exception encountered while processing NoShowsRaw".LogException(ex, Agora.Logging.LogLevel.Fatal);
                return false;
            }

            if (_NoShowRawList.Count == 0)
                return false;

            List<string> output = new() {"CONTACT LAST,CONTACT FIRST,PHONE,EMAIL"};

            foreach (var id in _NoShowRawList)
            {
                DataRow row = _Data.Rows.Find(id);
                if (row != null)
                {
                    output.Add($"{row[ColumnName.ContactLast]},{row[ColumnName.ContactFirst]},{row[ColumnName.Phone]},{row[ColumnName.Email]}");
                }
            }

            File.WriteAllLines(InputDir + "/ProcessedNoShows.csv", output);

            "Processing NoShowRaw DB - Done".LogInfo();

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
            "Reading Banned".LogInfo();
            var raw = _rawBanned = new DataTable();
            try
            {
                int lineNumber = 0;
                string filename = InputDir + "/" + BannedFilename;
                if (!File.Exists(filename))
                {
                    $"Banned DB: '{BannedFilename}' not found in '{InputDir}' - Skipping".LogWarn();
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
                            $"Line {lineNumber} - Length wrong: {line}".LogWarn();
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
            catch (Exception ex)
            {
                "Reading Banned DB - Failed".LogException(ex);
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

            "Reading Banned DB - Done".LogInfo();
            return true;
        }

        private bool ReadNoShows()
        {
             "Reading NoShows".LogInfo();
            var raw = _rawNoShows = new DataTable();
            int index = 0;
            foreach (var fn in NoShowFilenames)
            {
                try
                {
                    int lineNumber = 0;
                    string filename = InputDir + "/" + fn;
                    if (!File.Exists(filename))
                    {
                         $"NoShows DB: '{fn}' not found in '{InputDir}' - Skipping".LogWarn();
                        continue;
                    }
                    else
                         $"Reading NoShowDB - {filename}".LogInfo();

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
                            lineNumber++;

                            string[] tagInfo = SplitCSV(line);

                            if (tagInfo.Length != header.Length)
                            {
                                 $"Line {lineNumber} - Length wrong: {line}".LogWarn();
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
                         $"--- Num Records == {lineNumber}".LogInfo();
                    }
                }
                catch (Exception e)
                {
                    "Reading NoShow DB - Failed".LogException(e);
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

             "Reading NoShow DB - Done".LogInfo();
            return true;
        }

        private void ComputeNoShows()
        {
            int lineNumber = 0;
            int totalKidsNoShow = 0;
            int totalRegistrations = _Data.Rows.Count;
            int totalRegNoShow = 0;

            string filename = InputDir + "/" + NoShowRawFilename;
            string outFile = OutputDir + "/NoShows-Processed.csv";

            if (!File.Exists(filename))
            {
                 $"Missing <{filename}>.  After the event, this file is needed to produce event statistics.".LogWarn();
                return;
            }

            // Compute Totals
            int totalNSBoys_0_2 = 0,
            totalNSBoys_3_6 = 0,
            totalNSBoys_7_11 = 0,
            totalNSBoys_12_16 = 0,
            totalNSBoys_17 = 0,
            totalNSGirls_0_2 = 0,
            totalNSGirls_3_6 = 0,
            totalNSGirls_7_11 = 0,
            totalNSGirls_12_16 = 0,
            totalNSGirls_17 = 0;            

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
                             $"cn = '{cn}' - Does not exist.".LogWarn();
                            continue;
                        }
                        row[ColumnName.pDup] = "N";
                        of.WriteLine((string)row[ColumnName.ContactLast] + ',' +
                            (string)row[ColumnName.ContactFirst] + ',' +
                            (string)row[ColumnName.Phone] + ',' +
                            (string)row[ColumnName.Email]);
                        int kids = int.Parse((string)row[ColumnName.Total]);
                        totalKidsNoShow += kids;
                        totalRegNoShow++;

                        CountKids(row, Gender.Male, 0, 2, out int countTFT);
                        totalNSBoys_0_2 += countTFT;
                        CountKids(row, Gender.Male, 3, 6, out countTFT);
                        totalNSBoys_3_6 += countTFT;
                        CountKids(row, Gender.Male, 7, 10, out countTFT);
                        totalNSBoys_7_11 += countTFT;
                        CountKids(row, Gender.Male, 11, 16, out countTFT);
                        totalNSBoys_12_16 += countTFT;
                        CountKids(row, Gender.Male, 17, 18, out countTFT);
                        totalNSBoys_17 += countTFT;
                        CountKids(row, Gender.Female, 0, 2, out countTFT);
                        totalNSGirls_0_2 += countTFT;
                        CountKids(row, Gender.Female, 3, 6, out countTFT);
                        totalNSGirls_3_6 += countTFT;
                        CountKids(row, Gender.Female, 7, 10, out countTFT);
                        totalNSGirls_7_11 += countTFT;
                        CountKids(row, Gender.Female, 11, 16, out countTFT);
                        totalNSGirls_12_16 += countTFT;
                        CountKids(row, Gender.Female, 17, 18, out countTFT);
                        totalNSGirls_17 += countTFT;
                    }
                } while (!sr.EndOfStream);
            }

            StreamWriter sw = new StreamWriter(OutputDir + "/EventStatistics.csv");
            sw.WriteLine("Total Kids Registered," + _totalKids);
            sw.WriteLine("NoShows Kids," + totalKidsNoShow + " (" + (100f * totalKidsNoShow / _totalKids).ToString("00.0") + "%)");
            sw.WriteLine("Total Kids Served," + (_totalKids - totalKidsNoShow)) ;
            sw.WriteLine();
            sw.WriteLine("Total Num Regs," + totalRegistrations);
            sw.WriteLine("Total NoShow Regs," + totalRegNoShow + " (" + (100f * totalRegNoShow / totalRegistrations).ToString("00.0") + "%)");
            sw.WriteLine("Total Families Served," + (totalRegistrations - totalRegNoShow));
            sw.WriteLine();
            sw.WriteLine("Boys:");
            sw.WriteLine($"\t0-2: {totalNSBoys_0_2} / {_totalBoys_0_2} = {totalNSBoys_0_2 / (double)_totalBoys_0_2 * 100f:F2}");
            sw.WriteLine($"\t3-6: {totalNSBoys_3_6} / {_totalBoys_3_6} = {totalNSBoys_3_6 / (double) _totalBoys_3_6 * 100f:F2}");
            sw.WriteLine($"\t7-11: {totalNSBoys_7_11} / {_totalBoys_7_11} = {totalNSBoys_7_11 / (double)_totalBoys_7_11 * 100f:F2}");
            sw.WriteLine($"\t12-16: {totalNSBoys_12_16} / {_totalBoys_12_16} = {totalNSBoys_12_16 / (double)_totalBoys_12_16 * 100f:F2}");
            sw.WriteLine($"\t17: {totalNSBoys_17} / {_totalBoys_17} = {totalNSBoys_17 / (double)_totalBoys_17 * 100f:F2}");
            sw.WriteLine("Girls:");
            sw.WriteLine($"\t0-2: {totalNSGirls_0_2} / {_totalGirls_0_2} = {totalNSGirls_0_2 / (double)_totalGirls_0_2 * 100f:F2}");
            sw.WriteLine($"\t3-6: {totalNSGirls_3_6} / {_totalGirls_3_6} = {totalNSGirls_3_6 / (double)_totalGirls_3_6 * 100f:F2}");
            sw.WriteLine($"\t7-11: {totalNSGirls_7_11} / {_totalGirls_7_11} = {totalNSGirls_7_11 / (double)_totalGirls_7_11 * 100f:F2}");
            sw.WriteLine($"\t12-16: {totalNSGirls_12_16} / {_totalGirls_12_16} = {totalNSGirls_12_16 / (double)_totalGirls_12_16 * 100f:F2}");
            sw.WriteLine($"\t17: {totalNSGirls_17} / {_totalGirls_17} = {totalNSGirls_17 / (double)_totalGirls_17 * 100f:F2}");

            sw.Close();
        }

        private void Clear()
        {
             "Initializing Database".LogInfo();
            _Data.Clear();
            foreach (var colName in ColumnName.Array)
                _Data.Columns.Add(colName);

            foreach (var colName in ColumnName.ArrayInts)
                _Data.Columns.Add(new DataColumn(colName, typeof(int)));

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
             "Reading Special DB".LogInfo();
            var raw = _rawSpecialData = new DataTable();
            try
            {
                int lineNumber = 0;
                string filename = InputDir + "/" + SpecialFilename;
                if (!File.Exists(filename))
                {
                     $"Special DB: '{SpecialFilename}' not found in '{InputDir}' - Skipping".LogWarn();
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
                        if (line == null) return true;
                        string[] tagInfo = SplitCSV(line);

                        if (tagInfo.Length != header.Length)
                        {
                             $"Line {lineNumber} - Length wrong: {line}".LogWarn();
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
                "Reading Special DB - Failed".LogException(e);
                return false;
            }
             "Reading Special DB - Done".LogInfo();
            return true;
        }

        private bool ReadTFTRawData()
        {
             "Reading Toys-for-Tots Input".LogInfo();
            _rawTFTData = new DataTable();
            int lineNumber = 0;
            try
            {
                string filename = InputDir + "/" + TFTFilename;
                if (!File.Exists(filename))
                {
                     $"TFT DB: '{TFTFilename}' not found in '{InputDir}' - Skipping".LogWarn();
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

                        if( Agora.Logging.AgoraLogger.GetVerbosity() == Agora.Logging.LogLevel.Trace)
                        {
                            $"Processing Line {lineNumber}".LogTrace();
                        }

                        if (tagInfo.Length != header.Length)
                        {
                             $"Line {lineNumber} - Length wrong: {line}".LogWarn();
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
                            string state = (string)row[ColumnName.Status];
                            if (string.Compare("pending", state, true) == 0 ||
                                string.Compare("verify", state, true) == 0 ||
                                string.Compare("approved", state, true) == 0)
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
                            else
                                $"Skipping {(string)row[ColumnName.RequestID]} - {state}".LogTrace();
                        }
                        if (sr.EndOfStream)
                            go = false;
                    } while (go);
                }
            }
            catch (Exception e)
            {
                "Reading TFT DB - Failed".LogException(e);
                return false;
            }
            $"Reading Toys-for-Tots Input - Done - {lineNumber} successfully parsed.".LogInfo();
            return true;
        }

        public void WriteSpecial()
        {
             "Writing Special".LogInfo();
            var Entries = from myRow in _Data.AsEnumerable()
                          where (myRow.Field<string>(ColumnName.Organization).Contains("Late"))
                          orderby myRow.Field<string>(ColumnName.ControlNumber) ascending, myRow.Field<string>(ColumnName.ContactLast)
                          select myRow;

            int page = 1;
            foreach (var e in Entries)
            {
                e[ColumnName.BookNumber] = 3;
                e[ColumnName.PageNumber] = page++;
            }
            WriteCSV("LateOut.csv", Entries, false);
        }

        public void WriteBook()
        {
             $"Writing Book".LogInfo();
            var Entries = from myRow in _Data.AsEnumerable()
                              //where (myRow.Field<string>(ColumnName.ContactLast).CompareTo(_strBreak1End) <= 0)
                          orderby myRow.Field<string>(ColumnName.TimeSlotIndex) ascending, myRow.Field<string>(ColumnName.ContactLast)
                          select myRow;

            int[] page = { 1, 1 };
            int numEntries = Entries.Count();
            int book = 0;
            foreach (var e in Entries)
            {
                e[ColumnName.BookNumber] = (int)(book + 1);
                e[ColumnName.PageNumber] = (int)(page[book]);
                page[book]++;
                book = (book + 1) % 2;
            }

            var ResortedEntries = from myRow in _Data.AsEnumerable()
                                  orderby myRow.Field<int>(ColumnName.BookNumber) ascending, myRow.Field<int>(ColumnName.PageNumber)
                                  select myRow;

            WriteCSV("RegistrationBook.csv", ResortedEntries, false);
        }

        public void WriteBook2()
        {
             "Writing Book2".LogInfo();
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


        public void WriteTFTEmails()
        {
             "Writing Email Invitation List".LogInfo();
            var Entries = from myRow in _Data.AsEnumerable()
                          where (!string.IsNullOrEmpty(myRow.Field<string>(ColumnName.Email)))
                          orderby myRow.Field<string>(ColumnName.ContactLast)
                          select myRow;

            WriteCSV("EmailList.csv", Entries, false);
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

        string[] timeSlot = { "8:00AM-9:00AM", "9:00AM-10:00AM", "10:00AM-11:00AM", "11:00AM-Noon", "1:00PM-2:00PM", "2:00PM-3:00PM", "3:00PM-4:00PM", "4:00PM-5:00PM" };

        public void Merge()
        {
             "Merging".LogInfo();
            int index = 0;
            int count0 = 0, count7 = 0;

            // Merge TFT data
            index = 0;
            int totalRows = _rawTFTData.Rows.Count + 1;
            count0 = 0;
            count7 = 0;
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
                if (row[ColumnName.Penalty] is System.DBNull)
                {
                    int timeSlotIndex = (int)((double)(index++) / (double)(totalRows / 8.0f));
                    if (timeSlotIndex == 0) count0++;
                    if (timeSlotIndex == 7 && count7 > count0)
                    {
                        timeSlotIndex = 0;
                        count0++;
                        index++;
                    }
                    if (timeSlotIndex == 7) count7++;
                    row[ColumnName.TimeSlotIndex] = timeSlotIndex;
                    row[ColumnName.TimeSlot] = timeSlot[timeSlotIndex];
                }
                else
                {
                    count7++;
                    row[ColumnName.TimeSlotIndex] = 7;
                    row[ColumnName.TimeSlot] = timeSlot[7];
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

             "Merging - Done".LogInfo();
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
                string colGender = "ChildGender" + i;
                if (!(r[colNameAge] is System.DBNull) &&
                    !(r[colNameFirstName] is System.DBNull) &&
                    int.TryParse((string)r[colNameAge], out age) &&
                    age < 18)
                {
                    string firstName = Pretty(((string)r[colNameFirstName]).Trim());
                    StringBuilder prefix = new StringBuilder();

                    string gender = (string)r[colGender];
                    if (gender.Length == 0)
                        prefix.Append("UNKN ");

                    else
                        prefix.Append(((string)r[colGender]).Substring(0,1) == "F" ? "Girl " : "Boy  ");
                    if (age < 3)
                        prefix.Append("0-2  ");
                    else if (age < 7)
                        prefix.Append("3-6  ");
                    else if (age < 12)
                        prefix.Append("7-11 ");
                    else if (age < 17)
                        prefix.Append("12-16");
                    else
                        prefix.Append("17   ");
                    if (age < 10)
                        prefix.Append(" ");
                    r["R" + (i - 1).ToString()] = prefix.ToString() + $" ({age}): " + firstName;
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
            StreamWriter swOut = new StreamWriter(OutputDir + '/' + filename, bAppend);

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
                _totalGirls_17 = 0;
        public int
                _totalKids = 0,
                _totalReg = 0;
        public int [,] _boysByTimeSlot, _girlsByTimeSlot;

        void CountKids(DataRow e, Gender gender, int ageBegin, int ageEnd, out int countTFT)
        {
            countTFT = 0;
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
                            {
                                countTFT++;
                            }
                        }
                    }
                }
            }
        }

        public void ComputeBreakOut()
        {
            

             "Computing BreakOut".LogInfo();
            var entries = from myRow in _Data.AsEnumerable()
                          orderby myRow.Field<string>(ColumnName.ContactLast)
                          select myRow;
            // Compute Totals
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
            _boysByTimeSlot = new int[8, 5];
            _girlsByTimeSlot = new int[8, 5];

            int index = 0;
            foreach (var e in entries)
            {
                index++;
                int totalOnLine = Int32.Parse(e.Field<string>(ColumnName.Total));
                _totalReg++;

                int countTFT;
                int countOnLine = 0;
                int.TryParse((string)e[ColumnName.TimeSlotIndex], out int timeSlot);
                CountKids(e, Gender.Male, 0, 2, out countTFT);
                countOnLine += countTFT;
                _totalBoys_0_2 += countOnLine = countTFT;
                _boysByTimeSlot[timeSlot, 0] += countTFT;
                CountKids(e, Gender.Male, 3, 6, out countTFT);
                countOnLine += countTFT;
                _totalBoys_3_6 += countTFT;
                _boysByTimeSlot[timeSlot, 1] += countTFT;
                CountKids(e, Gender.Male, 7, 10, out countTFT);
                countOnLine += countTFT;
                _totalBoys_7_11 += countTFT;
                _boysByTimeSlot[timeSlot, 2] += countTFT;
                CountKids(e, Gender.Male, 11, 16, out countTFT);
                countOnLine += countTFT;
                _totalBoys_12_16 += countTFT;
                _boysByTimeSlot[timeSlot, 3] += countTFT;
                CountKids(e, Gender.Male, 17, 18, out countTFT);
                countOnLine += countTFT;
                _totalBoys_17 += countTFT;
                _boysByTimeSlot[timeSlot, 4] += countTFT;
                CountKids(e, Gender.Female, 0, 2, out countTFT);
                countOnLine += countTFT;
                _totalGirls_0_2 += countTFT;
                _girlsByTimeSlot[timeSlot, 0] += countTFT;
                CountKids(e, Gender.Female, 3, 6, out countTFT);
                countOnLine += countTFT;
                _totalGirls_3_6 += countTFT;
                _girlsByTimeSlot[timeSlot, 1] += countTFT;
                CountKids(e, Gender.Female, 7, 10, out countTFT);
                countOnLine += countTFT;
                _totalGirls_7_11 += countTFT;
                _girlsByTimeSlot[timeSlot, 2] += countTFT;
                CountKids(e, Gender.Female, 11, 16, out countTFT);
                countOnLine += countTFT;
                _totalGirls_12_16 += countTFT;
                _girlsByTimeSlot[timeSlot, 3] += countTFT;
                CountKids(e, Gender.Female, 17, 18, out countTFT);
                countOnLine += countTFT;
                _girlsByTimeSlot[timeSlot, 4] += countTFT;
                _totalGirls_17 += countTFT;

                if (countOnLine != totalOnLine)
                     $"Line {index} (cn = {(string)e[ColumnName.ControlNumber]}) - total ({totalOnLine}) != count of kids ({countOnLine})".LogWarn();
            }
            _totalKids +=
                _totalBoys_0_2 + 
                _totalBoys_3_6 +
                _totalBoys_7_11 +
                _totalBoys_12_16 +
                _totalBoys_17 +
                _totalGirls_0_2 + 
                _totalGirls_3_6 + 
                _totalGirls_7_11 + 
                _totalGirls_12_16 +
                _totalGirls_17;

            StreamWriter sw = new StreamWriter(OutputDir + "/RegistrationStatistics.csv");

            sw.WriteLine("Total_Registrations," + _Data.Rows.Count);
            sw.WriteLine("Total Accepted," + entries.Count());
            sw.WriteLine();

            _currentOperation.Value =
                "Gender,'0-2,'3-6,'7-11,'12-16,'17" + Environment.NewLine +
                "Boys," + (_totalBoys_0_2) + ',' +
                          (_totalBoys_3_6) + ',' +
                          (_totalBoys_7_11) + ',' +
                          (_totalBoys_12_16) + ',' +
                          (_totalBoys_17) + Environment.NewLine +
                "Girls," + (_totalGirls_0_2) + ',' +
                          (_totalGirls_3_6) + ',' +
                          (_totalGirls_7_11) + ',' +
                          (_totalGirls_12_16) + ',' +
                          (_totalGirls_17) + Environment.NewLine + Environment.NewLine +
                "Total," + (_totalGirls_0_2 + _totalBoys_0_2) + ',' + 
                           (_totalGirls_3_6 + _totalBoys_3_6) + ',' + 
                           (_totalGirls_7_11 + _totalBoys_7_11) + ',' +
                           (_totalGirls_12_16 + _totalBoys_7_11) + ',' + 
                           (_totalGirls_17 + _totalBoys_17);

            sw.WriteLine(_currentOperation.Value);
            sw.WriteLine("Total Kids by Group," + _totalKids);
            sw.WriteLine("Total Kids," + _totalKids);
            sw.WriteLine();

            sw.WriteLine("TimeSlot,Cars,'0-2 (b|g),'3-6 (b|g),'7-11 (b|g),'12-16 (b|g),'17 (b|g),Total (b|g|*)");
            int[] regByTimeSlot = new int[8];
            regByTimeSlot[0] = (from r in _Data.AsEnumerable() where (string)r[ColumnName.TimeSlotIndex] == "0" select r).Count();
            regByTimeSlot[1] = (from r in _Data.AsEnumerable() where (string)r[ColumnName.TimeSlotIndex] == "1" select r).Count();
            regByTimeSlot[2] = (from r in _Data.AsEnumerable() where (string)r[ColumnName.TimeSlotIndex] == "2" select r).Count();
            regByTimeSlot[3] = (from r in _Data.AsEnumerable() where (string)r[ColumnName.TimeSlotIndex] == "3" select r).Count();
            regByTimeSlot[4] = (from r in _Data.AsEnumerable() where (string)r[ColumnName.TimeSlotIndex] == "4" select r).Count();
            regByTimeSlot[5] = (from r in _Data.AsEnumerable() where (string)r[ColumnName.TimeSlotIndex] == "5" select r).Count();
            regByTimeSlot[6] = (from r in _Data.AsEnumerable() where (string)r[ColumnName.TimeSlotIndex] == "6" select r).Count();
            regByTimeSlot[7] = (from r in _Data.AsEnumerable() where (string)r[ColumnName.TimeSlotIndex] == "7" select r).Count();
            for (int i=0;i<8;i++)
            {
                sw.Write(timeSlot[i] + ',');
                sw.Write(regByTimeSlot[i].ToString() + ',');
                int totalBoys = 0, totalGirls = 0;
                for(int j=0;j<5;j++)
                {
                    sw.Write($"({_boysByTimeSlot[i, j]}|{_girlsByTimeSlot[i, j]}),");
                    totalBoys += _boysByTimeSlot[i, j];
                    totalGirls += _girlsByTimeSlot[i, j];
                }
                sw.WriteLine($"({totalBoys}|{totalGirls}|{totalBoys + totalGirls})");
            }

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

                        Directory.CreateDirectory(OutputDir);
                        StreamWriter swOut = new StreamWriter(OutputDir + "/Breakout.txt");
                        swOut.WriteLine(sb.ToString());
                        swOut.Close();
                         sb.ToString().LogInfo();
                        System.Threading.Thread.Sleep(250);
                         "Computing BreakOut - Done".LogInfo();
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
             "Checking for Similar Entries".LogInfo();
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
             "Checking for Similar Children".LogInfo();

            // construct the data to search
            foreach (DataRow r1 in this._Data.Rows)
            {
                progress++;
                if (currentProgress != (int)((double)progress / (double)_Data.Rows.Count * 10f))
                {
                    currentProgress = (int)((double)progress / (double)_Data.Rows.Count * 10f);
                     currentProgress.ToString().LogInfo();
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
                     currentProgress.ToString().LogInfo();
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
                StreamWriter sw = new StreamWriter(OutputDir + "/SimilarEntryIssues.dat");
                foreach (var tuple in dictControlNumberProperties)
                {
                    if (!string.IsNullOrEmpty(tuple.Value["SimilarEntryIssues"]))
                        sw.WriteLine(tuple.Key + "(" + tuple.Value["index"] + ") : " + tuple.Value["SimilarEntryIssues"]);
                }
                sw.Close();
            }

            {
                StreamWriter sw = new StreamWriter(OutputDir + "/SimilarEntryIssuesReport.txt");
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
            if (nCN < 0)
                sw.WriteLine("Special List");
            else
                sw.WriteLine("TFT List");
            
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
             "Checking for Similar Children".LogInfo();
            List<string> CNsWithChildrenNamingIssues = new List<string>();
            foreach (DataRow r in this._Data.Rows)
            {
                progress++;
                if (currentProgress != (int)((double)progress / (double)_Data.Rows.Count * 10f))
                {
                    currentProgress = (int)((double)progress / (double)_Data.Rows.Count * 10f);
                     $"Checking for Similar Children - step {currentProgress} of 10.".LogInfo();
                }

                if (string.Compare((string)r[ColumnName.Status], "approved", true) == 0 ) continue;
    
                (int[] nameIndexes, int numChildren) = GetChildren(r);
                string cn = (string)r[ColumnName.ControlNumber];
                if (!AreNumbersUnique(nameIndexes))
                    CNsWithChildrenNamingIssues.Add(cn);
                dictControlNumberToChildrenIndex[cn] = nameIndexes;
                dictControlNumberProperties[cn] = new Dictionary<string, string>();
                dictControlNumberProperties[cn]["ChildrenDupsIssue"] = string.Empty;
                dictControlNumberProperties[cn]["ChildrenDupsList"] = string.Empty;
                dictControlNumberProperties[cn]["index"] = r[ColumnName.BookNumber].ToString() + ',' + r[ColumnName.PageNumber].ToString();
            }
             " - Clearing Dups List ".LogInfo();
            foreach (var tuple1 in dictControlNumberToChildrenIndex)
            {
                if (!dictControlNumberProperties.ContainsKey(tuple1.Key))
                    dictControlNumberProperties[tuple1.Key] = new Dictionary<string, string>();
            }
            File.WriteAllLines(OutputDir + "/ChildNamingIssues.dat", CNsWithChildrenNamingIssues);

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
                     currentProgress.ToString().LogInfo();
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

            StreamWriter sw2 = new StreamWriter(OutputDir + "/ChildrenDupsIssues.dat");
            foreach (var tuple in dictControlNumberProperties)
            {
                if (!string.IsNullOrEmpty(tuple.Value["ChildrenDupsIssue"]))
                    sw2.WriteLine(tuple.Key + '(' + tuple.Value["index"] + ") : " + tuple.Value["ChildrenDupsIssue"]);
            }
            sw2.Close();

            {
                StreamWriter sw = new StreamWriter(OutputDir + "/ChildrenDupsIssuesReport.txt");
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