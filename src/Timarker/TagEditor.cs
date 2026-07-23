namespace Timarker;

public sealed class TagEditor : UserControl
{
    private readonly FlowLayoutPanel _chips = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        WrapContents = true
    };

    private readonly TextBox _input = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        PlaceholderText = "输入词条后按 Enter"
    };
    private readonly List<string> _tags = [];

    public TagEditor()
    {
        Dock = DockStyle.Fill;
        Height = 112;
        MinimumSize = new Size(0, 104);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var inputShell = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = Padding.Empty,
            Padding = new Padding(11, 8, 11, 6)
        };
        inputShell.Paint += (_, e) =>
        {
            ModernUi.DrawBorder(e.Graphics, inputShell.ClientRectangle, 9,
                _input.Focused ? Color.FromArgb(59, 130, 246) : Color.FromArgb(148, 163, 184), 1.25F);
        };
        _input.Enter += (_, _) => inputShell.Invalidate();
        _input.Leave += (_, _) => inputShell.Invalidate();
        inputShell.Controls.Add(_input);
        ModernUi.Round(inputShell, 9);
        layout.Controls.Add(inputShell, 0, 0);
        layout.Controls.Add(_chips, 0, 1);
        Controls.Add(layout);

        _input.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                AddTag(_input.Text);
                _input.Clear();
                e.SuppressKeyPress = true;
            }
        };
        L.Apply(this);
    }

    public string TextValue
    {
        get => string.Join(", ", _tags);
        set
        {
            _tags.Clear();
            foreach (var tag in SplitTags(value))
            {
                _tags.Add(tag);
            }
            Render();
        }
    }

    public void ClearTags()
    {
        _tags.Clear();
        _input.Clear();
        Render();
    }

    private void AddTag(string value)
    {
        foreach (var tag in SplitTags(value))
        {
            if (!_tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                _tags.Add(tag);
            }
        }
        Render();
    }

    private void Render()
    {
        _chips.Controls.Clear();
        foreach (var tag in _tags)
        {
            var chip = new Button
            {
                Text = $"{tag}  x",
                AutoSize = true,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(232, 240, 255),
                ForeColor = Color.FromArgb(37, 99, 235),
                Margin = new Padding(0, 0, 6, 6)
            };
            chip.FlatAppearance.BorderSize = 0;
            chip.FlatAppearance.BorderColor = Color.FromArgb(191, 219, 254);
            ModernUi.Outline(chip, 14, () => chip.FlatAppearance.BorderColor);
            chip.Click += (_, _) =>
            {
                _tags.Remove(tag);
                Render();
            };
            _chips.Controls.Add(chip);
        }
    }

    private static IEnumerable<string> SplitTags(string text)
    {
        return text
            .Split([',', '\uFF0C', ';', '\uFF1B', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 0);
    }
}
