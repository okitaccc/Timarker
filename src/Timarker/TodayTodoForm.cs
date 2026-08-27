using Timarker.Models;

namespace Timarker;

public sealed class TodayTodoForm : Form
{
    private readonly Func<IReadOnlyList<EventItem>> _getItems;
    private readonly Action<EventItem> _complete;
    private readonly Action<EventItem> _edit;
    private readonly FlowLayoutPanel _list = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(0, 0, 4, 0) };
    private readonly Label _count = new() { AutoSize = false, ForeColor = UiTokens.TextMuted, TextAlign = ContentAlignment.MiddleCenter };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 30_000 };
    private string _signature = "";
    private Point _dragOrigin;
    private int _resizeStartY;
    private int _resizeStartHeight;
    private bool _resizing;
    private bool _userSized;
    private bool _placed;

    public TodayTodoForm(Func<IReadOnlyList<EventItem>> getItems, Action<EventItem> complete, Action<EventItem> edit)
    {
        _getItems = getItems;
        _complete = complete;
        _edit = edit;
        Text = "今日清单";
        Width = UiTokens.Scale(340);
        MinimumSize = new Size(Width, UiTokens.Scale(142));
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = UiTokens.Border;
        Padding = new Padding(1);
        Font = UiTokens.Font();
        BuildUi();
        ModernUi.Round(this, UiTokens.RadiusLarge);
        _timer.Tick += (_, _) => RefreshItems();
        _timer.Start();
        RefreshItems(true);
    }

    public static IReadOnlyList<EventItem> SelectItems(IEnumerable<EventItem> events, DateTime now) => events
        .Where(x => x.Type is not EventType.Anniversary)
        .Where(x => x.Status is not (EventStatus.Done or EventStatus.Skipped or EventStatus.Postponed or EventStatus.Cancelled))
        .Where(x => !x.IsRecurringSeries || !x.IsRecurrencePaused)
        .Where(x => x.NextDueAt(now)?.Date == now.Date)
        .Where(x => !x.Occurrences.Any(o => o.ScheduledAt.Date == now.Date && o.Status is EventStatus.Done))
        .OrderByDescending(x => x.Priority)
        .ThenBy(x => x.CreatedAt)
        .ToList();

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Dispose();
        base.OnFormClosed(e);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, BackColor = UiTokens.Surface, Padding = new Padding(UiTokens.Space4, UiTokens.Space2, UiTokens.Space4, UiTokens.Space1) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, UiTokens.Scale(46)));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, UiTokens.Scale(22)));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, UiTokens.Scale(8)));

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Margin = Padding.Empty };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, UiTokens.Scale(62)));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, UiTokens.Scale(26)));
        var heading = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Margin = Padding.Empty };
        heading.RowStyles.Add(new RowStyle(SizeType.Absolute, UiTokens.Scale(23)));
        var title = new Label { Text = "今日清单", Dock = DockStyle.Fill, Font = UiTokens.Font(UiTokens.TextSection, FontStyle.Bold), ForeColor = UiTokens.Text, TextAlign = ContentAlignment.MiddleLeft };
        heading.Controls.Add(title);
        heading.Controls.Add(new Label { Text = DateTime.Today.ToString("M月d日  dddd"), Dock = DockStyle.Fill, Font = UiTokens.Font(UiTokens.TextSmall), ForeColor = UiTokens.TextMuted, TextAlign = ContentAlignment.TopLeft }, 0, 1);
        _count.BackColor = UiTokens.PrimarySoft;
        _count.ForeColor = UiTokens.Primary;
        _count.Font = UiTokens.Font(UiTokens.TextSmall, FontStyle.Bold);
        _count.Size = new Size(UiTokens.Scale(54), UiTokens.Scale(22));
        _count.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _count.Margin = new Padding(0, 0, UiTokens.Space1, 0);
        ModernUi.Pill(_count);
        var close = new ModernButton { Text = "×", Size = new Size(UiTokens.Scale(22), UiTokens.Scale(22)), Anchor = AnchorStyles.Top | AnchorStyles.Right, BackColor = UiTokens.SurfaceSubtle, ForeColor = UiTokens.TextMuted, Font = new Font("Segoe UI", 9F), Margin = Padding.Empty, Padding = Padding.Empty };
        close.FlatAppearance.BorderColor = UiTokens.Surface;
        close.Click += (_, _) => Close();
        header.Controls.Add(heading, 0, 0);
        header.Controls.Add(_count, 1, 0);
        header.Controls.Add(close, 2, 0);
        HookWindowDrag(header);
        HookWindowDrag(heading);
        HookWindowDrag(title);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(_list, 0, 1);
        root.Controls.Add(new Label { Text = "向右划完成   向左划编辑", Dock = DockStyle.Fill, ForeColor = UiTokens.TextMuted, Font = UiTokens.Font(UiTokens.TextSmall), TextAlign = ContentAlignment.BottomCenter }, 0, 2);
        var grip = new Panel { Dock = DockStyle.Fill, Cursor = Cursors.SizeNS, Margin = Padding.Empty };
        grip.Paint += (_, e) =>
        {
            using var pen = new Pen(UiTokens.Border, 2F) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
            var center = grip.ClientSize.Width / 2;
            e.Graphics.DrawLine(pen, center - 18, 3, center + 18, 3);
        };
        grip.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            _resizing = true;
            _resizeStartY = Cursor.Position.Y;
            _resizeStartHeight = Height;
            grip.Capture = true;
        };
        grip.MouseMove += (_, _) =>
        {
            if (!_resizing) return;
            var area = Screen.FromControl(this).WorkingArea;
            Height = Math.Clamp(_resizeStartHeight + Cursor.Position.Y - _resizeStartY, MinimumSize.Height, area.Bottom - Top);
            _userSized = true;
        };
        grip.MouseUp += (_, _) => { _resizing = false; grip.Capture = false; };
        grip.MouseCaptureChanged += (_, _) => { if (!grip.Capture) _resizing = false; };
        root.Controls.Add(grip, 0, 3);
        Controls.Add(root);
        Paint += (_, e) => ModernUi.DrawBorder(e.Graphics, ClientRectangle, UiTokens.RadiusLarge, UiTokens.Border);
    }

    public void RefreshItems(bool force = false)
    {
        var items = _getItems();
        var signature = string.Join('|', items.Select(x => $"{x.Id}:{x.UpdatedAt.Ticks}:{x.Status}"));
        if (!force && signature == _signature) return;
        _signature = signature;
        _list.SuspendLayout();
        foreach (var control in _list.Controls.Cast<Control>().ToArray()) control.Dispose();
        _list.Controls.Clear();
        foreach (var item in items)
        {
            var card = new SwipeTodoCard(item, Complete, Edit) { Width = ClientSize.Width - UiTokens.Scale(38) };
            _list.Controls.Add(card);
        }
        if (items.Count == 0)
            _list.Controls.Add(new Label { Text = "✓  今日事项已清空", Width = ClientSize.Width - 50, Height = UiTokens.Scale(52), Font = UiTokens.Font(UiTokens.TextEmphasis, FontStyle.Bold), ForeColor = UiTokens.Success, TextAlign = ContentAlignment.MiddleCenter });
        _list.ResumeLayout();
        _count.Visible = items.Count > 0;
        _count.Text = $"{items.Count} 项";
        if (!_userSized)
        {
            var desired = UiTokens.Scale(90 + Math.Max(1, Math.Min(items.Count, 7)) * 72);
            Height = Math.Min(desired, Screen.FromControl(this).WorkingArea.Height - UiTokens.Scale(48));
        }
        if (!_placed)
        {
            PlaceAtRight();
            _placed = true;
        }
    }

    private void Complete(EventItem item)
    {
        _complete(item);
        RefreshItems();
    }

    private void Edit(EventItem item)
    {
        var topMost = TopMost;
        TopMost = false;
        try { _edit(item); }
        finally { TopMost = topMost; Activate(); }
        RefreshItems();
    }

    private void PlaceAtRight()
    {
        var area = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(area.Right - Width - UiTokens.Space4, area.Top + UiTokens.Space4);
    }

    private void HookWindowDrag(Control control)
    {
        control.Cursor = Cursors.SizeAll;
        control.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) _dragOrigin = e.Location; };
        control.MouseMove += (_, e) => { if (e.Button == MouseButtons.Left) Location = new Point(Left + e.X - _dragOrigin.X, Top + e.Y - _dragOrigin.Y); };
    }
}

