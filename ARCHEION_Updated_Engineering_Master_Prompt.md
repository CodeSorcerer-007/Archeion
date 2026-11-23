# ARCHEION — Personal Android Backup, Preservation & Recovery System
## Updated Engineering Master Prompt (v2.0)

> **Working product name:** ARCHEION  
> **Brand naming convention:** Always spell the product name `ARCHEION` (all caps in product prose and UI branding). Use `Archeion` for C#/.NET type, namespace, solution, and project identifiers; use `archeion` for Android package path segments and lowercase technical identifiers. Do not retain the former working name in user-facing text, project names, namespaces, package identifiers, repository defaults, or documentation.  
> **Product owner / primary user:** The person running this coding workspace  
> **Primary purpose:** Build a genuinely functional, dependable, free, private Android backup and recovery tool for personal use.  
> **Target coding environment:** Antigravity with Gemini (or an equivalent coding agent)  
> **Specification date:** 23 November 2026  
> **Primary platforms:** Windows desktop + native Android companion  
> **Operating principle:** Functionality, data integrity, recoverability, and honest capability reporting take priority over feature count, market positioning, or visual effects.

---

# 0. Mission and non-negotiable product intent

Build **ARCHEION**, a real Windows application and native Android companion that let the owner discover Android devices, inspect their actual backup capabilities, preserve accessible data in a local backup library, verify that data, explore historical snapshots, restore supported data, and migrate supported data between Android devices.

ARCHEION is a **personal-use project**. It is not being built to compete with commercial products, establish a market category, attract customers, or generate revenue. Do not spend time on competitor research, market differentiation, sales positioning, monetization, subscriptions, accounts, advertising, analytics, growth features, or cloud infrastructure.

The software must be free to use by its owner. It must not contain:

- advertisements, paid tiers, subscriptions, usage quotas, or feature gates;
- mandatory accounts, registration, licensing servers, activation, or online checks;
- telemetry, analytics, behavioural tracking, or automatic diagnostic uploads;
- cloud backup, cloud sync, or a remote backend as a dependency;
- artificial restrictions intended to create a paid upgrade path.

Core backup, repository access, browsing, verification, restore planning, and supported restore operations must work offline. Internet may be used by the developer to download SDKs, dependencies, or updates, but it must not be required by the finished product's core workflows.

The project may later be shared with other people, but public distribution, Google Play publication, commercial readiness, multi-tenant support, and customer support are **not current requirements**. Do not let hypothetical public-release requirements delay personal functionality. Still use safe APIs, proper permission handling, secure cryptography, and clean engineering because the software will handle irreplaceable personal data.

## 0.1 Definition of a successful product

ARCHEION is successful when the owner can repeatedly complete this real workflow:

1. Launch the Windows application.
2. Connect an Android phone through an actually supported transport.
3. Discover and identify the physical phone accurately, or show a truthful partial/unknown-device state.
4. Probe what data is available on that exact phone and Android build.
5. See a clear capability and permission report before collection.
6. Select the data categories to back up.
7. Back up real user-authorized data without silently omitting inaccessible items.
8. Verify stored content using cryptographic hashes and repository checks.
9. Close and reopen ARCHEION and still see the repository, device, snapshots, and backup contents.
10. Browse and export captured items without reconnecting the phone.
11. Create later snapshots and compare what changed.
12. Restore supported data to a destination with a preview, confirmation, and post-restore validation.
13. Recover safely from disconnects, application restarts, external-drive removal, interrupted transfers, and partial failures.

A feature is not complete because its page exists. It is complete only when its real workflow, error states, persistence, tests, and documentation work.

## 0.2 Core product principles

1. **Functionality before presentation.** Build working end-to-end vertical slices, not a beautiful mock application.
2. **No fake completeness.** Never invent device data, backup contents, progress, throughput, capability, success, or restore results.
3. **Non-root baseline.** Do not root devices, unlock bootloaders, exploit vulnerabilities, bypass Android permission prompts, or use privileged methods without explicit supported user authorization.
4. **Capability-aware operation.** Device model alone never proves that a category can be collected. Probe the connected device and current permission state.
5. **Local-first.** The user's backup library belongs to the user and is stored at a user-selected path, not inside the application's installation directory.
6. **Recoverability over cleverness.** Prefer a straightforward, testable backup format and reliable recovery over premature optimization.
7. **Safe restore.** Preview changes, detect conflicts, preserve destination data by default, and ask before destructive operations.
8. **Real-device evidence.** Emulator and fixture tests are useful but must not be represented as hardware validation.
9. **Portable data.** Document the repository and snapshot formats so the owner can move or inspect their own backups.
10. **No unnecessary complexity.** Use a modular monolith on Windows and a modular Android app. Do not create microservices or empty abstractions.

---

# 1. Instructions to the coding agent

Act as the senior Windows engineer, Android platform engineer, security engineer, storage/database engineer, networking engineer, QA engineer, and technical lead responsible for delivering the software in the current repository.

## 1.1 Inspect before changing anything

Before editing:

- inspect the entire workspace and repository structure;
- inspect Git status, branches, uncommitted changes, and existing project history;
- inspect installed .NET SDKs/runtimes, Visual Studio/build tools, Windows SDKs, Windows App SDK availability, Java/JDK, Android SDKs, Android Studio/Gradle, ADB, and relevant environment variables;
- inspect existing code, build scripts, dependency versions, tests, documentation, and generated files;
- identify the actual Windows version/build and whether any requested target is unsupported;
- preserve useful existing work; never scaffold over a working project or delete unrelated files;
- record findings in `docs/ENVIRONMENT_AUDIT.md`.

Do not assume an SDK/runtime is installed because the specification mentions it. Do not download or install software without first checking what is already available and whether installation is necessary.

## 1.2 Plan, then execute

Create and maintain:

- `docs/IMPLEMENTATION_PLAN.md`
- `docs/STATUS.md`
- `docs/ENVIRONMENT_AUDIT.md`
- `docs/PLATFORM_RESEARCH.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/ARCHITECTURE.md`
- `docs/SECURITY.md`
- `docs/BACKUP_FORMAT.md`
- `docs/PROTOCOL.md`
- `docs/TESTING.md`
- `docs/USER_GUIDE.md`

Break work into small milestones with acceptance criteria. **Do not stop after writing a plan.** Start implementing the first end-to-end vertical slice immediately and continue through feasible milestones without waiting for another user message.

Only pause for a genuinely necessary decision, unavailable physical hardware, required credentials, or a platform limitation that cannot safely be resolved from the specification. Do not ask questions already answered here. Make sensible reversible choices and document them.

## 1.3 Build and test continuously

After every meaningful implementation slice:

1. build the affected project;
2. run the relevant unit and integration tests;
3. inspect compiler output, warnings, test failures, and runtime logs;
4. fix regressions before continuing;
5. update the implementation status with exact evidence.

Never defer all compilation until the end. Do not suppress errors, swallow non-zero exit codes, mark skipped tests as passed, or claim success because a command was launched.

## 1.4 Implement real behavior

- Every visible button, menu item, toggle, filter, and command must perform its labelled action.
- No hard-coded connected devices or fake backup history in production mode.
- Demo/preview data is allowed only in an explicitly labelled demo mode that is isolated from real repository operations.
- Progress must be based on real stages, bytes, files, or completed tasks. Use indeterminate progress where reliable estimates are unavailable.
- A zero-item result must be distinguished from denied permission, unsupported API, failed provider, skipped category, and not-yet-tested category.
- Never show a category as backed up simply because an exporter class or UI card exists.
- Never claim a restore succeeded without checking the result of the actual operation.

## 1.5 Research platform constraints before implementing collectors

Before making any Android data-access claim, research current official Android documentation and, when relevant, source code or documented OEM APIs. Record source URLs, access date, affected API levels, test evidence, and interpretation in `docs/PLATFORM_RESEARCH.md`.

Use current stable APIs and SDKs. Do not target preview/beta Android APIs in the production build. Preview versions may be used in a separate compatibility test branch only. Re-check current stable documentation before implementation because Android permission, storage, network, and background-execution rules change.

Explicitly investigate:

- scoped storage and Storage Access Framework restrictions;
- MediaStore and Photo Picker / selected-media access;
- contacts and calendar providers;
- installed-package visibility and APK access;
- ADB authorization and supported ADB commands;
- MTP behavior and Windows device integration;
- local-network permissions and discovery restrictions on current Android versions;
- foreground services, user-initiated data-transfer jobs, and WorkManager;
- app backup and restore limitations;
- OEM-specific interfaces only when publicly documented and safely testable.

