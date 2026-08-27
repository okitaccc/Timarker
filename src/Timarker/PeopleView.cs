using Timarker.Models;

namespace Timarker;

public sealed class PeopleView : UserControl
{
    private static Color AppBack => AppTheme.AppBack;
    private static Color TextMain => AppTheme.Text;
    private static Color TextMuted => AppTheme.Muted;
    private static Color Accent => UiTokens.Primary;
    private readonly List<PersonProfile> _people;
    private readonly List<EventItem> _events;
    private readonly List<ActivityRecord> _records;
    private readonly Action<EventItem> _editEvent;
    private readonly Action _save;
    private readonly ListBox _personList = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, ItemHeight = 72, DrawMode = DrawMode.OwnerDrawFixed };
    private readonly ListBox _eventList = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = EventCardRenderer.ItemHeight };
    private readonly Label _name = new() { AutoSize = true, Font = UiTokens.Font(UiTokens.TextPageTitle, FontStyle.Bold), ForeColor = TextMain };
    private readonly Label _meta = new() { AutoSize = true, ForeColor = TextMuted };
    private readonly Label _empty = new() { Text = "选择一个人物，查看与 TA 有关的生日、纪念日和事项。", AutoSize = true, ForeColor = TextMuted };

    public PeopleView(List<PersonProfile> people, List<EventItem> events, List<ActivityRecord> records, Action<EventItem> editEvent, Action save)
    {
        _people = people;
        _events = events;
        _records = records;
        _editEvent = editEvent;
        _save = save;
        Dock = DockStyle.Fill;
        BackColor = AppBack;
        Font = UiTokens.Font();
        BuildUi();
        RefreshView();
    }

    private void BuildUi()
    {
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(22), BackColor = AppBack };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var left = Card();
        left.Padding = new Padding(18);
        var leftLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        leftLayout.Controls.Add(Header("人物", "集中管理人物及相关时刻。"));
        _personList.DrawItem += DrawPerson;
        _personList.SelectedIndexChanged += (_, _) => ShowSelected();
        _personList.DoubleClick += (_, _) => EditPerson();
        leftLayout.Controls.Add(_personList, 0, 1);
        var add = Button("添加人物", true);
        add.Click += (_, _) => AddPerson();
        leftLayout.Controls.Add(add, 0, 2);
        left.Controls.Add(leftLayout);

        var right = Card();
        right.Padding = new Padding(24);
        var rightLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1 };
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var identity = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        identity.Controls.Add(_name);
        identity.Controls.Add(_meta);
        identity.Controls.Add(_empty);
        rightLayout.Controls.Add(identity);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        var link = Button("关联事项", true); link.Click += (_, _) => LinkEvents();
        var edit = Button("编辑资料", false); edit.Click += (_, _) => EditPerson();
        var delete = Button("删除人物", false); delete.Click += (_, _) => DeletePerson();
        actions.Controls.Add(link); actions.Controls.Add(edit); actions.Controls.Add(delete);
        rightLayout.Controls.Add(actions, 0, 1);
        rightLayout.Controls.Add(new Label { Text = "关联事项", Dock = DockStyle.Fill, Font = new Font(Font.FontFamily, 10F, FontStyle.Bold), ForeColor = TextMain, TextAlign = ContentAlignment.BottomLeft }, 0, 2);
        _eventList.DrawItem += DrawEvent;
        _eventList.DoubleClick += (_, _) => { if (_eventList.SelectedItem is EventItem item) _editEvent(item); };
        rightLayout.Controls.Add(_eventList, 0, 3);
        right.Controls.Add(rightLayout);

        shell.Controls.Add(left, 0, 0);
        shell.Controls.Add(right, 1, 0);
        Controls.Add(shell);
    }

    public void RefreshView()
    {
        var selectedId = (_personList.SelectedItem as PersonProfile)?.Id;
        _personList.Items.Clear();
        foreach (var person in _people.OrderBy(x => x.Name)) _personList.Items.Add(person);
        if (selectedId is not null)
        {
            for (var i = 0; i < _personList.Items.Count; i++)
                if (((PersonProfile)_personList.Items[i]).Id == selectedId) _personList.SelectedIndex = i;
        }
        if (_personList.SelectedIndex < 0 && _personList.Items.Count > 0) _personList.SelectedIndex = 0;
        ShowSelected();
    }

    private void ShowSelected()
    {
        var person = _personList.SelectedItem as PersonProfile;
        _name.Text = person?.Name ?? "还没有人物";
        var recordCount = person is null ? 0 : _records.Count(x => x.PersonIds.Contains(person.Id));
        _meta.Text = person is null ? "" : $"{person.Relationship.DefaultIfBlank("未填写关系")}  ·  {recordCount} 条共同记录";
        _empty.Visible = person is null;
        _eventList.Items.Clear();
        if (person is null) return;
        foreach (var item in _events.Where(x => x.PersonIds.Contains(person.Id)).OrderBy(x => x.NextDueAt(DateTime.Now) ?? DateTime.MaxValue))
            _eventList.Items.Add(item);
    }

    private void AddPerson()
    {
        var person = new PersonProfile();
        using var dialog = new PersonDialog(person, "添加人物");
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        _people.Add(person);
        _save();
        RefreshView();
        _personList.SelectedItem = person;
    }

    private void EditPerson()
    {
        if (_personList.SelectedItem is not PersonProfile person) return;
        using var dialog = new PersonDialog(person, "编辑人物");
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        _save();
        RefreshView();
    }

    private void DeletePerson()
    {
        if (_personList.SelectedItem is not PersonProfile person) return;
        if (MessageBox.Show($"删除人物“{person.Name}”？关联事项和记录会保留，只会解除关联。", "删除人物", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK) return;
        foreach (var item in _events) item.PersonIds.Remove(person.Id);
        foreach (var record in _records) record.PersonIds.Remove(person.Id);
        _people.Remove(person);
        _save();
        RefreshView();
    }

    private void LinkEvents()
    {
        if (_personList.SelectedItem is not PersonProfile person) return;
        var candidates = _events.OrderBy(x => x.Title).ToList();
        using var dialog = new EventLinkDialog(person, candidates);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        foreach (var item in candidates)
        {
            if (dialog.SelectedIds.Contains(item.Id))
            {
                if (!item.PersonIds.Contains(person.Id)) item.PersonIds.Add(person.Id);
            }
            else item.PersonIds.Remove(person.Id);
        }
        _save();
        ShowSelected();
    }

    private void DrawEvent(object? sender, DrawItemEventArgs e)
    {
        if (e.Index >= 0 && _eventList.Items[e.Index] is EventItem item)
            EventCardRenderer.Draw(e.Graphics, e.Bounds, item, Font, (e.State & DrawItemState.Selected) != 0, AppTheme.Surface, false);
    }

    private void DrawPerson(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || _personList.Items[e.Index] is not PersonProfile person) return;
        var selected = (e.State & DrawItemState.Selected) != 0;
        var card = Rectangle.Inflate(e.Bounds, -2, -4);
        using var background = new SolidBrush(selected ? AppTheme.Selected : AppTheme.SurfaceAlt);
        using var border = new Pen(selected ? Color.FromArgb(147, 197, 253) : AppTheme.Border);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using (var path = ModernUi.RoundedPath(card, 10))
        {
            e.Graphics.FillPath(background, path);
            e.Graphics.DrawPath(border, path);
        }
        using var nameFont = new Font(Font.FontFamily, 10F, FontStyle.Bold);
        TextRenderer.DrawText(e.Graphics, person.Name, nameFont, new Rectangle(card.Left + 12, card.Top + 9, card.Width - 24, 25), TextMain, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(e.Graphics, person.Relationship.DefaultIfBlank("未填写关系"), Font, new Rectangle(card.Left + 12, card.Top + 37, card.Width - 24, 23), TextMuted, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
    }

    private static Panel Card() => new() { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Margin = new Padding(7) };
    private static Control Header(string title, string subtitle)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        panel.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, Font = UiTokens.Font(UiTokens.TextPageTitle, FontStyle.Bold), ForeColor = TextMain });
        panel.Controls.Add(new Label { Text = subtitle, Dock = DockStyle.Fill, ForeColor = TextMuted }, 0, 1);
        return panel;
    }
    private static Button Button(string text, bool primary) => new ModernButton
    {
        Text = text, Height = 40, Width = primary ? 126 : 104, FlatStyle = FlatStyle.Flat,
        BackColor = primary ? Accent : UiTokens.Surface, ForeColor = primary ? Color.White : TextMain,
        Margin = new Padding(0, 5, 8, 5)
    };
}

