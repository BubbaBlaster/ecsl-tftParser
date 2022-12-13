using System.Data;
using System.Text;

namespace addresses;

internal class StatisticsAnalyzer
{
    public int _totalBoys_0_2 = 0,
               _totalBoys_3_6 = 0,
               _totalBoys_7_11 = 0,
               _totalBoys_12_16 = 0,
               _totalBoys_17 = 0,
               _totalGirls_0_2 = 0,
               _totalGirls_3_6 = 0,
               _totalGirls_7_11 = 0,
               _totalGirls_12_16 = 0,
               _totalGirls_17 = 0;
    public int
            _totalKids = 0,
            _totalReg = 0;
    public int[,] _boysByTimeSlot = new int[8, 5], 
                  _girlsByTimeSlot = new int[8, 5];
    public string _strBreak1End = string.Empty;
    public string _strBreak2Begin = string.Empty;
    public string _strBreak2End = string.Empty;
    public string _strBreak3Begin = string.Empty;
    public string _strBreak3End = string.Empty;
    public string _strBreak4Begin = string.Empty;

    public void Run()
    {
        "Computing BreakOut".LogInfo();
        var dB = DataManager.Instance.CurrentYearDB!;

        var entries = from myRow in dB.Data.AsEnumerable()
                      orderby myRow.Field<string>(ColumnName.ContactLast)
                      select myRow;
        // Compute Totals
        _totalBoys_0_2 = 0;
        _totalBoys_3_6 = 0;
        _totalBoys_7_11 = 0;
        _totalBoys_12_16 = 0;
        _totalBoys_17 = 0;
        _totalGirls_0_2 = 0;
        _totalGirls_3_6 = 0;
        _totalGirls_7_11 = 0;
        _totalGirls_12_16 = 0;
        _totalGirls_17 = 0;
        _totalKids = 0;

        int index = 0;
        foreach (var e in entries)
        {
            index++;
            int totalOnLine = Int32.Parse(e.Field<string>(ColumnName.Total));
            _totalReg++;

            int countTFT;
            int countOnLine = 0;
            int.TryParse((string)e[ColumnName.TimeSlotIndex], out int timeSlot);
            CountKids(e, Gender.Male, 0, 2, out countTFT);
            countOnLine += countTFT;
            _totalBoys_0_2 += countOnLine = countTFT;
            _boysByTimeSlot[timeSlot, 0] += countTFT;
            CountKids(e, Gender.Male, 3, 6, out countTFT);
            countOnLine += countTFT;
            _totalBoys_3_6 += countTFT;
            _boysByTimeSlot[timeSlot, 1] += countTFT;
            CountKids(e, Gender.Male, 7, 10, out countTFT);
            countOnLine += countTFT;
            _totalBoys_7_11 += countTFT;
            _boysByTimeSlot[timeSlot, 2] += countTFT;
            CountKids(e, Gender.Male, 11, 16, out countTFT);
            countOnLine += countTFT;
            _totalBoys_12_16 += countTFT;
            _boysByTimeSlot[timeSlot, 3] += countTFT;
            CountKids(e, Gender.Male, 17, 18, out countTFT);
            countOnLine += countTFT;
            _totalBoys_17 += countTFT;
            _boysByTimeSlot[timeSlot, 4] += countTFT;
            CountKids(e, Gender.Female, 0, 2, out countTFT);
            countOnLine += countTFT;
            _totalGirls_0_2 += countTFT;
            _girlsByTimeSlot[timeSlot, 0] += countTFT;
            CountKids(e, Gender.Female, 3, 6, out countTFT);
            countOnLine += countTFT;
            _totalGirls_3_6 += countTFT;
            _girlsByTimeSlot[timeSlot, 1] += countTFT;
            CountKids(e, Gender.Female, 7, 10, out countTFT);
            countOnLine += countTFT;
            _totalGirls_7_11 += countTFT;
            _girlsByTimeSlot[timeSlot, 2] += countTFT;
            CountKids(e, Gender.Female, 11, 16, out countTFT);
            countOnLine += countTFT;
            _totalGirls_12_16 += countTFT;
            _girlsByTimeSlot[timeSlot, 3] += countTFT;
            CountKids(e, Gender.Female, 17, 18, out countTFT);
            countOnLine += countTFT;
            _girlsByTimeSlot[timeSlot, 4] += countTFT;
            _totalGirls_17 += countTFT;

            if (countOnLine != totalOnLine)
                $"Line {index} (cn = {(string)e[ColumnName.ControlNumber]}) - total ({totalOnLine}) != count of kids ({countOnLine})".LogWarn();
        }
        _totalKids +=
            _totalBoys_0_2 +
            _totalBoys_3_6 +
            _totalBoys_7_11 +
            _totalBoys_12_16 +
            _totalBoys_17 +
            _totalGirls_0_2 +
            _totalGirls_3_6 +
            _totalGirls_7_11 +
            _totalGirls_12_16 +
            _totalGirls_17;

        StreamWriter sw = new(DataManager.Instance.OutputDir + "/RegistrationStatistics.csv");

        sw.WriteLine("Total_Registrations," + dB.Data.Rows.Count);
        sw.WriteLine("Total Accepted," + entries.Count());
        sw.WriteLine();

        string outStr =
            "Gender,'0-2,'3-6,'7-11,'12-16,'17" + Environment.NewLine +
            "Boys," + (_totalBoys_0_2) + ',' +
                      (_totalBoys_3_6) + ',' +
                      (_totalBoys_7_11) + ',' +
                      (_totalBoys_12_16) + ',' +
                      (_totalBoys_17) + Environment.NewLine +
            "Girls," + (_totalGirls_0_2) + ',' +
                      (_totalGirls_3_6) + ',' +
                      (_totalGirls_7_11) + ',' +
                      (_totalGirls_12_16) + ',' +
                      (_totalGirls_17) + Environment.NewLine + Environment.NewLine +
            "Total," + (_totalGirls_0_2 + _totalBoys_0_2) + ',' +
                       (_totalGirls_3_6 + _totalBoys_3_6) + ',' +
                       (_totalGirls_7_11 + _totalBoys_7_11) + ',' +
                       (_totalGirls_12_16 + _totalBoys_7_11) + ',' +
                       (_totalGirls_17 + _totalBoys_17);

        sw.WriteLine(outStr);
        sw.WriteLine("Total Kids by Group," + _totalKids);
        sw.WriteLine("Total Kids," + _totalKids);
        sw.WriteLine();

        sw.WriteLine("TimeSlot,Cars,'0-2 (b|g),'3-6 (b|g),'7-11 (b|g),'12-16 (b|g),'17 (b|g),Total (b|g|*)");
        int[] regByTimeSlot = new int[8];
        regByTimeSlot[0] = (from r in dB.Data.AsEnumerable() where (string)r[ColumnName.TimeSlotIndex] == "0" select r).Count();
        regByTimeSlot[1] = (from r in dB.Data.AsEnumerable() where (string)r[ColumnName.TimeSlotIndex] == "1" select r).Count();
        regByTimeSlot[2] = (from r in dB.Data.AsEnumerable() where (string)r[ColumnName.TimeSlotIndex] == "2" select r).Count();
        regByTimeSlot[3] = (from r in dB.Data.AsEnumerable() where (string)r[ColumnName.TimeSlotIndex] == "3" select r).Count();
        regByTimeSlot[4] = (from r in dB.Data.AsEnumerable() where (string)r[ColumnName.TimeSlotIndex] == "4" select r).Count();
        regByTimeSlot[5] = (from r in dB.Data.AsEnumerable() where (string)r[ColumnName.TimeSlotIndex] == "5" select r).Count();
        regByTimeSlot[6] = (from r in dB.Data.AsEnumerable() where (string)r[ColumnName.TimeSlotIndex] == "6" select r).Count();
        regByTimeSlot[7] = (from r in dB.Data.AsEnumerable() where (string)r[ColumnName.TimeSlotIndex] == "7" select r).Count();
        for (int i = 0; i < 8; i++)
        {
            sw.Write(Registrar.TimeSlots[i] + ',');
            sw.Write(regByTimeSlot[i].ToString() + ',');
            int totalBoys = 0, totalGirls = 0;
            for (int j = 0; j < 5; j++)
            {
                sw.Write($"({_boysByTimeSlot[i, j]}|{_girlsByTimeSlot[i, j]}),");
                totalBoys += _boysByTimeSlot[i, j];
                totalGirls += _girlsByTimeSlot[i, j];
            }
            sw.WriteLine($"({totalBoys}|{totalGirls}|{totalBoys + totalGirls})");
        }

        sw.Close();

        int n = 0;
        int state = 0;
        _strBreak2Begin = _strBreak1End = _strBreak2End = _strBreak3Begin = _strBreak3End = _strBreak4Begin = string.Empty;
        foreach (var e in entries)
        {
            n += Int32.Parse(e.Field<string>(ColumnName.Total));
            string strLastName = (string)e[ColumnName.ContactLast];
            switch (state)
            {
                case 0:
                    if (state == 0 && n > _totalKids / 4)
                    {
                        _strBreak1End = strLastName;
                        state++;
                    }
                    break;
                case 1:
                    if (string.IsNullOrEmpty(_strBreak2Begin) &&
                        _strBreak1End.CompareTo(strLastName) != 0)
                    {
                        _strBreak2Begin = strLastName;
                        state++;
                    }
                    break;
                case 2:
                    if (n > 2 * _totalKids / 4)
                    {
                        _strBreak2End = strLastName;
                        state++;
                    }
                    break;
                case 3:
                    if (string.IsNullOrEmpty(_strBreak3Begin) &&
                        _strBreak2End.CompareTo(strLastName) != 0)
                    {
                        _strBreak3Begin = strLastName;
                        state++;
                    }
                    break;
                case 4:
                    if (n > 3 * _totalKids / 4)
                    {
                        _strBreak3End = strLastName;
                        state++;
                    }
                    break;
                case 5:
                    if (string.IsNullOrEmpty(_strBreak4Begin) &&
                        _strBreak3End.CompareTo(strLastName) != 0)
                    {
                        _strBreak4Begin = strLastName;
                        state++;
                    }
                    break;
                case 6:
                    StringBuilder sb = new StringBuilder();
                    sb.Append("     " + "A - " + _strBreak1End + Environment.NewLine);
                    sb.Append("     " + _strBreak2Begin + " - " + _strBreak2End + Environment.NewLine);
                    sb.Append("     " + _strBreak3Begin + " - " + _strBreak3End + Environment.NewLine);
                    sb.Append("     " + _strBreak4Begin + " - Zzz");

                    StreamWriter swOut = new(DataManager.Instance.OutputDir + "/Breakout.txt");
                    swOut.WriteLine(sb.ToString());
                    swOut.Close();
                    sb.ToString().LogInfo();
                    "Computing BreakOut - Done".LogInfo();
                    return;
            }
        }
    }

    static void CountKids(DataRow e, Gender gender, int ageBegin, int ageEnd, out int countTFT)
    {
        countTFT = 0;
        for (int i = 1; i <= 10; i++)
        {
            string colNameAge = "ChildAge" + i;
            string colNameGender = "ChildGender" + i;
            if (!(e[colNameAge] is System.DBNull) &&
                !(e[colNameGender] is System.DBNull) &&
                ((string)e[colNameGender]).Length > 0)
            {
                char firstChar = ((string)e[colNameGender])[0];
                if ((gender == Gender.Male && firstChar == 'M') ||
                    (gender == Gender.Female && firstChar == 'F'))
                {
                    int age = -1;
                    if (int.TryParse((string)e[colNameAge], out age))
                    {
                        if (age >= ageBegin && age <= ageEnd)
                        {
                            countTFT++;
                        }
                    }
                }
            }
        }
    }
}
