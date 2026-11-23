using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Archeion.Contracts;
using Archeion.DeviceDiscovery;
using Archeion_App.ViewModels;

namespace Archeion_App.Pages;

public sealed partial class DevicesPage : Page
{
    public DevicesViewModel ViewModel { get; } = new();

    public DevicesPage()
    {
        InitializeComponent();
        ListEndpoints.ItemsSource = ViewModel.Endpoints;
        ListCapabilities.ItemsSource = ViewModel.CapabilitiesList;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.ScanEndpointsAsync();
        TxtStatus.Text = ViewModel.StatusMessage;
    }

    private async void BtnScan_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.ScanEndpointsAsync();
        TxtStatus.Text = ViewModel.StatusMessage;
    }

    private void ListEndpoints_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListEndpoints.SelectedItem is DiscoveredEndpointResult endpoint)
        {
            ViewModel.SelectedEndpoint = endpoint;
        }
    }
}
