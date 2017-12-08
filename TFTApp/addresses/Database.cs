using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Asteria.Utilities;
using Asteria.Logging;

namespace addresses
{
    public class Database
    {
        public DataTable Data { get; } = new DataTable();
        public ILogger _log = new Logger();
        private DataTable rawTFTData;
        private DataTable rawPSData;
        private DataTable rawPSAddData;
        private DataTable rawSpecialData;
        ObservableString _currentOperation = ObservableString.Get("CurrentOperation");

        public Database()
        {
            Clear();
        }

        public void Initialize()
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
                using (StreamReader sr = new StreamReader("Data/Special.csv"))
                {
                    String line = sr.ReadLine();
                    lineNumber++;
                    String[] header = SplitCSV(line);
                    for (int i = 0; i < header.Length; i++)
                    {
                        string columnName = header[i] = header[i].Trim(new char[] { '"', '\\', ',' });
                        rawSpecialData.Columns.Add(columnName);
                    }
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
                            row["State"] = "Texas";

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
                using (StreamReader sr = new StreamReader("Data/TFTExport.csv"))
                {
                    String line = sr.ReadLine();
                    lineNumber++;
                    String[] header = SplitCSV(line);
                    for (int i = 0; i < header.Length; i++)
                    {
                        string columnName = header[i] = header[i].Trim(new char[] { '"', '\\', ',' });
                        rawTFTData.Columns.Add(columnName);
                    }
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
                            row["State"] = "Texas";

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
                using (StreamReader sr = new StreamReader("Data/PS2017.csv"))
                {
                    String line = sr.ReadLine();
                    String[] header = SplitCSV(line);
                    for (int i = 0; i < header.Length; i++)
                    {
                        string columnName = header[i] = header[i].Trim(new char[] { '"', '\\', ',' });
                        rawPSData.Columns.Add(columnName);
                    }
                    rawPSData.Columns.Add(ColumnName.Boys_0_2);
                    rawPSData.Columns.Add(ColumnName.Boys_3_6);
                    rawPSData.Columns.Add(ColumnName.Boys_7_11);
                    rawPSData.Columns.Add(ColumnName.Boys_12_16);
                    rawPSData.Columns.Add(ColumnName.Boys_17);
                    rawPSData.Columns.Add(ColumnName.Boys_0_2_Names);
                    rawPSData.Columns.Add(ColumnName.Boys_3_6_Names);
                    rawPSData.Columns.Add(ColumnName.Boys_7_11_Names);
                    rawPSData.Columns.Add(ColumnName.Boys_12_16_Names);
                    rawPSData.Columns.Add(ColumnName.Boys_17_Names);
                    rawPSData.Columns.Add(ColumnName.Girls_0_2);
                    rawPSData.Columns.Add(ColumnName.Girls_3_6);
                    rawPSData.Columns.Add(ColumnName.Girls_7_11);
                    rawPSData.Columns.Add(ColumnName.Girls_12_16);
                    rawPSData.Columns.Add(ColumnName.Girls_17);
                    rawPSData.Columns.Add(ColumnName.Girls_0_2_Names);
                    rawPSData.Columns.Add(ColumnName.Girls_3_6_Names);
                    rawPSData.Columns.Add(ColumnName.Girls_7_11_Names);
                    rawPSData.Columns.Add(ColumnName.Girls_12_16_Names);
                    rawPSData.Columns.Add(ColumnName.Girls_17_Names);
                    rawPSData.Columns.Add(ColumnName.Total);
                    rawPSData.Columns.Add(ColumnName.R0);
                    rawPSData.Columns.Add(ColumnName.R1);
                    rawPSData.Columns.Add(ColumnName.R2);
                    rawPSData.Columns.Add(ColumnName.R3);
                    rawPSData.Columns.Add(ColumnName.R4);
                    rawPSData.Columns.Add(ColumnName.R5);
                    rawPSData.Columns.Add(ColumnName.R6);
                    rawPSData.Columns.Add(ColumnName.R7);
                    rawPSData.Columns.Add(ColumnName.R8);
                    rawPSData.Columns.Add(ColumnName.R9);
                    rawPSData.Columns.Add(ColumnName.Kids);

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
                            row["State"] = "Texas";

                            var existingRow = rawPSData.Rows.Find(row[ColumnName.ControlNumber]);

                            if (existingRow == null)
                            {
                                row[ColumnName.Organization] = "Project Smile";
                                row[ColumnName.Boys_0_2] = 0;
                                row[ColumnName.Boys_3_6] = 0;
                                row[ColumnName.Boys_7_11] = 0;
                                row[ColumnName.Boys_12_16] = 0;
                                row[ColumnName.Boys_17] = 0;
                                row[ColumnName.Boys_0_2_Names] = string.Empty;
                                row[ColumnName.Boys_3_6_Names] = string.Empty;
                                row[ColumnName.Boys_7_11_Names] = string.Empty;
                                row[ColumnName.Boys_12_16_Names] = string.Empty;
                                row[ColumnName.Boys_17_Names] = string.Empty;
                                row[ColumnName.Girls_0_2] = 0;
                                row[ColumnName.Girls_3_6] = 0;
                                row[ColumnName.Girls_7_11] = 0;
                                row[ColumnName.Girls_12_16] = 0;
                                row[ColumnName.Girls_17] = 0;
                                row[ColumnName.Girls_0_2_Names] = string.Empty;
                                row[ColumnName.Girls_3_6_Names] = string.Empty;
                                row[ColumnName.Girls_7_11_Names] = string.Empty;
                                row[ColumnName.Girls_12_16_Names] = string.Empty;
                                row[ColumnName.Girls_17_Names] = string.Empty;
                                row[ColumnName.Total] = 0;
                                row[ColumnName.Kids] = string.Empty;

                                rawPSData.Rows.Add(row);
                            }

                            int age = (int)float.Parse((string)row[ColumnName.Age]);
                            bool boy = ((string)row[ColumnName.Gender]) == "M";
                            string name = (string)row[ColumnName.ChildName];

                            string section = boy ? "Boys" : "Girls";
                            if (age < 3) section += " 0-2";
                            else if (age < 7) section += " 3-6";
                            else if (age < 12) section += " 7-11";
                            else if (age < 17) section += " 12-16";
                            else if (age < 18) section += " 17";
                            else
                            {
                                _log.Warning("Project Smile - " + row[ColumnName.ContactFirstName] + " " + row[ColumnName.ContactLastName]
                                     + " child '" + name + "' is above the age limit.");
                                continue;
                            }

                            if (existingRow != null)
                                row = existingRow;

                            int count = 1 + int.Parse((string)row[section]);
                            row[section] = count;
                            row[section + " Names"] = (string)row[section + " Names"] + (count > 1 ? ", " : "") + name;
                            int total = 1 + int.Parse((string)row[ColumnName.Total]);
                            row[ColumnName.Total] = total;

                            if( total > 10 )
                            {
                                _log.Warning("Project Smile - " + row[ColumnName.ContactFirstName] + " " + row[ColumnName.ContactLastName]
                                    + " has too many kids. '" + name +"' has been excluded from the list.");
                            }
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

        //Deprecated
        private void MergeWithRow(DataRow existingRow, DataRow row, string numColName, string nameColName)
        {
            int numExisting = int.Parse((string)existingRow[numColName]);
            if (numExisting == 0)
            {
                existingRow[numColName] = row[numColName];
                existingRow[nameColName] = row[nameColName];
            }
            else
            {
                int numNew = int.Parse((string)row[numColName]);
                if (numNew == 0)
                    return;
                existingRow[numColName] = (numExisting + numNew).ToString();
                existingRow[nameColName] = (string)existingRow[nameColName] + ", " + (string)row[nameColName];
            }
        }

        public void WriteSpecial()
        {
            _currentOperation.Value = "Writing Special";
            var Entries = from myRow in Data.AsEnumerable()
                          where (myRow.Field<string>(ColumnName.ControlNumber).Contains("S"))
                          orderby myRow.Field<string>(ColumnName.TimeSlot) descending, myRow.Field<string>(ColumnName.ContactLastName)
                          select myRow;

            int page = 1;
            foreach (var e in Entries)
            {
                e[ColumnName.BookNumber] = "Special";
                e[ColumnName.PageNumber] = page++;
            }
            WriteCSV("Data/SpecialOut.csv", Entries, false);
        }

        public void WriteBook1()
        {
            _currentOperation.Value = "Writing Book1";
            var Entries = from myRow in Data.AsEnumerable()
                                where (myRow.Field<string>(ColumnName.ContactLastName).CompareTo(_strBreak1End) <= 0 &&
                                   !myRow.Field<string>(ColumnName.ControlNumber).Contains("S"))
                                orderby myRow.Field<string>(ColumnName.TimeSlotIndex) ascending, myRow.Field<string>(ColumnName.ContactLastName)
                                select myRow;

            int page=1;
            foreach(var e in Entries)
            {
                e[ColumnName.BookNumber] = "1";
                e[ColumnName.PageNumber] = page++;
            }
            WriteCSV("Data/RegistrationBook.csv", Entries, false);
        }

        public void WriteBook2()
        {
            _currentOperation.Value = "Writing Book2";
            var Entries = from myRow in Data.AsEnumerable()
                          where (myRow.Field<string>(ColumnName.ContactLastName).CompareTo(_strBreak2End) <= 0 &&
                                 myRow.Field<string>(ColumnName.ContactLastName).CompareTo(_strBreak2Begin) >= 0 &&
                                   !myRow.Field<string>(ColumnName.ControlNumber).Contains("S"))
                          orderby myRow.Field<string>(ColumnName.TimeSlotIndex) ascending, myRow.Field<string>(ColumnName.ContactLastName)
                          select myRow;

            int page = 1;
            foreach (var e in Entries)
            {
                e[ColumnName.BookNumber] = "2";
                e[ColumnName.PageNumber] = page++;
            }
            WriteCSV("Data/RegistrationBook.csv", Entries, true);
        }

        public void WriteBook3()
        {
            _currentOperation.Value = "Writing Book3";
            var Entries = from myRow in Data.AsEnumerable()
                          where (myRow.Field<string>(ColumnName.ContactLastName).CompareTo(_strBreak3End) <= 0 &&
                                 myRow.Field<string>(ColumnName.ContactLastName).CompareTo(_strBreak3Begin) >= 0 &&
                                   !myRow.Field<string>(ColumnName.ControlNumber).Contains("S"))
                          orderby myRow.Field<string>(ColumnName.TimeSlotIndex) ascending, myRow.Field<string>(ColumnName.ContactLastName)
                          select myRow;

            int page = 1;
            foreach (var e in Entries)
            {
                e[ColumnName.BookNumber] = "3";
                e[ColumnName.PageNumber] = page++;
            }
            WriteCSV("Data/RegistrationBook.csv", Entries, true);
        }

        public void WriteBook4()
        {
            _currentOperation.Value = "Writing Book4";
            var Entries = from myRow in Data.AsEnumerable()
                          where (myRow.Field<string>(ColumnName.ContactLastName).CompareTo(_strBreak4Begin) >= 0 &&
                                   !myRow.Field<string>(ColumnName.ControlNumber).Contains("S"))
                          orderby myRow.Field<string>(ColumnName.TimeSlotIndex) ascending, myRow.Field<string>(ColumnName.ContactLastName)
                          select myRow;

            int page = 1;
            foreach (var e in Entries)
            {
                e[ColumnName.BookNumber] = "4";
                e[ColumnName.PageNumber] = page++;
            }
            WriteCSV("Data/RegistrationBook.csv", Entries, true);
        }

        public void WriteTFTEmails()
        {
            _currentOperation.Value = "Writing TFTEmails";
            var Entries = from myRow in Data.AsEnumerable()
                            where (myRow.Field<string>(ColumnName.Organization) != "Project Smile" &&
                                   !myRow.Field<string>(ColumnName.ControlNumber).Contains("S"))
                            orderby myRow.Field<string>(ColumnName.ContactLastName)
                            select myRow;

            WriteCSV("Data/TFTEmails.csv", Entries, false);
        }

        public void WriteProjectSmileInvitations()
        {
            _currentOperation.Value = "Writing Project Smile Invitations";
            var PSEntries = from myRow in Data.AsEnumerable()
                                 where (myRow.Field<string>(ColumnName.Organization) == "Project Smile" &&
                                   !myRow.Field<string>(ColumnName.ControlNumber).Contains("S"))
                                 orderby myRow.Field<string>(ColumnName.ContactLastName)
                                 select myRow;

            WriteCSV("Data/PSInvitations.csv", PSEntries, false);
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
                row[ColumnName.TimeSlotIndex] = 3;
                row[ColumnName.TimeSlot] = timeSlot[3]; // All special handling go into final timeslot

                Data.Rows.Add(row);
            }

            _currentOperation.Value = "Merging - Done";
        }

        private void GenerateRegistrationData(DataRow r)
        {
            r[ColumnName.ProperName] = r[ColumnName.ContactLastName] + ", " + r[ColumnName.ContactFirstName];
            r[ColumnName.Kids] = string.Empty;
            int count = 0;
            count = ParseChildren(r, count, ColumnName.Girls_0_2, ColumnName.Girls_0_2_Names, "Girl 0-2");
            count = ParseChildren(r, count, ColumnName.Girls_3_6, ColumnName.Girls_3_6_Names, "Girl 3-6");
            count = ParseChildren(r, count, ColumnName.Girls_7_11, ColumnName.Girls_7_11_Names, "Girl 7-11");
            count = ParseChildren(r, count, ColumnName.Girls_12_16, ColumnName.Girls_12_16_Names, "Girl 12-16");
            count = ParseChildren(r, count, ColumnName.Girls_17, ColumnName.Girls_17_Names, "Girl 17");
            count = ParseChildren(r, count, ColumnName.Boys_0_2, ColumnName.Boys_0_2_Names, "Boy 0-2");
            count = ParseChildren(r, count, ColumnName.Boys_3_6, ColumnName.Boys_3_6_Names, "Boy 3-6");
            count = ParseChildren(r, count, ColumnName.Boys_7_11, ColumnName.Boys_7_11_Names, "Boy 7-11");
            count = ParseChildren(r, count, ColumnName.Boys_12_16, ColumnName.Boys_12_16_Names, "Boy 12-16");
            count = ParseChildren(r, count, ColumnName.Boys_17, ColumnName.Boys_17_Names, "Boy 17");
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
            StreamWriter swOut = new StreamWriter(filename, bAppend);

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
                _totalGirls_17 = 0;

        public void ComputeBreakOut()
        {
            _currentOperation.Value = "Computing BreakOut";
            var pendingEntries = from myRow in Data.AsEnumerable()
                                  //where (myRow.Field<string>(ColumnName.Status) == "Pending")
                                  orderby myRow.Field<string>(ColumnName.ContactLastName)
                                  select myRow;
            // Compute Totals
            int totalKids = 0;
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
            foreach (var e in pendingEntries)
            {
                _totalBoys_0_2 += Int32.Parse(e.Field<string>(ColumnName.Boys_0_2));
                _totalBoys_3_6 += Int32.Parse(e.Field<string>(ColumnName.Boys_3_6));
                _totalBoys_7_11 += Int32.Parse(e.Field<string>(ColumnName.Boys_7_11));
                _totalBoys_12_16 += Int32.Parse(e.Field<string>(ColumnName.Boys_12_16));
                _totalBoys_17 += Int32.Parse(e.Field<string>(ColumnName.Boys_17));
                _totalGirls_0_2 += Int32.Parse(e.Field<string>(ColumnName.Girls_0_2));
                _totalGirls_3_6 += Int32.Parse(e.Field<string>(ColumnName.Girls_3_6));
                _totalGirls_7_11 += Int32.Parse(e.Field<string>(ColumnName.Girls_7_11));
                _totalGirls_12_16 += Int32.Parse(e.Field<string>(ColumnName.Girls_12_16));
                _totalGirls_17 += Int32.Parse(e.Field<string>(ColumnName.Girls_17));
            }
            totalKids += _totalBoys_0_2 + _totalBoys_3_6 + _totalBoys_7_11 + _totalBoys_12_16 + _totalBoys_17 +
                _totalGirls_0_2 + _totalGirls_3_6 + _totalGirls_7_11 + _totalGirls_12_16 + _totalGirls_17;

            _currentOperation.Value =
                "\t0-2\t3-6\t7_11\t12-16\t17" + Environment.NewLine +
                "Boys\t" + _totalBoys_0_2 + '\t' + _totalBoys_3_6 + '\t' + _totalBoys_7_11 + '\t' + _totalBoys_12_16 + '\t' + _totalBoys_17 + Environment.NewLine +
                "Girls\t" + _totalGirls_0_2 + '\t' + _totalGirls_3_6 + '\t' + _totalGirls_7_11 + '\t' + _totalGirls_12_16 + '\t' + _totalGirls_17 + Environment.NewLine +
                "Total\t" + (_totalGirls_0_2 + _totalBoys_0_2) + '\t' + (_totalGirls_3_6 + _totalBoys_3_6) + '\t' + (_totalGirls_7_11 + _totalBoys_7_11) + '\t' +
                            (_totalGirls_12_16 + _totalBoys_12_16) + '\t' + (_totalGirls_17 + _totalBoys_17);

            int n = 0;
            int state = 0;
            _strBreak2Begin = _strBreak1End = _strBreak2End = _strBreak3Begin = _strBreak3End = _strBreak4Begin = string.Empty;
            foreach (var e in pendingEntries)
            {
                n += Int32.Parse(e.Field<string>(ColumnName.Boys_0_2)) +
                    Int32.Parse(e.Field<string>(ColumnName.Boys_3_6)) +
                    Int32.Parse(e.Field<string>(ColumnName.Boys_7_11)) +
                    Int32.Parse(e.Field<string>(ColumnName.Boys_12_16)) +
                    Int32.Parse(e.Field<string>(ColumnName.Boys_17)) +
                     Int32.Parse(e.Field<string>(ColumnName.Girls_0_2)) +
                    Int32.Parse(e.Field<string>(ColumnName.Girls_3_6)) +
                    Int32.Parse(e.Field<string>(ColumnName.Girls_7_11)) +
                    Int32.Parse(e.Field<string>(ColumnName.Girls_12_16)) +
                    Int32.Parse(e.Field<string>(ColumnName.Girls_17));
                string strLastName = (string)e[ColumnName.ContactLastName];
                switch (state)
                {
                    case 0:
                        if (state == 0 && n > totalKids / 4)
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
                        if (n > 2 * totalKids / 4)
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
                        if (n > 3 * totalKids / 4)
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

                        StreamWriter swOut = new StreamWriter("Data/Breakout.txt");
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
    }
}
