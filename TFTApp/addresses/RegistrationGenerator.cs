using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace addresses
{
    class RegistrationGenerator
    {
        public string[] Kids = new string[10];
        public int[] count = new int[10];
        public string[] R = new string[10];
        int currentRow = 0;
        string [] Category = new string[]{ "Boy 0-2",
                                           "Boy 3-6",
                                           "Boy 7-11",
                                           "Boy 12-16",
                                           "Boy 17",
                                           "Girl 0-2",
                                           "Girl 3-6",
                                           "Girl 7-11",
                                           "Girl 12-16",
                                           "Girl 17" };

        public void LoadRow(DataRow r)
        {
            Kids[0] = r.Field<string>(ColumnName.Boys_0_2_Names);
            Kids[3] = r.Field<string>(ColumnName.Boys_12_16_Names);
            Kids[4] = r.Field<string>(ColumnName.Boys_17_Names);
            Kids[1] = r.Field<string>(ColumnName.Boys_3_6_Names);
            Kids[2] = r.Field<string>(ColumnName.Boys_7_11_Names);
            Kids[5] = r.Field<string>(ColumnName.Girls_0_2_Names);
            Kids[8] = r.Field<string>(ColumnName.Girls_12_16_Names);
            Kids[9] = r.Field<string>(ColumnName.Girls_17_Names);
            Kids[6] = r.Field<string>(ColumnName.Girls_3_6_Names);
            Kids[7] = r.Field<string>(ColumnName.Girls_7_11_Names);
            count[0] = Int32.Parse(r.Field<string>(ColumnName.Boys_0_2));
            count[3] = Int32.Parse(r.Field<string>(ColumnName.Boys_12_16));
            count[4] = Int32.Parse(r.Field<string>(ColumnName.Boys_17));
            count[1] = Int32.Parse(r.Field<string>(ColumnName.Boys_3_6));
            count[2] = Int32.Parse(r.Field<string>(ColumnName.Boys_7_11));
            count[5] = Int32.Parse(r.Field<string>(ColumnName.Girls_0_2));
            count[8] = Int32.Parse(r.Field<string>(ColumnName.Girls_12_16));
            count[9] = Int32.Parse(r.Field<string>(ColumnName.Girls_17));
            count[6] = Int32.Parse(r.Field<string>(ColumnName.Girls_3_6));
            count[7] = Int32.Parse(r.Field<string>(ColumnName.Girls_7_11));

            for(int i=0; i<10; i++)
            {
                Process(count[i], Kids[i], i == 4 || i == 9, Category[i]);
            }
        }

        private void Process(int n, string s, bool bMoney, string Category)
        {
            string[] split = s.Split(new char[] { ',', ';', '-', '(', ')', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }, 
                StringSplitOptions.RemoveEmptyEntries);
            if (n != split.Length)
                R[currentRow] = "ERROR - " + s;
            else
            {
                for (int i = 0; i < split.Length; i++)
                {
                    split[i] = split[i].Trim();
                    R[currentRow] = bMoney ? "                          R        " :
                                             "B        B        G                ";
                    R[currentRow] += Category + "  -----  " + split[i];
                }
            }
        }
    }
}