Do not use deprecated `adb backup` as a general backup mechanism. Do not use hidden APIs, privileged shell tricks, root, bootloader unlocks, vulnerability exploits, or undocumented filesystem paths as a substitute for supported access.

## 1.6 No destructive actions without explicit safeguards

- Do not delete, move, rename, or overwrite source phone content during backup.
- Do not delete an existing repository, snapshot, or destination file as a side effect of creating a new backup.
- Do not automatically overwrite destination phone data during restore.
- Preview all conflicts and irreversible operations.
- Preserve destination data by default; where overwrite is unavoidable, require clear confirmation.
- Make cancellation and crash recovery safe.
- Do not automatically clean up uncertain files that might be the only valid copy of user data.

## 1.7 Finish every work session with truthful status

Update `docs/STATUS.md` and report:

- features actually implemented;
- exact builds and tests run, with pass/fail/skip counts;
- what works on emulator/fixtures;
- what has been tested on real hardware and exact device/Android version;
- known limitations and blockers;
- next concrete milestone;
- exact commands to run or reproduce the tests.

Do not report a feature as complete if it is only partially implemented.

---

# 2. Product scope and supported operating model

## 2.1 Two coordinated applications

### Windows application — ARCHEION Desktop

The Windows application is the primary command centre and owns:

- device discovery and registry;
- ADB/MTP/local-network transport orchestration;
- device identity and capability assessment;
- backup plan creation and job coordination;
- repository creation, unlock, encryption, migration, and health;
- object storage, snapshot manifests, metadata database, and integrity verification;
- Backup Explorer, snapshot comparison, and exports;
- restore and cross-device migration planning/execution;
- scheduling, activity history, diagnostics, and settings.

### Android application — ARCHEION Companion

The Android application is a first-class companion and owns:

- device-side identity details and capability reporting;
- explicit pairing and trust management;
- permission requests with explanations;
- user-mediated access to selected files/folders;
- Android API-based exporters such as media, contacts, and calendar where supported;
- coordination of transfers to the trusted Windows host;
- chunk acknowledgements, checksums, resumability where possible, and transfer status;
- device-side history, diagnostics, and permission revocation controls.

The companion is not a hidden system agent. It must remain useful when permission is denied, the Windows host is absent, or pairing is revoked.

## 2.2 Local-first operational modes

Support these modes as implementation permits:

1. **USB / ADB mode:** Windows detects authorized Android devices and communicates through supported ADB operations. ADB authorization must be approved on the phone.
2. **USB / MTP mode:** Windows uses normal device file-transfer interfaces for content exposed by the phone. Do not assume MTP exposes every file.
3. **Companion USB mode:** The Android app participates in exporting data through its own public APIs and transfers it to Windows over a verified bridge.
4. **Trusted Wi-Fi/LAN mode:** Windows and Android connect after explicit authenticated pairing. No sensitive data or export operation is available to an unauthenticated LAN peer.
5. **Offline repository mode:** The owner can unlock and browse, verify, compare, export, and plan restores from a backup library without an active phone connection.

Do not force every transport into the first release. Implement the transport abstraction, then deliver and test one reliable transport end to end before expanding.

## 2.3 Non-goals and hard limitations

ARCHEION does not promise:

- a bit-for-bit image of an Android phone;
- full system partition, boot, modem, or firmware backups;
- universal extraction of app-private databases or encrypted app data;
- extraction of passwords, private keys, payment tokens, protected credentials, or another app's secure storage;
- access to every SMS/call log on every Android device;
- restoration of all device settings or OEM launcher/system state;
- an installable APK for every app regardless of package restrictions;
- an unrestricted copy of `Android/data` or `Android/obb`;
- compatibility with every phone model without hardware testing.

If a requested feature is not feasible through legitimate, documented, non-root methods, report it as restricted/unavailable, implement a safe adjacent workflow if useful, and continue with other work.

---

# 3. Technical stack and target platforms

Use the following baseline unless workspace inspection or platform research identifies a concrete incompatibility. Document any deviation and its reason.

## 3.1 Windows desktop

- **Language/runtime:** C# with .NET 10 LTS.
- **UI:** WinUI 3 with the current supported stable Windows App SDK at implementation time.
- **Architecture:** MVVM with dependency injection and clear Domain/Application/Infrastructure/Presentation boundaries.
- **Persistence:** SQLite with migrations.
- **Transport:** ADB/platform tools, Windows MTP integration where feasible, and authenticated local-network transport.
- **Target OS:** Windows 11 is the primary supported development and runtime target.
- **Windows 10:** Retain compatibility only where the selected .NET runtime, Windows App SDK, packaging mode, and actual OS edition are supported. Current Microsoft .NET support information may limit modern .NET support on Windows 10 to LTSC/Enterprise editions. Do not claim maintained support for Windows 10 Home/Pro 22H2 merely because WinUI itself can run on older Windows builds. If the development PC is Windows 10 Home/Pro, test the exact runtime/package combination and document the real result before promising compatibility.
- **SDK choice:** Use the latest stable Windows App SDK release that is compatible with installed tooling; never use Experimental/Preview for the production application. Check Microsoft release/support matrices at build time.
- **Packaging:** Prefer a reliable self-contained or otherwise clearly documented deployment path. The owner should not need to install development tooling to run the packaged desktop app.

Use asynchronous I/O, cancellation tokens, bounded concurrency, structured logging, and responsive UI state. Device commands and repository operations must not execute directly inside view code.

## 3.2 Android companion

- **Language:** Kotlin.
- **UI:** Jetpack Compose and Material 3, with a custom ARCHEION design system.
- **Minimum OS:** Android 10 / API 29 unless an inspected dependency or required API forces a higher minimum. Any increase requires a written reason.
- **Compile/target SDK:** Use the latest stable Android SDK/API level available at implementation time. As of this specification date Android 17 / API 37 is documented as stable; verify the current stable channel at implementation time. Do not target preview or beta SDKs in the release build.
- **Build:** Gradle Kotlin DSL and version catalog, with dependency versions pinned and checked for compatibility.
- **Architecture:** UI/state holders, domain use cases, exporters, permission manager, pairing manager, transfer engine, and verification layer.
- **Install:** Provide a debug APK and clear ADB install instructions for personal sideloading. Do not make Google Play publication a requirement.

Request permissions only when the user starts a feature requiring them. Explain each permission in context. Handle grant, denial, partial grant, revocation, and permission auto-reset.

## 3.3 Shared code and contracts

The Windows and Android projects live in one repository and share versioned protocol schemas, message definitions, backup format documentation, and cross-platform test vectors.

Do not attempt to share all business logic across C# and Kotlin. Share the protocol and file-format contracts; implement idiomatic platform-specific code with compatibility tests.

---

# 4. Android capability and feasibility model

A correct capability engine is essential. It must assess the connected device's actual state rather than assuming a feature from manufacturer or Android version alone.

## 4.1 Capability categories to evaluate

| Category | Initial access strategy | Restrictions / handling |
|---|---|---|
| Device identity | Android public APIs, ADB properties where authorized, MTP descriptors | Some identifiers unavailable or restricted; never treat one ID as universally stable |
| Device/build information | `Build` fields, package/system information, authorized ADB properties | Collect only useful fields; protect sensitive identifiers |
| Photos | MediaStore, Photo Picker, selected-media permission flow | Partial access must be represented accurately |
| Videos | MediaStore, Photo Picker, selected-media permission flow | Partial access must be represented accurately |
| Audio | MediaStore and user-selected files/folders | Respect scoped storage and permissions |
| Documents/downloads | Storage Access Framework, MTP where exposed | A selected directory is not access to the entire phone |
| User-selected folders | SAF `ACTION_OPEN_DOCUMENT_TREE` and persistable URI permissions | Android restricts important directories and roots; do not imply unrestricted filesystem access |
| Contacts | Contacts Provider with explicit read permission | Export only fields actually returned; protect personal information |
| Calendar | Calendar Provider with explicit permission, if supported by the device | Verify access and recurrence/exception handling; treat provider differences carefully |
| Installed app inventory | PackageManager with minimal package visibility declarations | Do not assume all installed packages are visible |
| APK export | Package metadata and accessible APK path where available | APK package is not app-private data; splits and install constraints must be reported |
| App-private data | App-supported export/import, documented participation, or platform-supported app backup | Never claim generic extraction of another app's private sandbox |
| SMS | No general default exporter | Defer unless the user deliberately changes the relevant default role and supported APIs/policies permit it; never bypass role/permission restrictions |
| Call logs | No general default exporter | Same restriction as SMS; not part of initial release |
| Wi-Fi credentials | No general exporter | Do not claim saved passwords or protected network credentials are exportable |
| System settings | Individual public APIs only, after verification | No universal settings backup or restore |
| Wallpaper | User-selected file or documented public interface where available | Do not claim full OEM wallpaper state unless verified |
| App-specific exports | App-provided file/API/integration | Each integration is separate and opt-in |
| Storage statistics | Public storage APIs or measured repository data | Show exact source and whether the value is approximate |

