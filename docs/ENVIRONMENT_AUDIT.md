# ARCHEION — Environment Audit

**Date:** 2026-11-23  
**Auditor:** Antigravity Autonomous Engineering Lead  
**Operating System:** Windows 11 Pro 64-bit (Version 10.0.26300, Build 26300)

---

## 1. Toolchain & Runtime Inventory

### 1.1 .NET & C#
- **Installed .NET SDKs:** `10.0.401` (Active default), `10.0.400`
- **Installed .NET Runtimes:**
  - `Microsoft.NETCore.App 10.0.401`
  - `Microsoft.WindowsDesktop.App 10.0.401`
  - `Microsoft.AspNetCore.App 10.0.401`
- **Installed Dotnet Workloads:** Baseline workloads installed.
- **Visual Studio:** Visual Studio Community v18 (`C:\Program Files\Microsoft Visual Studio\18\Community`)
- **MSBuild:** `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe`
- **WinUI 3 / Windows App SDK:** `dotnet new winui`, `winui-mvvm`, `winui-navview`, `wasdk-classlib` available.

### 1.2 Java & Android Toolchain
- **Default System Java:** Oracle Java 26.0.2 (`C:\Program Files\Common Files\Oracle\Java\javapath\java.exe`)
- **Android Dedicated JDK:** OpenJDK 21.0.8 LTS (`C:\Program Files\Android\openjdk\jdk-21.0.8`)
- **Android SDK Path:** `C:\Users\vishwa\AppData\Local\Android\Sdk`
- **ADB (Android Debug Bridge):** Version 1.0.41, build 37.0.1-15733141 (`C:\Users\vishwa\AppData\Local\Android\Sdk\platform-tools\adb.exe`)
- **Android Platforms Installed:**
  - `android-33` (Android 13)
  - `android-34` (Android 14)
  - `android-35` (Android 15)
  - `android-36` (Android 16)
  - `android-36.1`
- **Android Build-Tools Installed:**
  - `30.0.3`, `34.0.0`, `35.0.0`, `36.0.0`, `36.1.0`, `36.1.0-rc1`, `37.0.0`
- **Gradle:** Cached distributions available in `~/.gradle/wrapper/dists` (`gradle-8.10.2`, `8.11.1`, `9.1.0`, `9.2.0`, `9.7.1`).

### 1.3 Hardware & Attached Devices
- **Physical Devices via ADB:** `adb devices -l` reported 0 attached devices at audit time.
- **AVDs (Emulators):** None currently configured.
- **Storage / Filesystem:**
  - Workspace drive `E:\` (NTFS, local fixed SSD).
  - Primary system drive `C:\` (NTFS).

---

## 2. Platform Feasibility & Target Strategy

1. **Windows Desktop Command Centre:**
   - Target framework: `net10.0` (and `net10.0-windows10.0.19041.0` where WinUI / Windows App SDK is used).
   - Core libraries (`Archeion.Core`, `Archeion.Contracts`, `Archeion.Persistence`, etc.) target clean `net10.0` for pure, cross-platform and headless testability.
   - Desktop UI uses WinUI 3 with Windows App SDK 1.6+ / 1.7+, with MVVM architecture.
2. **Android Companion:**
   - Uses Gradle with OpenJDK 21 LTS (`JAVA_HOME = C:\Program Files\Android\openjdk\jdk-21.0.8`).
   - Compile SDK: `36` / `37`.
   - Min SDK: `29` (Android 10) to support modern scoped storage, MediaStore, SAF, and permission models.
   - Jetpack Compose + Material 3.
3. **Transport & Discovery:**
   - Primary: USB / ADB daemon communication using ADB socket protocol (`tcp:5037`) or direct CLI bridge.
   - Secondary: Authenticated local Wi-Fi / LAN TLS transport.
   - Offline mode: Direct repository browsing and export without phone connection.
