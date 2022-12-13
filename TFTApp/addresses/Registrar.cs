using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace addresses
{
    internal class Registrar
    {
        public static readonly string[] TimeSlots = { "8:00AM-9:00AM", "9:00AM-10:00AM", "10:00AM-11:00AM", "11:00AM-Noon", "Noon-1:00PM", "1:00PM-2:00PM", "2:00PM-3:00PM", "3:00PM-4:00PM" };

        public Registrar()
        { }

        public List<string> Run()
        {
            var ret = new List<string>();

            "    Registering - Starting".LogInfo();

            var dB = DataManager.Instance.CurrentYearDB!;

            "    --- Generating Registration Data".LogInfo();
            foreach (DataRow r in dB.Data.Rows)
                GenerateRegistrationData(r);

            #region Assign TimeSlot
            "    --- Assigning Time Slots".LogInfo();
            DataView dv = dB.Data.DefaultView;
            dv.Sort = ColumnName.ControlNumber;
            DataTable sortedTable = dv.ToTable();

            int numRows = dv.Count;
            int currentIndex = 0;
            int numPerSection = numRows / 8 + 2;

            foreach (DataRow r in sortedTable.Rows)
            {
                int timeSlotIndex = currentIndex / numPerSection;

                r[ColumnName.BookNumber] = currentIndex % 2 + 1;
                r[ColumnName.PageNumber] = currentIndex / 2 + 1;
                r[ColumnName.TimeSlotIndex] = timeSlotIndex;
                r[ColumnName.TimeSlot] = TimeSlots[timeSlotIndex];

                currentIndex++;

            }
            #endregion

            WriteBook(sortedTable);

            WriteEmailList(sortedTable);

            "   Registering - Done".LogInfo();

            return ret;
        }

        private void WriteEmailList(DataTable dB)
        {
            "Writing Email Invitation List".LogInfo();
            var Entries = from myRow in dB.AsEnumerable()
                          where (!string.IsNullOrEmpty(myRow.Field<string>(ColumnName.Email)) &&
                              myRow.Field<string>(ColumnName.AcceptsTerms) != "X")
                          orderby myRow.Field<string>(ColumnName.ContactLast)
                          select myRow;

            Utilities.WriteCSV("EmailList-Nice.csv", dB, Entries, false);

            Entries = from myRow in dB.AsEnumerable()
                          where (!string.IsNullOrEmpty(myRow.Field<string>(ColumnName.Email)) &&
                              myRow.Field<string>(ColumnName.AcceptsTerms) == "X")
                          orderby myRow.Field<string>(ColumnName.ContactLast)
                          select myRow;

            Utilities.WriteCSV("EmailList-Naughty.csv", dB, Entries, false);
        }

        public void WriteBook(DataTable dB)
        {
            $"Writing Book".LogInfo();

            var Entries = from myRow in dB.AsEnumerable()
                              //where (myRow.Field<string>(ColumnName.ContactLast).CompareTo(_strBreak1End) <= 0)
                          orderby myRow.Field<string>(ColumnName.TimeSlotIndex) ascending, myRow.Field<string>(ColumnName.ContactLast)
                          select myRow;

            int[] page = { 1, 1 };
            int numEntries = Entries.Count();
            int book = 0;
            foreach (var e in Entries)
            {
                e[ColumnName.BookNumber] = book + 1;
                e[ColumnName.PageNumber] = page[book];
                page[book]++;
                book = (book + 1) % 2;
            }

            var ResortedEntries = from myRow in dB.AsEnumerable()
                                  where (myRow.Field<string>(ColumnName.AcceptsTerms) != "X")
                                  orderby myRow.Field<int>(ColumnName.BookNumber) ascending,
                                    myRow.Field<int>(ColumnName.PageNumber)
                                  select myRow;

            Utilities.WriteCSV("RegistrationBook-Nice.csv", dB, ResortedEntries, false);

            ResortedEntries = from myRow in dB.AsEnumerable()
                              where (myRow.Field<string>(ColumnName.AcceptsTerms) == "X")
                              orderby myRow.Field<int>(ColumnName.BookNumber) ascending,
                                myRow.Field<int>(ColumnName.PageNumber)
                              select myRow;

            Utilities.WriteCSV("RegistrationBook-Naughty.csv", dB, ResortedEntries, false);
        }


        private static void GenerateRegistrationData(DataRow r)
        {
            r[ColumnName.ProperName] = r[ColumnName.ContactLast] + ", " + r[ColumnName.ContactFirst];
            r[ColumnName.Kids] = string.Empty;
            ParseChildren(r);
        }

        private static void ParseChildren(DataRow r)
        {
            for (int i = 1; i <= 10; i++)
            {
                string colNameAge = "ChildAge" + i;
                string colNameFirstName = "ChildFirst" + i;
                string colGender = "ChildGender" + i;
                if (r[colNameAge] is not DBNull &&
                    r[colNameFirstName] is not DBNull &&
                    int.TryParse((string)r[colNameAge], out int age) &&
                    age < 18)
                {
                    string firstName = Utilities.Pretty(((string)r[colNameFirstName]).Trim());
                    StringBuilder prefix = new();

                    string gender = (string)r[colGender];
                    if (gender.Length == 0)
                        prefix.Append("UNKN ");
                    else
                        prefix.Append(((string)r[colGender]).Substring(0, 1) == "F" ? "Girl " : "Boy  ");
                    if (age < 3)
                        prefix.Append("0-2  ");
                    else if (age < 7)
                        prefix.Append("3-6  ");
                    else if (age < 12)
                        prefix.Append("7-11 ");
                    else if (age < 17)
                        prefix.Append("12-16");
                    else
                        prefix.Append("17   ");
                    if (age < 10)
                        prefix.Append(" ");
                    r["R" + (i - 1).ToString()] = prefix.ToString() + $" ({age}): " + firstName;
                    bool bFirst = string.IsNullOrEmpty(((string)r[ColumnName.Kids]));
                    r[ColumnName.Kids] = r[ColumnName.Kids] + (bFirst ? "" : ", ") + firstName + $"({age})";
                }
            }
        }
    }
}
