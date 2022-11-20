using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Agora.Utilities;

namespace Agora.Forms
{
    public partial class GaugeControl : UserControl
    {
        #region User Control Properties
        [Category("Agora"), Description("Is the title to be visible?")]
        public bool IsTitleVisible { get { return _titleLabel.Visible; } set { _titleLabel.Visible = value; } }
        [Category("Agora"), Description("Is the unit to be visible?")]
        public bool IsUnitVisible { get { return _unitLabel.Visible; } set { _unitLabel.Visible = value; Invalidate(); } }
        [Category("Agora"), Description("Is the warning mark to be visible?")]
        public bool IsWarningMarkVisible { get; set; } = true;

        [Category("Agora"), Description("Is the arrow to be visible?")]
        public bool IsArrowVisible { get; set; } = true;
        [Category("Agora"), Description("The icon scale.")]
        public float IconScale { get; set; } = 1.0f;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Bindable(false)]
        [Browsable(false)]
        override public Color BackColor { get; set; }

        [Category("Agora"), Description("Minimum width of the control.")]
        public int MinWidth { get; set; } = 200;
        [Category("Agora"), Description("Minimum height of the control.")]
        public int MinHeight { get; set; } = 200;


        [Category("Agora"), Description("Are the legends to be visible?")]
        public bool IsLegendsVisible
        {
            get { return _legendLabel.Visible; }
            set
            {
                _legendLabel.Visible = value;

                if (!value)
                {
                    _secondLegendLabel.Visible =
                    _thirdLegendLabel.Visible =
                    _secondValueLabel.Visible =
                    _thirdValueLabel.Visible =
                    _secondUnitLabel.Visible =
                    _thirdUnitLabel.Visible = false;
                }
                else
                {
                    bool visible = !string.IsNullOrEmpty(SecondLegend);
                    _secondLegendLabel.Visible =
                    _secondValueLabel.Visible =
                    _secondUnitLabel.Visible = visible;
                    visible = !string.IsNullOrEmpty(ThirdLegend);
                    _thirdLegendLabel.Visible =
                    _thirdValueLabel.Visible =
                    _thirdUnitLabel.Visible = visible;
                }
            }
        }

        [Category("Agora"), Description("TopMargin is from control top to the dial, a fraction of Height")]
        public float TopMargin { get; set; }

        [Category("Agora"), Description("LeftMargin is from control left edge to the dial, a fraction of Width")]
        public float LeftMargin { get; set; }
        [Category("Agora"), Description("RightMargin is from control right edge to the dial, a fraction of Width")]
        public float RightMargin { get; set; }

        [Category("Agora.Ranges"), Description("Gauge Minimum Value")]
        public double Min { get { return _min; } set { _min = value; Invalidate(); } }
        private double _min = 0;
        [Category("Agora.Ranges"), Description("Gauge Maximum Value")]
        public double Max { get { return _max; } set { _max = value; Invalidate(); } }
        private double _max = 100;

        [Category("Agora.Ranges"), Description("The unit for all measurements.")]
        public string Unit { get { return _unit; } set { _unit = value; Invalidate(); } }
        private string _unit = "N/A";

        [Category("Agora.Ranges"), Description("The value")]
        public double Value { get { return _value; } set { _value = value; Invalidate(); } }
        private double _value = 20;

        [Category("Agora.Ranges"), Description("The second value")]
        public double SecondValue { get { return _secondValue; } set { _secondValue = value; Invalidate(); } }
        private double _secondValue = 0;
        [Category("Agora.Ranges"), Description("The third value")]
        public double ThirdValue { get { return _thirdValue; } set { _thirdValue = value; Invalidate(); } }
        private double _thirdValue = 0;

        [Category("Agora.Ranges"), Description("Precision of the digital displays.")]
        public int Precision { get { return _precision; } set { _precision = value; Invalidate(); } }
        int _precision = 1;

        [Category("Agora.Legend"), Description("The text for legend.")]
        public string Legend { get { return this._legendLabel.Text; } set { _legendLabel.Text = value; Invalidate(); } }

        [Category("Agora.Legend"), Description("The text for the second legend.")]
        public string SecondLegend
        {
            get { return _secondLegendLabel.Text; }
            set
            {
                _secondLegendLabel.Text = value;
                if (this.IsLegendsVisible)
                {
                    _secondLegendLabel.Visible = !string.IsNullOrEmpty(value);
                    _secondValueLabel.Visible = !string.IsNullOrEmpty(value);
                    _secondUnitLabel.Visible = !string.IsNullOrEmpty(value);
                    Invalidate();
                }
            }
        }

