using Microsoft.UI.Xaml.Controls;
using Archeion_App.ViewModels;

namespace Archeion_App.Pages;

public sealed partial class SnapshotsPage : Page
{
    public SnapshotsViewModel ViewModel { get; } = new();

    public SnapshotsPage()
    {
        InitializeComponent();
        ListSnapshots.ItemsSource = ViewModel.Snapshots;
    }
}
