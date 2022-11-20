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
    public partial class DLSLabel_SingleLineNamedObserving : UserControl
    {
        [Category("Agora")]
        [Description("The Agora.Utilities.ObservableString that is bound to the Value")]
        public string ObservableString_Value
        {
            get { return _value; }
            set
            {
                _value = value;
                if (DesignMode)
                    labelValue.Text = value;
            }
        }
        private string _value = String.Empty;

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

        public DLSLabel_SingleLineNamedObserving()
        {
            InitializeComponent();
            Paint += SetupObservable_Paint;
        }

        private void SetupObservable_Paint(object? sender, PaintEventArgs e)
        {
            if (!DesignMode && ObservableString_Value != null)
            {
                var str = ObservableString.Get(ObservableString_Value);
                str.PropertyChanged += Value_PropertyChanged;
                labelValue.Text = str.Value;
                Paint -= SetupObservable_Paint;
            }
        }

        private void Value_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender != null)
                labelValue.Text = ((ObservableString)sender).Value;
            Invalidate();
        }

        private void LabelValueName_Paint(object? sender, PaintEventArgs e)
        {
            SizeF szName = e.Graphics.MeasureString(labelValueName.Text, labelValueName.Font);
            SizeF szUnit = e.Graphics.MeasureString(labelUnit.Text, labelUnit.Font);
            SizeF szValue = e.Graphics.MeasureString(labelValue.Text, labelValue.Font);

            labelUnit.Location = new Point(labelValueName.Left + (int)szName.Width, labelValueName.Top);

            var contrastColor = Agora.Forms.Colors.ContrastColor(Parent.BackColor);

            labelValue.Location = new Point(labelUnit.Left + (int)szUnit.Width, 1 + labelValueName.Top + (int)szName.Height - (int)szValue.Height);
            labelValue.ForeColor = contrastColor;
            if (contrastColor == Color.White)
                labelValueName.ForeColor = labelUnit.ForeColor = Agora.Forms.Colors.BG_Grey06;
            else
                labelValueName.ForeColor = labelUnit.ForeColor = Agora.Forms.Colors.BG_Grey02;
        }
    }
}
