﻿using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using CDC.Utilities;
using CDC.Logging;
using CDC.Configuration;

namespace addresses
{
    public class Database
    {
        public DataTable Data { get; } = new DataTable();
        public ILogger _log = new Logger();
        private DataTable rawTFTData;
        private DataTable rawPSData;
        private DataTable rawSpecialData;
        ObservableString _currentOperation = ObservableString.Get("CurrentOperation");
        private string _OutputDir, _InputDir;

        public Database()
        {
            AppConfiguration.AppConfig.TryGetSetting("Data.InputDir", out Setting indir);
            _InputDir = indir.Value;

            if (!Directory.Exists(_InputDir))
                throw new Exception("Input directory not found.");

            AppConfiguration.AppConfig.TryGetSetting("Data.OutputDir", out Setting outdir);
            _OutputDir = outdir.Value;
            Directory.CreateDirectory(_OutputDir);

            Clear();
        }

        public void Initialize()
        {
            try
            {
                _currentOperation.Value = "Initializing Database";

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

                CheckDuplicates();

                ComputeNoShows();
            }
            catch(Exception e)
            {
                _log.Fatal(e, "Exception while creating database.");
            }
        }

        private void ComputeNoShows()
        {
            int lineNumber = 0;
            int totalKidsNoShow = 0;
            int totalKidsNoShowPS = 0;
            int totalKidsNoShowTFT = 0;
            int totalRegistrations = Data.Rows.Count;
            int totalRegNoShowPS = 0;
            int totalRegNoShowTFT = 0;

            string filename = _InputDir + "/NoShows.csv";

            if (!File.Exists(filename)) return;

            using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs, Encoding.Default))
            {
                do
                {
                    String cn = sr.ReadLine();
                    lineNumber++;
                    if (cn.Length > 0)
                    {
                        DataRow row = Data.Rows.Find(cn);
                        if (row == null)
                        {
                            _log.Warning("cn = '" + cn + "' - Does not exist.");
                            continue;
                        }
                        row[ColumnName.pDup] = "N";
                        int kids = int.Parse((string)row[ColumnName.Total]);
                        totalKidsNoShow += kids;
                        if (String.Compare("Project Smile",(string)row[ColumnName.Organization]) == 0)
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

            StreamWriter sw = new StreamWriter(_OutputDir + "/NoShowsStatistics.csv");
            sw.WriteLine("Total Num Kids," + _totalKids);
            sw.WriteLine("Total NoShows Kids," + totalKidsNoShow);
            sw.WriteLine("Project Smile NoShows Kids," + totalKidsNoShowPS + "," + 100f * totalKidsNoShowPS / _totalKidsPS + "%\n");
            sw.WriteLine("TFT NoShows Kids = " + totalKidsNoShowTFT + "," + 100f * totalKidsNoShowTFT / _totalKidsTFT + "%\n");

            sw.WriteLine("Total Num Registrations," + totalRegistrations);
            sw.WriteLine("Total NoShow PS," + totalRegNoShowPS + "," + 100f * totalRegNoShowPS / _totalRegPS + "%\n");
            sw.WriteLine("Total NoShow TFT," + totalRegNoShowTFT + "," + 100f * totalRegNoShowTFT / _totalRegTFT + "%\n");
            sw.Close();

            WriteProjectNoShows();
        }

        private void Clear()
        {
            _log.Info("Initializaing Database");
            _currentOperation.Value = "Clearing Database";
            Data.Clear();
            foreach (var colName in ColumnName.Array)
                Data.Columns.Add(colName);

            Data.PrimaryKey = new DataColumn[1] { Data.Columns[ColumnName.ControlNumber] };
        }

        public static string[] SplitCSV(string input)
        {
            Regex csvSplit = new Regex("(?:^|,)(\"(?:[^\"]+|\"\")*\"|[^,]*)", RegexOptions.Compiled);
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
            _currentOperation.Value = "Reading Special DB";
            rawSpecialData = new DataTable();
            try
            {
                int lineNumber = 0;
                string filename = _InputDir + "/Special.csv";
                if( !File.Exists(filename))
                {
                    _log.Warning("Special DB: 'Special.csv' not found in '" + _InputDir + "' - Skipping");
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
                        rawSpecialData.Columns.Add(columnName);
                    }
                    rawSpecialData.Columns.Add(ColumnName.Organization);
                    rawSpecialData.PrimaryKey = new DataColumn[1] { rawSpecialData.Columns[ColumnName.ControlNumber] };

                    bool go = true;
                    do
                    {
                        line = sr.ReadLine();
                        string[] tagInfo = SplitCSV(line);

                        if (tagInfo.Length != header.Length)
                        {
                            _log.Warning("Line " + lineNumber + " - Length wrong: " + line);
                            go = false;
                        }
                        else
                        {
                            DataRow row = rawSpecialData.NewRow();
                            for (int ii = 0; ii < header.Length; ii++)
                            {
                                string tag = tagInfo[ii].Trim(new char[] { '"', '\\', ',' });
                                row[header[ii]] = Pretty(tag);
                            }
                            row[ColumnName.State] = "Texas";
                            row[ColumnName.Organization] = string.Empty;

                            rawSpecialData.Rows.Add(row);
                        }
                        if (sr.EndOfStream)
                            go = false;
                    } while (go);
                }
            }
            catch (Exception e)
            {
                _log.Warning(e, "Reading Special DB - Failed");
                _currentOperation.Value = "Reading Special DB - Failed";
                return false;
            }
            _currentOperation.Value = "Reading Special DB - Done";
            return true;
        }

        private bool ReadTFTRawData()
        {
            _currentOperation.Value = "Reading TFT DB";
            rawTFTData = new DataTable();
            try
            {
                int lineNumber = 0;
                string filename = _InputDir + "/dashboard-family-requests-form-export.csv";
                if (!File.Exists(filename))
                {
                    _log.Warning("TFT DB: 'dashboard-family-requests-form-export.csv' not found in '" + _InputDir + "' - Skipping");
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
                        string columnName = header[i] = header[i].Trim(new char[] { '"', '\\', ',' });
                        rawTFTData.Columns.Add(columnName);
                    }
                    rawTFTData.Columns.Add(ColumnName.Organization);
                    rawTFTData.Columns.Add(ColumnName.TimeSlot);
                    rawTFTData.Columns.Add(ColumnName.TimeSlotIndex);
                    rawTFTData.PrimaryKey = new DataColumn[1] { rawTFTData.Columns[ColumnName.ControlNumber] };

                    bool go = true;
                    do
                    {
                        line = sr.ReadLine();
                        string[] tagInfo = SplitCSV(line);

                        if (tagInfo.Length != header.Length)
                        {
                            _log.Warning("Line " + lineNumber + " - Length wrong: " + line);
                            go = false;
                        }
                        else
                        {
                            DataRow row = rawTFTData.NewRow();
                            for (int ii = 0; ii < header.Length; ii++)
                            {
                                string tag = tagInfo[ii].Trim(new char[] { '"', '\\', ',' });
                                row[header[ii]] = Pretty(tag);
                            }
                            row[ColumnName.State] = "Texas";
                            row[ColumnName.TimeSlot] = string.Empty;
                            row[ColumnName.TimeSlotIndex] = string.Empty;
                            row[ColumnName.Organization] = "ToysForTots";

                            rawTFTData.Rows.Add(row);
                        }
                        if (sr.EndOfStream)
                            go = false;
                    } while (go);
                }
            }
            catch (Exception e)
            {
                _log.Warning(e, "Reading TFT DB - Failed");
                _currentOperation.Value = "Reading TFT DB - Failed";
                return false;
            }
            _currentOperation.Value = "Reading TFT DB - Done";
            return true;            
        }