internal sealed class PersonDialog : Form
{
    private readonly PersonProfile _person;
    private readonly ModernTextBox _name = new() { Dock = DockStyle.Fill };
    private readonly ModernTextBox _relationship = new() { Dock = DockStyle.Fill };
    private readonly ModernTextBox _notes = new() { Dock = DockStyle.Fill, Multiline = true, Height = 90 };
    private readonly ModernTextBox _tags = new() { Dock = DockStyle.Fill };

    public PersonDialog(PersonProfile person, string title)
    {
        _person = person; Text = title; Width = 500; Height = 430; FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterParent; Font = UiTokens.Font();
        var form = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), RowCount = 9, ColumnCount = 1, BackColor = UiTokens.Surface };
        Add(form, "姓名", _name); Add(form, "关系", _relationship); Add(form, "词条", _tags); Add(form, "备注", _notes);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        var save = new ModernButton { Text = "保存", Width = 100, Height = UiTokens.ControlHeight, BackColor = UiTokens.Primary, ForeColor = Color.White };
        var cancel = new ModernButton { Text = "取消", Width = 88, Height = 38 };
        save.Click += (_, _) => Save(); cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(save); buttons.Controls.Add(cancel); form.Controls.Add(buttons);
        _name.Text = person.Name; _relationship.Text = person.Relationship; _notes.Text = person.Notes; _tags.Text = person.Tags;
        Controls.Add(form); AcceptButton = save; CancelButton = cancel;
    }
    private static void Add(TableLayoutPanel form, string label, Control control)
    {
        form.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 7, 0, 4) }); form.Controls.Add(control);
    }
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_name.Text)) { MessageBox.Show("请填写姓名。"); return; }
        _person.Name = _name.Text.Trim(); _person.Relationship = _relationship.Text.Trim(); _person.Notes = _notes.Text.Trim(); _person.Tags = _tags.Text.Trim(); _person.UpdatedAt = DateTime.Now;
        DialogResult = DialogResult.OK;
    }
}

