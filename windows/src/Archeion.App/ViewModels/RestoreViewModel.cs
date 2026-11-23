using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Archeion.Application;
using Archeion.Contracts;
using Archeion.Domain;
using Archeion.RestoreEngine;
using Archeion_App.Services;

namespace Archeion_App.ViewModels;

public class RestoreViewModel : BaseViewModel
{
    private bool _isRestoring;
    private string _statusMessage = "Select a snapshot and restore destination directory.";
    private RestoreConflictAction _selectedConflictAction = RestoreConflictAction.Rename;

    public ObservableCollection<RestoreItemPlan> PlannedItems { get; } = new();

    public bool IsRestoring
    {
        get => _isRestoring;
        set => SetProperty(ref _isRestoring, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public RestoreConflictAction SelectedConflictAction
    {
        get => _selectedConflictAction;
        set => SetProperty(ref _selectedConflictAction, value);
    }

    public async Task PreviewRestoreAsync(Guid snapshotId, string destinationDirectory)
    {
        PlannedItems.Clear();
        try
        {
            var plan = await AppServices.Service.BuildRestorePlanAsync(snapshotId, itemIds: null, destinationDirectory, conflictAction: SelectedConflictAction);
            foreach (var item in plan.Items)
            {
                PlannedItems.Add(item);
            }
            StatusMessage = $"Plan generated: {plan.Items.Count} items to restore. Conflict mode: {SelectedConflictAction}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    public async Task ExecuteRestoreAsync(RestorePlan plan)
    {
        IsRestoring = true;
        StatusMessage = "Restoring items to destination directory...";

        try
        {
            var progress = new Progress<RestoreProgressReport>(report =>
            {
                StatusMessage = $"Restoring ({report.ProcessedItems}/{report.TotalItems}): {report.CurrentItemName}";
            });

            var result = await AppServices.Service.ExecuteRestoreAsync(plan, progress);
            StatusMessage = $"Restore complete: {result.RestoredCount} restored, {result.SkippedCount} skipped, {result.FailedCount} failed ({result.RestoredBytes} bytes).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Restore failed: {ex.Message}";
        }
        finally
        {
            IsRestoring = false;
        }
    }
}
