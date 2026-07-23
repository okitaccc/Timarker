using System.Drawing.Drawing2D;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Timarker;

internal static class ModernUi
{
    private static readonly ConditionalWeakTable<Control, object> Styled = new();

    public static void Style(Control control)
    {
        if (control is ContextMenuStrip menu)
        {
            Style(menu);
            return;
        }
        if (control is ModernButton modernButton)
        {
            Round(modernButton, modernButton.CornerRadius);
            return;
        }
        if (control is ModernNumericUpDown modernNumber)
        {
            Round(modernNumber, 9);
            return;
        }
        if (control is ModernTextBox modernText)
        {
            Round(modernText, 9);
            return;
        }
        if (control is ComboBox combo)
        {
            StyleComboBox(combo);
            return;
        }
        if (control is TextBox textBox && textBox.BorderStyle != BorderStyle.None)
        {
            textBox.BorderStyle = BorderStyle.FixedSingle;
            Round(textBox, 8);
            return;
        }

        var radius = control switch
        {
            Button => 7,
            DateTimePicker or NumericUpDown => 6,
            Panel { BorderStyle: BorderStyle.FixedSingle } => 10,
            _ => 0
        };
        if (radius > 0) Round(control, radius);
    }

    private static void StyleComboBox(ComboBox combo)
    {
        Round(combo, 8);
        combo.FlatStyle = FlatStyle.Flat;
    }

    public static void Round(Control control, int radius)
    {
        if (!Styled.TryGetValue(control, out _))
        {
            Styled.Add(control, new object());
            control.SizeChanged += (_, _) => SetRegion(control, radius);
            control.HandleCreated += (_, _) => SetRegion(control, radius);
        }
        SetRegion(control, radius);
    }

    public static void Pill(Control control) => Round(control, 999);

    public static void Outline(Control control, int radius, Func<Color> borderColor)
    {
        Round(control, radius);
        control.Paint += (_, e) => DrawBorder(e.Graphics, control.ClientRectangle, radius, borderColor());
    }

    public static void DrawBorder(Graphics graphics, Rectangle bounds, int radius, Color color, float width = 1F)
    {
        if (bounds.Width <= 3 || bounds.Height <= 3) return;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedPath(new Rectangle(1, 1, bounds.Width - 3, bounds.Height - 3), radius);
        using var pen = new Pen(color, width);
        graphics.DrawPath(pen, path);
    }

    public static GraphicsPath RoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        if (bounds.Width <= 0 || bounds.Height <= 0) return path;
        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static void SetRegion(Control control, int radius)
    {
        if (control.Width <= 1 || control.Height <= 1) return;
        using var path = RoundedPath(new Rectangle(0, 0, control.Width, control.Height), radius);
        var old = control.Region;
        control.Region = new Region(path);
        old?.Dispose();
    }

    public static void Style(ToolStripDropDown menu)
    {
        menu.Renderer = ModernMenuRenderer.Instance;
        menu.Padding = new Padding(7, 7, 7, 7);
        menu.Font = new Font("Microsoft YaHei UI", 9F);
        if (menu is ToolStripDropDownMenu dropDownMenu) dropDownMenu.ShowImageMargin = false;
        menu.MinimumSize = new Size(156, 0);
        menu.SizeChanged -= MenuSizeChanged;
        menu.SizeChanged += MenuSizeChanged;
        SetRegion(menu, 10);
    }

    private static void MenuSizeChanged(object? sender, EventArgs e)
    {
        if (sender is ToolStripDropDown menu) SetRegion(menu, 10);
    }

}

internal sealed class ModernButton : Button
{
    public int CornerRadius { get; set; } = 9;

    public ModernButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.BorderColor = Color.FromArgb(148, 163, 184);
        ModernUi.Round(this, CornerRadius);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var color = Enabled ? FlatAppearance.BorderColor : Color.FromArgb(203, 213, 225);
        ModernUi.DrawBorder(e.Graphics, ClientRectangle, CornerRadius, color, 1.2F);
    }
}

internal sealed class ModernNumericUpDown : UserControl
{
    private readonly TextBox _editor = new()
    {
        BorderStyle = BorderStyle.None,
        TextAlign = HorizontalAlignment.Left,
        BackColor = Color.White
    };
    private decimal _minimum;
    private decimal _maximum = 100;
    private decimal _increment = 1;
    private decimal _value;

