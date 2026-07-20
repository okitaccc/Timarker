using Timarker.Models;

namespace Timarker;

public sealed class FolderViewForm : UserControl
{
    private readonly IReadOnlyList<EventItem> _events;
    private readonly Action<EventItem> _edit;
    private readonly Action<EventItem> _delete;
    private readonly Action _createFolder;
    private readonly Action _save;
    private readonly ListBox _folders = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        DisplayMember = nameof(EventItem.Title),
        DrawMode = DrawMode.OwnerDrawFixed,
        ItemHeight = 64,
        IntegralHeight = false,
        BackColor = Color.White
    };
    private readonly ListBox _items = EventList();
    private readonly ListBox _available = EventList(SelectionMode.MultiExtended);
    private readonly ComboBox _tagFilter = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _title = new() { Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold) };

    public FolderViewForm(IReadOnlyList<EventItem> events, Action<EventItem> edit, Action<EventItem> delete, Action createFolder, Action save)
    {
        _events = events;
        _edit = edit;
        _delete = delete;
        _createFolder = createFolder;
        _save = save;
        Dock = DockStyle.Fill;
        MinimumSize = new Size(680, 460);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = Color.FromArgb(246, 247, 251);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(18), BackColor = BackColor };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));

        var left = Card();
        left.RowCount = 3;
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        left.Controls.Add(new Label { Text = "收藏夹", Dock = DockStyle.Fill, Font = new Font(Font, FontStyle.Bold) });
        left.Controls.Add(_folders);
        var add = new Button { Text = "新建收藏夹", Dock = DockStyle.Fill, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        add.FlatAppearance.BorderSize = 0;
        add.Click += (_, _) => { _createFolder(); RefreshFolders(); };
        left.Controls.Add(add);

        var right = Card();
        right.Margin = new Padding(14, 0, 0, 0);
        right.RowCount = 2;
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.Controls.Add(_title);
        right.Controls.Add(_items);

        var available = Card();
        available.Margin = new Padding(14, 0, 0, 0);
        available.RowCount = 4;
        available.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        available.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        available.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        available.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        available.Controls.Add(new Label { Text = "从全部事件添加", Dock = DockStyle.Fill, Font = new Font(Font, FontStyle.Bold) });
        available.Controls.Add(_tagFilter);
        available.Controls.Add(_available);
        var addSelected = new Button { Text = "添加选中的事件", Dock = DockStyle.Fill, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        addSelected.FlatAppearance.BorderSize = 0;
        addSelected.Click += (_, _) => AddSelectedEvents();
        available.Controls.Add(addSelected);

        root.Controls.Add(left, 0, 0);
        root.Controls.Add(right, 1, 0);
        root.Controls.Add(available, 2, 0);
        Controls.Add(root);

        _folders.SelectedIndexChanged += (_, _) => RefreshItems();
        _folders.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                _folders.SelectedIndex = _folders.IndexFromPoint(e.Location);
            }
        };
        _tagFilter.SelectedIndexChanged += (_, _) => RefreshAvailable();
        _folders.DrawItem += DrawFolder;
        _items.DrawItem += DrawEvent;
        _available.DrawItem += DrawEvent;
        _available.DoubleClick += (_, _) => AddSelectedEvents();
        _items.DoubleClick += (_, _) =>
        {
            if (_items.SelectedItem is EventItem item)
            {
                _edit(item);
                RefreshItems();
            }
        };
        var menu = new ContextMenuStrip();
        menu.Items.Add("编辑", null, (_, _) => { if (_items.SelectedItem is EventItem item) _edit(item); });
        menu.Items.Add("从收藏夹移除", null, (_, _) =>
        {
            if (_items.SelectedItem is EventItem item)
            {
                if (_folders.SelectedItem is EventItem folder)
                {
                    item.RemoveFromFolder(folder.Id);
                }
                _save();
                RefreshItems();
            }
        });
        _items.ContextMenuStrip = menu;
        var folderMenu = new ContextMenuStrip();
        folderMenu.Items.Add("重命名", null, (_, _) => RenameSelectedFolder());
        folderMenu.Items.Add(new ToolStripSeparator());
        var deleteFolder = folderMenu.Items.Add("删除收藏夹", null, (_, _) => DeleteSelectedFolder());
        deleteFolder.ForeColor = Color.FromArgb(220, 38, 38);
        _folders.ContextMenuStrip = folderMenu;
        RefreshTagFilter();
        RefreshFolders();
        L.Apply(menu);
        L.Apply(folderMenu);
        L.Apply(this);
    }

    private static TableLayoutPanel Card() => new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 1,
        Padding = new Padding(16),
        BackColor = Color.White
    };

    private static ListBox EventList(SelectionMode selectionMode = SelectionMode.One) => new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        DrawMode = DrawMode.OwnerDrawFixed,
        ItemHeight = EventCardRenderer.ItemHeight,
        IntegralHeight = false,
        SelectionMode = selectionMode
    };

    private void DrawEvent(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || sender is not ListBox list || list.Items[e.Index] is not EventItem item)
        {
            return;
        }

        EventCardRenderer.Draw(e.Graphics, e.Bounds, item, Font, (e.State & DrawItemState.Selected) != 0, Color.White);
        e.DrawFocusRectangle();
    }

    private void DrawFolder(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || sender is not ListBox list || list.Items[e.Index] is not EventItem folder) return;

        e.Graphics.FillRectangle(Brushes.White, e.Bounds);
        var selected = (e.State & DrawItemState.Selected) != 0;
        var card = Rectangle.Inflate(e.Bounds, -4, -5);
        using var background = new SolidBrush(selected ? Color.FromArgb(239, 246, 255) : Color.FromArgb(248, 250, 252));
        using var border = new Pen(selected ? Color.FromArgb(37, 99, 235) : Color.FromArgb(226, 232, 240));
        e.Graphics.FillRectangle(background, card);
        e.Graphics.DrawRectangle(border, card);

        var count = _events.Count(x => !x.IsGroup && x.IsInFolder(folder.Id));
        using var titleFont = new Font(Font.FontFamily, 10.5F, FontStyle.Bold);
        using var detailFont = new Font(Font.FontFamily, 8F);
        TextRenderer.DrawText(e.Graphics, folder.Title, titleFont,
            new Rectangle(card.Left + 13, card.Top + 8, card.Width - 26, 24), Color.FromArgb(15, 23, 42),
            TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(e.Graphics, $"{count} 个事件", detailFont,
            new Rectangle(card.Left + 13, card.Top + 34, card.Width - 26, 18), Color.FromArgb(100, 116, 139),
            TextFormatFlags.VerticalCenter);
        e.DrawFocusRectangle();
    }

    private void RefreshFolders()
    {
        var selectedId = (_folders.SelectedItem as EventItem)?.Id;
        _folders.Items.Clear();
        foreach (var folder in _events.Where(x => x.IsGroup).OrderBy(x => x.Title))
        {
            _folders.Items.Add(folder);
        }
        if (selectedId is not null)
        {
            _folders.SelectedItem = _folders.Items.Cast<EventItem>().FirstOrDefault(x => x.Id == selectedId);
        }
        if (_folders.SelectedIndex < 0 && _folders.Items.Count > 0)
        {
            _folders.SelectedIndex = 0;
        }
        RefreshItems();
    }

    private void RefreshItems()
    {
        _items.Items.Clear();
        if (_folders.SelectedItem is not EventItem folder)
        {
            _title.Text = "选择一个收藏夹";
            return;
        }
        var children = _events.Where(x => !x.IsGroup && x.IsInFolder(folder.Id)).OrderBy(x => x.Title).ToList();
        _title.Text = $"{folder.Title} · {children.Count} 个事件";
        foreach (var item in children)
        {
            _items.Items.Add(item);
        }
        RefreshAvailable();
    }

    private void RefreshTagFilter()
    {
        _tagFilter.Items.Clear();
        _tagFilter.Items.Add("全部词条");
        foreach (var tag in _events.Where(x => !x.IsGroup).SelectMany(x => SplitTags(x.Tags))
                     .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x))
        {
            _tagFilter.Items.Add(tag);
        }
        _tagFilter.SelectedIndex = 0;
    }

    private void RefreshAvailable()
    {
        _available.Items.Clear();
        if (_folders.SelectedItem is not EventItem folder)
        {
            return;
        }

        var tag = _tagFilter.SelectedIndex > 0 ? _tagFilter.SelectedItem?.ToString() : null;
        foreach (var item in _events.Where(x => !x.IsGroup && !x.IsInFolder(folder.Id))
                     .Where(x => tag is null || SplitTags(x.Tags).Contains(tag, StringComparer.OrdinalIgnoreCase))
                     .OrderBy(x => x.Title))
        {
            _available.Items.Add(item);
        }
    }

    private void AddSelectedEvents()
    {
        if (_folders.SelectedItem is not EventItem folder || _available.SelectedItems.Count == 0)
        {
            return;
        }

        foreach (var item in _available.SelectedItems.Cast<EventItem>().ToList())
        {
            item.AddToFolder(folder.Id);
        }
        _save();
        RefreshItems();
    }

    private void RenameSelectedFolder()
    {
        if (_folders.SelectedItem is not EventItem folder)
        {
            return;
        }

        using var dialog = new Form { Text = "重命名收藏夹", Width = 380, Height = 160, StartPosition = FormStartPosition.CenterParent, Font = Font };
        var input = new TextBox { Text = folder.Title, Dock = DockStyle.Top };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        var ok = new Button { Text = "保存", DialogResult = DialogResult.OK, Width = 84, Height = 34 };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 84, Height = 34 };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        dialog.Controls.Add(input);
        dialog.Controls.Add(buttons);
        dialog.Padding = new Padding(18);
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;
        dialog.Shown += (_, _) => input.SelectAll();
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK || string.IsNullOrWhiteSpace(input.Text))
        {
            return;
        }

        folder.Title = input.Text.Trim();
        folder.UpdatedAt = DateTime.Now;
        _save();
        RefreshFolders();
    }

    private void DeleteSelectedFolder()
    {
        if (_folders.SelectedItem is not EventItem folder) return;
        _delete(folder);
        RefreshFolders();
    }

    private static IEnumerable<string> SplitTags(string text) => text.Split(
        new[] { ',', '，', ';', '；', ' ' },
        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
