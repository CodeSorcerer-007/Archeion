using System.Diagnostics;
using System.Text.RegularExpressions;
using Archeion.Contracts;

namespace Archeion.Adb;

public interface IAdbClient
{
    Task<List<AdbDeviceEndpoint>> GetDevicesAsync(CancellationToken cancellationToken = default);
    Task<string> GetPropertyAsync(string serial, string propertyName, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> GetAllPropertiesAsync(string serial, CancellationToken cancellationToken = default);
    Task<List<string>> ListPackagesAsync(string serial, bool thirdPartyOnly = true, CancellationToken cancellationToken = default);
    Task<bool> PullFileAsync(string serial, string remotePath, string localPath, CancellationToken cancellationToken = default);
    bool IsAdbAvailable();
    string? AdbPath { get; }
}

public sealed record AdbDeviceEndpoint(
    string Serial,
    string State, // "device", "unauthorized", "offline", "recovery"
    string? Product,
    string? Model,
    string? Device,
    string? TransportId
);

public partial class AdbClient : IAdbClient
{
    private readonly string? _adbPath;
    private static readonly Regex DeviceLineRegex = CreateDeviceLineRegex();

    public string? AdbPath => _adbPath;

    public AdbClient(string? customAdbPath = null)
    {
        _adbPath = customAdbPath ?? LocateAdb();
    }

    public bool IsAdbAvailable() => !string.IsNullOrEmpty(_adbPath) && File.Exists(_adbPath);

    public async Task<List<AdbDeviceEndpoint>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        var devices = new List<AdbDeviceEndpoint>();
        if (!IsAdbAvailable()) return devices;

        var (exitCode, output, _) = await ExecuteCommandAsync("devices -l", cancellationToken);
        if (exitCode != 0 || string.IsNullOrWhiteSpace(output)) return devices;

        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith("List of devices attached", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("* daemon", StringComparison.OrdinalIgnoreCase)) continue;

            var match = DeviceLineRegex.Match(line.Trim());
            if (match.Success)
            {
                var serial = match.Groups["serial"].Value;
                var state = match.Groups["state"].Value;
                var extra = match.Groups["extra"].Value;

                string? product = ExtractAttribute(extra, "product:");
                string? model = ExtractAttribute(extra, "model:");
                string? device = ExtractAttribute(extra, "device:");
                string? transportId = ExtractAttribute(extra, "transport_id:");

                devices.Add(new AdbDeviceEndpoint(serial, state, product, model, device, transportId));
            }
        }

        return devices;
    }

    public async Task<string> GetPropertyAsync(string serial, string propertyName, CancellationToken cancellationToken = default)
    {
        if (!IsAdbAvailable()) return string.Empty;
        var (exitCode, output, _) = await ExecuteCommandAsync($"-s {serial} shell getprop {propertyName}", cancellationToken);
        return exitCode == 0 ? output.Trim() : string.Empty;
    }

    public async Task<Dictionary<string, string>> GetAllPropertiesAsync(string serial, CancellationToken cancellationToken = default)
    {
        var props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!IsAdbAvailable()) return props;

        var (exitCode, output, _) = await ExecuteCommandAsync($"-s {serial} shell getprop", cancellationToken);
        if (exitCode != 0) return props;

        // Matches lines formatted as [prop.name]: [value]
        var matches = Regex.Matches(output, @"\[(?<key>[^\]]+)\]:\s*\[(?<val>[^\]]*)\]");
        foreach (Match m in matches)
        {
            var key = m.Groups["key"].Value;
            var val = m.Groups["val"].Value;
            props[key] = val;
        }

        return props;
    }

    public async Task<List<string>> ListPackagesAsync(string serial, bool thirdPartyOnly = true, CancellationToken cancellationToken = default)
    {
        var packages = new List<string>();
        if (!IsAdbAvailable()) return packages;

        var args = thirdPartyOnly ? "-3" : "";
        var (exitCode, output, _) = await ExecuteCommandAsync($"-s {serial} shell pm list packages -f {args}", cancellationToken);
        if (exitCode != 0) return packages;

        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var l in lines)
        {
            var clean = l.Trim();
            if (clean.StartsWith("package:"))
            {
                packages.Add(clean[8..]);
            }
        }

        return packages;
    }

    public async Task<bool> PullFileAsync(string serial, string remotePath, string localPath, CancellationToken cancellationToken = default)
    {
        if (!IsAdbAvailable()) return false;
        var dir = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var (exitCode, _, _) = await ExecuteCommandAsync($"-s {serial} pull \"{remotePath}\" \"{localPath}\"", cancellationToken);
        return exitCode == 0 && File.Exists(localPath);
    }

    private async Task<(int ExitCode, string StdOut, string StdErr)> ExecuteCommandAsync(string arguments, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _adbPath!,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return (process.ExitCode, stdout, stderr);
    }

    private static string? ExtractAttribute(string extra, string prefix)
    {
        var idx = extra.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var sub = extra[(idx + prefix.Length)..].Trim();
        var endIdx = sub.IndexOf(' ');
        return endIdx > 0 ? sub[..endIdx] : sub;
    }

    private static string? LocateAdb()
    {
        // 1. Check LOCALAPPDATA Android SDK platform-tools
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var sdkAdb = Path.Combine(localAppData, "Android", "Sdk", "platform-tools", "adb.exe");
        if (File.Exists(sdkAdb)) return sdkAdb;

        // 2. Check ANDROID_HOME or ANDROID_SDK_ROOT
        var androidHome = Environment.GetEnvironmentVariable("ANDROID_HOME") ?? Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
        if (!string.IsNullOrEmpty(androidHome))
        {
            var homeAdb = Path.Combine(androidHome, "platform-tools", "adb.exe");
            if (File.Exists(homeAdb)) return homeAdb;
        }

        // 3. Check PATH
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathVar))
        {
            foreach (var dir in pathVar.Split(Path.PathSeparator))
            {
                var candidate = Path.Combine(dir, "adb.exe");
                if (File.Exists(candidate)) return candidate;
            }
        }

        return null;
    }

    [GeneratedRegex(@"^(?<serial>[^\s]+)\s+(?<state>[^\s]+)(\s+(?<extra>.*))?$")]
    private static partial Regex CreateDeviceLineRegex();
}