    public event EventHandler? ValueChanged;

    public decimal Minimum
    {
        get => _minimum;
        set { _minimum = value; Value = _value; }
    }

    public decimal Maximum
    {
        get => _maximum;
        set { _maximum = value; Value = _value; }
    }

    public decimal Increment
    {
        get => _increment;
        set => _increment = value <= 0 ? 1 : value;
    }

    public decimal Value
    {
        get => _value;
        set
        {
            var next = Math.Min(_maximum, Math.Max(_minimum, value));
            if (_value == next && _editor.Text.Length > 0) return;
            _value = next;
            _editor.Text = FormatValue(next);
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public HorizontalAlignment TextAlign
    {
        get => _editor.TextAlign;
        set => _editor.TextAlign = value;
    }

    public ModernNumericUpDown()
    {
        AutoSize = false;
        Size = new Size(82, 30);
        MinimumSize = new Size(72, 30);
        BackColor = Color.White;
        Controls.Add(_editor);
        _editor.Text = "0";
        _editor.Enter += (_, _) => Invalidate();
        _editor.Leave += (_, _) => { CommitText(); Invalidate(); };
        _editor.KeyDown += EditorKeyDown;
        ModernUi.Round(this, 9);
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        _editor.SetBounds(10, Math.Max(5, (Height - _editor.PreferredHeight) / 2), Math.Max(18, Width - 50), _editor.PreferredHeight);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        _editor.Focus();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && e.X >= Width - 34)
        {
            Step(e.Y < Height / 2 ? _increment : -_increment);
            _editor.Focus();
            return;
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        Step(e.Delta > 0 ? _increment : -_increment);
        base.OnMouseWheel(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var border = ContainsFocus ? Color.FromArgb(59, 130, 246) : Color.FromArgb(148, 163, 184);
        ModernUi.DrawBorder(e.Graphics, ClientRectangle, 9, Enabled ? border : Color.FromArgb(203, 213, 225), 1.2F);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var divider = new Pen(Color.FromArgb(226, 232, 240));
        e.Graphics.DrawLine(divider, Width - 34, 5, Width - 34, Height - 5);
        using var arrow = new Pen(Enabled ? Color.FromArgb(71, 85, 105) : Color.FromArgb(148, 163, 184), 1.5F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        var x = Width - 17;
        e.Graphics.DrawLine(arrow, x - 4, 11, x, 7);
        e.Graphics.DrawLine(arrow, x, 7, x + 4, 11);
        e.Graphics.DrawLine(arrow, x - 4, Height - 11, x, Height - 7);
        e.Graphics.DrawLine(arrow, x, Height - 7, x + 4, Height - 11);
    }

    private void EditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Up) Step(_increment);
        else if (e.KeyCode == Keys.Down) Step(-_increment);
        else if (e.KeyCode == Keys.Enter) CommitText();
        else return;
        e.SuppressKeyPress = true;
    }

    private void Step(decimal amount)
    {
        CommitText();
        Value += amount;
    }

    private void CommitText()
    {
        if (decimal.TryParse(_editor.Text, out var value)) Value = value;
        else _editor.Text = FormatValue(_value);
    }

    private static string FormatValue(decimal value) => decimal.Truncate(value) == value
        ? decimal.Truncate(value).ToString("0")
        : value.ToString("0.##");
}

internal sealed class ModernTextBox : UserControl
{
    private readonly TextBox _editor;

    [AllowNull]
    public override string Text
    {
        get => _editor?.Text ?? base.Text;
        set
        {
            if (_editor is null) base.Text = value ?? string.Empty;
            else if (_editor.Text != value) _editor.Text = value ?? string.Empty;
        }
    }

    public string PlaceholderText
    {
        get => _editor.PlaceholderText;
        set => _editor.PlaceholderText = value;
    }

    public bool Multiline
    {
        get => _editor.Multiline;
        set { _editor.Multiline = value; PerformLayout(); }
    }

    public ScrollBars ScrollBars
    {
        get => _editor.ScrollBars;
        set => _editor.ScrollBars = value;
    }

    public ModernTextBox()
    {
        AutoSize = false;
        Height = 30;
        MinimumSize = new Size(40, 30);
        BackColor = Color.White;
        TabStop = true;
        _editor = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = BackColor,
            ForeColor = ForeColor,
            Font = Font
        };
        _editor.TextChanged += (_, _) => base.Text = _editor.Text;
        _editor.Enter += (_, _) => Invalidate();
        _editor.Leave += (_, _) => Invalidate();
        Controls.Add(_editor);
        Click += (_, _) => _editor.Focus();
        ModernUi.Round(this, 9);
    }

    public void Clear() => _editor.Clear();

    public void SelectAll() => _editor.SelectAll();

    public new bool Focus() => _editor.Focus();

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (_editor is null) return;
        if (Multiline)
        {
            _editor.SetBounds(10, 8, Math.Max(10, Width - 20), Math.Max(10, Height - 16));
        }
        else
        {
            var editorHeight = _editor.PreferredHeight;
            _editor.SetBounds(10, Math.Max(2, (Height - editorHeight) / 2), Math.Max(10, Width - 20), editorHeight);
        }
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (_editor is not null) _editor.Font = Font;
        PerformLayout();
    }

    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
        if (_editor is not null) _editor.BackColor = BackColor;
    }

    protected override void OnForeColorChanged(EventArgs e)
    {
        base.OnForeColorChanged(e);
        if (_editor is not null) _editor.ForeColor = ForeColor;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        ModernUi.DrawBorder(e.Graphics, ClientRectangle, 9,
            ContainsFocus ? Color.FromArgb(59, 130, 246) : Color.FromArgb(148, 163, 184), 1.25F);
    }
}

