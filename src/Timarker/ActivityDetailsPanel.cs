using Timarker.Models;

namespace Timarker;

internal sealed class ActivityDetailsPanel : UserControl
{
    private readonly DataGridView _sessions = Grid();
    private readonly FlowLayoutPanel _apps = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        Padding = new Padding(12, 6, 12, 10),
        BackColor = AppTheme.Surface
    };
    private readonly Label _total = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, ForeColor = AppTheme.Muted, Padding = new Padding(0, 0, 12, 0) };
    public event Action<AppActivitySession, Control, Point>? SessionContextRequested;

    public ActivityDetailsPanel()
    {
        Dock = DockStyle.Fill;
        BackColor = AppTheme.AppBack;

        _sessions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "活动标题", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 58 });
        _sessions.Columns.Add(new DataGridViewTextBoxColumn { Name = "App", HeaderText = "应用", Width = 112 });
        _sessions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "分类", Width = 86 });
        _sessions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Start", HeaderText = "开始", Width = 74 });
        _sessions.Columns.Add(new DataGridViewTextBoxColumn { Name = "End", HeaderText = "结束", Width = 74 });
        _sessions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Duration", HeaderText = "持续时间", Width = 84 });
        _sessions.CellFormatting += FormatSessionCell;
        _sessions.CellMouseDown += SessionMouseDown;
        _apps.SizeChanged += (_, _) => ResizeAppRows();

        var split = new SplitContainer { Dock = DockStyle.Fill, Width = 1100, SplitterWidth = 7, BackColor = AppTheme.Border, Panel1MinSize = 300, Panel2MinSize = 220, SplitterDistance = 700, IsSplitterFixed = false };
        split.Panel1.BackColor = AppTheme.AppBack;
        split.Panel2.BackColor = AppTheme.AppBack;
        split.Panel1.Controls.Add(Section("活动明细", _sessions));

        var appBody = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, BackColor = AppTheme.Surface };
        appBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        appBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        appBody.Controls.Add(_apps);
        appBody.Controls.Add(_total, 0, 1);
        split.Panel2.Controls.Add(Section("应用用时", appBody));
        Controls.Add(split);
    }

    public void SetData(IReadOnlyList<AppActivitySession> sessions, DateTime start, DateTime end)
    {
        var active = sessions.Where(x => !x.IsIdle && x.ProcessName != "excluded").OrderByDescending(x => x.StartedAt).ToList();
        _sessions.SuspendLayout();
        _sessions.Rows.Clear();
        foreach (var item in active)
        {
            var title = string.IsNullOrWhiteSpace(item.WindowTitle) ? item.AppName : item.WindowTitle;
            var row = _sessions.Rows[_sessions.Rows.Add(title, item.AppName, item.Category, item.StartedAt.ToString("HH:mm:ss"), item.EndedAt.ToString("HH:mm:ss"), ActivityInsights.DurationText(ActivityInsights.Overlap(item, start, end)))];
            row.Tag = item;
        }
        _sessions.ResumeLayout();

        var total = active.Aggregate(TimeSpan.Zero, (sum, item) => sum + ActivityInsights.Overlap(item, start, end));
        _apps.SuspendLayout();
        _apps.Controls.Clear();
        foreach (var group in active.GroupBy(x => x.AppName).OrderByDescending(x => x.Sum(item => ActivityInsights.Overlap(item, start, end).Ticks)))
        {
            var duration = TimeSpan.FromTicks(group.Sum(item => ActivityInsights.Overlap(item, start, end).Ticks));
            _apps.Controls.Add(AppRow(group.Key, duration, total));
        }
        _apps.ResumeLayout();
        ResizeAppRows();
        _total.Text = $"合计  {ActivityInsights.DurationText(total)}";
    }

    private void SessionMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right || e.RowIndex < 0 || _sessions.Rows[e.RowIndex].Tag is not AppActivitySession session) return;
        _sessions.ClearSelection();
        _sessions.Rows[e.RowIndex].Selected = true;
        if (e.ColumnIndex >= 0) _sessions.CurrentCell = _sessions.Rows[e.RowIndex].Cells[e.ColumnIndex];
        SessionContextRequested?.Invoke(session, _sessions, _sessions.PointToClient(Cursor.Position));
    }

    private void ResizeAppRows()
    {
        var width = Math.Max(260, _apps.ClientSize.Width - _apps.Padding.Horizontal - ( _apps.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0));
        foreach (Control control in _apps.Controls) control.Width = width;
    }

    private static void FormatSessionCell(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (sender is not DataGridView grid || e.RowIndex < 0) return;
        var name = grid.Columns[e.ColumnIndex].Name;
        if (name is not ("App" or "Category")) return;
        if (e.CellStyle is not { } style) return;
        var value = e.Value?.ToString() ?? "";
        var color = name == "Category" ? ActivityColor(value) : AppColor(value);
        style.ForeColor = color;
        style.SelectionForeColor = color;
    }

    private static Control Section(string title, Control body)
    {
        var card = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, BackColor = AppTheme.Surface, Padding = new Padding(1) };
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, Font = UiTokens.Font(UiTokens.TextEmphasis, FontStyle.Bold), ForeColor = UiTokens.Text, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(UiTokens.RadiusLarge, 0, 0, 0) });
        card.Controls.Add(body, 0, 1);
        ModernUi.Outline(card, 10, () => AppTheme.Border);
        return card;
    }

    private static Control AppRow(string name, TimeSpan duration, TimeSpan total)
    {
        var percent = total <= TimeSpan.Zero ? 0D : duration.TotalSeconds / total.TotalSeconds;
        var color = AppColor(name);
        var row = new TableLayoutPanel { Width = 330, Height = 42, ColumnCount = 4, Margin = new Padding(0, 1, 0, 1), BackColor = AppTheme.Surface };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 24));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
        row.Controls.Add(new ColorDot(color), 0, 0);
        row.Controls.Add(new Label { Text = name, Dock = DockStyle.Fill, ForeColor = AppTheme.Text, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true }, 1, 0);
        row.Controls.Add(new UsageBar(percent, color) { Dock = DockStyle.Fill, Margin = new Padding(4, 12, 10, 12) }, 2, 0);
        row.Controls.Add(new Label { Text = ActivityInsights.DurationText(duration), Dock = DockStyle.Fill, ForeColor = AppTheme.Muted, TextAlign = ContentAlignment.MiddleRight }, 3, 0);
        return row;
    }

    private static Color ActivityColor(string value) => value switch
    {
        "工作" => UiTokens.Primary, "学习" => UiTokens.Violet,
        "沟通" => UiTokens.Info, "浏览" => UiTokens.Warning,
        "娱乐" => Color.FromArgb(225, 29, 72), "生活" => UiTokens.Success,
        _ => UiTokens.TextMuted
    };

    private static Color AppColor(string value)
    {
        Color[] colors = [Color.FromArgb(14, 165, 233), Color.FromArgb(245, 158, 11), Color.FromArgb(99, 102, 241), Color.FromArgb(239, 68, 68), Color.FromArgb(16, 185, 129), Color.FromArgb(168, 85, 247)];
        return colors[(value.GetHashCode(StringComparison.OrdinalIgnoreCase) & int.MaxValue) % colors.Length];
    }

    private static DataGridView Grid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackgroundColor = AppTheme.Surface,
            GridColor = AppTheme.Border,
            ForeColor = AppTheme.Text,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false,
            RowTemplate = { Height = 34 },
            EnableHeadersVisualStyles = false
        };
        grid.ColumnHeadersHeight = 36;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = AppTheme.SurfaceAlt, ForeColor = AppTheme.Muted, SelectionBackColor = AppTheme.SurfaceAlt, SelectionForeColor = AppTheme.Muted, Padding = new Padding(8, 0, 0, 0) };
        grid.DefaultCellStyle = new DataGridViewCellStyle { BackColor = AppTheme.Surface, ForeColor = AppTheme.Text, SelectionBackColor = AppTheme.Selected, SelectionForeColor = AppTheme.Text, Padding = new Padding(8, 0, 0, 0) };
        grid.AlternatingRowsDefaultCellStyle.BackColor = AppTheme.SurfaceAlt;
        return grid;
    }

    private sealed class UsageBar(double value, Color color) : Control
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var track = ModernUi.RoundedPath(ClientRectangle, Height / 2);
            using var trackBrush = new SolidBrush(AppTheme.SurfaceAlt);
            e.Graphics.FillPath(trackBrush, track);
            var width = Math.Max(4, (int)(Width * Math.Clamp(value, 0D, 1D)));
            using var fill = ModernUi.RoundedPath(new Rectangle(0, 0, width, Height), Height / 2);
            using var fillBrush = new SolidBrush(color);
            e.Graphics.FillPath(fillBrush, fill);
        }
    }

    private sealed class ColorDot(Color color) : Control
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(color);
            e.Graphics.FillEllipse(brush, 7, Height / 2 - 4, 8, 8);
        }
    }
}
