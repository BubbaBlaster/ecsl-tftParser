using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Text;

namespace Agora.Forms
{
    [ExcludeFromCodeCoverage]
    abstract public class DLSButton_IconBase : Button
    {
        [Category("Agora")]
        public DLSIcon.eDLSIcon Icon { get; set; }

        [Category("Agora"), DefaultValue(24.0f)]
        public float IconSize { get; set; }

        [Category("Agora")]
        public bool Selected { get; set; }

        /// Returns a FontFamily containing the DLSIcons
        private FontFamily _font
        {
            get
            {
                if (_pfc == null)
                {
                    _pfc = new PrivateFontCollection();
                    int fontLength = Properties.Resources.SchlumbergerDLSIcons.Length;
                    IntPtr data = Marshal.AllocCoTaskMem(fontLength);
                    Marshal.Copy(Properties.Resources.SchlumbergerDLSIcons, 0, data, fontLength);
                    _pfc.AddMemoryFont(data, fontLength);
                }
                return _pfc.Families[0];
            }
        }
        static PrivateFontCollection? _pfc = null;

        public DLSButton_IconBase()
        {
            InitializeComponent();

            Paint += DLSButton_Paint;
            SelectedBackgroundBrush = new SolidBrush(Colors.BG_Grey01);

            UpdateStyleSettings();
        }

        Font? IconFont;
        Brush SelectedBackgroundBrush;

        private void DLSButton_Paint(object? sender, PaintEventArgs e)
        {
            if (sender == null) return;

            if( IconFont == null )
                IconFont = new Font(_font, IconSize == 0 ? 50.0f : IconSize);

            var btn = (DLSButton_Icon)sender;
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };

            using (var drawBrush = new SolidBrush(Selected ? Agora.Forms.Colors.Action_VoxBlue : btn.ForeColor))
            {
                // adjust the rectangle just a small amount so that it aligns with the text underneath
                RectangleF rf = e.ClipRectangle;

                if (Selected)
                    e.Graphics.FillRectangle(SelectedBackgroundBrush, new RectangleF(0, 0, Size.Width, Size.Height));
                e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                e.Graphics.TextContrast = 10;
                e.Graphics.DrawString(Char.ToString((char)Icon), btn?.IconFont ?? IconFont, 
                    drawBrush, rf, sf);

                sf.Dispose();
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // DLSButton
            // 
            this.TabStop = false;
            this.EnabledChanged += new System.EventHandler(DLSButton_EnabledChanged);
            this.ResumeLayout(false);
        }

        protected override bool ShowFocusCues { get { return false; } }

        private void DLSButton_EnabledChanged(object? sender, EventArgs e)
        {
            UpdateStyleSettings();
        }

        private void UpdateStyleSettings()
        {
            this.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(0, 255, 255, 255);
            this.FlatAppearance.BorderSize = 0;
            this.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            BackColor = Color.FromArgb(0, 255, 255, 255);
            Text = "";
            if (Enabled)
            {
                ForeColor = Colors.LM_White;
            }
            else
            {
                ForeColor = Colors.LM_Grey07;
            }

        }
    }
}
