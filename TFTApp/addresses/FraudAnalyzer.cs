using Microsoft.Extensions.Configuration;
using System.Data;
using System.Globalization;
using System.Text;
using static Agora.SDK;

namespace addresses;

public class FraudAnalyzer
{
    TFTData Data;
    List<string> PrevDataFilenames;

    public FraudAnalyzer(TFTData thisYearsData)
    {
        Data = thisYearsData;
        PrevDataFilenames = Config.GetSection("Data:PrevYearFilenames").Get<List<string>>();
    }

    Dictionary<string, List<DataRow>> DictContactNameMatches = new();
    Dictionary<string, List<DataRow>> DictPhoneMatches = new();
    Dictionary<string, List<DataRow>> DictEmailMatches = new();

    void ProcessRow(DataRow row)
    {
        var phone = (string)row[ColumnName.Phone];
        var phone2 = (string)row[ColumnName.Phone2];
        var email = (string)row[ColumnName.Email];
        var contactName = (string)row[ColumnName.ContactLast] + ',' + (string)row[ColumnName.ContactFirst] +
            " <" + email + '>';
        var contact2Name = (string)row[ColumnName.Contact2Last] + ',' + (string)row[ColumnName.Contact2First];

        if (!string.IsNullOrEmpty(contactName))
        {
            if (!DictContactNameMatches.TryGetValue(contactName, out var list))
                DictContactNameMatches.Add(contactName, list = new List<DataRow>());
            list.Add(row);
        }

        if (!string.IsNullOrEmpty(contact2Name))
        {
            if (!DictContactNameMatches.TryGetValue(contact2Name, out var list))
                DictContactNameMatches.Add(contact2Name, list = new List<DataRow>());
            list.Add(row);
        }

        if (!string.IsNullOrEmpty(phone))
        {
            if (!DictPhoneMatches.TryGetValue(phone, out var list))
                DictPhoneMatches.Add(phone, list = new List<DataRow>());
            list.Add(row);
        }

        if (!string.IsNullOrEmpty(phone2))
        {
            if (!DictPhoneMatches.TryGetValue(phone2, out var list))
                DictPhoneMatches.Add(phone2, list = new List<DataRow>());
            list.Add(row);
        }

        if (!string.IsNullOrEmpty(email))
        {
            if (!DictEmailMatches.TryGetValue(email, out var list))
                DictEmailMatches.Add(email, list = new List<DataRow>());
            list.Add(row);
        }
    }

