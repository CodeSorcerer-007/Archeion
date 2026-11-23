using Microsoft.UI.Xaml.Controls;
using Archeion_App.ViewModels;

namespace Archeion_App.Pages;

public sealed partial class ExplorerPage : Page
{
    public ExplorerViewModel ViewModel { get; } = new();

    public ExplorerPage()
    {
        InitializeComponent();
        ListItems.ItemsSource = ViewModel.Items;
    }

    private void BtnSearch_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.SearchFilter = TxtSearch.Text;
        TxtStatus.Text = "Please select an active snapshot from the Snapshots tab first.";
    }
}
