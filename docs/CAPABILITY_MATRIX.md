# ARCHEION — Android Capability Matrix

**Policy:** Capability status is determined by live device probes and runtime permissions. No assumptions are made based solely on device model.

---

## 1. Capability Definitions

| Category | Typical Method | Required Permissions / User Action | Capture Status | Restore Status | OS Restrictions |
|---|---|---|---|---|---|
| **Device Identity & Hardware** | ADB `getprop` / Android `Build` API | None / ADB Authorized | Available | Read-Only | MAC address & IMEI restricted on Android 10+; use fallback fingerprinting |
| **Photos & Images** | MediaStore / Photo Picker / ADB pull | `READ_MEDIA_IMAGES` / User selection | AvailableWithPermission | Supported | Partial access on Android 14+ must be reported truthfully |
| **Videos** | MediaStore / Photo Picker / ADB pull | `READ_MEDIA_VIDEO` / User selection | AvailableWithPermission | Supported | Partial access on Android 14+ supported |
| **Audio & Music** | MediaStore / SAF | `READ_MEDIA_AUDIO` / User selection | AvailableWithPermission | Supported | Standard audio formats supported |
| **Documents & Downloads** | Storage Access Framework / MTP / ADB | SAF `ACTION_OPEN_DOCUMENT_TREE` | AvailableWithUserAction | Supported | User must grant directory tree access |
| **Custom User Folders** | Storage Access Framework (SAF) | User folder picker | AvailableWithUserAction | Supported | Restricted roots (`Android/data`, root sdcard) excluded by OS |
| **Contacts** | Contacts Provider | `READ_CONTACTS` (write for restore) | AvailableWithPermission | Supported (Merge/Skip) | Exported to vCard (VCF). Restore offers conflict preview |
| **Calendar** | Calendar Provider | `READ_CALENDAR` (write for restore) | AvailableWithPermission | Supported (Merge/Skip) | Exported to iCalendar (ICS). Recurring event rules supported |
| **Installed App Inventory** | PackageManager / ADB `pm list` | `<queries>` or ADB Authorized | Available | Informational / Batch Reinstall | Lists package name, version, installer, APK paths |
| **APK Binaries (Non-Split)** | `ApplicationInfo.sourceDir` / ADB pull | Permission or ADB Authorized | Available | Supported (via PackageInstaller) | Unrestricted APKs only; protected OEM APKs may fail extraction |
| **Split APK Bundles** | Base + Split APK paths | Permission or ADB Authorized | Available | Supported (Split Install) | Must be captured and installed as an atomic bundle |
| **App-Private Data** | None (Non-root) | Root / Exploit required | **BlockedByOS** | **BlockedByOS** | Strictly protected by Linux sandbox (UID isolation). Honest status: Not supported without root |
| **SMS & Call Logs** | SMS Provider / CallLog Provider | Default SMS app role / privileged | **BlockedByOS** | **BlockedByOS** | Defer in default mode; requires switching default SMS handler |
| **Wi-Fi Passwords** | Wi-Fi Manager | Root / System Certificate | **BlockedByOS** | **BlockedByOS** | Stored in keystore/wpa_supplicant protected partition |
| **System Settings** | Settings Provider | `WRITE_SECURE_SETTINGS` / Privileged | **Limited** | **Limited** | Only public settings supported; global system state is OEM-specific |
