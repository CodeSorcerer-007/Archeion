using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Archeion_App.Pages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Archeion_App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.SetIcon("Assets/AppIcon.ico");
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        NavFrame.GoBack();
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            NavFrame.Navigate(typeof(SettingsPage));
        }
        else if (args.SelectedItem is NavigationViewItem item)
        {
            switch (item.Tag)
            {
                case "home":
                    NavFrame.Navigate(typeof(HomePage));
                    break;
                case "devices":
                    NavFrame.Navigate(typeof(DevicesPage));
                    break;
                case "backup":
                    NavFrame.Navigate(typeof(BackupPage));
                    break;
                case "snapshots":
                    NavFrame.Navigate(typeof(SnapshotsPage));
                    break;
                case "explorer":
                    NavFrame.Navigate(typeof(ExplorerPage));
                    break;
                case "compare":
                    NavFrame.Navigate(typeof(ComparePage));
                    break;
                case "restore":
                    NavFrame.Navigate(typeof(RestorePage));
                    break;
                case "diagnostics":
                    NavFrame.Navigate(typeof(DiagnosticsPage));
                    break;
                default:
                    NavFrame.Navigate(typeof(HomePage));
                    break;
            }
        }
    }
}
