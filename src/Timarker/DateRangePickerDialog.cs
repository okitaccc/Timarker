using System.Drawing.Drawing2D;

namespace Timarker;

public sealed class DateRangePickerDialog : Form
{
    private static readonly Color Accent = Color.FromArgb(37, 99, 235);
    private readonly MonthPane _left;
    private readonly MonthPane _right;
    private readonly Label _startValue = ValueLabel();
    private readonly Label _endValue = ValueLabel();
    private readonly ComboBox _startHour = TimeChoice(24);
    private readonly ComboBox _startMinute = TimeChoice(60);
    private readonly ComboBox _endHour = TimeChoice(24);
    private readonly ComboBox _endMinute = TimeChoice(60);
    private DateTime _leftMonth;
    private DateTime _rightMonth;
    private bool _choosingEnd;
    private DateSelectionMode _mode;
    private readonly Button _startMode = ModeButton("仅起始");
    private readonly Button _endMode = ModeButton("仅截止");
    private readonly Button _rangeMode = ModeButton("日期区间");
    private readonly CheckBox _showLunar = LunarToggle();

    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public bool HasStart { get; private set; }
    public bool HasEnd { get; private set; }
    public TimeSpan StartTime => new(_startHour.SelectedIndex, _startMinute.SelectedIndex, 0);
    public TimeSpan EndTime => new(_endHour.SelectedIndex, _endMinute.SelectedIndex, 0);

