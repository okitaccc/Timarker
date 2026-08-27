using Timarker.Models;

namespace Timarker;

public sealed class NotesView : UserControl
{
    private static Color AppBack => AppTheme.AppBack;
    private static Color Surface => AppTheme.Surface;
    private static Color TextMain => AppTheme.Text;
    private static Color TextMuted => AppTheme.Muted;
    private static Color Accent => UiTokens.Primary;
    private static Color AccentSoft => AppTheme.Selected;
    private static Color Border => AppTheme.Border;

    private readonly List<NoteItem> _notes;
    private readonly Action _save;
    private readonly FlowLayoutPanel _board = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        WrapContents = true,
        BackColor = AppBack,
        Padding = new Padding(4, 8, 8, 8)
    };
    private readonly TextBox _search = new() { PlaceholderText = "搜索便签内容或词条", BorderStyle = BorderStyle.None };
    private readonly TextBox _title = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, PlaceholderText = "标题（可选）" };
    private readonly TextBox _content = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Multiline = true, ScrollBars = ScrollBars.Vertical, PlaceholderText = "写点什么……" };
    private readonly TextBox _tags = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, PlaceholderText = "添加词条（可选）" };
    private readonly CheckBox _pinned = new()
    {
        Text = "     置顶",
        Appearance = Appearance.Button,
        AutoSize = false,
        Size = new Size(82, 36),
        FlatStyle = FlatStyle.Flat,
        TextAlign = ContentAlignment.MiddleCenter,
        ForeColor = TextMuted,
        BackColor = Surface,
        Margin = new Padding(0, 5, 8, 0),
        Cursor = Cursors.Hand
    };
    private readonly TableLayoutPanel _shell;
    private readonly Panel _composerHost = new() { Dock = DockStyle.Fill, Margin = new Padding(4, 8, 4, 12) };
    private Control? _collapsedComposer;
    private Control? _expandedComposer;
    private Button? _deleteButton;
    private Guid? _editingId;
    private string _baselineTitle = "";
    private string _baselineContent = "";
    private string _baselineTags = "";
    private bool _baselinePinned;
    private readonly System.Windows.Forms.Timer _cardClickTimer = new() { Interval = SystemInformation.DoubleClickTime };
    private readonly System.Windows.Forms.Timer _searchTimer = new() { Interval = 150 };
    private Action? _pendingCardClick;

    public NotesView(List<NoteItem> notes, Action save)
    {
        _notes = notes;
        _save = save;
        Dock = DockStyle.Fill;
        BackColor = AppBack;
        Font = new Font("Microsoft YaHei UI", 9F);

        _shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(24, 20, 24, 18),
            BackColor = AppBack
        };
        _shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        _shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        _shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        BuildUi();
        _cardClickTimer.Tick += (_, _) =>
        {
            _cardClickTimer.Stop();
            var action = _pendingCardClick;
            _pendingCardClick = null;
            action?.Invoke();
        };
        _searchTimer.Tick += (_, _) =>
        {
            _searchTimer.Stop();
            RefreshBoard();
        };
        L.Apply(this);
        RefreshBoard();
    }

    private void BuildUi()
    {
        _search.BackColor = Surface;
        _title.BackColor = Surface;
        _content.BackColor = Surface;
        _tags.BackColor = Surface;

        _shell.Controls.Add(BuildHeader(), 0, 0);
        BuildComposer();
        _shell.Controls.Add(_composerHost, 0, 1);
        _shell.Controls.Add(_board, 0, 2);
        Controls.Add(_shell);

        _search.TextChanged += (_, _) =>
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        };
        _content.KeyDown += EditorKeyDown;
        _title.KeyDown += EditorKeyDown;
        _tags.KeyDown += EditorKeyDown;
        _pinned.FlatAppearance.BorderSize = 0;
        _pinned.CheckedChanged += (_, _) => StylePinButton();
        _pinned.Paint += (_, e) => DrawPin(e.Graphics, 12, 8, _pinned.Checked ? Color.White : Accent);
        ModernUi.Round(_pinned, 9);
        StylePinButton();
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = new Padding(4, 0, 4, 0) };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));

        var words = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Margin = Padding.Empty };
        words.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        words.Controls.Add(new Label
        {
            Text = "便签",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 19F, FontStyle.Bold),
            ForeColor = TextMain
        });
        words.Controls.Add(new Label
        {
            Text = "先记下来，需要时再把它变成行动。",
            Dock = DockStyle.Fill,
            ForeColor = TextMuted
        }, 0, 1);
        header.Controls.Add(words, 0, 0);
        header.Controls.Add(SearchField(), 1, 0);
        return header;
    }

    private void BuildComposer()
    {
        var collapsed = BorderedPanel(Surface, new Padding(18, 0, 18, 0));
        var prompt = new Label
        {
            Text = "写点什么……",
            Dock = DockStyle.Fill,
            ForeColor = TextMuted,
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.IBeam,
            Font = new Font(Font.FontFamily, 10F)
        };
        collapsed.Controls.Add(prompt);
        HookClick(collapsed, () => ShowEditor());

        var expanded = BorderedPanel(Surface, new Padding(18, 12, 18, 12));
        var form = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Margin = Padding.Empty };
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        form.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        _title.Font = new Font(Font.FontFamily, 11F, FontStyle.Bold);
        _content.Font = new Font(Font.FontFamily, 10F);
        form.Controls.Add(_title, 0, 0);
        form.Controls.Add(_content, 0, 1);

        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        var tagsField = BorderedPanel(Color.FromArgb(248, 250, 252), new Padding(10, 8, 10, 6));
        tagsField.Margin = new Padding(0, 4, 0, 4);
        _tags.BackColor = tagsField.BackColor;
        tagsField.Controls.Add(_tags);
        footer.Controls.Add(tagsField, 0, 0);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, FlowDirection = FlowDirection.RightToLeft, Margin = Padding.Empty };
        var done = ActionButton("完成", true, 74);
        done.Click += (_, _) => FinishEditor();
        var cancel = ActionButton("取消", false, 66);
        cancel.Click += (_, _) => HideEditor();
        _deleteButton = ActionButton("删除", false, 66);
        _deleteButton.ForeColor = UiTokens.Danger;
        _deleteButton.Visible = false;
        _deleteButton.Click += (_, _) => DeleteCurrent();
        actions.Controls.Add(cancel);
        actions.Controls.Add(done);
        actions.Controls.Add(_deleteButton);
        actions.Controls.Add(_pinned);
        footer.Controls.Add(actions, 0, 1);
        form.Controls.Add(footer, 0, 2);
        expanded.Controls.Add(form);
        expanded.Visible = false;

        _collapsedComposer = collapsed;
        _expandedComposer = expanded;
        _composerHost.Controls.Add(expanded);
        _composerHost.Controls.Add(collapsed);
    }

    public void RefreshView() => RefreshBoard();

    private void RefreshBoard()
    {
        _board.SuspendLayout();
        _board.Controls.Clear();
        var query = _search.Text.Trim();
        var items = _notes
            .Where(x => string.IsNullOrWhiteSpace(query)
                || x.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                || x.Content.Contains(query, StringComparison.OrdinalIgnoreCase)
                || EffectiveTags(x).Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.UpdatedAt)
            .ToList();

        var pinned = items.Where(x => x.IsPinned).ToList();
        var others = items.Where(x => !x.IsPinned).ToList();
        if (pinned.Count > 0) AddSection("置顶", pinned);
        if (others.Count > 0) AddSection(pinned.Count > 0 ? "其他" : "全部便签", others);

        if (items.Count == 0)
        {
            _board.Controls.Add(new Label
            {
                Text = _notes.Count == 0 ? "还没有便签。点击上方输入框，先记下一件事。" : "没有找到相关便签。",
                AutoSize = true,
                ForeColor = TextMuted,
                Font = new Font(Font.FontFamily, 10F),
                Padding = new Padding(8, 20, 8, 8)
            });
        }
        _board.ResumeLayout();
    }

    private void AddSection(string title, IReadOnlyList<NoteItem> notes)
    {
        var heading = new Label
        {
            Text = L.T(title),
            Width = Math.Max(300, _board.ClientSize.Width - 36),
            Height = 34,
            Font = new Font(Font.FontFamily, 10F, FontStyle.Bold),
            ForeColor = TextMain,
            Padding = new Padding(2, 6, 0, 0),
            Margin = new Padding(0, 0, 0, 6)
        };
        _board.Controls.Add(heading);
        _board.SetFlowBreak(heading, true);
        foreach (var note in notes) _board.Controls.Add(BuildNoteCard(note));
        if (_board.Controls.Count > 0) _board.SetFlowBreak(_board.Controls[^1], true);
    }

    private Control BuildNoteCard(NoteItem note)
    {
        var frame = new Panel
        {
            Width = 292,
            Height = 170,
            BackColor = _editingId == note.Id ? Color.FromArgb(147, 197, 253) : Border,
            Padding = new Padding(1),
            Margin = new Padding(0, 0, 14, 14),
            Cursor = Cursors.Hand
        };
        ModernUi.Round(frame, 12);
        frame.Paint += (_, e) => ModernUi.DrawBorder(e.Graphics, frame.ClientRectangle, 12,
            _editingId == note.Id ? Color.FromArgb(96, 165, 250) : Border, 1.2F);
        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            BackColor = _editingId == note.Id ? AccentSoft : Surface,
            Margin = Padding.Empty,
            Padding = new Padding(16, 0, 16, 10)
        };
        ModernUi.Round(card, 11);
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 4));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        card.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = note.IsPinned ? Accent : Color.FromArgb(203, 213, 225), Margin = new Padding(-16, 0, -16, 0) }, 0, 0);

        card.Controls.Add(new Label
        {
            Text = CardTitle(note),
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 10.5F, FontStyle.Bold),
            ForeColor = TextMain,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        }, 0, 1);
        card.Controls.Add(new Label
        {
            Text = CardBody(note),
            Dock = DockStyle.Fill,
            ForeColor = TextMuted,
            AutoEllipsis = true,
            Padding = new Padding(0, 4, 0, 4)
        }, 0, 2);
        card.Controls.Add(new Label
        {
            Text = TagText(note),
            Dock = DockStyle.Fill,
            ForeColor = Accent,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 3);

        var menu = new ModernContextMenuStrip();
        menu.Items.Add(note.IsPinned ? "取消置顶" : "置顶", null, (_, _) => TogglePinned(note));
        menu.Items.Add("编辑", null, (_, _) => ShowEditor(note));
        menu.Items.Add("悬浮到桌面", null, (_, _) => FloatNote(note));
        menu.Items.Add(new ToolStripSeparator());
        var delete = menu.Items.Add("删除", null, (_, _) => Delete(note));
        delete.Tag = "danger";
        L.Apply(menu);
        frame.ContextMenuStrip = menu;
        frame.Controls.Add(card);
        if (note.IsPinned)
        {
            var pin = new Panel
            {
                Width = 24,
                Height = 25,
                BackColor = _editingId == note.Id ? AccentSoft : Surface,
                Location = new Point((frame.Width - 24) / 2, 1),
                Enabled = false
            };
            pin.Paint += (_, e) => DrawPin(e.Graphics);
            frame.Controls.Add(pin);
            pin.BringToFront();
        }
        HookCard(frame, () => ShowEditor(note), () => FloatNote(note), menu);
        return frame;
    }

    private void FloatNote(NoteItem note) => new FloatingNoteForm(note, _save, RefreshBoard).Show();

    private void ShowEditor(NoteItem? note = null)
    {
        if (_expandedComposer?.Visible == true && _editingId != note?.Id && !ConfirmPendingChanges()) return;
        _editingId = note?.Id;
        _title.Text = note?.Title ?? string.Empty;
        _content.Text = note?.Content ?? string.Empty;
        _tags.Text = note is null ? string.Empty : EffectiveTags(note);
        _pinned.Checked = note?.IsPinned ?? false;
        CaptureBaseline();
        if (_deleteButton is not null) _deleteButton.Visible = note is not null;
        if (_collapsedComposer is not null) _collapsedComposer.Visible = false;
        if (_expandedComposer is not null) _expandedComposer.Visible = true;
        _shell.RowStyles[1].Height = 284;
        RefreshBoard();
        _content.Focus();
    }

    private bool FinishEditor()
    {
        if (string.IsNullOrWhiteSpace(_title.Text) && string.IsNullOrWhiteSpace(_content.Text))
        {
            MessageBox.Show(L.T("至少写下一点内容。"), L.T("便签是空的"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        var note = _editingId is Guid id ? _notes.FirstOrDefault(x => x.Id == id) : null;
        if (note is null)
        {
            note = new NoteItem();
            _notes.Add(note);
        }
        note.Title = _title.Text.Trim();
        note.Content = _content.Text.Trim();
        note.Tags = NormalizeTags(_tags.Text);
        note.Kind = NoteKind.General;
        note.IsPinned = _pinned.Checked;
        note.UpdatedAt = DateTime.Now;
        _save();
        HideEditor();
        return true;
    }

    private void HideEditor()
    {
        _editingId = null;
        _title.Clear();
        _content.Clear();
        _tags.Clear();
        _pinned.Checked = false;
        if (_deleteButton is not null) _deleteButton.Visible = false;
        if (_expandedComposer is not null) _expandedComposer.Visible = false;
        if (_collapsedComposer is not null) _collapsedComposer.Visible = true;
        _shell.RowStyles[1].Height = 76;
        RefreshBoard();
    }

    public bool ConfirmCanLeave() => _expandedComposer?.Visible != true || ConfirmPendingChanges();

    private bool ConfirmPendingChanges()
    {
        if (!HasUnsavedChanges()) return true;
        var result = MessageBox.Show(
            L.T("这张便签有尚未保存的修改。是否保存后继续？"),
            L.T("保存便签修改"),
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);
        if (result == DialogResult.Cancel) return false;
        if (result == DialogResult.Yes) return FinishEditor();
        HideEditor();
        return true;
    }

    private bool HasUnsavedChanges() =>
        _title.Text != _baselineTitle
        || _content.Text != _baselineContent
        || _tags.Text != _baselineTags
        || _pinned.Checked != _baselinePinned;

    private void CaptureBaseline()
    {
        _baselineTitle = _title.Text;
        _baselineContent = _content.Text;
        _baselineTags = _tags.Text;
        _baselinePinned = _pinned.Checked;
    }

    private void EditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.Enter)
        {
            FinishEditor();
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            HideEditor();
            e.SuppressKeyPress = true;
        }
    }

    private void Delete(NoteItem note)
    {
        if (MessageBox.Show(L.T("删除这张便签？"), L.T("删除便签"), MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK) return;
        _notes.Remove(note);
        _save();
        HideEditor();
    }

    private void DeleteCurrent()
    {
        if (_editingId is Guid id && _notes.FirstOrDefault(x => x.Id == id) is NoteItem note) Delete(note);
    }

    private void TogglePinned(NoteItem note)
    {
        note.IsPinned = !note.IsPinned;
        note.UpdatedAt = DateTime.Now;
        _save();
        RefreshBoard();
    }

    private Control SearchField()
    {
        var field = BorderedPanel(Surface, new Padding(14, 9, 12, 8));
        field.Dock = DockStyle.None;
        field.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        field.Width = 300;
        field.Height = 42;
        field.Margin = new Padding(16, 8, 0, 0);
        _search.Dock = DockStyle.Fill;
        field.Controls.Add(_search);
        return field;
    }

    private static Panel BorderedPanel(Color backColor, Padding padding)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = backColor, Padding = padding };
        ModernUi.Round(panel, 10);
        panel.Paint += (_, e) => ModernUi.DrawBorder(e.Graphics, panel.ClientRectangle, 10, Border, 1.2F);
        return panel;
    }

    private static Button ActionButton(string text, bool primary, int width)
    {
        var button = new ModernButton
        {
            Text = text,
            Width = width,
            Height = 36,
            BackColor = primary ? Accent : Surface,
            ForeColor = primary ? Color.White : TextMuted,
            Margin = new Padding(0, 5, 8, 0),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderColor = primary ? Accent : Border;
        return button;
    }

    private void StylePinButton()
    {
        _pinned.BackColor = _pinned.Checked ? Accent : AccentSoft;
        _pinned.ForeColor = _pinned.Checked ? Color.White : Accent;
    }

    private static void DrawPin(Graphics graphics) => DrawPin(graphics, 0, 0, Accent);

    private static void DrawPin(Graphics graphics, int offsetX, int offsetY, Color color)
    {
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var pen = new Pen(color, 1.8F) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
        graphics.DrawArc(pen, offsetX + 8, offsetY + 3, 8, 7, 180, 180);
        graphics.DrawLine(pen, offsetX + 8, offsetY + 7, offsetX + 6, offsetY + 12);
        graphics.DrawLine(pen, offsetX + 16, offsetY + 7, offsetX + 18, offsetY + 12);
        graphics.DrawLine(pen, offsetX + 6, offsetY + 12, offsetX + 18, offsetY + 12);
        graphics.DrawLine(pen, offsetX + 12, offsetY + 12, offsetX + 12, offsetY + 21);
    }

    private static void HookClick(Control root, Action action)
    {
        root.Cursor = Cursors.IBeam;
        root.Click += (_, _) => action();
        foreach (Control child in root.Controls) HookClick(child, action);
    }

    private void HookCard(Control root, Action clickAction, Action doubleClickAction, ContextMenuStrip menu)
    {
        root.Cursor = Cursors.Hand;
        root.Click += (_, _) =>
        {
            _pendingCardClick = clickAction;
            _cardClickTimer.Stop();
            _cardClickTimer.Start();
        };
        root.DoubleClick += (_, _) =>
        {
            _cardClickTimer.Stop();
            _pendingCardClick = null;
            doubleClickAction();
        };
        root.ContextMenuStrip = menu;
        foreach (Control child in root.Controls) HookCard(child, clickAction, doubleClickAction, menu);
    }

    private static string CardTitle(NoteItem note) => string.IsNullOrWhiteSpace(note.Title) ? FirstLine(note.Content) : note.Title;

    private static string CardBody(NoteItem note)
    {
        if (!string.IsNullOrWhiteSpace(note.Title)) return note.Content;
        var lines = note.Content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return lines.Length > 1 ? string.Join(Environment.NewLine, lines.Skip(1)) : string.Empty;
    }

    private static string FirstLine(string text) => text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? L.T("未命名便签");

    private static string TagText(NoteItem note) => string.Join("   ", SplitTags(EffectiveTags(note)).Take(3).Select(x => $"#{x}"));

    private static string EffectiveTags(NoteItem note)
    {
        var tags = SplitTags(note.Tags).ToList();
        var legacy = note.Kind switch { NoteKind.Work => "工作", NoteKind.Study => "学习", _ => null };
        if (legacy is not null && !tags.Contains(legacy, StringComparer.OrdinalIgnoreCase)) tags.Insert(0, legacy);
        return string.Join(", ", tags);
    }

    private static IEnumerable<string> SplitTags(string text) => text.Split([',', '，', ';', '；'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    private static string NormalizeTags(string text) => string.Join(", ", SplitTags(text).Distinct(StringComparer.OrdinalIgnoreCase));
}