## 4.2 Capability statuses

Use explicit states rather than a simple supported/unsupported boolean:

- `Available`
- `AvailableWithPermission`
- `AvailableWithUserAction`
- `Limited`
- `Unsupported`
- `BlockedByOS`
- `BlockedByOEM`
- `NotInstalled`
- `NotConnected`
- `NotTested`
- `PermissionDenied`
- `PermissionRevoked`
- `PartialAccess`
- `Error`

For each category, record:

- source/API/transport;
- Android/API and OEM conditions;
- permission or user action required;
- estimated vs measured item count/size;
- actual export format;
- capture status;
- restore status;
- test level (unit, emulator, physical device);
- limitations and user-facing explanation;
- last assessed timestamp;
- capability confidence/evidence.

## 4.3 Android storage rules

- Prefer SAF for user-selected files and folders where applicable.
- Persist URI permissions when the system grants persistable access, and handle revoked or invalid URIs.
- Use MediaStore for shared media as appropriate.
- Support Android 14+ selected photo/video access and refresh data after permission changes; do not cache permission assumptions indefinitely.
- Respect scoped storage. Do not try to access app-private directories of other packages.
- Do not request `MANAGE_EXTERNAL_STORAGE` just to avoid implementing SAF/MediaStore correctly. Only consider it after current official documentation confirms the use case is legitimate, technically necessary, and safe for private sideloading; document the trade-offs and make it opt-in.
- Do not use SAF to claim access to protected directories that Android explicitly excludes.
- Do not interpret `Android/data` and `Android/obb` restrictions as a bug to bypass.
- Do not use broad permissions if narrower APIs satisfy the requirement.

## 4.4 App inventory and APK exports

- Declare only the package visibility needed for the actual inventory workflow.
- First investigate targeted `<queries>` declarations and APIs.
- Do not add `QUERY_ALL_PACKAGES` by default.
- If broad visibility is genuinely required for the owner's inventory feature, explain why, assess current platform rules, and make the permission request explicit.
- Distinguish package name, version, installer, enabled state, split APKs, base APK path, and export result.
- Never label an APK export as an application-data backup.
- Restore/installing an APK must use supported Android install flows, user confirmation, and normal package-signature rules. Do not silently enable unknown-source installation or downgrade protections.

## 4.5 Contacts and calendar exporters

- Implement only after testing public provider behavior on supported Android versions.
- Request read/write permissions only for the action being performed.
- Use portable, documented formats (for example VCF for contacts and ICS where the implementation correctly supports calendar semantics).
- Preserve available fields and metadata; do not invent missing data.
- Handle duplicate contacts, recurring calendar events, recurrence exceptions, time zones, attendees, reminders, and provider-specific fields carefully.
- Restore should default to insert/merge with duplicate preview, not destructive replacement.
- Test actual export/import round trips on a physical device.

## 4.6 Auto Backup clarification

Android's Auto Backup is an app/platform-managed facility for an app's own data. It is not a universal third-party mechanism for extracting other applications' private data. ARCHEION must not treat it as a generic phone backup API or promise that an owner can read another app's cloud backup dataset.

---

# 5. Core user workflows

Implement these workflows as real application flows, not disconnected screens.

## 5.1 First launch and repository setup

1. Show a concise explanation that backups remain local to the selected library.
2. Let the user create or open a repository.
3. Choose a library path on an internal or external drive.
4. Validate directory permissions, free space, filesystem suitability, and path availability.
5. Choose encrypted or explicitly unencrypted repository mode.
6. For encrypted mode, create and verify a recovery method before accepting backup data.
7. Create repository metadata and database atomically.
8. Reopen the repository and verify it can be read.
9. Show a useful empty state with next actions.

Do not require account creation or Internet access.

## 5.2 Device discovery and registration

1. Detect USB/ADB/MTP endpoints and trusted companion sessions.
2. Deduplicate endpoints that belong to the same physical phone.
3. Read available identity fields and record each field's source/confidence.
4. Match against known device records.
5. If the match is uncertain, ask the user to confirm whether this is an existing device or a new device.
6. Probe capabilities and permissions.
7. Store the device profile and capability assessment.
8. Re-probe when relevant firmware/build/permission state changes.
9. Handle disconnects and reconnects without corrupting job state.

Device states:

- `Discovering`
- `Connecting`
- `Identifying`
- `Identified`
- `PartialIdentity`
- `UnknownDevice`
- `IdentityConflict`
- `Connected`
- `Disconnected`
- `Unauthorized`
- `Error`

## 5.3 Backup plan preview

Before any backup job:

- select source device;
- refresh identity/capabilities if stale;
- display each category and its current capability status;
- select categories and optional folders;
- show required permissions and user actions;
- show exclusions and known limitations;
- display measured or clearly labelled estimated counts/sizes only where grounded;
- show required repository space and a safety margin if calculable;
- explain what will be captured and what cannot be captured;
- freeze the resulting plan as an immutable job input.

The plan must distinguish `NoDataFound`, `Unavailable`, `NotSelected`, `Skipped`, `PermissionDenied`, `Failed`, and `Collected`.

## 5.4 Backup execution

Use real stages:

1. preflight and repository check;
2. permission and source validation;
3. enumeration;
4. collection/export;
5. streaming and chunking;
6. hashing;
7. storage/encryption;
8. verification;
9. manifest validation;
10. atomic snapshot commit;
11. summary/report generation.

Requirements:

- cancellation at safe boundaries;
- bounded retries with backoff for transient errors;
- resumability only when the source and saved partial state can be validated;
- no unbounded memory buffering;
- stream large files and apply bounded queues/backpressure;
- clean up incomplete staging data safely;
- do not finalize a snapshot as complete before verification and commit;
- allow a partial snapshot with explicit per-category outcomes when some categories fail;
- never report fake percentage or speed.

## 5.5 Backup Explorer

- Browse devices, snapshots, categories, directories, and items.
- Search by file name, logical path, category, device, snapshot, date, size, and file type.
- Filter by successful, partial, unavailable, or verification state.
- Preview safe supported media and documents.
- Export selected items to a user-chosen destination.
- Preserve original logical paths where captured, with a safe export path mapping.
- Display provenance: source device, snapshot, category, captured time, source provider, size, and hash/verification status.
- Never show uncaptured files as if they exist in the backup.

## 5.6 Snapshot comparison

Implement a real comparison between two snapshots for the same device or compatible logical source.

Classify items as:

- `Added`
- `RemovedFromSource`
- `Modified`
- `Unchanged`
- `Renamed` only when identity evidence is strong enough
- `Unknown` when the comparison cannot prove a result

Compare stable item IDs, normalized paths, size, hashes, and reliable metadata. Do not infer that a file was deleted from the phone simply because it is absent in a partial or permission-limited snapshot.

Allow the user to:

- filter by change type/category;
- preview old/new versions where supported;
- export a historical version;
- restore a selected historical version;
- inspect comparison evidence.

## 5.7 Restore workflow

1. Unlock repository.
2. Select source device/snapshot/items.
3. Connect and identify target if the restore requires a phone.
4. Probe target capabilities.
5. Build a destination-aware restore plan.
6. Map source logical categories to supported destination actions.
7. Preview conflicts, duplicates, overwrites, permissions, missing destinations, and irreversible actions.
8. Let the user choose merge/skip/rename/replace where supported.
9. Require clear confirmation for destructive or sensitive operations.
10. Execute with progress, cancellation, retry, and failure handling.
11. Verify restored data where possible.
12. Generate a report listing restored, skipped, failed, partial, and unverified items.

Never claim universal device restore. Start with file/media restoration and other categories only when their actual import interfaces are implemented and tested.

## 5.8 Cross-device migration

Treat migration as a source-to-destination restore plan, not as copying OEM backup formats blindly.

- identify source and destination devices;
- compare their Android versions and capabilities;
- map portable categories;
- preview supported, partial, user-action, unsupported, conflict, and unverified items;
- preserve source data;
- support selective migration;
- report every category that could not be migrated;
- validate the destination result.

## 5.9 Backup scheduling and automatic actions

Scheduling is valuable but must respect real OS/transport constraints.

Windows-side options:

- manual backup;
- backup when a known device connects, only after the owner explicitly enables it for that device;
- optional scheduled checks/backups while the Windows user session and required transport are available;
- optional startup/tray operation, off by default;
- per-device schedule and category preferences;
- last successful backup and overdue status;
- configurable retention.

