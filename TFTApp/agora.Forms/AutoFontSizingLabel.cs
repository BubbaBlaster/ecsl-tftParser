using Agora.Utilities;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
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
    public class AutoFontSizingLabel : Label
    {
        [Category("Agora")]
        [Description("The maximum size of the font to use for the label.")]
        public float MaximumFontSize { get; set; } = 16;

        private bool _bComputeFont = true;

        /// Constructor
        public AutoFontSizingLabel()
        {
            Paint += AutoFontSizingLabel_Paint;
            AutoSize = false;
        }

        private void AutoFontSizingLabel_Paint(object? sender, PaintEventArgs e)
        {
            if (_bComputeFont || DesignMode)
            {
                Font testFont = Font;
                for (float adjustedSize = MaximumFontSize; adjustedSize >= 6.0f; adjustedSize -= 0.1f)
                {
                    testFont = new Font(Font.Name, adjustedSize, Font.Style);
                    SizeF sizeOfTextUsingAdjustedSizeFont = e.Graphics.MeasureString(Text, testFont);

                    if (sizeOfTextUsingAdjustedSizeFont.Width < Size.Width-10) // 10 is a magic number that stops text from wrapping
                        break;
                }
                Font = testFont;
                _bComputeFont = false;
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // AutoFontSizingLabel
            // 
            this.TextChanged += new System.EventHandler(AutoFontSizingLabel_TextChanged);
            this.Resize += new System.EventHandler(AutoFontSizingLabel_Resize);
            this.ResumeLayout(false);

        }

        private void AutoFontSizingLabel_TextChanged(object? sender, System.EventArgs e)
        {
            _bComputeFont = true;
        }

        private void AutoFontSizingLabel_Resize(object? sender, System.EventArgs e)
        {
            _bComputeFont = true;
        }
    }
}
