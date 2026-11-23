using System;
using System.IO;
using Archeion.Adb;
using Archeion.Application;
using Archeion.CapabilityEngine;
using Archeion.DeviceDiscovery;
using Archeion.Domain;
using Archeion.Security;
using Archeion.SnapshotComparison;

namespace Archeion_App.Services;

public static class AppServices
{
    private static IArcheionApplicationService? _applicationService;

    public static IArcheionApplicationService Service => _applicationService ??= CreateDefaultApplicationService();

    public static string DefaultRepositoryDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ArcheionRepository");

    private static IArcheionApplicationService CreateDefaultApplicationService()
    {
        return new ArcheionApplicationService();
    }
}
