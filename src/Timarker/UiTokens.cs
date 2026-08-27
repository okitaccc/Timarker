using Timarker.Models;

namespace Timarker;

/// <summary>Timarker 的唯一视觉规范入口。页面不应再自行定义通用颜色、字号、圆角和控件高度。</summary>
internal static class UiTokens
{
    public const string FontFamily = "Microsoft YaHei UI";

    public const float TextSmall = 8.5F;
    public const float TextBody = 9F;
    public const float TextEmphasis = 10F;
    public const float TextSection = 12F;
    public const float TextPageTitle = 18F;
    public const float TextHero = 28F;

    public const int Space1 = 4;
    public const int Space2 = 8;
    public const int Space3 = 12;
    public const int Space4 = 16;
    public const int Space5 = 20;
    public const int Space6 = 24;

    public const int RadiusSmall = 8;
    public const int RadiusMedium = 10;
    public const int RadiusLarge = 14;

    public static int CompactHeight => Scale(32);
    public static int ControlHeight => Scale(36);
    public static int LargeControlHeight => Scale(40);
    public static int MenuRowHeight => Scale(36);

    public static Color Primary => Color.FromArgb(37, 99, 235);
    public static Color PrimaryHover => Color.FromArgb(29, 78, 216);
    public static Color PrimarySoft => AppTheme.IsDark ? Color.FromArgb(30, 58, 95) : Color.FromArgb(239, 246, 255);
    public static Color Focus => AppTheme.IsDark ? Color.FromArgb(96, 165, 250) : Color.FromArgb(59, 130, 246);
    public static Color Success => Color.FromArgb(22, 163, 74);
    public static Color Warning => Color.FromArgb(217, 119, 6);
    public static Color Danger => AppTheme.IsDark ? Color.FromArgb(248, 113, 113) : Color.FromArgb(220, 38, 38);
    public static Color Info => Color.FromArgb(8, 145, 178);
    public static Color Violet => Color.FromArgb(124, 58, 237);

    public static Color EventTypeColor(EventType type) => type switch
    {
        EventType.StartAt => Primary,
        EventType.Deadline => Color.FromArgb(234, 88, 12),
        EventType.TimeWindow => Info,
        EventType.AnytimeToday => Success,
        EventType.Recurring => Violet,
        EventType.Habit => Color.FromArgb(13, 148, 136),
        EventType.Birthday => Color.FromArgb(219, 39, 119),
        EventType.Anniversary => Warning,
        EventType.Maybe => Color.FromArgb(100, 116, 139),
        _ => Primary
    };

    public static Color AppBackground => AppTheme.AppBack;
    public static Color Surface => AppTheme.Surface;
    public static Color SurfaceSubtle => AppTheme.SurfaceAlt;
    public static Color Field => AppTheme.Field;
    public static Color Text => AppTheme.Text;
    public static Color TextMuted => AppTheme.Muted;
    public static Color Border => AppTheme.Border;
    public static Color Hover => AppTheme.Hover;
    public static Color Selected => AppTheme.Selected;
    public static Color Disabled => AppTheme.Disabled;

    public static Font Font(float size = TextBody, FontStyle style = FontStyle.Regular) => new(FontFamily, size, style);
    public static Font DrawingFont(float size = TextBody, FontStyle style = FontStyle.Regular) => new(FontFamily, size * AppTheme.FontScale, style);
    public static int Scale(int value) => (int)Math.Ceiling(value * AppTheme.FontScale);
}
