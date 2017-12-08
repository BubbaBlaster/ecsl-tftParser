using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using addresses;
using Asteria.Utilities;

namespace TFT
{
    public partial class Form1 : Form
    {
        Observable<bool> _bGridChanged = Observable<bool>.Get("GridChange");
        ObservableString _currentOperation = ObservableString.Get("CurrentOperation");
        Database DB;
        System.Timers.Timer sw = new System.Timers.Timer();
       
        public Form1()
        {
            InitializeComponent();
            sw.AutoReset = false;
            sw.Interval = 1500;
            sw.Elapsed += Sw_Elapsed;
            sw.Start();

            DB = new Database();

            rtbProgress.Multiline = true;
            _currentOperation.PropertyChanged += _currentOperation_PropertyChanged;
            _bGridChanged.PropertyChanged += _bGridChanged_PropertyChanged;
        }

        private void _currentOperation_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            rtbProgress.Text += _currentOperation.Value + Environment.NewLine;
            rtbProgress.SelectionStart = rtbProgress.Text.Length;
            rtbProgress.ScrollToCaret();
            rtbProgress.Update();
        }

        private void _bGridChanged_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            dataGridView1.DataSource = DB.Data;
            dataGridView1.DoubleBuffered(true);
            dataGridView1.Update();
        }

        private void Sw_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            DB.Initialize();

            _bGridChanged.Value = !_bGridChanged.Value;
        }

        private void OnDGChange()
        { 
            dataGridView1.DataSource = DB.Data;
            dataGridView1.DoubleBuffered(true);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        AddressParser ap = new addresses.AddressParser();

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            var row = dataGridView1.Rows[e.RowIndex];
            var RowKey = row.Cells[ColumnName.ControlNumber].Value;
            var DBRow = DB.Data.Rows.Find(RowKey);
            string cName = dataGridView1.Columns[e.ColumnIndex].Name;
            DBRow[cName] = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;

            dataGridView1.Update();

            UpdateTitleBar();            
        }

        private void UpdateTitleBar()
        {
            int numApprovedChildren = 0, numApprovedEntries = 0,
                numPendingChildren = 0, numPendingEntries = 0,
                numRejectedChildren = 0, numRejectedEntries = 0,
                numDuplicateChildren = 0, numDuplicateEntries = 0,
                numOtherChildren = 0, numOtherEntries = 0;
            foreach (DataRow r in DB.Data.Rows)
            {
                switch (((string)r[ColumnName.Status]))
                {
                    case "Pending":
                        numPendingChildren += Int32.Parse((string)r[ColumnName.Total]);
                        numPendingEntries++;
                        break;
                    case "Approved":
                        numApprovedChildren += Int32.Parse((string)r[ColumnName.Total]);
                        numApprovedEntries++;
                        break;
                    case "Rejected":
                        numRejectedChildren += Int32.Parse((string)r[ColumnName.Total]);
                        numRejectedEntries++;
                        break;
                    case "Duplicate":
                        numDuplicateChildren += Int32.Parse((string)r[ColumnName.Total]);
                        numDuplicateEntries++;
                        break;
                    default:
                        numOtherChildren += Int32.Parse((string)r[ColumnName.Total]);
                        numOtherEntries++;
                        break;
                }
            }

            Text = "Pending (" + numPendingEntries + "/" + numPendingChildren + ") : " +
                   "Approved (" + numApprovedEntries + "/" + numApprovedChildren + ") : " +
                   "Rejected (" + numRejectedEntries + "/" + numRejectedChildren + ") : " +
                   "Duplicate (" + numDuplicateEntries + "/" + numDuplicateChildren + ") : " +
                   "Other (" + numOtherEntries + "/" + numOtherChildren + ")";
        }

        private void computeSectionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Breakout at 'A-" + DB._strBreak1End + "' --- '" + 
                DB._strBreak2Begin + "-" + DB._strBreak2End + "' --- '" +
                DB._strBreak3Begin +"-" + DB._strBreak3End + "' --- '" + 
                DB._strBreak4Begin + "-Z'");
        }

        private void noShowsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var ns = new NoShowProcessor())
            {
                ns.ShowDialog();
            }
        }

        private void top5OrganizationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var d = new Top5Analysis(DB.Data))
            {
                d.ShowDialog();
            }
        }

        private void LoadMenuItem(object sender, EventArgs e)
        {
            //PostDB.ReadRawData("PData/Book1.csv");
            //PostDB.Merge();
            //PostDB.ReadRawData("PData/Book2.csv");
            //PostDB.Merge();
            //PostDB.ReadRawData("PData/Book3.csv");
            //PostDB.Merge();
            //PostDB.ReadRawData("PData/Book4.csv");
            //PostDB.Merge();
            //PostDB.ReadRawData("PData/Special.csv");
            //PostDB.Merge();

            int totalKidsRegistered = 0;
            int totalProjectSmileKidsRegistered = 0;
            foreach (DataRow dr in DB.Data.Rows)
            {
                for (int i = 0; i < 10; i++)
                {
                    string columnName = "R" + i.ToString();
                    if (!string.IsNullOrEmpty((string)dr[columnName]))
                    {
                        totalKidsRegistered += 1;
                        if (string.Equals((string)dr[ColumnName.Organization], "Project Smile"))
                            totalProjectSmileKidsRegistered++;
                    }                    
                }
            }

            StreamReader sr = new StreamReader("PData/NoShow.csv");

            string line;
            while( !sr.EndOfStream )
            {
                line = sr.ReadLine();
                DataRow dr = DB.Data.Rows.Find(line);
                if (dr != null)
                    DB.Data.Rows.Remove(dr);
            }
            sr.Close();

            int totalKids = 0;
            int totalPSKids = 0;
            int totalFamilies = DB.Data.Rows.Count;
            foreach(DataRow dr in DB.Data.Rows)
            {
                for (int i = 0; i < 10; i++)
                {
                    string columnName = "R" + i.ToString();
                    if (!string.IsNullOrEmpty((string)dr[columnName]))
                    {
                        totalKids += 1;
                        if (string.Equals((string)dr[ColumnName.Organization], "Project Smile"))
                            totalPSKids++;
                    }
                }
            }

            StreamWriter sw = new StreamWriter("PData/Results.txt");
            sw.WriteLine("Total Num of Kids Served = " + totalKids);
            sw.WriteLine("Total Num of Families = " + totalFamilies);
            sw.WriteLine("Total Kids Registered = " + totalKidsRegistered);
            sw.WriteLine("Total NoShows = " + (totalKidsRegistered - totalKids));
            sw.WriteLine("\nProjectSmile:");
            sw.WriteLine("Total Num of Kids Registered = " + totalProjectSmileKidsRegistered);
            sw.WriteLine("Total Num of Kids Served = " + totalPSKids);

                sw.Close();

            this.ResumeLayout();
            dataGridView1.Update();
            UpdateTitleBar();
        }
    }
}

public static class ExtensionMethods
{
    public static void DoubleBuffered(this DataGridView dgv, bool setting)
    {
        Type dgvType = dgv.GetType();
        PropertyInfo pi = dgvType.GetProperty("DoubleBuffered",
            BindingFlags.Instance | BindingFlags.NonPublic);
        pi.SetValue(dgv, setting, null);
    }
}
