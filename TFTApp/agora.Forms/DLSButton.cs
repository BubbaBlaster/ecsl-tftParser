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

namespace Agora.Forms
{
    [ExcludeFromCodeCoverage]
    public class DLSButton : Button
    {
        public enum ButtonStyle
        {
            Primary = 0,
            Secondary,
            Tertiary,
            Icon
        }

        [Category("Agora")]
        [Description("The DLS button style - see http://dls.slb.com/components/button/")]
        public ButtonStyle DLSStyle {
            get { return _dlsStyle; }
            set
            {
                _dlsStyle = value;
                UpdateStyleSettings();
            }
        }

        ButtonStyle _dlsStyle = ButtonStyle.Primary;

        public DLSButton()
        {
            InitializeComponent();
            
            Paint += DLSButton_Paint;
        }

        private void DLSButton_Paint(object? sender, PaintEventArgs e)
        {
            if (sender == null) return;

            var btn = (Button)sender;
            var drawBrush = new SolidBrush(btn.ForeColor);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            // adjust the rectangle just a small amount so that it aligns with the text underneath
            RectangleF rf = e.ClipRectangle;
            rf.X -= 1;
            rf.Y += 1;

            //e.Graphics.DrawString(btn.Text, btn.Font, drawBrush, rf, sf);
            drawBrush.Dispose();
            sf.Dispose();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // DLSButton
            // 
            this.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            this.TabStop = false;
            this.EnabledChanged += new EventHandler(this.DLSButton_EnabledChanged);
            this.ResumeLayout(false);
        }

        protected override bool ShowFocusCues { get { return false; } }

        private void DLSButton_EnabledChanged(object? sender, EventArgs e)
        {
            UpdateStyleSettings();
        }

        private void UpdateStyleSettings()
        {
            switch (_dlsStyle)
            {
                case ButtonStyle.Primary:
                    this.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
                    this.FlatAppearance.BorderSize = 0;
                    this.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                    if (Enabled)
                    {
                        BackColor = Colors.Action_VossBlue;
                        ForeColor = Colors.LM_White;
                    }
                    else
                    {
                        BackColor = Colors.LM_Grey05;
                        ForeColor = Colors.LM_White;
                    }
                    break;
                case ButtonStyle.Secondary:
                    this.FlatAppearance.BorderColor = Colors.LM_Grey02;
                    this.FlatAppearance.BorderSize = 2;
                    this.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                    if (Enabled)
                    {
                        BackColor = Colors.LM_Grey08;
                        ForeColor = Colors.LM_Grey02;
                    }
                    else
                    {
                        BackColor = Colors.LM_Grey05;
                        ForeColor = Colors.LM_Grey07;
                    }
                    break;
                case ButtonStyle.Tertiary:
                    this.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(0, 255, 255, 255);
                    this.FlatAppearance.BorderSize = 0;
                    this.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                    BackColor = Color.FromArgb(0, 255, 255, 255);
                    if (Enabled)
                    {
                        ForeColor = Colors.Action_VossBlue;
                    }
                    else
                    {
                        ForeColor = Colors.LM_Grey07;
                    }
                    break;
            }
        }
    }
}
