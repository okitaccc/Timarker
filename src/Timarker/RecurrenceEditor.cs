using Timarker.Models;

namespace Timarker;

internal sealed class RecurrenceEditor : UserControl
{
    private static readonly Color Border = Color.FromArgb(226, 232, 240);
    private static readonly Color Accent = Color.FromArgb(37, 99, 235);
    private static readonly Color Muted = Color.FromArgb(100, 116, 139);

    private readonly ComboBox _pattern = Combo();
    private readonly ModernNumericUpDown _every = new() { Minimum = 1, Maximum = 365, Value = 1, Width = 82 };
    private readonly ComboBox _unit = Combo(92);
    private readonly FlowLayoutPanel _intervalPanel = Row();
    private readonly FlowLayoutPanel _daysPanel = Row();
    private readonly ComboBox _week = Combo(110);
    private readonly ComboBox _day = Combo(110);
    private readonly FlowLayoutPanel _monthlyPanel = Row();
    private readonly ComboBox _endMode = Combo(132);
    private readonly ComboBox _missed = Combo(220);
    private readonly DateTimePicker _until = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd", Width = 126 };
    private readonly ModernNumericUpDown _count = new() { Minimum = 1, Maximum = 9999, Value = 10, Width = 88 };
    private readonly FlowLayoutPanel _endPanel = Row();
    private readonly Dictionary<DayOfWeek, CheckBox> _dayButtons = [];

    public RecurrenceEditor()
    {
        Dock = DockStyle.Fill;
        AutoSize = true;
        BackColor = Color.White;
        Margin = Padding.Empty;

        AddOption(_pattern, "按间隔重复", RepeatPattern.Interval);
        AddOption(_pattern, "每个工作日", RepeatPattern.Weekdays);
        AddOption(_pattern, "每周指定星期", RepeatPattern.SelectedWeekdays);
        AddOption(_pattern, "每月第几个星期", RepeatPattern.MonthlyNthWeekday);
        AddOption(_pattern, "每月最后一天", RepeatPattern.MonthlyLastDay);
        AddOption(_pattern, "每年农历日期", RepeatPattern.LunarYearly);

        AddOption(_unit, "天", RepeatUnit.Day);
        AddOption(_unit, "周", RepeatUnit.Week);
        AddOption(_unit, "月", RepeatUnit.Month);
        AddOption(_unit, "年", RepeatUnit.Year);

        AddOption(_week, "第一个", 1);
        AddOption(_week, "第二个", 2);
        AddOption(_week, "第三个", 3);
        AddOption(_week, "第四个", 4);
        AddOption(_week, "最后一个", -1);
        foreach (var (text, value) in DayOptions()) AddOption(_day, text, value);

        AddOption(_endMode, "永不结束", RecurrenceEndMode.Never);
        AddOption(_endMode, "截止到日期", RecurrenceEndMode.OnDate);
        AddOption(_endMode, "发生指定次数", RecurrenceEndMode.AfterCount);
        AddOption(_missed, "补提醒最近一次", MissedOccurrencePolicy.RemindLatest);
        AddOption(_missed, "直接跳到下一次", MissedOccurrencePolicy.SkipToNext);

        _intervalPanel.Controls.Add(MutedLabel("每"));
        _intervalPanel.Controls.Add(_every);
        _intervalPanel.Controls.Add(_unit);

        foreach (var (text, day) in DayOptions())
        {
            var button = new CheckBox
            {
                Text = L.T(text),
                Appearance = Appearance.Button,
                AutoSize = false,
                Width = 42,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.White,
                ForeColor = Muted,
                Margin = new Padding(0, 0, 5, 0)
            };
            button.FlatAppearance.BorderColor = Border;
            button.CheckedChanged += (_, _) =>
            {
                button.BackColor = button.Checked ? Color.FromArgb(239, 246, 255) : Color.White;
                button.ForeColor = button.Checked ? Accent : Muted;
                button.FlatAppearance.BorderColor = button.Checked ? Color.FromArgb(147, 197, 253) : Border;
            };
            _dayButtons[day] = button;
            _daysPanel.Controls.Add(button);
        }

        _monthlyPanel.Controls.Add(_week);
        _monthlyPanel.Controls.Add(_day);

        _endPanel.Controls.Add(_endMode);
        _endPanel.Controls.Add(_until);
        _endPanel.Controls.Add(_count);
        _endPanel.Controls.Add(MutedLabel("次"));

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1, RowCount = 0, Margin = Padding.Empty };
        Add(layout, "重复方式", _pattern);
        Add(layout, "", _intervalPanel);
        Add(layout, "", _daysPanel);
        Add(layout, "", _monthlyPanel);
        Add(layout, "错过后", _missed);
        Add(layout, "结束重复", _endPanel);
        Controls.Add(layout);

