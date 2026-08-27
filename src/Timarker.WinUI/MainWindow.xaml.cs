using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Timarker_WinUI.Pages;
using Microsoft.UI.Xaml.Media.Imaging;
using Timarker_WinUI.Services;
using Microsoft.UI.Dispatching;
using Timarker.Models;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Timarker_WinUI;

public sealed partial class MainWindow : Window
{
    private readonly DispatcherQueueTimer _reminderTimer;
    private EventItem? _reminding;
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.SetIcon("Assets/AppIcon.ico");
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1280, 820));
        NavFrame.Navigate(typeof(HomePage));
        ApplyAppearance();
        ApplyFeatures();
        _reminderTimer = DispatcherQueue.CreateTimer();
        _reminderTimer.Interval = TimeSpan.FromSeconds(20);
        _reminderTimer.Tick += (_, _) => CheckReminders();
        _reminderTimer.Start();
        CheckReminders();
    }

    private void CheckReminders()
    {
        if (ReminderBar.IsOpen) return;
        var data = AppData.Current;
        var now = DateTime.Now;
        var settings = data.Settings;
        if (settings.QuietHoursEnabled && InQuietHours(now.TimeOfDay, settings.QuietHoursStart, settings.QuietHoursEnd)) return;
        _reminding = data.Events.FirstOrDefault(x => x.IsDue(now));
        if (_reminding is null) return;
        ReminderText.Text = $"{_reminding.Title}\n{_reminding.NextDueAt(now):yyyy-MM-dd HH:mm}";
        _reminding.MarkReminded(now);
        data.Save();
        ReminderBar.IsOpen = true;
    }
    private static bool InQuietHours(TimeSpan now, TimeSpan start, TimeSpan end) => start <= end ? now >= start && now < end : now >= start || now < end;
    private void SnoozeReminder_Click(object sender, RoutedEventArgs e) { if (_reminding is not null) { _reminding.SnoozedUntil = DateTime.Now.AddMinutes(AppData.Current.Settings.DefaultSnoozeMinutes); AppData.Current.Save(); } ReminderBar.IsOpen = false; }
    private void CompleteReminder_Click(object sender, RoutedEventArgs e) { if (_reminding is not null) { _reminding.Complete(); AppData.Current.Save(); } ReminderBar.IsOpen = false; }

    public void ApplyAppearance()
    {
        var settings = AppData.Current.Settings;
        if (Content is FrameworkElement root)
            root.RequestedTheme = settings.Theme switch { "light" => ElementTheme.Light, "dark" => ElementTheme.Dark, _ => ElementTheme.Default };
        if (File.Exists(settings.BackgroundImagePath))
        {
            BackgroundImage.Source = new BitmapImage(new Uri(settings.BackgroundImagePath));
            BackgroundImage.Opacity = settings.BackgroundOpacity / 100d;
            BackgroundImage.Visibility = Visibility.Visible;
        }
        else BackgroundImage.Visibility = Visibility.Collapsed;
    }
    public void ApplyFeatures()
    {
        var disabled = AppData.Current.Settings.DisabledFeatures;
        foreach (var item in NavView.MenuItems.OfType<NavigationViewItem>())
        {
            var key = item.Tag?.ToString();
            if (key is not null && key != "home" && key != "calendar")
                item.Visibility = disabled.Contains(key, StringComparer.OrdinalIgnoreCase) ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        NavFrame.GoBack();
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            NavFrame.Navigate(typeof(SettingsPage));
        }
        else if (args.SelectedItem is NavigationViewItem item)
        {
            switch (item.Tag)
            {
                case "home":
                    NavFrame.Navigate(typeof(HomePage));
                    break;
                case "calendar":
                    NavFrame.Navigate(typeof(CalendarPage));
                    break;
                case "projects": NavFrame.Navigate(typeof(ProjectsPage)); break;
                case "folders": NavFrame.Navigate(typeof(FoldersPage)); break;
                case "records": NavFrame.Navigate(typeof(RecordsPage)); break;
                case "notes": NavFrame.Navigate(typeof(NotesPage)); break;
                case "pomodoro": NavFrame.Navigate(typeof(PomodoroPage)); break;
                default:
                    NavFrame.Navigate(typeof(AboutPage), item.Content?.ToString());
                    break;
            }
        }
    }
}
