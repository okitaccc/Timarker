using System.Drawing.Drawing2D;
using Timarker.Models;

namespace Timarker;

public static class EventCardRenderer
{
    public const int ItemHeight = 104;

    public static void Draw(Graphics graphics, Rectangle itemBounds, EventItem item, Font font, bool selected, Color canvas, bool showActions = false)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var canvasBrush = new SolidBrush(canvas);
        graphics.FillRectangle(canvasBrush, itemBounds);

        var card = Rectangle.Inflate(itemBounds, -4, -7);
        using var cardPath = RoundRect(card, UiTokens.RadiusMedium);
        using var back = new SolidBrush(selected ? UiTokens.Selected : UiTokens.SurfaceSubtle);
        var typeColor = UiTokens.EventTypeColor(item.Type);
        using var border = new Pen(selected ? typeColor : UiTokens.Border);
        graphics.FillPath(back, cardPath);
        graphics.DrawPath(border, cardPath);

        using var dot = new SolidBrush(typeColor);
        graphics.FillEllipse(dot, card.Left + 12, card.Top + 15, 8, 8);

        var contentLeft = card.Left + 30;
        var contentWidth = Math.Max(20, card.Right - contentLeft - (showActions ? 88 : 14));
        var due = item.NextDueAt(DateTime.Now)?.ToString("MM-dd HH:mm") ?? L.T("未设置时间");
        using var titleFont = UiTokens.DrawingFont(UiTokens.TextEmphasis, FontStyle.Bold);
        using var metaFont = UiTokens.DrawingFont(UiTokens.TextSmall);
        var titleColor = item.Status is EventStatus.Overdue ? UiTokens.Danger : UiTokens.Text;
        TextRenderer.DrawText(graphics, item.Title, titleFont, new Rectangle(contentLeft, card.Top + 9, contentWidth, 24), titleColor, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        var meta = item.Type switch
        {
            EventType.Anniversary => L.IsEnglish ? $"{item.AnniversarySummary(DateTime.Now)}  ·  Next {due}" : $"{item.AnniversarySummary(DateTime.Now)}  ·  下次 {due}",
            EventType.Birthday when item.BirthdaySummary(DateTime.Now).Length > 0 => L.IsEnglish ? $"{item.BirthdaySummary(DateTime.Now)}  ·  Next {due}" : $"{item.BirthdaySummary(DateTime.Now)}  ·  下次 {due}",
            EventType.Recurring or EventType.Habit when item.IsRecurrencePaused => L.IsEnglish ? "Recurrence paused" : "周期已暂停",
            EventType.Recurring or EventType.Habit => L.IsEnglish ? $"Completed {item.Occurrences.Count(x => x.Status is EventStatus.Done)} times  ·  Next {due}" : $"已完成 {item.Occurrences.Count(x => x.Status is EventStatus.Done)} 次  ·  下次 {due}",
            _ => $"{due}  ·  {L.T(item.TypeText)}  ·  {L.T(item.StatusText)}"
        };
        if (L.IsEnglish)
        {
            meta = item.Type switch
            {
                EventType.Anniversary => $"{item.AnniversarySummary(DateTime.Now)}  ·  Next {due}",
                EventType.Birthday when item.BirthdaySummary(DateTime.Now).Length > 0 => $"{item.BirthdaySummary(DateTime.Now)}  ·  Next {due}",
                EventType.Recurring or EventType.Habit when item.IsRecurrencePaused => "Recurrence paused",
                EventType.Recurring or EventType.Habit => $"Completed {item.Occurrences.Count(x => x.Status is EventStatus.Done)} times  ·  Next {due}",
                _ => $"{due}  ·  {L.T(item.TypeText)}  ·  {L.T(item.StatusText)}"
            };
        }
        TextRenderer.DrawText(graphics, meta, metaFont, new Rectangle(contentLeft, card.Top + 35, contentWidth, 20), AppTheme.Muted, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        var chipX = contentLeft;
        foreach (var tag in SplitTags($"{item.Categories}, {item.Tags}").Distinct(StringComparer.OrdinalIgnoreCase).Take(3))
        {
            var text = $"#{tag}";
            var width = Math.Min(TextRenderer.MeasureText(text, metaFont).Width + 14, card.Right - chipX - 8);
            if (width < 28)
            {
                break;
            }
            var chipRect = new Rectangle(chipX, card.Top + 63, width, 22);
            using var chipPath = RoundRect(chipRect, 10);
            using var chipBack = new SolidBrush(AppTheme.Hover);
            graphics.FillPath(chipBack, chipPath);
            TextRenderer.DrawText(graphics, text, metaFont, chipRect, typeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            chipX = chipRect.Right + 6;
        }

        if (showActions)
        {
            DrawActionButton(graphics, CountdownButtonBounds(itemBounds), L.T("倒计时"), false);
            if (item.Type is not EventType.Anniversary)
            {
                var completeText = item.IsRecurringSeries ? "完成本次" : item.Status is EventStatus.Done ? "已完成" : "完成";
                DrawActionButton(graphics, CompleteButtonBounds(itemBounds), L.T(completeText), !item.IsRecurringSeries && item.Status is EventStatus.Done);
            }
        }
    }

    public static Rectangle CountdownButtonBounds(Rectangle itemBounds)
    {
        var card = Rectangle.Inflate(itemBounds, -4, -7);
        return new Rectangle(card.Right - 76, card.Top + 11, 64, 28);
    }

    public static Rectangle CompleteButtonBounds(Rectangle itemBounds)
    {
        var card = Rectangle.Inflate(itemBounds, -4, -7);
        return new Rectangle(card.Right - 76, card.Top + 48, 64, 28);
    }

    private static void DrawActionButton(Graphics graphics, Rectangle bounds, string text, bool muted)
    {
        text = L.T(text);
        using var path = RoundRect(bounds, 6);
        using var back = new SolidBrush(muted ? AppTheme.Hover : AppTheme.Surface);
        using var border = new Pen(muted ? AppTheme.Border : Color.FromArgb(147, 197, 253));
        graphics.FillPath(back, path);
        graphics.DrawPath(border, path);
        TextRenderer.DrawText(graphics, text, SystemFonts.MessageBoxFont, bounds,
            muted ? UiTokens.TextMuted : UiTokens.Primary,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }

    private static IEnumerable<string> SplitTags(string text) => text.Split(
        new[] { ',', '，', ';', '；', ' ' },
        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static GraphicsPath RoundRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

}
