using Timarker.Models;

namespace Timarker;

public sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly PluginManager _plugins;
    private readonly ComboBox _languageBox = new ModernComboBox();
    private readonly ComboBox _themeBox = new ModernComboBox();
    private readonly ComboBox _fontScaleBox = new ModernComboBox();
    private readonly ComboBox _backgroundLayoutBox = new ModernComboBox();
    private readonly Label _backgroundName = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
    private readonly TrackBar _backgroundOpacity = new() { Minimum = 0, Maximum = 100, TickFrequency = 10, Dock = DockStyle.Fill };
    private readonly Label _opacityValue = new() { Width = 46, TextAlign = ContentAlignment.MiddleRight };
    private readonly Dictionary<string, CheckBox> _featureBoxes = FeatureCatalog.Optional
        .ToDictionary(x => x.Key, x => SettingCheckBox(x.Name, true));
    private readonly Dictionary<string, CheckBox> _pluginBoxes = new(StringComparer.OrdinalIgnoreCase);
    private readonly FlowLayoutPanel _pluginList = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
    private readonly CheckBox _closeToTrayBox = SettingCheckBox("关闭窗口时最小化到系统托盘", true);
    private readonly CheckBox _startWithWindowsBox = SettingCheckBox("开机后自动启动事刻");
    private readonly CheckBox _showTodayTodoBox = SettingCheckBox("启动 Timarker 时显示今日清单");
    private readonly CheckBox _quietHoursBox = SettingCheckBox("启用免打扰时段");
    private readonly CheckBox _activityTrackingBox = SettingCheckBox("启用本地时间追踪（数据不会上传）");
    private readonly CheckBox _activityTitlesBox = SettingCheckBox("保存窗口标题（可能含文件名、网页标题等隐私）");
    private readonly ModernNumericUpDown _activityIdleBox = NumberBox(1, 60, 1);
    private readonly ModernNumericUpDown _activityRetentionBox = NumberBox(7, 3650, 30);
    private readonly ModernTextBox _activityExcludedBox = new() { PlaceholderText = "例如：KeePass,1Password" };
    private readonly ComboBox _quietStartHourBox = TimeChoice(24);
    private readonly ComboBox _quietStartMinuteBox = TimeChoice(60);
    private readonly ComboBox _quietEndHourBox = TimeChoice(24);
    private readonly ComboBox _quietEndMinuteBox = TimeChoice(60);
    private readonly ModernNumericUpDown _leadBox = NumberBox(0, 43200, 5);
    private readonly ModernNumericUpDown _repeatMinutesBox = NumberBox(1, 1440, 5);
    private readonly ModernNumericUpDown _repeatCountBox = NumberBox(0, 20, 1);
    private readonly ModernNumericUpDown _snoozeBox = NumberBox(1, 1440, 5);
    private string _pendingBackgroundPath = "";
    private readonly string _originalAppearance;
    private readonly string _originalFeatures;
    private readonly string _originalPlugins;
    private readonly string _originalPluginCatalog;

    public SettingsForm(AppSettings settings, PluginManager plugins)
    {
        _settings = settings;
        _plugins = plugins;
        _originalAppearance = AppearanceKey(settings);
        _originalFeatures = FeatureKey(settings);
        _originalPlugins = PluginKey(settings);
        _originalPluginCatalog = PluginCatalogKey();
        L.Use(settings);
        Text = "设置";
        ClientSize = new Size(540, UiTokens.Scale(620));
        MinimumSize = new Size(556, UiTokens.Scale(659));
        StartPosition = FormStartPosition.CenterParent;
        Font = UiTokens.Font();
        BackColor = UiTokens.AppBackground;

        LoadValues();
        BuildUi();
        L.Apply(this);
    }

    public bool LanguageChanged { get; private set; }
    public bool AppearanceChanged { get; private set; }
    public bool FeaturesChanged { get; private set; }
    public bool PluginsChanged { get; private set; }
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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, UiTokens.Scale(54)));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, UiTokens.Scale(52)));

        root.Controls.Add(new Label
        {
            Text = "事刻设置",
            Dock = DockStyle.Fill,
            Font = UiTokens.Font(UiTokens.TextSection, FontStyle.Bold),
            ForeColor = UiTokens.Text
        });

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 23,
            BackColor = UiTokens.Surface,
            Padding = new Padding(16),
            AutoScroll = true
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 6; i++) form.RowStyles.Add(new RowStyle(SizeType.Absolute, UiTokens.Scale(42)));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, UiTokens.Scale(430)));
        for (var i = 0; i < 4; i++) form.RowStyles.Add(new RowStyle(SizeType.Absolute, UiTokens.Scale(27)));
        for (var i = 0; i < 11; i++) form.RowStyles.Add(new RowStyle(SizeType.Absolute, UiTokens.Scale(42)));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, UiTokens.Scale(100)));

        AddRow(form, 0, "语言", _languageBox);
        AddRow(form, 1, "外观", _themeBox);
        AddRow(form, 2, "字体大小", _fontScaleBox);
        AddRow(form, 3, "背景图片", BackgroundPicker());
        AddRow(form, 4, "背景显示", _backgroundLayoutBox);
        AddRow(form, 5, "背景透明度", OpacityPicker());
        form.Controls.Add(FeaturePicker(), 0, 6);
        form.SetColumnSpan(form.GetControlFromPosition(0, 6)!, 2);
        form.Controls.Add(_closeToTrayBox, 0, 7);
        form.SetColumnSpan(_closeToTrayBox, 2);
        form.Controls.Add(_startWithWindowsBox, 0, 8);
        form.SetColumnSpan(_startWithWindowsBox, 2);
        form.Controls.Add(_showTodayTodoBox, 0, 9);
        form.SetColumnSpan(_showTodayTodoBox, 2);
        form.Controls.Add(_quietHoursBox, 0, 10);
        form.SetColumnSpan(_quietHoursBox, 2);
        form.Controls.Add(_activityTrackingBox, 0, 11);
        form.SetColumnSpan(_activityTrackingBox, 2);
        form.Controls.Add(_activityTitlesBox, 0, 12);
        form.SetColumnSpan(_activityTitlesBox, 2);
        AddRow(form, 13, "空闲判定", WithUnit(_activityIdleBox, "分钟"));
        AddRow(form, 14, "活动保留", WithUnit(_activityRetentionBox, "天"));
        AddRow(form, 15, "排除的进程", _activityExcludedBox);
        AddRow(form, 16, "免打扰开始", TimePicker(_quietStartHourBox, _quietStartMinuteBox));
        AddRow(form, 17, "免打扰结束", TimePicker(_quietEndHourBox, _quietEndMinuteBox));
        AddRow(form, 18, "默认提前提醒", WithUnit(_leadBox, "分钟"));
        AddRow(form, 19, "默认重复间隔", WithUnit(_repeatMinutesBox, "分钟"));
        AddRow(form, 20, "默认重复次数", WithUnit(_repeatCountBox, "次"));
        AddRow(form, 21, "默认稍后提醒", WithUnit(_snoozeBox, "分钟"));
        var dataPrivacy = DataPrivacyCard();
        form.Controls.Add(dataPrivacy, 0, 22);
        form.SetColumnSpan(dataPrivacy, 2);
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
        _themeBox.Items.AddRange(["跟随系统", "浅色", "深色"]);
        _themeBox.SelectedIndex = _settings.Theme switch { "light" => 1, "dark" => 2, _ => 0 };
        _fontScaleBox.Items.AddRange(["较小 · 90%", "标准 · 100%", "较大 · 110%", "特大 · 125%"]);
        _fontScaleBox.SelectedIndex = _settings.FontScalePercent switch { <= 90 => 0, >= 125 => 3, >= 110 => 2, _ => 1 };
        _backgroundLayoutBox.Items.AddRange(["填充（推荐）", "适应", "平铺"]);
        _backgroundLayoutBox.SelectedIndex = _settings.BackgroundImageLayout switch { "fit" => 1, "tile" => 2, _ => 0 };
        _pendingBackgroundPath = _settings.BackgroundImagePath;
        RefreshBackgroundName();
        _backgroundOpacity.Value = Math.Clamp(_settings.BackgroundOpacity, 0, 100);
        RefreshOpacityText();
        foreach (var (key, box) in _featureBoxes) box.Checked = _settings.FeatureEnabled(key);
        _closeToTrayBox.Checked = _settings.CloseToTray;
        _startWithWindowsBox.Checked = _settings.StartWithWindows;
        _showTodayTodoBox.Checked = _settings.ShowTodayTodoOnStartup;
        _quietHoursBox.Checked = _settings.QuietHoursEnabled;
        _activityTrackingBox.Checked = _settings.ActivityTrackingEnabled;
        _activityTitlesBox.Checked = _settings.ActivityStoreWindowTitles;
        _activityIdleBox.Value = Clamp(_settings.ActivityIdleMinutes, _activityIdleBox);
        _activityRetentionBox.Value = Clamp(_settings.ActivityRetentionDays, _activityRetentionBox);
        _activityExcludedBox.Text = string.Join(",", _settings.ActivityExcludedProcesses);
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
        _settings.Theme = _themeBox.SelectedIndex switch { 1 => "light", 2 => "dark", _ => "system" };
        _settings.FontScalePercent = _fontScaleBox.SelectedIndex switch { 0 => 90, 2 => 110, 3 => 125, _ => 100 };
        _settings.BackgroundImageLayout = _backgroundLayoutBox.SelectedIndex switch { 1 => "fit", 2 => "tile", _ => "fill" };
        _settings.BackgroundOpacity = _backgroundOpacity.Value;
        _settings.BackgroundImagePath = SaveBackgroundImage(_pendingBackgroundPath);
        AppearanceChanged = _originalAppearance != AppearanceKey(_settings);
        _settings.DisabledFeatures = _featureBoxes.Where(x => !x.Value.Checked).Select(x => x.Key).ToList();
        FeaturesChanged = _originalFeatures != FeatureKey(_settings);
        _settings.DisabledPlugins = _pluginBoxes.Where(x => !x.Value.Checked).Select(x => x.Key).ToList();
        PluginsChanged = _originalPlugins != PluginKey(_settings) || _originalPluginCatalog != PluginCatalogKey();
        _settings.CloseToTray = _closeToTrayBox.Checked;
        _settings.StartWithWindows = _startWithWindowsBox.Checked;
        _settings.ShowTodayTodoOnStartup = _showTodayTodoBox.Checked;
        _settings.QuietHoursEnabled = _quietHoursBox.Checked;
        _settings.ActivityTrackingEnabled = _activityTrackingBox.Checked;
        _settings.ActivityStoreWindowTitles = _activityTitlesBox.Checked;
        _settings.ActivityIdleMinutes = (int)_activityIdleBox.Value;
        _settings.ActivityRetentionDays = (int)_activityRetentionBox.Value;
        _settings.ActivityExcludedProcesses = _activityExcludedBox.Text.Split([',', '，', ';', '；'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        _settings.QuietHoursStart = SelectedTime(_quietStartHourBox, _quietStartMinuteBox);
        _settings.QuietHoursEnd = SelectedTime(_quietEndHourBox, _quietEndMinuteBox);
        _settings.DefaultReminderLeadMinutes = (int)_leadBox.Value;
        _settings.DefaultReminderRepeatMinutes = (int)_repeatMinutesBox.Value;
        _settings.DefaultReminderRepeatCount = (int)_repeatCountBox.Value;
        _settings.DefaultSnoozeMinutes = (int)_snoozeBox.Value;
        if (TopLevel) DialogResult = DialogResult.OK;
        else SettingsSaved?.Invoke(this, EventArgs.Empty);
    }

    private Control BackgroundPicker()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Margin = Padding.Empty };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62));
        var choose = Button("选择", false);
        var clear = Button("清除", false);
        choose.Dock = clear.Dock = DockStyle.Fill;
        choose.Margin = new Padding(6, 3, 0, 3);
        clear.Margin = new Padding(6, 3, 0, 3);
        choose.Click += (_, _) => ChooseBackground();
        clear.Click += (_, _) => { _pendingBackgroundPath = ""; RefreshBackgroundName(); };
        panel.Controls.Add(_backgroundName, 0, 0);
        panel.Controls.Add(choose, 1, 0);
        panel.Controls.Add(clear, 2, 0);
        return panel;
    }

    private Control OpacityPicker()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = Padding.Empty };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
        _backgroundOpacity.ValueChanged += (_, _) => RefreshOpacityText();
        panel.Controls.Add(_backgroundOpacity, 0, 0);
        panel.Controls.Add(_opacityValue, 1, 0);
        return panel;
    }

    private Control FeaturePicker()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            Margin = Padding.Empty,
            Padding = new Padding(UiTokens.Space3),
            BackColor = UiTokens.SurfaceSubtle
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, UiTokens.Scale(36)));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, UiTokens.Scale(178)));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, UiTokens.Scale(52)));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(SectionTitle("功能模块", "只显示你会用到的入口；核心的“今日、创建、设置”始终保留。"));

        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Margin = Padding.Empty };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        foreach (var group in FeatureCatalog.Optional.GroupBy(x => x.Group).OrderBy(x => x.Key))
        {
            var card = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                BackColor = UiTokens.SurfaceSubtle,
                Margin = new Padding(0, 0, group.Key % 2 == 0 ? UiTokens.Space2 : 0, UiTokens.Space2),
                Padding = new Padding(UiTokens.Space3, UiTokens.Space2, UiTokens.Space3, UiTokens.Space2)
            };
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, UiTokens.Scale(24)));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            card.Controls.Add(new Label
            {
                Text = FeatureCatalog.GroupName(group.Key),
                Dock = DockStyle.Fill,
                Font = UiTokens.Font(UiTokens.TextSmall, FontStyle.Bold),
                ForeColor = UiTokens.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft
            });

            var choices = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, Margin = Padding.Empty, Padding = Padding.Empty };
            foreach (var module in group)
            {
                var box = _featureBoxes[module.Key];
                box.Appearance = Appearance.Button;
                box.AutoSize = false;
                box.Width = Math.Max(78, TextRenderer.MeasureText(module.Name, Font).Width + 30);
                box.Height = UiTokens.CompactHeight;
                box.TextAlign = ContentAlignment.MiddleCenter;
                box.Margin = new Padding(0, 0, UiTokens.Space2, UiTokens.Space1);
                box.FlatStyle = FlatStyle.Flat;
                box.FlatAppearance.BorderSize = 0;
                box.FlatAppearance.MouseOverBackColor = UiTokens.Hover;
                box.CheckedChanged += (_, _) => StyleFeatureToggle(box);
                StyleFeatureToggle(box);
                ModernUi.Outline(box, UiTokens.RadiusSmall, () => box.FlatAppearance.BorderColor);
                choices.Controls.Add(box);
            }
            card.Controls.Add(choices, 0, 1);
            ModernUi.Outline(card, UiTokens.RadiusMedium, () => UiTokens.Border);
            grid.Controls.Add(card, group.Key % 2, group.Key / 2);
        }
        root.Controls.Add(grid, 0, 1);

        var pluginHeader = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Margin = Padding.Empty };
        pluginHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pluginHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));
        pluginHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));
        pluginHeader.Controls.Add(SectionTitle("插件", "插件会在本机运行，仅安装你信任的来源。"));
        var open = Button("打开目录", false);
        var install = Button("安装插件", true);
        open.Margin = install.Margin = new Padding(UiTokens.Space2, UiTokens.Space2, 0, UiTokens.Space2);
        open.Click += (_, _) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(AppPaths.PluginsDirectory) { UseShellExecute = true });
        install.Click += (_, _) => InstallPlugin();
        pluginHeader.Controls.Add(open, 1, 0);
        pluginHeader.Controls.Add(install, 2, 0);
        root.Controls.Add(pluginHeader, 0, 2);

        RefreshPluginList();
        root.Controls.Add(_pluginList, 0, 3);
        ModernUi.Outline(root, UiTokens.RadiusMedium, () => UiTokens.Border);
        return root;
    }

    private static Control SectionTitle(string title, string description)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Margin = Padding.Empty };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, UiTokens.Scale(20)));
        panel.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, Font = UiTokens.Font(UiTokens.TextBody, FontStyle.Bold), ForeColor = UiTokens.Text });
        panel.Controls.Add(new Label { Text = description, Dock = DockStyle.Fill, Font = UiTokens.Font(UiTokens.TextSmall), ForeColor = UiTokens.TextMuted });
        return panel;
    }

    private void RefreshPluginList()
    {
        _pluginList.SuspendLayout();
        _pluginList.Controls.Clear();
        _pluginBoxes.Clear();
        if (_plugins.Plugins.Count == 0)
        {
            _pluginList.Controls.Add(new Label { Text = "还没有安装插件。可安装 .tmplugin 或 .zip 插件包。", AutoSize = true, ForeColor = UiTokens.TextMuted, Margin = new Padding(UiTokens.Space2) });
        }
        foreach (var plugin in _plugins.Plugins)
        {
            var row = new TableLayoutPanel { Width = Math.Max(480, _pluginList.ClientSize.Width - 24), Height = UiTokens.Scale(56), ColumnCount = 3, BackColor = UiTokens.Surface, Margin = new Padding(0, 0, 0, UiTokens.Space2), Padding = new Padding(UiTokens.Space3, UiTokens.Space1, UiTokens.Space2, UiTokens.Space1) };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
            var detail = new Label
            {
                Text = $"{plugin.Manifest.Name}  ·  {plugin.Manifest.Version}\n{(plugin.Error is null ? plugin.Manifest.Description : "无法加载：" + plugin.Error)}",
                Dock = DockStyle.Fill,
                ForeColor = plugin.Error is null ? UiTokens.Text : UiTokens.Danger,
                AutoEllipsis = true
            };
            var toggle = SettingCheckBox("启用", true);
            toggle.Appearance = Appearance.Button;
            toggle.Checked = plugin.Error is null && !_settings.DisabledPlugins.Contains(plugin.Manifest.Id, StringComparer.OrdinalIgnoreCase);
            toggle.Dock = DockStyle.Fill;
            toggle.Margin = new Padding(UiTokens.Space2);
            toggle.Enabled = plugin.Error is null;
            toggle.CheckedChanged += (_, _) => StyleFeatureToggle(toggle);
            StyleFeatureToggle(toggle);
            ModernUi.Outline(toggle, UiTokens.RadiusSmall, () => toggle.Checked ? UiTokens.Focus : UiTokens.Border);
            _pluginBoxes[plugin.Manifest.Id] = toggle;
            var remove = Button("卸载", false);
            remove.ForeColor = UiTokens.Danger;
            remove.Margin = new Padding(UiTokens.Space2);
            remove.Click += (_, _) => UninstallPlugin(plugin);
            row.Controls.Add(detail, 0, 0);
            row.Controls.Add(toggle, 1, 0);
            row.Controls.Add(remove, 2, 0);
            ModernUi.Outline(row, UiTokens.RadiusSmall, () => UiTokens.Border);
            _pluginList.Controls.Add(row);
        }
        _pluginList.ResumeLayout();
    }

    private void InstallPlugin()
    {
        using var dialog = new OpenFileDialog { Title = "安装 Timarker 插件", Filter = "Timarker 插件 (*.tmplugin;*.zip)|*.tmplugin;*.zip" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (MessageBox.Show("插件代码会在 Timarker 中运行。请确认该插件来自你信任的来源。", "安装插件", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK) return;
        try
        {
            _plugins.Install(dialog.FileName);
            RefreshPluginList();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "无法安装插件", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UninstallPlugin(PluginDescriptor plugin)
    {
        if (MessageBox.Show($"确定卸载“{plugin.Manifest.Name}”吗？", "卸载插件", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK) return;
        try
        {
            _plugins.Uninstall(plugin);
            RefreshPluginList();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "暂时无法卸载", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private static void StyleFeatureToggle(CheckBox box)
    {
        box.BackColor = box.Checked ? UiTokens.PrimarySoft : UiTokens.Surface;
        box.ForeColor = box.Checked ? UiTokens.Primary : UiTokens.Text;
        box.FlatAppearance.BorderColor = box.Checked ? UiTokens.Focus : UiTokens.Border;
        box.Font = UiTokens.Font(UiTokens.TextBody, box.Checked ? FontStyle.Bold : FontStyle.Regular);
        box.Invalidate();
    }

    private Control DataPrivacyCard()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = AppTheme.SurfaceAlt, Margin = new Padding(0, 8, 0, 0), Padding = new Padding(12, 8, 10, 8) };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        panel.Controls.Add(new Label
        {
            Text = "数据与隐私\n事项、复盘和时间追踪默认仅保存在此电脑。自动备份、恢复文件和崩溃日志也位于数据目录。",
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.Text,
            AutoEllipsis = true
        });
        var open = Button("打开数据目录", false);
        open.Width = 108;
        open.Click += (_, _) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", AppPaths.DataDirectory) { UseShellExecute = true });
        panel.Controls.Add(open, 1, 0);
        ModernUi.Round(panel, 10);
        return panel;
    }

    private void ChooseBackground()
    {
        using var dialog = new OpenFileDialog { Title = "选择主窗口背景", Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.webp" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _pendingBackgroundPath = dialog.FileName;
        RefreshBackgroundName();
    }

    private void RefreshBackgroundName() => _backgroundName.Text = string.IsNullOrWhiteSpace(_pendingBackgroundPath) ? "使用默认背景" : Path.GetFileName(_pendingBackgroundPath);
    private void RefreshOpacityText() => _opacityValue.Text = $"{_backgroundOpacity.Value}%";

    private static string SaveBackgroundImage(string source)
    {
        if (string.IsNullOrWhiteSpace(source) || !File.Exists(source)) return "";
        var directory = Path.Combine(AppPaths.DataDirectory, "backgrounds");
        Directory.CreateDirectory(directory);
        var target = Path.Combine(directory, $"main{Path.GetExtension(source).ToLowerInvariant()}");
        if (!Path.GetFullPath(source).Equals(Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase)) File.Copy(source, target, true);
        return target;
    }

    private static string AppearanceKey(AppSettings settings) => $"{settings.Theme}|{settings.FontScalePercent}|{settings.BackgroundImagePath}|{settings.BackgroundImageLayout}|{settings.BackgroundOpacity}";
    private static string FeatureKey(AppSettings settings) => string.Join('|', settings.DisabledFeatures.OrderBy(x => x));
    private static string PluginKey(AppSettings settings) => string.Join('|', settings.DisabledPlugins.OrderBy(x => x));
    private string PluginCatalogKey() => string.Join('|', _plugins.Plugins.Select(x => x.Manifest.Id).OrderBy(x => x));

    private static void AddRow(TableLayoutPanel form, int row, string label, Control control)
    {
        form.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty,
            ForeColor = UiTokens.TextMuted
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
            Height = UiTokens.ControlHeight,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? UiTokens.Primary : UiTokens.Surface,
            ForeColor = primary ? Color.White : UiTokens.Text,
            Margin = new Padding(8, 8, 0, 0)
        };
        button.FlatAppearance.BorderColor = primary ? UiTokens.Primary : UiTokens.Border;
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
            ForeColor = UiTokens.TextMuted,
            Font = UiTokens.Font(UiTokens.TextEmphasis, FontStyle.Bold)
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
