using System.Security.Cryptography;
using System.Text;
using Archeion.Application;
using Archeion.BackupEngine;
using Archeion.Contracts;
using Archeion.DeviceDiscovery;
using Archeion.Domain.Entities;
using Archeion.Domain.ValueObjects;
using Archeion.RestoreEngine;
using Xunit;

namespace Archeion.IntegrationTests;

public class IntegrationTests : IDisposable
{
    private readonly string _testRoot;

    public IntegrationTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "ArcheionIntegration_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testRoot))
            {
                Directory.Delete(_testRoot, recursive: true);
            }
        }
        catch { /* best effort */ }
    }

    [Fact]
    public async Task EndToEnd_PersonalBackupAndRestoreCycle_Succeeds()
    {
        var appService = new ArcheionApplicationService();

        // 1. First Launch & Create Encrypted Repository
        var repoPath = Path.Combine(_testRoot, "PrimaryLibrary");
        const string password = "PersonalMasterPassword2026!";
        var session = await appService.CreateRepositoryAsync(repoPath, password, "My Personal Phone Backups");

        Assert.NotNull(session);
        Assert.NotNull(session.MasterEncryptionKey);
        Assert.NotNull(session.RecoveryKey);
        Assert.True(File.Exists(Path.Combine(repoPath, "repository.json")));
        Assert.True(File.Exists(Path.Combine(repoPath, "keys", "key-envelope.json")));
        Assert.True(File.Exists(Path.Combine(repoPath, "database", "library.sqlite")));

        // 2. Close & Reopen Repository using Password
        var reopenedSession = await appService.OpenRepositoryAsync(repoPath, password);
        Assert.Equal(session.Metadata.RepositoryId, reopenedSession.Metadata.RepositoryId);
        Assert.Equal(session.MasterEncryptionKey, reopenedSession.MasterEncryptionKey);

        // 3. Reopen using Recovery Key
        var recoveryKey = session.RecoveryKey!;
        var recoverySession = await appService.OpenRepositoryAsync(repoPath, recoveryKey: recoveryKey);
        Assert.Equal(session.Metadata.RepositoryId, recoverySession.Metadata.RepositoryId);
        Assert.Equal(session.MasterEncryptionKey, recoverySession.MasterEncryptionKey);

        // 4. Register Device (from Endpoint probe)
        var endpoint = new DiscoveredEndpointResult(
            EndpointIdentifier: "adb:emulator-5554",
            Transport: TransportType.Adb,
            State: DeviceState.Connected,
            Serial: "emulator-5554",
            Manufacturer: "Google",
            Model: "Pixel 8 Pro",
            StatusMessage: "Connected and authorized"
        );

        // Register device from discovered endpoint
        var device = await appService.RegisterDeviceFromEndpointAsync(endpoint);
        Assert.NotNull(device);
        Assert.Equal(DeviceState.Identified, device.State);

        var registeredDevices = await appService.GetRegisteredDevicesAsync();
        Assert.NotEmpty(registeredDevices);

        var capabilities = await appService.AuditDeviceCapabilitiesAsync(device.Id);
        Assert.NotNull(capabilities);
        Assert.Equal(CapabilityStatus.BlockedByOS, capabilities.GetStatus(BackupCategory.AppPrivateData));
        Assert.Equal(CapabilityStatus.Available, capabilities.GetStatus(BackupCategory.AppInventory));

        // 5. Build Immutable Backup Plan
        var plan = await appService.CreateBackupPlanAsync(device.Id, new List<BackupCategory>
        {
            BackupCategory.Photos,
            BackupCategory.Documents,
            BackupCategory.Contacts
        });

        // 6. Execute Backup with Simulated In-Memory Data Source
        var testPhotoBytes = Encoding.UTF8.GetBytes("JPEG-SIMULATED-PHOTO-CONTENT-2026-VACATION");
        var testDocBytes = Encoding.UTF8.GetBytes("CONFIDENTIAL-NOTES-TEXT-DOCUMENT");
        var testVcfBytes = Encoding.UTF8.GetBytes("BEGIN:VCARD\r\nVERSION:3.0\r\nFN:John Doe\r\nTEL:555-1234\r\nEND:VCARD\r\n");

        var testDataSource = new InMemoryTestDataSource(new Dictionary<string, byte[]>
        {
            { "DCIM/Camera/IMG_001.jpg", testPhotoBytes },
            { "Documents/Personal/notes.txt", testDocBytes },
            { "Contacts/contacts.vcf", testVcfBytes }
        });

        var progressReports = new List<BackupProgressReport>();
        var progress = new Progress<BackupProgressReport>(r => progressReports.Add(r));

        var snapshot = await appService.ExecuteBackupAsync(plan, testDataSource, progress);
        Assert.NotNull(snapshot);
        Assert.Equal(SnapshotState.Completed, snapshot.State);
        Assert.Equal(3, snapshot.Items.Count);
        Assert.Equal(VerificationStatus.Verified, snapshot.VerificationStatus);
        Assert.True(snapshot.TotalBytes > 0);

        // 7. Verify snapshot manifest file was written
        var manifestPath = Path.Combine(repoPath, "devices", device.Id.ToString(), "snapshots", snapshot.Id.ToString(), "manifest.json");
        Assert.True(File.Exists(manifestPath));

        // 8. Offline Navigation & Backup Explorer
        var retrievedSnapshots = await appService.GetSnapshotsAsync(device.Id);
        Assert.Single(retrievedSnapshots);

        var browsedItems = await appService.BrowseSnapshotItemsAsync(snapshot.Id);
        Assert.Equal(3, browsedItems.Count);

        var photoItem = browsedItems.First(i => i.LogicalPath.EndsWith("IMG_001.jpg"));
        Assert.Equal(BackupCategory.Photos, photoItem.Category);

        // 9. Export Item To Destination & Verify Hash
        var exportPath = Path.Combine(_testRoot, "Exports", "IMG_001.jpg");
        var exportSuccess = await appService.ExportItemToFileAsync(snapshot.Id, photoItem.Id, exportPath);
        Assert.True(exportSuccess);
        Assert.True(File.Exists(exportPath));
        var exportedBytes = await File.ReadAllBytesAsync(exportPath);
        Assert.Equal(testPhotoBytes, exportedBytes);

        // 10. Run Full Repository Integrity Scan
        var scanResult = await appService.RunIntegrityScanAsync();
        Assert.True(scanResult.TotalObjectsChecked >= 4); // 3 items + 1 manifest
        Assert.Equal(scanResult.TotalObjectsChecked, scanResult.IntactObjectsCount);
        Assert.Equal(0, scanResult.CorruptedObjectsCount);
        Assert.Equal(0, scanResult.MissingObjectsCount);
        Assert.Empty(scanResult.Discrepancies);

        // 11. Create Second Snapshot and Compare
        var modifiedDocBytes = Encoding.UTF8.GetBytes("CONFIDENTIAL-NOTES-UPDATED-CONTENT");
        var newAudioBytes = Encoding.UTF8.GetBytes("MP3-AUDIO-VOICE-MEMO");

        var secondDataSource = new InMemoryTestDataSource(new Dictionary<string, byte[]>
        {
            { "DCIM/Camera/IMG_001.jpg", testPhotoBytes }, // Unchanged
            { "Documents/Personal/notes.txt", modifiedDocBytes }, // Modified
            { "VoiceRecorder/memo.mp3", newAudioBytes } // Added, contacts.vcf Removed
        });

        var plan2 = await appService.CreateBackupPlanAsync(device.Id, new List<BackupCategory>
        {
            BackupCategory.Photos,
            BackupCategory.Documents,
            BackupCategory.Audio
        });

        var snapshot2 = await appService.ExecuteBackupAsync(plan2, secondDataSource);
        Assert.NotNull(snapshot2);

        var diff = await appService.CompareSnapshotsAsync(snapshot.Id, snapshot2.Id);
        Assert.Equal(1, diff.UnchangedCount); // IMG_001.jpg
        Assert.Equal(1, diff.ModifiedCount);  // notes.txt
        Assert.Equal(1, diff.AddedCount);     // memo.mp3
        Assert.Equal(1, diff.RemovedCount);   // contacts.vcf

        // 12. Safe Conflict-Aware Restore Execution
        var restoreDestDir = Path.Combine(_testRoot, "RestoredFiles");
        var restorePlan = await appService.BuildRestorePlanAsync(snapshot.Id, null, restoreDestDir, RestoreConflictAction.Replace);
        Assert.Equal(3, restorePlan.Items.Count);

        var restoreResult = await appService.ExecuteRestoreAsync(restorePlan);
        Assert.Equal(3, restoreResult.RestoredCount);
        Assert.Equal(0, restoreResult.FailedCount);
        Assert.Equal(0, restoreResult.SkippedCount);

        // Verify restored files exist on disk and match exact byte contents
        var restoredPhoto = Path.Combine(restoreDestDir, "DCIM", "Camera", "IMG_001.jpg");
        var restoredDoc = Path.Combine(restoreDestDir, "Documents", "Personal", "notes.txt");
        var restoredVcf = Path.Combine(restoreDestDir, "Contacts", "contacts.vcf");

        Assert.True(File.Exists(restoredPhoto));
        Assert.True(File.Exists(restoredDoc));
        Assert.True(File.Exists(restoredVcf));

        Assert.Equal(testPhotoBytes, await File.ReadAllBytesAsync(restoredPhoto));
        Assert.Equal(testDocBytes, await File.ReadAllBytesAsync(restoredDoc));
        Assert.Equal(testVcfBytes, await File.ReadAllBytesAsync(restoredVcf));
    }
}

