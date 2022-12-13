using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Agora.SDK;

namespace addresses
{
    public class ChildFirstNameAnalyzer
    {
        public static void Run(TFTData db)
        {
            string OutFileName = "ChildFirstNameErrors-" + db.TFTFilename + ".txt";
            string OutputDir = DataManager.Instance.OutputDir;

            $"    Analyzing Child First Name Errors '{db.TFTFilename}'".LogInfo();

            StreamWriter sw = new(OutputDir + "/" + OutFileName);

            // construct the data to search
            foreach (DataRow r in db.Data.Rows)
            {
                var results = AnalyzeRow(r);
                if (results.Any())
                    DataManager.Instance.AddErrors(db.TFTFilename, (string)r[ColumnName.ControlNumber], results);
            }
            sw.Close();

            $"    Analyzing Child First Name Errors '{db.TFTFilename}' - Done".LogInfo();
        }

        public static List<string> AnalyzeRow(DataRow r)
        {
            List<string> ret = new();

            for (int i = 1; i < 11; i++)
            {
                string colFirstName = "ChildFirst" + i;
                string colAge = "ChildAge" + i;

                if (r[colAge] is not System.DBNull && !string.IsNullOrEmpty((string)r[colAge]))
                {
                    var name = (string)r[colFirstName];
                    if (string.IsNullOrEmpty(name))
                    {
                        ret.Add($"Child {i} First Name Blank");
                    }
                    else if (name == (string)r[ColumnName.ContactLast])
                    {
                        ret.Add($"Child {i} FirstName same as ContactLast");
                    }
                }
            }
            return ret;
        }
    }
}
