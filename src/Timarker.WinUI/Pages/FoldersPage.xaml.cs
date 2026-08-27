using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Timarker.Models;
using Timarker_WinUI.Services;
namespace Timarker_WinUI.Pages;
public sealed partial class FoldersPage : Page
{
    private Folder? Selected => FolderList.SelectedItem as Folder;
    public FoldersPage(){InitializeComponent(); RefreshList();}
    private void RefreshList(){FolderList.ItemsSource=null; FolderList.ItemsSource=AppData.Current.Folders.ToList();}
    private void RefreshEvents()=>FolderEvents.ItemsSource=Selected is null?null:AppData.Current.Events.Where(x=>Selected.Contains(x.Id)||x.IsInFolder(Selected.Id)).ToList();
    private async void Add_Click(object s,RoutedEventArgs e){var b=new TextBox{PlaceholderText="例如：生日"};var d=new ContentDialog{XamlRoot=XamlRoot,Title="新建收藏夹",Content=b,PrimaryButtonText="创建",CloseButtonText="取消"};if(await d.ShowAsync()!=ContentDialogResult.Primary||string.IsNullOrWhiteSpace(b.Text))return;AppData.Current.Folders.Add(new Folder{Name=b.Text.Trim()});AppData.Current.Save();RefreshList();}
    private void Folder_Changed(object s,SelectionChangedEventArgs e){FolderEmpty.Visibility=Selected is null?Visibility.Visible:Visibility.Collapsed;FolderDetail.Visibility=Selected is null?Visibility.Collapsed:Visibility.Visible;RefreshEvents();}
    private async void AddEvent_Click(object s,RoutedEventArgs e){if(Selected is null)return;var list=new ListView{ItemsSource=AppData.Current.Events.Where(x=>!Selected.Contains(x.Id)).ToList(),DisplayMemberPath="Title",SelectionMode=ListViewSelectionMode.Multiple,MaxHeight=400};var d=new ContentDialog{XamlRoot=XamlRoot,Title="添加事项",Content=list,PrimaryButtonText="添加",CloseButtonText="取消"};if(await d.ShowAsync()!=ContentDialogResult.Primary)return;foreach(EventItem item in list.SelectedItems)Selected.Add(item.Id);AppData.Current.Save();RefreshEvents();}
    private async void Rename_Click(object s,RoutedEventArgs e){if(Selected is null)return;var b=new TextBox{Text=Selected.Name};var d=new ContentDialog{XamlRoot=XamlRoot,Title="重命名收藏夹",Content=b,PrimaryButtonText="保存",CloseButtonText="取消"};if(await d.ShowAsync()!=ContentDialogResult.Primary||string.IsNullOrWhiteSpace(b.Text))return;Selected.Name=b.Text.Trim();Selected.UpdatedAt=DateTime.Now;AppData.Current.Save();RefreshList();}
    private async void Delete_Click(object s,RoutedEventArgs e){if(Selected is null)return;var f=Selected;var d=new ContentDialog{XamlRoot=XamlRoot,Title="删除收藏夹？",Content="收藏夹里的事项不会被删除。",PrimaryButtonText="删除",CloseButtonText="取消",DefaultButton=ContentDialogButton.Close};if(await d.ShowAsync()!=ContentDialogResult.Primary)return;AppData.Current.Folders.Remove(f);AppData.Current.Events.ForEach(x=>x.RemoveFromFolder(f.Id));AppData.Current.Save();RefreshList();FolderDetail.Visibility=Visibility.Collapsed;}
}