Android-side options:

- use WorkManager for deferrable work that can tolerate system scheduling;
- use a user-initiated data-transfer mechanism where the current OS version supports it and the transfer is directly initiated by the user;
- use a foreground service only when required, permitted, correctly typed, user-visible, and compliant with current restrictions;
- do not promise exact-time background execution;
- do not assume a USB connection, unlocked phone, permission, Wi-Fi session, or Windows receiver will remain available;
- never attempt to evade background execution limits.

A scheduled backup that cannot run must record why it did not run and provide a useful notification/history entry; it must not fabricate a successful backup.

---

# 6. Backup repository and storage engine

The repository is a first-class subsystem. Its design must prioritize integrity, safe reopening, portability, and recoverability.

## 6.1 Repository requirements

- One configurable repository root path.
- Internal and removable/external drive support.
- Safe repository relocation with validation and post-copy verification.
- Repository format version and explicit migrations.
- SQLite metadata database with schema migrations.
- Content object store separate from the database.
- Snapshot manifests with schemas.
- Staging directory for incomplete jobs.
- Atomic commit where the filesystem permits it.
- Recovery after crash, power loss, or application termination.
- Orphan-object discovery and safe cleanup.
- Integrity scanner and repository health report.
- Snapshot history and configurable retention.
- Export/import of portable snapshots.
- Missing-drive detection and clean recovery when the drive is reconnected.
- Never store backup payloads inside the application installation folder.

## 6.2 Suggested repository layout

Adapt this only with a documented reason:

```text
ArcheionLibrary/
├── repository.json
├── database/
│   └── library.sqlite
├── keys/
│   └── key-envelope metadata only (never raw password)
├── devices/
│   └── <device-uuid>/
│       ├── profile.json
│       ├── capabilities.json
│       └── snapshots/
│           └── <snapshot-id>/
│               ├── manifest.json (or encrypted manifest object)
│               └── report.json
├── objects/
│   └── <object-id-prefix>/<object-id>
├── staging/
│   └── <job-id>/
├── exports/
├── reports/
├── logs/
└── repository-checks/
```

Do not assume a drive letter or fixed path. Store relative object references safely and validate all paths when opening a repository.

## 6.3 Object storage

- Stream source content to temporary staging files.
- Compute a content hash while streaming.
- Validate expected byte count only when a trustworthy size is known.
- Use stable object IDs and metadata.
- Atomically promote verified objects into the object store.
- Maintain reference counts or a reliable reachability index.
- Avoid reading an entire large backup into memory.
- Handle duplicate content deliberately.
- Ensure object deletion is permitted only when no snapshot references it and retention rules have been applied.

## 6.4 Incremental backup and deduplication

Implement in stages:

1. First deliver correct full snapshots.
2. Add change detection using source metadata and hashes where source access allows.
3. Add parent-snapshot relationships and unchanged-object reuse.
4. Add retention and garbage collection after reference integrity tests.
5. Consider deduplication only after its privacy, encryption, crash-recovery, and correctness implications have been reviewed.

Do not claim a backup is incremental if the source must be fully reread and compared. Label the actual work performed. Avoid cross-repository or cross-user convergent encryption. Deduplication must never weaken encryption or make deletion/retention unsafe.

## 6.5 Snapshot manifest

Create a versioned JSON schema. A manifest should contain:

- schema and repository format versions;
- snapshot UUID and device UUID;
- source device identity with sensitive fields sanitized/encrypted as needed;
- creation time, UTC timestamp, and original timezone information;
- Android version/API/build information when available;
- adapter/profile IDs and versions;
- parent snapshot reference for incremental snapshots;
- backup plan reference and selected categories;
- per-category results and limitations;
- items and object references;
- logical paths and original timestamps only when actually captured;
- measured sizes and cryptographic hashes;
- verification state;
- encryption metadata without secrets;
- restore compatibility metadata;
- collection evidence/test state;
- complete/partial/failed status.

Validate every manifest before browsing, importing, or restoring. Handle unknown newer schema versions safely and read-only where possible. Never destructively rewrite an unknown format.

## 6.6 Portable export/import

Provide a documented portable snapshot or repository export that can be copied to another drive/computer and imported into another ARCHEION installation.

Requirements:

- include manifest, required objects, format version, and integrity information;
- include encryption metadata but never plaintext secrets;
- verify every object before import;
- reject path traversal, malformed manifests, oversized entries, and incompatible formats;
- support partial imports only with explicit reporting;
- never overwrite an existing repository or snapshot silently;
- document whether an export is a full standalone snapshot or depends on parent/base snapshots.

---

# 7. Security, encryption, and privacy

Security is a functional requirement because the repository may contain personal photos, documents, contacts, and other sensitive data.

## 7.1 Threat model

Document at least:

- stolen or lost external backup drive;
- unauthorized access to the Windows account;
- malware or malicious process on the PC;
- compromised or malicious connected phone;
- malicious participant on the local network;
- pairing-token theft/replay;
- corrupted or tampered backup object;
- malicious path/archive entry;
- repository rollback or partial corruption;
- forgotten password or lost recovery key;
- secrets exposed through logs, crash dumps, temporary files, or UI previews;
- unsafe restore to a phone or filesystem destination.

## 7.2 Encryption requirements

Use vetted cryptographic libraries and established algorithms. Do not write cryptographic primitives.

Baseline design to evaluate and document:

- authenticated encryption using AES-256-GCM or another reviewed standard supported consistently by both platforms;
- a strong password-based KDF such as Argon2id using a maintained, audited library, or a justified platform-supported alternative;
- random salts and unique nonces;
- separate keys for distinct purposes;
- per-object data-encryption keys wrapped by a repository key where this simplifies rotation and isolation;
- authenticated associated data binding ciphertext to repository ID, object ID, format version, and relevant metadata;
- recovery-key generation using a cryptographically secure random source;
- explicit verification that the owner has saved the recovery key before relying on it;
- secure key handling in memory and best-effort clearing of temporary buffers;
- no password or raw key in configuration, logs, database, crash reports, or command lines.

Cryptographic implementation must be reviewed for nonce uniqueness, salt/parameter encoding, authentication-tag handling, key separation, wrong-password behaviour, interrupted writes, and cross-platform interoperability.

Do not invent custom encryption, custom KDFs, or an ad-hoc network cipher.

## 7.3 Encryption user experience

- Encrypted repository is the recommended default.
- Unencrypted mode is allowed for personal use but must clearly warn that anyone with access to its files can read the backup contents.
- Password and recovery-key setup must be understandable.
- Show a recovery-key confirmation step.
- Explain that losing all unlock credentials may make encrypted data unrecoverable.
- Support repository lock/unlock and optional idle lock.
- Never claim filenames, metadata, logs, filesystem traces, or external OS traces are hidden unless they are actually protected.
- Provide a safe password-change/key-rotation workflow only after it has been tested.

## 7.4 Network and pairing security

- Use TLS or another reviewed authenticated encrypted transport.
- Pairing must require explicit approval on both Windows and Android.
- Use short-lived QR pairing data; never place repository passwords, recovery keys, or long-lived secrets in QR codes.
- Bind trust to certificates or another reviewed key-establishment method.
- Store pairing credentials securely and provide revoke/remove controls.
- Reject expired, replayed, malformed, or mismatched pairing requests.
- Discovery may advertise a minimal service marker only; do not expose private device details before authentication.
- Validate message length, schema, device/session identity, and authorization on every request.
- Do not accept arbitrary filesystem paths from the phone.

## 7.5 Data minimization and logging

- Collect only metadata needed for backup, verification, restore, and troubleshooting.
- Never log media contents, contacts, messages, passwords, secrets, recovery keys, raw personal identifiers, or full file content.
- Sanitize paths and identifiers in diagnostic exports.
- Rotate and bound logs.
- Do not upload logs automatically.
- Allow the owner to inspect and export sanitized diagnostics locally.

---

# 8. Device discovery and identity architecture

Implement a pipeline rather than scattering device detection through UI code.

## 8.1 Discovery pipeline

1. Enumerate USB devices, ADB endpoints, MTP devices, and locally paired companion sessions.
2. Normalize transport identifiers.
3. Deduplicate multiple endpoints for the same physical phone.
4. Probe authorized device information.
5. Build a multi-attribute fingerprint.
6. Match the fingerprint against registered devices.
7. resolve ambiguity with user confirmation;
8. record sources and confidence for properties;
9. store device profile and current transport state;
10. compare current firmware/build and permissions against the latest assessment;
11. schedule capability re-probe when meaningful changes are observed.

