using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Text;

namespace Agora.Forms
{
    [ExcludeFromCodeCoverage]
    public class DLSButton_NamedIcon : Button
    {
        [Category("Agora")]
        public DLSIcon.eDLSIcon Icon { get; set; }

        [Category("Agora")]
        public float IconSize { get; set; } = 16;

        [Category("Agora")]
        public string IconText { get; set; } = String.Empty;

        [Category("Agora")]
        public bool Selected
        {
            get { return _selected; }
            set
            {
                if (value != _selected)
                {
                    _selected = value;
                    SelectedChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private bool _selected;

        public event EventHandler? SelectedChanged;

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

        public DLSButton_NamedIcon()
        {
            InitializeComponent();

            Paint += DLSButton_Paint;

            IconFont = new Font(_font, 24);
            IconTextFont = new Font("Segoe UI", 10, FontStyle.Bold);
            SelectedBackgroundBrush = new SolidBrush(Agora.Forms.Colors.BG_Grey01);
        }

        Font IconTextFont;
        Font IconFont;
        Brush SelectedBackgroundBrush;

        private void DLSButton_Paint(object? sender, PaintEventArgs e)
        {
            if (sender == null ||
                e == null ||
                e.Graphics == null) return;

            var btn = (DLSButton_NamedIcon)sender;
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            using (var drawBrush = new SolidBrush(Selected ? Agora.Forms.Colors.Action_VoxBlue : btn.ForeColor))
            {
                // adjust the rectangle just a small amount so that it aligns with the text underneath
                RectangleF rf = e.ClipRectangle;

                if (Selected)
                    e.Graphics.FillRectangle(SelectedBackgroundBrush, new RectangleF(0, 0, Size.Width, Size.Height));
                e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                e.Graphics.TextContrast = 10;
                e.Graphics.DrawString(Char.ToString((char)Icon), btn.IconFont, drawBrush, rf, sf);

                rf.Y += 24;
                e.Graphics.DrawString(btn.IconText, btn.IconTextFont, drawBrush, rf, sf);
                sf.Dispose();
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.TabStop = false;
            this.EnabledChanged += new System.EventHandler(this.DLSButton_EnabledChanged);
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
