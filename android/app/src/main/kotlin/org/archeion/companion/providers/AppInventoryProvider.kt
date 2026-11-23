package org.archeion.companion.providers

import android.content.Context
import android.content.pm.ApplicationInfo
import android.content.pm.PackageInfo
import android.content.pm.PackageManager
import android.os.Build
import kotlinx.serialization.Serializable
import java.io.File
import java.io.FileInputStream
import java.io.InputStream

@Serializable
data class AppMetadata(
    val packageName: String,
    val appName: String,
    val versionName: String,
    val versionCode: Long,
    val targetSdkVersion: Int,
    val minSdkVersion: Int,
    val isSystemApp: Boolean,
    val firstInstallTime: Long,
    val lastUpdateTime: Long,
    val baseApkPath: String,
    val baseApkSizeBytes: Long,
    val splitApkNames: List<String> = emptyList()
)

class AppInventoryProvider(private val context: Context) {

    fun enumerateInstalledApps(): List<AppMetadata> {
        val pm = context.packageManager
        val packages: List<PackageInfo> = try {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                pm.getInstalledPackages(PackageManager.PackageInfoFlags.of(0))
            } else {
                pm.getInstalledPackages(0)
            }
        } catch (e: Exception) {
            emptyList()
        }

        val result = mutableListOf<AppMetadata>()
        for (pkg in packages) {
            val appInfo = pkg.applicationInfo ?: continue
            val isSystem = (appInfo.flags and ApplicationInfo.FLAG_SYSTEM) != 0

            val appName = try {
                pm.getApplicationLabel(appInfo).toString()
            } catch (e: Exception) {
                pkg.packageName
            }

            val apkFile = File(appInfo.sourceDir)
            val apkSize = if (apkFile.exists()) apkFile.length() else 0L

            val splitNames = appInfo.splitSourceDirs?.map { File(it).name } ?: emptyList()

            val vCode = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) {
                pkg.longVersionCode
            } else {
                @Suppress("DEPRECATION")
                pkg.versionCode.toLong()
            }

            val minSdk = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                appInfo.minSdkVersion
            } else {
                21
            }

            result.add(
                AppMetadata(
                    packageName = pkg.packageName,
                    appName = appName,
                    versionName = pkg.versionName ?: "unknown",
                    versionCode = vCode,
                    targetSdkVersion = appInfo.targetSdkVersion,
                    minSdkVersion = minSdk,
                    isSystemApp = isSystem,
                    firstInstallTime = pkg.firstInstallTime,
                    lastUpdateTime = pkg.lastUpdateTime,
                    baseApkPath = appInfo.sourceDir,
                    baseApkSizeBytes = apkSize,
                    splitApkNames = splitNames
                )
            )
        }

        return result
    }

    fun openApkStream(app: AppMetadata): InputStream? {
        val file = File(app.baseApkPath)
        return if (file.exists() && file.canRead()) {
            FileInputStream(file)
        } else {
            null
        }
    }
}
