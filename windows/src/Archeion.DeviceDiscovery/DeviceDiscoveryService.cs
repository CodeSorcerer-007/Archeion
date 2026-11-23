using System.Security.Cryptography;
using System.Text;
using Archeion.Adb;
using Archeion.Contracts;
using Archeion.Domain.Entities;

namespace Archeion.DeviceDiscovery;

public interface IDeviceDiscoveryService
{
    Task<List<DiscoveredEndpointResult>> DiscoverEndpointsAsync(CancellationToken cancellationToken = default);
    Task<DeviceRecord> ResolveDeviceIdentityAsync(DiscoveredEndpointResult endpoint, CancellationToken cancellationToken = default);
}

public sealed record DiscoveredEndpointResult(
    string EndpointIdentifier,
    TransportType Transport,
    DeviceState State,
    string? Serial,
    string? Manufacturer,
    string? Model,
    string? StatusMessage
);

public sealed class DeviceDiscoveryService : IDeviceDiscoveryService
{
    private readonly IAdbClient _adbClient;

    public DeviceDiscoveryService(IAdbClient adbClient)
    {
        _adbClient = adbClient ?? throw new ArgumentNullException(nameof(adbClient));
    }

    public async Task<List<DiscoveredEndpointResult>> DiscoverEndpointsAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<DiscoveredEndpointResult>();
        if (!_adbClient.IsAdbAvailable())
        {
            return results;
        }

        var adbEndpoints = await _adbClient.GetDevicesAsync(cancellationToken);
        foreach (var ep in adbEndpoints)
        {
            var (state, msg) = ParseState(ep.State);
            results.Add(new DiscoveredEndpointResult(
                EndpointIdentifier: $"adb:{ep.Serial}",
                Transport: TransportType.Adb,
                State: state,
                Serial: ep.Serial,
                Manufacturer: ep.Product,
                Model: ep.Model,
                StatusMessage: msg
            ));
        }

        return results;
    }

    public async Task<DeviceRecord> ResolveDeviceIdentityAsync(DiscoveredEndpointResult endpoint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        if (endpoint.State == DeviceState.Unauthorized)
        {
            return new DeviceRecord
            {
                Id = DeriveDeterministicId($"unauthorized:{endpoint.Serial}"),
                DisplayName = endpoint.Model ?? "Unauthorized Device",
                Model = endpoint.Model ?? "Unknown Android Device",
                State = DeviceState.Unauthorized,
                IdentityConfidence = "Low",
                Notes = "Device is connected via USB but waiting for RSA key authorization prompt on screen."
            };
        }

        if (endpoint.State != DeviceState.Connected && endpoint.State != DeviceState.Identified)
        {
            return new DeviceRecord
            {
                Id = DeriveDeterministicId($"offline:{endpoint.Serial}"),
                DisplayName = endpoint.Model ?? "Disconnected Device",
                Model = endpoint.Model ?? "Unknown Android Device",
                State = endpoint.State,
                IdentityConfidence = "Low"
            };
        }

        // Query real device properties
        var serial = endpoint.Serial ?? throw new InvalidOperationException("Serial cannot be null for connected endpoint.");
        var props = await _adbClient.GetAllPropertiesAsync(serial, cancellationToken);

        var manufacturer = props.GetValueOrDefault("ro.product.manufacturer", endpoint.Manufacturer ?? "Android");
        var brand = props.GetValueOrDefault("ro.product.brand", manufacturer);
        var model = props.GetValueOrDefault("ro.product.model", endpoint.Model ?? "Device");
        var modelCode = props.GetValueOrDefault("ro.product.modelcode", model);
        var product = props.GetValueOrDefault("ro.product.name", "generic");
        var codename = props.GetValueOrDefault("ro.product.device", product);
        var androidVersion = props.GetValueOrDefault("ro.build.version.release", "14");
        var apiLevelStr = props.GetValueOrDefault("ro.build.version.sdk", "34");
        var buildFingerprint = props.GetValueOrDefault("ro.build.fingerprint", $"{manufacturer}/{product}/{codename}:{androidVersion}");
        var securityPatch = props.GetValueOrDefault("ro.build.version.security_patch", "2024-01-01");

        _ = int.TryParse(apiLevelStr, out var apiLevel);
        if (apiLevel == 0) apiLevel = 34;

        var deterministicId = DeriveDeterministicId(buildFingerprint);

        return new DeviceRecord
        {
            Id = deterministicId,
            DisplayName = $"{brand} {model}",
            Manufacturer = manufacturer,
            Brand = brand,
            Model = model,
            ModelCode = modelCode,
            Product = product,
            Codename = codename,
            AndroidVersion = androidVersion,
            ApiLevel = apiLevel,
            BuildFingerprint = buildFingerprint,
            SecurityPatch = securityPatch,
            State = DeviceState.Identified,
            IdentityConfidence = "High",
            LastSeenUtc = DateTimeOffset.UtcNow
        };
    }

    private static (DeviceState State, string Message) ParseState(string rawState) => rawState.ToLowerInvariant() switch
    {
        "device" => (DeviceState.Connected, "Connected and authorized"),
        "unauthorized" => (DeviceState.Unauthorized, "Unauthorized: Please unlock phone and tap 'Allow USB debugging'"),
        "offline" => (DeviceState.Disconnected, "Device is offline"),
        "recovery" => (DeviceState.Error, "Device is in recovery mode"),
        _ => (DeviceState.UnknownDevice, $"Device state: {rawState}")
    };

    private static Guid DeriveDeterministicId(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var guidBytes = new byte[16];
        Buffer.BlockCopy(hash, 0, guidBytes, 0, 16);
        return new Guid(guidBytes);
    }
}
