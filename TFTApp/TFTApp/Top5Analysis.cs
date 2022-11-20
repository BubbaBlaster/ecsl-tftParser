using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TFT
{
    public partial class Top5Analysis : Form
    {
        DataTable T = new DataTable();

        public Top5Analysis(DataTable db)
        {
            InitializeComponent();

            T.Columns.Add("Organization", typeof(string));
            T.Columns.Add("Registered", typeof(int));
            T.Columns.Add("Approved", typeof(int));
            T.Columns.Add("Duplicate", typeof(int));
            T.Columns.Add("Rejected", typeof(int));
            T.Columns.Add("Pending", typeof(int));
            T.Columns.Add("Verify", typeof(int));
            T.PrimaryKey = new DataColumn[1] { T.Columns["Organization"] };

            foreach (DataRow r in db.Rows)
            {
                DataRow match = T.Rows.Find(r[addresses.ColumnName.Organization]);
                if( match == null )
                {
                    match = T.NewRow();
                    match["Organization"] = r[addresses.ColumnName.Organization];
                    match["Registered"] = 0;
                    match["Approved"] = 0;
                    match["Duplicate"] = 0;
                    match["Rejected"] = 0;
                    match["Pending"] = 0;
                    match["Verify"] = 0;
                    T.Rows.Add(match);
                }
                int total = System.Convert.ToInt32((string)r[addresses.ColumnName.Total]);
                match["Registered"] = (int)match["Registered"] + 1;
                match[(string)r[addresses.ColumnName.Status]] =
                    (int)match[(string)r[addresses.ColumnName.Status]] + 1;
            }
            dataGridView1.DataSource = T;
        }
    }
}
