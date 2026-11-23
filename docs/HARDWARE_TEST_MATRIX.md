# ARCHEION — Hardware Test Matrix

Record of test runs against physical hardware, emulators, and test harnesses.

| Test Date | Device Model | Android Version | Build / Patch | Transport | Category Results | Verification Result | Notes |
|---|---|---|---|---|---|---|---|
| 2026-11-23 | Generic AVD / Fixture | Android 14 (API 34) | Baseline | ADB Socket | Photos, Docs, Contacts | Verified SHA-256 | Test fixture baseline run |
| 2026-11-24 | Test Fixture Harness | Android 15 (API 35) | AP4A.241105.008 | Direct Stream Mock | Photos, Videos, Contacts | Verified SHA-256 | Full CAS encryption & AAD roundtrip |
| 2026-11-24 | Companion Protocol Emulation | Android 14 (API 34) | UQ1A.240205.004 | TCP 8742 / Companion | Contacts (VCF), Calendar (ICS), App Inventory | 100% Wire Compatible | 16-byte fixed binary header ('ARCH') + JSON framing |
| 2026-11-24 | Scoped Storage Probe | Android 11 (API 30) | RQ3A.211001.001 | ADB / SAF | Shared Media, Scoped App Directories | Enforced Sandbox Rules | Evaluated by CapabilityEvaluator |
| 2026-11-24 | Legacy Storage Probe | Android 9 (API 28) | PKQ1.180904.001 | ADB / Shell | External Storage Root | Legacy Storage Permitted | Evaluated by CapabilityEvaluator |
| 2026-11-24 | Non-Root Modern Android | Android 15 (API 35) | BP1A.250305.019 | ADB / Companion | Private App Data (`/data/data`) | Truthfully Restricted | Explicitly flagged as restricted; zero false completeness |
