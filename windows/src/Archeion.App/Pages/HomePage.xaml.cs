using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Archeion_App.ViewModels;

namespace Archeion_App.Pages;

public sealed partial class HomePage : Page
{
    public OverviewViewModel ViewModel { get; } = new();

    public HomePage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await RefreshDataAsync();
    }

    private async void BtnRefresh_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await RefreshDataAsync();
    }

    private async System.Threading.Tasks.Task RefreshDataAsync()
    {
        await ViewModel.InitializeAsync();
        TxtRepoStatus.Text = ViewModel.RepositoryStatus;
        TxtDeviceSummary.Text = ViewModel.DeviceSummary;
        TxtStats.Text = ViewModel.StatsSummary;
    }
}
