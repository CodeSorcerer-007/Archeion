# ARCHEION — Architecture Document

**System Architecture:** Modular Monolith (.NET 10 LTS on Windows + Native Kotlin Compose Companion on Android)

```
+-----------------------------------------------------------------------------------+
|                              ARCHEION Desktop (Windows)                           |
|                                                                                   |
|  +--------------------+  +---------------------+  +----------------------------+  |
|  | Presentation (UI)  |  | Application Layer   |  | Domain Layer               |  |
|  | WinUI 3 (XAML/MVVM)|  | Job Coordinators    |  | Entities, Value Objects    |  |
|  | Pages & Controls   |  | Plan Builders, DTOs |  | Business Rules, Contracts  |  |
|  +--------------------+  +---------------------+  +----------------------------+  |
|            |                        |                           |                 |
|  +--------------------+  +---------------------+  +----------------------------+  |
|  | Device Subsystems  |  | Storage Engine      |  | Security & Cryptography    |  |
|  | - AdbClient        |  | - SQLite Metadata   |  | - AES-256-GCM Authenticated|  |
|  | - DeviceDiscovery  |  | - Content Store     |  | - Argon2id / PBKDF2 KDF    |  |
|  | - CapabilityEngine |  |   (objects/xx/hash) |  | - 256-bit Recovery Keys    |  |
|  | - StrategyEngine   |  | - Staging / Commit  |  | - Zero Plaintext Leakage   |  |
|  +--------------------+  +---------------------+  +----------------------------+  |
+-----------------------------------------------------------------------------------+
                                    |
                    USB ADB Socket / LAN TLS Protocol
                                    |
+-----------------------------------------------------------------------------------+
|                             ARCHEION Companion (Android)                          |
|                                                                                   |
|  +--------------------+  +---------------------+  +----------------------------+  |
|  | Jetpack Compose UI |  | Exporters & Probes  |  | Transfer & Streaming       |  |
|  | Material 3 Views   |  | - SAF Tree Scanner  |  | - Chunked Streamer         |  |
|  | Pairing QR Screen  |  | - MediaStore Exporter| - SHA-256 Checksumming      |  |
|  | Permission Manager |  | - Contacts / Cal    |  | - Flow Control / Retries   |  |
|  +--------------------+  +---------------------+  +----------------------------+  |
+-----------------------------------------------------------------------------------+
```

## 1. Core Principles & Boundaries
1. **Domain Isolation:** Pure domain logic has zero external dependencies and targets pure `net10.0`.
2. **Local-First & Offline:** Zero cloud dependencies, zero telemetry, zero mandatory remote connections.
3. **Immutability of Backups:** Snapshot manifests are immutable once committed. Content objects are content-addressed and verified via cryptographic hashes.
4. **Defensive Restore:** Safe previews, conflict detection, no silent overwrites.
