package org.kenjinx.android.cheats

import android.content.Context
import androidx.core.content.edit

class CheatPrefs(private val context: Context) {
    private fun key(titleId: String) = "cheats_$titleId"
    private val prefs get() = context.getSharedPreferences("cheats", Context.MODE_PRIVATE)

    fun getEnabled(titleId: String): MutableSet<String> {
        return prefs.getStringSet(key(titleId), emptySet())?.toMutableSet() ?: mutableSetOf()
    }

    fun setEnabled(titleId: String, keys: Set<String>) {
        prefs.edit { putStringSet(key(titleId), keys.toSet()) }
    }
}