Do not rely solely on serial number, Android ID, MAC address, model name, or a single USB descriptor. Some identifiers may be hidden, reset, duplicated, or restricted.

## 8.2 Device profile fields

- ARCHEION internal UUID;
- user-editable display name;
- manufacturer, brand, model, model code, product, codename;
- Android release and API level;
- build fingerprint and security patch where available;
- transport endpoints and states;
- first/last seen timestamps;
- identity confidence and source evidence;
- last capability assessment;
- adapter/profile version;
- device-specific backup preferences and exclusions;
- last backup timestamp and most recent successful snapshot;
- snapshot chain and backup/restore/migration history;
- user notes.

---

# 9. Adaptive strategy and capability engine

The strategy engine chooses among supported sources using both device identity and live capability probes.

## 9.1 Strategy selection

1. Read device fingerprint/build information.
2. Select candidate profile metadata from Generic Android and tested OEM profiles.
3. Validate schema/version/signature if applicable.
4. Probe real permissions, APIs, transports, and accessible sources.
5. Compare profile expectations with observed state.
6. Build a per-category strategy with source, permission, expected coverage, restore path, limitations, and confidence.
7. Prefer a verified source over a theoretical method.
8. explain the chosen method to the user;
9. record profile/adapter versions in the snapshot;
10. fall back safely when a profile is missing or does not match.

Profiles are declarative data only. Never execute profile scripts or untrusted downloaded code. Adapter implementations are shipped code and must be reviewed/tested.

## 9.2 OEM profile families

Create the framework for Generic Android and future profiles such as Samsung, Google Pixel, OnePlus, Xiaomi/Redmi, Motorola, Nothing, Oppo/Realme, Vivo, and others.

Do not spend early milestones building many untested profiles. Start with Generic Android and one or more actual phones available to the owner. Add an OEM profile only when a real, documented behaviour difference justifies it and physical-device tests exist.

## 9.3 Profile schema

Use versioned JSON Schema with:

- profile ID/version;
- OEM/model patterns and exclusions;
- supported API range;
- build/firmware constraints;
- adapter identifiers;
- capability declarations and probe definitions;
- permissions/roles;
- backup sources and restore providers;
- warnings and known issues;
- validation state;
- hardware test record references.

Do not include executable code in a profile.

---

# 10. Windows application experience

The UI should be clean, reliable, and efficient. It must support the workflows rather than imitate a generic analytics dashboard.

## 10.1 Main navigation

- Overview
- Devices
- Backup
- Backups & Snapshots
- Backup Explorer
- Compare Snapshots
- Restore & Migration
- Activity
- Diagnostics
- Settings

Navigation labels may be combined where that improves usability, but none of the functional areas may disappear.

## 10.2 Overview

Show only measured or sourced information:

- actual connected devices;
- last backup per device;
- recent snapshots;
- active jobs;
- repository size and available/free space;
- repository health and most recent verification;
- overdue scheduled backups;
- clear actions to connect, back up, browse, and verify.

No fake statistics, decorative charts, placeholder numbers, or invented device records.

## 10.3 Devices

- list registered and currently detected devices;
- model, Android version, connection status, last seen, last backup, and health;
- detect/refresh action;
- add/recognize device action;
- user-editable device name;
- device detail pages with identity, firmware, capability report, permission state, backup history, exclusions, and diagnostics;
- removal from registry must not delete snapshots;
- snapshot deletion is a separate destructive operation.

## 10.4 Backup page

- choose connected/registered device;
- show actual capability state;
- refresh permissions/capabilities;
- select categories and folders;
- set per-device exclusions;
- see plan preview and limitations;
- start/cancel backup;
- see stage-based progress and verified results.

## 10.5 Backups and snapshots

- browse snapshots by device/date/status;
- show full/partial/failed state;
- show measured size and item counts;
- show parent snapshot and incremental status;
- open reports;
- verify snapshot;
- export portable snapshot;
- apply retention only with visible rules and safe previews.

## 10.6 Backup Explorer

- category and folder navigation;
- search and filters;
- safe previews;
- multiple selection;
- export to destination;
- captured metadata/provenance;
- checksum/integrity status;
- historical version access.

## 10.7 Compare Snapshots

- select two snapshots;
- summarize added/removed/modified/unchanged/unknown items;
- filter by category and status;
- compare file versions when supported;
- export or restore selected historical versions;
- explain incomplete comparisons caused by partial source access.

## 10.8 Restore and Migration

- choose source snapshot and target;
- show source/target compatibility;
- show category mapping and limitations;
- preview conflicts, duplicates, overwrites, and permissions;
- present safe merge/skip/rename/replace choices;
- require explicit confirmation;
- show progress, cancel, retry, and recovery;
- show post-restore validation report.

## 10.9 Activity and diagnostics

- backup/restore/migration history;
- date, device, status, duration, measured bytes, and sanitized error code;
- search/filter;
- detailed report;
- active-job cancellation where supported;
- local diagnostic bundle with secret redaction;
- ADB connection troubleshooting and authorization guidance.

## 10.10 Settings

- repository path and management;
- encryption, password, and recovery-key management;
- storage and retention;
- optional backup-on-connect behaviour;
- schedules and conditions;
- trusted Android devices/hosts;
- network transport settings;
- ADB path/setup/diagnostics;
- privacy and diagnostics;
- theme and accessibility;
- application version, licences, logs, and update method.

## 10.11 UX quality requirements

- Every page has loading, empty, success, partial, error, and disconnected states as applicable.
- Keyboard navigation and accessible names for important controls.
- Correct high-DPI scaling and responsive layout.
- Respect Windows contrast settings where practical.
- Avoid blocking the UI thread.
- Preserve navigation and selection state predictably.
- Use a restrained visual system: clear typography, good spacing, crisp icons, subtle motion, and no excessive gradients/glow/glass effects.
- Visual polish must never replace actual functionality.

---

# 11. Android companion experience

## 11.1 Required screens

1. **Welcome / Setup:** Explain its role, local transfer model, and privacy; no permission wall.
2. **Dashboard:** Device information, Android version, capability status, paired Windows host, active/recent sessions.
3. **Pairing:** USB and Wi-Fi/LAN pairing, QR exchange, host approval, trust/revoke controls.
4. **Capabilities:** Category-level support and limitations.
5. **Permissions:** Explain, request, inspect, and re-check only the permission needed for a selected task.
6. **Export:** Select supported data categories and files/folders; show counts/size where measured.
7. **Transfer:** Live progress, measured throughput, chunk/retry status, cancel, and connection state.
8. **Activity:** Completed, partial, failed, and cancelled sessions.
9. **Settings:** Privacy, permissions, trusted hosts, export defaults, diagnostics, version.

## 11.2 Android exporter design

Each exporter must implement an explicit contract such as:

- stable source ID;
- category;
- required permissions and user action;
- capability assessment;
- enumeration;
- streaming export;
- metadata extraction;
- cancellation;
- result summary;
- limitations;
- unit/instrumentation tests.

Implement initial exporters in this order, subject to hardware validation:

1. user-selected files and directories via SAF;
2. photos/videos/audio through the correct current media APIs and permission model;
3. documents and downloads when explicitly selected or exposed through MTP;
4. contacts;
5. calendar;
6. app inventory metadata and accessible APK exports;
7. app-specific integrations only when an app provides a documented export route.

Permission denial must not crash the app or make unrelated categories unusable.

## 11.3 Background and long-running work

- Use coroutines for in-process async operations.
- Use WorkManager for deferrable/reliable scheduled work that can be delayed by Android.
- For long user-initiated transfer operations, evaluate the current Android user-initiated data transfer API.
- Use foreground services only for permitted, user-visible, correctly declared service types.
- Respect Android 12+ background-start restrictions and Android 15+ foreground-service time limits.
- Re-check Android 16/17 and later changes before finalizing background transfer architecture.
- Persist transfer checkpoints in a safe way and report interruption accurately.
- Do not use a periodic worker to pretend to maintain a permanent USB/Wi-Fi session.

---

# 12. Windows–Android protocol and secure transport

## 12.1 Contract ownership

Store language-neutral JSON Schemas under `schemas/protocol/`. Generate DTOs if a stable generator is suitable; otherwise create compatible C# and Kotlin DTOs and test them against shared fixtures.

Every message should include, where applicable:

- protocol version;
- session ID;
- message ID;
- correlation ID;
- sender/receiver identity;
- message type;
- timestamp;
- payload schema version;
- payload length/limits;
- authentication/session context.

Reject incompatible protocol versions with a useful error. Support version negotiation and explicit feature negotiation.

