using Timarker.Models;

namespace Timarker;

internal sealed class SidebarNavigation : UserControl
{
    private static Color Accent => UiTokens.Primary;
    private readonly Dictionary<string, Button> _buttons = [];

    public SidebarNavigation(AppSettings settings, IReadOnlyList<PluginDescriptor> plugins, Func<string, bool> canNavigate, Action<string> navigate)
    {
        Dock = DockStyle.Fill;
        Margin = new Padding(0, 0, 16, 0);
        BackColor = AppTheme.AppBack;
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, BackColor = BackColor, AutoScroll = true };
        var row = 0;
        void Add(Control control, float height, SizeType type = SizeType.Absolute)
        {
            panel.RowStyles.Add(new RowStyle(type, height));
            panel.Controls.Add(control, 0, row++);
        }

        Add(Brand(), 66);
        int? previousGroup = null;
        foreach (var module in FeatureCatalog.All.Where(x => !x.CanDisable || settings.FeatureEnabled(x.Key)))
        {
            if (previousGroup != module.Group)
                Add(new Label { Text = FeatureCatalog.GroupName(module.Group), Dock = DockStyle.Fill, ForeColor = UiTokens.TextMuted, Font = UiTokens.Font(UiTokens.TextSmall), TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(UiTokens.Space3, 0, 0, 2) }, 26);
            var button = CreateButton(module.Name);
            _buttons[module.Key] = button;
            button.Click += (_, _) =>
            {
                if (!canNavigate(module.Key)) return;
                SelectModule(module.Key);
                navigate(module.Key);
            };
            Add(button, 44);
            previousGroup = module.Group;
        }
        var activePlugins = plugins.Where(x => x.Instance is not null).ToList();
        if (activePlugins.Count > 0)
        {
            Add(new Label { Text = "插件", Dock = DockStyle.Fill, ForeColor = UiTokens.TextMuted, Font = UiTokens.Font(UiTokens.TextSmall), TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(UiTokens.Space3, 0, 0, 2) }, 26);
            foreach (var plugin in activePlugins)
            {
                var key = "plugin:" + plugin.Manifest.Id;
                var button = CreateButton(plugin.Manifest.Name);
                _buttons[key] = button;
                button.Click += (_, _) =>
                {
                    if (!canNavigate(key)) return;
                    SelectModule(key);
                    navigate(key);
                };
                Add(button, 44);
            }
        }
        Add(new Panel { Dock = DockStyle.Fill, BackColor = BackColor }, 100, SizeType.Percent);
        panel.RowCount = row;
        Controls.Add(panel);
        SelectModule("editor");
    }

    public void SelectModule(string key)
    {
        foreach (var (candidate, button) in _buttons) StyleButton(button, candidate == key);
    }

    private static Button CreateButton(string text)
    {
        var button = new ModernButton
        {
            Text = text,
            Height = UiTokens.ControlHeight,
            Dock = DockStyle.Fill,
            BackColor = AppTheme.AppBack,
            ForeColor = AppTheme.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(UiTokens.RadiusLarge, 0, 0, 0),
            Margin = new Padding(0, 3, 0, 3),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = AppTheme.AppBack;
        button.FlatAppearance.MouseOverBackColor = AppTheme.Hover;
        return button;
    }

    private static void StyleButton(Button button, bool selected)
    {
        button.BackColor = selected ? AppTheme.Selected : AppTheme.AppBack;
        button.ForeColor = selected ? Accent : AppTheme.Text;
        button.Font = UiTokens.Font(UiTokens.TextBody, selected ? FontStyle.Bold : FontStyle.Regular);
        button.FlatAppearance.BorderColor = selected ? UiTokens.Focus : AppTheme.AppBack;
    }

    private static Control Brand()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, BackColor = AppTheme.AppBack };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        panel.Controls.Add(new Label { Text = "事刻", Dock = DockStyle.Fill, Font = UiTokens.Font(UiTokens.TextSection, FontStyle.Bold), ForeColor = UiTokens.Text });
        panel.Controls.Add(new Label { Text = "计划 · 执行 · 回顾", Dock = DockStyle.Fill, ForeColor = AppTheme.Muted });
        return panel;
    }
}