internal sealed class ModernComboBox : ComboBox
{
    private const int PaintMessage = 0x000F;
    private const int ShowDropDownMessage = 0x014F;
    private const int LeftButtonDownMessage = 0x0201;
    private const int LeftButtonDoubleClickMessage = 0x0203;
    private ToolStripDropDown? _popup;

    public ModernComboBox()
    {
        DropDownStyle = ComboBoxStyle.DropDownList;
        FlatStyle = FlatStyle.Flat;
        ItemHeight = 28;
        MaxDropDownItems = 8;
        BackColor = Color.White;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.F4 || e.Alt && e.KeyCode is Keys.Down)
        {
            ShowModernDropDown();
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        base.OnSelectedIndexChanged(e);
        Invalidate();
    }

    protected override void WndProc(ref Message m)
    {
        // The native ComboBox opens its menu before OnMouseDown runs. Intercept
        // the window message so the legacy menu never gets a frame to paint.
        if (m.Msg is LeftButtonDownMessage or LeftButtonDoubleClickMessage)
        {
            ShowModernDropDown();
            return;
        }
        if (m.Msg == ShowDropDownMessage && m.WParam != IntPtr.Zero)
        {
            ShowModernDropDown();
            return;
        }
        base.WndProc(ref m);
        if (m.Msg == PaintMessage) DrawClosedState();
    }

    private void ShowModernDropDown()
    {
        if (!Enabled || Items.Count == 0 || _popup is { Visible: true }) return;
        OnDropDown(EventArgs.Empty);

        var visibleItems = Math.Min(Math.Max(1, Items.Count), Math.Max(1, MaxDropDownItems));
        var popupWidth = Math.Max(Width, DropDownWidth);
        var list = new ListBox
        {
            BorderStyle = BorderStyle.None,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 30,
            IntegralHeight = false,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(31, 41, 55),
            Font = Font,
            Size = new Size(popupWidth - 12, visibleItems * 30)
        };
        foreach (var item in Items) list.Items.Add(item);
        list.SelectedIndex = SelectedIndex;
        list.DrawItem += DrawPopupItem;

        var host = new ToolStripControlHost(list)
        {
            AutoSize = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            Size = list.Size
        };
        _popup = new ToolStripDropDown
        {
            AutoSize = false,
            BackColor = Color.White,
            Padding = new Padding(6),
            Size = new Size(popupWidth, visibleItems * 30 + 12),
            Renderer = ModernMenuRenderer.Instance
        };
        _popup.Items.Add(host);
        ModernUi.Round(_popup, 12);
        _popup.Closed += (_, _) =>
        {
            _popup = null;
            OnDropDownClosed(EventArgs.Empty);
            Invalidate();
        };
        list.MouseUp += (_, e) =>
        {
            var index = list.IndexFromPoint(e.Location);
            if (index < 0) return;
            SelectedIndex = index;
            _popup?.Close();
        };
        list.KeyDown += (_, e) =>
        {
            if (e.KeyCode is Keys.Enter && list.SelectedIndex >= 0)
            {
                SelectedIndex = list.SelectedIndex;
                _popup?.Close();
            }
            else if (e.KeyCode is Keys.Escape)
            {
                _popup?.Close();
            }
        };

        _popup.Show(this, new Point(0, Height + 4));
        list.Focus();
        Invalidate();
    }

