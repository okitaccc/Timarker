using System.Drawing.Drawing2D;
using Timarker.Models;

namespace Timarker;

internal sealed class ActivityCategoryChart : Control
{
    private IReadOnlyList<(string Name, TimeSpan Value)> _items = [];

    public ActivityCategoryChart()
    {
        Dock = DockStyle.Fill;
        DoubleBuffered = true;
        BackColor = AppTheme.Surface;
        MinimumSize = new Size(0, 190);
    }

    public void SetData(IReadOnlyDictionary<string, TimeSpan> values)
    {
        _items = values.Where(x => x.Key != "空闲" && x.Value > TimeSpan.Zero).OrderByDescending(x => x.Value).Select(x => (x.Key, x.Value)).ToList();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var titleFont = new Font(Font, FontStyle.Bold);
        TextRenderer.DrawText(e.Graphics, "当天时间分布", titleFont, new Point(18, 14), AppTheme.Text);
        TextRenderer.DrawText(e.Graphics, "按活动分类汇总", Font, new Point(18, 38), AppTheme.Muted);
        if (_items.Count == 0)
        {
            TextRenderer.DrawText(e.Graphics, "当天还没有可统计的时间记录", Font, new Rectangle(18, 78, Width - 36, 30), AppTheme.Muted);
            return;
        }

        var diameter = Math.Min(138, Height - 64);
        var chart = new Rectangle(28, 58, diameter, diameter);
        var total = _items.Sum(x => x.Value.TotalSeconds);
        var angle = -90F;
        foreach (var item in _items)
        {
            var sweep = (float)(item.Value.TotalSeconds / total * 360D);
            using var pen = new Pen(ColorFor(item.Name), Math.Max(18, diameter / 5F)) { StartCap = LineCap.Flat, EndCap = LineCap.Flat };
            var ring = Rectangle.Inflate(chart, -(int)pen.Width / 2, -(int)pen.Width / 2);
            e.Graphics.DrawArc(pen, ring, angle, sweep);
            angle += sweep;
        }
        using var totalFont = new Font(Font.FontFamily, 13F * AppTheme.FontScale, FontStyle.Bold);
        TextRenderer.DrawText(e.Graphics, Duration(total), totalFont, chart, AppTheme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        var legendX = chart.Right + 44;
        var legendY = 58;
        var columnWidth = 210;
        for (var i = 0; i < _items.Count; i++)
        {
            var column = i / 4;
            var row = i % 4;
            var x = legendX + column * columnWidth;
            var y = legendY + row * 36;
            using var dot = new SolidBrush(ColorFor(_items[i].Name));
            e.Graphics.FillEllipse(dot, x, y + 5, 10, 10);
            TextRenderer.DrawText(e.Graphics, _items[i].Name, Font, new Rectangle(x + 18, y, 92, 24), AppTheme.Text, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(e.Graphics, ActivityInsights.DurationText(_items[i].Value), Font, new Rectangle(x + 108, y, 96, 24), AppTheme.Muted, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        }
    }

    private static string Duration(double seconds) => ActivityInsights.DurationText(TimeSpan.FromSeconds(seconds));
    private static Color ColorFor(string value) => value switch
    {
        "工作" => UiTokens.Primary, "学习" => UiTokens.Violet, "沟通" => UiTokens.Info,
        "浏览" => UiTokens.Warning, "娱乐" => Color.FromArgb(225, 29, 72), "生活" => UiTokens.Success,
        _ => UiTokens.TextMuted
    };
}
