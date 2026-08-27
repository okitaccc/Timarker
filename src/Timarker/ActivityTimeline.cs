using System.Drawing.Drawing2D;
using Timarker.Models;

namespace Timarker;

internal sealed class ActivityTimeline : Control
{
    internal const int PixelsPerMinute = 3;
    internal const int HeaderHeight = 42;
    internal const int LaneHeight = 54;
    private IReadOnlyList<AppActivitySession> _sessions = [];
    private DateTime _day = DateTime.Today;
    private readonly ToolTip _tip = new() { OwnerDraw = true, InitialDelay = 250, ReshowDelay = 80, AutoPopDelay = 10_000, ShowAlways = true };
    private AppActivitySession? _hoveredSession;

    public ActivityTimeline()
    {
        Width = 24 * 60 * PixelsPerMinute;
        Height = HeaderHeight + LaneHeight * 3;
        DoubleBuffered = true;
        BackColor = AppTheme.Surface;
        Cursor = Cursors.Hand;
        _tip.Popup += (_, e) => e.ToolTipSize = new Size(340, string.IsNullOrWhiteSpace(_hoveredSession?.WindowTitle) ? 94 : 116);
        _tip.Draw += DrawTip;
    }

    public AppActivitySession? SelectedSession { get; private set; }
    public event Action<AppActivitySession>? SessionSelected;
    public event Action<AppActivitySession, Point>? SessionContextRequested;

    public void SetData(DateTime day, IReadOnlyList<AppActivitySession> sessions)
    {
        _day = day.Date;
        _sessions = sessions;
        if (SelectedSession is not null && !_sessions.Contains(SelectedSession)) SelectedSession = null;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        var session = HitTest(e.Location);
        if (session is null) return;
        SelectedSession = session;
        Invalidate();
        SessionSelected?.Invoke(session);
        if (e.Button == MouseButtons.Right) SessionContextRequested?.Invoke(session, e.Location);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var session = HitTest(e.Location);
        Cursor = session is null ? Cursors.Default : Cursors.Hand;
        if (ReferenceEquals(session, _hoveredSession)) return;
        _tip.Hide(this);
        _hoveredSession = session;
        Invalidate();
        if (session is not null) _tip.Show(TipText(session), this, e.X + 14, e.Y + 18, 10_000);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _tip.Hide(this);
        _hoveredSession = null;
        Cursor = Cursors.Default;
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tip.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        DrawScale(e.Graphics);
        DrawLane(e.Graphics, 0, x => x.IsIdle ? "空闲" : x.Category, x => CategoryColor(x.IsIdle ? "空闲" : x.Category), mergeAdjacent: true);
        DrawLane(e.Graphics, 1, x => x.AppName, x => AppColor(x.ProcessName), mergeAdjacent: false);
        DrawLane(e.Graphics, 2, DocumentTitle, x => AppColor(x.WindowTitle), mergeAdjacent: false);
        if (_day == DateTime.Today)
        {
            var x = (int)DateTime.Now.TimeOfDay.TotalMinutes * PixelsPerMinute;
            using var pen = new Pen(Color.FromArgb(239, 68, 68), 2);
            e.Graphics.DrawLine(pen, x, 0, x, Height);
        }
    }

    private void DrawScale(Graphics graphics)
    {
        using var gridPen = new Pen(AppTheme.Border);
        using var hourFont = new Font(Font.FontFamily, 8F, FontStyle.Bold);
        using var minuteFont = new Font(Font.FontFamily, 7F);
        for (var minute = 0; minute <= 24 * 60; minute += 5)
        {
            var x = minute * PixelsPerMinute;
            var major = minute % 60 == 0;
            var medium = minute % 15 == 0;
            graphics.DrawLine(gridPen, x, HeaderHeight - (major ? 15 : medium ? 9 : 5), x, Height);
            if (major && minute < 24 * 60)
                TextRenderer.DrawText(graphics, $"{minute / 60:00}:00", hourFont, new Point(x + 4, 7), AppTheme.Text);
            else if (medium && minute < 24 * 60)
                TextRenderer.DrawText(graphics, $"{minute % 60:00}", minuteFont, new Point(x + 3, 13), AppTheme.Muted);
        }
        graphics.DrawLine(gridPen, 0, HeaderHeight - 1, Width, HeaderHeight - 1);
    }

