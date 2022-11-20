using Agora.Utilities;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Agora.Forms
{

    /// <summary>
    /// The LEDBulb is a .Net control for Windows Forms that emulates an
    /// LED light with two states On and Off.  The purpose of the control is to 
    /// provide a sleek looking representation of an LED light that is sizable, 
    /// has a transparent background and can be set to different colors.  DLSLedBulb
    /// converts this to a 4 color LED
    /// </summary>
    public partial class DLSLED_Observing : Control
    {
        public enum LEDColor
        {
            Green,
            Yellow,
            Red,
            Blue
        }

        public enum LEDShape
        {
            Round,
            Rectangular
        }

        private readonly Color _reflectionColor = System.Drawing.Color.FromArgb(180, 225, 225, 225);
        private readonly Color[] _surroundColor = new Color[] { System.Drawing.Color.FromArgb(0, 255, 255, 255) };

        [DefaultValue("LED_ON_OFF"), Category("Agora")]
        public string ObservableOnOffBoolean
        {
            get { return _ObservableOnOff?.Name ?? string.Empty; }
            set {
                if (_ObservableOnOff != null)
                    _ObservableOnOff.PropertyChanged -= _ObservabledOnOff_PropertyChanged;
                _ObservableOnOff = Observable<bool>.Get(value);
                _ObservableOnOff.PropertyChanged += _ObservabledOnOff_PropertyChanged;
                Update();
            }
        }
        private Observable<bool>? _ObservableOnOff = null;

        private void _ObservabledOnOff_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            On = _ObservableOnOff?.Value ?? false;
            Update();
        }

        /// Gets or Sets the shape of the LED light
        [DefaultValue(LEDShape.Round), Category("Agora")]
        public LEDShape Shape
        {
            get { return _shape; }
            set
            {
                if (_shape == value) return;
                _shape = value;
                _UpdateRqd = true;
                this.Invalidate();  // Redraw the control
            }
        }
        private LEDShape _shape;

        /// Gets or Sets the color of the LED light
        [DefaultValue(LEDColor.Green), Category("Agora")]
        public LEDColor Color
        {
            get { return _color; }
            set
            {
                if (_color == value) return;
                _color = value;
                var C = ConvertToColor(_color);
                this.DarkColor = ControlPaint.Dark(C);
                this.DarkDarkColor = ControlPaint.DarkDark(C);
                _UpdateRqd = true;
                this.Invalidate();  // Redraw the control
            }
        }
        private LEDColor _color;

        private Color DarkColor { get; set; }
        private Color DarkDarkColor { get; set; }

        /// <summary>
        /// Gets or Sets whether the light is turned on
        /// </summary>
        public bool On
        {
            get { return _on; }
            set { _on = value; this.Invalidate(); }
        }
        private bool _on = true;

        public DLSLED_Observing()
        {
            SetStyle(ControlStyles.DoubleBuffer
            | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.ResizeRedraw
            | ControlStyles.UserPaint
            | ControlStyles.SupportsTransparentBackColor, true);

            //Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

            TextChanged += LedBulb_TextChanged;

            this.Color = LEDColor.Green;
            Agora.Utilities.Timers.Get(600).Elapsed += Timer600_Elapsed;
        }

        private void LedBulb_TextChanged(object? sender, EventArgs e)
        {
            _UpdateRqd = true;
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Agora.Utilities.Timers.Get(600).Elapsed -= Timer600_Elapsed;
            }
            base.Dispose(disposing);
        }

        private void Timer600_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (Blink) On = !On;
        }

        /// <summary>
        /// Handles the Paint event for this UserControl
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            // Create an offscreen graphics object for double buffering
            if (_UpdateRqd)
            {
                Font testFont = Font;
                for (float adjustedSize = 20; adjustedSize >= 6.0f; adjustedSize -= 0.1f)
                {
                    testFont = new Font(Font.Name, adjustedSize, Font.Style);
                    SizeF sizeOfTextUsingAdjustedSizeFont = e.Graphics.MeasureString(Text, testFont);

                    if (sizeOfTextUsingAdjustedSizeFont.Width < Size.Width - (Size.Height * .6f) - 10 && // 10 is a magic number that stops text from wrapping
                        sizeOfTextUsingAdjustedSizeFont.Height < Size.Height)
                    {
                        _locationLabelY = (int)(Size.Height / 2 - sizeOfTextUsingAdjustedSizeFont.Height / 2);
                        break;
                    }
                }
                Font = testFont;

                _OffBitmap = new Bitmap(ClientRectangle.Width, ClientRectangle.Height);
                using (Graphics g = Graphics.FromImage(_OffBitmap))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    drawControl(g, false);

                    using (var textBrush = new SolidBrush(ForeColor))
                        g.DrawString(Text, Font, textBrush, Size.Height * 1.2f, _locationLabelY);
                }
                _OnBitmap = new Bitmap(ClientRectangle.Width, ClientRectangle.Height);
                using (Graphics g = Graphics.FromImage(_OnBitmap))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    drawControl(g, true);
                    using (var textBrush = new SolidBrush(ForeColor))
                        g.DrawString(Text, Font, textBrush, Size.Height * 1.2f, _locationLabelY);
                }

                _UpdateRqd = false;
            }
            // Draw the image to the screen

            e.Graphics.DrawImageUnscaled(On ? _OnBitmap! : _OffBitmap!, 0, 0);
        }

        int _locationLabelY = 0;


        protected override void OnSizeChanged(EventArgs e)
        {
            _UpdateRqd = true;
            base.OnSizeChanged(e);
        }

        bool _UpdateRqd = true;
        Bitmap? _OnBitmap, _OffBitmap;

        /// <summary>
        /// Renders the control to an image
        /// </summary>
        private void drawControl(Graphics g, bool on)
        {
            // Is the bulb on or off
            Color lightColor = (on) ? ConvertToColor(this.Color) : System.Drawing.Color.FromArgb(150, this.DarkColor);
            Color darkColor = (on) ? this.DarkColor : this.DarkDarkColor;

            if (Shape == LEDShape.Round)
            {
                // Calculate the dimensions of the bulb
                int width = this.Width - (this.Padding.Left + this.Padding.Right);
                int height = this.Height - (this.Padding.Top + this.Padding.Bottom);
                // Diameter is the lesser of width and height
                int diameter = Math.Min(width, height);
                // Subtract 1 pixel so ellipse doesn't get cut off
                diameter = Math.Max(diameter - 1, 1);

                // Draw the background ellipse
                var rectangle = new Rectangle(this.Padding.Left, this.Padding.Top, diameter, diameter);
                g.FillEllipse(new SolidBrush(darkColor), rectangle);

                // Draw the glow gradient
                var path = new GraphicsPath();
                path.AddEllipse(rectangle);
                var pathBrush = new PathGradientBrush(path);
                pathBrush.CenterColor = lightColor;
                pathBrush.SurroundColors = new Color[] { System.Drawing.Color.FromArgb(150, lightColor) };
                g.FillEllipse(pathBrush, rectangle);

                // Draw the white reflection gradient
                var offset = Convert.ToInt32(diameter * .15F);
                var diameter1 = Convert.ToInt32(rectangle.Width * .8F);
                var whiteRect = new Rectangle(rectangle.X - offset, rectangle.Y - offset, diameter1, diameter1);
                var path1 = new GraphicsPath();
                path1.AddEllipse(whiteRect);
                var pathBrush1 = new PathGradientBrush(path);
                pathBrush1.CenterColor = _reflectionColor;
                pathBrush1.SurroundColors = _surroundColor;
                g.FillEllipse(pathBrush1, whiteRect);

                // Draw the border
                g.SetClip(this.ClientRectangle);
                if (this.On) g.DrawEllipse(new Pen(System.Drawing.Color.FromArgb(85, System.Drawing.Color.Black), 1F), rectangle);
            }
            // Rectangle
            else
            {
                // Calculate the dimensions of the bulb
                int width = this.Height / 2;
                int height = this.Height - (this.Padding.Top + this.Padding.Bottom);

                // Draw the background ellipse
                var rectangle = new Rectangle(this.Padding.Left, this.Padding.Top, width, height);
                g.FillRectangle(new SolidBrush(darkColor), rectangle);

                // Draw the glow gradient
                var path = new GraphicsPath();
                path.AddRectangle(rectangle);
                var pathBrush = new PathGradientBrush(path);
                pathBrush.CenterColor = lightColor;
                pathBrush.SurroundColors = new Color[] { System.Drawing.Color.FromArgb(0, lightColor) };
                g.FillRectangle(pathBrush, rectangle);

                // Draw the white reflection gradient
                var offset = Convert.ToInt32(width * .15F);
                var diameter1 = Convert.ToInt32(rectangle.Width * .8F);
                var whiteRect = new Rectangle(rectangle.X - offset, rectangle.Y - offset, diameter1, diameter1);
                var path1 = new GraphicsPath();
                path1.AddRectangle(whiteRect);
                var pathBrush1 = new PathGradientBrush(path);
                pathBrush1.CenterColor = _reflectionColor;
                pathBrush1.SurroundColors = _surroundColor;
                g.FillRectangle(pathBrush1, whiteRect);

                // Draw the border
                g.SetClip(this.ClientRectangle);
                if (this.On) g.DrawRectangle(new Pen(System.Drawing.Color.FromArgb(85, System.Drawing.Color.Black), 1F), rectangle);
            }
        }

        /// <summary>
        /// Causes the Led to start blinking
        /// </summary>
        public bool Blink
        {
            get { return _blink; }
            set
            {
                _blink = value;
                if (!_blink) On = true;
            }
        }
        private bool _blink;

        Color ConvertToColor(LEDColor col)
        {
            switch (col)
            {
                default:
                case LEDColor.Green: return System.Drawing.Color.LightGreen;
                case LEDColor.Red: return System.Drawing.Color.Red;
                case LEDColor.Yellow: return System.Drawing.Color.Yellow;
                case LEDColor.Blue: return System.Drawing.Color.DodgerBlue;
            }
        }
    }
}
