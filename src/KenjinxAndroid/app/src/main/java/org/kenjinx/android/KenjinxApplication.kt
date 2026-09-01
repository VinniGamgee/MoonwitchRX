package org.kenjinx.android

import android.app.Application
import android.content.Context
import java.io.File

class KenjinxApplication : Application() {
    init {
        instance = this
    }

    fun getPublicFilesDir(): File = getExternalFilesDir(null) ?: filesDir

    companion object {
        lateinit var instance: KenjinxApplication
            private set

        val context: Context get() = instance.applicationContext
    }
}