        _pattern.SelectedIndexChanged += (_, _) => RefreshVisibility();
        _endMode.SelectedIndexChanged += (_, _) => RefreshVisibility();
        Reset();
    }

    public void Reset()
    {
        SelectValue(_pattern, RepeatPattern.Interval);
        SelectValue(_unit, RepeatUnit.Day);
        _every.Value = 1;
        foreach (var button in _dayButtons.Values) button.Checked = button == _dayButtons[DayOfWeek.Monday];
        SelectValue(_week, 1);
        SelectValue(_day, DayOfWeek.Monday);
        SelectValue(_endMode, RecurrenceEndMode.Never);
        SelectValue(_missed, MissedOccurrencePolicy.RemindLatest);
        _until.Value = DateTime.Today.AddMonths(1);
        _count.Value = 10;
        RefreshVisibility();
    }

    public void SetSimple(RepeatUnit unit, int every)
    {
        SelectValue(_pattern, RepeatPattern.Interval);
        SelectValue(_unit, unit);
        _every.Value = Math.Clamp(every, (int)_every.Minimum, (int)_every.Maximum);
        RefreshVisibility();
    }

    public void LoadFrom(EventItem item)
    {
        SelectValue(_pattern, item.RepeatPattern);
        SelectValue(_unit, item.RepeatUnit is RepeatUnit.None ? RepeatUnit.Day : item.RepeatUnit);
        _every.Value = Math.Clamp(item.RepeatEvery, (int)_every.Minimum, (int)_every.Maximum);
        var selectedDays = item.RepeatDaysOfWeek.Count == 0 && item.StartAt is not null
            ? new HashSet<DayOfWeek> { item.StartAt.Value.DayOfWeek }
            : item.RepeatDaysOfWeek.ToHashSet();
        foreach (var pair in _dayButtons) pair.Value.Checked = selectedDays.Contains(pair.Key);
        SelectValue(_week, item.RepeatWeekOfMonth);
        SelectValue(_day, item.RepeatDayOfWeek);
        SelectValue(_endMode, item.RecurrenceEndMode);
        SelectValue(_missed, item.MissedOccurrencePolicy);
        _until.Value = item.RepeatUntil is { } until && until >= _until.MinDate && until <= _until.MaxDate
            ? until
            : DateTime.Today.AddMonths(1);
        _count.Value = Math.Clamp(item.RepeatCount == 0 ? 10 : item.RepeatCount, (int)_count.Minimum, (int)_count.Maximum);
        RefreshVisibility();
    }

    public bool ApplyTo(EventItem item, DateTime? startAt, out string? error)
    {
        error = null;
        var pattern = Value<RepeatPattern>(_pattern);
        var days = _dayButtons.Where(x => x.Value.Checked).Select(x => x.Key).ToList();
        if (pattern is RepeatPattern.SelectedWeekdays && days.Count == 0)
        {
            error = "请至少选择一个星期。";
            return false;
        }
        var endMode = Value<RecurrenceEndMode>(_endMode);
        if (endMode is RecurrenceEndMode.OnDate && startAt is not null && _until.Value.Date < startAt.Value.Date)
        {
            error = "重复截止日期不能早于首次发生日期。";
            return false;
        }

        item.RepeatPattern = pattern;
        item.RepeatEvery = (int)_every.Value;
        item.RepeatUnit = pattern switch
        {
            RepeatPattern.Interval => Value<RepeatUnit>(_unit),
            RepeatPattern.MonthlyNthWeekday or RepeatPattern.MonthlyLastDay => RepeatUnit.Month,
            RepeatPattern.LunarYearly => RepeatUnit.Year,
            RepeatPattern.SelectedWeekdays => RepeatUnit.Week,
            _ => RepeatUnit.Day
        };
        item.RepeatDaysOfWeek = days;
        item.RepeatWeekOfMonth = Value<int>(_week);
        item.RepeatDayOfWeek = Value<DayOfWeek>(_day);
        item.RecurrenceEndMode = endMode;
        item.MissedOccurrencePolicy = Value<MissedOccurrencePolicy>(_missed);
        item.RepeatUntil = endMode is RecurrenceEndMode.OnDate ? _until.Value.Date : null;
        item.RepeatCount = endMode is RecurrenceEndMode.AfterCount ? (int)_count.Value : 0;
        return true;
    }

    private void RefreshVisibility()
    {
        var pattern = Value<RepeatPattern>(_pattern);
        _intervalPanel.Visible = pattern is RepeatPattern.Interval;
        _daysPanel.Visible = pattern is RepeatPattern.SelectedWeekdays;
        _monthlyPanel.Visible = pattern is RepeatPattern.MonthlyNthWeekday;
        var endMode = Value<RecurrenceEndMode>(_endMode);
        _until.Visible = endMode is RecurrenceEndMode.OnDate;
        _count.Visible = endMode is RecurrenceEndMode.AfterCount;
        _endPanel.Controls[^1].Visible = endMode is RecurrenceEndMode.AfterCount;
    }

    private static void Add(TableLayoutPanel layout, string label, Control control)
    {
        if (label.Length > 0)
        {
            var labelRow = layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(new Label { Text = L.T(label), AutoSize = true, ForeColor = Muted, Margin = new Padding(0, 4, 0, 5) }, 0, labelRow);
        }
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 0, 0, 7);
        layout.Controls.Add(control, 0, row);
    }

    private static FlowLayoutPanel Row() => new() { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Margin = Padding.Empty };

    private static Label MutedLabel(string text) => new()
    {
        Text = L.T(text),
        AutoSize = true,
        ForeColor = Muted,
        Padding = new Padding(0, 7, 2, 0)
    };

    private static ComboBox Combo(int width = 220) => new ModernComboBox
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.White,
        Width = width
    };

    private static IEnumerable<(string Text, DayOfWeek Day)> DayOptions()
    {
        yield return ("周一", DayOfWeek.Monday);
        yield return ("周二", DayOfWeek.Tuesday);
        yield return ("周三", DayOfWeek.Wednesday);
        yield return ("周四", DayOfWeek.Thursday);
        yield return ("周五", DayOfWeek.Friday);
        yield return ("周六", DayOfWeek.Saturday);
        yield return ("周日", DayOfWeek.Sunday);
    }

    private static void AddOption<T>(ComboBox box, string text, T value)
    {
        box.Items.Add(new Option<T>(text, value));
        if (box.SelectedIndex < 0) box.SelectedIndex = 0;
    }

    private static T Value<T>(ComboBox box) => ((Option<T>)box.SelectedItem!).Value;

    private static void SelectValue<T>(ComboBox box, T value)
    {
        for (var i = 0; i < box.Items.Count; i++)
        {
            if (box.Items[i] is Option<T> option && EqualityComparer<T>.Default.Equals(option.Value, value))
            {
                box.SelectedIndex = i;
                return;
            }
        }
    }

    private sealed record Option<T>(string Text, T Value)
    {
        public override string ToString() => L.T(Text);
    }
}
