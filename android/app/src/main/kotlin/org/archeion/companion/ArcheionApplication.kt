package org.archeion.companion

import android.app.Application
import android.app.NotificationChannel
import android.app.NotificationManager
import android.os.Build

class ArcheionApplication : Application() {

    companion object {
        const val CHANNEL_ID_DATA_SYNC = "archeion_data_sync"
    }

    override fun onCreate() {
        super.onCreate()
        createNotificationChannels()
    }

    private fun createNotificationChannels() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                CHANNEL_ID_DATA_SYNC,
                getString(R.string.channel_name),
                NotificationManager.IMPORTANCE_LOW
            ).apply {
                description = getString(R.string.channel_desc)
                setShowBadge(false)
            }
            val notificationManager = getSystemService(NotificationManager::class.java)
            notificationManager?.createNotificationChannel(channel)
        }
    }
}
