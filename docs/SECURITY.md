# ARCHEION — Security, Encryption & Privacy Specification

## 1. Threat Model & Protections

| Threat | Impact | Mitigation in ARCHEION |
|---|---|---|
| Stolen external backup drive | Data exposure | AES-256-GCM encryption with Argon2id/PBKDF2 key derivation and random salts |
| Untrusted peer on local Wi-Fi | Tampering / Snooping | Explicit out-of-band QR pairing ceremony, mutual TLS / authenticated sessions |
| Path traversal attack from device | Host filesystem damage | Rigorous path sanitization, rejection of `..` / root paths, safe relative mapping |
| Tampered or damaged backup file | Corrupt restore | Per-object SHA-256 hashes, GCM authentication tags, pre-restore verification |
| Forgotten user password | Inaccessible data | 256-bit cryptographically secure recovery key presented and confirmed at setup |
| Sensitive data leakage in logs | Privacy violation | Strict log redactor: zero secrets, passwords, PII, or raw file contents logged |

## 2. Cryptographic Architecture

1. **Authenticated Encryption:**
   - Algorithm: `AES-256-GCM` (NIST SP 800-38D).
   - Key size: 256 bits (32 bytes).
   - Nonce / IV: 96 bits (12 bytes), generated via cryptographically secure random number generator (`RandomNumberGenerator`). Nonces are never reused.
   - Auth Tag: 128 bits (16 bytes).
   - Associated Data (AAD): Binds ciphertext to repository UUID, object ID, and format version to prevent ciphertext substitution across repositories or snapshots.
2. **Key Derivation (KDF):**
   - High-cost Argon2id or high-iteration PBKDF2 with SHA-256 (600,000+ iterations) using 256-bit random salt.
3. **Recovery Key:**
   - 256 bits of CSPRNG entropy encoded in human-readable base32/hex format with check bits.
   - Encrypts master repository key as an alternative key envelope.
