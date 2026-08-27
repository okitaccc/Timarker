using Timarker.Models;

namespace Timarker;

public sealed class EventEditForm : Form
{
    private static Color AppBack => AppTheme.AppBack;
    private static Color TextMain => AppTheme.Text;
    private static Color TextMuted => AppTheme.Muted;
    private static Color Accent => UiTokens.Primary;
    private static Color Border => AppTheme.Border;
    private readonly EventItem _item;
    private readonly bool _projectStep;
    private readonly ModernTextBox _title = new() { Dock = DockStyle.Fill };
    private readonly ModernTextBox _notes = new() { Dock = DockStyle.Fill, Multiline = true, Height = 86 };
    private readonly TagEditor _tags = new() { Height = 124 };
    private readonly CommonTagPicker _categories = new();
    private readonly ComboBox _type = new ModernComboBox { Dock = DockStyle.Fill };
    private readonly ComboBox _status = new ModernComboBox { Dock = DockStyle.Fill };
    private readonly RecurrenceEditor _recurrence = new();
    private readonly ModernTextBox _subjectName = new() { Dock = DockStyle.Fill, PlaceholderText = "例如：妈妈、我们、入职" };
    private readonly ModernTextBox _relationship = new() { Dock = DockStyle.Fill, PlaceholderText = "例如：家人、伴侣、工作" };
    private readonly ComboBox _birthdayCalendar = new ModernComboBox { Dock = DockStyle.Fill };
    private readonly ComboBox _birthdayLeapDayRule = new ModernComboBox { Dock = DockStyle.Fill };
    private readonly CheckBox _birthdayYearKnown = new() { Text = "显示年龄（已知出生年份）", Checked = true, AutoSize = true };
    private readonly CheckBox _birthdayIsLeapMonth = new() { Text = "这是闰月生日", AutoSize = true };
    private readonly ComboBox _anniversaryMode = new ModernComboBox { Dock = DockStyle.Fill };
    private readonly ModernTextBox _milestoneDays = new() { Dock = DockStyle.Fill, PlaceholderText = "例如：10, 100, 365, 520, 1000" };
    private readonly CheckBox _hasStart = new() { Text = "开始时间" };
    private readonly Button _dateRange = new ModernButton() { Dock = DockStyle.Fill, Height = 38 };
    private readonly DateTimePicker _startDate = DateBox();
    private readonly ModernNumericUpDown _startHour = NumberBox(23);
    private readonly ModernNumericUpDown _startMinute = NumberBox(59);
    private readonly CheckBox _hasDeadline = new() { Text = "截止时间" };
    private readonly DateTimePicker _deadlineDate = DateBox();
    private readonly ModernNumericUpDown _deadlineHour = NumberBox(23);
    private readonly ModernNumericUpDown _deadlineMinute = NumberBox(59);
    private readonly ModernNumericUpDown _reminderLead = ReminderBox(365);
    private readonly ComboBox _reminderLeadUnit = new ModernComboBox { Width = 72 };
    private readonly ModernNumericUpDown _reminderRepeatMinutes = ReminderBox(365, 10, 1);
    private readonly ComboBox _reminderRepeatUnit = new ModernComboBox { Width = 72 };
    private readonly ModernNumericUpDown _reminderRepeatCount = ReminderBox(20);
    private readonly CheckBox _reminderRepeatEnabled = new() { Text = "再次提醒", AutoSize = true };
    private readonly CheckBox _reminderEnabled = new() { Text = "需要提醒", AutoSize = true };
    private Control? _occasionPanel;
    private Control? _birthdayPanel;
    private Control? _anniversaryPanel;
    private Label? _anniversarySection;
    private Control? _repeatPanel;
    private string _baseline = "";
    private bool _allowClose;

