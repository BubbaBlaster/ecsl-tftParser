using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Agora.SDK;
using static addresses.Utilities;
using System.Data;

namespace addresses;
internal class BannedAnalyzer
{
    public BannedAnalyzer() { }
    public void Run()
    {
        "Analyzing for Banned Entries - Starting".LogInfo();
        string filename = DataManager.Instance.InputDir + '/' + Config["Data:BannedFilename"];
        Agora.Utilities.CsvReader reader = new();

        var lines = reader.ReadCSV(filename);
        if (lines == null ||
            lines.Length == 0 ||
            lines[0].Length == 0)
        {
            "Error reading Banned.".LogError();
            return;
        }

        HashSet<string> bannedNames = new();
        Dictionary<string, string> bannedEmails = new();
        Dictionary<string, string> bannedPhones = new();

        for (int i = 1; i < lines.Length; i++)
        {
            string name = Pretty(lines[i][2]) + ',' + Pretty(lines[i][3]);
            bannedNames.Add(name);
            bannedEmails.TryAdd(Pretty(lines[i][9]), name);
            bannedPhones.TryAdd(PhoneString(lines[i][8]), name);
        }

        List<Tuple<DataRow, string>> bannedRows = new();

        var dB = DataManager.Instance.CurrentYearDB!;
        foreach (DataRow row in dB.Data.Rows)
        {
            string name = (string)row[ColumnName.ContactLast] + ',' + (string)row[ColumnName.ContactFirst];
            if (bannedNames.Contains(name))
            {
                bannedRows.Add(new(row, "Banned Primary Contact"));
                continue;
            }
            name = (string)row[ColumnName.Contact2Last] + ',' + (string)row[ColumnName.Contact2First];
            if (bannedNames.Contains(name))
            {
                bannedRows.Add(new(row, "Banned Secondary Contact"));
                continue;
            }
            if (bannedEmails.ContainsKey((string)row[ColumnName.Email]))
            {
                bannedRows.Add(new(row, "Banned Email"));
                continue;
            }
            if (bannedPhones.ContainsKey((string)row[ColumnName.Phone]))
            {
                bannedRows.Add(new(row, "Banned Phone1"));
                continue;
            }
            if (bannedPhones.ContainsKey((string)row[ColumnName.Phone2]))
            {
                bannedRows.Add(new(row, "Banned Phone2"));
                continue;
            }
        }

        StreamWriter sw = new(DataManager.Instance.OutputDir + $"/BannedEntriesReport-{dB.TFTFilename}.txt");

        foreach (var item in bannedRows)
            sw.WriteLine((string)item.Item1[ColumnName.ControlNumber] + " - " + item.Item2);

        sw.Close();


        "Analyzing for Banned Entries - Done".LogInfo();
    }
}
