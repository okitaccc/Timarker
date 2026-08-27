using Timarker.Models;

namespace Timarker;

public sealed class FloatingNoteForm : Form
{
    private static Color Accent => UiTokens.Primary;
    private readonly NoteItem _note;
    private readonly Action _save;
    private readonly Action _changed;
    private readonly Label _title;
    private readonly Label _content;
    private readonly Label _tags;
    private Point _dragOrigin;
    private bool _dragging;

    public FloatingNoteForm(NoteItem note, Action save, Action changed)
    {
        _note = note;
        _save = save;
        _changed = changed;
        Size = new Size(320, 230);
        StartPosition = FormStartPosition.Manual;
        var area = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(
            Math.Clamp(Cursor.Position.X - Width / 2, area.Left, Math.Max(area.Left, area.Right - Width)),
            Math.Clamp(Cursor.Position.Y - 24, area.Top, Math.Max(area.Top, area.Bottom - Height)));
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = AppTheme.Border;
        Font = UiTokens.Font();
        Padding = new Padding(1);
        ModernUi.Round(this, 12);

        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            BackColor = AppTheme.Surface,
            Padding = new Padding(18, 0, 18, 14)
        };
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 5));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        var accent = new Panel { Dock = DockStyle.Fill, BackColor = Accent, Margin = new Padding(-18, 0, -18, 0) };
        _title = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 11F, FontStyle.Bold),
            ForeColor = AppTheme.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        var close = new ModernButton
        {
            Text = "×",
            Size = new Size(30, 30),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.Muted,
            Font = new Font("Segoe UI", 13F),
            Margin = Padding.Empty
        };
        close.FlatAppearance.BorderColor = AppTheme.Surface;
        close.Click += (_, _) => Close();
        var titleBar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = Padding.Empty };
        titleBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        titleBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
        titleBar.Controls.Add(_title, 0, 0);
        titleBar.Controls.Add(close, 1, 0);

        _content = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.Muted,
            Padding = new Padding(0, 6, 0, 6),
            AutoEllipsis = true
        };
        _tags = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Accent,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        card.Controls.Add(accent, 0, 0);
        card.Controls.Add(titleBar, 0, 1);
        card.Controls.Add(_content, 0, 2);
        card.Controls.Add(_tags, 0, 3);
        Controls.Add(card);
        Paint += (_, e) => ModernUi.DrawBorder(e.Graphics, ClientRectangle, 12, AppTheme.Border);
        HookDrag(titleBar);
        HookDrag(_title);
        _title.DoubleClick += (_, _) => EditNote();
        _content.DoubleClick += (_, _) => EditNote();
        RefreshContent();
    }

    private void HookDrag(Control control)
    {
        control.Cursor = Cursors.SizeAll;
        control.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) { _dragging = true; _dragOrigin = e.Location; } };
        control.MouseMove += (_, e) => { if (_dragging) Location = new Point(Location.X + e.X - _dragOrigin.X, Location.Y + e.Y - _dragOrigin.Y); };
        control.MouseUp += (_, _) => _dragging = false;
    }

    private void EditNote()
    {
        using var dialog = new FloatingNoteEditForm(_note);
        var restoreTopMost = TopMost;
        TopMost = false;
        DialogResult result;
        try
        {
            result = dialog.ShowDialog(this);
        }
        finally
        {
            TopMost = restoreTopMost;
            Activate();
        }
        if (result != DialogResult.OK) return;
        _note.UpdatedAt = DateTime.Now;
        _save();
        _changed();
        RefreshContent();
    }

    private void RefreshContent()
    {
        Text = string.IsNullOrWhiteSpace(_note.Title) ? "便签" : _note.Title;
        _title.Text = string.IsNullOrWhiteSpace(_note.Title) ? FirstLine(_note.Content) : _note.Title;
        _content.Text = _note.Content;
        _tags.Text = string.Join("   ", SplitTags(_note.Tags).Take(4).Select(x => $"#{x}"));
    }

    private static IEnumerable<string> SplitTags(string text) =>
        text.Split([',', '，', ';', '；'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string FirstLine(string text) =>
        text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? "未命名便签";
}

internal sealed class FloatingNoteEditForm : Form
{
    private static Color Accent => UiTokens.Primary;
    private readonly NoteItem _note;
    private readonly ModernTextBox _title = new() { Dock = DockStyle.Fill, PlaceholderText = "标题（可选）" };
    private readonly ModernTextBox _content = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, PlaceholderText = "写点什么……" };
    private readonly ModernTextBox _tags = new() { Dock = DockStyle.Fill, PlaceholderText = "词条，用逗号分隔" };
    private string _baseline = "";

    public FloatingNoteEditForm(NoteItem note)
    {
        _note = note;
        Text = "编辑便签";
        ClientSize = new Size(480, 390);
        MinimumSize = MaximumSize = new Size(496, 429);
        StartPosition = FormStartPosition.CenterParent;
        Font = UiTokens.Font();
        BackColor = AppTheme.AppBack;

        var form = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, Padding = new Padding(22), BackColor = AppTheme.AppBack };
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        form.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        form.Controls.Add(_title, 0, 0);
        form.Controls.Add(_content, 0, 1);
        form.Controls.Add(_tags, 0, 2);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 8, 0, 0) };
        var save = new ModernButton { Text = "保存", Width = 88, Height = 36, BackColor = Accent, ForeColor = Color.White };
        var cancel = new ModernButton { Text = "取消", Width = 88, Height = 36, BackColor = AppTheme.Surface, ForeColor = AppTheme.Text };
        save.Click += (_, _) => SaveAndClose();
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        actions.Controls.Add(save);
        actions.Controls.Add(cancel);
        form.Controls.Add(actions, 0, 3);
        Controls.Add(form);

        _title.Text = note.Title;
        _content.Text = note.Content;
        _tags.Text = note.Tags;
        _baseline = Fingerprint();
        L.Apply(this);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult is DialogResult.None && Fingerprint() != _baseline)
        {
            var result = MessageBox.Show("便签有尚未保存的修改。是否保存后关闭？", "保存便签修改", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Cancel) e.Cancel = true;
            else if (result == DialogResult.Yes)
            {
                e.Cancel = true;
                BeginInvoke((MethodInvoker)SaveAndClose);
            }
        }
        base.OnFormClosing(e);
    }

    private string Fingerprint() => $"{_title.Text}\n{_content.Text}\n{_tags.Text}";

    private void SaveAndClose()
    {
        if (string.IsNullOrWhiteSpace(_title.Text) && string.IsNullOrWhiteSpace(_content.Text))
        {
            MessageBox.Show("至少写下一点内容。", "便签是空的", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _note.Title = _title.Text.Trim();
        _note.Content = _content.Text.Trim();
        _note.Tags = string.Join(", ", _tags.Text.Split([',', '，', ';', '；'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase));
        DialogResult = DialogResult.OK;
    }
}