internal sealed class SwipeTodoCard : UserControl
{
    private const int Threshold = 68;
    private readonly EventItem _item;
    private readonly Action<EventItem> _complete;
    private readonly Action<EventItem> _edit;
    private readonly Panel _content;
    private Point _start;
    private bool _dragging;

    public SwipeTodoCard(EventItem item, Action<EventItem> complete, Action<EventItem> edit)
    {
        _item = item;
        _complete = complete;
        _edit = edit;
        Height = UiTokens.Scale(64);
        Margin = new Padding(0, 0, 0, UiTokens.Space2);
        BackColor = UiTokens.Surface;
        DoubleBuffered = true;

        Controls.Add(new Label { Text = "完成", Dock = DockStyle.Left, Width = 72, BackColor = UiTokens.Success, ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter, Font = UiTokens.Font(UiTokens.TextSmall, FontStyle.Bold) });
        Controls.Add(new Label { Text = "编辑", Dock = DockStyle.Right, Width = 72, BackColor = UiTokens.Primary, ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter, Font = UiTokens.Font(UiTokens.TextSmall, FontStyle.Bold) });
        _content = new Panel { Bounds = ClientRectangle, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom, BackColor = UiTokens.SurfaceSubtle, Cursor = Cursors.Hand };
        var marker = new Panel { Dock = DockStyle.Left, Width = UiTokens.Scale(4), BackColor = UiTokens.EventTypeColor(item.Type) };
        var title = new Label { Text = item.Title.DefaultIfBlank("未命名事项"), Dock = DockStyle.Fill, Padding = new Padding(UiTokens.Space4, UiTokens.Space2, UiTokens.Space3, 0), Font = UiTokens.Font(UiTokens.TextEmphasis, FontStyle.Bold), ForeColor = UiTokens.Text, AutoEllipsis = true };
        var meta = new Label { Text = Meta(item), Dock = DockStyle.Bottom, Height = UiTokens.Scale(25), Padding = new Padding(UiTokens.Space4, 0, UiTokens.Space3, UiTokens.Space1), Font = UiTokens.Font(UiTokens.TextSmall), ForeColor = UiTokens.TextMuted, AutoEllipsis = true };
        _content.Controls.Add(title);
        _content.Controls.Add(meta);
        _content.Controls.Add(marker);
        Controls.Add(_content);
        _content.BringToFront();
        foreach (var control in new Control[] { _content, title, meta }) HookSwipe(control);
        Resize += (_, _) => { if (!_dragging) _content.Bounds = ClientRectangle; ModernUi.Round(this, UiTokens.RadiusMedium); ModernUi.Round(_content, UiTokens.RadiusMedium); };
        ModernUi.Round(this, UiTokens.RadiusMedium);
        ModernUi.Round(_content, UiTokens.RadiusMedium);
        Paint += (_, e) => ModernUi.DrawBorder(e.Graphics, ClientRectangle, UiTokens.RadiusMedium, UiTokens.Border);
    }

