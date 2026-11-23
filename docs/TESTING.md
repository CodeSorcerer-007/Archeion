# ARCHEION — Testing & Quality Gates

## 1. Test Tiers
1. **Unit Tests (`windows/tests/Archeion.UnitTests`):**
   - Pure domain models, identity fingerprinting, capability logic, manifest validation, snapshot diffing.
2. **Security Tests (`windows/tests/Archeion.SecurityTests`):**
   - Cryptographic tests: AES-256-GCM encryption/decryption, wrong-password rejection, tampered ciphertext detection, AAD mismatch, nonce uniqueness, recovery-key round trip.
3. **Storage & Integration Tests (`windows/tests/Archeion.IntegrationTests`):**
   - SQLite migrations, repository create/open/reopen, object staging, crash-recovery rollback, atomic commit.
4. **Protocol Tests (`windows/tests/Archeion.ProtocolTests`):**
   - Shared JSON fixtures across C# and Kotlin, framing, chunk acknowledgements, malformed payload rejection.
5. **Physical Device Validation (`docs/HARDWARE_TEST_MATRIX.md`):**
   - Real hardware test logs with model, Android build, permissions, byte counts, and hash verifications.
