using System;
using Archeion.Application;
using Archeion.Contracts;
using Archeion.Domain;
using Archeion.Domain.ValueObjects;
using Archeion_App.Services;

namespace Archeion_App.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private string _repositoryPath = AppServices.DefaultRepositoryDirectory;
    private bool _autoBackupOnConnect = true;
    private int _retentionMaxSnapshots = 10;
    private string _emergencyKeyDisplay = string.Empty;

    public string RepositoryPath
    {
        get => _repositoryPath;
        set => SetProperty(ref _repositoryPath, value);
    }

    public bool AutoBackupOnConnect
    {
        get => _autoBackupOnConnect;
        set => SetProperty(ref _autoBackupOnConnect, value);
    }

    public int RetentionMaxSnapshots
    {
        get => _retentionMaxSnapshots;
        set => SetProperty(ref _retentionMaxSnapshots, value);
    }

    public string EmergencyKeyDisplay
    {
        get => _emergencyKeyDisplay;
        set => SetProperty(ref _emergencyKeyDisplay, value);
    }

    public void GenerateNewEmergencyRecoveryKey()
    {
        var key = RecoveryKey.Generate();
        EmergencyKeyDisplay = key.KeyString;
    }
}
