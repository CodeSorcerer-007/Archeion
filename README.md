# ARCHEION — Personal Android Backup, Preservation & Recovery System

ARCHEION is a free, local-first, private Windows application and native Android companion designed to preserve, verify, explore, and restore your Android data without cloud lock-in, subscriptions, or root requirements.

## Key Features
- **Local-First & Private:** Everything is stored on your local disk or external SSD. Zero telemetry, zero accounts, zero online checks.
- **Truthful Capability Engine:** Inspects your exact device and Android version to report real accessibility before you back up.
- **Strong Authenticated Encryption:** AES-256-GCM repository encryption with Argon2id/PBKDF2 key derivation and emergency recovery keys.
- **Content-Addressable Object Store:** Backups are cryptographically hashed (SHA-256), staged, verified, and atomically committed.
- **Backup Explorer:** Browse, search, filter, and export files from any snapshot without needing the phone connected.
- **Snapshot Intelligence:** Compare snapshots to inspect changes over time.
- **Safe Recovery:** Preview conflicts, duplicate handling, and validation before restoring.

## Architecture & Requirements
- **Windows:** Windows 11 (or supported Windows 10), .NET 10 LTS, WinUI 3.
- **Android:** Android 10 (API 29) to Android 17 (API 37), Jetpack Compose, Material 3.
- **Documentation:** See `docs/` for architecture, platform research, security, protocol, and testing specs.
