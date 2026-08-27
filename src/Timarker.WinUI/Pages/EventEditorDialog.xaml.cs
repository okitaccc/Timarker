using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Timarker.Models;

namespace Timarker_WinUI.Pages;

public sealed partial class EventEditorDialog : ContentDialog
{
    private readonly EventItem _item;
    public EventItem Result => _item;

    public EventEditorDialog(EventItem? item = null)
    {
        InitializeComponent();
        _item = item ?? new EventItem { StartAt = DateTime.Now.AddHours(1) };
        Title = item is null ? "新建事项" : "编辑事项";
        TitleBox.Text = _item.Title;
        TagsBox.Text = _item.Tags;
        NotesBox.Text = _item.Notes;
        DateBox.Date = (_item.NextDueAt(DateTime.Now) ?? DateTime.Now).Date;
        TimeBox.Time = (_item.NextDueAt(DateTime.Now) ?? DateTime.Now).TimeOfDay;
        SelectByTag(TypeBox, _item.Type.ToString());
        SelectByTag(PriorityBox, _item.Priority.ToString());
        LeadBox.Value = _item.ReminderLeadMinutes;
        RepeatReminderBox.Value = _item.ReminderRepeatMinutes;
        RepeatPanel.Visibility = _item.Type == EventType.Recurring ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void SelectByTag(ComboBox box, string value)
    {
        box.SelectedItem = box.Items.OfType<ComboBoxItem>().FirstOrDefault(x => string.Equals(x.Tag?.ToString(), value, StringComparison.OrdinalIgnoreCase)) ?? box.Items[0];
    }

    private void Type_Changed(object sender, SelectionChangedEventArgs e) => RepeatPanel.Visibility = SelectedType() == EventType.Recurring ? Visibility.Visible : Visibility.Collapsed;

    private EventType SelectedType() => Enum.Parse<EventType>(((ComboBoxItem)TypeBox.SelectedItem).Tag!.ToString()!);

    private void Save_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text))
        {
            args.Cancel = true;
            ErrorBar.Message = "请填写事项名称。";
            ErrorBar.IsOpen = true;
            return;
        }

        var at = DateBox.Date.Date + TimeBox.Time;
        _item.Title = TitleBox.Text.Trim();
        _item.Type = SelectedType();
        _item.Priority = Enum.Parse<EventPriority>(((ComboBoxItem)PriorityBox.SelectedItem).Tag!.ToString()!);
        _item.Tags = TagsBox.Text.Trim();
        _item.Notes = NotesBox.Text.Trim();
        _item.ReminderLeadMinutes = (int)LeadBox.Value;
        _item.ReminderRepeatMinutes = (int)RepeatReminderBox.Value;
        _item.UpdatedAt = DateTime.Now;
        _item.StartAt = _item.Type == EventType.Deadline ? null : at;
        _item.DeadlineAt = _item.Type == EventType.Deadline ? at : null;
        if (_item.Type == EventType.Recurring)
        {
            _item.RepeatEvery = (int)RepeatEveryBox.Value;
            _item.RepeatUnit = Enum.Parse<RepeatUnit>(((ComboBoxItem)RepeatUnitBox.SelectedItem).Tag!.ToString()!);
            _item.RepeatPattern = RepeatPattern.Interval;
        }
        if (_item.Type == EventType.Birthday) { _item.BirthdayMonth = at.Month; _item.BirthdayDay = at.Day; _item.Tags = EnsureTag(_item.Tags, "生日"); }
        if (_item.Type == EventType.Anniversary) _item.Tags = EnsureTag(_item.Tags, "纪念日");
    }

    private static string EnsureTag(string tags, string tag) => tags.Split([',', '，'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Contains(tag) ? tags : string.IsNullOrWhiteSpace(tags) ? tag : $"{tags}, {tag}";
}
