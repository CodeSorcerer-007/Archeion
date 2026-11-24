package org.archeion.companion.network

import android.content.Context
import android.net.nsd.NsdManager
import android.net.nsd.NsdServiceInfo
import android.os.Build
import android.util.Log

class ArcheionNsdManager(private val context: Context) {

    companion object {
        private const val TAG = "ArcheionNSD"
        const val SERVICE_TYPE = "_archeion._tcp."
    }

    private val nsdManager: NsdManager? by lazy {
        context.getSystemService(Context.NSD_SERVICE) as? NsdManager
    }

    private var registrationListener: NsdManager.RegistrationListener? = null
    private var isRegistered = false

    fun registerService(port: Int) {
        if (isRegistered) return

        val serviceInfo = NsdServiceInfo().apply {
            serviceName = "ARCHEION-${Build.MODEL.replace(" ", "-")}"
            serviceType = SERVICE_TYPE
            setPort(port)
            setAttribute("version", "1.0.0")
            setAttribute("platform", "android")
            setAttribute("apiLevel", Build.VERSION.SDK_INT.toString())
        }

        registrationListener = object : NsdManager.RegistrationListener {
            override fun onServiceRegistered(info: NsdServiceInfo) {
                isRegistered = true
                Log.i(TAG, "mDNS Service registered: ${info.serviceName} on port ${info.port}")
            }

            override fun onRegistrationFailed(info: NsdServiceInfo, errorCode: Int) {
                Log.w(TAG, "mDNS Service registration failed: error code $errorCode")
                isRegistered = false
            }

            override fun onServiceUnregistered(info: NsdServiceInfo) {
                Log.i(TAG, "mDNS Service unregistered")
                isRegistered = false
            }

            override fun onUnregistrationFailed(info: NsdServiceInfo, errorCode: Int) {
                Log.w(TAG, "mDNS Service unregistration failed: error code $errorCode")
            }
        }

        try {
            nsdManager?.registerService(serviceInfo, NsdManager.PROTOCOL_DNS_SD, registrationListener)
        } catch (e: Exception) {
            Log.w(TAG, "Failed to register NSD service: ${e.message}")
        }
    }

    fun unregisterService() {
        if (!isRegistered || registrationListener == null) return
        try {
            nsdManager?.unregisterService(registrationListener)
        } catch (e: Exception) {
            Log.w(TAG, "Error unregistering NSD service: ${e.message}")
        } finally {
            isRegistered = false
            registrationListener = null
        }
    }
}