## 12.2 Initial message types

Implement only the messages needed by completed workflows, including:

- `HELLO`
- `PROTOCOL_NEGOTIATION`
- `PAIRING_INIT`
- `PAIRING_CONFIRM`
- `PAIRING_COMPLETE`
- `PAIRING_REVOKE`
- `DEVICE_INFO`
- `CAPABILITY_REQUEST`
- `CAPABILITY_REPORT`
- `PERMISSION_STATUS`
- `BACKUP_PLAN_REQUEST`
- `BACKUP_PLAN_RESPONSE`
- `BACKUP_START`
- `EXPORT_REQUEST`
- `EXPORT_STARTED`
- `EXPORT_PROGRESS`
- `DATA_CHUNK`
- `CHUNK_ACK`
- `EXPORT_COMPLETE`
- `VERIFICATION_RESULT`
- `BACKUP_CANCEL`
- `RESTORE_PLAN_REQUEST`
- `RESTORE_PLAN_RESPONSE`
- `RESTORE_START`
- `RESTORE_PROGRESS`
- `RESTORE_COMPLETE`
- `ERROR`
- `SESSION_CLOSE`

## 12.3 Transfer protocol

- Use bounded chunks and bounded in-flight queues.
- Include chunk index, object/transfer ID, byte offset, declared length, and checksum.
- Acknowledge received chunks.
- Retry only within bounded policies.
- Resume only when source data identity, session trust, offsets, and integrity checks remain valid.
- Handle disconnect, timeout, duplicate message, reordering, cancellation, and process restart.
- Validate all lengths, counts, message types, paths, and schema fields.
- Prevent path traversal and unsafe archive extraction.
- Do not allow a peer to request arbitrary phone or Windows filesystem paths.
- Apply backpressure and do not buffer an entire backup in memory.

## 12.4 Local-network pairing

- Use local discovery only to find a minimal service marker.
- Do not initiate backup just because a device is discovered.
- Require explicit initial pairing approval on both devices.
- Use a short-lived QR challenge or similarly reviewed trust ceremony.
- Authenticate subsequent sessions with established credentials/certificates.
- Encrypt data in transit.
- Support session expiration, revocation, and key rotation.
- Revalidate current Android local-network permission requirements for the targeted Android release.

---

# 13. Database and persistence model

Use SQLite with explicit migrations, foreign keys, transactions, constraints, and indexes. Keep payload contents and raw secrets out of the database.

Suggested tables (adapt when implementation evidence justifies it):

### `repositories`

- `id` UUID primary key
- `format_version`
- `display_name`
- `root_path`
- `encryption_mode`
- `created_at`, `last_opened_at`
- `state`, `health_status`

### `devices`

- `id` ARCHEION UUID
- `display_name`
- `manufacturer`, `brand`, `model`, `model_code`, `product`, `codename`
- `android_version`, `api_level`, `build_fingerprint`, `security_patch`
- `identity_confidence`
- `first_seen_at`, `last_seen_at`
- `last_profile_version`, `last_capability_assessment_id`
- `device_state`, `notes`

### `device_identifiers`

- `id`, `device_id`, `identifier_type`, protected/hash value, source, confidence, observed_at
- Store only identifiers that are permitted and necessary. Prefer keyed hashes where appropriate.

### `device_endpoints`

- `id`, `device_id`, `transport_type`, safe endpoint descriptor, `last_seen_at`, `state`

### `adapter_profiles`

- `id`, `profile_version`, `manufacturer`, `model_pattern`, `min_api`, `max_api`, build constraints, validation state, source reference, updated_at

### `capability_assessments`

- `id`, `device_id`, `assessed_at`, Android version, build fingerprint, adapter/profile IDs and versions, overall status, report JSON

### `backup_jobs`

- `id`, `device_id`, `repository_id`, `job_type`, `state`, `started_at`, `finished_at`, `plan_json`, `result_summary`, `error_code`, `cancel_requested`

### `snapshots`

- `id`, `device_id`, `repository_id`, `parent_snapshot_id`, `created_at`, `state`, `manifest_object_id`, `total_bytes`, `verified_bytes`, `verification_status`, `coverage_summary`, profile/adapter versions, notes

### `backup_items`

- `id`, `snapshot_id`, `category`, `relative_path_or_logical_name`, `object_id`, `source_provider`, `size_bytes`, `content_hash`, `metadata_json`, `collection_status`, `restore_status`

### `objects`

- `object_id`, `storage_key`, `content_hash`, `size_bytes`, `encryption_mode`, `created_at`, `reference_count`, `verification_status`

### `restore_jobs`

- `id`, `source_snapshot_id`, `target_device_id`, `state`, `started_at`, `finished_at`, `plan_json`, `result_summary`, `error_code`

### `migration_jobs`

- `id`, `source_device_id`, `target_device_id`, `source_snapshot_id`, `state`, `plan_json`, `started_at`, `finished_at`, `result_summary`

### `permissions_and_consents`

- `id`, `device_id`, permission/capability, status, granted_at, last_checked_at, source

### `job_events`

- `id`, `job_id`, timestamp, event type, severity, sanitized message, metadata JSON

### `repository_migrations`

- `version`, `applied_at`, `description`, `checksum`

Add indexes for device, snapshot, category, date, state, object references, and common search fields. Avoid unbounded event growth. Test migrations from every supported repository schema version.

---

# 14. Backup health, verification, and recovery readiness

Add a dedicated repository/device health area focused on whether backups can actually be used.

## 14.1 Health signals

- last successful backup timestamp;
- last successful integrity scan;
- latest snapshot completeness;
- latest verification result;
- missing or corrupted objects;
- unreferenced objects;
- repository drive availability;
- encrypted repository unlock/recovery readiness state;
- categories not captured or currently inaccessible;
- restore compatibility for a selected destination;
- last successful export/restore validation where available.

## 14.2 Integrity scanning

- Verify object existence, size, hash, and authentication tag.
- Validate manifests and database references.
- Identify missing, corrupted, malformed, or unreferenced objects.
- Do not silently modify or delete damaged data during a scan.
- Provide a repair workflow that previews changes and preserves the original damaged data until repair is verified.
- Distinguish `Healthy`, `Warning`, `Degraded`, `Corrupt`, `Locked`, `MissingDrive`, and `Unknown`.

## 14.3 Restore rehearsal

Provide non-destructive recovery checks:

- validate repository unlock;
- validate manifest and object references;
- decrypt and hash selected objects to a temporary verification location;
- test portable export/import on a copy or temporary repository;
- generate a restore-readiness report;
- do not alter source phone or backup snapshots.

Never claim an entire phone can be restored merely because the repository is healthy. Restore readiness is category- and destination-specific.

---

# 15. Testing and quality gates

Testing is mandatory for data integrity. A compile-only application is not sufficient.

## 15.1 Unit tests

- identity normalization and fingerprint matching;
- endpoint deduplication;
- profile matching and precedence;
- capability state transitions;
- backup-plan generation;
- category result classification;
- snapshot manifest validation;
- schema/version compatibility;
- database migrations;
- object-store references and garbage collection;
- hashing and integrity validation;
- encryption/decryption and wrong-password behaviour;
- recovery-key round trip;
- nonce uniqueness/format validation;
- restore conflict mapping;
- path safety and traversal prevention;
- job state transitions and cancellation;
- retention policy calculations;
- snapshot comparison classification.

## 15.2 Windows integration tests

- USB/ADB discovery using fixtures and a physical device;
- MTP discovery and accessible-file enumeration;
- repository create/open/relocate;
- encrypted and unencrypted repository round trip;
- wrong-key rejection;
- crash recovery during object staging and snapshot commit;
- portable snapshot export/import;
- missing-drive recovery;
- backup commit and reopen;
- full integrity scan;
- job cancellation/retry/resume;
- restore plan generation;
- safe export to destination.

## 15.3 Android tests

- permission grant, partial grant, denial, revocation, and regrant;
- SAF selected-file and folder access;
- persistable URI grant survival and invalidation;
- MediaStore/Photo Picker behaviour across API levels;
- contacts/calendar export and import fixtures;
- package visibility and APK export restrictions;
- foreground/background lifecycle;
- WorkManager constraints and retry;
- user-initiated transfer lifecycle where available;
- pairing/revocation;
- protocol compatibility vectors;
- transfer interruption, checksum mismatch, retries, and resume.

## 15.4 Cross-platform protocol tests

- C# and Kotlin read the same fixtures;
- message serialization/deserialization;
- protocol negotiation;
- unknown optional fields;
- unsupported major versions;
- chunk hashes and acknowledgements;
- duplicate/reordered messages;
- cancellation and session close;
- malformed/oversized payload rejection.

