using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Archeion.Application;
using Archeion.Domain;
using Archeion_App.Services;

namespace Archeion_App.ViewModels;

public class OverviewViewModel : BaseViewModel
{
    private string _repositoryStatus = "Checking repository...";
    private string _deviceSummary = "No devices detected";
    private string _statsSummary = "0 Snapshots | 0 Bytes";
    private bool _isBusy;

    public string RepositoryStatus
    {
        get => _repositoryStatus;
        set => SetProperty(ref _repositoryStatus, value);
    }

    public string DeviceSummary
    {
        get => _deviceSummary;
        set => SetProperty(ref _deviceSummary, value);
    }

    public string StatsSummary
    {
        get => _statsSummary;
        set => SetProperty(ref _statsSummary, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var app = AppServices.Service;
            if (app.CurrentSession == null)
            {
                var repoPath = AppServices.DefaultRepositoryDirectory;
                if (System.IO.Directory.Exists(repoPath))
                {
                    try
                    {
                        await app.OpenRepositoryAsync(repoPath);
                        RepositoryStatus = $"Repository: Unlocked ({repoPath})";
                    }
                    catch
                    {
                        RepositoryStatus = $"Repository Locked or Encrypted ({repoPath})";
                    }
                }
                else
                {
                    await app.CreateRepositoryAsync(repoPath);
                    RepositoryStatus = $"Repository Created: ({repoPath})";
                }
            }
            else
            {
                RepositoryStatus = $"Repository: Active ({app.CurrentSession.Metadata.RootPath})";
            }

            if (app.CurrentSession != null)
            {
                var devices = await app.GetRegisteredDevicesAsync();
                DeviceSummary = $"{devices.Count} Registered Android Device(s)";
            }
        }
        catch (Exception ex)
        {
            RepositoryStatus = $"Status: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
