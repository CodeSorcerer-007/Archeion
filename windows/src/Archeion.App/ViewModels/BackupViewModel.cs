using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Archeion.Application;
using Archeion.BackupEngine;
using Archeion.Contracts;
using Archeion.Domain;
using Archeion.Domain.Entities;
using Archeion_App.Services;

namespace Archeion_App.ViewModels;

public class BackupViewModel : BaseViewModel
{
    private bool _isBackingUp;
    private string _currentStage = "Idle";
    private string _currentItem = string.Empty;
    private int _progressPercent;
    private long _processedBytes;
    private long _totalBytes;
    private string _statusMessage = "Select categories and trigger backup.";
    private CancellationTokenSource? _cts;

    public bool BackupPhotos { get; set; } = true;
    public bool BackupVideos { get; set; } = true;
    public bool BackupAudio { get; set; } = true;
    public bool BackupContacts { get; set; } = true;
    public bool BackupCalendar { get; set; } = true;
    public bool BackupAppInventory { get; set; } = true;

    public bool IsBackingUp
    {
        get => _isBackingUp;
        set => SetProperty(ref _isBackingUp, value);
    }

    public string CurrentStage
    {
        get => _currentStage;
        set => SetProperty(ref _currentStage, value);
    }

    public string CurrentItem
    {
        get => _currentItem;
        set => SetProperty(ref _currentItem, value);
    }

    public int ProgressPercent
    {
        get => _progressPercent;
        set => SetProperty(ref _progressPercent, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public async Task StartBackupAsync(DeviceRecord device, IBackupDataSource dataSource)
    {
        IsBackingUp = true;
        _cts = new CancellationTokenSource();
        StatusMessage = "Starting backup session...";

        var categories = new List<BackupCategory>();
        if (BackupPhotos) categories.Add(BackupCategory.Photos);
        if (BackupVideos) categories.Add(BackupCategory.Videos);
        if (BackupAudio) categories.Add(BackupCategory.Audio);
        if (BackupContacts) categories.Add(BackupCategory.Contacts);
        if (BackupCalendar) categories.Add(BackupCategory.Calendar);
        if (BackupAppInventory) categories.Add(BackupCategory.AppInventory);

        try
        {
            var app = AppServices.Service;
            var plan = await app.CreateBackupPlanAsync(device.Id, categories, cancellationToken: _cts.Token);

            var progress = new Progress<BackupProgressReport>(report =>
            {
                CurrentStage = report.CurrentStage;
                CurrentItem = report.CurrentItemName;
                ProgressPercent = report.TotalBytes > 0 ? (int)((double)report.ProcessedBytes / report.TotalBytes * 100) : 0;
                _processedBytes = report.ProcessedBytes;
                _totalBytes = report.TotalBytes;
                StatusMessage = $"{report.CurrentStage}: {report.CurrentItemName} ({report.ProcessedItems}/{report.TotalItems})";
            });

            var snapshot = await app.ExecuteBackupAsync(plan, dataSource, progress, _cts.Token);
            StatusMessage = $"Backup complete! Snapshot ID: {snapshot.Id} ({snapshot.TotalItems} items, {snapshot.TotalBytes} bytes).";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Backup cancelled by user.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Backup failed: {ex.Message}";
        }
        finally
        {
            IsBackingUp = false;
        }
    }

    public void Cancel()
    {
        _cts?.Cancel();
    }
}
