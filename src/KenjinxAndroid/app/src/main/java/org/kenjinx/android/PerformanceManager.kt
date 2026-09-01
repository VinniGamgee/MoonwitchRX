package org.kenjinx.android

import android.content.Intent
import android.os.Build
import androidx.annotation.RequiresApi
import kotlin.math.abs

class PerformanceManager(private val activity: MainActivity) {
    @Volatile
    private var turboEnabled = false

    companion object {
        @RequiresApi(Build.VERSION_CODES.R)
        fun force60HzRefreshRate(enable: Boolean, activity: MainActivity) {
            // Hack for MIUI devices since they don't support the standard Android APIs
            try {
                val setFpsIntent = Intent("com.miui.powerkeeper.SET_ACTIVITY_FPS").apply {
                    putExtra("package_name", activity.packageName)
                    putExtra("isEnter", enable)
                }
                activity.sendBroadcast(setFpsIntent)
            } catch (_: Exception) {
            }

            if (enable)
                activity.display?.supportedModes?.minByOrNull { abs(it.refreshRate - 60f) }
                    ?.let { activity.window.attributes.preferredDisplayModeId = it.modeId }
            else
                activity.display?.supportedModes?.maxByOrNull { it.refreshRate }
                    ?.let { activity.window.attributes.preferredDisplayModeId = it.modeId }
        }
    }

    @Synchronized
    fun setTurboMode(enable: Boolean) {
        if (turboEnabled == enable) {
            return
        }

        NativeHelpers.instance.setTurboMode(enable)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
            force60HzRefreshRate(enable, activity)
        }
        turboEnabled = enable
    }
}
