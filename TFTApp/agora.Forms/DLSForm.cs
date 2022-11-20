using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Agora.Forms
{
    /// <summary>
    /// Agora Form with the Agora Logo.
    /// 
    /// Does not move well on the screen...
    /// Use the VertialTabControl to provide a tabbed page experience similar to a DELFI DLS SPA.
    /// </summary>
    public class DLSForm : Form
    {
        Point pntInnerFrameTopLeftPoint = new Point(100, 75);
        Label labelTitle;
        Button CloseButton, MaximizeButton, MinimizeButton;

    #region MinMaxCloseButtons
    public bool CloseButtonActive {
            get 
            {
                return CloseButton.Visible;
            }
            set
            {
                CloseButton.Enabled = value;
                CloseButton.Visible = value;
                UpdateButtons();
            }
        }

        public bool MinimizeButtonActive
        {
            get
            {
                return MinimizeButton.Visible;
            }
            set
            {
                MinimizeButton.Enabled = value;
                MinimizeButton.Visible = value;
                UpdateButtons();
            }
        }

        public bool MaximizeButtonActive
        {
            get
            {
                return MaximizeButton.Visible;
            }
            set
            {
                MaximizeButton.Enabled = value;
                MaximizeButton.Visible = value;
                UpdateButtons();
            }
        }

        private void UpdateButtons()
        {
            int position = 0;
            if( CloseButton.Enabled )
                CloseButton.Location = new Point(Size.Width - 40 * ++position, 0);            
            if (MaximizeButton.Enabled)
                MaximizeButton.Location = new Point(Size.Width - 40 * ++position, 0);
            if (MinimizeButton.Enabled)
                MinimizeButton.Location = new Point(Size.Width - 40 * ++position, 0);
        }
        #endregion

        public DLSForm()
        {
            SuspendLayout();

            MouseDown += DLSForm_MouseDown;

            BackColor = Colors.BG_Grey03;
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Name = "DLSMainForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Font = new System.Drawing.Font("Segoe UI", 11f);

            Controls.Add(new Label
            {
                Text = "Agora",
                Font = new System.Drawing.Font("Microsoft Yi Baiti", 26f, FontStyle.Regular, GraphicsUnit.Point, ((byte)(1))),
                ForeColor = Agora.Forms.Colors.LM_Grey07,
                Location = new Point(55, 0),
                AutoSize = true
            });

            Controls.Add(labelTitle = new Label
            {
                Text = "Form.Text",
                Font = new System.Drawing.Font("Segoe UI", 12.5f, FontStyle.Bold, GraphicsUnit.Point, ((byte)(1))),
                ForeColor = Agora.Forms.Colors.LM_Grey07,
                Location = new Point(67, 37),
                AutoSize = true
            });

            double scale = 0.5f;
            Controls.Add(new PictureBox
            {
                Image = Agora.Forms.Properties.Resources.Agora_logo_gold,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Size = new Size((int)(121 * scale), (int)(116 * scale)),
                Location = new Point(5, 5)
            });

            CloseButton = new Button
            {
                Font = new System.Drawing.Font("Marlett", 10f, FontStyle.Regular, GraphicsUnit.Point, ((byte)(1))),
                ForeColor = Color.White,
                Size = new Size(40, 30),
                Location = new Point(this.Size.Width-30, 0),
                Text = '\u0072'.ToString(),
                FlatStyle = FlatStyle.Flat,
                TabStop = false,
                TabIndex = 0
            };
            CloseButton.FlatAppearance.BorderSize = 0;
            CloseButton.FlatAppearance.BorderColor = Color.FromArgb(0, 255, 255, 255); //transparent
            CloseButton.Click += FormLabel_DoubleClick;
            Controls.Add(CloseButton);

            MaximizeButton = new Button
            {
                Font = new System.Drawing.Font("Marlett", 10f, FontStyle.Regular, GraphicsUnit.Point, ((byte)(1))),
                ForeColor = Color.White,
                Size = new Size(40, 30),
                Location = new Point(this.Size.Width - 60, 0),
                Text = '\u0031'.ToString(),
                FlatStyle = FlatStyle.Flat,
                TabStop = false,
                TabIndex = 0
            };
            MaximizeButton.FlatAppearance.BorderSize = 0;
            MaximizeButton.FlatAppearance.BorderColor = Color.FromArgb(0, 255, 255, 255); //transparent
            MaximizeButton.Click += MaximizeButton_Click;
            Controls.Add(MaximizeButton);

            MinimizeButton = new Button
            {
                Font = new System.Drawing.Font("Marlett", 10f, FontStyle.Regular, GraphicsUnit.Point, ((byte)(1))),
                ForeColor = Color.White,
                Size = new Size(40, 30),
                Location = new Point(this.Size.Width - 90, 0),
                Text = '\u0030'.ToString(),
                FlatStyle = FlatStyle.Flat,
                TabStop = false
            };
            MinimizeButton.FlatAppearance.BorderSize = 0;
            MinimizeButton.FlatAppearance.BorderColor = Color.FromArgb(0, 255, 255, 255); //transparent
            MinimizeButton.Click += MinimizeButton_Click; ;
            Controls.Add(MinimizeButton);

            ResumeLayout();

            this.Resize += DLSForm_Resize;
        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        private void DLSForm_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void MinimizeButton_Click(object? sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void MaximizeButton_Click(object? sender, EventArgs e)
        {
            if(WindowState == FormWindowState.Maximized)
            {
                WindowState = FormWindowState.Normal;
                MaximizeButton.Text = '\u0031'.ToString();
            }
            else
            {
                WindowState = FormWindowState.Maximized;
                MaximizeButton.Text = '\u0032'.ToString();
            }
        }

        private void DLSForm_Resize(object? sender, EventArgs e)
        {
            UpdateButtons();
        }

        private void FormLabel_DoubleClick(object? sender, EventArgs e)
        {
            Close();
        }

        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                labelTitle.Text = value;
                base.Text = value;
            }
        }
    }
}
