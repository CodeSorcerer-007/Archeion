using Microsoft.UI.Xaml.Controls;
using Archeion_App.ViewModels;

namespace Archeion_App.Pages;

public sealed partial class BackupPage : Page
{
    public BackupViewModel ViewModel { get; } = new();

    public BackupPage()
    {
        InitializeComponent();
    }

    private void BtnStartBackup_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.BackupPhotos = ChkPhotos.IsChecked ?? true;
        ViewModel.BackupVideos = ChkVideos.IsChecked ?? true;
        ViewModel.BackupAudio = ChkAudio.IsChecked ?? true;
        ViewModel.BackupContacts = ChkContacts.IsChecked ?? true;
        ViewModel.BackupCalendar = ChkCalendar.IsChecked ?? true;
        ViewModel.BackupAppInventory = ChkApps.IsChecked ?? true;

        TxtStatus.Text = "Please connect an Android device endpoint from the Devices tab first.";
    }

    private void BtnCancel_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.Cancel();
        TxtStatus.Text = "Backup cancellation requested.";
    }
}
