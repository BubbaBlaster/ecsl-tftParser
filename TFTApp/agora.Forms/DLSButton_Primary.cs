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
    public class DLSButton_Primary : Button
    {
        public DLSButton_Primary()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TabStop = false;
            this.EnabledChanged += new System.EventHandler(this.DLSButton_EnabledChanged);
            UpdateStyleSettings();
            this.ResumeLayout(false);
        }

        protected override bool ShowFocusCues { get { return false; } }

        private void DLSButton_EnabledChanged(object? sender, EventArgs e)
        {
            UpdateStyleSettings();
        }

        private void UpdateStyleSettings()
        {
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
        }
    }
}
