using Timarker.Models;

namespace Timarker;

public sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly ComboBox _languageBox = new ModernComboBox();
    private readonly CheckBox _closeToTrayBox = SettingCheckBox("关闭窗口时最小化到系统托盘", true);
    private readonly CheckBox _startWithWindowsBox = SettingCheckBox("开机后自动启动事刻");
    private readonly CheckBox _quietHoursBox = SettingCheckBox("启用免打扰时段");
    private readonly ComboBox _quietStartHourBox = TimeChoice(24);
    private readonly ComboBox _quietStartMinuteBox = TimeChoice(60);
    private readonly ComboBox _quietEndHourBox = TimeChoice(24);
    private readonly ComboBox _quietEndMinuteBox = TimeChoice(60);
    private readonly ModernNumericUpDown _leadBox = NumberBox(0, 43200, 5);
    private readonly ModernNumericUpDown _repeatMinutesBox = NumberBox(1, 1440, 5);
    private readonly ModernNumericUpDown _repeatCountBox = NumberBox(0, 20, 1);
    private readonly ModernNumericUpDown _snoozeBox = NumberBox(1, 1440, 5);

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;
        L.Use(settings);
        Text = "设置";
        ClientSize = new Size(540, 620);
        MinimumSize = new Size(556, 659);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = Color.FromArgb(246, 247, 251);

        LoadValues();
        BuildUi();
        L.Apply(this);
    }

    public bool LanguageChanged { get; private set; }
    public event EventHandler? SettingsSaved;
    public event EventHandler? CancelRequested;

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18),
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        root.Controls.Add(new Label
        {
            Text = "事刻设置",
            Dock = DockStyle.Fill,
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 41, 55)
        });

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 10,
            BackColor = Color.White,
            Padding = new Padding(16)
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        for (var i = 0; i < 3; i++) form.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        for (var i = 0; i < 6; i++) form.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        AddRow(form, 0, "语言", _languageBox);
        form.Controls.Add(_closeToTrayBox, 0, 1);
        form.SetColumnSpan(_closeToTrayBox, 2);
        form.Controls.Add(_startWithWindowsBox, 0, 2);
        form.SetColumnSpan(_startWithWindowsBox, 2);
        form.Controls.Add(_quietHoursBox, 0, 3);
        form.SetColumnSpan(_quietHoursBox, 2);
        AddRow(form, 4, "免打扰开始", TimePicker(_quietStartHourBox, _quietStartMinuteBox));
        AddRow(form, 5, "免打扰结束", TimePicker(_quietEndHourBox, _quietEndMinuteBox));
        AddRow(form, 6, "默认提前提醒", WithUnit(_leadBox, "分钟"));
        AddRow(form, 7, "默认重复间隔", WithUnit(_repeatMinutesBox, "分钟"));
        AddRow(form, 8, "默认重复次数", WithUnit(_repeatCountBox, "次"));
        AddRow(form, 9, "默认稍后提醒", WithUnit(_snoozeBox, "分钟"));
        root.Controls.Add(form);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var save = Button("保存", true);
        var cancel = Button("取消", false);
        save.Click += (_, _) => SaveValues();
        cancel.Click += (_, _) =>
        {
            if (TopLevel) DialogResult = DialogResult.Cancel;
            else CancelRequested?.Invoke(this, EventArgs.Empty);
        };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(save);
        root.Controls.Add(buttons);

        Controls.Add(root);
    }

    private void LoadValues()
    {
        _languageBox.Items.AddRange(["简体中文", "English"]);
        _languageBox.SelectedIndex = _settings.Language.Equals("en", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        _closeToTrayBox.Checked = _settings.CloseToTray;
        _startWithWindowsBox.Checked = _settings.StartWithWindows;
        _quietHoursBox.Checked = _settings.QuietHoursEnabled;
        SetTime(_quietStartHourBox, _quietStartMinuteBox, _settings.QuietHoursStart);
        SetTime(_quietEndHourBox, _quietEndMinuteBox, _settings.QuietHoursEnd);
        _leadBox.Value = Clamp(_settings.DefaultReminderLeadMinutes, _leadBox);
        _repeatMinutesBox.Value = Clamp(_settings.DefaultReminderRepeatMinutes, _repeatMinutesBox);
        _repeatCountBox.Value = Clamp(_settings.DefaultReminderRepeatCount, _repeatCountBox);
        _snoozeBox.Value = Clamp(_settings.DefaultSnoozeMinutes, _snoozeBox);
    }

    private void SaveValues()
    {
        var language = _languageBox.SelectedIndex == 1 ? "en" : "zh-CN";
        LanguageChanged = !_settings.Language.Equals(language, StringComparison.OrdinalIgnoreCase);
        _settings.Language = language;
        _settings.CloseToTray = _closeToTrayBox.Checked;
        _settings.StartWithWindows = _startWithWindowsBox.Checked;
        _settings.QuietHoursEnabled = _quietHoursBox.Checked;
        _settings.QuietHoursStart = SelectedTime(_quietStartHourBox, _quietStartMinuteBox);
        _settings.QuietHoursEnd = SelectedTime(_quietEndHourBox, _quietEndMinuteBox);
        _settings.DefaultReminderLeadMinutes = (int)_leadBox.Value;
        _settings.DefaultReminderRepeatMinutes = (int)_repeatMinutesBox.Value;
        _settings.DefaultReminderRepeatCount = (int)_repeatCountBox.Value;
        _settings.DefaultSnoozeMinutes = (int)_snoozeBox.Value;
        if (TopLevel) DialogResult = DialogResult.OK;
        else SettingsSaved?.Invoke(this, EventArgs.Empty);
    }

    private static void AddRow(TableLayoutPanel form, int row, string label, Control control)
    {
        form.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty,
            ForeColor = Color.FromArgb(107, 114, 128)
        }, 0, row);
        control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        control.Margin = new Padding(0, 3, 0, 3);
        form.Controls.Add(control, 1, row);
    }

    private static Control WithUnit(Control control, string unit)
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = false, WrapContents = false, Margin = Padding.Empty };
        control.Margin = new Padding(4, 2, 4, 2);
        panel.Controls.Add(control);
        panel.Controls.Add(new Label { Text = unit, AutoSize = true, Margin = new Padding(4, 10, 0, 0) });
        return panel;
    }

    private static Button Button(string text, bool primary)
    {
        var button = new ModernButton
        {
            Text = text,
            Width = 88,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Color.FromArgb(37, 99, 235) : Color.White,
            ForeColor = primary ? Color.White : Color.FromArgb(31, 41, 55),
            Margin = new Padding(8, 8, 0, 0)
        };
        button.FlatAppearance.BorderColor = primary ? Color.FromArgb(37, 99, 235) : Color.FromArgb(203, 213, 225);
        return button;
    }

    private static ComboBox TimeChoice(int count)
    {
        var box = new ModernComboBox
        {
            Width = 62,
            DropDownWidth = 62,
            MaxDropDownItems = 8
        };
        for (var i = 0; i < count; i++) box.Items.Add(i.ToString("00"));
        return box;
    }

    private static Control TimePicker(ComboBox hour, ComboBox minute)
    {
        var panel = new TableLayoutPanel { Height = 36, ColumnCount = 3, RowCount = 1, Margin = Padding.Empty };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 24));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        hour.Dock = minute.Dock = DockStyle.Fill;
        hour.Margin = minute.Margin = Padding.Empty;
        panel.Controls.Add(hour, 0, 0);
        panel.Controls.Add(new Label
        {
            Text = ":",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(100, 116, 139),
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
        }, 1, 0);
        panel.Controls.Add(minute, 2, 0);
        return panel;
    }

    private static void SetTime(ComboBox hour, ComboBox minute, TimeSpan time)
    {
        hour.SelectedIndex = Math.Clamp(time.Hours, 0, 23);
        minute.SelectedIndex = Math.Clamp(time.Minutes, 0, 59);
    }

    private static TimeSpan SelectedTime(ComboBox hour, ComboBox minute) =>
        new(Math.Max(0, hour.SelectedIndex), Math.Max(0, minute.SelectedIndex), 0);

    private static ModernNumericUpDown NumberBox(int min, int max, int increment)
    {
        return new ModernNumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Increment = increment,
            Width = 88
        };
    }

    private static CheckBox SettingCheckBox(string text, bool isChecked = false) => new()
    {
        Text = text,
        Checked = isChecked,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(2, 0, 0, 0)
    };

    private static decimal Clamp(int value, ModernNumericUpDown box)
    {
        return Math.Min(box.Maximum, Math.Max(box.Minimum, value));
    }
}
