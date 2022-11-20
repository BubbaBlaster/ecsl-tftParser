using System;
using System.Windows.Forms;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace Agora.Forms
{
    [ExcludeFromCodeCoverage]
    public class DLSButton_Secondary : Button
    {
        public DLSButton_Secondary()
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
            this.FlatAppearance.BorderColor = Colors.Action_VoxBlue;
            this.FlatAppearance.BorderSize = 2;
            this.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            if (Enabled)
            {
                BackColor = Colors.Transparent;
                ForeColor = Colors.LM_White;
            }
            else
            {
                BackColor = Colors.LM_Grey05;
                ForeColor = Colors.LM_Grey07;
            }
        }
    }
}