internal sealed class InMemoryTestDataSource : IBackupDataSource
{
    private readonly Dictionary<string, byte[]> _files;

    public InMemoryTestDataSource(Dictionary<string, byte[]> files)
    {
        _files = files;
    }

    public Task<List<SourceItemDescriptor>> EnumerateItemsAsync(BackupPlan plan, CancellationToken cancellationToken = default)
    {
        var items = new List<SourceItemDescriptor>();
        foreach (var (path, data) in _files)
        {
            var cat = path.StartsWith("DCIM") ? BackupCategory.Photos :
                      path.StartsWith("Documents") ? BackupCategory.Documents :
                      path.StartsWith("Contacts") ? BackupCategory.Contacts :
                      path.StartsWith("VoiceRecorder") ? BackupCategory.Audio :
                      BackupCategory.Documents;

            items.Add(new SourceItemDescriptor(
                Category: cat,
                LogicalPath: path,
                SizeBytes: data.Length,
                LastModifiedUtc: DateTimeOffset.UtcNow,
                SourceProvider: "SimulatedProvider"
            ));
        }

        return Task.FromResult(items);
    }

    public Task<Stream> OpenReadStreamAsync(SourceItemDescriptor item, CancellationToken cancellationToken = default)
    {
        if (_files.TryGetValue(item.LogicalPath, out var data))
        {
            return Task.FromResult<Stream>(new MemoryStream(data));
        }

        throw new FileNotFoundException("File not found in test source", item.LogicalPath);
    }
}