        [Category("Agora.Legend"), Description("The text for the third legend.")]
        public string ThirdLegend
        {
            get { return _thirdLegendLabel.Text; }
            set
            {
                _thirdLegendLabel.Text = value;
                if (this.IsLegendsVisible)
                {
                    _thirdLegendLabel.Visible = !string.IsNullOrEmpty(value);
                    _thirdValueLabel.Visible = !string.IsNullOrEmpty(value);
                    _thirdUnitLabel.Visible = !string.IsNullOrEmpty(value);
                    Invalidate();
                }
            }
        }

        [Category("Agora.Ranges"), Description("The threshold value")]
        public double Threshold { get { return _threshold; } set { _threshold = value; Invalidate(); } }
        double _threshold = 90;

        [Category("Agora.Ranges"), Description("Is the threshold to be visible?")]
        public bool IsThresholdVisible { get { return _thresholdLabel.Visible; } set { _thresholdLabel.Visible = value; Invalidate(); }}

        [Category("Agora.Ranges"), Description("The average value")]
        public double ArrowValue { get { return _arrowValue; } set { _arrowValue = value; Invalidate(); } }
        double _arrowValue = 50;

        [Category("Agora"), Description("The color of the dial.")]
        public Color DialColor { get; set; }
        [Category("Agora"), Description("The color of the arrow.")]
        public Color ArrowColor { get { return _arrowColor; } set { _arrowColor = value; Invalidate(); } }
        Color _arrowColor;

        [Category("Agora.Legend"), Description("The color of the legend.")]
        public Color LegendColor { get; set; }
        [Category("Agora.Legend"), Description("The color of the second legend.")]
        public Color SecondLegendColor { get; set; }
        [Category("Agora.Legend"), Description("The color of the third legend.")]
        public Color ThirdLegendColor { get; set; }
        //public Color AbnormalColor { get; set; } 

        [Category("Agora"), Description("The void color.")]
        public Color VoidColor { get; set; }

        [Category("Agora"), Description("The Title.")]
        public string Title
        {
            get { return _titleLabel.Text; }
            set { _titleLabel.Text = value; }
        }
        [Category("Agora"), Description("The Title font.")]
        public Font TitleLabelFont
        {
            get { return _titleLabel.Font; }
            set
            {
                this._titleLabel.Font = value;
            }
        }

        [Category("Agora.Legend"), Description("The legend font.")]
        public Font LegendLabelFont
        {
            get { return _legendLabel.Font; }
            set
            {
                _legendLabel.Font = value;
                _secondLegendLabel.Font = value;
                _thirdLegendLabel.Font = value;
            }
        }

        [Category("Agora.Legend"), Description("The legend label color.")]
        public Color LegendLabelColor
        {
            get { return _legendLabel.ForeColor; }
            set
            {
                _legendLabel.ForeColor = value;
                _secondLegendLabel.ForeColor = value;
                _thirdLegendLabel.ForeColor = value;
            }
        }

        [Category("Agora"), Description("The title color.")]
        public Color TitleLabelColor
        {
            get { return _titleLabel.ForeColor; }
            set
            {
                _titleLabel.ForeColor = value;
            }
        }

        [Category("Agora"), Description("The value label font.")]
        public Font ValueLabelFont
        {
            get { return _valueLabel.Font; }
            set
            {
                _valueLabel.Font = value;
            }
        }

        //[Category("Agora"), Description("The value label color.")]
        //public Color ValueLabelColor { get; set; }

        [Category("Agora"), Description("The font for ValueLabel.")]
        public Font LegendValueLabelFont
        {
            get { return _secondValueLabel.Font; }
            set
            {
                _secondValueLabel.Font = value;
                _thirdValueLabel.Font = value;
            }
        }

        [Category("Agora"), Description("The color of the label for Legend Values.")]
        public Color LegendValueLabelColor
        {
            get { return _secondValueLabel.ForeColor; }
            set
            {
                _secondValueLabel.ForeColor = value;
                _thirdValueLabel.ForeColor = value;
                _secondUnitLabel.ForeColor = value;
                _thirdUnitLabel.ForeColor = value;
            }
        }

