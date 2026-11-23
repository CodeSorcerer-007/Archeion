using Archeion.Contracts;
using Archeion.Domain.Entities;

namespace Archeion.CapabilityEngine;

public interface ICapabilityEvaluator
{
    CapabilityAssessment Evaluate(DeviceRecord device, Dictionary<string, bool>? grantedPermissions = null);
}

public sealed class CapabilityEvaluator : ICapabilityEvaluator
{
    public CapabilityAssessment Evaluate(DeviceRecord device, Dictionary<string, bool>? grantedPermissions = null)
    {
        ArgumentNullException.ThrowIfNull(device);
        var permissions = grantedPermissions ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        var report = new Dictionary<BackupCategory, CapabilityReportItem>();

        // 1. Device Identity
        report[BackupCategory.DeviceIdentity] = new CapabilityReportItem(
            Category: BackupCategory.DeviceIdentity,
            Status: CapabilityStatus.Available,
            Reason: "Device properties and build descriptors are readable via public platform APIs and ADB.",
            RequiredPermission: "None",
            EstimatedItemCount: 1,
            EstimatedSizeBytes: 1024
        );

        // 2. Photos
        var hasPhotoPermission = device.ApiLevel >= 33
            ? permissions.GetValueOrDefault("android.permission.READ_MEDIA_IMAGES", true)
            : permissions.GetValueOrDefault("android.permission.READ_EXTERNAL_STORAGE", true);

        report[BackupCategory.Photos] = new CapabilityReportItem(
            Category: BackupCategory.Photos,
            Status: hasPhotoPermission ? CapabilityStatus.AvailableWithPermission : CapabilityStatus.PermissionDenied,
            Reason: device.ApiLevel >= 34
                ? "MediaStore / Photo Picker supported. Note: On Android 14+, user may grant partial access."
                : "MediaStore image collection supported.",
            RequiredPermission: device.ApiLevel >= 33 ? "READ_MEDIA_IMAGES" : "READ_EXTERNAL_STORAGE",
            EstimatedItemCount: 0,
            EstimatedSizeBytes: 0
        );

        // 3. Videos
        var hasVideoPermission = device.ApiLevel >= 33
            ? permissions.GetValueOrDefault("android.permission.READ_MEDIA_VIDEO", true)
            : permissions.GetValueOrDefault("android.permission.READ_EXTERNAL_STORAGE", true);

        report[BackupCategory.Videos] = new CapabilityReportItem(
            Category: BackupCategory.Videos,
            Status: hasVideoPermission ? CapabilityStatus.AvailableWithPermission : CapabilityStatus.PermissionDenied,
            Reason: "MediaStore video collection supported.",
            RequiredPermission: device.ApiLevel >= 33 ? "READ_MEDIA_VIDEO" : "READ_EXTERNAL_STORAGE",
            EstimatedItemCount: 0,
            EstimatedSizeBytes: 0
        );

        // 4. Audio
        var hasAudioPermission = device.ApiLevel >= 33
            ? permissions.GetValueOrDefault("android.permission.READ_MEDIA_AUDIO", true)
            : permissions.GetValueOrDefault("android.permission.READ_EXTERNAL_STORAGE", true);

        report[BackupCategory.Audio] = new CapabilityReportItem(
            Category: BackupCategory.Audio,
            Status: hasAudioPermission ? CapabilityStatus.AvailableWithPermission : CapabilityStatus.PermissionDenied,
            Reason: "MediaStore audio collection supported.",
            RequiredPermission: device.ApiLevel >= 33 ? "READ_MEDIA_AUDIO" : "READ_EXTERNAL_STORAGE",
            EstimatedItemCount: 0,
            EstimatedSizeBytes: 0
        );

        // 5. Documents & Downloads
        report[BackupCategory.Documents] = new CapabilityReportItem(
            Category: BackupCategory.Documents,
            Status: CapabilityStatus.AvailableWithUserAction,
            Reason: "User selects Documents directory via Storage Access Framework (SAF) folder picker.",
            RequiredPermission: "User Action (SAF folder selection)",
            EstimatedItemCount: 0,
            EstimatedSizeBytes: 0
        );

        report[BackupCategory.Downloads] = new CapabilityReportItem(
            Category: BackupCategory.Downloads,
            Status: CapabilityStatus.AvailableWithUserAction,
            Reason: "User selects Download directory via Storage Access Framework (SAF) folder picker.",
            RequiredPermission: "User Action (SAF folder selection)",
            EstimatedItemCount: 0,
            EstimatedSizeBytes: 0
        );

        // 6. User Folders
        report[BackupCategory.UserFolders] = new CapabilityReportItem(
            Category: BackupCategory.UserFolders,
            Status: CapabilityStatus.AvailableWithUserAction,
            Reason: "SAF ACTION_OPEN_DOCUMENT_TREE enables selective user folder extraction. System roots are restricted by OS.",
            RequiredPermission: "User Action (SAF folder selection)",
            EstimatedItemCount: 0,
            EstimatedSizeBytes: 0
        );

        // 7. Contacts
        var hasContactsPermission = permissions.GetValueOrDefault("android.permission.READ_CONTACTS", true);
        report[BackupCategory.Contacts] = new CapabilityReportItem(
            Category: BackupCategory.Contacts,
            Status: hasContactsPermission ? CapabilityStatus.AvailableWithPermission : CapabilityStatus.PermissionDenied,
            Reason: "Contacts Provider export to standard vCard (.vcf) format.",
            RequiredPermission: "android.permission.READ_CONTACTS",
            EstimatedItemCount: 0,
            EstimatedSizeBytes: 0
        );

        // 8. Calendar
        var hasCalendarPermission = permissions.GetValueOrDefault("android.permission.READ_CALENDAR", true);
        report[BackupCategory.Calendar] = new CapabilityReportItem(
            Category: BackupCategory.Calendar,
            Status: hasCalendarPermission ? CapabilityStatus.AvailableWithPermission : CapabilityStatus.PermissionDenied,
            Reason: "Calendar Provider export to standard iCalendar (.ics) format.",
            RequiredPermission: "android.permission.READ_CALENDAR",
            EstimatedItemCount: 0,
            EstimatedSizeBytes: 0
        );

        // 9. App Inventory
        report[BackupCategory.AppInventory] = new CapabilityReportItem(
            Category: BackupCategory.AppInventory,
            Status: CapabilityStatus.Available,
            Reason: "Installed package names, versions, and installers enumerated via PackageManager or ADB.",
            RequiredPermission: "None (or QUERY_ALL_PACKAGES for full package names)",
            EstimatedItemCount: 0,
            EstimatedSizeBytes: 0
        );

        // 10. APK Binaries
        report[BackupCategory.Apks] = new CapabilityReportItem(
            Category: BackupCategory.Apks,
            Status: CapabilityStatus.Available,
            Reason: "Accessible base APK binaries and split bundles can be exported for archival and reinstall.",
            RequiredPermission: "None / ADB authorized",
            EstimatedItemCount: 0,
            EstimatedSizeBytes: 0
        );

        // 11. App-Private Data (Strict non-root boundary)
        report[BackupCategory.AppPrivateData] = new CapabilityReportItem(
            Category: BackupCategory.AppPrivateData,
            Status: CapabilityStatus.BlockedByOS,
            Reason: "Android Linux kernel UID sandbox restricts access to /data/data/<package>/ without root or custom app integration.",
            RequiredPermission: "Blocked by OS (Non-root baseline)",
            EstimatedItemCount: 0,
            EstimatedSizeBytes: 0
        );

        // 12. SMS & Call Logs
        report[BackupCategory.Sms] = new CapabilityReportItem(
            Category: BackupCategory.Sms,
            Status: CapabilityStatus.BlockedByOS,
            Reason: "SMS read access requires setting companion as default SMS role on modern Android.",
            RequiredPermission: "Default SMS App Role (Blocked in baseline mode)",
            EstimatedItemCount: 0,
            EstimatedSizeBytes: 0
        );

        report[BackupCategory.CallLogs] = new CapabilityReportItem(
            Category: BackupCategory.CallLogs,
            Status: CapabilityStatus.BlockedByOS,
            Reason: "Call log provider access requires privileged dialer role.",
            RequiredPermission: "Default Dialer Role (Blocked in baseline mode)",
            EstimatedItemCount: 0,
            EstimatedSizeBytes: 0
        );

        // 13. System Settings
        report[BackupCategory.SystemSettings] = new CapabilityReportItem(
            Category: BackupCategory.SystemSettings,
            Status: CapabilityStatus.Limited,
            Reason: "Only non-secure public system settings can be queried. Global system state is OEM-restricted.",
            RequiredPermission: "None",
            EstimatedItemCount: 0,
            EstimatedSizeBytes: 0
        );

        return new CapabilityAssessment
        {
            Id = Guid.NewGuid(),
            DeviceId = device.Id,
            ApiLevel = device.ApiLevel,
            AssessedAtUtc = DateTimeOffset.UtcNow,
            CategoryCapabilities = report
        };
    }
}
