using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Agora.SDK;

namespace addresses;

public class SimilarEntryAnalyzer
{
    readonly TFTData DB;

    public SimilarEntryAnalyzer(TFTData db)
    {
        DB = db;
    }

    public void Run()
    {
        $"    Analyzing '{DB.TFTFilename}' for Similar Entries - Starting".LogInfo();

        foreach (var r in FindSimilarContactInformation())
            DataManager.Instance.AddErrors(DB.TFTFilename, r.Key, r.Value);

        $"    Analyzing '{DB.TFTFilename}' for Similar Entries - Done".LogInfo();
    }

    class ControlNumberProperties
    {
        public string StreetNum = string.Empty;
        public string StreetName = string.Empty;
        public string Phone = string.Empty;
        public string Email = string.Empty;
        public string NameComposite = string.Empty;
        public string SimilarEntryIssues = string.Empty;
        readonly public List<string> SimilarEntryList = new();
    }

    readonly private Dictionary<string, ControlNumberProperties> DictControlNumberProperties = new();

    public Dictionary<string, List<string>> FindSimilarContactInformation()
    {
        var ret = new Dictionary<string, List<string>>();

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
        "    Checking for Similar Contact Information - Starting".LogInfo();

        double numRows = DB.Data.Rows.Count;
        double progress = 0;
        // construct the data to search
        foreach (DataRow r1 in DB.Data.Rows)
        {
            progress++;
            if (currentProgress != (int)(progress / numRows * 10f))
            {
                currentProgress = (int)(progress / numRows * 10f);
                $"       Part 1/2 - step {currentProgress} of 10.".LogInfo();
            }
            string cn = (string)r1[ColumnName.ControlNumber];
            if (!DictControlNumberProperties.TryGetValue(cn, out var td))
                td = DictControlNumberProperties[cn] = new();

            string address;
            if (r1[ColumnName.Address2] is not DBNull)
                address = (string)r1[ColumnName.Address] + (string)r1[ColumnName.Address2];
            else
                address = (string)r1[ColumnName.Address];
            td.StreetNum = FirstNumberInString(address);
            td.StreetName = Utilities.Pretty(StringAfterNumber(address));
            td.Phone = (string)r1[ColumnName.Phone];
            td.Email = (string)r1[ColumnName.Email];
            td.NameComposite = (((string)r1[ColumnName.ContactFirst]) + "---").Substring(0, 3).ToUpper() +
                                  (((string)r1[ColumnName.ContactLast]) + "---").Substring(0, 3).ToUpper();
            td.SimilarEntryIssues = string.Empty;
        }

        progress = 0;
        currentProgress = -1;
        numRows = DictControlNumberProperties.Count;
        foreach (var tuple1 in DictControlNumberProperties)
        {
            progress++;
            if (currentProgress != (int)(progress / numRows * 10f))
            {
                currentProgress = (int)(progress / numRows * 10f);
                $"       Part 2/2 - step {currentProgress} of 10.".LogInfo();
            }
            int t1 = int.Parse(tuple1.Key);
            foreach (var tuple2 in DictControlNumberProperties)
            {
                int t2 = int.Parse(tuple2.Key);
                if (t1 >= t2) continue;

                if ((tuple1.Value.StreetNum.Length > 0 && tuple1.Value.StreetNum == tuple2.Value.StreetNum &&
                     tuple1.Value.StreetName.Length > 5 && tuple1.Value.StreetName == tuple2.Value.StreetName) ||
                    (tuple1.Value.Phone.Length > 5 && tuple1.Value.Phone == tuple2.Value.Phone) ||
                    (tuple1.Value.Email.Length > 5 && tuple1.Value.Email == tuple2.Value.Email))
                {
                    // check to see that each children's name is unique in both entries

                    HashSet<int> merged = new HashSet<int>();
                    foreach (var val in DB.DictControlNumberToChildrenIndex[tuple1.Key]) merged.Add(val);
                    foreach (var val in DB.DictControlNumberToChildrenIndex[tuple2.Key]) merged.Add(val);
                    
                    if (merged.Count < DB.DictControlNumberToChildrenIndex[tuple1.Key].Length + 
                                       DB.DictControlNumberToChildrenIndex[tuple2.Key].Length)
                    {
                        DictControlNumberProperties[tuple1.Key].SimilarEntryIssues += tuple2.Key;
                        DictControlNumberProperties[tuple1.Key].SimilarEntryList.Add(tuple2.Key);
                    }
                }
            }
        }

        foreach (var tuple in DictControlNumberProperties)
        {
            if (!string.IsNullOrEmpty(tuple.Value.SimilarEntryIssues))
            {                
                var cn = tuple.Key;
                if (!ret.TryGetValue(tuple.Key, out var list))
                    list = ret[cn] = new List<string>();

                var iter = tuple.Value.SimilarEntryList.GetEnumerator();
                iter.MoveNext();
                string v = iter.Current;
                while (iter.MoveNext())
                    v += "," + iter.Current;
                list.Add($"Similar to {v}");
            }
        }

        {
            StreamWriter sw = new StreamWriter(DataManager.Instance.OutputDir + $"/SimilarEntryIssuesReport-{DB.TFTFilename}.txt");
            string strHeading = "------------------------------------------------------------------------------";
            foreach (var tuple in DictControlNumberProperties)
            {
                if (!string.IsNullOrEmpty(tuple.Value.SimilarEntryIssues))
                {
                    sw.WriteLine(strHeading);
                    ReportWriter.WriteList(sw, DB.Data, new List<string>() { tuple.Key });
                    sw.WriteLine("                                                                is SIMILAR TO");
                    ReportWriter.WriteList(sw, DB.Data, tuple.Value.SimilarEntryList);
                }
            }
            sw.Close();
        }

        "    Checking for Similar Contact Information - Done".LogInfo();

        return ret;
    }

   
}
