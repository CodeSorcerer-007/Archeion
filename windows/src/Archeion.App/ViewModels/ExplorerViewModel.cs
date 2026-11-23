using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Archeion.Application;
using Archeion.Contracts;
using Archeion.Domain;
using Archeion.Domain.Entities;
using Archeion_App.Services;

namespace Archeion_App.ViewModels;

public class ExplorerViewModel : BaseViewModel
{
    private bool _isLoading;
    private string _searchFilter = string.Empty;
    private BackupCategory? _categoryFilter;
    private string _statusMessage = string.Empty;
    private BackupItemRecord? _selectedItem;

    public ObservableCollection<BackupItemRecord> Items { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string SearchFilter
    {
        get => _searchFilter;
        set => SetProperty(ref _searchFilter, value);
    }

    public BackupCategory? CategoryFilter
    {
        get => _categoryFilter;
        set => SetProperty(ref _categoryFilter, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public BackupItemRecord? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public async Task SearchAsync(Guid snapshotId)
    {
        IsLoading = true;
        Items.Clear();
        try
        {
            var results = await AppServices.Service.BrowseSnapshotItemsAsync(
                snapshotId,
                categoryFilter: _categoryFilter,
                search: _searchFilter
            );

            foreach (var item in results)
            {
                Items.Add(item);
            }
            StatusMessage = $"Found {results.Count} item(s).";
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

    public async Task<bool> ExportSelectedItemAsync(Guid snapshotId, string destinationFilePath)
    {
        if (SelectedItem == null) return false;
        try
        {
            StatusMessage = $"Exporting {SelectedItem.LogicalPath}...";
            var success = await AppServices.Service.ExportItemToFileAsync(snapshotId, SelectedItem.Id, destinationFilePath);
            StatusMessage = success ? "Item successfully exported!" : "Export verification failed.";
            return success;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
            return false;
        }
    }
}
