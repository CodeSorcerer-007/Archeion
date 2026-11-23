# ARCHEION — Master Implementation Plan

Implementation roadmap structured as strict verification gates.

---

## Phase Breakdown

### Milestone 0: Environment & Feasibility
- [x] Audit developer environment (Windows 11, .NET 10, Java 21, Android SDK 33-36.1, ADB 1.0.41, Visual Studio 2026).
- [x] Check ADB connectivity and daemon readiness.
- [x] Author comprehensive platform research documentation in `docs/`:
  - `PLATFORM_RESEARCH.md`
  - `CAPABILITY_MATRIX.md`
  - `ARCHITECTURE.md`
  - `SECURITY.md`
  - `BACKUP_FORMAT.md`
  - `PROTOCOL.md`
  - `TESTING.md`
  - `USER_GUIDE.md`
  - `DEVICE_COMPATIBILITY.md`
- [x] Define Protocol and Manifest JSON Schemas in `schemas/`.
- [x] Acceptance Gate: All documentation and schemas completed and verified.

### Milestone 1: Contracts, Schemas & Domain Primitives
- [x] Create shared contract and model libraries:
  - `shared/Archeion.Contracts`
  - `windows/src/Archeion.Domain`
  - `windows/src/Archeion.Security`
  - `windows/src/Archeion.Persistence`
  - `windows/src/Archeion.ObjectStore`
- [x] Implement cryptographic engine:
  - AES-256-GCM streaming encryption with authenticated associated data (AAD).
  - PBKDF2 / Argon2id key derivation with high iteration count (600,000 iterations).
  - 256-bit emergency recovery key generator and parser.
- [x] Implement content-addressable object store (CAS) with staging, deduplication, and atomic promotion.
- [x] Implement SQLite schema migrations (v1) with foreign keys and index optimization.
- [x] Acceptance Gate: Passing automated unit, security, and storage tests.

### Milestone 2: Transport & Discovery Subsystems
- [x] Implement `windows/src/Archeion.Adb`:
  - Socket transport for local ADB server daemon (TCP port 5037).
  - Fallback execution using `adb.exe`.
  - Device enumeration, property queries, package list retrieval.
- [x] Implement `windows/src/Archeion.DeviceDiscovery`:
  - Unified endpoint discovery (USB ADB + Network Companion).
  - Deterministic device ID generation via multi-attribute hardware fingerprinting.
- [x] Acceptance Gate: Endpoint discovery and identity derivation validated.

### Milestone 3: Capability & Policy Engine
- [x] Implement `windows/src/Archeion.CapabilityEngine`:
  - Truthful capability matrix evaluator covering Android 5 through Android 16.
  - Scoped storage rules (Android 10 Q, Android 11 R).
  - Category-by-category strategy compilation with explicit limitations and zero fabricated claims.
- [x] Acceptance Gate: Capabilities reflect observed OS/permission facts, never assumptions.

### Milestone 4: Repository Foundation & Integrity Engine
- [x] Implement `windows/src/Archeion.Application`:
  - Repository lifecycle: `CreateRepositoryAsync`, `OpenRepositoryAsync`, `Lock`, `Unlock`.
  - Encrypted repository envelope and master key management.
  - Unencrypted mode with explicit warning flag.
  - Full object hash verification, database consistency check, orphaned object detection.
- [x] Acceptance Gate: Reopening repository verifies cleanly; wrong password fails cleanly; staging rollback tested.

### Milestone 5: First Real Backup Vertical Slice
- [x] Implement `windows/src/Archeion.BackupEngine`:
  - Immutable Backup Plan builder from user selection and capability audit.
  - Preflight checks (free space, repository health, device readiness).
  - Streaming collection pipeline: Source extraction → Chunking → SHA-256 computation → Encryption (if enabled) → Atomic object promotion.
  - Atomic snapshot commit to SQLite and `manifest.json`.
- [x] Implement Backup Explorer & Item Exporter:
  - Snapshot navigation, item search, filter by category and verification status.
  - Safe export to destination directory with original relative paths and SHA-256 verification.
- [x] Acceptance Gate: End-to-end backup of test media/files, verification of hashes, export matches byte-for-byte.

### Milestone 6: Android Companion App & Protocol Bridge
- [x] Scaffold Android Companion in `android/`:
  - Jetpack Compose + Material 3 UI.
  - Coroutines + Foreground service (`dataSync`).
  - SAF document tree picker & MediaStore query providers.
- [x] Implement protocol bridge over ADB reverse/forward socket and LAN TCP.
- [x] Cross-platform protocol tests verifying C# and Kotlin handle identical packets and schemas.
- [x] Acceptance Gate: Companion app connects to desktop, reports live permissions, and streams test payload with chunk ACKs.

### Milestone 7: Extended Exporters, App Inventory & APK Extraction
- [x] Contacts VCF exporter/importer using Android Contacts Provider (RFC 6350).
- [x] Calendar ICS exporter using Android Calendar Provider (RFC 5545).
- [x] App inventory metadata and accessible base APK extraction (via PackageManager / ADB).
- [x] Acceptance Gate: Clear separation between APK binaries and app-private data; all tests passing.

### Milestone 8: Snapshot Comparison, Health & Portable Packaging
- [x] Two-snapshot differential engine (`Added`, `RemovedFromSource`, `Modified`, `Unchanged`, `Renamed`).
- [x] Deduplication & incremental object reuse across snapshots.
- [x] Acceptance Gate: Change detection accurately identifies file edits/deletions without false positives.

### Milestone 9: Restore Engine & Migration Planning
- [x] Restore destination capability probing.
- [x] Destination conflict analysis (preview overwrite, skip, rename, merge).
- [x] Selective restore execution with validation and audit log.
- [x] Acceptance Gate: File/media restoration to destination verified; destructive replacement safeguarded.

### Milestone 10: WinUI 3 Desktop App, Scheduling & Release Packaging
- [x] Complete WinUI 3 desktop shell:
  - Navigation: Overview, Devices, Backup, Backups & Snapshots, Backup Explorer, Compare Snapshots, Restore & Migration, Activity, Diagnostics, Settings.
  - High-DPI, dark/light theme, accessible controls.
  - Fully bound to Application & Domain services (no business logic in code-behind).
- [x] Backup-on-connect trigger & retention manager configuration.
- [x] Final security audit, hardware test matrix documentation, and release readiness verification.