## 15.5 Security tests

- path traversal and symlink escape;
- malformed JSON/schema rejection;
- malicious archive entries;
- tampered ciphertext/tag failure;
- wrong password/recovery key;
- replayed/expired pairing attempt;
- untrusted LAN peer;
- secret redaction in logs;
- database corruption recovery;
- object truncation and hash mismatch;
- interrupted key rotation if implemented.

## 15.6 Physical-device validation

Create `docs/HARDWARE_TEST_MATRIX.md` and record:

- exact phone manufacturer/model;
- Android release/API/build/security patch where available;
- USB mode and ADB authorization state;
- MTP availability;
- companion installed/version;
- pairing transport;
- permission state;
- category results;
- file counts/bytes actually measured;
- backup hash verification;
- export/restore round trip;
- disconnect/cancel/resume tests;
- date and test evidence.

Never report hardware validation complete without a real device test record. Start with the phone(s) actually available to the owner; do not delay generic implementation while waiting for unsupported OEM devices.

## 15.7 End-to-end acceptance tests

1. Launch Windows app with no repository and create a repository.
2. Close and reopen the application; open the same repository.
3. Connect one real Android phone and identify it.
4. Display a truthful capability report.
5. Install/open the companion if required.
6. Pair with explicit user approval.
7. Create a backup plan.
8. Back up at least one real accessible category.
9. Verify hashes and manifest.
10. Browse the snapshot without reconnecting the phone.
11. Export a backed-up item and compare its bytes/hash.
12. Reconnect and recognize the same device.
13. Create a second snapshot and compare it with the first.
14. Run repository integrity scan.
15. Test a supported restore to a safe destination/test device.
16. Test a cross-device compatibility plan without claiming unsupported restore.
17. Test disconnect/cancel/restart recovery.
18. Remove and reconnect the external library drive if applicable.
19. Test encrypted and unencrypted paths.
20. Test portable snapshot export/import.

---

# 16. Repository structure

Create a clean monorepo and adapt the structure to actual modules. Avoid empty projects created merely to imitate this diagram.

```text
ARCHEION/
├── README.md
├── .gitignore
├── global.json
├── Directory.Build.props
├── docs/
│   ├── ENVIRONMENT_AUDIT.md
│   ├── PRODUCT_SPEC.md
│   ├── ARCHITECTURE.md
│   ├── IMPLEMENTATION_PLAN.md
│   ├── STATUS.md
│   ├── PLATFORM_RESEARCH.md
│   ├── CAPABILITY_MATRIX.md
│   ├── SECURITY.md
│   ├── PROTOCOL.md
│   ├── BACKUP_FORMAT.md
│   ├── DEVICE_COMPATIBILITY.md
│   ├── HARDWARE_TEST_MATRIX.md
│   ├── TESTING.md
│   └── USER_GUIDE.md
├── schemas/
│   ├── protocol/
│   │   ├── protocol-version.json
│   │   ├── messages/
│   │   └── examples/
│   └── backup/
│       ├── manifest.schema.json
│       └── device-profile.schema.json
├── shared/
│   ├── Archeion.Contracts/
│   ├── protocol-schema/
│   └── protocol-test-vectors/
├── windows/
│   ├── Archeion.sln
│   ├── src/
│   │   ├── Archeion.App/
│   │   ├── Archeion.Presentation/
│   │   ├── Archeion.Application/
│   │   ├── Archeion.Domain/
│   │   ├── Archeion.Infrastructure/
│   │   ├── Archeion.DeviceDiscovery/
│   │   ├── Archeion.DeviceIdentity/
│   │   ├── Archeion.CapabilityEngine/
│   │   ├── Archeion.StrategyEngine/
│   │   ├── Archeion.Adapters.Abstractions/
│   │   ├── Archeion.Adapters.GenericAndroid/
│   │   ├── Archeion.Adb/
│   │   ├── Archeion.Mtp/
│   │   ├── Archeion.CompanionBridge/
│   │   ├── Archeion.NetworkPairing/
│   │   ├── Archeion.BackupEngine/
│   │   ├── Archeion.RestoreEngine/
│   │   ├── Archeion.MigrationEngine/
│   │   ├── Archeion.BackupLibrary/
│   │   ├── Archeion.ObjectStore/
│   │   ├── Archeion.Security/
│   │   ├── Archeion.Persistence/
│   │   ├── Archeion.Diagnostics/
│   │   └── Archeion.Scheduler/
│   └── tests/
│       ├── Archeion.UnitTests/
│       ├── Archeion.IntegrationTests/
│       ├── Archeion.ProtocolTests/
│       ├── Archeion.BackupRestoreTests/
│       └── Archeion.SecurityTests/
├── android/
│   ├── settings.gradle.kts
│   ├── build.gradle.kts
│   ├── gradle/libs.versions.toml
│   ├── app/
│   │   ├── src/main/AndroidManifest.xml
│   │   ├── src/main/java/.../archeion/
│   │   │   ├── app/
│   │   │   ├── ui/
│   │   │   ├── domain/
│   │   │   ├── data/
│   │   │   ├── pairing/
│   │   │   ├── protocol/
│   │   │   ├── exporters/
│   │   │   ├── permissions/
│   │   │   ├── transfer/
│   │   │   └── verification/
│   │   └── src/test/
│   └── protocol-tests/
├── tools/
│   ├── scripts/
│   └── profile-validator/
└── .github/workflows/
```

Create modules only when a concrete boundary and implementation exist. Keep the initial solution buildable.

---

# 17. Milestones and implementation order

The coding agent must proceed through the milestones. Do not jump ahead to decorative UI or broad OEM feature claims.

## Milestone 0 — Workspace, environment, and feasibility

- inspect workspace/toolchain/Git;
- verify stable SDK/runtime availability;
- research official platform restrictions;
- create plan, status, architecture, security outline, capability matrix, and repository skeleton;
- identify actual owner test device(s) if connected;
- **Gate:** documented non-root capabilities and limitations; Windows and Android projects initialized with validated build prerequisites.

## Milestone 1 — Buildable application shells and contracts

- create WinUI 3 shell and navigation;
- create Android Compose shell;
- create shared protocol and backup schemas;
- create SQLite migrations baseline;
- add unit/protocol test projects;
- **Gate:** both applications compile; protocol test vectors pass; no fake device data is presented in normal mode.

## Milestone 2 — Device discovery and identity

- implement ADB detection and authorization state;
- implement available MTP discovery;
- create endpoint deduplication;
- identify device properties and confidence;
- create device registry and reconnect matching;
- **Gate:** at least one physical phone is detected and correctly represented; unknown/unauthorized states work.

## Milestone 3 — Capability engine

- generic Android adapter;
- category capability model;
- permission state collection;
- SAF/media capability assessment;
- capability report UI;
- stale/re-probe logic;
- **Gate:** category statuses derive from observed APIs/permissions, not hard-coded model assumptions.

## Milestone 4 — Repository foundation

- repository create/open/relocate;
- SQLite schema and migrations;
- object store;
- manifest schema;
- hashing and integrity validation;
- encrypted and unencrypted modes;
- staging/atomic commit/crash recovery;
- **Gate:** repository round-trip, reopen, wrong-key test, hash test, and crash-recovery test pass.

## Milestone 5 — First real backup vertical slice

- backup plan preview;
- select accessible files/media;
- collect real data;
- stream objects into repository;
- generate manifest;
- verify snapshot;
- show truthful report;
- **Gate:** real content from the physical phone is backed up, verified, reopened, browsed, and exported.

## Milestone 6 — Android companion pairing and transfer

- explicit trust ceremony;
- USB and/or LAN transport (choose the reliable first path);
- protocol negotiation;
- chunk transfer and checksums;
- retries, cancellation, reconnect;
- **Gate:** Windows and Android exchange real protocol messages and transfer/verify a real test file.

## Milestone 7 — More exporters and Backup Explorer

- contacts/calendar only after verified public API implementations;
- app inventory/APK export with clear limits;
- category/folder Explorer;
- search/filter/preview/export;
- **Gate:** each category shown as implemented has a real test and evidence.

## Milestone 8 — Snapshot intelligence and repository health

- second/later snapshots;
- snapshot comparison;
- incremental reuse only where correctly supported;
- integrity scanner;
- restore rehearsal;
- portable export/import;
- **Gate:** comparison results are evidence-based; export/import round-trip verifies all included objects.

## Milestone 9 — Restore and migration

- destination capability probe;
- restore plan and conflict preview;
- supported file/media restore;
- contacts/calendar restore only when tested;
- cross-device normalized mapping;
- post-restore validation;
- **Gate:** at least one supported restore category succeeds on a test destination and is verified.

