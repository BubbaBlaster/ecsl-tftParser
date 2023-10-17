using Agora.Logging;
using System.ComponentModel;

namespace Agora.Forms
{
    public class ListViewLogDisplay : ILoggerTarget
    {
        public ListView _listView;
        readonly Font bold = new("Segoe UI", 7F, FontStyle.Bold, GraphicsUnit.Point, 0);

        public bool InfoLevelBold { get; set; } = false;

        public ListViewLogDisplay(ListView listView)
        {
            _listView = listView;
            _listView.Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            _listView.View = View.Details;
            _listView.GridLines = false;
            _listView.AllowColumnReorder = false;
            _listView.Columns.Add("Time", (int)(0.10f * _listView.Size.Width), HorizontalAlignment.Right);
            _listView.Columns.Add("Level", (int)(0.07f * _listView.Size.Width), HorizontalAlignment.Center);
            _listView.Columns.Add("Message", (int)(0.70f * _listView.Size.Width), HorizontalAlignment.Left);
            _listView.Columns.Add("Location", (int)(0.12f * _listView.Size.Width), HorizontalAlignment.Left);

            _listView.DoubleBuffered(true);
        }

        private void Add(long tick, LogLevel level, string message, bool heading = false)
        {
            if (level < Agora.SDK.Log.GetLevel())
                return;

            _listView.InvokeIfRequired(() =>
                {
                    _listView.BeginUpdate();

                    if (_listView.Items.Count > 100)
                        _listView.Items.RemoveAt(0);
                    ListViewItem i = new(tick.ToString());
                    i.SubItems.Add(level.ToString());
                    i.SubItems.Add(heading ? $"--- {message} ---" : message);
                    switch (level)
                    {
                        case LogLevel.Trace:
                            i.ForeColor = Color.DarkGray;
                            break;
                        case LogLevel.Debug:
                            i.ForeColor = Color.Gray;
                            break;
                        case LogLevel.Info:
                            i.ForeColor = Color.Black;
                            if (InfoLevelBold) i.Font = bold;
                            break;
                        case LogLevel.Warn:
                            i.BackColor = Color.Orange;
                            i.ForeColor = Color.Black;
                            i.Font = bold;
                            break;
                        case LogLevel.Error:
                            i.ForeColor = Color.Red;
                            i.Font = bold;
                            break;
                        case LogLevel.Fatal:
                            i.ForeColor = Color.White;
                            i.BackColor = Color.Red;
                            i.Font = bold;
                            break;
                    }
                    _listView.Items.Add(i);
                    _listView.TopItem = i;
                    _listView.EndUpdate();
                });
        }

        public void Write(long ticks, LogLevel level, string message, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            Add(ticks, level, message);
        }

        public void WriteHeading(long ticks, string message)
        {
            Add(ticks, LogLevel.Info, message, true);
        }

        public void WriteException(long ticks, LogLevel level, Exception ex, string message, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            Add(ticks, LogLevel.Info, message);
        }
    }
}
