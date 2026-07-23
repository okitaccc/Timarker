using Timarker.Models;
using Timarker.Services;
using System.Text.Json;

namespace Timarker;

public sealed class MainForm : Form
{
    private static readonly Size DefaultWindowSize = new(1320, 820);
    private static readonly Color AppBack = Color.FromArgb(246, 247, 251);
    private static readonly Color PanelBack = Color.White;
    private static readonly Color TextMain = Color.FromArgb(31, 41, 55);
    private static readonly Color TextMuted = Color.FromArgb(107, 114, 128);
    private static readonly Color Accent = Color.FromArgb(37, 99, 235);
    private static readonly Color Danger = Color.FromArgb(220, 38, 38);
    private static readonly Color Border = Color.FromArgb(226, 232, 240);

    private readonly EventStore _store;
    private readonly List<EventItem> _events;
    private readonly List<PersonProfile> _people;
    private readonly List<ActivityRecord> _records;
    private readonly List<NoteItem> _notes;
    private readonly SettingsStore _settingsStore = new();
    private readonly AppSettings _settings;
    private readonly NotifyIcon _notifyIcon;
    private readonly ReminderEngine _reminders;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 30_000 };
    private bool _isHandlingReminders;

    private readonly ModernTextBox _titleBox = new() { PlaceholderText = "例如：明天下午三点开会" };
    private readonly ModernTextBox _notesBox = new() { PlaceholderText = "备注，可不填", Multiline = true, Height = 72 };
    private readonly TagEditor _tagsBox = new();
    private readonly CommonTagPicker _categoryBox = new();
    private readonly ComboBox _parentBox = new ModernComboBox();
    private readonly ComboBox _typeBox = new ModernComboBox();
    private readonly ComboBox _priorityBox = new ModernComboBox();
    private readonly ComboBox _calendarBox = new ModernComboBox();
    private readonly ComboBox _anniversaryModeBox = new ModernComboBox();
    private readonly ModernTextBox _milestoneDaysBox = new() { PlaceholderText = "例如：10, 100, 365, 520, 1000" };
    private readonly ModernTextBox _subjectNameBox = new() { PlaceholderText = "例如：妈妈、我们、入职" };
    private readonly ModernTextBox _relationshipBox = new() { PlaceholderText = "例如：家人、伴侣、工作" };
    private readonly CheckBox _birthdayYearKnownBox = new() { Text = "显示年龄（已知出生年份）", Checked = true, AutoSize = true };
    private readonly CheckBox _birthdayLeapMonthBox = new() { Text = "这是闰月生日", AutoSize = true };
    private readonly ComboBox _birthdayLeapDayRuleBox = new ModernComboBox();
    private readonly Button _ordinaryPurposeButton = PurposeButton("普通事项");
    private readonly Button _recurringPurposeButton = PurposeButton("周期事项");
    private readonly Button _birthdayPurposeButton = PurposeButton("生日");
    private readonly Button _anniversaryPurposeButton = PurposeButton("纪念日");
    private readonly ComboBox _templateBox = new ModernComboBox();
    private readonly ComboBox _filterBox = new ModernComboBox { Width = 120 };
    private readonly ModernTextBox _searchBox = new() { PlaceholderText = "搜索标题/词条", Width = 140, AutoSize = false };
    private readonly CheckBox _hasStartBox = new() { Text = "设置开始时间", Checked = true };
    private readonly DateTimePicker _startDateBox = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd", Width = 130 };
    private readonly Button _chooseDateButton = new ModernButton() { Text = "选择日期与时间", Height = 40 };
    private readonly Button _startDateButton = new ModernButton() { Width = 178, Height = 36, TabStop = false };
    private readonly ModernNumericUpDown _startHourBox = TimeNumber(23);
    private readonly ModernNumericUpDown _startMinuteBox = TimeNumber(59);
    private readonly CheckBox _hasDeadlineBox = new() { Text = "设置截止时间" };
    private readonly DateTimePicker _deadlineDateBox = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd", Width = 130 };
    private readonly Button _deadlineDateButton = new ModernButton() { Width = 178, Height = 36, TabStop = false };
    private readonly ModernNumericUpDown _deadlineHourBox = TimeNumber(23);
    private readonly ModernNumericUpDown _deadlineMinuteBox = TimeNumber(59);
    private readonly RecurrenceEditor _recurrenceEditor = new();
    private readonly ModernNumericUpDown _postponeMinutesBox = new() { Minimum = 5, Maximum = 10080, Increment = 5, Value = 10, Width = 82 };
    private readonly ModernNumericUpDown _reminderLeadBox = new() { Minimum = 0, Maximum = 365, Width = 82 };
    private readonly ComboBox _reminderLeadUnitBox = new ModernComboBox { Width = 72 };
    private readonly ModernNumericUpDown _reminderRepeatMinutesBox = new() { Minimum = 1, Maximum = 365, Value = 10, Width = 82 };
    private readonly ComboBox _reminderRepeatUnitBox = new ModernComboBox { Width = 72 };
    private readonly ModernNumericUpDown _reminderRepeatCountBox = new() { Minimum = 0, Maximum = 20, Width = 82 };
    private readonly CheckBox _reminderRepeatEnabledBox = new() { Text = "再次提醒", AutoSize = true };
    private readonly FlowLayoutPanel _selectedTagsPanel = new() { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, BackColor = Color.White };
    private readonly ListBox _eventList = new() { HorizontalScrollbar = false, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = EventCardRenderer.ItemHeight };
    private readonly Panel _leftContent = new() { Dock = DockStyle.Fill };
    private readonly ListBox _currentList = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        DrawMode = DrawMode.OwnerDrawFixed,
        ItemHeight = EventCardRenderer.ItemHeight,
        IntegralHeight = false
    };
    private readonly ListBox _recommendedList = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        DrawMode = DrawMode.OwnerDrawFixed,
        ItemHeight = EventCardRenderer.ItemHeight,
        IntegralHeight = false
    };
    private Control? _editorView;
    private Control? _timelineView;
    private Control? _folderView;
    private ProjectView? _projectView;
    private PeopleView? _peopleView;
    private HistoryView? _historyView;
    private NotesView? _notesView;
    private SettingsForm? _settingsView;
    private TableLayoutPanel? _root;
    private FloatingCountdownForm? _floating;
    private PomodoroForm? _pomodoro;
    private CalendarViewForm? _calendarView;
    private Control? _birthdayOptionsPanel;
    private Control? _anniversaryOptionsPanel;
    private Control? _occasionSubjectPanel;
    private Control? _advancedOptionsPanel;
    private Control? _startTimePanel;
    private Control? _deadlineTimePanel;
    private Control? _repeatOptionsPanel;
    private Button? _moreOptionsButton;
    private string? _autoPurposeTag;
    private Guid? _editingId;
    private Button? _saveButton;
    private Action? _selectEditorNavigation;
    private Action? _selectSettingsNavigation;
    private bool _allowExit;
    private Rectangle _lastNormalBounds;
    private FormWindowState _lastVisibleState = FormWindowState.Normal;

    public MainForm(EventStore store)
    {
        _store = store;
        _events = _store.Load();
        _people = _store.People;
        _records = _store.Records;
        _notes = _store.Notes;
        _settings = _settingsStore.Load();
        L.Use(_settings);
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Text = L.Brand,
            Visible = true
        };
        ConfigureTrayMenu();
        _reminders = new ReminderEngine(_notifyIcon);

        Text = L.Brand;
        Size = DefaultWindowSize;
        MinimumSize = new Size(1100, 640);
        BackColor = AppBack;
        Font = new Font("Microsoft YaHei UI", 9F);
        StartPosition = FormStartPosition.CenterScreen;

        SetDefaultTimeValues();
        BuildUi();
        ApplySettingsToEditorDefaults();
        RefreshList();
        L.Apply(this);
        _lastNormalBounds = new Rectangle(Location, Size);
        SizeChanged += (_, _) => RefreshLayoutAfterResize();
        Resize += (_, _) => RememberWindowBounds();
        Move += (_, _) => RememberWindowBounds();

        _timer.Tick += (_, _) => CheckReminders();
        _timer.Start();
        Shown += (_, _) =>
        {
            if (_store.RecoveryMessage is not null)
            {
                MessageBox.Show(_store.RecoveryMessage, "数据恢复", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            CheckReminders();
        };
    }

    public bool RestartRequested { get; private set; }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _floating?.Close();
        _pomodoro?.Close();
        _calendarView?.Close();
        _settingsView?.Close();
        _notifyIcon.Dispose();
        _timer.Dispose();
        base.OnFormClosed(e);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_allowExit)
        {
            _timer.Stop();
            _notifyIcon.Visible = false;
            base.OnFormClosing(e);
            return;
        }

        _timer.Stop();
        if (!_settings.CloseToTrayPromptDismissed)
        {
            e.Cancel = true;
            ShowCloseBehaviorPrompt();
            return;
        }

        if (_settings.CloseToTray)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        _notifyIcon.Visible = false;
        base.OnFormClosing(e);
    }

    private void ConfigureTrayMenu()
    {
        var menu = new ModernContextMenuStrip();
        menu.Items.Add("显示主窗口", null, (_, _) => RestoreFromTray());
        menu.Items.Add("设置", null, (_, _) => OpenSettings());
        menu.Items.Add("退出", null, (_, _) => ExitApplication());
        _notifyIcon.ContextMenuStrip = menu;
        L.Apply(menu);
        _notifyIcon.DoubleClick += (_, _) => RestoreFromTray();
    }

    private void HideToTray()
    {
        RememberWindowBounds();
        Hide();
        ShowInTaskbar = false;
        _timer.Start();
        _notifyIcon.BalloonTipTitle = L.IsEnglish ? "Timarker is still running" : "事刻仍在运行";
        _notifyIcon.BalloonTipText = L.IsEnglish ? "Reminders continue in the background." : "提醒会继续在后台工作。";
        _notifyIcon.ShowBalloonTip(3000);
    }

    private void RestoreFromTray()
    {
        _timer.Stop();
        if (_lastNormalBounds.Width >= MinimumSize.Width && _lastNormalBounds.Height >= MinimumSize.Height)
        {
            Bounds = _lastNormalBounds;
        }
        else
        {
            Size = DefaultWindowSize;
            CenterToScreen();
        }

        Show();
        ShowInTaskbar = true;
        WindowState = _lastVisibleState is FormWindowState.Minimized
            ? FormWindowState.Normal
            : _lastVisibleState;
        Activate();
        _timer.Start();
    }

    private void ExitApplication()
    {
        _allowExit = true;
        _timer.Stop();
        _notifyIcon.Visible = false;
        Close();
    }

    private void ShowCloseBehaviorPrompt()
    {
        using var dialog = new CloseBehaviorDialog(_settings.CloseToTray ? CloseBehavior.MinimizeToTray : CloseBehavior.Exit);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            _timer.Start();
            return;
        }

        _settings.CloseToTray = dialog.SelectedBehavior is CloseBehavior.MinimizeToTray;
        _settings.CloseToTrayPromptDismissed = dialog.RememberChoice;
        _settingsStore.Save(_settings);

        if (_settings.CloseToTray)
        {
            BeginInvoke((MethodInvoker)HideToTray);
            return;
        }

        ExitApplication();
    }

    private void RememberWindowBounds()
    {
        if (!Visible)
        {
            return;
        }

        if (WindowState is FormWindowState.Normal)
        {
            _lastNormalBounds = Bounds;
            _lastVisibleState = FormWindowState.Normal;
        }
        else if (WindowState is FormWindowState.Maximized)
        {
            _lastVisibleState = FormWindowState.Maximized;
        }
    }

    private void SetDefaultTimeValues()
    {
        var now = DateTime.Now.AddMinutes(10);
        _startDateBox.Value = now.Date;
        _startHourBox.Value = now.Hour;
        _startMinuteBox.Value = now.Minute;
        _deadlineDateBox.Value = now.Date;
        _deadlineHourBox.Value = now.Hour;
        _deadlineMinuteBox.Value = now.Minute;
    }

    private void ApplySettingsToEditorDefaults()
    {
        SetReminderDuration(_reminderLeadBox, _reminderLeadUnitBox, _settings.DefaultReminderLeadMinutes);
        SetReminderDuration(_reminderRepeatMinutesBox, _reminderRepeatUnitBox, _settings.DefaultReminderRepeatMinutes);
        SetNumericValue(_reminderRepeatCountBox, _settings.DefaultReminderRepeatCount);
        _reminderRepeatEnabledBox.Checked = _settings.DefaultReminderRepeatCount > 0;
        UpdateReminderRepeatControls();
    }

    private void UpdateReminderRepeatControls()
    {
        _reminderRepeatMinutesBox.Enabled = _reminderRepeatEnabledBox.Checked;
        _reminderRepeatUnitBox.Enabled = _reminderRepeatEnabledBox.Checked;
        _reminderRepeatCountBox.Enabled = _reminderRepeatEnabledBox.Checked;
    }

    private void UpdateAnniversaryControls()
    {
        var purpose = SelectedValue<EventType>(_typeBox);
        var birthday = purpose is EventType.Birthday;
        var anniversary = purpose is EventType.Anniversary;
        var recurring = purpose is EventType.Recurring or EventType.Habit;
        var ordinary = !birthday && !anniversary && !recurring;
        if (_birthdayOptionsPanel is not null) _birthdayOptionsPanel.Visible = birthday;
        if (_anniversaryOptionsPanel is not null) _anniversaryOptionsPanel.Visible = anniversary;
        if (_occasionSubjectPanel is not null) _occasionSubjectPanel.Visible = birthday || anniversary;
        _birthdayLeapMonthBox.Enabled = birthday && SelectedValue<CalendarKind>(_calendarBox) is CalendarKind.Lunar;
        if (_startTimePanel is not null) _startTimePanel.Visible = true;
        if (_deadlineTimePanel is not null) _deadlineTimePanel.Visible = ordinary;
        if (_repeatOptionsPanel is not null) _repeatOptionsPanel.Visible = recurring;
        _hasStartBox.Visible = ordinary;
        _hasDeadlineBox.Visible = ordinary;
        StylePurposeButton(_ordinaryPurposeButton, ordinary);
        StylePurposeButton(_recurringPurposeButton, recurring);
        StylePurposeButton(_birthdayPurposeButton, birthday);
        StylePurposeButton(_anniversaryPurposeButton, anniversary);
        UpdatePurposeTag(birthday ? "生日" : anniversary ? "纪念日" : recurring ? "周期" : null);
        _chooseDateButton.Text = L.T(birthday ? "选择生日日期与时间"
            : anniversary ? "选择纪念日与时间"
            : recurring ? "选择首次发生日期与时间"
            : "选择日期与时间");
        if (birthday || anniversary || recurring)
        {
            _hasStartBox.Checked = true;
            _hasDeadlineBox.Checked = false;
        }
    }

    private void UpdatePurposeTag(string? tag)
    {
        var tags = SplitTags(_tagsBox.TextValue).ToList();
        if (_autoPurposeTag is not null)
        {
            tags.RemoveAll(value => value.Equals(_autoPurposeTag, StringComparison.OrdinalIgnoreCase));
        }
        if (tag is not null && !tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
        {
            tags.Add(tag);
        }
        _autoPurposeTag = tag;
        _tagsBox.TextValue = string.Join(", ", tags);
        UpdateSelectedTags();
    }

    private static void SetNumericValue(ModernNumericUpDown box, int value)
    {
        box.Value = Math.Min(box.Maximum, Math.Max(box.Minimum, value));
    }

    private void OpenDateRangePicker()
    {
        using var picker = new DateRangePickerDialog(
            _startDateBox.Value,
            _deadlineDateBox.Value,
            _hasStartBox.Checked,
            _hasDeadlineBox.Checked,
            new TimeSpan((int)_startHourBox.Value, (int)_startMinuteBox.Value, 0),
            new TimeSpan((int)_deadlineHourBox.Value, (int)_deadlineMinuteBox.Value, 0));
        if (picker.ShowDialog(this) != DialogResult.OK) return;
        if (picker.HasStart) _startDateBox.Value = picker.StartDate;
        if (picker.HasEnd) _deadlineDateBox.Value = picker.EndDate;
        _hasStartBox.Checked = picker.HasStart;
        _hasDeadlineBox.Checked = picker.HasEnd;
        SetNumericValue(_startHourBox, picker.StartTime.Hours);
        SetNumericValue(_startMinuteBox, picker.StartTime.Minutes);
        SetNumericValue(_deadlineHourBox, picker.EndTime.Hours);
        SetNumericValue(_deadlineMinuteBox, picker.EndTime.Minutes);
        RefreshDateButtons();
    }

    private void RefreshDateButtons()
    {
        _startDateButton.Text = _hasStartBox.Checked ? $"{DateButtonText(_startDateBox.Value)}  ·  {_startHourBox.Value:00}:{_startMinuteBox.Value:00}" : L.T("未选择起始日期");
        _deadlineDateButton.Text = _hasDeadlineBox.Checked ? $"{DateButtonText(_deadlineDateBox.Value)}  ·  {_deadlineHourBox.Value:00}:{_deadlineMinuteBox.Value:00}" : L.T("未选择截止日期");
    }

    private static string DateButtonText(DateTime date) =>
        $"{date:yyyy-MM-dd}  ·  {LunarDate.FullText(date)}";

    private static void SetReminderDuration(ModernNumericUpDown valueBox, ComboBox unitBox, int minutes)
    {
        var unit = minutes > 0 && minutes % 1440 == 0
            ? ReminderUnit.Day
            : minutes > 0 && minutes % 60 == 0
                ? ReminderUnit.Hour
                : ReminderUnit.Minute;
        SelectComboValue(unitBox, unit);
        SetNumericValue(valueBox, minutes / UnitMinutes(unit));
    }

    private static int ReminderMinutes(ModernNumericUpDown valueBox, ComboBox unitBox) =>
        (int)valueBox.Value * UnitMinutes(SelectedValue<ReminderUnit>(unitBox));

    private static int UnitMinutes(ReminderUnit unit) => unit switch
    {
        ReminderUnit.Hour => 60,
        ReminderUnit.Day => 1440,
        _ => 1
    };

    private void RefreshLayoutAfterResize()
    {
        if (!IsHandleCreated)
        {
            return;
        }

        BeginInvoke((MethodInvoker)(() =>
        {
            PerformLayout();
            _leftContent.PerformLayout();
            _eventList.Invalidate();
            _currentList.Invalidate();
            _recommendedList.Invalidate();
            Invalidate(true);
        }));
    }

    private void BuildUi()
    {
        AddOption(_typeBox, "普通事项", EventType.StartAt);
        AddOption(_typeBox, "周期事项", EventType.Recurring);
        AddOption(_typeBox, "生日", EventType.Birthday);
        AddOption(_typeBox, "纪念日", EventType.Anniversary);

        AddOption(_priorityBox, "不设置", EventPriority.None);
        AddOption(_priorityBox, "低", EventPriority.Low);
        AddOption(_priorityBox, "普通", EventPriority.Normal);
        AddOption(_priorityBox, "高", EventPriority.High);

        _reminderRepeatEnabledBox.CheckedChanged += (_, _) => UpdateReminderRepeatControls();

        AddOption(_calendarBox, "阳历", CalendarKind.Solar);
        AddOption(_calendarBox, "阴历", CalendarKind.Lunar);
        AddOption(_birthdayLeapDayRuleBox, "按 2 月 28 日提醒", LeapDayRule.February28);
        AddOption(_birthdayLeapDayRuleBox, "按 3 月 1 日提醒", LeapDayRule.March1);
        AddOption(_anniversaryModeBox, "累计天数", AnniversaryMode.Days);
        AddOption(_anniversaryModeBox, "周年", AnniversaryMode.Years);
        AddOption(_anniversaryModeBox, "天数与周年", AnniversaryMode.Both);

        foreach (var unitBox in new[] { _reminderLeadUnitBox, _reminderRepeatUnitBox })
        {
            AddOption(unitBox, "分钟", ReminderUnit.Minute);
            AddOption(unitBox, "小时", ReminderUnit.Hour);
            AddOption(unitBox, "天", ReminderUnit.Day);
        }

        AddOption(_templateBox, "不使用模板", TimeTemplate.None);
        AddOption(_templateBox, "今天下班前", TimeTemplate.TodayBeforeWorkOff);
        AddOption(_templateBox, "明天上午", TimeTemplate.TomorrowMorning);
        AddOption(_templateBox, "一周后截止", TimeTemplate.NextWeekDeadline);
        AddOption(_templateBox, "每天习惯", TimeTemplate.DailyHabit);
        AddOption(_templateBox, "生日", TimeTemplate.Birthday);
        AddOption(_templateBox, "纪念日", TimeTemplate.Anniversary);

        AddOption(_filterBox, "全部", TimelineFilter.All);
        AddOption(_filterBox, "今天", TimelineFilter.Today);
        AddOption(_filterBox, "本周", TimelineFilter.ThisWeek);
        AddOption(_filterBox, "逾期", TimelineFilter.Overdue);
        AddOption(_filterBox, "待处理", TimelineFilter.Pending);
        AddOption(_filterBox, "进行中", TimelineFilter.InProgress);
        AddOption(_filterBox, "已完成", TimelineFilter.Done);
        AddOption(_filterBox, "生日", TimelineFilter.Birthday);
        AddOption(_filterBox, "可能要做", TimelineFilter.Maybe);

        AddOption(_parentBox, "不加入收藏夹", (Guid?)null);
        foreach (var control in new Control[] { _titleBox, _notesBox, _typeBox, _priorityBox, _calendarBox, _anniversaryModeBox, _milestoneDaysBox, _subjectNameBox, _relationshipBox, _birthdayLeapDayRuleBox, _templateBox, _parentBox, _searchBox })
        {
            StyleInput(control);
        }

        _hasStartBox.CheckedChanged += (_, _) => { SetTimeRowEnabled(StartTimeControls(), _hasStartBox.Checked); RefreshDateButtons(); };
        _hasDeadlineBox.CheckedChanged += (_, _) => { SetTimeRowEnabled(DeadlineTimeControls(), _hasDeadlineBox.Checked); RefreshDateButtons(); };
        _filterBox.SelectedIndexChanged += (_, _) => RefreshList();
        _searchBox.TextChanged += (_, _) => RefreshList();
        _templateBox.SelectedIndexChanged += (_, _) => ApplySelectedTemplate();
        _typeBox.SelectedIndexChanged += (_, _) => UpdateAnniversaryControls();
        _ordinaryPurposeButton.Click += (_, _) => SelectComboValue(_typeBox, EventType.StartAt);
        _recurringPurposeButton.Click += (_, _) => SelectComboValue(_typeBox, EventType.Recurring);
        _birthdayPurposeButton.Click += (_, _) => SelectComboValue(_typeBox, EventType.Birthday);
        _anniversaryPurposeButton.Click += (_, _) => SelectComboValue(_typeBox, EventType.Anniversary);
        _calendarBox.SelectedIndexChanged += (_, _) =>
        {
            _birthdayLeapMonthBox.Enabled = SelectedValue<CalendarKind>(_calendarBox) is CalendarKind.Lunar;
            RefreshDateButtons();
        };
        _chooseDateButton.Click += (_, _) => OpenDateRangePicker();
        _startDateButton.Click += (_, _) => OpenDateRangePicker();
        _deadlineDateButton.Click += (_, _) => OpenDateRangePicker();
        _startDateBox.ValueChanged += (_, _) => RefreshDateButtons();
        _deadlineDateBox.ValueChanged += (_, _) => RefreshDateButtons();
        HookTagUpdates(_categoryBox);
        HookTagUpdates(_tagsBox);
        _eventList.DoubleClick += (_, _) => LoadSelectedForEdit();
        _currentList.DrawItem += DrawSideEventItem;
        _recommendedList.DrawItem += DrawSideEventItem;
        _eventList.MouseClick += EventListMouseClick;
        _currentList.MouseClick += EventListMouseClick;
        _recommendedList.MouseClick += EventListMouseClick;
        AttachEventContextMenu(_eventList);
        AttachEventContextMenu(_currentList);
        AttachEventContextMenu(_recommendedList);
        SetTimeRowEnabled(DeadlineTimeControls(), false);
        _chooseDateButton.BackColor = Color.FromArgb(239, 246, 255);
        _chooseDateButton.ForeColor = Accent;
        _chooseDateButton.FlatAppearance.BorderColor = Color.FromArgb(147, 197, 253);
        _startDateButton.FlatAppearance.BorderColor = Border;
        _deadlineDateButton.FlatAppearance.BorderColor = Border;
        RefreshParentOptions();
        UpdateAnniversaryControls();
        RefreshDateButtons();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(18),
            BackColor = AppBack
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 136));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 440));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root = root;

        _editorView = BuildEditor();
        _leftContent.Controls.Add(_editorView);

        root.Controls.Add(BuildSidebar(), 0, 0);
        root.Controls.Add(_leftContent, 1, 0);
        _timelineView = BuildTimeline();
        root.Controls.Add(_timelineView, 2, 0);
        Controls.Add(root);
    }

    private Control BuildSidebar()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 14,
            Margin = new Padding(0, 0, 16, 0),
            BackColor = AppBack,
            Padding = new Padding(0, 0, 10, 0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        for (var i = 0; i < 4; i++) panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
        for (var i = 0; i < 4; i++) panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
        for (var i = 0; i < 2; i++) panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(SidebarBrand());
        var navigation = new List<Button>();
        void Select(Button selected)
        {
            foreach (var button in navigation) StyleSidebarButton(button, button == selected);
        }
        Button NavigationButton(string text, Action action)
        {
            var button = SidebarButton(text);
            navigation.Add(button);
            button.Click += (_, _) => { Select(button); action(); };
            return button;
        }

        var add = NavigationButton("新建事务", () => ShowLeftView(_editorView!));
        panel.Controls.Add(add);

        var current = NavigationButton("当前事务", () => ShowLeftView(BuildCurrentView()));
        panel.Controls.Add(current);

        var recommended = NavigationButton("推荐优先", () => ShowLeftView(BuildRecommendedView()));
        panel.Controls.Add(recommended);

        var calendar = NavigationButton("日历视图", OpenCalendarView);
        panel.Controls.Add(calendar);
        panel.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = AppBack });

        var projects = NavigationButton("项目", OpenProjectView);
        panel.Controls.Add(projects);

        var folders = NavigationButton("收藏夹", OpenFolderView);
        panel.Controls.Add(folders);

        var history = NavigationButton("记录", OpenHistoryView);
        panel.Controls.Add(history);

        var notes = NavigationButton("便签", OpenNotesView);
        panel.Controls.Add(notes);
        panel.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = AppBack });

        var pomodoro = NavigationButton("番茄钟", OpenPomodoroView);
        panel.Controls.Add(pomodoro);

        var settings = NavigationButton("设置", OpenSettings);
        panel.Controls.Add(settings);

        _selectEditorNavigation = () => Select(add);
        _selectSettingsNavigation = () => Select(settings);
        Select(add);

        return panel;
    }

    private void ShowLeftView(Control view)
    {
        if (_pomodoro is { IsDisposed: false }) _pomodoro.Visible = false;
        if (_calendarView is { IsDisposed: false }) _calendarView.Visible = false;
        if (_projectView is not null) _projectView.Visible = false;
        if (_peopleView is not null) _peopleView.Visible = false;
        if (_historyView is not null) _historyView.Visible = false;
        if (_notesView is not null) _notesView.Visible = false;
        if (_settingsView is { IsDisposed: false }) _settingsView.Visible = false;
        if (_folderView is not null)
        {
            _root?.Controls.Remove(_folderView);
            _folderView.Dispose();
            _folderView = null;
        }
        _leftContent.Visible = true;
        if (_timelineView is not null)
        {
            _timelineView.Visible = true;
        }
        _leftContent.Controls.Clear();
        _leftContent.Controls.Add(view);
    }

    private Control BuildCurrentView()
    {
        _currentList.Items.Clear();
        foreach (var item in CurrentItems())
        {
            _currentList.Items.Add(item);
        }

        return ListViewPanel("当前事务", "今天、进行中、待处理的事项。", _currentList);
    }

    private Control BuildRecommendedView()
    {
        _recommendedList.Items.Clear();
        foreach (var item in RecommendedItems())
        {
            _recommendedList.Items.Add(item);
        }

        return ListViewPanel("推荐优先处理", "逾期和最近到期的事项排在前面。", _recommendedList);
    }

    private Control ListViewPanel(string title, string subtitle, ListBox list)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(18),
            Margin = new Padding(0, 0, 16, 0),
            BackColor = PanelBack
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(Header(title, subtitle));
        list.DoubleClick -= SideListDoubleClick;
        list.DoubleClick += SideListDoubleClick;
        panel.Controls.Add(list);
        return panel;
    }

    private void SideListDoubleClick(object? sender, EventArgs e)
    {
        if (sender is ListBox { SelectedItem: EventItem item })
        {
            EditEventFromDialog(item);
        }
    }

    private void AttachEventContextMenu(ListBox list)
    {
        list.MouseDown += (_, e) =>
        {
            if (e.Button is MouseButtons.Right)
            {
                list.SelectedIndex = list.IndexFromPoint(e.Location);
            }
        };

        var menu = new ModernContextMenuStrip();
        menu.Opening += (_, e) =>
        {
            menu.Items.Clear();
            if (list.SelectedItem is not EventItem item)
            {
                e.Cancel = true;
                return;
            }

            menu.Items.Add("编辑", null, (_, _) => EditEventFromDialog(item));
            if (item.Status is not EventStatus.Cancelled && item.IsRecurringSeries)
            {
                if (!item.IsRecurrencePaused)
                {
                    menu.Items.Add("完成本次", null, (_, _) => SetStatus(item, EventStatus.Done));
                    menu.Items.Add("跳过本次", null, (_, _) => SetStatus(item, EventStatus.Skipped));
                }
                menu.Items.Add(item.IsRecurrencePaused ? "恢复重复" : "暂停重复", null, (_, _) =>
                {
                    item.SetRecurrencePaused(!item.IsRecurrencePaused);
                    SaveAndRefresh();
                });
                menu.Items.Add("结束重复", null, (_, _) =>
                {
                    item.EndRecurringSeries();
                    SaveAndRefresh();
                });
            }
            else if (item.Status is not (EventStatus.Cancelled or EventStatus.Done))
            {
                menu.Items.Add("完成", null, (_, _) => SetStatus(item, EventStatus.Done));
            }

            var addToFolder = new ToolStripMenuItem("加入收藏夹");
            foreach (var folder in _events.Where(x => x.IsGroup).OrderBy(x => x.Title))
            {
                var folderItem = new ToolStripMenuItem(folder.Title) { Checked = item.IsInFolder(folder.Id) };
                folderItem.Click += (_, _) => AddToFolder(item, folder);
                addToFolder.DropDownItems.Add(folderItem);
            }
            if (addToFolder.DropDownItems.Count == 0)
            {
                addToFolder.DropDownItems.Add("暂无收藏夹").Enabled = false;
            }
            menu.Items.Add(addToFolder);
            menu.Items.Add("新建收藏夹并加入", null, (_, _) => CreateFolder(item));

            var groupByTag = new ToolStripMenuItem("根据词条组合");
            foreach (var tag in SplitTags(item.Tags).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                groupByTag.DropDownItems.Add(tag, null, (_, _) => CreateFolderFromTag(tag));
            }
            if (groupByTag.DropDownItems.Count == 0)
            {
                groupByTag.DropDownItems.Add("该事件没有词条").Enabled = false;
            }
            menu.Items.Add(groupByTag);

            var removeFromFolder = new ToolStripMenuItem("从收藏夹移除");
            foreach (var folder in _events.Where(x => x.IsGroup && item.IsInFolder(x.Id)).OrderBy(x => x.Title))
            {
                removeFromFolder.DropDownItems.Add(folder.Title, null, (_, _) => RemoveFromFolder(item, folder));
            }
            if (removeFromFolder.DropDownItems.Count > 0)
            {
                menu.Items.Add(removeFromFolder);
            }

            menu.Items.Add(new ToolStripSeparator());
            var delete = menu.Items.Add("删除", null, (_, _) => DeleteEvent(item));
            delete.ForeColor = Danger;
        };
        list.ContextMenuStrip = menu;
    }

    private void AddToFolder(EventItem item, EventItem folder)
    {
        if (item.IsGroup || item.IsProject || item.Id == folder.Id)
        {
            return;
        }
        item.AddToFolder(folder.Id);
        SaveAndRefresh();
    }

    private void RemoveFromFolder(EventItem item, EventItem folder)
    {
        item.RemoveFromFolder(folder.Id);
        SaveAndRefresh();
    }

    private EventItem? CreateFolder(EventItem? item = null)
    {
        var name = PromptForText("新建收藏夹", "收藏夹名称");
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var folder = new EventItem { Title = name.Trim(), IsGroup = true, Type = EventType.Maybe, Priority = EventPriority.None };
        _events.Add(folder);
        if (item is { IsGroup: false })
        {
            item.AddToFolder(folder.Id);
        }
        SaveAndRefresh();
        return folder;
    }

    private void CreateFolderFromTag(string tag)
    {
        var folder = _events.FirstOrDefault(x => x.IsGroup && x.Title.Equals(tag, StringComparison.OrdinalIgnoreCase));
        if (folder is null)
        {
            folder = new EventItem { Title = tag, IsGroup = true, Type = EventType.Maybe, Priority = EventPriority.None };
            _events.Add(folder);
        }

        foreach (var item in _events.Where(x => !x.IsGroup && !x.IsProject && SplitTags(x.Tags).Contains(tag, StringComparer.OrdinalIgnoreCase)))
        {
            item.AddToFolder(folder.Id);
        }
        SaveAndRefresh();
    }

    private void DeleteEvent(EventItem item)
    {
        if (MessageBox.Show($"确定删除“{item.Title}”吗？", "删除事件", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        if (item.IsProject)
        {
            foreach (var step in _events.Where(x => x.ProjectId == item.Id))
            {
                step.ProjectId = null;
                step.ProjectOrder = 0;
            }
        }
        else if (item.IsGroup)
        {
            foreach (var child in _events.Where(x => x.IsInFolder(item.Id)))
            {
                child.RemoveFromFolder(item.Id);
            }
        }
        _events.Remove(item);
        SaveAndRefresh();
    }

    private static string? PromptForText(string title, string label)
    {
        using var dialog = new Form { Text = title, Width = 380, Height = 170, StartPosition = FormStartPosition.CenterParent, Font = new Font("Microsoft YaHei UI", 9F) };
        var input = new ModernTextBox { Dock = DockStyle.Top };
        var ok = new Button { Text = "确定", DialogResult = DialogResult.OK, Width = 84, Height = 32 };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 84, Height = 32 };
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(18) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(new Label { Text = label, AutoSize = true });
        root.Controls.Add(input);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        root.Controls.Add(buttons);
        dialog.Controls.Add(root);
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;
        return dialog.ShowDialog() == DialogResult.OK ? input.Text : null;
    }

    private Control BuildEditor()
    {
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18, 16, 18, 14),
            Margin = new Padding(0, 0, 16, 0),
            BackColor = PanelBack
        };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        shell.Controls.Add(Header("新建事项", "先填写必要信息，其他设置按需展开。"), 0, 0);

        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = PanelBack,
            Padding = new Padding(0, 0, 8, 0)
        };

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 0,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            BackColor = PanelBack
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddSection(form, "快速创建");
        AddFormRow(form, "事项名称", _titleBox);
        AddFormRow(form, "快速设置", _templateBox);
        AddFormRow(form, "事项用途", PurposeSelector());

        AddSection(form, "日期与提醒");
        AddWideRow(form, _chooseDateButton);
        AddWideRow(form, _hasStartBox);
        _startTimePanel = TimePanel(_startDateButton);
        AddWideRow(form, _startTimePanel);
        AddWideRow(form, _hasDeadlineBox);
        _deadlineTimePanel = TimePanel(_deadlineDateButton);
        AddWideRow(form, _deadlineTimePanel);
        _repeatOptionsPanel = OptionsPanel(("重复规则", (Control)_recurrenceEditor));
        AddWideRow(form, _repeatOptionsPanel);
        _occasionSubjectPanel = OptionsPanel(
            ("人物或纪念对象", (Control)_subjectNameBox),
            ("关系或类别", (Control)_relationshipBox));
        AddWideRow(form, _occasionSubjectPanel);
        _birthdayOptionsPanel = OptionsPanel(
            ("生日历法", (Control)_calendarBox),
            ("出生年份", (Control)_birthdayYearKnownBox),
            ("农历闰月", (Control)_birthdayLeapMonthBox),
            ("2 月 29 日在非闰年", (Control)_birthdayLeapDayRuleBox));
        AddWideRow(form, _birthdayOptionsPanel);
        _anniversaryOptionsPanel = OptionsPanel(
            ("纪念日显示", (Control)_anniversaryModeBox),
            ("里程碑天数", (Control)_milestoneDaysBox));
        AddWideRow(form, _anniversaryOptionsPanel);
        AddFormRow(form, "提醒方式", ReminderPolicyPanel());

        AddSection(form, "词条");
        AddFormRow(form, "常用词条", _categoryBox);
        AddFormRow(form, "自定义词条", _tagsBox);
        AddFormRow(form, "已选择词条", _selectedTagsPanel);

        _moreOptionsButton = SecondaryButton("展开更多设置");
        _moreOptionsButton.Dock = DockStyle.Fill;
        _moreOptionsButton.Click += (_, _) => ToggleAdvancedOptions();
        AddWideRow(form, _moreOptionsButton);
        _advancedOptionsPanel = AdvancedOptionsPanel();
        _advancedOptionsPanel.Visible = false;
        AddWideRow(form, _advancedOptionsPanel);
        UpdateSelectedTags();
        UpdateAnniversaryControls();

        _saveButton = PrimaryButton("添加到事刻");
        _saveButton.Click += (_, _) => SaveEvent();
        _saveButton.Dock = DockStyle.Fill;

        scroll.Controls.Add(form);
        shell.Controls.Add(scroll, 0, 1);
        shell.Controls.Add(_saveButton, 0, 2);
        return shell;
    }

    private Control PurposeSelector()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Height = 50,
            ColumnCount = 4,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = new Padding(4),
            BackColor = Color.FromArgb(248, 250, 252)
        };
        for (var i = 0; i < 4; i++) panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        panel.Controls.Add(_ordinaryPurposeButton, 0, 0);
        panel.Controls.Add(_recurringPurposeButton, 1, 0);
        panel.Controls.Add(_birthdayPurposeButton, 2, 0);
        panel.Controls.Add(_anniversaryPurposeButton, 3, 0);
        panel.Paint += (_, e) => ModernUi.DrawBorder(e.Graphics, panel.ClientRectangle, 12, Border);
        ModernUi.Round(panel, 12);
        return panel;
    }

    private Control AdvancedOptionsPanel()
    {
        var panel = OptionsTable();
        AddSection(panel, "更多设置");
        AddFormRow(panel, "优先级", _priorityBox);
        AddFormRow(panel, "加入收藏夹", FolderSelector());
        AddFormRow(panel, "备注", _notesBox);
        return panel;
    }

    private Control FolderSelector()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Height = 36, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        _parentBox.Dock = DockStyle.Fill;
        _parentBox.Margin = new Padding(0, 0, 8, 0);
        panel.Controls.Add(_parentBox, 0, 0);
        var create = SecondaryButton("新建收藏夹");
        create.Dock = DockStyle.Fill;
        create.Margin = Padding.Empty;
        create.Click += (_, _) =>
        {
            var folder = CreateFolder();
            if (folder is not null) SelectComboValue(_parentBox, (Guid?)folder.Id);
        };
        panel.Controls.Add(create, 1, 0);
        return panel;
    }

    private static Control OptionsPanel(params (string Label, Control Control)[] fields)
    {
        var panel = OptionsTable();
        foreach (var field in fields) AddFormRow(panel, field.Label, field.Control);
        return panel;
    }

    private static TableLayoutPanel OptionsTable()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1, RowCount = 0, Margin = Padding.Empty };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return panel;
    }

    private void ToggleAdvancedOptions()
    {
        if (_advancedOptionsPanel is null || _moreOptionsButton is null) return;
        _advancedOptionsPanel.Visible = !_advancedOptionsPanel.Visible;
        _moreOptionsButton.Text = L.T(_advancedOptionsPanel.Visible ? "收起更多设置" : "展开更多设置");
    }

    private Control BuildTimeline()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18),
            BackColor = PanelBack
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

        panel.Controls.Add(TimelineHeader());

        _eventList.BorderStyle = BorderStyle.None;
        _eventList.BackColor = PanelBack;
        _eventList.ForeColor = TextMain;
        _eventList.Dock = DockStyle.Fill;
        _eventList.DrawItem += DrawEventItem;
        panel.Controls.Add(_eventList);
        panel.Controls.Add(ActionPanel());

        return panel;
    }

    private Control Header(string title, string subtitle)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Margin = new Padding(0, 0, 0, 12)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        panel.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 15F, FontStyle.Bold),
            ForeColor = TextMain
        });
        panel.Controls.Add(new Label
        {
            Text = subtitle,
            Dock = DockStyle.Fill,
            ForeColor = TextMuted
        });
        return panel;
    }

    private Control TimelineHeader()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 12)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 182));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270));
        panel.Controls.Add(Header("今日与未来", "可按状态筛选，也可以打开悬浮倒计时。"), 0, 0);
        var filterPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        filterPanel.Controls.Add(ToolbarLabel("筛选"), 0, 0);
        _filterBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _filterBox.Margin = new Padding(0);
        filterPanel.Controls.Add(_filterBox, 1, 0);

        var searchPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = Padding.Empty };
        searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        searchPanel.Controls.Add(ToolbarLabel("搜索"), 0, 0);
        _searchBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _searchBox.Margin = new Padding(0);
        searchPanel.Controls.Add(_searchBox, 1, 0);
        var searchButton = SecondaryButton("搜索");
        searchButton.AutoSize = false;
        searchButton.Dock = DockStyle.None;
        searchButton.Anchor = AnchorStyles.None;
        searchButton.Width = 68;
        _searchBox.Height = _filterBox.PreferredHeight;
        searchButton.Height = _filterBox.Height;
        searchButton.Margin = Padding.Empty;
        searchButton.Click += (_, _) => RefreshList();
        searchPanel.Controls.Add(searchButton, 2, 0);

        panel.Controls.Add(filterPanel, 1, 0);
        panel.Controls.Add(searchPanel, 2, 0);
        return panel;
    }

    private static Label ToolbarLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = TextMuted,
        Margin = Padding.Empty
    };

    private void UpdateSelectedTags()
    {
        _selectedTagsPanel.Controls.Clear();
        var tags = SplitTags($"{_categoryBox.SelectedText}, {_tagsBox.TextValue}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (tags.Count == 0)
        {
            _selectedTagsPanel.Controls.Add(new Label { Text = "尚未选择词条", AutoSize = true, ForeColor = TextMuted, Margin = new Padding(0, 6, 0, 6) });
            return;
        }

        foreach (var tag in tags)
        {
            var chip = new Label
            {
                Text = $"# {tag}",
                AutoSize = true,
                Padding = new Padding(10, 5, 10, 5),
                Margin = new Padding(0, 0, 6, 6),
                BackColor = Color.FromArgb(239, 246, 255),
                ForeColor = Accent
            };
            ModernUi.Pill(chip);
            _selectedTagsPanel.Controls.Add(chip);
        }
    }

    private void HookTagUpdates(Control root)
    {
        root.Click += (_, _) => BeginInvoke((MethodInvoker)UpdateSelectedTags);
        root.TextChanged += (_, _) => BeginInvoke((MethodInvoker)UpdateSelectedTags);
        root.ControlAdded += (_, e) =>
        {
            if (e.Control is not null)
            {
                HookTagUpdates(e.Control);
            }
            BeginInvoke((MethodInvoker)UpdateSelectedTags);
        };
        foreach (Control child in root.Controls)
        {
            HookTagUpdates(child);
        }
    }

    private static Label Label(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 3),
            ForeColor = TextMuted
        };
    }

    private static void AddSection(TableLayoutPanel form, string text)
    {
        var row = form.RowCount++;
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var label = new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
            ForeColor = TextMain,
            Margin = new Padding(0, row == 0 ? 2 : 16, 0, 8)
        };
        form.Controls.Add(label, 0, row);
    }

    private static void AddFormRow(TableLayoutPanel form, string labelText, Control control)
    {
        var labelRow = form.RowCount++;
        var controlRow = form.RowCount++;
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var label = Label(labelText);
        label.Anchor = AnchorStyles.Left;
        label.Margin = new Padding(0, 5, 0, 3);
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 0, 0, 9);
        form.Controls.Add(label, 0, labelRow);
        form.Controls.Add(control, 0, controlRow);
    }

    private static void AddWideRow(TableLayoutPanel form, Control control)
    {
        var row = form.RowCount++;
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 2, 0, 7);
        form.Controls.Add(control, 0, row);
    }

    private static Control TimePanel(Control date)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Height = 40, ColumnCount = 1, RowCount = 1, Margin = new Padding(0, 0, 0, 4) };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        date.Dock = DockStyle.Fill;
        date.Margin = Padding.Empty;
        panel.Controls.Add(date, 0, 0);
        return panel;
    }

    private static Label TimeUnitLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        ForeColor = TextMuted,
        Margin = Padding.Empty
    };

    private Control ReminderPolicyPanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 4, Margin = Padding.Empty };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddPolicyRow(panel, 0, "提前提醒", _reminderLeadBox, _reminderLeadUnitBox);
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(_reminderRepeatEnabledBox, 0, 1);
        panel.SetColumnSpan(_reminderRepeatEnabledBox, 4);
        AddPolicyRow(panel, 2, "提醒间隔", _reminderRepeatMinutesBox, _reminderRepeatUnitBox);
        AddPolicyRow(panel, 3, "最多提醒", _reminderRepeatCountBox, new Label { Text = "次", AutoSize = true });
        return panel;
    }

    private static void AddPolicyRow(TableLayoutPanel panel, int row, string label, Control control, Control unit)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = TextMuted }, 0, row);
        panel.Controls.Add(control, 1, row);
        unit.Anchor = AnchorStyles.Left;
        unit.ForeColor = TextMuted;
        panel.Controls.Add(unit, 2, row);
    }

    private Control ActionPanel()
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 12, 0, 0), WrapContents = false };

        panel.Controls.Add(new Label { Text = "延后", AutoSize = true, Padding = new Padding(6, 8, 0, 0), ForeColor = TextMuted });
        panel.Controls.Add(_postponeMinutesBox);
        panel.Controls.Add(new Label { Text = "分钟", AutoSize = true, Padding = new Padding(0, 8, 4, 0), ForeColor = TextMuted });

        var postpone = SecondaryButton("延期事项");
        postpone.Click += (_, _) => PostponeSelected();
        panel.Controls.Add(postpone);

        return panel;
    }

    private void DrawEventItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || _eventList.Items[e.Index] is not EventItem item)
        {
            return;
        }

        EventCardRenderer.Draw(e.Graphics, e.Bounds, item, Font, (e.State & DrawItemState.Selected) != 0, PanelBack, true);
    }

    private void DrawSideEventItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || sender is not ListBox list || list.Items[e.Index] is not EventItem item)
        {
            return;
        }

        EventCardRenderer.Draw(e.Graphics, e.Bounds, item, Font, (e.State & DrawItemState.Selected) != 0, PanelBack, true);
    }

    private static void DrawText(Graphics graphics, string text, Font font, Brush brush, Rectangle bounds)
    {
        using var format = new StringFormat
        {
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };
        graphics.DrawString(text, font, brush, bounds, format);
    }

    private static IEnumerable<string> SideChips(EventItem item)
    {
        foreach (var tag in SplitTags($"{item.Categories}, {item.Tags}").Distinct(StringComparer.OrdinalIgnoreCase))
        {
            yield return $"#{tag}";
        }
    }

    private void ApplySelectedTemplate()
    {
        var template = SelectedValue<TimeTemplate>(_templateBox);
        if (template is TimeTemplate.None)
        {
            return;
        }

        var now = DateTime.Now;
        _recurrenceEditor.Reset();
        _hasStartBox.Checked = true;
        _hasDeadlineBox.Checked = false;
        ApplySettingsToEditorDefaults();

        switch (template)
        {
            case TimeTemplate.TodayBeforeWorkOff:
                SelectComboValue(_typeBox, EventType.StartAt);
                _hasStartBox.Checked = false;
                _hasDeadlineBox.Checked = true;
                SetTimeBoxes(now.Date.AddHours(18), _deadlineDateBox, _deadlineHourBox, _deadlineMinuteBox);
                SetReminderDuration(_reminderLeadBox, _reminderLeadUnitBox, 30);
                break;
            case TimeTemplate.TomorrowMorning:
                SelectComboValue(_typeBox, EventType.StartAt);
                SetTimeBoxes(now.Date.AddDays(1).AddHours(9), _startDateBox, _startHourBox, _startMinuteBox);
                break;
            case TimeTemplate.NextWeekDeadline:
                SelectComboValue(_typeBox, EventType.StartAt);
                _hasStartBox.Checked = false;
                _hasDeadlineBox.Checked = true;
                SetTimeBoxes(now.Date.AddDays(7).AddHours(18), _deadlineDateBox, _deadlineHourBox, _deadlineMinuteBox);
                SetReminderDuration(_reminderLeadBox, _reminderLeadUnitBox, 1440);
                break;
            case TimeTemplate.DailyHabit:
                SelectComboValue(_typeBox, EventType.Recurring);
                SetTimeBoxes(now.Date.AddDays(1).AddHours(9), _startDateBox, _startHourBox, _startMinuteBox);
                _recurrenceEditor.SetSimple(RepeatUnit.Day, 1);
                break;
            case TimeTemplate.Birthday:
                SelectComboValue(_typeBox, EventType.Birthday);
                SetTimeBoxes(now.Date.AddHours(9), _startDateBox, _startHourBox, _startMinuteBox);
                SetReminderDuration(_reminderLeadBox, _reminderLeadUnitBox, 5 * 1440);
                break;
            case TimeTemplate.Anniversary:
                SelectComboValue(_typeBox, EventType.Anniversary);
                SetTimeBoxes(now.Date.AddHours(9), _startDateBox, _startHourBox, _startMinuteBox);
                SelectComboValue(_anniversaryModeBox, AnniversaryMode.Both);
                _milestoneDaysBox.Text = "10, 100, 365, 520, 1000";
                SetReminderDuration(_reminderLeadBox, _reminderLeadUnitBox, 5 * 1440);
                break;
        }
    }

    private void SaveEvent()
    {
        var selectedPurpose = SelectedValue<EventType>(_typeBox);
        if (string.IsNullOrWhiteSpace(_titleBox.Text) && !string.IsNullOrWhiteSpace(_subjectNameBox.Text))
        {
            _titleBox.Text = selectedPurpose is EventType.Birthday
                ? $"{_subjectNameBox.Text.Trim()}的生日"
                : selectedPurpose is EventType.Anniversary
                    ? $"{_subjectNameBox.Text.Trim()}纪念日"
                    : "";
        }
        if (string.IsNullOrWhiteSpace(_titleBox.Text))
        {
            MessageBox.Show("先写一下要提醒的事项。", "缺少事项", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var purpose = selectedPurpose;
        DateTime? startAt = _hasStartBox.Checked ? BuildTime(_startDateBox, _startHourBox, _startMinuteBox) : null;
        DateTime? deadlineAt = _hasDeadlineBox.Checked ? BuildTime(_deadlineDateBox, _deadlineHourBox, _deadlineMinuteBox) : null;
        var type = purpose is EventType.Birthday or EventType.Anniversary
            ? purpose
            : purpose is EventType.Recurring or EventType.Habit
                ? EventType.Recurring
            : startAt is not null && deadlineAt is not null
                ? EventType.TimeWindow
                : deadlineAt is not null
                    ? EventType.Deadline
                    : EventType.StartAt;
        if (startAt is not null && deadlineAt is not null && deadlineAt < startAt)
        {
            MessageBox.Show("截止时间不能早于开始时间。", "时间设置有误", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var item = _editingId is null
            ? new EventItem()
            : _events.FirstOrDefault(e => e.Id == _editingId.Value) ?? new EventItem();
        item.Title = _titleBox.Text.Trim();
        item.Notes = _notesBox.Text.Trim();
        item.Tags = NormalizeTags($"{_tagsBox.TextValue}, {SelectedCategories()}");
        item.Categories = "";
        item.IsGroup = false;
        var selectedFolderId = SelectedValue<Guid?>(_parentBox);
        if (selectedFolderId is not null)
        {
            item.AddToFolder(selectedFolderId.Value);
        }
        item.Type = type;
        item.Priority = SelectedValue<EventPriority>(_priorityBox);
        item.StartAt = startAt;
        item.DeadlineAt = deadlineAt;
        if (type is EventType.Recurring or EventType.Habit)
        {
            if (!_recurrenceEditor.ApplyTo(item, startAt, out var recurrenceError))
            {
                MessageBox.Show(recurrenceError, "重复规则有误", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
        }
        else
        {
            item.RepeatUnit = RepeatUnit.None;
        }
        item.BirthdayCalendar = SelectedValue<CalendarKind>(_calendarBox);
        item.BirthdayYearKnown = _birthdayYearKnownBox.Checked;
        item.BirthdayIsLeapMonth = _birthdayLeapMonthBox.Checked && item.BirthdayCalendar is CalendarKind.Lunar;
        item.BirthdayLeapDayRule = SelectedValue<LeapDayRule>(_birthdayLeapDayRuleBox);
        item.SubjectName = _subjectNameBox.Text.Trim();
        item.Relationship = _relationshipBox.Text.Trim();
        ResolvePersonAssociation(item);
        item.AnniversaryMode = SelectedValue<AnniversaryMode>(_anniversaryModeBox);
        item.MilestoneDays = _milestoneDaysBox.Text.Trim();
        item.ReminderLeadMinutes = ReminderMinutes(_reminderLeadBox, _reminderLeadUnitBox);
        item.ReminderRepeatMinutes = ReminderMinutes(_reminderRepeatMinutesBox, _reminderRepeatUnitBox);
        item.ReminderRepeatCount = _reminderRepeatEnabledBox.Checked ? (int)_reminderRepeatCountBox.Value : 0;
        item.UpdatedAt = DateTime.Now;

        if (item.Type is EventType.Birthday)
        {
            var birthday = BuildTime(_startDateBox, _startHourBox, _startMinuteBox);
            item.StartAt = birthday;
            item.DeadlineAt = null;
            item.RepeatUnit = RepeatUnit.Year;
            if (item.BirthdayCalendar is CalendarKind.Lunar)
            {
                var calendar = new System.Globalization.ChineseLunisolarCalendar();
                var lunarYear = calendar.GetYear(birthday);
                var calendarMonth = calendar.GetMonth(birthday);
                var leapMonth = calendar.GetLeapMonth(lunarYear);
                item.BirthdayMonth = leapMonth > 0 && calendarMonth >= leapMonth ? calendarMonth - 1 : calendarMonth;
                item.BirthdayDay = calendar.GetDayOfMonth(birthday);
            }
            else
            {
                item.BirthdayMonth = birthday.Month;
                item.BirthdayDay = birthday.Day;
                item.BirthdayIsLeapMonth = false;
            }
        }

        else if (item.Type is EventType.Anniversary)
        {
            item.StartAt ??= DateTime.Now;
            item.DeadlineAt = null;
            item.RepeatUnit = RepeatUnit.None;
            item.BirthdayMonth = item.StartAt.Value.Month;
            item.BirthdayDay = item.StartAt.Value.Day;
        }

        if (item.Type is EventType.Deadline && item.DeadlineAt is null)
        {
            item.DeadlineAt = item.StartAt ?? DateTime.Now;
        }

        if (_editingId is null)
        {
            _events.Add(item);
        }

        _editingId = null;
        if (_saveButton is not null)
        {
            _saveButton.Text = L.T("添加到事刻");
        }

        SaveAndRefresh();
        _titleBox.Clear();
        _notesBox.Clear();
        _tagsBox.ClearTags();
        _milestoneDaysBox.Clear();
        _subjectNameBox.Clear();
        _relationshipBox.Clear();
        _birthdayYearKnownBox.Checked = true;
        _birthdayLeapMonthBox.Checked = false;
        _recurrenceEditor.Reset();
        ClearCategories();
        ApplySettingsToEditorDefaults();
        SelectComboValue(_templateBox, TimeTemplate.None);
        SelectComboValue(_typeBox, EventType.StartAt);
    }

    private EventItem? SelectedEvent()
    {
        return _eventList.SelectedItem as EventItem;
    }

    private void LoadSelectedForEdit()
    {
        var item = SelectedEvent();
        if (item is null)
        {
            return;
        }

        EditEventFromDialog(item);
    }

    private void EventListMouseClick(object? sender, MouseEventArgs e)
    {
        if (sender is not ListBox list)
        {
            return;
        }

        var index = list.IndexFromPoint(e.Location);
        if (index < 0 || list.Items[index] is not EventItem item)
        {
            return;
        }

        list.SelectedIndex = index;
        var bounds = list.GetItemRectangle(index);
        if (EventCardRenderer.CountdownButtonBounds(bounds).Contains(e.Location))
        {
            ToggleFloatingWindow(item);
        }
        else if (CanComplete(item) && EventCardRenderer.CompleteButtonBounds(bounds).Contains(e.Location))
        {
            SetStatus(item, EventStatus.Done);
        }
    }

    private IReadOnlyList<EventItem> NextFloatingEvents(EventItem? preferred = null)
    {
        var now = DateTime.Now;
        var items = _events
            .Where(e => e.Status is not (EventStatus.Done or EventStatus.Skipped or EventStatus.Cancelled or EventStatus.Postponed))
            .Where(e => e.NextDueAt(now) is not null)
            .OrderBy(e => e.NextDueAt(now))
            .Take(4)
            .ToList();
        if (preferred is not null)
        {
            items.Remove(preferred);
            items.Insert(0, preferred);
            if (items.Count > 4) items.RemoveAt(items.Count - 1);
        }
        return items;
    }

    private void UpdateSelected(EventStatus status)
    {
        var item = SelectedEvent();
        if (item is null)
        {
            return;
        }

        SetStatus(item, status);
    }

    private void SetStatus(EventItem item, EventStatus status)
    {
        if (status is EventStatus.Done)
        {
            CompleteAndRecord(item);
        }
        else if (status is EventStatus.Skipped)
        {
            item.SkipOccurrence();
        }
        else
        {
            item.Status = status;
            item.UpdatedAt = DateTime.Now;
        }

        SaveAndRefresh();
    }

    private void PostponeSelected()
    {
        var item = SelectedEvent();
        if (item is null)
        {
            return;
        }

        var minutes = (int)_postponeMinutesBox.Value;
        if (item.IsRecurringSeries || item.ProjectId is not null)
        {
            item.ShiftSchedule(minutes);
            SaveAndRefresh();
            return;
        }
        item.Status = EventStatus.Postponed;
        item.UpdatedAt = DateTime.Now;

        var copy = new EventItem
        {
            Title = item.Title,
            Notes = item.Notes,
            Type = item.Type,
            Priority = item.Priority,
            StartAt = Shift(item.StartAt, minutes) ?? DateTime.Now.AddMinutes(minutes),
            EndAt = Shift(item.EndAt, minutes),
            DeadlineAt = Shift(item.DeadlineAt, minutes),
            RepeatUnit = item.RepeatUnit,
            RepeatEvery = item.RepeatEvery,
            BirthdayCalendar = item.BirthdayCalendar,
            BirthdayMonth = item.BirthdayMonth,
            BirthdayDay = item.BirthdayDay,
            BirthdayYearKnown = item.BirthdayYearKnown,
            BirthdayIsLeapMonth = item.BirthdayIsLeapMonth,
            BirthdayLeapDayRule = item.BirthdayLeapDayRule,
            SubjectName = item.SubjectName,
            Relationship = item.Relationship,
            PersonIds = [.. item.PersonIds],
            AnniversaryMode = item.AnniversaryMode,
            MilestoneDays = item.MilestoneDays,
            Status = EventStatus.Pending,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _events.Add(copy);
        SaveAndRefresh();
    }

    private void DeleteSelected()
    {
        var item = SelectedEvent();
        if (item is null)
        {
            return;
        }

        _events.Remove(item);
        SaveAndRefresh();
    }

    private void ToggleFloatingWindow(EventItem? preferred = null)
    {
        if (_floating is { IsDisposed: false })
        {
            _floating.Close();
            _floating = null;
            if (preferred is null) return;
        }

        _floating = new FloatingCountdownForm(() => NextFloatingEvents(preferred), item => SetStatus(item, EventStatus.Done));
        _floating.FormClosed += (_, _) => _floating = null;
        _floating.Show();
    }

    private void OpenPomodoroView()
    {
        if (_root is null)
        {
            return;
        }

        if (_folderView is not null)
        {
            _root.Controls.Remove(_folderView);
            _folderView.Dispose();
            _folderView = null;
        }
        if (_calendarView is { IsDisposed: false }) _calendarView.Visible = false;
        if (_projectView is not null) _projectView.Visible = false;
        if (_peopleView is not null) _peopleView.Visible = false;
        if (_historyView is not null) _historyView.Visible = false;
        if (_notesView is not null) _notesView.Visible = false;
        if (_settingsView is { IsDisposed: false }) _settingsView.Visible = false;
        _leftContent.Visible = false;
        if (_timelineView is not null) _timelineView.Visible = false;

        if (_pomodoro is null || _pomodoro.IsDisposed)
        {
            _pomodoro = new PomodoroForm(_notifyIcon)
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };
            _pomodoro.FormClosed += (_, _) => _pomodoro = null;
            _root.Controls.Add(_pomodoro, 1, 0);
            _root.SetColumnSpan(_pomodoro, 2);
            _pomodoro.Show();
        }
        else
        {
            _pomodoro.Visible = true;
        }
        _pomodoro.BringToFront();
    }

    private void OpenCalendarView()
    {
        if (_root is null) return;

        if (_pomodoro is { IsDisposed: false }) _pomodoro.Visible = false;
        if (_projectView is not null) _projectView.Visible = false;
        if (_peopleView is not null) _peopleView.Visible = false;
        if (_historyView is not null) _historyView.Visible = false;
        if (_notesView is not null) _notesView.Visible = false;
        if (_settingsView is { IsDisposed: false }) _settingsView.Visible = false;
        if (_folderView is not null)
        {
            _root.Controls.Remove(_folderView);
            _folderView.Dispose();
            _folderView = null;
        }
        _leftContent.Visible = false;
        if (_timelineView is not null) _timelineView.Visible = false;

        if (_calendarView is null || _calendarView.IsDisposed)
        {
            _calendarView = new CalendarViewForm(
                _events, (item, day) => EditEventFromDialog(item, day), CreateEventFromCalendar,
                item => SetStatus(item, EventStatus.Done), ToggleRecurrencePaused, DeleteEvent, AddToFolder,
                item => CreateFolder(item), CreateFolderFromTag)
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };
            _calendarView.FormClosed += (_, _) => _calendarView = null;
            _root.Controls.Add(_calendarView, 1, 0);
            _root.SetColumnSpan(_calendarView, 2);
            _calendarView.Show();
        }
        else
        {
            _calendarView.RefreshView();
            _calendarView.Visible = true;
        }
        _calendarView.BringToFront();
    }

    private void OpenFolderView()
    {
        if (_root is null)
        {
            return;
        }

        if (_pomodoro is { IsDisposed: false }) _pomodoro.Visible = false;
        if (_calendarView is { IsDisposed: false }) _calendarView.Visible = false;
        if (_projectView is not null) _projectView.Visible = false;
        if (_peopleView is not null) _peopleView.Visible = false;
        if (_historyView is not null) _historyView.Visible = false;
        if (_notesView is not null) _notesView.Visible = false;
        if (_settingsView is { IsDisposed: false }) _settingsView.Visible = false;
        _folderView?.Dispose();
        _folderView = new FolderViewForm(_events, EditEventFromDialog, DeleteEvent, () => CreateFolder(), SaveAndRefresh)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 0)
        };
        _leftContent.Visible = false;
        if (_timelineView is not null)
        {
            _timelineView.Visible = false;
        }
        _root.Controls.Add(_folderView, 1, 0);
        _root.SetColumnSpan(_folderView, 2);
        _folderView.BringToFront();
    }

    private void OpenProjectView()
    {
        if (_root is null) return;
        if (_pomodoro is { IsDisposed: false }) _pomodoro.Visible = false;
        if (_calendarView is { IsDisposed: false }) _calendarView.Visible = false;
        if (_peopleView is not null) _peopleView.Visible = false;
        if (_historyView is not null) _historyView.Visible = false;
        if (_notesView is not null) _notesView.Visible = false;
        if (_settingsView is { IsDisposed: false }) _settingsView.Visible = false;
        if (_folderView is not null)
        {
            _root.Controls.Remove(_folderView);
            _folderView.Dispose();
            _folderView = null;
        }
        _leftContent.Visible = false;
        if (_timelineView is not null) _timelineView.Visible = false;
        if (_projectView is null || _projectView.IsDisposed)
        {
            _projectView = new ProjectView(
                _events,
                EditEventFromDialog,
                item => SetStatus(item, EventStatus.Done),
                DeleteEvent,
                SaveAndRefresh);
            _root.Controls.Add(_projectView, 1, 0);
            _root.SetColumnSpan(_projectView, 2);
        }
        _projectView.RefreshView();
        _projectView.Visible = true;
        _projectView.BringToFront();
    }

    private void OpenPeopleView()
    {
        if (_root is null) return;
        HideEmbeddedViews();
        if (_peopleView is null || _peopleView.IsDisposed)
        {
            _peopleView = new PeopleView(_people, _events, _records, EditEventFromDialog, SaveAndRefresh);
            _root.Controls.Add(_peopleView, 1, 0);
            _root.SetColumnSpan(_peopleView, 2);
        }
        _peopleView.RefreshView();
        _peopleView.Visible = true;
        _peopleView.BringToFront();
    }

    private void OpenHistoryView()
    {
        if (_root is null) return;
        HideEmbeddedViews();
        if (_historyView is null || _historyView.IsDisposed)
        {
            _historyView = new HistoryView(_records, _events, SaveAndRefresh);
            _root.Controls.Add(_historyView, 1, 0);
            _root.SetColumnSpan(_historyView, 2);
        }
        _historyView.RefreshView();
        _historyView.Visible = true;
        _historyView.BringToFront();
    }

    private void OpenNotesView()
    {
        if (_root is null) return;
        HideEmbeddedViews();
        if (_notesView is null || _notesView.IsDisposed)
        {
            _notesView = new NotesView(_notes, SaveAndRefresh);
            _root.Controls.Add(_notesView, 1, 0);
            _root.SetColumnSpan(_notesView, 2);
        }
        _notesView.RefreshView();
        _notesView.Visible = true;
        _notesView.BringToFront();
    }

    private void HideEmbeddedViews()
    {
        if (_pomodoro is { IsDisposed: false }) _pomodoro.Visible = false;
        if (_calendarView is { IsDisposed: false }) _calendarView.Visible = false;
        if (_projectView is not null) _projectView.Visible = false;
        if (_peopleView is not null) _peopleView.Visible = false;
        if (_historyView is not null) _historyView.Visible = false;
        if (_notesView is not null) _notesView.Visible = false;
        if (_settingsView is { IsDisposed: false }) _settingsView.Visible = false;
        if (_folderView is not null)
        {
            _root?.Controls.Remove(_folderView);
            _folderView.Dispose();
            _folderView = null;
        }
        _leftContent.Visible = false;
        if (_timelineView is not null) _timelineView.Visible = false;
    }

    private void OpenSettings()
    {
        if (_root is null) return;
        if (!Visible) RestoreFromTray();
        HideEmbeddedViews();
        _selectSettingsNavigation?.Invoke();
        if (_settingsView is null || _settingsView.IsDisposed)
        {
            _settingsView = new SettingsForm(_settings)
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                MinimumSize = Size.Empty
            };
            _settingsView.SettingsSaved += (_, _) =>
            {
                _settingsStore.Save(_settings);
                if (_settingsView.LanguageChanged)
                {
                    RestartRequested = true;
                    _allowExit = true;
                    Close();
                    return;
                }
                ApplySettingsToEditorDefaults();
            };
            _settingsView.CancelRequested += (_, _) =>
            {
                var view = _settingsView;
                _settingsView = null;
                if (view is not null)
                {
                    _root.Controls.Remove(view);
                    view.Dispose();
                }
                _selectEditorNavigation?.Invoke();
                ShowLeftView(_editorView!);
            };
            _root.Controls.Add(_settingsView, 1, 0);
            _root.SetColumnSpan(_settingsView, 2);
            _settingsView.Show();
        }
        else _settingsView.Visible = true;
        _settingsView.BringToFront();
    }

    private void EditEventFromDialog(EventItem item) => EditEventFromDialog(item, null);

    private void EditEventFromDialog(EventItem item, DateTime? occurrenceDay)
    {
        var scope = item.IsRecurringSeries ? ChooseRecurrenceEditScope() : RecurrenceEditScope.EntireSeries;
        if (scope is null) return;

        var occurrenceAt = item.IsRecurringSeries
            ? occurrenceDay is null
                ? item.CurrentOrNextOccurrenceAt(DateTime.Now)
                : item.OccurrenceOn(occurrenceDay.Value)
            : null;
        var working = CloneEvent(item);
        if (scope is not RecurrenceEditScope.EntireSeries && occurrenceAt is not null)
        {
            MoveWorkingCopyToOccurrence(working, occurrenceAt.Value);
        }
        using var dialog = new EventEditForm(working, working.ProjectId is not null);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        ResolvePersonAssociation(working);

        var index = _events.IndexOf(item);
        if (index < 0) return;
        if (scope is RecurrenceEditScope.EntireSeries || occurrenceAt is null)
        {
            working.Id = item.Id;
            working.Occurrences.RemoveAll(x => !x.IsHandled);
            _events[index] = working;
        }
        else if (scope is RecurrenceEditScope.ThisOccurrence)
        {
            item.SkipOccurrenceAt(occurrenceAt.Value);
            working.DetachFromSeries();
            _events.Add(working);
        }
        else
        {
            var first = item.CurrentOrNextOccurrenceAt((item.StartAt ?? occurrenceAt.Value).AddTicks(-1));
            if (first == occurrenceAt)
            {
                working.Id = item.Id;
                working.Occurrences = [];
                _events[index] = working;
            }
            else
            {
                item.EndBefore(occurrenceAt.Value);
                working.ResetAsNewSeries();
                _events.Add(working);
            }
        }
        SaveAndRefresh();
    }

    private void ToggleRecurrencePaused(EventItem item)
    {
        item.SetRecurrencePaused(!item.IsRecurrencePaused);
        SaveAndRefresh();
    }

    private RecurrenceEditScope? ChooseRecurrenceEditScope()
    {
        var page = new TaskDialogPage
        {
            Caption = L.T("编辑周期事项"),
            Heading = L.T("要修改哪些事件？"),
            Text = L.T("选择修改范围。已完成和已跳过的历史记录不会被改动。"),
            AllowCancel = true,
            Icon = TaskDialogIcon.Information
        };
        var onlyThis = new TaskDialogCommandLinkButton(L.T("仅本次"), L.T("把当前一次作为独立事项修改"));
        var thisAndFuture = new TaskDialogCommandLinkButton(L.T("本次及以后"), L.T("保留过去记录，从本次开始使用新规则"));
        var entire = new TaskDialogCommandLinkButton(L.T("整个系列"), L.T("修改这个周期事项的全部规则"));
        page.Buttons.Add(onlyThis);
        page.Buttons.Add(thisAndFuture);
        page.Buttons.Add(entire);
        var result = TaskDialog.ShowDialog(this, page);
        if (result == onlyThis) return RecurrenceEditScope.ThisOccurrence;
        if (result == thisAndFuture) return RecurrenceEditScope.ThisAndFuture;
        if (result == entire) return RecurrenceEditScope.EntireSeries;
        return null;
    }

    private static EventItem CloneEvent(EventItem item) =>
        JsonSerializer.Deserialize<EventItem>(JsonSerializer.Serialize(item)) ?? throw new InvalidOperationException("无法复制事项。");

    private static void MoveWorkingCopyToOccurrence(EventItem item, DateTime occurrenceAt)
    {
        var offset = item.StartAt is null ? TimeSpan.Zero : occurrenceAt - item.StartAt.Value;
        item.StartAt = occurrenceAt;
        item.EndAt = item.EndAt?.Add(offset);
        item.DeadlineAt = item.DeadlineAt?.Add(offset);
        item.Status = EventStatus.Pending;
        item.IsRecurrencePaused = false;
    }

    private void CreateEventFromCalendar(DateTime day)
    {
        var item = new EventItem
        {
            StartAt = day.Date.AddHours(9),
            Type = EventType.StartAt,
            Status = EventStatus.Pending,
            ReminderRepeatMinutes = 10
        };

        using var dialog = new EventEditForm(item);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            ResolvePersonAssociation(item);
            _events.Add(item);
            SaveAndRefresh();
        }
    }

    private void RefreshList()
    {
        var now = DateTime.Now;
        foreach (var item in _events.Where(e => e.IsOverdue(now)))
        {
            item.Status = EventStatus.Overdue;
        }

        _eventList.Items.Clear();
        foreach (var item in _events
            .Where(e => MatchesFilter(e, now))
            .OrderBy(e => e.Status is EventStatus.Done or EventStatus.Skipped or EventStatus.Cancelled or EventStatus.Postponed)
            .ThenBy(e => e.NextDueAt(now) ?? DateTime.MaxValue))
        {
            _eventList.Items.Add(item);
        }
    }

    private void CheckReminders()
    {
        var now = DateTime.Now;
        if (_isHandlingReminders || IsInQuietHours(now))
        {
            return;
        }

        _isHandlingReminders = true;
        _timer.Stop();
        try
        {
            var due = _reminders.FindDueEvents(_events, now);
            foreach (var item in due)
            {
                // 先持久化“已提醒”，再显示模态窗口，避免窗口消息循环让计时器重复进入。
                item.MarkReminded(DateTime.Now);
                _store.Save(_events);
                HandleReminder(item);
            }

            if (due.Count > 0)
            {
                SaveAndRefresh();
            }
        }
        finally
        {
            _isHandlingReminders = false;
            if (!_allowExit) _timer.Start();
        }
    }

    private void HandleReminder(EventItem item)
    {
        using var dialog = new ReminderDialog(item, _settings.DefaultSnoozeMinutes);
        var result = Visible ? dialog.ShowDialog(this) : dialog.ShowDialog();
        if (result != DialogResult.OK)
        {
            return;
        }

        switch (dialog.SelectedAction)
        {
            case ReminderDialogAction.Complete:
                CompleteAndRecord(item);
                break;
            case ReminderDialogAction.Snooze:
                item.SnoozeUntil(DateTime.Now.AddMinutes(dialog.Minutes));
                break;
            case ReminderDialogAction.Postpone:
                item.ShiftSchedule(dialog.Minutes);
                break;
            default:
                if (item.Type is EventType.StartAt && item.Status is EventStatus.Pending)
                {
                    item.Status = EventStatus.InProgress;
                }
                break;
        }
    }

    private bool IsInQuietHours(DateTime now)
    {
        if (!_settings.QuietHoursEnabled)
        {
            return false;
        }

        var current = now.TimeOfDay;
        var start = _settings.QuietHoursStart;
        var end = _settings.QuietHoursEnd;
        return start <= end
            ? current >= start && current < end
            : current >= start || current < end;
    }

    private void SaveAndRefresh()
    {
        _store.Save(_events, _people, _records, _notes);
        RefreshParentOptions();
        RefreshTagFilter();
        RefreshList();
        RefreshSideLists();
        if (_calendarView is { IsDisposed: false }) _calendarView.RefreshView();
        if (_projectView is { IsDisposed: false }) _projectView.RefreshView();
        if (_peopleView is { IsDisposed: false }) _peopleView.RefreshView();
        if (_historyView is { IsDisposed: false }) _historyView.RefreshView();
        if (_notesView is { IsDisposed: false }) _notesView.RefreshView();
    }

    private void RefreshSideLists()
    {
        _currentList.Items.Clear();
        foreach (var item in CurrentItems())
        {
            _currentList.Items.Add(item);
        }

        _recommendedList.Items.Clear();
        foreach (var item in RecommendedItems())
        {
            _recommendedList.Items.Add(item);
        }
    }

    private bool MatchesFilter(EventItem item, DateTime now)
    {
        if (item.IsGroup || item.IsProject)
        {
            return false;
        }

        var statusMatched = SelectedValue<TimelineFilter>(_filterBox) switch
        {
            TimelineFilter.Today => item.NextDueAt(now)?.Date == now.Date,
            TimelineFilter.ThisWeek => IsThisWeek(item, now),
            TimelineFilter.Overdue => item.Status is EventStatus.Overdue,
            TimelineFilter.Pending => item.Status is EventStatus.Pending or EventStatus.InProgress,
            TimelineFilter.InProgress => item.Status is EventStatus.InProgress,
            TimelineFilter.Done => item.Status is EventStatus.Done,
            TimelineFilter.Birthday => item.Type is EventType.Birthday,
            TimelineFilter.Maybe => item.Type is EventType.Maybe,
            TimelineFilter.Groups => item.IsGroup,
            _ => true
        };

        return statusMatched && MatchesSearch(item);
    }

    private static bool IsThisWeek(EventItem item, DateTime now)
    {
        var due = item.NextDueAt(now);
        if (due is null)
        {
            return false;
        }

        var start = now.Date.AddDays(-((int)now.DayOfWeek + 6) % 7);
        return due.Value.Date >= start && due.Value.Date < start.AddDays(7);
    }

    private bool MatchesSearch(EventItem item)
    {
        var keyword = _searchBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return true;
        }

        return item.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || item.Notes.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || SplitTags($"{item.Tags}, {item.Categories}").Any(tag => tag.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static DateTime BuildTime(DateTimePicker date, ModernNumericUpDown hour, ModernNumericUpDown minute)
    {
        return date.Value.Date.AddHours((double)hour.Value).AddMinutes((double)minute.Value);
    }

    private static DateTime? Shift(DateTime? value, int minutes)
    {
        return value?.AddMinutes(minutes);
    }

    private static void SetTimeBoxes(DateTime value, DateTimePicker date, ModernNumericUpDown hour, ModernNumericUpDown minute)
    {
        date.Value = value.Date;
        hour.Value = value.Hour;
        minute.Value = value.Minute;
    }

    private static bool CanComplete(EventItem item)
    {
        return item.Status is not EventStatus.Cancelled
            && (!item.IsRecurringSeries || !item.IsRecurrencePaused)
            && (item.IsRecurringSeries || item.Status is not EventStatus.Done);
    }

    private void ResolvePersonAssociation(EventItem item)
    {
        if (item.Type is not (EventType.Birthday or EventType.Anniversary) || string.IsNullOrWhiteSpace(item.SubjectName)) return;
        var person = _people.FirstOrDefault(x => x.Name.Equals(item.SubjectName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (person is null)
        {
            person = new PersonProfile { Name = item.SubjectName.Trim(), Relationship = item.Relationship.Trim() };
            _people.Add(person);
        }
        else if (string.IsNullOrWhiteSpace(person.Relationship) && !string.IsNullOrWhiteSpace(item.Relationship))
        {
            person.Relationship = item.Relationship.Trim();
            person.UpdatedAt = DateTime.Now;
        }
        item.PersonIds = [person.Id];
    }

    private void CompleteAndRecord(EventItem item)
    {
        var now = DateTime.Now;
        var dueAt = item.NextDueAt(now);
        item.Complete(now);
        var kind = item.Type switch
        {
            EventType.Birthday => ActivityRecordKind.BirthdayCelebrated,
            EventType.Anniversary => ActivityRecordKind.AnniversaryCelebrated,
            _ => ActivityRecordKind.EventCompleted
        };
        _records.Add(new ActivityRecord
        {
            Kind = kind,
            EventId = item.Id,
            ProjectId = item.ProjectId,
            PersonIds = [.. item.PersonIds],
            Title = kind switch
            {
                ActivityRecordKind.BirthdayCelebrated => $"庆祝了{item.SubjectName.DefaultIfBlank(item.Title)}的生日",
                ActivityRecordKind.AnniversaryCelebrated => $"纪念了{item.SubjectName.DefaultIfBlank(item.Title)}",
                _ => $"完成了：{item.Title}"
            },
            Detail = dueAt is null ? item.TypeText : $"计划时间 {dueAt:yyyy-MM-dd HH:mm}",
            OccurredAt = now
        });

        if (item.ProjectId is not Guid projectId) return;
        var steps = _events.Where(x => x.ProjectId == projectId).ToList();
        if (steps.Count == 0 || steps.Any(x => x.Status is not (EventStatus.Done or EventStatus.Skipped or EventStatus.Cancelled))) return;
        if (_records.Any(x => x.Kind is ActivityRecordKind.ProjectCompleted && x.ProjectId == projectId)) return;
        var project = _events.FirstOrDefault(x => x.Id == projectId && x.IsProject);
        _records.Add(new ActivityRecord
        {
            Kind = ActivityRecordKind.ProjectCompleted,
            ProjectId = projectId,
            Title = $"完成项目：{project?.Title ?? "未命名项目"}",
            OccurredAt = now
        });
    }

    private void RefreshParentOptions()
    {
        var selected = _parentBox.SelectedItem is Option<Guid?> option ? option.Value : null;
        _parentBox.Items.Clear();
        AddOption(_parentBox, "不加入收藏夹", (Guid?)null);
        foreach (var group in _events.Where(e => e.IsGroup).OrderBy(e => e.Title))
        {
            AddOption(_parentBox, group.Title, (Guid?)group.Id);
        }
        SelectComboValue(_parentBox, selected);
    }

    private void RefreshTagFilter()
    {
        // 词条现在通过搜索框过滤；保留这个方法，避免刷新流程分叉。
    }

    private static string NormalizeTags(string text)
    {
        return string.Join(", ", SplitTags(text).Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private string SelectedCategories()
    {
        return _categoryBox.SelectedText;
    }

    private void ClearCategories()
    {
        _categoryBox.ClearSelected();
    }

    private static IEnumerable<string> SplitTags(string text)
    {
        return text
            .Split(new[] { ',', '，', ';', '；', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 0);
    }

    private Control[] StartTimeControls()
    {
        return [_startHourBox, _startMinuteBox];
    }

    private Control[] DeadlineTimeControls()
    {
        return [_deadlineHourBox, _deadlineMinuteBox];
    }

    private static void SetTimeRowEnabled(IEnumerable<Control> controls, bool enabled)
    {
        foreach (var control in controls)
        {
            control.Enabled = enabled;
        }
    }

    private static Button PrimaryButton(string text)
    {
        var button = new Button
        {
            Text = text,
            Height = 38,
            Dock = DockStyle.Top,
            BackColor = Accent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 8, 0, 0)
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private static Button SecondaryButton(string text)
    {
        var button = new Button
        {
            Text = text,
            Height = 32,
            AutoSize = true,
            BackColor = Color.White,
            ForeColor = TextMain,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 8, 0)
        };
        button.FlatAppearance.BorderColor = Border;
        return button;
    }

    private static Button PurposeButton(string text)
    {
        var button = new ModernButton
        {
            Text = text,
            Dock = DockStyle.Fill,
            Height = 42,
            BackColor = Color.White,
            ForeColor = TextMain,
            Margin = new Padding(3, 0, 3, 0),
            Cursor = Cursors.Hand
        };
        return button;
    }

    private static void StylePurposeButton(Button button, bool selected)
    {
        button.BackColor = selected ? Color.FromArgb(239, 246, 255) : Color.White;
        button.ForeColor = selected ? Accent : TextMain;
        button.Font = new Font("Microsoft YaHei UI", 9F, selected ? FontStyle.Bold : FontStyle.Regular);
        button.FlatAppearance.BorderColor = selected ? Color.FromArgb(96, 165, 250) : Border;
    }

    private static Button SidebarButton(string text)
    {
        var button = new ModernButton
        {
            Text = text,
            Height = 36,
            Dock = DockStyle.Fill,
            BackColor = AppBack,
            ForeColor = TextMain,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0),
            Margin = new Padding(0, 3, 0, 3),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = AppBack;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
        return button;
    }

    private static void StyleSidebarButton(Button button, bool selected)
    {
        button.BackColor = selected ? Color.FromArgb(229, 239, 255) : AppBack;
        button.ForeColor = selected ? Accent : TextMain;
        button.Font = new Font("Microsoft YaHei UI", 9F, selected ? FontStyle.Bold : FontStyle.Regular);
        button.FlatAppearance.BorderColor = selected ? Color.FromArgb(191, 219, 254) : AppBack;
    }

    private static Control SidebarBrand()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = AppBack };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        panel.Controls.Add(new Label
        {
            Text = "事刻",
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold),
            ForeColor = TextMain
        });
        panel.Controls.Add(new Label
        {
            Text = "TIMARKER",
            Dock = DockStyle.Fill,
            ForeColor = TextMuted
        });
        return panel;
    }

    private static Label SidebarSection(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        ForeColor = TextMuted,
        Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold),
        Padding = new Padding(4, 6, 0, 0)
    };

    private IEnumerable<EventItem> CurrentItems()
    {
        var now = DateTime.Now;
        return _events
            .Where(e => !e.IsGroup && !e.IsProject)
            .Where(e => e.Status is EventStatus.Pending or EventStatus.InProgress || e.NextDueAt(now)?.Date == now.Date)
            .OrderBy(e => e.NextDueAt(now) ?? DateTime.MaxValue)
            .Take(20);
    }

    private IEnumerable<EventItem> RecommendedItems()
    {
        var now = DateTime.Now;
        return _events
            .Where(e => !e.IsGroup && !e.IsProject)
            .Where(e => e.Status is not (EventStatus.Done or EventStatus.Skipped or EventStatus.Cancelled or EventStatus.Postponed))
            .OrderByDescending(e => e.Status is EventStatus.Overdue)
            .ThenBy(e => e.NextDueAt(now) ?? DateTime.MaxValue)
            .Take(20);
    }

    private static ModernNumericUpDown TimeNumber(int max)
    {
        return new ModernNumericUpDown
        {
            Minimum = 0,
            Maximum = max,
            Width = 78,
            TextAlign = HorizontalAlignment.Center
        };
    }

    private static void StyleInput(Control control)
    {
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 0, 0, 4);
        control.BackColor = Color.White;
        control.ForeColor = TextMain;
    }

    private static Color StatusColor(EventStatus status)
    {
        return status switch
        {
            EventStatus.Done => Color.FromArgb(22, 163, 74),
            EventStatus.Skipped => Color.FromArgb(100, 116, 139),
            EventStatus.Postponed => Color.FromArgb(217, 119, 6),
            EventStatus.Overdue => Danger,
            EventStatus.Cancelled => Color.FromArgb(100, 116, 139),
            EventStatus.InProgress => Accent,
            _ => Color.FromArgb(79, 70, 229)
        };
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

    private enum TimelineFilter
    {
        All,
        Today,
        ThisWeek,
        Overdue,
        Pending,
        InProgress,
        Done,
        Birthday,
        Maybe,
        Groups
    }

    private enum TimeTemplate
    {
        None,
        TodayBeforeWorkOff,
        TomorrowMorning,
        NextWeekDeadline,
        DailyHabit,
        Birthday,
        Anniversary
    }

    private enum ReminderUnit
    {
        Minute,
        Hour,
        Day
    }

    private enum RecurrenceEditScope
    {
        ThisOccurrence,
        ThisAndFuture,
        EntireSeries
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
}
