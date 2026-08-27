namespace Timarker;

public sealed class CommonTagPicker : UserControl
{
    private readonly FlowLayoutPanel _panel = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = false,
        WrapContents = true
    };

    public CommonTagPicker()
    {
        Dock = DockStyle.Fill;
        Height = 152;
        MinimumSize = new Size(0, 144);
        Padding = Padding.Empty;
        BackColor = AppTheme.Surface;
        Controls.Add(_panel);

        foreach (var tag in new[]
        {
            "工作", "学习", "生活", "家庭", "健康", "运动",
            "财务", "购物", "社交", "旅行", "生日", "纪念日", "医疗", "兴趣"
        })
        {
            var chip = new CheckBox
            {
                Text = tag,
                Tag = tag,
                Appearance = Appearance.Button,
                AutoSize = false,
                Width = TextRenderer.MeasureText(tag, Font).Width + 28,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.SurfaceAlt,
                ForeColor = AppTheme.Muted,
                Margin = new Padding(0, 0, 8, 8),
                Padding = Padding.Empty,
                TextAlign = ContentAlignment.MiddleCenter,
                UseCompatibleTextRendering = false
            };
            chip.FlatAppearance.BorderSize = 0;
            chip.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            chip.CheckedChanged += (_, _) => StyleChip(chip);
            StyleChip(chip);
            ModernUi.Outline(chip, 16, () => chip.FlatAppearance.BorderColor);
            _panel.Controls.Add(chip);
        }
        L.Apply(this);
    }

    public string SelectedText => string.Join(", ", _panel.Controls
        .OfType<CheckBox>()
        .Where(c => c.Checked)
        .Select(c => c.Tag?.ToString()));

    public void ClearSelected()
    {
        foreach (var chip in _panel.Controls.OfType<CheckBox>())
        {
            chip.Checked = false;
        }
    }

    public void SetSelected(IEnumerable<string> tags)
    {
        var selected = tags.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var chip in _panel.Controls.OfType<CheckBox>())
        {
            chip.Checked = selected.Contains(chip.Tag?.ToString() ?? chip.Text);
        }
    }

    private static void StyleChip(CheckBox chip)
    {
        var tag = chip.Tag?.ToString() ?? chip.Text;
        chip.Text = chip.Checked ? $"✓ {tag}" : tag;
        chip.BackColor = chip.Checked ? AppTheme.Selected : AppTheme.SurfaceAlt;
        chip.ForeColor = chip.Checked ? Color.FromArgb(96, 165, 250) : AppTheme.Muted;
        chip.FlatAppearance.BorderColor = chip.Checked ? UiTokens.Primary : UiTokens.Border;
    }
}
