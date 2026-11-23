using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Archeion.Application;
using Archeion.Domain;
using Archeion.Domain.Entities;
using Archeion_App.Services;

namespace Archeion_App.ViewModels;

public class SnapshotsViewModel : BaseViewModel
{
    private bool _isLoading;
    private string _statusMessage = string.Empty;
    private SnapshotRecord? _selectedSnapshot;

    public ObservableCollection<SnapshotRecord> Snapshots { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public SnapshotRecord? SelectedSnapshot
    {
        get => _selectedSnapshot;
        set => SetProperty(ref _selectedSnapshot, value);
    }

    public async Task LoadSnapshotsAsync(Guid deviceId)
    {
        IsLoading = true;
        Snapshots.Clear();
        try
        {
            var items = await AppServices.Service.GetSnapshotsAsync(deviceId);
            foreach (var item in items)
            {
                Snapshots.Add(item);
            }
            StatusMessage = $"Loaded {items.Count} snapshot(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
