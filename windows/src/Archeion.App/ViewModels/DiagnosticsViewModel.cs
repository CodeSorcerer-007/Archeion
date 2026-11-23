using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Archeion.Application;
using Archeion.Domain;
using Archeion_App.Services;

namespace Archeion_App.ViewModels;

public class DiagnosticsViewModel : BaseViewModel
{
    private bool _isScanning;
    private string _statusMessage = "Ready to perform repository integrity audit.";
    private IntegrityScanResult? _lastScanResult;

    public ObservableCollection<string> Discrepancies { get; } = new();

    public bool IsScanning
    {
        get => _isScanning;
        set => SetProperty(ref _isScanning, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public IntegrityScanResult? LastScanResult
    {
        get => _lastScanResult;
        set => SetProperty(ref _lastScanResult, value);
    }

    public async Task RunScanAsync()
    {
        IsScanning = true;
        StatusMessage = "Scanning content-addressable object store integrity...";
        Discrepancies.Clear();

        try
        {
            var result = await AppServices.Service.RunIntegrityScanAsync();
            LastScanResult = result;

            foreach (var d in result.Discrepancies)
            {
                Discrepancies.Add(d);
            }

            StatusMessage = $"Scan complete: {result.IntactObjectsCount} intact, " +
                             $"{result.CorruptedObjectsCount} corrupted, " +
                             $"{result.MissingObjectsCount} missing.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    public async Task CleanupStagingAsync()
    {
        try
        {
            StatusMessage = "Cleaning up orphan staging files...";
            var cleaned = await AppServices.Service.CleanupStagingAsync("manual_cleanup");
            StatusMessage = $"Staging cleaned: {cleaned} temp file(s) removed.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Cleanup failed: {ex.Message}";
        }
    }
}
