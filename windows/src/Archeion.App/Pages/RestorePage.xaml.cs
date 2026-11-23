using Microsoft.UI.Xaml.Controls;
using Archeion_App.ViewModels;

namespace Archeion_App.Pages;

public sealed partial class RestorePage : Page
{
    public RestoreViewModel ViewModel { get; } = new();

    public RestorePage()
    {
        InitializeComponent();
        ListPlannedItems.ItemsSource = ViewModel.PlannedItems;
    }
}
