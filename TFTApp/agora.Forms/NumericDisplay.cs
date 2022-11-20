using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using Agora.Utilities;
using System.Drawing.Text;

namespace Agora.Forms
{
    public enum AlertLevel
    {
        Green = 0,
        Yellow = 1,
        Red = 2,
        Unknown = 3
    }

    public partial class NumericDisplay : UserControl
    {
        [Category("Agora")]
        [Description("The name displayed above the digital number. Ex. 'DPTH (m)'")]
        public string DisplayName { get; set; } = "Display Name";

        [Category("Agora")]
        [Description("The text displayed in the numeric portion of the display. Ex. 'N/A'")]
        public string Number { get; set; } = "Number";

        [Category("Agora")]
        [Description("The Alert Level of the display which affects its color.")]
        public AlertLevel AlertLevel { get; set; } = AlertLevel.Unknown;

        [Category("Agora")]
        [Description("The name of the ObservableString to the numerical display to.")]
        public string ObservableStringName { get; set; } = String.Empty;

        public NumericDisplay()
        {
            SuspendLayout();
            DoubleBuffered = true;
            Paint += ObservingLabel_Paint;
            Paint += NumericDisplay_Paint;
            ResumeLayout();
            _stringFormat = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };
        }

        ~NumericDisplay()
        {
            _stringFormat?.Dispose();
            _whiteBrush?.Dispose();
            _blackBrush?.Dispose();
        }

        private void ObservingLabel_Paint(object? sender, PaintEventArgs e)
        {
            if (!DesignMode && ObservableStringName != null)
            {
                var str = ObservableString.Get((string)ObservableStringName);
                str.PropertyChanged += ObservingLabel_PropertyChanged;
                Number = str.Value;
                ObservableStringName = String.Empty;
                Paint -= ObservingLabel_Paint;
            }
        }

        private void ObservingLabel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if( sender != null )
                Number = ((ObservableString)sender).Value;
            Invalidate();
        }

        private void UpdateColors()
        {
            switch (AlertLevel)
            {
                case AlertLevel.Green:
                    _backgroundColor = Colors.Alert_Green;
                    break;
                case AlertLevel.Red:
                    _backgroundColor = Colors.Alert_Red;
                    break;
                case AlertLevel.Yellow:
                    _backgroundColor = Colors.Alert_Yellow;
                    break;
                case AlertLevel.Unknown:
                    _backgroundColor = Colors.Alert_BlueGrey;
                    break;
            }
        }

        Color _backgroundColor;
        StringFormat _stringFormat;
        Brush _whiteBrush = new SolidBrush(Color.White);
        Brush _blackBrush = new SolidBrush(Color.Black);

        private void NumericDisplay_Paint(object? sender, PaintEventArgs e)
        {
            UpdateColors();
            Color gradientColor = Color.FromArgb((byte)(255 - (255 - _backgroundColor.R) * .9),
                (byte)(255 - (255 - _backgroundColor.G) * .9),
                (byte)(255 - (255 - _backgroundColor.B) * .9)
                );
            var ForeColor = Colors.ContrastColor(_backgroundColor);

            var B = new Bitmap(Width, Height);
            var G = Graphics.FromImage(B);
            G.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            G.TextContrast = 1;
            G.Clear(_backgroundColor);

            var rectName = new Rectangle(0, 0, Width, (int)(Height / 3.0f));
            var rectNumber = new Rectangle(5, (int)(Height / 3.0f), Width - 10, (int)(Height * 2.0f / 3.0f) - 5);

            G.FillRectangle(_whiteBrush, rectNumber);

            using (var brush = new SolidBrush(ForeColor))
            {
                G.DrawString(DisplayName, Font, brush, rectName, _stringFormat);
                G.DrawString(Number, Font, _blackBrush, rectNumber, _stringFormat);
            }

            e.Graphics.DrawImage(B, 0, 0);

            B.Dispose();
            G.Dispose();
        }
    }
}
