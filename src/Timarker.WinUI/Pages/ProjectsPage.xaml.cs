using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Timarker.Models;
using Timarker_WinUI.Services;

namespace Timarker_WinUI.Pages;

public sealed partial class ProjectsPage : Page
{
    private Project? Selected => ProjectList.SelectedItem as Project;
    public ProjectsPage() { InitializeComponent(); Refresh(); }
    private void Refresh() => ProjectList.ItemsSource = null; private void RefreshList() => ProjectList.ItemsSource = AppData.Current.Projects.ToList();
    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e) { base.OnNavigatedTo(e); RefreshList(); }
    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        var box = new TextBox { PlaceholderText = "例如：完成毕业论文" };
        var dialog = new ContentDialog { XamlRoot = XamlRoot, Title = "新建项目", Content = box, PrimaryButtonText = "创建", CloseButtonText = "取消", DefaultButton = ContentDialogButton.Primary };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(box.Text)) return;
        AppData.Current.Projects.Add(new Project { Name = box.Text.Trim() }); AppData.Current.Save(); RefreshList();
    }
    private void Project_Changed(object sender, SelectionChangedEventArgs e)
    {
        EmptyPanel.Visibility = Selected is null ? Visibility.Visible : Visibility.Collapsed; DetailPanel.Visibility = Selected is null ? Visibility.Collapsed : Visibility.Visible;
        if (Selected is null) return; ProjectNameBox.Text = Selected.Name; ProjectNotesBox.Text = Selected.Notes; RefreshSteps();
    }
    private void RefreshSteps() => StepList.ItemsSource = Selected?.Steps.OrderBy(x => x.Order).Select(x => AppData.Current.Events.FirstOrDefault(e => e.Id == x.EventId)?.Title ?? "已删除的事项").ToList();
    private async void AddStep_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is null) return; var candidates = AppData.Current.Events.Where(x => !Selected.Steps.Any(s => s.EventId == x.Id)).ToList();
        var box = new ComboBox { ItemsSource = candidates, DisplayMemberPath = "Title", HorizontalAlignment = HorizontalAlignment.Stretch };
        var dialog = new ContentDialog { XamlRoot = XamlRoot, Title = "从事项中添加步骤", Content = box, PrimaryButtonText = "添加", CloseButtonText = "取消" };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary || box.SelectedItem is not EventItem item) return;
        Selected.Steps.Add(new ProjectStep { EventId = item.Id, Order = Selected.Steps.Count + 1 }); item.ProjectId = Selected.Id; AppData.Current.Save(); RefreshSteps();
    }
    private void Save_Click(object sender, RoutedEventArgs e) { if (Selected is null || string.IsNullOrWhiteSpace(ProjectNameBox.Text)) return; Selected.Name = ProjectNameBox.Text.Trim(); Selected.Notes = ProjectNotesBox.Text.Trim(); Selected.UpdatedAt = DateTime.Now; AppData.Current.Save(); RefreshList(); }
    private async void Delete_Click(object sender, RoutedEventArgs e) { if (Selected is null) return; var project = Selected; var d = new ContentDialog { XamlRoot = XamlRoot, Title = "删除项目？", Content = "项目中的事项不会被删除。", PrimaryButtonText = "删除", CloseButtonText = "取消", DefaultButton = ContentDialogButton.Close }; if (await d.ShowAsync() != ContentDialogResult.Primary) return; AppData.Current.Projects.Remove(project); AppData.Current.Events.Where(x => x.ProjectId == project.Id).ToList().ForEach(x => x.ProjectId = null); AppData.Current.Save(); RefreshList(); DetailPanel.Visibility = Visibility.Collapsed; }
}
