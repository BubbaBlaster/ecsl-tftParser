using Agora.Utilities;
using System;
using System.Data;
using System.Text.RegularExpressions;

namespace addresses
{
    public class TFTData
    {
        public DataTable Data { get; } = new DataTable();
        public string TFTFilename { get; }
        public Dictionary<string, int[]> DictControlNumberToChildrenIndex = new();

        public TFTData(string filename)
        {
            TFTFilename = filename;

            Initialize();

            try
            {
                $"Reading '{TFTFilename}' - Starting".LogInfo();

                Read();

                $"Reading '{TFTFilename}' - Done".LogInfo();
            }
            catch (Exception ex)
            {
                $"Exception while reading TFTData[{TFTFilename}].".LogException(ex, Agora.Logging.LogLevel.Fatal);
                return;
            }
        }

        private void Initialize()
        {
            $"Initializing '{TFTFilename}' Database - Starting".LogInfo();
            Data.Clear();
            foreach (var colName in ColumnName.Array)
                AddColumn(colName);

            foreach (var colName in ColumnName.ArrayInts)
                Data.Columns.Add(new DataColumn(colName, typeof(int)));

            Data.PrimaryKey = new DataColumn[1] { Data.Columns[ColumnName.ControlNumber]! };
            $"Initializing '{TFTFilename}' Database - Done".LogInfo();
        }

        void AddColumn(string name)
        {
            if (!Data.Columns.Contains(name))
                Data.Columns.Add(name);
        }

        private bool Read()
        {
            int currentLine = 0;
            Data.Clear();
            Data.TableName = TFTFilename;
            try
            {
                string filename = DataManager.Instance.InputDir + "/" + TFTFilename;
                if (!File.Exists(filename))
                {
                    $"    '{TFTFilename}' not found in '{DataManager.Instance.InputDir}' - Skipping".LogWarn();
                    return false;
                }

                CsvReader reader = new();
                var lines = reader.ReadCSV(filename);
                $"    Read {lines.Length} lines.".LogInfo();

                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    // Read the header row
                    var header = lines[0];
                    for (int i = 0; i < header.Length; i++)
                    {
                        string columnName = header[i] = header[i].Trim(new char[] { '"', '\\', ',', ' ' });
                        AddColumn(columnName);
                    }
                    AddColumn(ColumnName.Organization);
                    AddColumn(ColumnName.TimeSlot);
                    AddColumn(ColumnName.TimeSlotIndex);
                    AddColumn(ColumnName.Total);
                    AddColumn(ColumnName.Address2);
                    AddColumn(ColumnName.Penalty);
                    AddColumn(ColumnName.Filename);
                    Data.PrimaryKey = new DataColumn[1] { Data.Columns[ColumnName.ControlNumber]! };

                    for(int num=1; num < lines.Length; num++) 
                    {
                        currentLine = num;
                        string[] tagInfo = lines[num];

                        if (Agora.Logging.AgoraLogger.GetVerbosity() == Agora.Logging.LogLevel.Trace)
                            $"    Processing Line {num}".LogTrace();

                        DataRow row = Data.NewRow();
                        for (int ii = 0; ii < header.Length; ii++)
                        {
                            string tag = tagInfo[ii].Trim(new char[] { '"', '\\', ',' });
                            row[header[ii]] = Utilities.Pretty(tag);
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
                            row[ColumnName.Filename] = TFTFilename;
                            Utilities.CorrectPhone(row);
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

                            Data.Rows.Add(row);
                        }
                        else
                            $"    Skipping {(string)row[ColumnName.ControlNumber]} - {state}".LogTrace();
                    }
                }
            }
            catch (Exception e)
            {
                $"    Reading TFT DB - Failed on line {currentLine}".LogException(e);
                return false;
            }
            IndexChildrenNames();

            return true;
        }

        public void IndexChildrenNames()
        {
            "   Indexing Children Name".LogInfo();
            double progress = 0;
            int currentProgress = -1;
            double numRos = Data.Rows.Count;
            foreach (DataRow r in Data.Rows)
            {
                progress++;
                if (currentProgress != (int)(progress / numRos * 10f))
                {
                    currentProgress = (int)(progress / numRos * 10f);
                    $"       step {currentProgress} of 10.".LogInfo();
                }

                var nameIndexes = DataManager.NameIndexer.GetNameIndices(r);
                string cn = (string)r[ColumnName.ControlNumber];

                // start constructing the name indexes used for similar submissions below
                DictControlNumberToChildrenIndex.Add(cn, nameIndexes);
            }
            "   Indexing Children Name - Done".LogInfo();
        }
    }
}