        private bool ReadPSRawData()
        {
            _currentOperation.Value = "Reading ProjectSmile DB";
            rawPSData = new DataTable();
            try
            {
                string filename = _InputDir + "/PS2018.csv";
                if (!File.Exists(filename))
                {
                    _log.Warning("Project Smile DB: 'PS2018.csv' not found in '" + _InputDir + "' - Skipping");
                    return false;
                }

                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.Default))
                {
                    String line = sr.ReadLine();
                    String[] header = SplitCSV(line);
                    for (int i = 0; i < header.Length; i++)
                    {
                        string columnName = header[i] = header[i].Trim(new char[] { '"', '\\', ',' });
                        rawPSData.Columns.Add(columnName);
                    }
                    
                    rawPSData.Columns.Add(ColumnName.Organization);
                    rawTFTData.Columns.Add(ColumnName.TimeSlot);
                    rawTFTData.Columns.Add(ColumnName.TimeSlotIndex);
                    rawPSData.PrimaryKey = new DataColumn[1] { rawPSData.Columns[ColumnName.ControlNumber] };

                    bool go = true;
                    do
                    {
                        line = sr.ReadLine();
                        string[] tagInfo = SplitCSV(line);

                        if (tagInfo.Length != header.Length)
                            go = false;
                        else
                        {
                            DataRow row = rawPSData.NewRow();
                            for (int ii = 0; ii < header.Length; ii++)
                            {
                                string tag = tagInfo[ii].Trim(new char[] { '"', '\\', ',' });
                                row[header[ii]] = Pretty(tag);
                            }
                            row[ColumnName.State] = "Texas";
                            row[ColumnName.Organization] = "Project Smile";

                            var existingRow = rawPSData.Rows.Find(row[ColumnName.ControlNumber]);

                            // for each child, check to see if child is too old
                            int totalKids = 0;
                            for (int i = 1; i <= 10; i++)
                            {
                                string agekey = "CHILDAGE" + i;
                                if (int.TryParse((string)row[agekey], out int age))
                                {
                                    totalKids++;
                                    if (age > 17)
                                    {
                                        _log.Warning("Project Smile - " + row[ColumnName.ContactFirst] + " " + row[ColumnName.ContactLast]
                                             + " child '" + (string)row["CHILDNAME" + i] + "' is above the age limit.");
                                        continue;
                                    }
                                }
                            }

                            if (existingRow != null)
                                row = existingRow;

                            row[ColumnName.Total] = totalKids;
                        }
                        if (sr.EndOfStream)
                            go = false;
                    } while (go);
                }
            }
            catch (Exception e)
            {
                _log.Warning(e, "Reading ProjectSmile DB - Failed");
                _currentOperation.Value = "Reading ProjectSmile DB - Failed";
                return false;
            }
            _currentOperation.Value = "Reading ProjectSmile DB - Done";
            return true;
        }

        public void WriteSpecial()
        {
            _currentOperation.Value = "Writing Special";
            var Entries = from myRow in Data.AsEnumerable()
                          where (myRow.Field<string>(ColumnName.ControlNumber).Contains("S"))
                          orderby myRow.Field<string>(ColumnName.TimeSlot) descending, myRow.Field<string>(ColumnName.ContactLast)
                          select myRow;

            int page = 1;
            foreach (var e in Entries)
            {
                e[ColumnName.BookNumber] = "Special";
                e[ColumnName.PageNumber] = page++;
            }
            WriteCSV("SpecialOut.csv", Entries, false);
        }

        public void WriteBook1()
        {
            _currentOperation.Value = "Writing Book1";
            var Entries = from myRow in Data.AsEnumerable()
                                where (myRow.Field<string>(ColumnName.ContactLast).CompareTo(_strBreak1End) <= 0 &&
                                   !myRow.Field<string>(ColumnName.ControlNumber).Contains("S"))
                                orderby myRow.Field<string>(ColumnName.TimeSlotIndex) ascending, myRow.Field<string>(ColumnName.ContactLast)
                                select myRow;

            int page=1;
            foreach(var e in Entries)
            {
                e[ColumnName.BookNumber] = "1";
                e[ColumnName.PageNumber] = page++;
            }
            WriteCSV("RegistrationBook.csv", Entries, false);
        }

        public void WriteBook2()
        {
            _currentOperation.Value = "Writing Book2";
            var Entries = from myRow in Data.AsEnumerable()
                          where (myRow.Field<string>(ColumnName.ContactLast).CompareTo(_strBreak2End) <= 0 &&
                                 myRow.Field<string>(ColumnName.ContactLast).CompareTo(_strBreak2Begin) >= 0 &&
                                   !myRow.Field<string>(ColumnName.ControlNumber).Contains("S"))
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
            _currentOperation.Value = "Writing Book3";
            var Entries = from myRow in Data.AsEnumerable()
                          where (myRow.Field<string>(ColumnName.ContactLast).CompareTo(_strBreak3End) <= 0 &&
                                 myRow.Field<string>(ColumnName.ContactLast).CompareTo(_strBreak3Begin) >= 0 &&
                                   !myRow.Field<string>(ColumnName.ControlNumber).Contains("S"))
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
            _currentOperation.Value = "Writing Book4";
            var Entries = from myRow in Data.AsEnumerable()
                          where (myRow.Field<string>(ColumnName.ContactLast).CompareTo(_strBreak4Begin) >= 0 &&
                                   !myRow.Field<string>(ColumnName.ControlNumber).Contains("S"))
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
            _currentOperation.Value = "Writing TFTEmails";
            var Entries = from myRow in Data.AsEnumerable()
                            where (myRow.Field<string>(ColumnName.Organization) != "Project Smile" &&
                                   !myRow.Field<string>(ColumnName.ControlNumber).Contains("S"))
                            orderby myRow.Field<string>(ColumnName.ContactLast)
                            select myRow;

            WriteCSV("TFTEmails.csv", Entries, false);
        }

        public void WriteProjectSmileInvitations()
        {
            _currentOperation.Value = "Writing Project Smile Invitations";
            var PSEntries = from myRow in Data.AsEnumerable()
                                 where (myRow.Field<string>(ColumnName.Organization) == "Project Smile" &&
                                   !myRow.Field<string>(ColumnName.ControlNumber).Contains("S"))
                                 orderby myRow.Field<string>(ColumnName.ContactLast)
                                 select myRow;

            WriteCSV("PSInvitations.csv", PSEntries, false);
        }

        public void WriteProjectNoShows()
        {
            _currentOperation.Value = "Writing Project Smile No Shows";
            var PSEntries = from myRow in Data.AsEnumerable()
                            where (myRow.Field<string>(ColumnName.Organization) == "Project Smile" &&
                              !myRow.Field<string>(ColumnName.ControlNumber).Contains("S") &&
                              myRow.Field<string>(ColumnName.pDup) == "N")
                            orderby myRow.Field<string>(ColumnName.ContactLast)
                            select myRow;

            WriteCSV("PSNoShows.csv", PSEntries, false);
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
            _currentOperation.Value = "Merging";
            string[] timeSlot = { "8:00AM-10:00AM", "10:00AM-Noon", "1:00PM-3:00PM", "3:00PM-5:00PM" };

            // Merge PS data
            int index = 0;
            foreach (DataRow rawRow in rawPSData.Rows)
            {
                DataRow row = Data.NewRow();
                foreach (DataColumn col in Data.Columns)
                {
                    if (rawPSData.Columns.Contains(col.ColumnName) )
                        row[col.ColumnName] = rawRow[col.ColumnName];
                }
                GenerateRegistrationData(row);
                CorrectPhone(row);

                // This will randomly distribute Project Smile families
                int timeSlotIndex = (index++) % 4; 
                row[ColumnName.TimeSlotIndex] = timeSlotIndex;
                row[ColumnName.TimeSlot] = timeSlot[timeSlotIndex];

                Data.Rows.Add(row);
            }

            // Merge TFT data
            index = 0;
            int totalRows = rawTFTData.Rows.Count+1;
            foreach (DataRow rawRow in rawTFTData.Rows)
            {
                DataRow row = Data.NewRow();
                foreach (DataColumn col in Data.Columns)
                {
                    if (rawTFTData.Columns.Contains(col.ColumnName))
                        row[col.ColumnName] = rawRow[col.ColumnName];
                    
                }
                GenerateRegistrationData(row);
                CorrectPhone(row);

                // This will put TFT families in the order of entry
                int timeSlotIndex = (int)((double)(index++) / (double)(totalRows / 4.0f)); 
                row[ColumnName.TimeSlotIndex] = timeSlotIndex;
                row[ColumnName.TimeSlot] = timeSlot[timeSlotIndex]; 

                Data.Rows.Add(row);
            }

            // Merge Special data
            index = 0;
            foreach (DataRow rawRow in rawSpecialData.Rows)
            {
                DataRow row = Data.NewRow();
                foreach (DataColumn col in Data.Columns)
                {
                    if (rawSpecialData.Columns.Contains(col.ColumnName))
                        row[col.ColumnName] = rawRow[col.ColumnName];

                }
                GenerateRegistrationData(row);
                CorrectPhone(row);

                row[ColumnName.TimeSlotIndex] = 3;
                row[ColumnName.TimeSlot] = timeSlot[3]; // All special handling go into final timeslot

                Data.Rows.Add(row);
            }

            _currentOperation.Value = "Merging - Done";
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
            row[ColumnName.Phone2] = phoneString((string)row[ColumnName.Phone2]);
        }

        private void GenerateRegistrationData(DataRow r)
        {
            r[ColumnName.ProperName] = r[ColumnName.ContactLast] + ", " + r[ColumnName.ContactFirst];
            r[ColumnName.Kids] = string.Empty;
        }

        private int ParseChildren(DataRow r, int count, string colName_NumChildren, string colName_NameChildren, string strAgeRangePretty )
        {
            int num = int.Parse((string)r[colName_NumChildren]);
            if (num == 0)
                return count;
            string[] names;
            string tmpNames = Pretty( (string)r[colName_NameChildren] );
            string strNames = string.Empty;
            for(int i=0; i<tmpNames.Length; i++)
            {
                if (tmpNames[i] >= '0' && tmpNames[i] <= '9' || tmpNames[i] == '-')
                    continue;
                strNames += tmpNames[i];
            }
            if (num == 1)
            {
                names = new string[1];
                names[0] = strNames;
            }
            else
            { 
                char[] sep = new char[] { ';', ',', ':', '-' };
                names = strNames.Split(sep, StringSplitOptions.RemoveEmptyEntries);
            }

            if (names.Length != num)
            {
                _log.Warning("Error while parsing children in record: " + r[ColumnName.ControlNumber]);
                return count;
            }

            int index = 0;
            string prefix = "Blue     Blue     Green   .        ";
            if( strAgeRangePretty.Contains("17") )
                prefix =    ".        .        .       GftCrd   ";
            foreach (var n in names)
            {
                string strControlNumer = (string)r[ColumnName.ControlNumber];
                if (count + index < 10)
                {
                    r["R" + (count + index).ToString()] = prefix + strAgeRangePretty + ": " + Pretty(n);
                    bool bFirst = string.IsNullOrEmpty(((string)r[ColumnName.Kids]));
                    r[ColumnName.Kids] = r[ColumnName.Kids] + (bFirst ? "" : ", ") + Pretty(n.Trim());
                }
                index++;
            }

            count += num;

            return count;
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
                foreach (DataColumn s in Data.Columns)
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
                foreach (DataColumn s in Data.Columns)
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
                return 0;
            }

            _currentOperation.Value = "Computing BreakOut";
            var entries = from myRow in Data.AsEnumerable()
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

            sw.WriteLine("Total_Registrations," + Data.Rows.Count);
            sw.WriteLine("Total Accepted," + entries.Count());
            sw.WriteLine("Total Accepted PS," + _totalRegPS);
            sw.WriteLine("Total Accepted TFT," + _totalRegTFT);
            _currentOperation.Value =
                "Gender,0-2,3_6,7_11,12_16,17" + Environment.NewLine +
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
                        _currentOperation.Value = sb.ToString();
                        System.Threading.Thread.Sleep(250);
                        _currentOperation.Value = "Computing BreakOut - Done";
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
            _currentOperation.Value = "Checking for Similar Entries";
            System.Threading.Thread.Sleep(100);
            string FirstNumberInString(string s)
            {
                string numString = string.Empty;
                foreach(char c in s)
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
                foreach(char c in s)
                {
                    if (char.IsNumber(c))
                        index++;
                    else break;
                }
                return s.Substring(index).Trim();
            }

            int currentProgress = -1;
            int progress = 0;
            _currentOperation.Value = "Checking for Similar Children";
            foreach (DataRow r1 in this.Data.Rows)
            {
                progress++;
                if (currentProgress != (int)((double)progress / (double)Data.Rows.Count * 10f))
                {
                    currentProgress = (int)((double)progress / (double)Data.Rows.Count * 10f);
                    _currentOperation.Value = currentProgress.ToString();
                    System.Threading.Thread.Sleep(100);
                }
                string cn = (string)r1[ColumnName.ControlNumber];
                if (!dictControlNumberProperties.ContainsKey(cn))
                    dictControlNumberProperties[cn] = new Dictionary<string, string>();
                var td = dictControlNumberProperties[cn];
                td["streetNum"] = FirstNumberInString((string)r1[ColumnName.Address]);
                td["streetName"] = Pretty(StringAfterNumber((string)r1[ColumnName.Address]));
                td["phone"] = (string)r1[ColumnName.Phone];
                td["email"] = (string)r1[ColumnName.Email];
                td["nameComposite"] = (((string)r1[ColumnName.ContactFirst]) + "---").Substring(0, 3).ToUpper() +
                                      (((string)r1[ColumnName.ContactLast]) + "---").Substring(0, 3).ToUpper();
                td["SimilarEntryIssues"] = string.Empty;
            }

            progress = 0;
            currentProgress = -1;
            foreach (var tuple1 in dictControlNumberProperties)
            {
                progress++;
                if (currentProgress != (int)((double)progress / (double)dictControlNumberToChildrenIndex.Count * 10f))
                {
                    currentProgress = (int)((double)progress / (double)dictControlNumberToChildrenIndex.Count * 10f);
                    _currentOperation.Value = currentProgress.ToString();
                }
                foreach (var tuple2 in dictControlNumberProperties)
                {
                    if (tuple1.Key == tuple2.Key) continue;

                    if( tuple1.Value["streetNum"].Length > 0 && tuple1.Value["streetNum"] == tuple2.Value["streetNum"] &&
                        tuple1.Value["streetName"].Length > 5 && tuple1.Value["streetName"] == tuple2.Value["streetName"] ||
                        tuple1.Value["phone"].Length > 5 && tuple1.Value["phone"] == tuple2.Value["phone"] ||
                        tuple1.Value["email"].Length > 5 && tuple1.Value["email"] == tuple2.Value["email"] )
                    {
                        HashSet<int> merged = new HashSet<int>();
                        foreach (var val in dictControlNumberToChildrenIndex[tuple1.Key]) merged.Add(val);
                        foreach (var val in dictControlNumberToChildrenIndex[tuple2.Key]) merged.Add(val);

                        if (merged.Count < dictControlNumberToChildrenIndex[tuple1.Key].Length + dictControlNumberToChildrenIndex[tuple2.Key].Length)
                            dictControlNumberProperties[tuple1.Key]["SimilarEntryIssues"] += tuple2.Key +
                                "(" + dictControlNumberProperties[tuple2.Key]["index"] + "),";
                    }
                }
            }

            StreamWriter sw = new StreamWriter(_OutputDir + "/SimilarEntryIssues.dat");
            foreach (var tuple in dictControlNumberProperties)
            {
                if (!string.IsNullOrEmpty(tuple.Value["SimilarEntryIssues"]))
                    sw.WriteLine(tuple.Key + "(" + tuple.Value["index"] + ") : " + tuple.Value["SimilarEntryIssues"]);
            }
            sw.Close();
        }

        #region SimilarChildrenListFinder
        private Dictionary<string, int[]> dictControlNumberToChildrenIndex = new Dictionary<string, int[]>();

        public void FindSimilarChildrenLists()
        {
            int currentProgress = -1;
            int progress = 0;
            _currentOperation.Value = "Checking for Similar Children";
            foreach (DataRow r in this.Data.Rows)
            {
                progress++;
                if (currentProgress != (int)((double)progress / (double)Data.Rows.Count * 10f))
                {
                    currentProgress = (int)((double)progress / (double)Data.Rows.Count * 10f);
                    _currentOperation.Value = currentProgress.ToString();
                    System.Threading.Thread.Sleep(100);
                }
                (int[] nameIndexes, int numChildren) = GetChildren(r);
                string cn = (string)r[ColumnName.ControlNumber];
                dictControlNumberToChildrenIndex[cn] = nameIndexes;
                dictControlNumberProperties[cn] = new Dictionary<string, string>();
                dictControlNumberProperties[cn]["ChildrenDupsIssue"] = string.Empty;
                dictControlNumberProperties[cn]["index"] = (string)r[ColumnName.BookNumber] + ',' + (string)r[ColumnName.PageNumber];
            }
            _currentOperation.Value = " - Clearing Dups List ";
            foreach (var tuple1 in dictControlNumberToChildrenIndex)
            {
                if(!dictControlNumberProperties.ContainsKey(tuple1.Key) )
                    dictControlNumberProperties[tuple1.Key] = new Dictionary<string, string>();
                
            }

            progress = 0;
            currentProgress = -1;
            foreach (var tuple1 in dictControlNumberToChildrenIndex)
            {
                progress++;
                if (currentProgress != (int)((double)progress / (double)dictControlNumberToChildrenIndex.Count * 10f))
                {
                    currentProgress = (int)((double)progress / (double)dictControlNumberToChildrenIndex.Count * 10f);
                    _currentOperation.Value = currentProgress.ToString();
                }
                if (tuple1.Value.Length > 2)
                {
                    foreach (var tuple2 in dictControlNumberToChildrenIndex)
                    {
                        if (tuple2.Value.Length < 3 ||
                            tuple2.Key == tuple1.Key) continue;

                        HashSet<int> merged = new HashSet<int>();
                        foreach (var val in tuple1.Value) merged.Add(val);
                        foreach (var val in tuple2.Value) merged.Add(val);

                        if (merged.Count < .7 * (tuple1.Value.Length + tuple2.Value.Length))
                            dictControlNumberProperties[tuple1.Key]["ChildrenDupsIssue"] += tuple2.Key + '(' + dictControlNumberProperties[tuple2.Key]["index"] + "),";
                    }
                }
            }

            StreamWriter sw = new StreamWriter(_OutputDir + "/ChildrenDupsIssues.dat");
            foreach (var tuple in dictControlNumberProperties)
            {
                if (!string.IsNullOrEmpty(tuple.Value["ChildrenDupsIssue"]))
                    sw.WriteLine(tuple.Key + '(' + tuple.Value["index"] +") : " + tuple.Value["ChildrenDupsIssue"]);
            }
            sw.Close();
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
                string name = ((string)r[colName]).Trim();
                if(name.Length > 0)
                    childrenNames.Add(name);
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