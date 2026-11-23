using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Archeion_App.ViewModels;

namespace Archeion_App.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; } = new();

    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        TxtRepoPath.Text = ViewModel.RepositoryPath;
    }

    private void BtnGenKey_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.GenerateNewEmergencyRecoveryKey();
        TxtEmergencyKey.Text = ViewModel.EmergencyKeyDisplay;
    }
}