    private void DrawPopupItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not ListBox list || e.Index < 0 || e.Index >= list.Items.Count) return;
        using (var background = new SolidBrush(Color.White)) e.Graphics.FillRectangle(background, e.Bounds);
        if ((e.State & DrawItemState.Selected) != 0)
        {
            var selectedBounds = Rectangle.Inflate(e.Bounds, -3, -2);
            using var selectedPath = ModernUi.RoundedPath(selectedBounds, 6);
            using var selectedBrush = new SolidBrush(Color.FromArgb(239, 246, 255));
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillPath(selectedBrush, selectedPath);
        }
        TextRenderer.DrawText(e.Graphics, GetItemText(list.Items[e.Index]), Font,
            new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 20, e.Bounds.Height),
            Color.FromArgb(31, 41, 55), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    private void DrawClosedState()
    {
        if (!IsHandleCreated || Width <= 1 || Height <= 1) return;
        using var graphics = Graphics.FromHwnd(Handle);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = ModernUi.RoundedPath(bounds, 8);
        using var background = new SolidBrush(Enabled ? Color.White : Color.FromArgb(248, 250, 252));
        using var border = new Pen(Focused || _popup is { Visible: true } ? Color.FromArgb(96, 165, 250) : Color.FromArgb(203, 213, 225));
        graphics.FillPath(background, path);
        graphics.DrawPath(border, path);

        TextRenderer.DrawText(graphics, GetItemText(SelectedItem), Font,
            new Rectangle(12, 0, Math.Max(0, Width - 44), Height),
            Enabled ? Color.FromArgb(31, 41, 55) : Color.FromArgb(148, 163, 184),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        using var arrow = new Pen(Color.FromArgb(71, 85, 105), 1.7F) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        var centerX = Width - 20;
        var centerY = Height / 2;
        graphics.DrawLine(arrow, centerX - 4, centerY - 2, centerX, centerY + 2);
        graphics.DrawLine(arrow, centerX, centerY + 2, centerX + 4, centerY - 2);
    }
}

internal sealed class ModernContextMenuStrip : ContextMenuStrip
{
    public ModernContextMenuStrip() => ModernUi.Style((ToolStripDropDown)this);
}

internal sealed class ModernMenuRenderer : ToolStripProfessionalRenderer
{
    public static ModernMenuRenderer Instance { get; } = new();

    private ModernMenuRenderer() : base(new ModernMenuColors())
    {
        RoundedEdges = false;
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Color.FromArgb(31, 41, 55) : Color.FromArgb(148, 163, 184);
        base.OnRenderItemText(e);
    }
}

internal sealed class ModernMenuColors : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => Color.White;
    public override Color MenuBorder => Color.FromArgb(226, 232, 240);
    public override Color MenuItemBorder => Color.FromArgb(219, 234, 254);
    public override Color MenuItemSelected => Color.FromArgb(239, 246, 255);
    public override Color MenuItemSelectedGradientBegin => MenuItemSelected;
    public override Color MenuItemSelectedGradientEnd => MenuItemSelected;
    public override Color ImageMarginGradientBegin => Color.White;
    public override Color ImageMarginGradientMiddle => Color.White;
    public override Color ImageMarginGradientEnd => Color.White;
    public override Color SeparatorDark => Color.FromArgb(226, 232, 240);
    public override Color SeparatorLight => Color.FromArgb(226, 232, 240);
}
