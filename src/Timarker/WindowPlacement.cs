using Timarker.Models;

namespace Timarker;

internal sealed class WindowPlacement
{
    private Rectangle _normalBounds;
    private FormWindowState _visibleState = FormWindowState.Normal;

    public void ApplyInitial(Form form, AppSettings settings, Size defaultSize)
    {
        var saved = settings.MainWindowX is int x && settings.MainWindowY is int y
            && settings.MainWindowWidth is int width && settings.MainWindowHeight is int height
            ? new Rectangle(x, y, Math.Max(form.MinimumSize.Width, width), Math.Max(form.MinimumSize.Height, height))
            : Rectangle.Empty;
        var visible = Screen.AllScreens.Any(screen => Rectangle.Intersect(screen.WorkingArea, saved) is { Width: >= 120, Height: >= 80 });
        if (!visible)
        {
            form.Size = defaultSize;
            form.StartPosition = FormStartPosition.CenterScreen;
            return;
        }

        form.StartPosition = FormStartPosition.Manual;
        form.Bounds = saved;
        _normalBounds = saved;
        if (settings.MainWindowMaximized)
        {
            _visibleState = FormWindowState.Maximized;
            form.WindowState = FormWindowState.Maximized;
        }
    }

    public void Remember(Form form)
    {
        if (!form.Visible) return;
        if (form.WindowState is FormWindowState.Normal)
        {
            _normalBounds = form.Bounds;
            _visibleState = FormWindowState.Normal;
        }
        else if (form.WindowState is FormWindowState.Maximized) _visibleState = FormWindowState.Maximized;
    }

    public void Restore(Form form, Size defaultSize)
    {
        if (_normalBounds.Width >= form.MinimumSize.Width && _normalBounds.Height >= form.MinimumSize.Height) form.Bounds = _normalBounds;
        else
        {
            form.Size = defaultSize;
            var area = Screen.FromPoint(Cursor.Position).WorkingArea;
            form.Location = new Point(area.Left + (area.Width - form.Width) / 2, area.Top + (area.Height - form.Height) / 2);
        }
        form.Show();
        form.ShowInTaskbar = true;
        form.WindowState = _visibleState is FormWindowState.Minimized ? FormWindowState.Normal : _visibleState;
        form.Activate();
    }

    public void WriteTo(AppSettings settings)
    {
        if (_normalBounds.Width <= 0 || _normalBounds.Height <= 0) return;
        settings.MainWindowX = _normalBounds.X;
        settings.MainWindowY = _normalBounds.Y;
        settings.MainWindowWidth = _normalBounds.Width;
        settings.MainWindowHeight = _normalBounds.Height;
        settings.MainWindowMaximized = _visibleState is FormWindowState.Maximized;
    }
}
