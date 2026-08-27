using System.Collections.ObjectModel;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Timarker.Models;
using Timarker.Services;
using Timarker_WinUI.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Timarker_WinUI.Pages;

public sealed partial class HomePage : Page
{
    private readonly AppData _data = AppData.Current;
    public ObservableCollection<EventCardViewModel> VisibleEvents { get; } = [];

    public HomePage()
    {
        InitializeComponent();
        RefreshEvents();
    }

    private void RefreshEvents()
    {
        var query = _data.Events.AsEnumerable();
        var selectedTag = (FilterBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        if (selectedTag == "pending") query = query.Where(x => x.Status is not (EventStatus.Done or EventStatus.Cancelled));
        if (selectedTag == "done") query = query.Where(x => x.Status == EventStatus.Done);
        var search = SearchBox.Text.Trim();
        if (search.Length > 0) query = query.Where(x => x.Title.Contains(search, StringComparison.CurrentCultureIgnoreCase) || x.Tags.Contains(search, StringComparison.CurrentCultureIgnoreCase));

        var now = DateTime.Now;
        var cards = query.Select(x => new { Item = x, Due = x.NextDueAt(now) })
            .OrderBy(x => x.Due ?? DateTime.MaxValue).Take(100)
            .Select(x => EventCardViewModel.From(x.Item, x.Due)).ToList();
        VisibleEvents.Clear();
        foreach (var card in cards) VisibleEvents.Add(card);
        CountText.Text = cards.Count == 0 ? "暂时没有事项" : $"{cards.Count} 个事项";
    }

    private void Filter_Changed(object sender, SelectionChangedEventArgs e) => RefreshEvents();
    private void Search_Click(object sender, RoutedEventArgs e) => RefreshEvents();
    private void Search_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => RefreshEvents();
    private void Search_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) { if (string.IsNullOrWhiteSpace(sender.Text)) RefreshEvents(); }
    private async void NewEvent_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new EventEditorDialog { XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        _data.Events.Add(dialog.Result);
        _data.Save();
        QuickTitleBox.Text = "";
        RefreshEvents();
    }
    private async void Event_Click(object sender, ItemClickEventArgs e) { if (e.ClickedItem is EventCardViewModel card) await Edit(card); }
    private async void EditEvent_Click(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.DataContext is EventCardViewModel card) await Edit(card); }
    private async Task Edit(EventCardViewModel card)
    {
        var item = _data.Events.First(x => x.Id == card.Id);
        var dialog = new EventEditorDialog(item) { XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        _data.Save(); RefreshEvents();
    }
    private void CompleteEvent_Click(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.DataContext is not EventCardViewModel card) return; _data.Events.First(x => x.Id == card.Id).Complete(); _data.Save(); RefreshEvents(); }
    private async void AddToFolder_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not EventCardViewModel card) return;
        var box = new ComboBox { ItemsSource = _data.Folders, DisplayMemberPath = "Name", HorizontalAlignment = HorizontalAlignment.Stretch };
        var dialog = new ContentDialog { XamlRoot = XamlRoot, Title = "加入收藏夹", Content = box, PrimaryButtonText = "加入", CloseButtonText = "取消" };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary || box.SelectedItem is not Folder folder) return;
        folder.Add(card.Id); _data.Events.First(x => x.Id == card.Id).AddToFolder(folder.Id); _data.Save();
    }
    private async void DeleteEvent_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not EventCardViewModel card) return;
        var item = _data.Events.First(x => x.Id == card.Id);
        var dialog = new ContentDialog { XamlRoot = XamlRoot, Title = "删除事项？", Content = item.Title, PrimaryButtonText = "删除", CloseButtonText = "取消", DefaultButton = ContentDialogButton.Close };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        _data.Events.Remove(item); _data.Folders.ForEach(x => x.Remove(item.Id)); _data.Save(); RefreshEvents();
    }
}

public sealed class EventCardViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Meta { get; set; } = "";
    public IReadOnlyList<string> Tags { get; set; } = [];
    public Brush AccentBrush { get; set; } = new SolidColorBrush(Colors.RoyalBlue);

    public static EventCardViewModel From(EventItem item, DateTime? due)
    {
        var type = item.Type switch { EventType.Birthday => "生日", EventType.Anniversary => "纪念日", EventType.Recurring or EventType.Habit => "周期", _ => "事项" };
        var when = due is null ? "未设置时间" : due.Value.ToString(due.Value.Date == DateTime.Today ? "今天 HH:mm" : "M月d日 HH:mm");
        var color = item.Type switch { EventType.Birthday => Colors.DeepPink, EventType.Anniversary => Colors.Orange, EventType.Recurring or EventType.Habit => Colors.MediumSeaGreen, _ => Colors.RoyalBlue };
        return new EventCardViewModel
        {
            Id = item.Id,
            Title = string.IsNullOrWhiteSpace(item.Title) ? "未命名事项" : item.Title,
            Meta = $"{when}  ·  {type}",
            Tags = item.Tags.Split([',', '，', '#'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(x => $"#{x}").Take(4).ToArray(),
            AccentBrush = new SolidColorBrush(color)
        };
    }
}
