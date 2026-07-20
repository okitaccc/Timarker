using Timarker.Models;

namespace Timarker;

public sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly ComboBox _languageBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _closeToTrayBox = new() { Text = "关闭窗口时最小化到系统托盘", Checked = true, AutoSize = true };
    private readonly CheckBox _startWithWindowsBox = new() { Text = "开机后自动启动事刻", AutoSize = true };
    private readonly CheckBox _quietHoursBox = new() { Text = "启用免打扰时段", AutoSize = true };
    private readonly DateTimePicker _quietStartBox = TimePicker();
    private readonly DateTimePicker _quietEndBox = TimePicker();
    private readonly NumericUpDown _leadBox = NumberBox(0, 43200, 5);
    private readonly NumericUpDown _repeatMinutesBox = NumberBox(1, 1440, 5);
    private readonly NumericUpDown _repeatCountBox = NumberBox(0, 20, 1);
    private readonly NumericUpDown _snoozeBox = NumberBox(1, 1440, 5);

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;
        L.Use(settings);
        Text = "设置";
        Width = 520;
        Height = 560;
        MinimumSize = new Size(460, 460);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = Color.FromArgb(246, 247, 251);

        LoadValues();
        BuildUi();
        L.Apply(this);
    }

    public bool LanguageChanged { get; private set; }

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
        for (var i = 0; i < form.RowCount; i++)
        {
            form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        AddRow(form, 0, "语言", _languageBox);
        form.Controls.Add(_closeToTrayBox, 0, 1);
        form.SetColumnSpan(_closeToTrayBox, 2);
        form.Controls.Add(_startWithWindowsBox, 0, 2);
        form.SetColumnSpan(_startWithWindowsBox, 2);
        form.Controls.Add(_quietHoursBox, 0, 3);
        form.SetColumnSpan(_quietHoursBox, 2);
        AddRow(form, 4, "免打扰开始", _quietStartBox);
        AddRow(form, 5, "免打扰结束", _quietEndBox);
        AddRow(form, 6, "默认提前提醒", WithUnit(_leadBox, "分钟"));
        AddRow(form, 7, "默认重复间隔", WithUnit(_repeatMinutesBox, "分钟"));
        AddRow(form, 8, "默认重复次数", WithUnit(_repeatCountBox, "次"));
        AddRow(form, 9, "默认稍后提醒", WithUnit(_snoozeBox, "分钟"));
        root.Controls.Add(form);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var save = Button("保存", true);
        var cancel = Button("取消", false);
        save.Click += (_, _) => SaveValues();
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
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
        _quietStartBox.Value = DateTime.Today.Add(_settings.QuietHoursStart);
        _quietEndBox.Value = DateTime.Today.Add(_settings.QuietHoursEnd);
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
        _settings.QuietHoursStart = _quietStartBox.Value.TimeOfDay;
        _settings.QuietHoursEnd = _quietEndBox.Value.TimeOfDay;
        _settings.DefaultReminderLeadMinutes = (int)_leadBox.Value;
        _settings.DefaultReminderRepeatMinutes = (int)_repeatMinutesBox.Value;
        _settings.DefaultReminderRepeatCount = (int)_repeatCountBox.Value;
        _settings.DefaultSnoozeMinutes = (int)_snoozeBox.Value;
        DialogResult = DialogResult.OK;
    }

    private static void AddRow(TableLayoutPanel form, int row, string label, Control control)
    {
        form.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 8, 0, 8),
            ForeColor = Color.FromArgb(107, 114, 128)
        }, 0, row);
        control.Dock = DockStyle.Fill;
        form.Controls.Add(control, 1, row);
    }

    private static Control WithUnit(Control control, string unit)
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false };
        panel.Controls.Add(control);
        panel.Controls.Add(new Label { Text = unit, AutoSize = true, Padding = new Padding(4, 8, 0, 0) });
        return panel;
    }

    private static Button Button(string text, bool primary)
    {
        return new Button
        {
            Text = text,
            Width = 88,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Color.FromArgb(37, 99, 235) : Color.White,
            ForeColor = primary ? Color.White : Color.FromArgb(31, 41, 55),
            Margin = new Padding(8, 8, 0, 0)
        };
    }

    private static DateTimePicker TimePicker()
    {
        return new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "HH:mm",
            ShowUpDown = true
        };
    }

    private static NumericUpDown NumberBox(int min, int max, int increment)
    {
        return new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Increment = increment,
            Width = 88
        };
    }

    private static decimal Clamp(int value, NumericUpDown box)
    {
        return Math.Min(box.Maximum, Math.Max(box.Minimum, value));
    }
}
