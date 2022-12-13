using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace addresses;

public static class Utilities
{
    public static string Pretty(string txt)
    {
        txt = txt.Replace(",", ", ");
        string[] words = txt.ToLower().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string ret = "";
        bool bFirst = true;
        foreach (var w in words)
        {
            if (!bFirst)
                ret += ' ';
            else
                bFirst = false;
            ret += char.ToUpper(w[0]) + w[1..];
        }

        return ret;
    }

    public static string PhoneString(string input)
    {
        string result = string.Empty;
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (c >= '0' && c <= '9')
                result += c;
        }
        if (result.Length == 10)
            return result[..3] + '-' +
                result[3..6] + '-' +
                result[6..10];
        return input.Trim();
    }

    public static void CorrectPhone(DataRow row)
    {
        if (row.Table.Columns.Contains(ColumnName.Phone) &&
            row[ColumnName.Phone] is not DBNull)
            row[ColumnName.Phone] = PhoneString((string)row[ColumnName.Phone]);
        if (row.Table.Columns.Contains(ColumnName.Phone2) &&
            row[ColumnName.Phone2] is not DBNull)
            row[ColumnName.Phone2] = PhoneString((string)row[ColumnName.Phone2]);
    }

    public static void WriteCSV(string filename, DataTable dB, OrderedEnumerableRowCollection<DataRow> data, bool bAppend)
    {
        StreamWriter swOut = new(DataManager.Instance.OutputDir + '/' + filename, bAppend);

        if (!bAppend)
        {
            // write headings
            bool bFirst = true;
            foreach (DataColumn s in dB.Columns)
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
            foreach (DataColumn s in dB.Columns)
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
}
