package org.archeion.companion.service

import android.app.Notification
import android.app.PendingIntent
import android.app.Service
import android.content.Intent
import android.content.pm.ServiceInfo
import android.os.Binder
import android.os.Build
import android.os.IBinder
import androidx.core.app.NotificationCompat
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import org.archeion.companion.ArcheionApplication
import org.archeion.companion.MainActivity
import org.archeion.companion.protocol.SessionSummary

data class TransferState(
    val isActive: Boolean = false,
    val currentItemName: String = "",
    val currentCategory: String = "",
    val processedBytes: Long = 0,
    val totalBytes: Long = 0,
    val processedItems: Long = 0,
    val totalItems: Long = 0,
    val lastSession: SessionSummary? = null,
    val lastCompletedTime: Long? = null
) {
    val progressPercent: Int
        get() = if (totalBytes > 0) ((processedBytes.toDouble() / totalBytes) * 100).toInt().coerceIn(0, 100) else 0
}

class TransferForegroundService : Service() {

    private val binder = LocalBinder()

    companion object {
        const val NOTIFICATION_ID = 87421
        const val ACTION_START = "org.archeion.companion.action.START_TRANSFER"
        const val ACTION_STOP = "org.archeion.companion.action.STOP_TRANSFER"

        private val _transferState = MutableStateFlow(TransferState())
        val transferState: StateFlow<TransferState> = _transferState.asStateFlow()

        fun updateProgress(
            itemName: String,
            category: String,
            processedBytes: Long,
            totalBytes: Long,
            processedItems: Long,
            totalItems: Long
        ) {
            _transferState.value = _transferState.value.copy(
                isActive = true,
                currentItemName = itemName,
                currentCategory = category,
                processedBytes = processedBytes,
                totalBytes = totalBytes,
                processedItems = processedItems,
                totalItems = totalItems
            )
        }

        fun recordCompletion(summary: SessionSummary) {
            _transferState.value = _transferState.value.copy(
                isActive = false,
                lastSession = summary,
                lastCompletedTime = System.currentTimeMillis()
            )
        }

        fun completeTransfer() {
            _transferState.value = _transferState.value.copy(isActive = false)
        }
    }

    inner class LocalBinder : Binder() {
        fun getService(): TransferForegroundService = this@TransferForegroundService
    }

    override fun onBind(intent: Intent?): IBinder = binder

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        when (intent?.action) {
            ACTION_STOP -> {
                completeTransfer()
                stopForeground(STOP_FOREGROUND_REMOVE)
                stopSelf()
            }
            ACTION_START -> {
                val notification = buildNotification("ARCHEION Backup Transfer", "Preparing transfer session...", 0)
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
                    startForeground(
                        NOTIFICATION_ID,
                        notification,
                        ServiceInfo.FOREGROUND_SERVICE_TYPE_DATA_SYNC
                    )
                } else {
                    startForeground(NOTIFICATION_ID, notification)
                }
            }
        }
        return START_NOT_STICKY
    }

    fun updateNotification(itemName: String, percent: Int) {
        val notification = buildNotification(
            title = "ARCHEION Backup in Progress ($percent%)",
            text = itemName,
            progress = percent
        )
        val manager = getSystemService(NOTIFICATION_SERVICE) as android.app.NotificationManager
        manager.notify(NOTIFICATION_ID, notification)
    }

    private fun buildNotification(title: String, text: String, progress: Int): Notification {
        val pendingIntent = PendingIntent.getActivity(
            this,
            0,
            Intent(this, MainActivity::class.java),
            PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_UPDATE_CURRENT
        )

        return NotificationCompat.Builder(this, ArcheionApplication.CHANNEL_ID_DATA_SYNC)
            .setContentTitle(title)
            .setContentText(text)
            .setSmallIcon(android.R.drawable.stat_sys_download)
            .setProgress(100, progress, false)
            .setContentIntent(pendingIntent)
            .setOngoing(true)
            .build()
    }
}
