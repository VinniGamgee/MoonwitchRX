package org.kenjinx.android

import android.app.PendingIntent
import android.content.ComponentName
import android.content.Context
import android.content.Intent
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.util.Base64
import android.widget.Toast
import androidx.core.content.pm.ShortcutInfoCompat
import androidx.core.content.pm.ShortcutManagerCompat
import androidx.core.graphics.drawable.IconCompat
import androidx.core.net.toUri

object ShortcutHelper {

    /**
     * Creates a shortcut to launch a game.
     *
     * @param context           Context
     * @param title             Display name of the shortcut
     * @param bootPathUri       URI/string of the game file
     * @param useGridIcon       If true, the grid icon will be preferred (bitmap or Base64).
     * @param gridIconBitmap    Optional: Get the bitmap directly from your grid (recommended)
     * @param gridIconBase64    Optional: Base64 icon (if you have that at this location)
     */
    fun createGameShortcut(
        context: Context,
        title: String,
        bootPathUri: String,
        useGridIcon: Boolean,
        gridIconBitmap: Bitmap? = null,
        gridIconBase64: String? = null
    ) {
        val uri = runCatching { bootPathUri.toUri() }.getOrNull()

        // --- Select icon (Grid bitmap > Base64 > App icon)
        var icon = IconCompat.createWithResource(context, R.mipmap.ic_launcher)
        if (useGridIcon) {
            val bmp = gridIconBitmap ?: decodeBase64ToBitmap(gridIconBase64)
            if (bmp != null) icon = IconCompat.createWithBitmap(bmp)
        }

        // --- Explicit Intent exactly as in the working wizard ---
        // ACTION_VIEW + setDataAndType + clipData + GRANT flags + Component to MainActivity
        val launchIntent = Intent(Intent.ACTION_VIEW).apply {
            component = ComponentName(context, MainActivity::class.java)
            if (uri != null) {
                setDataAndType(uri, context.contentResolver.getType(uri) ?: "*/*")
                clipData = android.content.ClipData.newUri(
                    context.contentResolver,
                    "GameUri",
                    uri
                )
            }
            putExtra("bootPath", bootPathUri)
            putExtra("forceNceAndPptc", false)
            addFlags(
                Intent.FLAG_ACTIVITY_NEW_TASK or
                    Intent.FLAG_ACTIVITY_CLEAR_TOP or
                    Intent.FLAG_GRANT_READ_URI_PERMISSION or
                    Intent.FLAG_GRANT_WRITE_URI_PERMISSION
            )
        }

        // Best possible persistence/grants (failsafe, if already persisted → ignore exceptions)
        if (uri != null) {
            val rw = Intent.FLAG_GRANT_READ_URI_PERMISSION or Intent.FLAG_GRANT_WRITE_URI_PERMISSION
            runCatching { context.contentResolver.takePersistableUriPermission(uri, rw) }
            runCatching { context.grantUriPermission(context.packageName, uri, rw) }
        }

        // Stable ID scheme so that the same title/bootPath is not duplicated multiple times.
        val shortcutId = makeStableId(title, bootPathUri)

        val shortcut = ShortcutInfoCompat.Builder(context, shortcutId)
            .setShortLabel(title)
            .setLongLabel(title)
            .setIcon(icon)
            .setIntent(launchIntent)
            .build()

        // Custom app message (as before): Directly before the system PIN dialog
        Toast.makeText(context, "Creating shortcut “$title”…", Toast.LENGTH_SHORT).show()

        val callbackIntent = ShortcutManagerCompat.createShortcutResultIntent(context, shortcut)
        val successCallback = PendingIntent.getBroadcast(
            context,
            0,
            callbackIntent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )

        // Request to the launcher
        ShortcutManagerCompat.requestPinShortcut(context, shortcut, successCallback.intentSender)
    }

    private fun makeStableId(title: String?, bootPath: String?): String {
        val safeTitle = (title ?: "").trim()
        val safeBoot = (bootPath ?: "").trim()
        return "moonwitchrx_${safeTitle}_${safeBoot}".take(90) // The ID must be <100 characters
    }

    private fun decodeBase64ToBitmap(b64: String?): Bitmap? {
        if (b64.isNullOrBlank()) return null
        return runCatching {
            val bytes = Base64.decode(b64, Base64.DEFAULT)
            BitmapFactory.decodeByteArray(bytes, 0, bytes.size)
        }.getOrNull()
    }
}
