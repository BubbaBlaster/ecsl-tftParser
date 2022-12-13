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

public class DuplicatesAnalyzer
{
    readonly TFTData DB;

    public DuplicatesAnalyzer(TFTData db)
    {
        DB = db;
    }

    public void Run()
    {
        $"    Analyzing '{DB.TFTFilename}' for Duplicates".LogInfo();

        foreach (var r in FindSimilarChildrenLists())
            DataManager.Instance.AddErrors(DB.TFTFilename, r.Key, r.Value);

        $"    Analyzing '{DB.TFTFilename}' for Duplicates - Done".LogInfo();
    }

    class ControlNumberProperties
    {
        readonly public List<string> ChildrenDupsCNList = new();
        public string StreetNum = string.Empty;
        public string StreetName = string.Empty;
        public string Phone = string.Empty;
        public string Email = string.Empty;
        public string NameComposite = string.Empty;
        public string SimilarEntryIssues = string.Empty;
        readonly public List<string> SimilarEntryList = new();
    }

    readonly private Dictionary<string, ControlNumberProperties> DictControlNumberProperties = new();


    public static bool AreNumbersUnique(int[] nameIndexes)
    {
        var hash = new HashSet<int>();
        foreach (var n in nameIndexes)
            hash.Add(n);
        return (hash.Count == nameIndexes.Length);
    }

    public Dictionary<string, List<string>> FindSimilarChildrenLists()
    {
        var ret = new Dictionary<string, List<string>>();

        int currentProgress = -1;
        double progress = 0;
        "    Checking for duplicate children within each individual entry".LogInfo();

        double numRos = DB.Data.Rows.Count;
        foreach (DataRow r in DB.Data.Rows)
        {
            progress++;
            if (currentProgress != (int)(progress / numRos * 10f))
            {
                currentProgress = (int)(progress / numRos * 10f);
                $"       step {currentProgress} of 10.".LogInfo();
            }

            // Check if the status is already "approved"
            if (string.Compare((string)r[ColumnName.Status], "approved", true) == 0) continue;
            string cn = (string)r[ColumnName.ControlNumber];

            // If the children are listed more than once within the same list
            if (!AreNumbersUnique(DB.DictControlNumberToChildrenIndex[cn]))
            {
                if (!ret.TryGetValue(cn, out var list))
                    list = ret[cn] = new List<string>();
                list.Add("One or more children have the same first name.");
            }

            // start constructing the name indexes used for similar submissions below
            DictControlNumberProperties.Add(cn, new());
        }

        "    Checking for similar submissions".LogInfo();
        progress = 0;
        currentProgress = -1;
        double numEntries = DB.DictControlNumberToChildrenIndex.Count;
        // for each submission (given by ControlNumber) check to see how many other submissions are similar.
        foreach (var tuple1 in DB.DictControlNumberToChildrenIndex)
        {
            progress++;
            if (currentProgress != (int)(progress / numEntries * 10f))
            {
                currentProgress = (int)(progress / numEntries * 10f);
                $"       step {currentProgress} of 10.".LogInfo();
            }

            int cn1 = int.Parse(tuple1.Key);

            // are there at least 2 children?
            if (tuple1.Value.Length > 2)
            {
                // examine all other submissions
                foreach (var tuple2 in DB.DictControlNumberToChildrenIndex)
                {
                    int cn2 = int.Parse(tuple2.Key);
                    if (tuple2.Value.Length < 3 || cn1 == cn2) continue;

                    // merge the two unique children name indexes into one list called 'merged'
                    HashSet<int> merged = new();
                    foreach (var val in tuple1.Value) merged.Add(val);
                    foreach (var val in tuple2.Value) merged.Add(val);

                    // if the length of merged is less than 70% of the combined length of both together, then there are a significant number of duplicates
                    if (merged.Count < .7 * (tuple1.Value.Length + tuple2.Value.Length))
                    {
                        DictControlNumberProperties[tuple1.Key].ChildrenDupsCNList.Add(tuple2.Key);
                    }
                }
            }
        }

        foreach (var tuple in DictControlNumberProperties)
        {
            if (tuple.Value.ChildrenDupsCNList.Any())
            {
                var cn = tuple.Key;
                if (!ret.TryGetValue(tuple.Key, out var list))
                    list = ret[cn] = new List<string>();

                var iter = tuple.Value.ChildrenDupsCNList.GetEnumerator();
                iter.MoveNext();
                string v = iter.Current;
                while (iter.MoveNext())
                    v += "," + iter.Current;
                list.Add($"Duplicates with {v}");
            }
        }

        {
            StreamWriter sw = new(DataManager.Instance.OutputDir + $"/ChildrenDupsIssuesReport-{DB.TFTFilename}.txt");
            string strHeading = "------------------------------------------------------------------------------";
            foreach (var tuple in DictControlNumberProperties)
            {
                if (tuple.Value.ChildrenDupsCNList.Any())
                {
                    sw.WriteLine(strHeading);
                    ReportWriter.WriteList(sw, DB.Data, new() { tuple.Key });
                    sw.WriteLine("                                                  may have duplicate children");
                    ReportWriter.WriteList(sw, DB.Data, tuple.Value.ChildrenDupsCNList);
                }
            }
            sw.Close();
        }
        return ret;
    }
}
