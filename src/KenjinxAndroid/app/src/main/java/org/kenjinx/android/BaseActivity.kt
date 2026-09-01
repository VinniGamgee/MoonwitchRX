package org.kenjinx.android

import androidx.activity.ComponentActivity

abstract class BaseActivity : ComponentActivity() {
    companion object {
        val crashHandler = CrashHandler()
    }
}
