using System;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace Agora.Forms
{
    /// <summary>
    /// This control allows is a Tab Control with the tabs on the left instead of on the top.  It allows you to set Icons on each tab.  
    /// Use the ImageIndex on each tab page to select the Icon, which are set in a collection via the Visual Studio Properties page 
    /// in Agora Category. (select the Category view to sort the properties by category)
    /// 
    /// DLS Icons are found here: https://dls.swt.slb.com/components/icon/  (Most are available)
    /// </summary>
    public class VerticalTabControl : TabControl
    {
        [Browsable(true)]
        [Category("Agora")]
        public DLSIcon.eDLSIcon[] Icons { get; set; } = new DLSIcon.eDLSIcon[0];

        /// Returns a FontFamily containing the DLSIcons
        static private FontFamily _font
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

        public VerticalTabControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer,
                true);
            DoubleBuffered = true;
            SizeMode = TabSizeMode.Fixed;
            ItemSize = new Size(100, 200);
            _FontSelected = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            _FontUnselected = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
        }

        ~VerticalTabControl()
        {
            _BorderPen?.Dispose();
            _SelectedBrush?.Dispose();
            _UnSelectedBrush?.Dispose();
            _IconFont?.Dispose();
            _pfc?.Dispose();
            _FontSelected?.Dispose();
            _FontUnselected?.Dispose();
            _stringFormat?.Dispose();
        }

        Font _FontSelected, _FontUnselected;
        StringFormat _stringFormat = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };

        protected override void CreateHandle()
        {
            base.CreateHandle();
            Alignment = TabAlignment.Left;
        }

        Font? _IconFont;
        float IconSize = 18f;

        Pen _BorderPen = new Pen(Color.FromArgb(170, 187, 204));
        Brush _SelectedBrush = new SolidBrush(Colors.BG_Grey04),
              _UnSelectedBrush = new SolidBrush(Colors.BG_Grey03);

        protected override void OnPaint(PaintEventArgs e)
        {
            var B = new Bitmap(Width, Height);
            var G = Graphics.FromImage(B);
            G.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            G.TextContrast = 1;

            if (_IconFont == null)
                _IconFont = new Font(_font, IconSize == 0 ? 50.0f : IconSize);

            var tab = SelectedTab;
            if (tab != null)
                tab.BackColor = Parent.BackColor;// Color.White;

            G.Clear(Parent.BackColor);

            // This line fills the full area behind the tab headings
            // G.FillRectangle(ToBrush(Color.FromArgb(246, 248, 252)), new Rectangle(0, 0, ItemSize.Height + 4, Height));

            // to get rid of the borders around each heading
            G.DrawLine(_BorderPen, new Point(Width - 1, 0), new Point(Width - 1, Height - 1));
            G.DrawLine(_BorderPen, new Point(ItemSize.Height + 1, 0), new Point(Width - 1, 0));
            G.DrawLine(_BorderPen, new Point(ItemSize.Height + 3, Height - 1), new Point(Width - 1, Height - 1));

            // draws the line right of the tabs
            G.DrawLine(_BorderPen,
                new Point(ItemSize.Height + 3, 0),
                new Point(ItemSize.Height + 3, 999));            

            for (int i = 0; i < TabCount; i++)
            {
                var x2 = new Rectangle(new Point(GetTabRect(i).Location.X - 2, GetTabRect(i).Location.Y - 2),
                    new Size(GetTabRect(i).Width + 3, GetTabRect(i).Height - 15));                

                if (i == SelectedIndex)
                {
                    G.FillRectangle(i == SelectedIndex ? _SelectedBrush : _UnSelectedBrush, x2);

                    // draw the outline around the selected tab
                    G.DrawRectangle(_BorderPen, x2);

                    G.SmoothingMode = SmoothingMode.HighQuality;
                    var p = new Point[] {
                        new Point(ItemSize.Height - 3, GetTabRect(i).Location.Y + 20),
                        new Point(ItemSize.Height + 4, GetTabRect(i).Location.Y + 14),
                        new Point(ItemSize.Height + 4, GetTabRect(i).Location.Y + 27)
                    };
                    G.FillPolygon(Brushes.White, p);
                    G.DrawPolygon(_BorderPen, p);

                    if (Icons != null)
                    {
                        try
                        {
                            int index = TabPages[i].ImageIndex;
                            if (index >= 0 && index < Icons.Length)
                            {
                                var rectIcon = new Rectangle(x2.X, x2.Y + 5, x2.Width, (int)(x2.Height / 2.0f));
                                var rectName = new Rectangle(x2.X, (int)(x2.Y + x2.Height / 2.0f), x2.Width, (int)(x2.Height / 2.0f));
                                //G.DrawRectangle(ToPen(Colors.Alert_Red), rectIcon);
                                //G.DrawRectangle(ToPen(Colors.Action_VoxBlue), rectName);
                                G.DrawString(Char.ToString((char)Icons[index]), _IconFont, Brushes.White, rectIcon,
                                    _stringFormat);
                                G.DrawString(TabPages[i].Text, _FontSelected, Brushes.White, rectName,
                                    _stringFormat);
                            }
                            else
                                G.DrawString(TabPages[i].Text, _FontSelected, Brushes.White, x2, _stringFormat);
                        }
                        catch (Exception)
                        {
                            G.DrawString(TabPages[i].Text, _FontSelected, Brushes.White, x2, _stringFormat);
                        }
                    }
                    else
                        G.DrawString(TabPages[i].Text, _FontSelected, Brushes.White, x2, _stringFormat);

                    G.DrawLine(_BorderPen,
                        new Point(x2.Location.X - 1, x2.Location.Y - 1),
                        new Point(x2.Location.X, x2.Location.Y));
                    G.DrawLine(_BorderPen,
                        new Point(x2.Location.X - 1, x2.Bottom - 1),
                        new Point(x2.Location.X, x2.Bottom));
                }
                else
                {
                    G.FillRectangle(_UnSelectedBrush, x2);
                    G.DrawLine(_BorderPen,
                        new Point(x2.Right, x2.Top),
                        new Point(x2.Right, x2.Bottom));
                    if (Icons != null)
                    {
                        try
                        {
                            int index = TabPages[i].ImageIndex;
                            if (index >= 0 && index < Icons.Length)
                            {
                                var rectIcon = new Rectangle(x2.X, x2.Y + 5, x2.Width, (int)(x2.Height / 2.0f));
                                var rectName = new Rectangle(x2.X, (int)(x2.Y + x2.Height / 2.0f), x2.Width, (int)(x2.Height / 2.0f));
                                G.DrawString(Char.ToString((char)Icons[index]), _IconFont, Brushes.White, rectIcon, _stringFormat);
                                G.DrawString(TabPages[i].Text, _FontUnselected, Brushes.White, rectName, _stringFormat);
                            }
                            else
                                G.DrawString(TabPages[i].Text, _FontUnselected, Brushes.White, x2, _stringFormat);
                        }
                        catch (Exception)
                        {
                            G.DrawString(TabPages[i].Text, _FontUnselected, Brushes.White, x2, _stringFormat);
                        }
                    }
                    else
                        G.DrawString(TabPages[i].Text, _FontUnselected, Brushes.White, x2, _stringFormat);
                }
            }

            Image img = (Image)B.Clone();
            e.Graphics.DrawImage(img, 0, 0);

            img.Dispose();
            G.Dispose();
            B.Dispose();
        }
    }
}