        [Category("Agora"), Description("The font for unit.")]
        public Font UnitLabelFont
        {
            get { return _unitLabel.Font; }
            set
            {
                _unitLabel.Font = value;
                _secondUnitLabel.Font = value;
                _thirdUnitLabel.Font = value;
            }
        }

        [Category("Agora.Range"), Description("The font for min/max values.")]
        public Font MinMaxLabelFont
        {
            get { return _minLabel.Font; }
            set
            {
                this._minLabel.Font = value;
                this._maxLabel.Font = value;
                this._thresholdLabel.Font = value;
            }
        }

        [Category("Agora.Range"), Description("The font color for min/max values.")]
        public Color MinMaxLabelColor
        {
            get { return _minLabel.ForeColor; }
            set
            {
                this._minLabel.ForeColor = value;
                this._maxLabel.ForeColor = value;
                this._thresholdLabel.ForeColor = value;
            }
        }

        #endregion

        public GaugeControl()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.DoubleBuffered = true;
            this.BackColor = System.Drawing.Color.White;
            this.Margin = new System.Windows.Forms.Padding(0, 1, 0, 1);
            this.Name = "GaugeControl";
            this.Size = new System.Drawing.Size(250, 230);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.MyPaint);
            this.ParentChanged += GaugeControl_ParentChanged;
            this.ResumeLayout(false);

            InitLabel(_titleLabel);
            InitLabel(_valueLabel);
            InitLabel(_maxLabel);
            InitLabel(_minLabel);
            InitLabel(_unitLabel);
            InitLabel(_legendLabel);
            InitLabel(_secondLegendLabel);
            InitLabel(_thirdLegendLabel);
            InitLabel(_thresholdLabel);
            InitLabel(_secondValueLabel);
            InitLabel(_thirdValueLabel);
            InitLabel(_secondUnitLabel);
            InitLabel(_thirdUnitLabel);

            SetTemplate(0);

            TopMargin = 0.2f;
            LeftMargin = 0.1f;
            RightMargin = 0.15f;

            IsLegendsVisible = true;
            this.SizeChanged += GaugeControl_SizeChanged;

            Timers.Get(600).Elapsed += WarningFlash;
        }

        private void GaugeControl_ParentChanged(object? sender, EventArgs e)
        {
            if( Parent != null )
                BackColor = Parent.BackColor;
        }

        void InitLabel(Label label)
        {
            label.Visible = true;
            label.AutoSize = true;
            label.BackColor = Colors.Transparent;
            label.TextAlign = ContentAlignment.TopLeft;
            this.Controls.Add(label);
        }

        public void SetTemplate(int type)
        {
            if (type == 0)
            {
                DialColor = Colors.ColorFromString("#5E6670");
                ArrowColor = Colors.DM_Grey05;
                VoidColor = Colors.DM_Grey01;
                TitleLabelFont = new Font("Segoe UI", 14, FontStyle.Bold);
                TitleLabelColor = Colors.DM_White;
                ValueLabelFont = new Font("Segoe UI", 20, FontStyle.Bold);
                UnitLabelFont = new Font("Segoe UI", 10, FontStyle.Regular);
                MinMaxLabelFont = new Font("Segoe UI", 7, FontStyle.Bold);
                MinMaxLabelColor = Colors.DM_White;
                LegendLabelFont = new Font("Segoe UI", 8, FontStyle.Bold);
                LegendLabelColor = Colors.ColorFromString("#99A6B5");
                LegendValueLabelFont = new Font("Segoe UI", 14, FontStyle.Bold);
                LegendValueLabelColor = Colors.ColorFromString("#E8ECF2");
                LegendColor = Colors.ColorFromString("#558DB9");
                SecondLegendColor = Colors.ColorFromString("#016737");
                ThirdLegendColor = Colors.ColorFromString("#7FAE35");
            }
        }

        private void UpdateFontSize()
        {
            // update the font size
            float fontScale = Math.Min(Width * 1.0f / MinWidth, Height * 1.0f / MinHeight) * 0.9f;

            if (fontScale.CompareTo(1.0f) < 0) fontScale = 1.0f;
            if (fontScale.CompareTo(3.0f) > 0) fontScale = 3.0f;

            TitleLabelFont = new Font("Segoe UI", 14 * fontScale, FontStyle.Bold);
            ValueLabelFont = new Font("Segoe UI", 20 * fontScale, FontStyle.Bold);
            UnitLabelFont = new Font("Segoe UI", 10 * fontScale, FontStyle.Regular);
            MinMaxLabelFont = new Font("Segoe UI", 7 * fontScale, FontStyle.Bold);
            LegendLabelFont = new Font("Segoe UI", 8 * fontScale, FontStyle.Bold);
            LegendValueLabelFont = new Font("Segoe UI", 14 * fontScale, FontStyle.Bold);
        }

        private const int WarningMarkWidth = 32;
        void DrawWarningMark(PaintEventArgs e)
        {
            if (!IsWarningMarkVisible || Value < Threshold)
                return;

            var brush = new SolidBrush(Colors.DM_Grey03);

            int w = WarningMarkWidth;
            e.Graphics.FillRectangle(brush, new Rectangle(Width - w, 0, w, w));
            w = w - 2;
            if (_warningMarkOn)
            {
                brush.Color = Colors.Alert_Red;
                var pen = new Pen(Colors.Alert_Red);
                var font = new Font("Segoe UI", 12, FontStyle.Bold);
                e.Graphics.DrawArc(pen, this.Width - w, 10, w - 10, w - 10, 0, 360);
                e.Graphics.DrawString("!", font, brush, this.Width - 25, 10);
                font.Dispose();
                pen.Dispose();
            }
            brush.Dispose();
            _warningMarkOn = !_warningMarkOn;
        }

        void DrawArrow(PaintEventArgs e)
        {
            if (!IsArrowVisible)
                return;

            int dialRadius = DialRadius;
            int centerX = CenterX;
            int centerY = CenterY;

            if (IsArrowVisible)
            {
                var brush = new SolidBrush(Parent.BackColor);
                int radius = (int)(dialRadius * 0.90);
                double radian = Math.PI * (Max - ArrowValue) / (Max - Min);
                PointF[] arrow = new PointF[3];
                arrow[0] = new PointF((float)(centerX + radius * Math.Cos(radian)), (float)(centerY - radius * Math.Sin(radian)));
                radius = (int)(dialRadius);
                var temp = new PointF((float)(centerX + radius * Math.Cos(radian)), (float)(centerY - radius * Math.Sin(radian)));
                radian = Math.PI / 2 - radian;
                var dx = 0.045 * centerX * Math.Cos(radian);
                var dy = 0.045 * centerX * Math.Sin(radian);

                arrow[1] = new PointF((float)(temp.X + dx), (float)(temp.Y + dy));
                arrow[2] = new PointF((float)(temp.X - dx), (float)(temp.Y - dy));
                brush.Color = ArrowColor;
                e.Graphics.FillPolygon(brush, arrow);
                brush.Dispose();
            }
        }

        int DialRadius { get { return (int)(this.Width * (1.0 - LeftMargin - RightMargin) / 2); } }

        int CenterX { get { return (int)(this.Width * LeftMargin + DialRadius); } }

        int CenterY { get { return (int)(TopMargin * Height + DialRadius); } }

        void DrawDial(PaintEventArgs e)
        {
            int dialRadius = DialRadius;
            int centerX = CenterX;
            int centerY = CenterY;

            if (Min >= Max)
                Max = Min + 1;

            var brush = new SolidBrush(Parent.BackColor);
            //e.Graphics.FillRectangle(brush, 0-1, 0, Width+1, Height);

            brush.Color = DialColor;
            int radius = dialRadius;
            e.Graphics.FillPie(brush, centerX - radius, centerY - radius, 2 * radius, 2 * radius, 180, 180);

            brush.Color = Colors.Alert_Red;
            float angle = (float)(180 * (Threshold - Min) / (Max - Min));
            if (angle < 0) angle = 0;
            if (angle > 180) angle = 180;
            e.Graphics.FillPie(brush, centerX - radius, centerY - radius, 2 * radius, 2 * radius, 180 + angle, 180 - angle);

            radius = (int)(dialRadius * 0.96);
            brush.Color = Parent.BackColor;
            e.Graphics.FillPie(brush, centerX - radius, centerY - radius, 2 * radius, 2 * radius, 175, 190);

            radius = (int)(dialRadius * 0.90);
            brush.Color = VoidColor;
            e.Graphics.FillPie(brush, centerX - radius, centerY - radius, 2 * radius, 2 * radius, 180, 180);

            brush.Color = LegendColor;
            float sweep = 0;
            if (!double.IsNaN(Value))
            {
                sweep = (float)(180 * (Value - Min) / (Max - Min));
            }
            if (sweep < 0) sweep = 0;
            if (sweep > 180) sweep = 180;
            e.Graphics.FillPie(brush, centerX - radius, centerY - radius, 2 * radius, 2 * radius, 180, sweep);

            radius = (int)(dialRadius * 0.75);
            brush.Color = Parent.BackColor;
            e.Graphics.FillPie(brush, centerX - radius, centerY - radius, 2 * radius, 2 * radius, 175, 190);
            brush.Dispose();
        }

        protected void MyPaint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            if (e.ClipRectangle.Width == WarningMarkWidth)
            {
                DrawWarningMark(e);
                return;
            }
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            DrawDial(e);

            int centerX = CenterX;
            int centerY = CenterY;
            int dialRadius = DialRadius;

            if (Min >= Max)
                Max = Min + 1;

            var brush = new SolidBrush(Parent.BackColor);
            //string unit = UnitConverter.Instance.GetDisplayUnit(UnitSystem, Measurement, Unit);
            var format = string.Format("{{0:F{0}}}", Precision);
            if (!double.IsNaN(Value))
            {
                _valueLabel.Text = string.Format(format, Value);
            }
            else
            {
                _valueLabel.Text = "---";
            }
            SizeF size = e.Graphics.MeasureString(_valueLabel.Text, ValueLabelFont);
            _valueLabel.Location = new Point((int)(centerX - size.Width / 2), (int)(centerY - size.Height));

            _unitLabel.Text = Unit;
            _secondUnitLabel.Text = Unit;
            _thirdUnitLabel.Text = Unit;
            var unitSize = e.Graphics.MeasureString(_unitLabel.Text, UnitLabelFont);
            _unitLabel.Location = new Point((int)(centerX - unitSize.Width / 2), (int)(centerY - 3));

            _minLabel.Text = string.Format(format, Min);
            size = e.Graphics.MeasureString(_minLabel.Text, MinMaxLabelFont);
            _minLabel.Location = new Point((int)(centerX - dialRadius - 2), (int)(centerY + 2));

            _maxLabel.Text = string.Format(format, Max);
            size = e.Graphics.MeasureString(_maxLabel.Text, MinMaxLabelFont);
            _maxLabel.Location = new Point((int)(centerX + dialRadius - size.Width - 2), (int)(centerY + 2));

            if (!string.IsNullOrEmpty(_legendLabel.Text) && IsLegendsVisible)
            {
                var legendSize = e.Graphics.MeasureString(_legendLabel.Text, LegendLabelFont);
                brush.Color = LegendColor;
                int y = 10;
                e.Graphics.FillRectangle(brush, 10, y, legendSize.Height * IconScale, legendSize.Height * IconScale);
                int x = (int)(12 + legendSize.Height);
                this._legendLabel.Location = new Point(x, y);
                y = (int)this.Height;
                int unitX = 0;

                if (!string.IsNullOrEmpty(_thirdLegendLabel.Text))
                {
                    if (!double.IsNaN(ThirdValue))
                    {
                        _thirdValueLabel.Text = string.Format(format, ThirdValue);
                    }
                    else
                    {
                        _thirdValueLabel.Text = "---";
                    }
                    var thirdLegendSize = e.Graphics.MeasureString(_thirdLegendLabel.Text, LegendLabelFont);
                    var thirdValueSize = e.Graphics.MeasureString(_thirdValueLabel.Text, LegendValueLabelFont);
                    unitX = (int)(thirdLegendSize.Width + thirdValueSize.Width);
                }
                if (!string.IsNullOrEmpty(_secondLegendLabel.Text))
                {
                    if (!double.IsNaN(SecondValue))
                    {
                        _secondValueLabel.Text = string.Format(format, SecondValue);
                    }
                    else
                    {
                        _secondValueLabel.Text = "---";
                    }
                    var secondLegendSize = e.Graphics.MeasureString(_secondLegendLabel.Text, LegendLabelFont);
                    var secondValueSize = e.Graphics.MeasureString(_secondValueLabel.Text, LegendValueLabelFont);
                    int temp = (int)(secondLegendSize.Width + secondValueSize.Width);
                    if (unitX < temp)
                        unitX = temp;
                }
                unitX = unitX + x + 25;
                if (unitX > (int)(this.Width - 10 - unitSize.Width))
                    unitX = (int)(this.Width - 10 - unitSize.Width);

                if (!string.IsNullOrEmpty(_thirdLegendLabel.Text))
                {
                    brush.Color = ThirdLegendColor;

                    y -= (int)(legendSize.Height + 10);
                    e.Graphics.FillRectangle(brush, 10, y - legendSize.Height / 2, legendSize.Height, legendSize.Height);
                    _thirdLegendLabel.Location = new Point(x, (int)(y - legendSize.Height / 2));

                    int x1 = unitX;
                    _thirdUnitLabel.Location = new Point(x1, (int)(y - unitSize.Height / 2));
                    size = e.Graphics.MeasureString(_thirdValueLabel.Text, LegendValueLabelFont);
                    x1 -= (int)size.Width + 2;
                    _thirdValueLabel.Location = new Point(x1, (int)(y - size.Height / 2));
                }

                if (!string.IsNullOrEmpty(_secondLegendLabel.Text))
                {
                    y -= (int)(legendSize.Height + 10);
                    brush.Color = SecondLegendColor;

                    e.Graphics.FillRectangle(brush, 10, y - legendSize.Height / 2, legendSize.Height, legendSize.Height);
                    _secondLegendLabel.Location = new Point(x, (int)(y - legendSize.Height / 2));
                    int x1 = unitX;

                    _secondUnitLabel.Location = new Point(x1, (int)(y - unitSize.Height / 2));
                    size = e.Graphics.MeasureString(_secondValueLabel.Text, LegendValueLabelFont);
                    x1 -= (int)size.Width + 2;
                    _secondValueLabel.Location = new Point(x1, (int)(y - size.Height / 2));
                }
            }

            _thresholdLabel.Text = string.Format(format, Threshold);

            double radian = Math.PI * (Max - Threshold) / (Max - Min);
            int radius = (int)(dialRadius + 8);
            _thresholdLabel.Location = new Point((int)(centerX + radius * Math.Cos(radian)), (int)(centerY - radius * Math.Sin(radian)));

            size = e.Graphics.MeasureString(_titleLabel.Text, TitleLabelFont);
            _titleLabel.Location = new Point((int)(centerX - size.Width / 2), (int)(TopMargin * Height / 2 - size.Height / 2));

            Color valueLabelColor = Value < Threshold ? Colors.DM_White : Colors.Alert_Red;
            _valueLabel.ForeColor = valueLabelColor;
            _unitLabel.ForeColor = valueLabelColor;

            DrawArrow(e);
            DrawWarningMark(e);
        }

        private void GaugeControl_SizeChanged(object? sender, EventArgs e)
        {
            UpdateFontSize();
            Invalidate();
        }

        private void WarningFlash(object? sender, EventArgs e)
        {
            int w = WarningMarkWidth;
            Invalidate(new Rectangle(Width - w, 0, w, w));
        }

        private void SecondLegendLabel_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show(SecondLegend, "Do you want to make this variable primary?", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                //TODO: also need to switch Ave, Min, Max, Threshhold ,if they are not shared between the three variables
                string tmp = Legend;
                Legend = SecondLegend;
                SecondLegend = tmp;
                double tmpValue = Value;
                Value = SecondValue;
                SecondValue = tmpValue;
                Invalidate();
            }
            else if (dialogResult == DialogResult.No)
            {
                return;
            }
        }

        private void ThirdLegendLabel_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show(ThirdLegend, "Do you want to make this variable primary?", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                //TODO: also need to switch Ave, Min, Max, Threshhold ,if they are not shared between the three variables
                string tmp = Legend;
                Legend = ThirdLegend;
                ThirdLegend = tmp;
                double tmpValue = Value;
                Value = ThirdValue;
                ThirdValue = tmpValue;
                Invalidate();
            }
            else if (dialogResult == DialogResult.No)
            {
                return;
            }
        }

        Label _valueLabel = new Label();
        Label _unitLabel = new Label();
        Label _minLabel = new Label();
        Label _maxLabel = new Label();
        Label _titleLabel = new Label();
        Label _thresholdLabel = new Label();

        Label _legendLabel = new Label();
        Label _secondLegendLabel = new Label();
        Label _thirdLegendLabel = new Label();
        Label _secondValueLabel = new Label();
        Label _thirdValueLabel = new Label();
        Label _secondUnitLabel = new Label();
        Label _thirdUnitLabel = new Label();

        bool _warningMarkOn = true;
    }
}