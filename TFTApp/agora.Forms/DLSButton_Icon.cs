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
    public class DLSButton_Icon : DLSButton_IconBase
    {
        public DLSButton_Icon()
        {
            InitializeComponent();
        }


        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // DLSButton
            // 
            this.TabStop = false;
            this.ResumeLayout(false);
        }

    }
}