    public DateRangePickerDialog(DateTime start, DateTime end, bool hasStart = true, bool hasEnd = true,
        TimeSpan? startTime = null, TimeSpan? endTime = null)
    {
        StartDate = start.Date;
        EndDate = hasStart && hasEnd && end.Date < start.Date ? start.Date : end.Date;
        HasStart = hasStart;
        HasEnd = hasEnd;
        _mode = hasStart && hasEnd ? DateSelectionMode.Range : hasEnd ? DateSelectionMode.End : hasStart ? DateSelectionMode.Start : DateSelectionMode.Range;
        _choosingEnd = false;
        SetTime(_startHour, _startMinute, startTime ?? start.TimeOfDay);
        SetTime(_endHour, _endMinute, endTime ?? end.TimeOfDay);
        _leftMonth = new DateTime(StartDate.Year, StartDate.Month, 1);
        _rightMonth = new DateTime(EndDate.Year, EndDate.Month, 1);
        if (_rightMonth == _leftMonth) _rightMonth = _leftMonth.AddMonths(1);
        _left = new MonthPane(0, ChooseDate, ChooseMonth, MoveMonth);
        _right = new MonthPane(1, ChooseDate, ChooseMonth, MoveMonth);
        Text = "选择日期与时间";
        ClientSize = new Size(1040, 730);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = Color.White;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(30, 24, 30, 22), ColumnCount = 2, RowCount = 4, BackColor = Color.White };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.Controls.Add(ModeSelector(), 0, 0); root.SetColumnSpan(root.GetControlFromPosition(0, 0)!, 2);
        root.Controls.Add(RangeHeader(), 0, 1); root.SetColumnSpan(root.GetControlFromPosition(0, 1)!, 2);
        _left.Margin = new Padding(0, 14, 14, 10); _right.Margin = new Padding(14, 14, 0, 10);
        root.Controls.Add(_left, 0, 2); root.Controls.Add(_right, 1, 2);
        root.Controls.Add(Footer(), 0, 3); root.SetColumnSpan(root.GetControlFromPosition(0, 3)!, 2);
        Controls.Add(root);
        _startMode.Click += (_, _) => SetMode(DateSelectionMode.Start);
        _endMode.Click += (_, _) => SetMode(DateSelectionMode.End);
        _rangeMode.Click += (_, _) => SetMode(DateSelectionMode.Range);
        _showLunar.CheckedChanged += (_, _) =>
        {
            StyleLunarToggle(_showLunar);
            _left.SetShowLunar(_showLunar.Checked);
            _right.SetShowLunar(_showLunar.Checked);
            RefreshMonths();
        };
        StyleLunarToggle(_showLunar);
        RefreshMonths();
        L.Apply(this);
    }

    private Control ModeSelector()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 4, 0, 6) };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 102));
        var modes = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        modes.Controls.Add(new Label { Text = "选择方式", AutoSize = true, ForeColor = Color.FromArgb(71, 85, 105), Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold), Margin = new Padding(0, 8, 14, 0) });
        modes.Controls.Add(_startMode);
        modes.Controls.Add(_endMode);
        modes.Controls.Add(_rangeMode);
        panel.Controls.Add(modes, 0, 0);
        _showLunar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        panel.Controls.Add(_showLunar, 1, 0);
        return panel;
    }

    private void SetMode(DateSelectionMode mode)
    {
        _mode = mode;
        _choosingEnd = false;
        if (mode == DateSelectionMode.Start) HasEnd = false;
        if (mode == DateSelectionMode.End) HasStart = false;
        RefreshMonths();
    }

    private Control RangeHeader()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(14, 8, 14, 8) };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.Controls.Add(DateField("开始日期", _startValue, _startHour, _startMinute), 0, 0);
        panel.Controls.Add(new Label { Text = "→", Dock = DockStyle.Fill, Margin = Padding.Empty, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(148, 163, 184), Font = new Font(Font.FontFamily, 14F) }, 1, 0);
        panel.Controls.Add(DateField("结束日期", _endValue, _endHour, _endMinute), 2, 0);
        return panel;
    }

    private static Control DateField(string caption, Label value, ComboBox hour, ComboBox minute)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { Text = caption, Dock = DockStyle.Fill, ForeColor = Color.FromArgb(100, 116, 139), Font = new Font("Microsoft YaHei UI", 8F) });
        panel.Controls.Add(value);
        var time = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 3, 0, 0) };
        time.Controls.Add(new Label { Text = "时间", AutoSize = true, ForeColor = Color.FromArgb(100, 116, 139), Margin = new Padding(0, 6, 8, 0) });
        time.Controls.Add(hour);
        time.Controls.Add(new Label { Text = ":", AutoSize = true, ForeColor = Color.FromArgb(100, 116, 139), Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold), Margin = new Padding(5, 4, 5, 0) });
        time.Controls.Add(minute);
        panel.Controls.Add(time);
        return panel;
    }

    private Control Footer()
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 10, 0, 0) };
        var ok = new Button { Text = "确定", DialogResult = DialogResult.OK, Width = 96, Height = 36, BackColor = Accent, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        ok.FlatAppearance.BorderSize = 0;
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 82, Height = 36, FlatStyle = FlatStyle.Flat };
        cancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        var today = new Button { Text = "今天", Width = 72, Height = 36, FlatStyle = FlatStyle.Flat };
        today.Click += (_, _) =>
        {
            if (_mode == DateSelectionMode.Start) { StartDate = DateTime.Today; HasStart = true; HasEnd = false; }
            else if (_mode == DateSelectionMode.End) { EndDate = DateTime.Today; HasStart = false; HasEnd = true; }
            else { StartDate = EndDate = DateTime.Today; HasStart = HasEnd = true; _choosingEnd = true; }
            _leftMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            _rightMonth = _leftMonth.AddMonths(1);
            RefreshMonths();
        };
        panel.Controls.Add(cancel); panel.Controls.Add(ok); panel.Controls.Add(today);
        AcceptButton = ok; CancelButton = cancel;
        return panel;
    }

    private void ChooseDate(DateTime date)
    {
        if (_mode == DateSelectionMode.Start)
        {
            StartDate = date.Date;
            HasStart = true;
            HasEnd = false;
            RefreshMonths();
            return;
        }
        if (_mode == DateSelectionMode.End)
        {
            EndDate = date.Date;
            HasStart = false;
            HasEnd = true;
            RefreshMonths();
            return;
        }
        if (!_choosingEnd || date < StartDate)
        {
            StartDate = EndDate = date.Date;
            HasStart = HasEnd = true;
            _choosingEnd = true;
        }
        else
        {
            EndDate = date.Date;
            _choosingEnd = false;
        }
        RefreshMonths();
    }

    private void ChooseMonth(DateTime current, int offset)
    {
        using var picker = new MonthYearDialog(current);
        if (picker.ShowDialog(this) != DialogResult.OK) return;
        if (offset == 0) _leftMonth = picker.SelectedMonth;
        else _rightMonth = picker.SelectedMonth;
        RefreshMonths();
    }

    private void MoveMonth(int offset, int delta)
    {
        if (offset == 0) _leftMonth = _leftMonth.AddMonths(delta);
        else _rightMonth = _rightMonth.AddMonths(delta);
        RefreshMonths();
    }

    private void RefreshMonths()
    {
        _left.SetMonth(_leftMonth, HasStart ? StartDate : null, HasEnd ? EndDate : null);
        _right.SetMonth(_rightMonth, HasStart ? StartDate : null, HasEnd ? EndDate : null);
        _left.SetShowLunar(_showLunar.Checked);
        _right.SetShowLunar(_showLunar.Checked);
        _startHour.Enabled = _startMinute.Enabled = HasStart;
        _endHour.Enabled = _endMinute.Enabled = HasEnd;
        StyleMode(_startMode, _mode == DateSelectionMode.Start);
        StyleMode(_endMode, _mode == DateSelectionMode.End);
        StyleMode(_rangeMode, _mode == DateSelectionMode.Range);
        SetValue(_startValue, HasStart, StartDate);
        SetValue(_endValue, HasEnd, EndDate);
    }

    private void SetValue(Label label, bool selected, DateTime date)
    {
        label.Text = selected
            ? _showLunar.Checked ? $"{date:yyyy-MM-dd}  {LunarDate.FullText(date)}" : $"{date:yyyy-MM-dd}"
            : "未选择";
        label.ForeColor = selected ? Color.FromArgb(15, 23, 42) : Color.FromArgb(148, 163, 184);
    }

    private static ComboBox TimeChoice(int count)
    {
        var box = new ModernComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            Width = 66,
            Height = 30,
            BackColor = Color.White,
            Font = new Font("Microsoft YaHei UI", 9F),
            IntegralHeight = false,
            DropDownHeight = 240
        };
        for (var value = 0; value < count; value++) box.Items.Add(value.ToString("00"));
        box.SelectedIndex = 0;
        return box;
    }

    private static Button ModeButton(string text) => new()
    {
        Text = text,
        Width = 96,
        Height = 34,
        Margin = new Padding(0, 0, 8, 0),
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand
    };

    private static CheckBox LunarToggle() => new()
    {
        Text = "农历  开",
        Checked = true,
        Appearance = Appearance.Button,
        AutoSize = false,
        Width = 90,
        Height = 34,
        FlatStyle = FlatStyle.Flat,
        TextAlign = ContentAlignment.MiddleCenter,
        Cursor = Cursors.Hand
    };

    private static void StyleLunarToggle(CheckBox toggle)
    {
        toggle.Text = toggle.Checked ? "农历  开" : "农历  关";
        toggle.BackColor = toggle.Checked ? Color.FromArgb(239, 246, 255) : Color.White;
        toggle.ForeColor = toggle.Checked ? Accent : Color.FromArgb(100, 116, 139);
        toggle.FlatAppearance.BorderColor = toggle.Checked ? Color.FromArgb(147, 197, 253) : Color.FromArgb(203, 213, 225);
    }

    private static void StyleMode(Button button, bool selected)
    {
        button.BackColor = selected ? Accent : Color.White;
        button.ForeColor = selected ? Color.White : Color.FromArgb(71, 85, 105);
        button.FlatAppearance.BorderColor = selected ? Accent : Color.FromArgb(203, 213, 225);
    }

    private enum DateSelectionMode { Start, End, Range }

    private static void SetTime(ComboBox hour, ComboBox minute, TimeSpan time)
    {
        hour.SelectedIndex = Math.Clamp(time.Hours, 0, 23);
        minute.SelectedIndex = Math.Clamp(time.Minutes, 0, 59);
    }

    private static Label ValueLabel() => new() { Dock = DockStyle.Fill, ForeColor = Color.FromArgb(15, 23, 42), Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold) };

    private sealed class MonthPane : UserControl
    {
        private readonly int _offset;
        private readonly Button _title = new() { Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), BackColor = Color.White };
        private readonly CalendarCanvas _canvas;

        public MonthPane(int offset, Action<DateTime> choose, Action<DateTime, int> chooseMonth, Action<int, int> moveMonth)
        {
            _offset = offset; Dock = DockStyle.Fill; BackColor = Color.White;
            _canvas = new CalendarCanvas(choose);
            _title.FlatAppearance.BorderSize = 0; _title.Click += (_, _) => chooseMonth(_canvas.Month, _offset);
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 3, BackColor = Color.White };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
            var prev = NavButton("‹"); var next = NavButton("›");
            prev.Click += (_, _) => moveMonth(_offset, -1); next.Click += (_, _) => moveMonth(_offset, 1);
            root.Controls.Add(prev, 0, 0); root.Controls.Add(_title, 1, 0); root.Controls.Add(next, 2, 0);
            root.Controls.Add(_canvas, 0, 1); root.SetColumnSpan(_canvas, 3); Controls.Add(root);
        }

        public void SetMonth(DateTime month, DateTime? start, DateTime? end) { _title.Text = L.IsEnglish ? $"{month:MMMM yyyy} ⌄" : $"{month:yyyy 年 M 月} ⌄"; _canvas.SetMonth(month, start, end); }
        public void SetShowLunar(bool value) => _canvas.SetShowLunar(value);

        private static Button NavButton(string text)
        {
            var button = new Button { Text = text, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Color.FromArgb(71, 85, 105), Font = new Font("Microsoft YaHei UI", 13F), Cursor = Cursors.Hand };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }
    }

    private sealed class CalendarCanvas : Control
    {
        private readonly Action<DateTime> _choose;
        private DateTime? _start, _end;
        private DateTime? _hover;
        private bool _showLunar = true;
        public DateTime Month { get; private set; }

        public CalendarCanvas(Action<DateTime> choose) { _choose = choose; Dock = DockStyle.Fill; DoubleBuffered = true; Cursor = Cursors.Hand; BackColor = Color.White; }
        public void SetMonth(DateTime month, DateTime? start, DateTime? end) { Month = month; _start = start; _end = end; Invalidate(); }
        public void SetShowLunar(bool value) { _showLunar = value; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var width = Math.Max(1, ClientSize.Width / 7); var height = Math.Max(1, (ClientSize.Height - 38) / 6);
            var weekdays = new[] { "一", "二", "三", "四", "五", "六", "日" };
            for (var i = 0; i < 7; i++) TextRenderer.DrawText(e.Graphics, weekdays[i], Font, new Rectangle(i * width, 0, width, 30), Color.FromArgb(100, 116, 139), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            var first = Month.AddDays(-(((int)Month.DayOfWeek + 6) % 7));
            for (var i = 0; i < 42; i++) DrawDay(e.Graphics, first.AddDays(i), new Rectangle(i % 7 * width, 38 + i / 7 * height, width, height));
        }

        private void DrawDay(Graphics g, DateTime day, Rectangle rect)
        {
            var inMonth = day.Month == Month.Month;
            var inRange = _start is not null && _end is not null && day.Date >= _start.Value && day.Date <= _end.Value;
            var endpoint = day.Date == _start || day.Date == _end; var hover = day.Date == _hover;
            var size = Math.Min(58, Math.Min(rect.Width - 8, rect.Height - 8));
            var circle = new Rectangle(rect.Left + (rect.Width - size) / 2, rect.Top + (rect.Height - size) / 2, size, size);
            if (inRange) { using var range = new SolidBrush(Color.FromArgb(239, 246, 255)); g.FillRectangle(range, new Rectangle(rect.Left, circle.Top, rect.Width, circle.Height)); }
            if (endpoint || hover) { using var path = Round(circle, size / 2); using var brush = new SolidBrush(endpoint ? Accent : Color.FromArgb(226, 232, 240)); g.FillPath(brush, path); }
            var main = endpoint ? Color.White : inMonth ? Color.FromArgb(15, 23, 42) : Color.FromArgb(203, 213, 225);
            var sub = endpoint ? Color.White : inMonth ? Color.FromArgb(100, 116, 139) : Color.FromArgb(203, 213, 225);
            var mainTop = _showLunar ? circle.Top + 7 : circle.Top + (circle.Height - 20) / 2;
            TextRenderer.DrawText(g, day.Day.ToString(), Font, new Rectangle(circle.Left, mainTop, circle.Width, 20), main, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
            if (_showLunar)
            {
                using var lunarFont = new Font(Font.FontFamily, 8F);
                TextRenderer.DrawText(g, LunarDate.Text(day), lunarFont, new Rectangle(circle.Left + 2, circle.Top + 29, circle.Width - 4, 18), sub, TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }
            if (day.Date == DateTime.Today && !endpoint) { using var pen = new Pen(Accent, 1.5F); g.DrawEllipse(pen, circle); }
        }

        protected override void OnMouseMove(MouseEventArgs e) { _hover = Hit(e.Location); Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { _hover = null; Invalidate(); }
        protected override void OnMouseClick(MouseEventArgs e) { var day = Hit(e.Location); if (day is not null) _choose(day.Value); }
        private DateTime? Hit(Point p) { if (p.Y < 38) return null; var w = Math.Max(1, ClientSize.Width / 7); var h = Math.Max(1, (ClientSize.Height - 38) / 6); var first = Month.AddDays(-(((int)Month.DayOfWeek + 6) % 7)); return first.AddDays(Math.Clamp((p.Y - 38) / h, 0, 5) * 7 + Math.Clamp(p.X / w, 0, 6)); }
        private static GraphicsPath Round(Rectangle r, int radius) { var p = new GraphicsPath(); p.AddEllipse(r); return p; }
    }

    internal sealed class MonthYearDialog : Form
    {
        private readonly TableLayoutPanel _root = new() { Dock = DockStyle.Fill, Padding = new Padding(20), RowCount = 2, ColumnCount = 1, BackColor = Color.White };
        private readonly Label _title = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold) };
        private readonly Panel _body = new() { Dock = DockStyle.Fill };
        private int _selectedYear;
        private int _decadeStart;
        public DateTime SelectedMonth { get; private set; }

        public MonthYearDialog(DateTime current)
        {
            SelectedMonth = new DateTime(current.Year, current.Month, 1);
            _selectedYear = current.Year;
            _decadeStart = current.Year / 10 * 10;
            Text = "选择年月";
            Size = new Size(460, 400);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.White;
            _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _root.Controls.Add(Header());
            _root.Controls.Add(_body);
            Controls.Add(_root);
            ShowYears();
            L.Apply(this);
        }

        private Control Header()
        {
            var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = Color.White };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
            var prev = NavButton("‹");
            var next = NavButton("›");
            prev.Click += (_, _) => { _decadeStart -= 10; ShowYears(); };
            next.Click += (_, _) => { _decadeStart += 10; ShowYears(); };
            _title.Click += (_, _) => ShowYears();
            _title.Cursor = Cursors.Hand;
            header.Controls.Add(prev, 0, 0); header.Controls.Add(_title, 1, 0); header.Controls.Add(next, 2, 0);
            return header;
        }

        private void ShowYears()
        {
            _title.Text = $"{_decadeStart} – {_decadeStart + 9}";
            var grid = Grid(4, 3);
            for (var year = _decadeStart - 1; year <= _decadeStart + 10; year++)
            {
                var value = year;
                var muted = year < _decadeStart || year > _decadeStart + 9;
                var button = ChoiceButton(year.ToString(), year == _selectedYear, muted);
                button.Click += (_, _) => { _selectedYear = value; ShowMonths(); };
                grid.Controls.Add(button);
            }
            SetBody(grid);
        }

        private void ShowMonths()
        {
            _title.Text = $"‹  {_selectedYear} 年";
            var grid = Grid(4, 3);
            for (var month = 1; month <= 12; month++)
            {
                var value = month;
                var selected = _selectedYear == SelectedMonth.Year && month == SelectedMonth.Month;
                var button = ChoiceButton($"{month} 月", selected, false);
                button.Click += (_, _) => { SelectedMonth = new DateTime(_selectedYear, value, 1); DialogResult = DialogResult.OK; };
                grid.Controls.Add(button);
            }
            SetBody(grid);
        }

        private void SetBody(Control control)
        {
            _body.Controls.Clear();
            _body.Controls.Add(control);
        }

        private static TableLayoutPanel Grid(int rows, int columns)
        {
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = rows, ColumnCount = columns, Padding = new Padding(6, 12, 6, 8), BackColor = Color.White };
            for (var i = 0; i < rows; i++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / rows));
            for (var i = 0; i < columns; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columns));
            return grid;
        }

        private static Button ChoiceButton(string text, bool selected, bool muted)
        {
            var button = new Button { Text = text, Dock = DockStyle.Fill, Margin = new Padding(7), FlatStyle = FlatStyle.Flat, BackColor = selected ? Accent : Color.White, ForeColor = selected ? Color.White : muted ? Color.FromArgb(148, 163, 184) : Color.FromArgb(15, 23, 42), Cursor = Cursors.Hand };
            button.FlatAppearance.BorderSize = selected ? 0 : 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            button.FlatAppearance.MouseOverBackColor = selected ? Accent : Color.FromArgb(239, 246, 255);
            return button;
        }

        private static Button NavButton(string text)
        {
            var button = new Button { Text = text, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Color.FromArgb(71, 85, 105), Font = new Font("Microsoft YaHei UI", 14F), Cursor = Cursors.Hand };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }
    }
}
