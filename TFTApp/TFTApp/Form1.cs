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
using Agora.Utilities;
using static Agora.SDK;

namespace TFT
{
    public partial class Form1 : Form
    {
        Observable<bool> _bGridChanged = Observable<bool>.Get("GridChange");
        System.Timers.Timer sw = new System.Timers.Timer();

        public DataTable? Data = null;

        public Form1()
        {
            InitializeComponent();

            Agora.Logging.Logger.Instance.Add(new Agora.Forms.ListViewLogDisplay(listView1));
            Agora.Logging.Logger.Instance.Add(new Agora.Forms.FileLoggerTarget(@"N:\TFTGit"));

            sw.AutoReset = false;
            sw.Interval = 1500;
            sw.Elapsed += Sw_Elapsed;

            sw.Start();

            _bGridChanged.PropertyChanged += _bGridChanged_PropertyChanged;
        }

        private void _bGridChanged_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            dataGridView1.DataSource = Data;
            dataGridView1.DoubleBuffered(true);
            dataGridView1.Update();
        }

        private void Sw_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var dm = DataManager.Instance;
            dm.Initialize();
            Data = dm.CurrentYearDB.Data;

            dm.Analyze();

            dm.Register();

            dm.Special();

            _bGridChanged.Value = !_bGridChanged.Value;
        }

        private void OnDGChange()
        { 
            dataGridView1.DataSource = Data;
            dataGridView1.DoubleBuffered(true);
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void DataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            var row = dataGridView1.Rows[e.RowIndex];
            var RowKey = row.Cells[ColumnName.ControlNumber].Value;
            var DBRow = Data.Rows.Find(RowKey);
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
            foreach (DataRow r in Data.Rows)
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

        private void NoShowsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var ns = new NoShowProcessor())
            {
                ns.ShowDialog();
            }
        }

        private void Top5OrganizationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var d = new Top5Analysis(Data))
            {
                d.ShowDialog();
            }
        }

        private void LoadMenuItem(object sender, EventArgs e)
        {
            int totalKidsRegistered = 0;
            int totalProjectSmileKidsRegistered = 0;
            foreach (DataRow dr in Data.Rows)
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
                DataRow dr = Data.Rows.Find(line);
                if (dr != null)
                    Data.Rows.Remove(dr);
            }
            sr.Close();

            int totalKids = 0;
            int totalPSKids = 0;
            int totalFamilies = Data.Rows.Count;
            foreach(DataRow dr in Data.Rows)
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
