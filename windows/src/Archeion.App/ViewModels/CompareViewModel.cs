using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Archeion.Application;
using Archeion.Domain;
using Archeion.Domain.Entities;
using Archeion.SnapshotComparison;
using Archeion_App.Services;

namespace Archeion_App.ViewModels;

public class CompareViewModel : BaseViewModel
{
    private bool _isComparing;
    private string _summaryMessage = "Select two snapshots to compare.";

    public ObservableCollection<SnapshotItemDiff> Differences { get; } = new();

    public bool IsComparing
    {
        get => _isComparing;
        set => SetProperty(ref _isComparing, value);
    }

    public string SummaryMessage
    {
        get => _summaryMessage;
        set => SetProperty(ref _summaryMessage, value);
    }

    public async Task CompareAsync(Guid baselineSnapshotId, Guid targetSnapshotId)
    {
        IsComparing = true;
        Differences.Clear();
        SummaryMessage = "Computing two-snapshot differential...";

        try
        {
            var diff = await AppServices.Service.CompareSnapshotsAsync(baselineSnapshotId, targetSnapshotId);
            foreach (var d in diff.ItemDifferences)
            {
                Differences.Add(d);
            }

            SummaryMessage = $"Comparison complete: {diff.ItemDifferences.Count} total differences " +
                             $"({diff.AddedCount} Added, {diff.ModifiedCount} Modified, " +
                             $"{diff.RemovedCount} Removed, {diff.RenamedCount} Renamed, {diff.UnchangedCount} Unchanged).";
        }
        catch (Exception ex)
        {
            SummaryMessage = $"Comparison failed: {ex.Message}";
        }
        finally
        {
            IsComparing = false;
        }
    }
}