## Milestone 10 — Scheduling, recovery, polish, release artifacts

- optional backup-on-connect;
- schedules and retention;
- notification/error reporting;
- reconnect and interrupted-job recovery;
- accessibility/performance polish;
- clean builds and security regression suite;
- Windows package and Android APK where build environment permits;
- **Gate:** all claimed capabilities have implementation and evidence; no critical/high unresolved data-loss defect without explicit disposition.

---

# 18. Performance, reliability, and operational requirements

- Use asynchronous I/O and cancellation throughout.
- Stream media and large files; never load an entire backup into memory.
- Use bounded worker queues and backpressure.
- Avoid excessive repeated hashing of immutable verified objects unless required by a user-requested integrity scan.
- Measure actual throughput; do not estimate a speed and display it as real.
- Keep Windows UI responsive.
- Handle Windows long paths, file locks, permissions, removable media, and drive removal.
- Ensure database transactions and filesystem commit order are recoverable.
- Keep Android battery, storage, thermal, and network limits visible.
- Handle Android lifecycle/process death and current background execution limits.
- Treat clock/time-zone changes correctly in schedule and snapshot metadata.
- Ensure one job's cancellation does not cancel an unrelated job.
- Use conservative heavy-job concurrency by default; discovery/lightweight checks may run concurrently.
- Never delete the only known valid copy as part of cleanup or retention.

---

# 19. Definition of done for every feature

A feature is complete only when all applicable conditions are met:

- implementation exists in the actual application;
- real user workflow is wired end to end;
- success, partial, denied, empty, disconnected, loading, and error states exist where relevant;
- cancellation and recovery behaviour are implemented;
- unit/integration tests exist and pass;
- physical-device tests exist when the feature depends on real device behaviour;
- data model and migrations are correct;
- documentation and capability matrix are updated;
- no fabricated values or fake success states exist;
- platform limitations are documented;
- affected builds are clean;
- logs do not expose secrets or user content.

A screenshot, empty page, interface control, class stub, TODO, or mocked repository does not satisfy this definition.

---

# 20. Build, tooling, scripts, and documentation

## 20.1 Windows commands and scripts

Provide scripts for:

- environment/prerequisite checks;
- solution restore;
- Debug build;
- Release build;
- unit/integration test execution;
- packaging;
- ADB discovery diagnostics;
- repository validation.

## 20.2 Android commands and scripts

Provide commands for:

- Gradle sync/build;
- Debug APK build;
- unit tests;
- lint/static analysis;
- install/uninstall on a connected device;
- protocol tests;
- exporter instrumentation tests where practical.

## 20.3 Cross-platform validation

- validate JSON Schemas;
- run protocol fixtures on C# and Kotlin;
- keep shared fixtures under source control;
- CI may run build/unit/protocol tests without a phone;
- hardware tests remain a separate documented lane.

## 20.4 Required documentation

`README.md` must include:

- what ARCHEION does and does not do;
- offline/local storage explanation;
- prerequisites;
- build and run commands;
- how to authorize ADB;
- how to install the Android companion;
- how to create/open an encrypted repository;
- supported categories and limitations;
- how to back up, verify, browse, export, compare, restore;
- troubleshooting;
- where logs and reports are stored;
- recovery-key warning.

Do not fabricate links to generated packages. Only mention an APK/installer if it exists at the documented path.

---

# 21. Final audit format

At the end of a major milestone or release candidate, provide this report.

## Product status

- version/stage;
- implemented workflows;
- Windows build status;
- Android build status;
- test totals, failures, and skipped tests;
- known defects.

## Hardware validation

- exact device model and Android version/build if available;
- transport methods tested;
- categories tested;
- number of files/bytes measured;
- backup/verification/export results;
- restore/migration results;
- hardware tests not yet performed.

## Capability coverage

A table with:

- category;
- capture status;
- restore status;
- method/source;
- permission/user action;
- tested device(s);
- limitations;
- evidence/reference.

## Data integrity and security

- repository round-trip results;
- integrity scan results;
- encryption tests;
- recovery-key test results;
- protocol security tests;
- unresolved risks.

## Artifacts

Exact paths to generated installer/APK/test reports only when they exist.

## Remaining work

Prioritized tasks, blockers, and practical next steps. Distinguish a genuine platform limitation from an unfinished implementation.

---

# 22. Start now — first required vertical slice

Begin by inspecting the current workspace and toolchain. Then create the plan/status/research documentation and a minimal buildable monorepo.

Implement this first real end-to-end slice before broadening scope:

**Windows launch → detect a physically connected Android device (or show a truthful no-device/unauthorized state) → identify available device information → probe real capabilities → display the capability report → register the device in the local library.**

Then continue immediately with:

**Select an accessible file/media category → create backup plan → collect real user-authorized data → write it to a repository → verify hashes → commit a snapshot → close/reopen repository → browse/export the captured item.**

Do not stop at scaffolding. Build and test after every meaningful milestone. Implement the Windows and Android projects in the same repository and keep their shared contracts compatible.

---

# 23. Non-negotiable closing instruction

**Deliver a working personal tool, not a product pitch or a collection of screens.**

Do not spend implementation time trying to differentiate ARCHEION from commercial products. The owner's goal is to have a capable tool without paying for a commercial tool or depending on a subscription. Build the useful functionality directly and keep it free, offline-first, and under the owner's control.

Prioritize in this order:

1. preserve real user data correctly;
2. verify that backups are intact and recoverable;
3. make restore operations safe and honest;
4. support the widest legitimate set of categories possible on the owner's actual devices;
5. keep the repository portable and maintainable;
6. provide a dependable companion and transfer workflow;
7. add scheduling and convenience after the underlying backup path is proven;
8. polish the UI without allowing visual work to delay core functionality.

Never claim full Android backup, universal app-data extraction, complete system cloning, or universal restore when Android's platform security model does not permit it. Never fabricate functionality, tests, build artifacts, or hardware validation.

The correct outcome is a reliable, well-tested personal backup and recovery system whose limitations are visible and whose successful operations can be independently verified.

---

# Appendix A — Official platform research starting points

These are starting references, not a substitute for re-checking the current documentation before implementation. Record the access date and the exact APIs/versions relied on in `docs/PLATFORM_RESEARCH.md`.

## Android

- Android latest platform updates: https://developer.android.com/latest-updates/
- Android platform compatibility: https://developer.android.com/guide/app-compatibility
- Android 17 release notes: https://developer.android.com/about/versions/17/release-notes
- Android 16 behaviour changes: https://developer.android.com/about/versions/16/behavior-changes-16
- Shared files and Storage Access Framework: https://developer.android.com/training/data-storage/shared/documents-files
- Android 11 scoped-storage updates: https://developer.android.com/about/versions/11/privacy/storage
- Partial photo/video access: https://developer.android.com/about/versions/14/changes/partial-photo-video-access
- Auto Backup for Apps: https://developer.android.com/identity/data/autobackup
- Contacts Provider: https://developer.android.com/identity/providers/contacts-provider
- CalendarContract reference: https://developer.android.com/reference/android/provider/CalendarContract
- Package visibility: https://developer.android.com/training/package-visibility/declaring
- WorkManager: https://developer.android.com/develop/background-work/background-tasks/persistent
- User-initiated data transfer: https://developer.android.com/develop/background-work/background-tasks/uidt
- Foreground service restrictions: https://developer.android.com/develop/background-work/services/fgs/restrictions-bg-start
- Foreground service changes: https://developer.android.com/develop/background-work/services/fgs/changes
- ADB documentation: https://developer.android.com/tools/adb

## Windows and .NET

- .NET releases and support: https://learn.microsoft.com/dotnet/core/releases-and-support
- Install .NET on Windows / OS support matrix: https://learn.microsoft.com/dotnet/core/install/windows
- Windows App SDK support matrix: https://learn.microsoft.com/windows/apps/windows-app-sdk/support
- Windows SDK and version overview: https://learn.microsoft.com/windows/apps/get-started/versioning-overview
- WinUI 3: https://learn.microsoft.com/windows/apps/winui
- Windows App SDK release channels: https://learn.microsoft.com/windows/apps/windows-app-sdk/release-channels

## Personal-use deployment

- Keep release signing keys out of Git.
- Build and install the Android APK privately using a documented, repeatable process.
- Build a Windows package that can be installed on the owner's machine without development tools.
- Document any Windows version/runtime limitations clearly.
- Do not add a cloud update service. A future update mechanism may simply be a manually downloaded and locally verified release artifact.

---

**End of ARCHEION Engineering Master Prompt v2.0**
