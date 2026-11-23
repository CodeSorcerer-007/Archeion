# ARCHEION — Backup Repository & Storage Format Specification

**Format Version:** `1.0.0`

## 1. Directory Layout

A valid ARCHEION backup library follows this layout:

```text
ArcheionLibrary/
├── repository.json                   # Repository root descriptor & format version
├── database/
│   └── library.sqlite                # Relational metadata, devices, jobs, index
├── keys/
│   └── key-envelope.json             # Key derivation parameters, encrypted master key
├── devices/
│   └── <device-uuid>/
│       ├── profile.json              # Observed hardware & Android identity
│       ├── capabilities.json         # Capability assessment history
│       └── snapshots/
│           └── <snapshot-id>/
│               ├── manifest.json     # Full snapshot manifest (or encrypted envelope)
│               └── report.json       # Job outcome, counts, sizes, limitations
├── objects/
│   └── <prefix-2-hex>/               # Sharded content-addressable storage
│       └── <object-sha256>           # Payload (plaintext or AES-256-GCM ciphertext)
├── staging/
│   └── <job-uuid>/                   # Temporary in-flight streaming chunks
├── exports/                          # User-exported items or portable archives
├── reports/                          # Verification and health audit reports
└── logs/                             # Sanitized diagnostic logs
```

## 2. Object Store Mechanics
- Every stored file payload is identified by its SHA-256 hash (`ContentHash`).
- In encrypted mode, the object payload is stored as:
  `[12 bytes Nonce] [Ciphertext] [16 bytes GCM Auth Tag]`.
- Objects are staged in `staging/<job-id>/` during transfer. Only when the full byte count and cryptographic hash match the manifest declaration is the object atomically renamed (promoted) into `objects/xx/<hash>`.
- Objects are immutable and reference-counted across snapshots.
