using Agora.Utilities;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

namespace Agora.Forms
{
    /// <summary>
    /// An Observing labels allows you to specify an Agora.Utilities.ObservableString to
    /// always be in sync with.  When adding the control to a Form, just set 'Tag' to the name
    /// of the ObservableString that you would like it to be in sync with.
    /// 
    /// Note, that after the Tag is set and running, it cannot be changed to another ObservableString
    /// programmatically.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ObservingLabel : Label
    {
        /// ObservableString to observe
        [Category("Agora"),
            Description("The name of the ObservableString")]
        public string ObservableString_Name { get; set; } = String.Empty;

        /// Constructor
        public ObservingLabel()
        {
            Paint += ObservingLabel_Paint;
            Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        }

        private void ObservingLabel_Paint(object? sender, PaintEventArgs e)
        {
            if( !DesignMode && !string.IsNullOrEmpty(ObservableString_Name))
            {
                var str = ObservableString.Get(ObservableString_Name);
                str.PropertyChanged += ObservingLabel_PropertyChanged;
                Text = str.Value;
                Paint -= ObservingLabel_Paint;
            }
        }

        private void ObservingLabel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender == null) return;
            Text = ((ObservableString)sender).Value;
        }
    }
}
