using Agora.Utilities;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;

namespace Agora.Forms
{
    /// <summary>
    /// An Observing textbox allows you to specify an Agora.Utilities.ObservableString to
    /// always be in sync with.  When adding the control to a Form, just set 'ObservableString_Name' to the name
    /// of the ObservableString that you would like it to be in sync with.
    /// 
    /// Note, that after the Tag is set and running, it cannot be changed to another ObservableString
    /// programmatically.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DLSLabeledTextBox_Observing : UserControl
    {
        /// ObservableString to observe
        [Category("Agora"),
            Description("The name of the ObservableString")]
        public string ObservableString_Name
        {
            get { return _tb.ObservableString_Name; }
            set { _tb.ObservableString_Name = value; }
        }

        [Category("Agora")]
        [Description("Label above the TextBox")]
        public string Label
        {
            get { return _strLabel; }
            set
            {
                _strLabel = value;
                Invalidate();
            }
        }
        private string _strLabel = String.Empty;

        DLSTextBox_Observing _tb = new DLSTextBox_Observing();
        Label _label = new Label();
        Point top = new System.Drawing.Point(0, 0);
        Point tbOriginalLocation = new System.Drawing.Point(0, 20);

        /// Constructor
        public DLSLabeledTextBox_Observing()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _label.AutoSize = true;
            _label.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            _label.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            _label.Location = new System.Drawing.Point(4, 4);
            _label.Name = Name + ".Label";

            _tb.Width = Width;
            _tb.Location = new System.Drawing.Point(0, 20);
            _tb.Name = Name + ".TextBox";
            Controls.Add(_tb);
            Controls.Add(_label);

            Paint += DLSLabeledTextBox_Observing_Paint;
        }

        private void DLSLabeledTextBox_Observing_Paint(object? sender, PaintEventArgs e)
        {
            var contrastColor = Agora.Forms.Colors.ContrastColor(Parent.BackColor);
            if (contrastColor == Color.White)
                _label.ForeColor = Agora.Forms.Colors.BG_Grey06;
            else
                _label.ForeColor = Agora.Forms.Colors.BG_Grey02;

            _tb.Width = Width;

            if (string.IsNullOrEmpty(Label))
                _tb.Location = top;
            else 
                _tb.Location = tbOriginalLocation;
            _label.Text = Label;
        }
    }
}