    public Dictionary<string, List<string>> Run()
    {
        var ret = new Dictionary<string, List<string>>();

        StringBuilder sb = new();

        "    Checking for Fraud - Starting".LogInfo();

        List<TFTData> prevData = new();

        List<string> tableColNames = new();
        tableColNames.Add( Data.TFTFilename );


        foreach (string filename in PrevDataFilenames)
        {
            $"       Loading '{filename}'".LogInfo();
            prevData.Add(new(filename));
            tableColNames.Add(filename);
        }

        Dictionary<string, TFTData> DictTFTDataByTableName = new();

        foreach (TFTData oldData in prevData)
        {
            $"       Processing '{oldData.TFTFilename}'".LogInfo();
            DictTFTDataByTableName[oldData.Data.TableName] = oldData;
            foreach (DataRow oldRow in oldData.Data.Rows)
                ProcessRow(oldRow);
        }

        StreamWriter sw = new StreamWriter(DataManager.Instance.OutputDir + $"/FraudIssuesReport-{Data.TFTFilename}.txt");
        string strHeading = "------------------------------------------------------------------------------";

        "        Checking for Matches".LogInfo();
        foreach (DataRow row in Data.Data.Rows)
        {
            HashSet<DataRow> matches = new();

            var cn = (string)row[ColumnName.ControlNumber];
            var phone = (string)row[ColumnName.Phone];
            var phone2 = (string)row[ColumnName.Phone2];
            var email = (string)row[ColumnName.Email];
            var contactName = (string)row[ColumnName.ContactLast] + ',' + (string)row[ColumnName.ContactFirst] +
                " <" + email + '>' ;
            var contact2Name = (string)row[ColumnName.Contact2Last] + ',' + (string)row[ColumnName.Contact2First];

            if (contactName.Length > 1)
            {
                if (DictContactNameMatches.TryGetValue(contactName, out var list))
                    foreach(var r in list) matches.Add(r);
            }
            if (false && contact2Name.Length > 1)
            {
                if (DictContactNameMatches.TryGetValue(contact2Name, out var list))
                    foreach (var r in list) matches.Add(r);
            }
            if (false && !string.IsNullOrEmpty(phone))
            {
                if (DictPhoneMatches.TryGetValue(phone, out var list))
                    foreach (var r in list) matches.Add(r);
            }
            if (false && !string.IsNullOrEmpty(phone2))
            {
                if (DictPhoneMatches.TryGetValue(phone2, out var list))
                    foreach (var r in list) matches.Add(r);
            }
            if (!string.IsNullOrEmpty(email))
            {
                if (DictPhoneMatches.TryGetValue(email, out var list))
                    foreach (var r in list) matches.Add(r);
            }

            HashSet<int> indexes = new();
            foreach (var r in matches)
            {                
                string cnx = (string)r[ColumnName.ControlNumber];
                var childNameIndices = DictTFTDataByTableName[r.Table.TableName].DictControlNumberToChildrenIndex[cnx];
                foreach (var child in childNameIndices)
                    indexes.Add(child);
            }
            var childNameIndicesInCurrent = Data.DictControlNumberToChildrenIndex[cn];
            foreach (var d in childNameIndicesInCurrent) {
                indexes.Remove(d);
            }

            Dictionary<string, Dictionary<string, string>> table = new();

            void AddChildren(DataRow r)
            {
                string filename = (string)r[ColumnName.Filename];
                for (int i = 1; i < 11; i++)
                {
                    string colFirstName = ColumnName.ChildFirst + i.ToString();
                    string colAge = ColumnName.ChildAge + i.ToString();

                    if (r[colFirstName] is not DBNull)
                    {
                        string firstName = (string)r[colFirstName];

                        if( !string.IsNullOrEmpty(firstName) )
                        {
                            if ( !table.TryGetValue(firstName, out var dictYearAge))
                                dictYearAge = table[firstName] = new();
                            dictYearAge[filename] = (string)r[colAge] + ':' + (string)r[ColumnName.ControlNumber];
                        }
                    }
                }
            }

            AddChildren(row);
            foreach (var matchRows in matches)
                AddChildren(matchRows);

            sw.WriteLine(strHeading);
            
            sb.Clear();
            sb.Append(row[ColumnName.ControlNumber]).Append(" - ")
                .Append(row[ColumnName.ContactLast]).Append(", ")
                .Append(row[ColumnName.ContactFirst]).Append(" <")
                .Append(row[ColumnName.Email]).Append(">");
            sw.WriteLine(sb.ToString());

            sb.Clear();
            string spaces = "                                                                            ";
            sb.Append(spaces[0..12]);
            foreach(var fNames in tableColNames)
            {
                sb.Append(fNames).Append(spaces[fNames.Length..12]);
            }
            sw.WriteLine(sb.ToString());

            foreach(var name in table.Keys)
            {
                sb.Clear();
                string n = name.Substring(0, name.Length < 12 ? name.Length : 11);
                sb.Append(n).Append(spaces[n.Length..12]);
                var dictYearAge = table[name];
                foreach (var fNames in tableColNames)
                {
                    if (dictYearAge.TryGetValue(fNames, out var age))
                        sb.Append(age).Append(spaces[age.Length..12]);
                    else
                        sb.Append(spaces[0..12]); 
                }
                sw.WriteLine(sb.ToString());
            }
        }
        sw.Close();        

        return ret;
    }
}
