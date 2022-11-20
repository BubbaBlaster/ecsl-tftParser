using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Agora.Utilities;

namespace Agora.Forms
{
    public partial class DLSLabelWithUnit : UserControl
    {
        [Category("Agora")]
        [Description("The unit of the Value.")]
        public string Units
        {
            get { return _strUnits; }
            set
            {
                _strUnits = value;
                if (!string.IsNullOrEmpty(_strUnits) && _strUnits.Length > 0)
                    labelUnit.Text = '(' + value + ')';
                else
                    labelUnit.Text = string.Empty;
            }
        }
        private string _strUnits = String.Empty;

        [Category("Agora")]
        [Description("The Name of the Value.")]
        public string ValueName
        {
            get { return _strName; }
            set
            {
                _strName = value;
                labelValueName.Text = value;
                
            }
        }
        private string _strName = String.Empty;

        public DLSLabelWithUnit()
        {
            InitializeComponent();
        }

        private void LabelValueName_Paint(object sender, PaintEventArgs e)
        {
            SizeF szName = e.Graphics.MeasureString(labelValueName.Text, labelValueName.Font);

            labelUnit.Location = new Point(labelValueName.Left + (int)szName.Width, labelValueName.Top);
            labelValueName.ForeColor = labelUnit.ForeColor = ForeColor;            

            var contrastColor = Agora.Forms.Colors.ContrastColor(Parent.BackColor);            
        }
    }
}
