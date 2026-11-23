using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Archeion.Application;
using Archeion.Contracts;
using Archeion.DeviceDiscovery;
using Archeion.Domain;
using Archeion.Domain.Entities;
using Archeion_App.Services;

namespace Archeion_App.ViewModels;

public class DevicesViewModel : BaseViewModel
{
    private bool _isScanning;
    private string _statusMessage = "Ready to scan for Android devices.";
    private DiscoveredEndpointResult? _selectedEndpoint;
    private CapabilityAssessment? _currentAssessment;

    public ObservableCollection<DiscoveredEndpointResult> Endpoints { get; } = new();
    public ObservableCollection<CapabilityReportItem> CapabilitiesList { get; } = new();

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

    public DiscoveredEndpointResult? SelectedEndpoint
    {
        get => _selectedEndpoint;
        set
        {
            if (SetProperty(ref _selectedEndpoint, value))
            {
                _ = InspectEndpointAsync(value);
            }
        }
    }

    public CapabilityAssessment? CurrentAssessment
    {
        get => _currentAssessment;
        set => SetProperty(ref _currentAssessment, value);
    }

    public async Task ScanEndpointsAsync()
    {
        IsScanning = true;
        StatusMessage = "Scanning USB ADB and network companion endpoints...";
        Endpoints.Clear();
        CapabilitiesList.Clear();

        try
        {
            var results = await AppServices.Service.DiscoverDevicesAsync();
            foreach (var r in results)
            {
                Endpoints.Add(r);
            }
            StatusMessage = $"Found {results.Count} device endpoint(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Discovery error: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    private async Task InspectEndpointAsync(DiscoveredEndpointResult? endpoint)
    {
        if (endpoint == null) return;

        try
        {
            var app = AppServices.Service;
            if (app.CurrentSession == null) return;

            var device = await app.RegisterDeviceFromEndpointAsync(endpoint);
            var assessment = await app.AuditDeviceCapabilitiesAsync(device.Id);
            CurrentAssessment = assessment;

            CapabilitiesList.Clear();
            foreach (var item in assessment.CategoryCapabilities.Values)
            {
                CapabilitiesList.Add(item);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Audit failed: {ex.Message}";
        }
    }
}
