# ARCHEION — Android Platform Research & Feasibility Analysis

**Specification Date:** 2026-11-23  
**Target Platform:** Android 10 (API 29) through Android 16/17 (API 36/37)  
**Operating Principle:** Non-root baseline. Legitimate, documented public APIs only.

---

## 1. Storage & Filesystem Architecture

### 1.1 Scoped Storage (Android 10 - API 29, Android 11 - API 30+)
- **Baseline:** Direct unrestricted access to `/sdcard/` is deprecated and prohibited for normal apps without special access.
- **`Android/data` and `Android/obb` Restrictions:** Starting with Android 11, Google blocked access to `Android/data` and `Android/obb` even through the Storage Access Framework (`ACTION_OPEN_DOCUMENT_TREE`). ARCHEION treats this as an OS-enforced boundary. We do not attempt private directory traversal or bypasses.
- **SAF (Storage Access Framework):**
  - Uses `ACTION_OPEN_DOCUMENT_TREE` or `ACTION_OPEN_DOCUMENT`.
  - The user explicitly grants access to a directory (e.g., `Documents`, `Download`, or a custom folder).
  - Persistable URI permissions must be acquired via `contentResolver.takePersistableUriPermission(uri, takeFlags)`.
  - Revocations or missing volumes must be caught with graceful error handling.

### 1.2 MediaStore & Photo Picker (Android 13 - API 33, Android 14 - API 34+)
- **Read Permissions:**
  - Android 13+: `READ_MEDIA_IMAGES`, `READ_MEDIA_VIDEO`, `READ_MEDIA_AUDIO`.
  - Android 14+: `READ_MEDIA_VISUAL_USER_SELECTED` allows users to grant access to a partial subset of photos/videos. ARCHEION must accurately report `PartialAccess` rather than assuming access to the entire gallery.
  - Legacy (Android 10-12): `READ_EXTERNAL_STORAGE`.
- **Query Strategy:**
  - Query `MediaStore.Images.Media.EXTERNAL_CONTENT_URI`, `MediaStore.Video.Media.EXTERNAL_CONTENT_URI`, `MediaStore.Audio.Media.EXTERNAL_CONTENT_URI`.
  - Extract columns: `_ID`, `DISPLAY_NAME`, `SIZE`, `DATE_MODIFIED`, `DATE_ADDED`, `MIME_TYPE`, `RELATIVE_PATH`.
  - Stream data via `contentResolver.openInputStream(contentUri)`. Never read the whole file into byte arrays.

---

## 2. Personal Information Exporters

### 2.1 Contacts Provider
- **API:** `ContactsContract.Contacts`, `ContactsContract.CommonDataKinds`.
- **Permission:** `android.permission.READ_CONTACTS` (and `WRITE_CONTACTS` for restore).
- **Format:** Portable vCard 3.0 / 4.0 (VCF).
- **Constraints:** Contact photos, structured names, emails, phones, notes, organizations, and postal addresses are supported. Account-specific sync fields (e.g., Google sync tokens) are excluded or normalized.

### 2.2 Calendar Provider
- **API:** `CalendarContract.Events`, `CalendarContract.Instances`, `CalendarContract.Reminders`.
- **Permission:** `android.permission.READ_CALENDAR` (and `WRITE_CALENDAR` for restore).
- **Format:** iCalendar 2.0 (ICS).
- **Constraints:** Recurring events, rule definitions (RRULE), exceptions (EXDATE), reminders, and event attendees must be faithfully serialized. OEM calendar sync quirks must be handled defensively.

---

## 3. Package Management & Applications

### 3.1 Package Visibility (Android 11 - API 30+)
- Apps cannot query all installed packages without declaring `<queries>` or requesting `QUERY_ALL_PACKAGES`.
- `QUERY_ALL_PACKAGES` is restricted on Google Play, but for personal sideloaded utility use, it is permitted if explicitly authorized.
- Alternative via ADB: `pm list packages -f` executed by the desktop host can enumerate package paths and versions directly over USB without requiring companion permissions.
- **APK Extraction:**
  - Base APK path is accessible via `ApplicationInfo.sourceDir` if permitted.
  - Split APKs (`splitSourceDirs`) must be grouped and preserved together.
  - App-private data (`/data/data/<package>/`) is strictly protected by Linux user sandboxing (UID separation) on non-root Android. We do NOT pretend to back up private databases without app-provided export mechanisms.

---

## 4. Communication & Background Work

### 4.1 Background Execution & Foreground Services
- Modern Android aggressively restricts background work:
  - Doze mode & App Standby buckets.
  - Android 14+ requires explicit foreground service types (`dataSync`).
  - Android 15/16 limits foreground service execution time and background starts.
- **Architectural Decision:**
  - Active transfers are user-initiated and bound to a visible Foreground Service with an ongoing notification showing real-time progress and a Cancel action.
  - For long deferrable tasks, use WorkManager with network/charging constraints.

### 4.2 ADB Transport (USB & Wi-Fi)
- Desktop ADB client communicates directly with ADB daemon (`adbd`) on the phone over USB.
- Standard capabilities:
  - Query device properties (`getprop`).
  - File extraction from accessible storage (`adb pull`).
  - Package listing (`pm list packages -f -3`).
  - Reverse/forward socket tunneling (`adb forward` / `adb reverse`) to bridge TCP traffic between companion app and desktop.
