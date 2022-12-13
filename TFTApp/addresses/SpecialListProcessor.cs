using System.Data;
using static Agora.SDK;

namespace addresses;

internal class SpecialListProcessor
{
    internal void Run()
    {
        string filename = Config["Data:SpecialFilename"];

        if (string.IsNullOrEmpty(filename))
        {
            "'Data:SpecialFilename' not set. Skipping Special Processing.".LogInfo();
            return;
        }

        TFTData special = new(filename);

        if (special.Data.Rows.Count == 0)
        {
            $"Special: No data in '{filename}'.".LogInfo();
            return;
        }

        "    --- Generating Special Registration Data".LogInfo();
        foreach (DataRow r in special.Data.Rows)
            Registrar.GenerateRegistrationData(r);

        #region Assign TimeSlot
        "    --- Assigning Time Slots".LogInfo();
        DataView dv = special.Data.DefaultView;
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
            r[ColumnName.TimeSlot] = Registrar.TimeSlots[timeSlotIndex];

            currentIndex++;

        }
        #endregion

        Registrar.WriteBook(sortedTable, "SpecialBook");

        Registrar.WriteEmailList(sortedTable, "SpecialEmailList");

        "   Registering - Done".LogInfo();
    }
}