    private void DrawLane(Graphics graphics, int lane, Func<AppActivitySession, string> text, Func<AppActivitySession, Color> color, bool mergeAdjacent)
    {
        var y = HeaderHeight + lane * LaneHeight;
        using var border = new Pen(AppTheme.Border);
        graphics.DrawLine(border, 0, y + LaneHeight - 1, Width, y + LaneHeight - 1);
        foreach (var segment in Segments(text, mergeAdjacent))
        {
            var session = segment.Sessions[0];
            var start = segment.Start < _day ? _day : segment.Start;
            var end = segment.End > _day.AddDays(1) ? _day.AddDays(1) : segment.End;
            if (end <= start) continue;
            var x = (int)start.TimeOfDay.TotalMinutes * PixelsPerMinute;
            var width = Math.Max(3, (int)(end - start).TotalMinutes * PixelsPerMinute);
            var bounds = new Rectangle(x, y + 5, width, LaneHeight - 10);
            using var brush = new SolidBrush(color(session));
            graphics.FillRectangle(brush, bounds);
            var selected = SelectedSession is not null && segment.Sessions.Contains(SelectedSession);
            var hovered = _hoveredSession is not null && segment.Sessions.Contains(_hoveredSession);
            if (selected || hovered)
            {
                using var outline = new Pen(Color.White, hovered ? 3 : 2);
                graphics.DrawRectangle(outline, bounds.X + 1, bounds.Y + 1, Math.Max(1, bounds.Width - 3), bounds.Height - 3);
            }
            if (width > 58)
                TextRenderer.DrawText(graphics, segment.Key, Font, Rectangle.Inflate(bounds, -7, -5), Color.White, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
        }
    }

    private IReadOnlyList<TimelineSegment> Segments(Func<AppActivitySession, string> key, bool merge)
    {
        if (!merge) return _sessions.Select(x => new TimelineSegment(key(x), x.StartedAt, x.EndedAt, [x])).ToList();
        var result = new List<TimelineSegment>();
        foreach (var session in _sessions.OrderBy(x => x.StartedAt))
        {
            var value = key(session);
            var previous = result.LastOrDefault();
            if (previous is not null && previous.Key == value && session.StartedAt - previous.End <= TimeSpan.FromSeconds(6))
            {
                previous.End = session.EndedAt > previous.End ? session.EndedAt : previous.End;
                previous.Sessions.Add(session);
            }
            else result.Add(new TimelineSegment(value, session.StartedAt, session.EndedAt, [session]));
        }
        return result;
    }

    private AppActivitySession? HitTest(Point point)
    {
        if (point.Y < HeaderHeight || point.Y >= Height) return null;
        var time = _day.AddMinutes((double)point.X / PixelsPerMinute);
        return _sessions.LastOrDefault(x => x.StartedAt <= time && x.EndedAt >= time);
    }

    private static string TipText(AppActivitySession session)
    {
        var duration = ActivityInsights.DurationText(session.Duration);
        var text = $"{session.StartedAt:HH:mm} – {session.EndedAt:HH:mm}  ·  {duration}\n{session.AppName}\n标签：{(session.IsIdle ? "空闲" : session.Category)}";
        return string.IsNullOrWhiteSpace(session.WindowTitle) ? text : $"{text}\n{session.WindowTitle}";
    }

    private static string DocumentTitle(AppActivitySession session)
    {
        if (session.IsIdle) return "空闲";
        return string.IsNullOrWhiteSpace(session.WindowTitle) ? session.AppName : session.WindowTitle;
    }

    private void DrawTip(object? sender, DrawToolTipEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = ModernUi.RoundedPath(new Rectangle(1, 1, e.Bounds.Width - 3, e.Bounds.Height - 3), 10);
        using var background = new SolidBrush(AppTheme.Surface);
        using var border = new Pen(AppTheme.Border);
        e.Graphics.FillPath(background, path);
        e.Graphics.DrawPath(border, path);
        var lines = (e.ToolTipText ?? "").Split('\n');
        using var timeFont = new Font(Font.FontFamily, 8.5F);
        using var appFont = new Font(Font.FontFamily, 10F, FontStyle.Bold);
        TextRenderer.DrawText(e.Graphics, lines[0], timeFont, new Rectangle(14, 10, e.Bounds.Width - 28, 20), AppTheme.Muted, TextFormatFlags.EndEllipsis);
        if (lines.Length > 1) TextRenderer.DrawText(e.Graphics, lines[1], appFont, new Rectangle(14, 33, e.Bounds.Width - 28, 24), AppTheme.Text, TextFormatFlags.EndEllipsis);
        if (lines.Length > 2) TextRenderer.DrawText(e.Graphics, lines[2], Font, new Rectangle(14, 61, e.Bounds.Width - 28, 20), AppTheme.Muted, TextFormatFlags.EndEllipsis);
        if (lines.Length > 3) TextRenderer.DrawText(e.Graphics, lines[3], Font, new Rectangle(14, 85, e.Bounds.Width - 28, 20), AppTheme.Muted, TextFormatFlags.EndEllipsis);
    }

    private static Color CategoryColor(string value) => value switch
    {
        "工作" => UiTokens.Primary, "学习" => UiTokens.Violet,
        "沟通" => UiTokens.Info, "浏览" => UiTokens.Warning,
        "娱乐" => Color.FromArgb(225, 29, 72), "生活" => UiTokens.Success,
        "空闲" => Color.FromArgb(148, 163, 184), _ => Color.FromArgb(100, 116, 139)
    };

    private static Color AppColor(string value)
    {
        var colors = new[] { Color.FromArgb(14, 165, 233), Color.FromArgb(245, 158, 11), Color.FromArgb(99, 102, 241), Color.FromArgb(239, 68, 68), Color.FromArgb(16, 185, 129), Color.FromArgb(168, 85, 247) };
        return colors[(value.GetHashCode(StringComparison.OrdinalIgnoreCase) & int.MaxValue) % colors.Length];
    }

    private sealed class TimelineSegment(string key, DateTime start, DateTime end, List<AppActivitySession> sessions)
    {
        public string Key { get; } = key;
        public DateTime Start { get; } = start;
        public DateTime End { get; set; } = end;
        public List<AppActivitySession> Sessions { get; } = sessions;
    }
}