internal static class PersonTextExtensions
{
    public static string DefaultIfBlank(this string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;
}

internal sealed class EventLinkDialog : Form
{
    private readonly CheckedListBox _list = new() { Dock = DockStyle.Fill, CheckOnClick = true, BorderStyle = BorderStyle.None };
    public HashSet<Guid> SelectedIds { get; } = [];

    public EventLinkDialog(PersonProfile person, IReadOnlyList<EventItem> events)
    {
        Text = $"关联到 {person.Name}"; Width = 520; Height = 560; StartPosition = FormStartPosition.CenterParent;
        Font = UiTokens.Font(); BackColor = UiTokens.Surface; MinimumSize = new Size(440, 460);
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(22), BackColor = UiTokens.Surface };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 66)); shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        shell.Controls.Add(new Label { Text = "选择与这个人物有关的事项", Dock = DockStyle.Fill, Font = UiTokens.Font(UiTokens.TextSection, FontStyle.Bold), ForeColor = UiTokens.Text });
        foreach (var item in events) _list.Items.Add(item, item.PersonIds.Contains(person.Id));
        shell.Controls.Add(_list, 0, 1);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill }; var save = new ModernButton { Text = "保存关联", Width = 108, Height = UiTokens.ControlHeight, BackColor = UiTokens.Primary, ForeColor = Color.White }; var cancel = new ModernButton { Text = "取消", Width = 88, Height = UiTokens.ControlHeight };
        save.Click += (_, _) => { foreach (var value in _list.CheckedItems.OfType<EventItem>()) SelectedIds.Add(value.Id); DialogResult = DialogResult.OK; };
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel; buttons.Controls.Add(save); buttons.Controls.Add(cancel); shell.Controls.Add(buttons, 0, 2);
        Controls.Add(shell); AcceptButton = save; CancelButton = cancel;
    }
}
