using Microsoft.UI.Xaml.Controls;
using Archeion_App.ViewModels;

namespace Archeion_App.Pages;

public sealed partial class DiagnosticsPage : Page
{
    public DiagnosticsViewModel ViewModel { get; } = new();

    public DiagnosticsPage()
    {
        InitializeComponent();
        ListDiscrepancies.ItemsSource = ViewModel.Discrepancies;
    }

    private async void BtnRunScan_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.RunScanAsync();
        TxtStatus.Text = ViewModel.StatusMessage;
    }

    private async void BtnCleanupStaging_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.CleanupStagingAsync();
        TxtStatus.Text = ViewModel.StatusMessage;
    }
}
