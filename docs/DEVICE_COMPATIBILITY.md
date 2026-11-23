# ARCHEION — Device Compatibility Guide

## Supported Device Profiles
1. **Generic Android (Baseline):**
   - Covers Android 10 (API 29) to Android 17 (API 37).
   - Standard MediaStore, SAF, Contacts, and Calendar providers.
   - Standard ADB property queries and package manager commands.
2. **OEM Considerations:**
   - **Samsung (One UI):** Secure Folder data is isolated; dual messenger apps have separate package clones; standard public providers work.
   - **Google Pixel:** Pure AOSP providers; high fidelity photo metadata.
   - **Xiaomi / Redmi (MIUI / HyperOS):** Stricter background execution controls; permission prompts require explicit user confirmation.
   - **OnePlus / Oppo / Realme (OxygenOS / ColorOS):** Standard scoped storage rules apply.