    public EventEditForm(EventItem item, bool projectStep = false)
    {
        _item = item;
        _projectStep = projectStep;
        Text = projectStep ? "编辑项目步骤" : "编辑事件";
        Width = 700;
        Height = 780;
        MinimumSize = new Size(620, 680);
        StartPosition = FormStartPosition.CenterParent;
        Font = UiTokens.Font();
        BackColor = AppBack;

        BuildUi();
        LoadItem();
        L.Apply(this);
        _baseline = Fingerprint();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_allowClose && DialogResult != DialogResult.OK && Fingerprint() != _baseline)
        {
            var result = MessageBox.Show("事件有尚未保存的修改。是否保存后关闭？", "保存事件修改", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Cancel) e.Cancel = true;
            else if (result == DialogResult.Yes)
            {
                e.Cancel = true;
                BeginInvoke((MethodInvoker)SaveAndClose);
            }
            else _allowClose = true;
        }
        base.OnFormClosing(e);
    }

    private void BuildUi()
    {
        AddOption(_type, "一次性事项", EventType.StartAt);
        AddOption(_type, "截止事项", EventType.Deadline);
        AddOption(_type, "时间段", EventType.TimeWindow);
        AddOption(_type, "当天内完成", EventType.AnytimeToday);
        if (!_projectStep)
        {
            AddOption(_type, "周期事项", EventType.Recurring);
            AddOption(_type, "习惯", EventType.Habit);
            AddOption(_type, "生日提醒", EventType.Birthday);
            AddOption(_type, "纪念日", EventType.Anniversary);
        }
        AddOption(_type, "可能要做", EventType.Maybe);

        AddOption(_status, "待处理", EventStatus.Pending);
        AddOption(_status, "进行中", EventStatus.InProgress);
        AddOption(_status, "已完成", EventStatus.Done);
        AddOption(_status, "已跳过", EventStatus.Skipped);
        AddOption(_status, "已延期", EventStatus.Postponed);
        AddOption(_status, "已逾期", EventStatus.Overdue);
        AddOption(_status, "已取消", EventStatus.Cancelled);
        AddOption(_anniversaryMode, "累计天数", AnniversaryMode.Days);
        AddOption(_anniversaryMode, "周年", AnniversaryMode.Years);
        AddOption(_anniversaryMode, "天数与周年", AnniversaryMode.Both);
        AddOption(_birthdayCalendar, "阳历", CalendarKind.Solar);
        AddOption(_birthdayCalendar, "阴历", CalendarKind.Lunar);
        AddOption(_birthdayLeapDayRule, "按 2 月 28 日提醒", LeapDayRule.February28);
        AddOption(_birthdayLeapDayRule, "按 3 月 1 日提醒", LeapDayRule.March1);
        foreach (var unit in new[] { _reminderLeadUnit, _reminderRepeatUnit })
        {
            AddOption(unit, "分钟", ReminderUnit.Minute);
            AddOption(unit, "小时", ReminderUnit.Hour);
            AddOption(unit, "天", ReminderUnit.Day);
        }
        foreach (var control in new Control[] { _title, _notes, _type, _status, _subjectName, _relationship, _birthdayCalendar, _birthdayLeapDayRule, _anniversaryMode, _milestoneDays })
        {
            control.BackColor = UiTokens.Surface;
            if (control is ComboBox combo) combo.FlatStyle = FlatStyle.Flat;
        }
        _dateRange.FlatStyle = FlatStyle.Flat;
        _dateRange.BackColor = Color.FromArgb(239, 246, 255);
        _dateRange.ForeColor = Accent;
        _dateRange.FlatAppearance.BorderColor = Color.FromArgb(147, 197, 253);

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(22, 18, 22, 18),
            BackColor = AppBack
        };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Margin = Padding.Empty };
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        header.Controls.Add(new Label { Text = _projectStep ? "编辑项目步骤" : "编辑事件", Dock = DockStyle.Fill, Font = new Font(Font.FontFamily, 16F, FontStyle.Bold), ForeColor = TextMain });
        header.Controls.Add(new Label { Text = _projectStep ? "步骤会同步显示在时间线、日历与提醒中。" : "修改后会同步更新日历、收藏夹与提醒。", Dock = DockStyle.Fill, ForeColor = TextMuted });
        shell.Controls.Add(header, 0, 0);

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = UiTokens.Surface, Padding = new Padding(22, 12, 22, 18) };
        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 0,
            Margin = Padding.Empty,
            BackColor = UiTokens.Surface
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddSection(form, "基本信息");
        AddField(form, "事项名称", _title);
        AddWide(form, TwoFields(("事项类型", (Control)_type), ("当前状态", _status)));
        _repeatPanel = _recurrence;
        AddWide(form, _repeatPanel);

        AddSection(form, "日期与提醒");
        AddField(form, "日期范围", _dateRange);
        var selectedDates = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false, Margin = Padding.Empty };
        selectedDates.Controls.Add(_hasStart);
        selectedDates.Controls.Add(_hasDeadline);
        AddWide(form, selectedDates);
        AddField(form, "提醒策略", ReminderPanel());

        _occasionPanel = TwoFields(("人物或纪念对象", (Control)_subjectName), ("关系或类别", _relationship));
        AddWide(form, _occasionPanel);
        _birthdayPanel = TwoFields(
            ("生日历法", (Control)_birthdayCalendar),
            ("2 月 29 日在非闰年", _birthdayLeapDayRule));
        AddWide(form, _birthdayPanel);
        var birthdayChecks = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false };
        birthdayChecks.Controls.Add(_birthdayYearKnown);
        birthdayChecks.Controls.Add(_birthdayIsLeapMonth);
        AddWide(form, birthdayChecks);

        AddSection(form, "词条与备注");
        AddField(form, "常用词条", _categories);
        AddField(form, "自定义词条", _tags);
        AddField(form, "备注", _notes);

        _anniversarySection = AddSection(form, "纪念日设置");
        _anniversaryPanel = TwoFields(("显示方式", (Control)_anniversaryMode), ("里程碑天数", _milestoneDays));
        AddWide(form, _anniversaryPanel);

        scroll.Controls.Add(form);
        shell.Controls.Add(scroll, 0, 1);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 10, 0, 0) };
        var cancel = ActionButton("取消", false);
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(cancel);
        var save = ActionButton("保存修改", true);
        save.Click += (_, _) => SaveAndClose();
        buttons.Controls.Add(save);
        shell.Controls.Add(buttons, 0, 2);

        _hasStart.CheckedChanged += (_, _) => { SetEnabled([_startDate, _startHour, _startMinute], _hasStart.Checked); RefreshDateRangeText(); };
        _hasDeadline.CheckedChanged += (_, _) => { SetEnabled([_deadlineDate, _deadlineHour, _deadlineMinute], _hasDeadline.Checked); RefreshDateRangeText(); };
        _type.SelectedIndexChanged += (_, _) => UpdateAnniversaryControls();
        _birthdayCalendar.SelectedIndexChanged += (_, _) => UpdateAnniversaryControls();
        _dateRange.Click += (_, _) => OpenDateRangePicker();
        _reminderRepeatEnabled.CheckedChanged += (_, _) => UpdateReminderRepeatControls();
        _reminderEnabled.CheckedChanged += (_, _) => UpdateReminderControls();

        AcceptButton = save;
        CancelButton = cancel;
        Controls.Add(shell);
    }

    private void LoadItem()
    {
        _title.Text = _item.Title;
        _notes.Text = _item.Notes;
        _tags.TextValue = NormalizeTags($"{_item.Tags}, {_item.Categories}");
        SetCategories($"{_item.Tags}, {_item.Categories}");
        SelectComboValue(_type, _item.Type);
        SelectComboValue(_status, _item.Status);
        _recurrence.LoadFrom(_item);
        SelectComboValue(_anniversaryMode, _item.AnniversaryMode);
        SelectComboValue(_birthdayCalendar, _item.BirthdayCalendar);
        SelectComboValue(_birthdayLeapDayRule, _item.BirthdayLeapDayRule);
        _subjectName.Text = _item.SubjectName;
        _relationship.Text = _item.Relationship;
        _birthdayYearKnown.Checked = _item.BirthdayYearKnown;
        _birthdayIsLeapMonth.Checked = _item.BirthdayIsLeapMonth;
        _milestoneDays.Text = _item.MilestoneDays;
        _hasStart.Checked = _item.StartAt is not null;
        _hasDeadline.Checked = _item.DeadlineAt is not null;
        SetTime(_item.StartAt ?? DateTime.Now, _startDate, _startHour, _startMinute);
        SetTime(_item.DeadlineAt ?? DateTime.Now, _deadlineDate, _deadlineHour, _deadlineMinute);
        SetReminderDuration(_reminderLead, _reminderLeadUnit, _item.ReminderLeadMinutes);
        SetReminderDuration(_reminderRepeatMinutes, _reminderRepeatUnit, _item.ReminderRepeatMinutes);
        _reminderRepeatCount.Value = Math.Min(_reminderRepeatCount.Maximum, Math.Max(_reminderRepeatCount.Minimum, _item.ReminderRepeatCount));
        _reminderRepeatEnabled.Checked = _item.ReminderRepeatCount > 0;
        _reminderEnabled.Checked = _item.ReminderEnabled;
        UpdateReminderControls();
        UpdateReminderRepeatControls();
        SetEnabled([_startDate, _startHour, _startMinute], _hasStart.Checked);
        SetEnabled([_deadlineDate, _deadlineHour, _deadlineMinute], _hasDeadline.Checked);
        UpdateAnniversaryControls();
        RefreshDateRangeText();
    }

    private void SaveAndClose()
    {
        var selectedType = SelectedValue<EventType>(_type);
        if (string.IsNullOrWhiteSpace(_title.Text) && !string.IsNullOrWhiteSpace(_subjectName.Text))
        {
            _title.Text = selectedType is EventType.Birthday
                ? $"{_subjectName.Text.Trim()}的生日"
                : selectedType is EventType.Anniversary
                    ? $"{_subjectName.Text.Trim()}纪念日"
                    : "";
        }
        if (string.IsNullOrWhiteSpace(_title.Text))
        {
            MessageBox.Show("事项不能为空。");
            return;
        }

        DateTime? startAt = _hasStart.Checked ? BuildTime(_startDate, _startHour, _startMinute) : null;
        DateTime? deadlineAt = _hasDeadline.Checked ? BuildTime(_deadlineDate, _deadlineHour, _deadlineMinute) : null;
        if (startAt is not null && deadlineAt is not null && deadlineAt < startAt)
        {
            MessageBox.Show("截止时间不能早于开始时间。", "时间设置有误", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (selectedType is EventType.Recurring or EventType.Habit or EventType.AnytimeToday
            && !_recurrence.ApplyTo(_item, startAt, out var recurrenceError))
        {
            MessageBox.Show(recurrenceError, "重复规则有误", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _item.Title = _title.Text.Trim();
        _item.Notes = _notes.Text.Trim();
        _item.Tags = NormalizeTags($"{_tags.TextValue}, {SelectedCategories()}");
        _item.Categories = "";
        _item.Type = selectedType;
        _item.Status = SelectedValue<EventStatus>(_status);
        if (_item.Type is not (EventType.Recurring or EventType.Habit or EventType.AnytimeToday)) _item.RepeatUnit = RepeatUnit.None;
        _item.AnniversaryMode = SelectedValue<AnniversaryMode>(_anniversaryMode);
        _item.MilestoneDays = _milestoneDays.Text.Trim();
        _item.SubjectName = _subjectName.Text.Trim();
        _item.Relationship = _relationship.Text.Trim();
        _item.BirthdayCalendar = SelectedValue<CalendarKind>(_birthdayCalendar);
        _item.BirthdayYearKnown = _birthdayYearKnown.Checked;
        _item.BirthdayIsLeapMonth = _birthdayIsLeapMonth.Checked && _item.BirthdayCalendar is CalendarKind.Lunar;
        _item.BirthdayLeapDayRule = SelectedValue<LeapDayRule>(_birthdayLeapDayRule);
        _item.StartAt = startAt;
        _item.DeadlineAt = deadlineAt;
        _item.ReminderLeadMinutes = ReminderMinutes(_reminderLead, _reminderLeadUnit);
        _item.ReminderEnabled = _reminderEnabled.Checked;
        _item.ReminderRepeatMinutes = ReminderMinutes(_reminderRepeatMinutes, _reminderRepeatUnit);
        _item.ReminderRepeatCount = _reminderRepeatEnabled.Checked ? (int)_reminderRepeatCount.Value : 0;
        if (_item.Type is EventType.Anniversary)
        {
            _item.DeadlineAt = null;
            _item.RepeatUnit = RepeatUnit.None;
        }
        else if (_item.Type is EventType.Birthday && _item.StartAt is not null)
        {
            var birthday = _item.StartAt.Value;
            _item.DeadlineAt = null;
            _item.RepeatUnit = RepeatUnit.Year;
            if (_item.BirthdayCalendar is CalendarKind.Lunar)
            {
                var calendar = new System.Globalization.ChineseLunisolarCalendar();
                var lunarYear = calendar.GetYear(birthday);
                var calendarMonth = calendar.GetMonth(birthday);
                var leapMonth = calendar.GetLeapMonth(lunarYear);
                _item.BirthdayMonth = leapMonth > 0 && calendarMonth >= leapMonth ? calendarMonth - 1 : calendarMonth;
                _item.BirthdayDay = calendar.GetDayOfMonth(birthday);
            }
            else
            {
                _item.BirthdayMonth = birthday.Month;
                _item.BirthdayDay = birthday.Day;
                _item.BirthdayIsLeapMonth = false;
            }
        }
        _item.NormalizeAfterLoad();
        _item.UpdatedAt = DateTime.Now;
        _allowClose = true;
        DialogResult = DialogResult.OK;
    }

    private string Fingerprint() => string.Join('|',
        _title.Text, _notes.Text, _tags.TextValue, _categories.SelectedText,
        _type.SelectedIndex, _status.SelectedIndex, _recurrence.StateFingerprint(),
        _subjectName.Text, _relationship.Text, _birthdayCalendar.SelectedIndex,
        _birthdayLeapDayRule.SelectedIndex, _birthdayYearKnown.Checked, _birthdayIsLeapMonth.Checked,
        _anniversaryMode.SelectedIndex, _milestoneDays.Text,
        _hasStart.Checked, _startDate.Value.Date.Ticks, _startHour.Value, _startMinute.Value,
        _hasDeadline.Checked, _deadlineDate.Value.Date.Ticks, _deadlineHour.Value, _deadlineMinute.Value,
        _reminderLead.Value, _reminderLeadUnit.SelectedIndex, _reminderRepeatEnabled.Checked, _reminderRepeatMinutes.Value,
        _reminderRepeatUnit.SelectedIndex, _reminderRepeatCount.Value, _reminderEnabled.Checked);

    private static Label Label(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Margin = new Padding(0, 8, 0, 4)
    };

    private static Label AddSection(TableLayoutPanel form, string text)
    {
        var row = form.RowCount++;
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var label = new Label
        {
            Text = text,
            AutoSize = true,
            Font = UiTokens.Font(UiTokens.TextEmphasis, FontStyle.Bold),
            ForeColor = TextMain,
            Margin = new Padding(0, row == 0 ? 2 : 16, 0, 8)
        };
        form.Controls.Add(label, 0, row);
        return label;
    }

    private static void AddField(TableLayoutPanel form, string label, Control control)
    {
        var labelRow = form.RowCount++;
        var controlRow = form.RowCount++;
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        form.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = TextMuted, Margin = new Padding(0, 5, 0, 4) }, 0, labelRow);
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 0, 0, 9);
        form.Controls.Add(control, 0, controlRow);
    }

    private static void AddWide(TableLayoutPanel form, Control control)
    {
        var row = form.RowCount++;
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 0, 0, 9);
        form.Controls.Add(control, 0, row);
    }

    private static Control TwoFields(params (string Label, Control Control)[] fields)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = fields.Length, RowCount = 2, Margin = Padding.Empty };
        for (var i = 0; i < fields.Length; i++)
        {
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / fields.Length));
            panel.Controls.Add(new Label { Text = fields[i].Label, AutoSize = true, ForeColor = TextMuted, Margin = new Padding(i == 0 ? 0 : 8, 4, 0, 4) }, i, 0);
            fields[i].Control.Dock = DockStyle.Fill;
            fields[i].Control.Margin = new Padding(i == 0 ? 0 : 8, 0, 0, 0);
            panel.Controls.Add(fields[i].Control, i, 1);
        }
        return panel;
    }

    private static Button ActionButton(string text, bool primary)
    {
        var button = new ModernButton
        {
            Text = text,
            Width = primary ? 104 : 84,
            Height = 36,
            BackColor = primary ? Accent : UiTokens.Surface,
            ForeColor = primary ? Color.White : TextMain,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(8, 0, 0, 0)
        };
        button.FlatAppearance.BorderColor = Border;
        return button;
    }

    private static Control TimePanel(ModernNumericUpDown hour, ModernNumericUpDown minute)
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        panel.Controls.Add(hour);
        panel.Controls.Add(Label("时"));
        panel.Controls.Add(minute);
        panel.Controls.Add(Label("分"));
        return panel;
    }

    private void OpenDateRangePicker()
    {
        using var picker = new DateRangePickerDialog(
            _startDate.Value,
            _deadlineDate.Value,
            _hasStart.Checked,
            _hasDeadline.Checked,
            new TimeSpan((int)_startHour.Value, (int)_startMinute.Value, 0),
            new TimeSpan((int)_deadlineHour.Value, (int)_deadlineMinute.Value, 0));
        if (picker.ShowDialog(this) != DialogResult.OK) return;
        if (picker.HasStart) _startDate.Value = picker.StartDate;
        if (picker.HasEnd) _deadlineDate.Value = picker.EndDate;
        _hasStart.Checked = picker.HasStart;
        _hasDeadline.Checked = picker.HasEnd;
        _startHour.Value = picker.StartTime.Hours;
        _startMinute.Value = picker.StartTime.Minutes;
        _deadlineHour.Value = picker.EndTime.Hours;
        _deadlineMinute.Value = picker.EndTime.Minutes;
        RefreshDateRangeText();
    }

    private void RefreshDateRangeText()
    {
        var start = $"{_startDate.Value:yyyy-MM-dd} {LunarDate.FullText(_startDate.Value)}  {_startHour.Value:00}:{_startMinute.Value:00}";
        var end = $"{_deadlineDate.Value:yyyy-MM-dd} {LunarDate.FullText(_deadlineDate.Value)}  {_deadlineHour.Value:00}:{_deadlineMinute.Value:00}";
        _dateRange.Text = (_hasStart.Checked, _hasDeadline.Checked) switch
        {
            (true, true) => $"{start}  →  {end}",
            (true, false) => start,
            (false, true) => end,
            _ => "选择日期"
        };
    }

    private static DateTimePicker DateBox() => new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "yyyy-MM-dd",
        Width = 130
    };

    private Control ReminderPanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 4, RowCount = 5, Margin = Padding.Empty };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.Controls.Add(_reminderEnabled, 0, 0);
        panel.SetColumnSpan(_reminderEnabled, 4);
        AddReminderRow(panel, 1, "提前提醒", _reminderLead, _reminderLeadUnit);
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(_reminderRepeatEnabled, 0, 2);
        panel.SetColumnSpan(_reminderRepeatEnabled, 4);
        AddReminderRow(panel, 3, "提醒间隔", _reminderRepeatMinutes, _reminderRepeatUnit);
        AddReminderRow(panel, 4, "最多提醒", _reminderRepeatCount, ReminderLabel("次"));
        return panel;
    }

    private static Label ReminderLabel(string text) => new() { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = Padding.Empty };

    private static void AddReminderRow(TableLayoutPanel panel, int row, string label, Control value, Control unit)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(ReminderLabel(label), 0, row);
        panel.Controls.Add(value, 1, row);
        unit.Anchor = AnchorStyles.Left;
        panel.Controls.Add(unit, 2, row);
    }

    private void UpdateReminderRepeatControls()
    {
        var enabled = !_reminderEnabled.Visible || _reminderEnabled.Checked;
        _reminderRepeatMinutes.Enabled = enabled && _reminderRepeatEnabled.Checked;
        _reminderRepeatUnit.Enabled = enabled && _reminderRepeatEnabled.Checked;
        _reminderRepeatCount.Enabled = enabled && _reminderRepeatEnabled.Checked;
    }

    private void UpdateReminderControls()
    {
        var enabled = !_reminderEnabled.Visible || _reminderEnabled.Checked;
        _reminderLead.Enabled = enabled;
        _reminderLeadUnit.Enabled = enabled;
        _reminderRepeatEnabled.Enabled = enabled;
        UpdateReminderRepeatControls();
    }

    private static ModernNumericUpDown NumberBox(int max) => new()
    {
        Minimum = 0,
        Maximum = max,
        Width = 78,
        TextAlign = HorizontalAlignment.Center
    };

    private static ModernNumericUpDown ReminderBox(int max, int value = 0, int min = 0) => new()
    {
        Minimum = min,
        Maximum = max,
        Value = value,
        Width = 82,
        TextAlign = HorizontalAlignment.Center
    };

    private static Button Button(string text) => new()
    {
        Text = text,
        Height = 32,
        AutoSize = true,
        Margin = new Padding(8, 8, 0, 0)
    };

    private static DateTime BuildTime(DateTimePicker date, ModernNumericUpDown hour, ModernNumericUpDown minute)
    {
        return date.Value.Date.AddHours((double)hour.Value).AddMinutes((double)minute.Value);
    }

    private static void SetTime(DateTime value, DateTimePicker date, ModernNumericUpDown hour, ModernNumericUpDown minute)
    {
        date.Value = value.Date;
        hour.Value = value.Hour;
        minute.Value = value.Minute;
    }

    private static void SetReminderDuration(ModernNumericUpDown value, ComboBox unit, int minutes)
    {
        var selectedUnit = minutes > 0 && minutes % 1440 == 0 ? ReminderUnit.Day
            : minutes > 0 && minutes % 60 == 0 ? ReminderUnit.Hour
            : ReminderUnit.Minute;
        SelectComboValue(unit, selectedUnit);
        value.Value = Math.Min(value.Maximum, Math.Max(value.Minimum, minutes / UnitMinutes(selectedUnit)));
    }

    private static int ReminderMinutes(ModernNumericUpDown value, ComboBox unit) =>
        (int)value.Value * UnitMinutes(SelectedValue<ReminderUnit>(unit));

    private static int UnitMinutes(ReminderUnit unit) => unit switch
    {
        ReminderUnit.Hour => 60,
        ReminderUnit.Day => 1440,
        _ => 1
    };

    private static void SetEnabled(IEnumerable<Control> controls, bool enabled)
    {
        foreach (var control in controls)
        {
            control.Enabled = enabled;
        }
    }

    private void UpdateAnniversaryControls()
    {
        var type = _type.SelectedItem is Option<EventType> option ? option.Value : EventType.StartAt;
        var birthday = type is EventType.Birthday;
        var anniversary = type is EventType.Anniversary;
        if (_occasionPanel is not null) _occasionPanel.Visible = birthday || anniversary;
        if (_birthdayPanel is not null) _birthdayPanel.Visible = birthday;
        if (_anniversaryPanel is not null) _anniversaryPanel.Visible = anniversary;
        if (_anniversarySection is not null) _anniversarySection.Visible = anniversary;
        if (_repeatPanel is not null) _repeatPanel.Visible = type is EventType.Recurring or EventType.Habit or EventType.AnytimeToday;
        _reminderEnabled.Visible = true;
        UpdateReminderControls();
        _birthdayYearKnown.Visible = birthday;
        _birthdayIsLeapMonth.Visible = birthday;
        _birthdayIsLeapMonth.Enabled = birthday && SelectedValue<CalendarKind>(_birthdayCalendar) is CalendarKind.Lunar;
    }

    private static IEnumerable<string> SplitTags(string text)
    {
        return text
            .Split(new[] { ',', '，', ';', '；', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 0);
    }

    private static string NormalizeTags(string text)
    {
        return string.Join(", ", SplitTags(text).Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private string SelectedCategories()
    {
        return _categories.SelectedText;
    }

    private void SetCategories(string categories)
    {
        _categories.SetSelected(SplitTags(categories));
    }

    private static void AddOption<T>(ComboBox box, string text, T value)
    {
        box.Items.Add(new Option<T>(text, value));
        if (box.SelectedIndex < 0)
        {
            box.SelectedIndex = 0;
        }
    }

    private static T SelectedValue<T>(ComboBox box)
    {
        return ((Option<T>)box.SelectedItem!).Value;
    }

    private static void SelectComboValue<T>(ComboBox box, T value)
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

    private enum ReminderUnit
    {
        Minute,
        Hour,
        Day
    }
}
