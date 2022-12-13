using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace addresses
{
    public static class ReportWriter
    {
        public static void WriteList(StreamWriter sw, DataTable table, List<string> list)
        {
            bool bFirst = true;
            foreach (var key in list)
            {
                if (!string.IsNullOrEmpty(key))
                {
                    if( !bFirst)
                        sw.WriteLine("   - - - - - - -");
                    else
                        bFirst = false;
                    var row = table.Rows.Find(key);
                    if (row == null)
                        throw new NoNullAllowedException("Found null row...");
                    WriteFamily(sw, row);
                }
            }
        }

        public static void WriteFamily(StreamWriter sw, DataRow row)
        {
            static string S(object v)
            {
                if (v is DBNull) return string.Empty;
                return (string)v;
            }

            string cn = S(row[ColumnName.ControlNumber]);
            int nCN = int.Parse(cn);
            if (nCN < 0)
                sw.WriteLine("Special List");
            else
                sw.WriteLine($"TFT List - {row.Table.TableName}");

            sw.WriteLine(cn + "   " + S(row[ColumnName.ContactLast]) + ", " + S(row[ColumnName.ContactFirst]));
            if (row[ColumnName.Address2] is not DBNull)
            {
                sw.WriteLine(S(row[ColumnName.Address]) + " " + S(row[ColumnName.Address2]) +
                    ", " + S(row[ColumnName.City]) + "  " + S(row[ColumnName.Zip]));
            }
            else
                sw.WriteLine(S(row[ColumnName.Address]) + ", " + S(row[ColumnName.City]) + "  " + S(row[ColumnName.Zip]));

            sw.WriteLine(S(row[ColumnName.Phone]) + " - " + S(row[ColumnName.Email]));
            for (int i = 1; i <= 10; i++)
            {
                if (row["ChildFirst" + i] is not DBNull)
                {
                    if (S(row["ChildFirst" + i]).Length > 0)
                        sw.WriteLine(S(row["ChildFirst" + i]) + " " + S(row["ChildLast" + i]) + " " + S(row["ChildAge" + i]) + " (" +
                            S(row["ChildGender" + i]) + ")");
                }
            }
        }
    }
}
