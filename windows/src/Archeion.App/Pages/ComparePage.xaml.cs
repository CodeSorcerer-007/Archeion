using Microsoft.UI.Xaml.Controls;
using Archeion_App.ViewModels;

namespace Archeion_App.Pages;

public sealed partial class ComparePage : Page
{
    public CompareViewModel ViewModel { get; } = new();

    public ComparePage()
    {
        InitializeComponent();
        ListDifferences.ItemsSource = ViewModel.Differences;
    }
}