    private static string Meta(EventItem item)
    {
        var labels = new List<string> { item.TypeText };
        if (item.Priority is EventPriority.High) labels.Add("高优先级");
        var tag = item.Tags.Split([',', '，', ';', '；'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        if (tag is not null) labels.Add("#" + tag);
        return string.Join("  ·  ", labels);
    }

    private void HookSwipe(Control control)
    {
        control.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) { _dragging = true; _start = PointToClient(control.PointToScreen(e.Location)); control.Capture = true; } };
        control.MouseMove += (_, e) =>
        {
            if (!_dragging) return;
            var current = PointToClient(control.PointToScreen(e.Location));
            _content.Left = Math.Clamp(current.X - _start.X, -92, 92);
        };
        control.MouseUp += (_, _) => FinishSwipe(control);
        control.MouseCaptureChanged += (_, _) => { if (_dragging && !control.Capture) FinishSwipe(control); };
    }

    private void FinishSwipe(Control source)
    {
        if (!_dragging) return;
        _dragging = false;
        source.Capture = false;
        var offset = _content.Left;
        _content.Left = 0;
        if (offset >= Threshold)
        {
            Visible = false;
            FindForm()?.BeginInvoke((MethodInvoker)(() => _complete(_item)));
        }
        else if (offset <= -Threshold) _edit(_item);
    }
}
