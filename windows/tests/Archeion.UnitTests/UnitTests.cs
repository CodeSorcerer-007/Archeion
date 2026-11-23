using Archeion.CapabilityEngine;
using Archeion.Contracts;
using Archeion.Domain.Entities;
using Archeion.Domain.ValueObjects;
using Archeion.SnapshotComparison;
using Xunit;

namespace Archeion.UnitTests;

public class UnitTests
{
    [Fact]
    public void ContentHash_ValidHash_Succeeds()
    {
        const string valid = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        var hash = new ContentHash(valid);
        Assert.Equal(valid, hash.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b85")] // 63 chars
    [InlineData("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b8555")] // 65 chars
    [InlineData("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b85g")] // invalid char 'g'
    public void ContentHash_InvalidHash_ThrowsArgumentException(string invalid)
    {
        Assert.Throws<ArgumentException>(() => new ContentHash(invalid));
    }

    [Fact]
    public void StoragePath_PreventsPathTraversal()
    {
        Assert.Throws<ArgumentException>(() => new StoragePath("../secret/passwords.txt"));
        Assert.Throws<ArgumentException>(() => new StoragePath("DCIM/../../etc/shadow"));
        Assert.Throws<ArgumentException>(() => new StoragePath("C:/Windows/System32/cmd.exe"));
    }

    [Fact]
    public void StoragePath_NormalizesSlashes()
    {
        var path = new StoragePath(@"DCIM\Camera\Photos\vacation.jpg");
        Assert.Equal("DCIM/Camera/Photos/vacation.jpg", path.NormalizedPath);
        Assert.Equal("vacation.jpg", path.FileName);
    }

    [Fact]
    public void RecoveryKey_GeneratesValid256BitKey()
    {
        var key = RecoveryKey.Generate();
        Assert.NotNull(key.KeyString);
        var bytes = key.ToBytes();
        Assert.Equal(32, bytes.Length);

        // Reconstruct from string
        var reconstructed = new RecoveryKey(key.KeyString);
        Assert.Equal(key.KeyString, reconstructed.KeyString);
    }

    [Fact]
    public void CapabilityEvaluator_TruthfullyReportsBlockedByOS()
    {
        var evaluator = new CapabilityEvaluator();
        var device = new DeviceRecord
        {
            Id = Guid.NewGuid(),
            Manufacturer = "Google",
            Model = "Pixel 8",
            AndroidVersion = "15",
            ApiLevel = 35
        };

        var assessment = evaluator.Evaluate(device);

        Assert.Equal(CapabilityStatus.BlockedByOS, assessment.GetStatus(BackupCategory.AppPrivateData));
        Assert.Equal(CapabilityStatus.BlockedByOS, assessment.GetStatus(BackupCategory.Sms));
        Assert.Equal(CapabilityStatus.BlockedByOS, assessment.GetStatus(BackupCategory.CallLogs));
        Assert.Equal(CapabilityStatus.Available, assessment.GetStatus(BackupCategory.AppInventory));
        Assert.Equal(CapabilityStatus.Available, assessment.GetStatus(BackupCategory.Apks));
        Assert.Equal(CapabilityStatus.AvailableWithUserAction, assessment.GetStatus(BackupCategory.Documents));
    }

    [Fact]
    public void SnapshotComparator_ClassifiesAdded_Removed_Modified_Unchanged_Renamed()
    {
        var comparator = new SnapshotComparator();
        var baseSnap = new SnapshotRecord
        {
            Id = Guid.NewGuid(),
            Items =
            {
                new BackupItemRecord { Id = Guid.NewGuid(), LogicalPath = "photos/img1.jpg", Sha256 = "1111111111111111111111111111111111111111111111111111111111111111", SizeBytes = 100 },
                new BackupItemRecord { Id = Guid.NewGuid(), LogicalPath = "docs/file_to_modify.txt", Sha256 = "2222222222222222222222222222222222222222222222222222222222222222", SizeBytes = 200 },
                new BackupItemRecord { Id = Guid.NewGuid(), LogicalPath = "docs/file_to_remove.txt", Sha256 = "3333333333333333333333333333333333333333333333333333333333333333", SizeBytes = 300 },
                new BackupItemRecord { Id = Guid.NewGuid(), LogicalPath = "docs/old_name.txt", Sha256 = "4444444444444444444444444444444444444444444444444444444444444444", SizeBytes = 400 }
            }
        };

        var targetSnap = new SnapshotRecord
        {
            Id = Guid.NewGuid(),
            Items =
            {
                new BackupItemRecord { Id = Guid.NewGuid(), LogicalPath = "photos/img1.jpg", Sha256 = "1111111111111111111111111111111111111111111111111111111111111111", SizeBytes = 100 }, // Unchanged
                new BackupItemRecord { Id = Guid.NewGuid(), LogicalPath = "docs/file_to_modify.txt", Sha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", SizeBytes = 250 }, // Modified
                new BackupItemRecord { Id = Guid.NewGuid(), LogicalPath = "docs/new_name.txt", Sha256 = "4444444444444444444444444444444444444444444444444444444444444444", SizeBytes = 400 }, // Renamed
                new BackupItemRecord { Id = Guid.NewGuid(), LogicalPath = "music/song.mp3", Sha256 = "5555555555555555555555555555555555555555555555555555555555555555", SizeBytes = 500 } // Added
            }
        };

        var diff = comparator.Compare(baseSnap, targetSnap);

        Assert.Equal(1, diff.UnchangedCount);
        Assert.Equal(1, diff.ModifiedCount);
        Assert.Equal(1, diff.RenamedCount);
        Assert.Equal(1, diff.AddedCount);
        Assert.Equal(1, diff.RemovedCount);
    }
}
