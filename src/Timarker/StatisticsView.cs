using Timarker.Models;
using Timarker.Services;

namespace Timarker;

internal sealed class StatisticsView : UserControl
{
    private readonly ActivityStore _store;
    private readonly ActivityCollector _collector;
    private readonly IReadOnlyList<Project> _projects;
    private readonly Label _period = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = UiTokens.Font(UiTokens.TextSection, FontStyle.Bold), ForeColor = UiTokens.Text };
    private readonly Label _total = Metric();
    private readonly Label _idle = Metric();
    private readonly Label _top = Metric();
    private readonly FlowLayoutPanel _apps = RankingList();
    private readonly FlowLayoutPanel _categories = RankingList();
    private readonly FlowLayoutPanel _projectStats = RankingList();
    private readonly Dictionary<RangeKind, ModernButton> _rangeButtons = [];
    private RangeKind _range = RangeKind.Day;
    private DateTime _anchor = DateTime.Today;

    public StatisticsView(ActivityStore store, ActivityCollector collector, IReadOnlyList<Project> projects)
    {
        _store = store;
        _collector = collector;
        _projects = projects;
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
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, Padding = new Padding(26, 20, 26, 24), BackColor = AppTheme.AppBack };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var words = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        words.Controls.Add(new Label { Text = "时间统计", Dock = DockStyle.Fill, Font = UiTokens.Font(UiTokens.TextPageTitle, FontStyle.Bold), ForeColor = UiTokens.Text });
        words.Controls.Add(new Label { Text = "统一查看应用、分类和项目分别用了多久。", Dock = DockStyle.Fill, ForeColor = AppTheme.Muted }, 0, 1);
        root.Controls.Add(words);
        root.Controls.Add(RangeBar(), 0, 1);
        root.Controls.Add(Metrics(), 0, 2);
        var columns = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Margin = new Padding(0, 10, 0, 0) };
        for (var i = 0; i < 3; i++) columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        columns.Controls.Add(Card("应用用时", _apps), 0, 0);
        columns.Controls.Add(Card("分类用时", _categories), 1, 0);
        columns.Controls.Add(Card("项目用时", _projectStats), 2, 0);
        root.Controls.Add(columns, 0, 3);
        Controls.Add(root);
    }

    private Control RangeBar()
    {
        var bar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6 };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        var ranges = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        AddRange(ranges, RangeKind.Day, "日"); AddRange(ranges, RangeKind.Week, "周"); AddRange(ranges, RangeKind.Month, "月");
        var previous = Button("‹", 36); previous.Click += (_, _) => MovePeriod(-1);
        var next = Button("›", 36); next.Click += (_, _) => MovePeriod(1);
        var today = Button("回到今天", 70); today.Click += (_, _) => { _anchor = DateTime.Today; RefreshView(); };
        bar.Controls.Add(ranges, 0, 0); bar.Controls.Add(previous, 1, 0); bar.Controls.Add(_period, 2, 0); bar.Controls.Add(next, 3, 0); bar.Controls.Add(today, 5, 0);
        return bar;
    }

    private void AddRange(Control parent, RangeKind kind, string text)
    {
        var button = Button(text, 64);
        button.Click += (_, _) => { _range = kind; _anchor = DateTime.Today; RefreshView(); };
        _rangeButtons[kind] = button;
        ((Control.ControlCollection)parent.Controls).Add(button);
    }

    private Control Metrics()
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        for (var i = 0; i < 3; i++) row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        row.Controls.Add(MetricCard("有效使用", _total), 0, 0); row.Controls.Add(MetricCard("空闲时间", _idle), 1, 0); row.Controls.Add(MetricCard("最常使用", _top), 2, 0);
        return row;
    }

    public void RefreshView()
    {
        var (start, end) = Range();
        _period.Text = PeriodText(start, end);
        foreach (var pair in _rangeButtons) { pair.Value.BackColor = pair.Key == _range ? UiTokens.Primary : UiTokens.Surface; pair.Value.ForeColor = pair.Key == _range ? Color.White : UiTokens.Text; }
        var sessions = _store.Sessions.Where(x => x.EndedAt > start && x.StartedAt < end).ToList();
        var active = sessions.Where(x => !x.IsIdle).ToList();
        _total.Text = ActivityInsights.DurationText(Sum(active, start, end));
        _idle.Text = ActivityInsights.DurationText(Sum(sessions.Where(x => x.IsIdle), start, end));
        var appGroups = active.GroupBy(x => x.AppName).Select(x => (x.Key, Value: Sum(x, start, end))).OrderByDescending(x => x.Value).ToList();
        _top.Text = appGroups.FirstOrDefault().Key ?? "暂无";
        FillRanking(_apps, appGroups);
        FillRanking(_categories, ActivityInsights.ByCategory(active, start, end).Select(x => (x.Key, x.Value)).OrderByDescending(x => x.Value));
        FillRanking(_projectStats, active.Where(x => x.ProjectId is not null).GroupBy(x => x.ProjectId).Select(x => (_projects.FirstOrDefault(p => p.Id == x.Key)?.Name ?? "未知项目", Sum(x, start, end))).OrderByDescending(x => x.Item2));
    }

    private void FillRanking(FlowLayoutPanel panel, IEnumerable<(string Name, TimeSpan Value)> source)
    {
        panel.SuspendLayout(); panel.Controls.Clear();
        var items = source.Where(x => x.Value > TimeSpan.Zero).Take(30).ToList();
        var max = items.Count == 0 ? 1D : items.Max(x => x.Value.TotalSeconds);
        foreach (var item in items)
        {
            var row = new TableLayoutPanel { Width = Math.Max(220, panel.ClientSize.Width - 28), Height = 54, RowCount = 2, ColumnCount = 2, Margin = new Padding(4, 3, 4, 3) };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            row.Controls.Add(new Label { Text = item.Name, Dock = DockStyle.Fill, ForeColor = AppTheme.Text, AutoEllipsis = true });
            row.Controls.Add(new Label { Text = ActivityInsights.DurationText(item.Value), Dock = DockStyle.Fill, ForeColor = AppTheme.Muted, TextAlign = ContentAlignment.TopRight }, 1, 0);
            var track = new Panel { Dock = DockStyle.Fill, Height = 7, BackColor = AppTheme.SurfaceAlt, Margin = new Padding(0, 6, 0, 6) };
            var fill = new Panel { Dock = DockStyle.Left, Width = Math.Max(4, (int)((row.Width - 12) * item.Value.TotalSeconds / max)), BackColor = Color.FromArgb(59, 130, 246) };
            track.Controls.Add(fill); row.Controls.Add(track, 0, 1); row.SetColumnSpan(track, 2); panel.Controls.Add(row);
        }
        if (items.Count == 0) panel.Controls.Add(new Label { Text = "这个时间范围内暂无数据", AutoSize = true, ForeColor = AppTheme.Muted, Margin = new Padding(10, 14, 0, 0) });
        panel.ResumeLayout();
    }

    private (DateTime Start, DateTime End) Range() => _range switch
    {
        RangeKind.Week => (_anchor.Date.AddDays(-(((int)_anchor.DayOfWeek + 6) % 7)), _anchor.Date.AddDays(-(((int)_anchor.DayOfWeek + 6) % 7)).AddDays(7)),
        RangeKind.Month => (new DateTime(_anchor.Year, _anchor.Month, 1), new DateTime(_anchor.Year, _anchor.Month, 1).AddMonths(1)),
        _ => (_anchor.Date, _anchor.Date.AddDays(1))
    };
    private void MovePeriod(int direction) { _anchor = _range switch { RangeKind.Week => _anchor.AddDays(7 * direction), RangeKind.Month => _anchor.AddMonths(direction), _ => _anchor.AddDays(direction) }; RefreshView(); }
    private string PeriodText(DateTime start, DateTime end) => _range switch { RangeKind.Day => start.Date == DateTime.Today ? "今天" : start.ToString("yyyy年M月d日"), RangeKind.Month => start.ToString("yyyy年M月"), _ => $"{start:M月d日} — {end.AddDays(-1):M月d日}" };
    private static TimeSpan Sum(IEnumerable<AppActivitySession> source, DateTime start, DateTime end) => TimeSpan.FromTicks(source.Sum(x => ActivityInsights.Overlap(x, start, end).Ticks));
    private void CollectorChanged(object? sender, EventArgs e) { if (Visible && IsHandleCreated) BeginInvoke((MethodInvoker)RefreshView); }
    private static FlowLayoutPanel RankingList() => new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(8) };
    private static Label Metric() => new() { Dock = DockStyle.Fill, Font = UiTokens.Font(UiTokens.TextSection, FontStyle.Bold), ForeColor = UiTokens.Text };
    private static Control MetricCard(string title, Label value) { var card = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Margin = new Padding(0, 4, 10, 4), Padding = new Padding(16, 10, 12, 8), BackColor = AppTheme.Surface }; card.Controls.Add(value); card.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, ForeColor = AppTheme.Muted }, 0, 1); ModernUi.Round(card, 12); return card; }
    private static Control Card(string title, Control content) { var card = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Margin = new Padding(0, 0, UiTokens.Space3, 0), Padding = new Padding(UiTokens.Space3), BackColor = UiTokens.Surface }; card.RowStyles.Add(new RowStyle(SizeType.Absolute, UiTokens.LargeControlHeight)); card.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); card.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, Font = UiTokens.Font(UiTokens.TextSection, FontStyle.Bold), ForeColor = UiTokens.Text }); card.Controls.Add(content, 0, 1); ModernUi.Round(card, UiTokens.RadiusLarge); return card; }
    private static ModernButton Button(string text, int width) { var button = new ModernButton { Text = text, Width = width, Height = UiTokens.ControlHeight, BackColor = UiTokens.Surface, ForeColor = UiTokens.Text, Margin = new Padding(UiTokens.Space1) }; button.FlatAppearance.BorderColor = UiTokens.Border; return button; }
    private enum RangeKind { Day, Week, Month }
}
