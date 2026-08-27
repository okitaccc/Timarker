using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Timarker;
using Timarker_WinUI.Services;

namespace Timarker_WinUI.Pages;

public sealed partial class CalendarPage : Page
{
    private DateTime _month = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    public string[] Weekdays { get; } = ["周一", "周二", "周三", "周四", "周五", "周六", "周日"];
    public ObservableCollection<CalendarDayViewModel> Days { get; } = [];
    public CalendarPage() { InitializeComponent(); Refresh(); }

    private void Refresh()
    {
        MonthButton.Content = $"{_month:yyyy年 M月}";
        var offset = ((int)_month.DayOfWeek + 6) % 7;
        var first = _month.AddDays(-offset);
        var data = AppData.Current.Events;
        Days.Clear();
        for (var i = 0; i < 42; i++)
        {
            var date = first.AddDays(i);
            var count = data.Count(x => x.NextDueAt(date)?.Date == date.Date);
            Days.Add(new CalendarDayViewModel(date, date.Month == _month.Month, LunarSwitch?.IsOn != false, count));
        }
    }
    private void Previous_Click(object sender, RoutedEventArgs e) { _month = _month.AddMonths(-1); Refresh(); }
    private void Next_Click(object sender, RoutedEventArgs e) { _month = _month.AddMonths(1); Refresh(); }
    private void Today_Click(object sender, RoutedEventArgs e) { _month = new(DateTime.Today.Year, DateTime.Today.Month, 1); Refresh(); }
    private void Lunar_Toggled(object sender, RoutedEventArgs e) => Refresh();
    private async void Month_Click(object sender, RoutedEventArgs e)
    {
        var picker = new CalendarDatePicker { Date = _month, DateFormat = "{year.full}年 {month.integer}月", PlaceholderText = "选择年月" };
        var dialog = new ContentDialog { XamlRoot = XamlRoot, Title = "跳转到月份", Content = picker, PrimaryButtonText = "跳转", CloseButtonText = "取消", DefaultButton = ContentDialogButton.Primary };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary && picker.Date is { } date) { _month = new(date.Year, date.Month, 1); Refresh(); }
    }
    private async void Day_Click(object sender, ItemClickEventArgs e)
    {
        var day = (CalendarDayViewModel)e.ClickedItem;
        var items = AppData.Current.Events.Where(x => x.NextDueAt(day.Date)?.Date == day.Date).Select(x => x.Title).ToArray();
        await new ContentDialog { XamlRoot = XamlRoot, Title = $"{day.Date:M月d日}", Content = items.Length == 0 ? "当天没有事项。" : string.Join("\n", items), PrimaryButtonText = "知道了" }.ShowAsync();
    }
}

public sealed class CalendarDayViewModel(DateTime date, bool inMonth, bool showLunar, int count)
{
    public DateTime Date { get; } = date;
    public string Day { get; } = date.Day.ToString();
    public string Lunar { get; } = showLunar ? LunarDate.Text(date) : "";
    public string Summary { get; } = count == 0 ? "" : $"{count} 项";
    public Visibility EventVisibility { get; } = count == 0 ? Visibility.Collapsed : Visibility.Visible;
    public Brush BorderBrush { get; } = new SolidColorBrush(inMonth ? Microsoft.UI.Colors.Transparent : Microsoft.UI.Colors.Gray);
}
