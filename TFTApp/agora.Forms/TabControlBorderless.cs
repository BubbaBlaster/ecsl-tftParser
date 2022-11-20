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
    /// Deprecated.
    /// 
    /// The purpose of this control was to allow navigation in WinForms similar to DELFI.  Use the Vertical Tab Control instead.
    /// </summary>
    public class TabControlBorderless : TabControl
    {
        private const int TCM_ADJUSTRECT = 0x1328;

        protected override void WndProc(ref Message m)
        {
            // Hide the tab headers at run-time
            if (m.Msg == TCM_ADJUSTRECT && !DesignMode)
            {
                m.Result = (IntPtr)1;
                return;
            }

            // call the base class implementation
            base.WndProc(ref m);
        }
        private List<Panel> pages = new List<Panel>();

        public void MakeTransparent()
        {
            if (TabCount == 0) throw new InvalidOperationException();
            var height = Size.Height;
            // Move controls to panels
            for (int tab = 0; tab < TabCount; ++tab)
            {
                var page = new Panel
                {
                    Location = this.Location,
                    Size = this.Size,
                    BackColor = Color.Transparent,
                    Visible = tab == this.SelectedIndex
                };
                for (int ix = TabPages[tab].Controls.Count - 1; ix >= 0; --ix)
                {
                    TabPages[tab].Controls[ix].Parent = page;
                }
                pages.Add(page);
                this.Parent.Controls.Add(page);
            }
            this.Height = height /* + 1 */;
            Visible = false;
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            SuspendLayout();
            base.OnSelectedIndexChanged(e);
            for (int tab = 0; tab < pages.Count; ++tab)
            {
                pages[tab].Visible = tab == SelectedIndex;
            }
            ResumeLayout();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) foreach (var page in pages) page.Dispose();
            base.Dispose(disposing);
        }

    }
}
