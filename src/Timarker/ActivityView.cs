using System.Text;
using Timarker.Models;
using Timarker.Services;

namespace Timarker;

internal sealed class ActivityView : UserControl
{
    private static readonly string[] Categories = ["未分类", "工作", "学习", "沟通", "浏览", "娱乐", "生活"];
    private readonly ActivityStore _store;
    private readonly ActivityCollector _collector;
    private readonly IReadOnlyList<Project> _projects;
    private readonly IReadOnlyList<EventItem> _events;
    private readonly List<ActivityRecord> _records;
    private readonly Action _saveMainData;
    private readonly AppSettings _settings;
    private readonly Label _focus = Metric();
    private readonly Label _idle = Metric();
    private readonly Label _topApp = Metric();
    private readonly Label _unclassified = Metric();
    private readonly Label _dateTitle = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = UiTokens.Font(UiTokens.TextSection, FontStyle.Bold), ForeColor = UiTokens.Primary, BackColor = UiTokens.Selected, Margin = new Padding(6, 5, 6, 5) };
    private readonly Label _weekTrend = new() { Dock = DockStyle.Fill, ForeColor = AppTheme.Muted, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ActivityTimeline _timeline = new();
    private readonly Panel _timelineScroll = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.Surface };
    private readonly ActivityCategoryChart _categoryChart = new();
    private readonly ActivityDetailsPanel _details = new();
    private readonly FlowLayoutPanel _stats = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(8) };
    private readonly ComboBox _category = new ModernComboBox { Width = 110 };
    private readonly ComboBox _project = new ModernComboBox { Width = 150 };
    private readonly ComboBox _event = new ModernComboBox { Width = 180 };
    private readonly ModernTextBox _titleMatch = new() { Width = 150, PlaceholderText = "标题包含（可选）" };
    private DateTime _day = DateTime.Today;
    private DateTime _pendingScrollTarget = DateTime.Today;

    public ActivityView(ActivityStore store, ActivityCollector collector, IReadOnlyList<Project> projects, IReadOnlyList<EventItem> events, List<ActivityRecord> records, AppSettings settings, Action saveMainData)
    {
        _store = store;
        _collector = collector;
        _projects = projects;
        _events = events;
        _records = records;
        _settings = settings;
        _saveMainData = saveMainData;
        Dock = DockStyle.Fill;
        BackColor = AppTheme.AppBack;
        Font = UiTokens.Font();
        BuildUi();
        _collector.ActivityChanged += CollectorChanged;
        RefreshView();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _collector.ActivityChanged -= CollectorChanged;
        base.Dispose(disposing);
    }

    private void BuildUi()
    {
        RefreshCategoryChoices();
        _category.SelectedIndex = 0;
        FillLinks();
        _timeline.SessionSelected += LoadSelected;
        _timeline.SessionContextRequested += (session, point) => ShowActivityMenu(session, _timeline, point);
        _details.SessionContextRequested += ShowActivityMenu;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, Padding = new Padding(UiTokens.Space5, UiTokens.RadiusLarge, UiTokens.Space5, UiTokens.RadiusLarge), BackColor = UiTokens.AppBackground };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.Controls.Add(Header(), 0, 0);
        root.Controls.Add(DateBar(), 0, 1);
        root.Controls.Add(Workspace(), 0, 2);
        root.Controls.Add(RuleBar(), 0, 3);
        Controls.Add(root);
    }

    private Control Header()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 540));
        var words = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Margin = Padding.Empty };
        words.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        words.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        words.Controls.Add(new Label { Text = "时间追踪", Dock = DockStyle.Fill, Font = UiTokens.Font(UiTokens.TextPageTitle, FontStyle.Bold), ForeColor = UiTokens.Text, TextAlign = ContentAlignment.BottomLeft, AutoEllipsis = true });
        words.Controls.Add(new Label { Text = "看看一天中具体什么时间做了什么。", Dock = DockStyle.Fill, Font = UiTokens.Font(), ForeColor = UiTokens.TextMuted, TextAlign = ContentAlignment.TopLeft, AutoEllipsis = true }, 0, 1);
        panel.Controls.Add(words);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        var pause = Button(_collector.IsPaused ? "继续记录" : "暂停记录", true, 96);
        pause.Click += (_, _) => { _collector.TogglePaused(); pause.Text = _collector.IsPaused ? "继续记录" : "暂停记录"; };
        var export = Button("导出 CSV", false, 96);
        export.Click += (_, _) => ExportCsv();
        var clear = Button("清理数据", false, 88);
        clear.ForeColor = UiTokens.Danger;
        clear.Click += (_, _) => ClearActivities();
        var maintain = Button("整理数据库", false, 96);
        maintain.Click += (_, _) => MaintainDatabase();
        var record = Button("生成今日记录", false, 110);
        record.Click += (_, _) => GenerateDailyRecord();
        actions.Controls.Add(pause);
        actions.Controls.Add(export);
        actions.Controls.Add(clear);
        actions.Controls.Add(maintain);
        actions.Controls.Add(record);
        panel.Controls.Add(actions, 1, 0);
        return panel;
    }

    private Control Workspace()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Height = 600,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 7,
            SplitterDistance = ActivityTimeline.HeaderHeight + ActivityTimeline.LaneHeight * 3 + 10,
            Panel1MinSize = ActivityTimeline.HeaderHeight + ActivityTimeline.LaneHeight * 3 + 2,
            Panel2MinSize = 170,
            BackColor = AppTheme.Border,
            IsSplitterFixed = false
        };
        split.Panel1.BackColor = AppTheme.AppBack;
        split.Panel2.BackColor = AppTheme.AppBack;
        split.Panel1.Controls.Add(TimelineCard());
        split.Panel2.Controls.Add(_details);
        return split;
    }

    private Control Metrics()
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, Margin = Padding.Empty };
        for (var i = 0; i < 4; i++) row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        row.Controls.Add(MetricCard("专注使用", _focus), 0, 0);
        row.Controls.Add(MetricCard("空闲时间", _idle), 1, 0);
        row.Controls.Add(MetricCard("最常使用", _topApp), 2, 0);
        row.Controls.Add(MetricCard("未分类", _unclassified), 3, 0);
        return row;
    }

    private Control DateBar()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, BackColor = AppTheme.Surface, Padding = new Padding(6, 1, 6, 1), Margin = new Padding(0, 0, 0, 4) };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
        var previous = Button("‹", false, 36); previous.Click += (_, _) => ChangeDay(-1);
        var today = Button("今", false, 36); today.Click += (_, _) => { _day = DateTime.Today; RefreshView(); };
        var next = Button("›", false, 36); next.Click += (_, _) => ChangeDay(1);
        panel.Controls.Add(previous, 0, 0);
        panel.Controls.Add(today, 1, 0);
        panel.Controls.Add(_dateTitle, 2, 0);
        panel.Controls.Add(_weekTrend, 3, 0);
        panel.Controls.Add(next, 4, 0);
        ModernUi.Round(_dateTitle, 8);
        ModernUi.Outline(panel, 10, () => AppTheme.Border);
        return panel;
    }

    private Control RuleBar()
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(10, 8, 10, 6), BackColor = AppTheme.Surface };
        panel.Controls.Add(new Label { Text = "归类所选应用", AutoSize = true, Margin = new Padding(0, 10, 10, 0), ForeColor = AppTheme.Muted });
        panel.Controls.Add(_category);
        panel.Controls.Add(_project);
        panel.Controls.Add(_event);
        panel.Controls.Add(_titleMatch);
        var save = Button("保存规则", true, 88);
        save.Click += (_, _) => SaveRule();
        panel.Controls.Add(save);
        ModernUi.Round(panel, 12);
        return panel;
    }

    public void RefreshView()
    {
        var start = _day.Date;
        var end = start.AddDays(1);
        var sessions = _store.Sessions.Where(x => x.EndedAt > start && x.StartedAt < end).OrderBy(x => x.StartedAt).ToList();
        _dateTitle.Text = _day.Date == DateTime.Today ? "今天" : _day.ToString("M 月 d 日");
        _timeline.SetData(_day, sessions);
        _categoryChart.SetData(ActivityInsights.ByCategory(sessions.Where(x => !x.IsIdle), start, end));
        _details.SetData(sessions, start, end);
        _pendingScrollTarget = sessions.LastOrDefault()?.EndedAt ?? _day;
        if (IsHandleCreated) BeginInvoke((MethodInvoker)(() => ScrollTimelineTo(_pendingScrollTarget)));

        var focus = sessions.Where(x => !x.IsIdle && x.ProcessName != "excluded").Aggregate(TimeSpan.Zero, (sum, x) => sum + ActivityInsights.Overlap(x, start, end));
        var idle = sessions.Where(x => x.IsIdle).Aggregate(TimeSpan.Zero, (sum, x) => sum + ActivityInsights.Overlap(x, start, end));
        var unknown = sessions.Where(x => !x.IsIdle && x.Category == "未分类").Aggregate(TimeSpan.Zero, (sum, x) => sum + ActivityInsights.Overlap(x, start, end));
        var top = sessions.Where(x => !x.IsIdle && x.ProcessName != "excluded").GroupBy(x => x.AppName).OrderByDescending(g => g.Sum(x => x.Duration.Ticks)).FirstOrDefault();
        _focus.Text = ActivityInsights.DurationText(focus);
        _idle.Text = ActivityInsights.DurationText(idle);
        _unclassified.Text = ActivityInsights.DurationText(unknown);
        _topApp.Text = top?.Key ?? "暂无";
        RefreshWeekTrend();
    }

    private void RefreshStats(List<AppActivitySession> sessions, DateTime start, DateTime end)
    {
        _stats.SuspendLayout();
        _stats.Controls.Clear();
        foreach (var pair in ActivityInsights.ByCategory(sessions, start, end).OrderByDescending(x => x.Value))
        {
            var card = new TableLayoutPanel { Width = 260, Height = 58, ColumnCount = 2, BackColor = AppTheme.SurfaceAlt, Margin = new Padding(4) };
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
            card.Controls.Add(new Label { Text = pair.Key, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0), ForeColor = AppTheme.Text });
            card.Controls.Add(new Label { Text = ActivityInsights.DurationText(pair.Value), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 12, 0), ForeColor = AppTheme.Muted }, 1, 0);
            ModernUi.Round(card, 10);
            _stats.Controls.Add(card);
        }
        foreach (var group in sessions.Where(x => x.ProjectId is not null).GroupBy(x => x.ProjectId).OrderByDescending(x => x.Sum(s => s.Duration.Ticks)))
        {
            var project = _projects.FirstOrDefault(x => x.Id == group.Key);
            if (project is null) continue;
            var label = new Label { Width = 260, Height = 34, Text = $"项目 · {project.Name}    {ActivityInsights.DurationText(TimeSpan.FromTicks(group.Sum(x => x.Duration.Ticks)))}", ForeColor = AppTheme.Muted, Padding = new Padding(10, 8, 0, 0) };
            _stats.Controls.Add(label);
        }
        _stats.ResumeLayout();
    }

    private void RefreshWeekTrend()
    {
        var start = _day.Date.AddDays(-6);
        var total = _store.Sessions.Where(x => !x.IsIdle).Aggregate(TimeSpan.Zero, (sum, x) => sum + ActivityInsights.Overlap(x, start, _day.Date.AddDays(1)));
        _weekTrend.Text = $"近 7 天有效使用 {ActivityInsights.DurationText(total)}";
    }

    private void SaveRule()
    {
        if (_timeline.SelectedSession is not AppActivitySession session || session.IsIdle) return;
        var rule = _store.Rules.FirstOrDefault(x => x.ProcessName.Equals(session.ProcessName, StringComparison.OrdinalIgnoreCase) && x.TitleContains == _titleMatch.Text.Trim());
        rule ??= new AppActivityRule { ProcessName = session.ProcessName, TitleContains = _titleMatch.Text.Trim() };
        if (!_store.Rules.Contains(rule)) _store.Rules.Add(rule);
        rule.Category = _category.SelectedItem?.ToString() ?? "未分类";
        rule.ProjectId = (_project.SelectedItem as Choice)?.Id;
        rule.EventId = (_event.SelectedItem as Choice)?.Id;
        foreach (var item in _store.Sessions.Where(x => rule.Matches(x.ProcessName, x.WindowTitle)))
        {
            item.Category = rule.Category;
            item.ProjectId = rule.ProjectId;
            item.EventId = rule.EventId;
        }
        _store.Save(_settings.ActivityRetentionDays, saveAllSessions: true);
        RefreshView();
    }

    private void LoadSelected(AppActivitySession session)
    {
        _category.SelectedItem = session.Category;
        SelectChoice(_project, session.ProjectId);
        SelectChoice(_event, session.EventId);
    }

    private Control TimelineCard()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, BackColor = AppTheme.Surface, Margin = new Padding(0, 4, 0, 8), Padding = Padding.Empty };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = Padding.Empty, Padding = Padding.Empty, BackColor = AppTheme.Surface };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var labels = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, BackColor = AppTheme.SurfaceAlt };
        labels.RowStyles.Add(new RowStyle(SizeType.Absolute, ActivityTimeline.HeaderHeight));
        for (var i = 0; i < 3; i++) labels.RowStyles.Add(new RowStyle(SizeType.Absolute, ActivityTimeline.LaneHeight));
        labels.Controls.Add(new Label { Text = "时间", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = AppTheme.Muted });
        labels.Controls.Add(LaneLabel("标签", UiTokens.Primary), 0, 1);
        labels.Controls.Add(LaneLabel("应用", UiTokens.Violet), 0, 2);
        labels.Controls.Add(LaneLabel("文档／页面", UiTokens.Warning), 0, 3);
        _timelineScroll.Controls.Add(_timeline);
        shell.Controls.Add(labels, 0, 0); shell.Controls.Add(_timelineScroll, 1, 0);
        ModernUi.Outline(shell, 10, () => AppTheme.Border);
        root.Controls.Add(shell);
        return root;
    }

    private static Label LaneLabel(string text, Color color) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        ForeColor = color,
        BackColor = AppTheme.IsDark ? AppTheme.SurfaceAlt : Color.FromArgb(248, 250, 252),
        Font = UiTokens.Font(UiTokens.TextBody, FontStyle.Bold)
    };

    private void ScrollTimelineTo(DateTime time)
    {
        var x = Math.Max(0, (int)time.TimeOfDay.TotalMinutes * ActivityTimeline.PixelsPerMinute - _timelineScroll.ClientSize.Width + 140);
        _timelineScroll.AutoScrollPosition = new Point(x, 0);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        BeginInvoke((MethodInvoker)(() => ScrollTimelineTo(_pendingScrollTarget)));
    }

    private void ShowActivityMenu(AppActivitySession session, Control source, Point point)
    {
        if (session.IsIdle) return;
        var menu = new ModernContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem($"{session.AppName}  ·  {session.StartedAt:HH:mm}–{session.EndedAt:HH:mm}") { Enabled = false, Tag = "header" });
        menu.Items.Add(new ToolStripSeparator());
        foreach (var name in ActivityCategories())
        {
            var item = new ToolStripMenuItem(name) { Tag = session.Category == name ? "checked" : null };
            item.Click += (_, _) => ApplyActivityCategory(session, name);
            menu.Items.Add(item);
        }
        menu.Items.Add(new ToolStripSeparator());
        var create = new ToolStripMenuItem("新建活动分类…");
        create.Click += (_, _) =>
        {
            var name = PromptForCategory();
            if (!string.IsNullOrWhiteSpace(name)) ApplyActivityCategory(session, name.Trim());
        };
        menu.Items.Add(create);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("关联项目／事项…", null, (_, _) => { LoadSelected(session); _project.Focus(); }));
        menu.Show(source, point);
    }

    private void ApplyActivityCategory(AppActivitySession session, string category)
    {
        var rule = _store.Rules.FirstOrDefault(x => x.ProcessName.Equals(session.ProcessName, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(x.TitleContains));
        rule ??= new AppActivityRule { ProcessName = session.ProcessName };
        if (!_store.Rules.Contains(rule)) _store.Rules.Add(rule);
        rule.Category = category;
        foreach (var item in _store.Sessions.Where(x => x.ProcessName.Equals(session.ProcessName, StringComparison.OrdinalIgnoreCase))) item.Category = category;
        _store.Save(_settings.ActivityRetentionDays, saveAllSessions: true);
        RefreshCategoryChoices();
        RefreshView();
    }

    private IEnumerable<string> ActivityCategories() => Categories.Concat(_store.Rules.Select(x => x.Category)).Concat(_store.Sessions.Select(x => x.Category)).Where(x => !string.IsNullOrWhiteSpace(x) && x != "空闲").Distinct(StringComparer.OrdinalIgnoreCase);
    private void RefreshCategoryChoices()
    {
        var selected = _category.SelectedItem?.ToString();
        _category.Items.Clear();
        _category.Items.AddRange(ActivityCategories().Cast<object>().ToArray());
        _category.SelectedItem = selected;
        if (_category.SelectedIndex < 0 && _category.Items.Count > 0) _category.SelectedIndex = 0;
    }

    private string? PromptForCategory()
    {
        using var dialog = new Form { Text = "新建活动分类", ClientSize = new Size(360, 150), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, StartPosition = FormStartPosition.CenterParent, BackColor = AppTheme.AppBack, Font = Font };
        var input = new ModernTextBox { PlaceholderText = "例如：开发、调研、休息", Dock = DockStyle.Top, Height = 38 };
        var ok = Button("创建", true, 82); var cancel = Button("取消", false, 82);
        ok.DialogResult = DialogResult.OK; cancel.DialogResult = DialogResult.Cancel;
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 8, 0, 0) };
        actions.Controls.Add(ok); actions.Controls.Add(cancel);
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) }; body.Controls.Add(input); body.Controls.Add(actions); dialog.Controls.Add(body);
        dialog.AcceptButton = ok; dialog.CancelButton = cancel;
        return dialog.ShowDialog(FindForm()) == DialogResult.OK ? input.Text : null;
    }

    private void FillLinks()
    {
        _project.Items.Add(new Choice(null, "不关联项目"));
        foreach (var project in _projects.OrderBy(x => x.Name)) _project.Items.Add(new Choice(project.Id, project.Name));
        _event.Items.Add(new Choice(null, "不关联事项"));
        foreach (var item in _events.Where(x => x.Status is not EventStatus.Done).OrderBy(x => x.Title).Take(100)) _event.Items.Add(new Choice(item.Id, item.Title));
        _project.SelectedIndex = _event.SelectedIndex = 0;
    }

    private void ExportCsv()
    {
        using var dialog = new SaveFileDialog { Filter = "CSV 文件|*.csv", FileName = $"Timarker-活动-{_day:yyyy-MM-dd}.csv" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var rows = _store.Sessions.Where(x => x.StartedAt.Date == _day.Date).OrderBy(x => x.StartedAt);
        using var writer = new StreamWriter(dialog.FileName, false, new UTF8Encoding(true));
        writer.WriteLine("开始,结束,应用,进程,分类,窗口标题");
        foreach (var item in rows) writer.WriteLine(string.Join(',', Csv(item.StartedAt.ToString("yyyy-MM-dd HH:mm:ss")), Csv(item.EndedAt.ToString("yyyy-MM-dd HH:mm:ss")), Csv(item.AppName), Csv(item.ProcessName), Csv(item.Category), Csv(item.WindowTitle)));
    }

    private void ClearActivities()
    {
        if (MessageBox.Show("确定清空全部应用活动记录吗？分类规则会保留。", "清空活动记录", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK) return;
        _collector.ResetCurrent();
        _store.ClearSessions();
        RefreshView();
    }

    private void MaintainDatabase()
    {
        _collector.ResetCurrent();
        _store.Maintain(_settings.ActivityRetentionDays);
        RefreshView();
        MessageBox.Show($"数据库整理完成，已清理超过 {_settings.ActivityRetentionDays} 天的活动数据。", "整理完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void GenerateDailyRecord()
    {
        var start = _day.Date;
        var end = start.AddDays(1);
        var sessions = _store.Sessions.Where(x => !x.IsIdle && x.EndedAt > start && x.StartedAt < end).ToList();
        if (sessions.Count == 0) return;
        var categories = ActivityInsights.ByCategory(sessions, start, end).OrderByDescending(x => x.Value)
            .Select(x => $"{x.Key} {ActivityInsights.DurationText(x.Value)}");
        var topApps = sessions.GroupBy(x => x.AppName).OrderByDescending(x => x.Sum(s => s.Duration.Ticks)).Take(3)
            .Select(x => $"{x.Key} {ActivityInsights.DurationText(TimeSpan.FromTicks(x.Sum(s => s.Duration.Ticks)))}");
        var title = $"{_day:yyyy-MM-dd} 活动回顾";
        var record = _records.FirstOrDefault(x => x.Kind == ActivityRecordKind.Note && x.Title == title);
        record ??= new ActivityRecord { Kind = ActivityRecordKind.Note, Title = title, OccurredAt = _day.Date.AddHours(23).AddMinutes(59) };
        if (!_records.Contains(record)) _records.Add(record);
        record.Detail = $"分类：{string.Join("；", categories)}\r\n常用应用：{string.Join("；", topApps)}";
        _saveMainData();
        MessageBox.Show("今日活动摘要已写入记录页。", "记录已生成", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void CollectorChanged(object? sender, EventArgs e)
    {
        if (!Visible || !IsHandleCreated) return;
        BeginInvoke((MethodInvoker)RefreshView);
    }

    private void ChangeDay(int days) { _day = _day.AddDays(days); RefreshView(); }
    private string LinkedText(AppActivitySession item)
    {
        var project = item.ProjectId is Guid projectId ? _projects.FirstOrDefault(x => x.Id == projectId)?.Name : null;
        var activity = item.EventId is Guid eventId ? _events.FirstOrDefault(x => x.Id == eventId)?.Title : null;
        return string.Concat(project is null ? "" : $" · {project}", activity is null ? "" : $" · {activity}");
    }
    private static string Csv(string text) => $"\"{text.Replace("\"", "\"\"")}\"";
    private static void SelectChoice(ComboBox box, Guid? id) { for (var i = 0; i < box.Items.Count; i++) if ((box.Items[i] as Choice)?.Id == id) { box.SelectedIndex = i; return; } }
    private sealed record Choice(Guid? Id, string Name) { public override string ToString() => Name; }

    private static Label Metric() => new() { Dock = DockStyle.Fill, Font = UiTokens.Font(UiTokens.TextSection, FontStyle.Bold), ForeColor = UiTokens.Text, TextAlign = ContentAlignment.MiddleLeft };
    private static Control MetricCard(string title, Label value)
    {
        var card = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Margin = new Padding(0, 4, 10, 4), Padding = new Padding(16, 10, 12, 8), BackColor = AppTheme.Surface };
        card.Controls.Add(value);
        card.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, ForeColor = AppTheme.Muted }, 0, 1);
        ModernUi.Round(card, 12);
        return card;
    }
    private static Control Card(Control child, Padding padding) { var panel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0), Padding = padding, BackColor = AppTheme.Surface }; panel.Controls.Add(child); ModernUi.Round(panel, 12); return panel; }
    private static Button Button(string text, bool primary, int width) { var button = new ModernButton { Text = text, Width = width, Height = UiTokens.ControlHeight, BackColor = primary ? UiTokens.Primary : UiTokens.Surface, ForeColor = primary ? Color.White : UiTokens.Text, Margin = new Padding(6, 4, 0, 0) }; button.FlatAppearance.BorderColor = primary ? UiTokens.Primary : UiTokens.Border; return button; }
}
