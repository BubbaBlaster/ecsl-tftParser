using Agora.Utilities;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
    public class DLSTextBox_Observing : TextBox
    {
        /// ObservableString to observe
        [Category("Agora"),
            Description("The name of the ObservableString")]
        public string ObservableString_Name { get; set; } = String.Empty;

        ObservableString _oString = new ObservableString();

        /// Constructor
        public DLSTextBox_Observing()
        {
            Layout += Observing_Layout;
            this.TextChanged += Observing_TextChanged;
            Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        }

        private void DLSTextBox_Observing_Layout(object? sender, LayoutEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        private void Observing_TextChanged(object? sender, System.EventArgs e)
        {
            if (_oString != null && Text != _oString.Value)
                _oString.Value = Text;
        }

        private void Observing_Layout(object? sender, LayoutEventArgs e)
        {
            if( !DesignMode && !string.IsNullOrEmpty(ObservableString_Name))
            {
                _oString = ObservableString.Get(ObservableString_Name);
                _oString.PropertyChanged += Observing_PropertyChanged;
                if (string.IsNullOrEmpty(_oString.Value))
                    _oString.Value = Text;
                else
                    Text = _oString.Value;

                Layout -= Observing_Layout;
            }
        }

        private void Observing_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender == null) return;
            Text = ((ObservableString)sender).Value;
        }
    }
}
