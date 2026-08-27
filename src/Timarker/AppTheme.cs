using Microsoft.Win32;
using Timarker.Models;
using System.Runtime.CompilerServices;

namespace Timarker;

internal static class AppTheme
{
    private static readonly ConditionalWeakTable<Control, object> Watched = new();
    private static readonly ConditionalWeakTable<Control, FontState> Fonts = new();
    public static bool IsDark { get; private set; }
    public static float FontScale { get; private set; } = 1F;
    public static Color AppBack => IsDark ? Color.FromArgb(11, 18, 32) : Color.FromArgb(246, 247, 251);
    public static Color Surface => IsDark ? Color.FromArgb(17, 24, 39) : Color.White;
    public static Color Text => IsDark ? Color.FromArgb(229, 231, 235) : Color.FromArgb(31, 41, 55);
    public static Color Muted => IsDark ? Color.FromArgb(148, 163, 184) : Color.FromArgb(100, 116, 139);
    public static Color Border => IsDark ? Color.FromArgb(51, 65, 85) : Color.FromArgb(226, 232, 240);
    public static Color Field => IsDark ? Color.FromArgb(15, 23, 42) : Color.White;
    public static Color SurfaceAlt => IsDark ? Color.FromArgb(24, 34, 51) : Color.FromArgb(248, 250, 252);
    public static Color Hover => IsDark ? Color.FromArgb(30, 41, 59) : Color.FromArgb(241, 245, 249);
    public static Color Selected => IsDark ? Color.FromArgb(30, 58, 95) : Color.FromArgb(239, 246, 255);
    public static Color Disabled => IsDark ? Color.FromArgb(30, 41, 59) : Color.FromArgb(248, 250, 252);

    public static void Use(AppSettings settings)
    {
        FontScale = Math.Clamp(settings.FontScalePercent, 90, 125) / 100F;
        IsDark = settings.Theme switch
        {
            "dark" => true,
            "light" => false,
            _ => SystemUsesDarkTheme()
        };
    }

    public static void Apply(Control root)
    {
        Style(root);
        if (!Watched.TryGetValue(root, out _))
        {
            Watched.Add(root, new object());
            root.ControlAdded += (_, e) => { if (e.Control is not null) Apply(e.Control); };
        }
        foreach (Control child in root.Controls) Apply(child);
    }

    private static void Style(Control control)
    {
        ApplyFontScale(control);
        if (!IsDark) return;
        control.BackColor = TranslateBack(control.BackColor);
        control.ForeColor = TranslateFore(control.ForeColor);
        if (control is ModernButton button && button.BackColor == Surface)
            button.FlatAppearance.BorderColor = Border;
    }

    private static void ApplyFontScale(Control control)
    {
        if (!Fonts.TryGetValue(control, out var state))
        {
            state = new FontState(control.Font);
            Fonts.Add(control, state);
        }
        var size = state.BaseSize * FontScale;
        if (Math.Abs(control.Font.Size - size) > .05F)
            control.Font = new Font(state.Family, size, state.Style, GraphicsUnit.Point);
    }

    private sealed record FontState(string Family, float BaseSize, FontStyle Style)
    {
        public FontState(Font font) : this(font.FontFamily.Name, font.SizeInPoints, font.Style) { }
    }

    private static Color TranslateBack(Color color)
    {
        if (color == Color.Transparent || color.A == 0) return color;
        if (color.ToArgb() == SystemColors.Control.ToArgb()) return AppBack;
        if (color.ToArgb() == SystemColors.Window.ToArgb()) return Field;
        if (color == Color.White) return Surface;
        if (Is(color, 246, 247, 251)) return AppBack;
        if (Is(color, 248, 250, 252)) return SurfaceAlt;
        if (Is(color, 241, 245, 249)) return Hover;
        if (Is(color, 239, 246, 255) || Is(color, 229, 239, 255)) return Selected;
        return color;
    }

    private static Color TranslateFore(Color color)
    {
        if (color.ToArgb() == SystemColors.ControlText.ToArgb() || color.ToArgb() == SystemColors.WindowText.ToArgb() || color.ToArgb() == Color.Black.ToArgb()) return Text;
        if (Is(color, 15, 23, 42) || Is(color, 31, 41, 55)) return Text;
        if (Is(color, 100, 116, 139) || Is(color, 107, 114, 128) || Is(color, 71, 85, 105)) return Muted;
        return color;
    }

    private static bool Is(Color color, int r, int g, int b) => color.R == r && color.G == g && color.B == b;

    private static bool SystemUsesDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch { return false; }
    }
}
