using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Agora.SDK;
using static addresses.Utilities;
using System.Data;
using Agora.Utilities;

namespace addresses;
internal class BannedAnalyzer
{
    public Dictionary<string, List<string>> BannedControlNumbers = new();
    public BannedAnalyzer() { }
    public void Run()
    {
        "Analyzing for Banned Entries - Starting".LogInfo();
        string filename = DataManager.Instance.InputDir + '/' + Config["Data:BannedFilename"];

        var lines = CsvReader.ReadCSV(filename);
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
            bannedEmails.TryAdd(lines[i][10].ToLower(), name);
            bannedPhones.TryAdd(PhoneString(lines[i][8]), name);
        }

        List<Tuple<DataRow, string>> bannedRows = new();

        var dB = DataManager.Instance.CurrentYearDB!;
        foreach (DataRow row in dB.Data.Rows)
        {
            if (((string)row[ColumnName.Status]).ToLower() == "approved")
                continue;
            string name = (string)row[ColumnName.ContactLast] + ',' + (string)row[ColumnName.ContactFirst];
            if (bannedNames.Contains(name))
            {
                bannedRows.Add(new(row, $"Banned Primary Contact - {name}"));
            }
            name = (string)row[ColumnName.Contact2Last] + ',' + (string)row[ColumnName.Contact2First];
            if (bannedNames.Contains(name))
            {
                bannedRows.Add(new(row, $"Banned Secondary Contact - {name}"));
            }

            string key = ((string)row[ColumnName.Email]).ToLower();
            if (bannedEmails.ContainsKey(key))
            {
                bannedRows.Add(new(row, $"Banned Email - {bannedEmails[key]}"));
            }

            key = (string)row[ColumnName.Phone];
            if (bannedPhones.ContainsKey(key))
            {
                bannedRows.Add(new(row, $"Banned Phone1 - {bannedPhones[key]}"));
            }

            key = (string)row[ColumnName.Phone2];
            if (bannedPhones.ContainsKey(key))
            {
                bannedRows.Add(new(row, $"Banned Phone2 - {bannedPhones[key]}"));
            }
            if (((string)row[ColumnName.Address]).ToLower().Contains("tomasa"))
            {
                bannedRows.Add(new(row, $"Banned Street (Tomasa)"));
            }
        }

        StreamWriter sw = new(DataManager.Instance.OutputDir + $"/BannedEntriesReport-{dB.TFTFilename}.txt");

        foreach (var item in bannedRows)
        {
            string cn = (string)item.Item1[ColumnName.ControlNumber];
            string reason = item.Item2;
            if (!BannedControlNumbers.ContainsKey(cn))
                BannedControlNumbers.Add(cn, new() { reason });
            else
                BannedControlNumbers[cn].Add(reason);
            sw.WriteLine(cn + " - " + reason);
        }

        sw.Close();


        "Analyzing for Banned Entries - Done".LogInfo();
    }
}
