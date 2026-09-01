package org.kenjinx.android

import android.annotation.SuppressLint
import android.app.Activity
import android.app.AlertDialog
import android.content.Intent
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.net.Uri
import android.os.Bundle
import android.widget.EditText
import android.widget.Toast
import android.content.pm.ActivityInfo
import android.util.Base64
import androidx.documentfile.provider.DocumentFile
import org.kenjinx.android.viewmodels.GameInfo

@SuppressLint("SourceLockedOrientationActivity")
class ShortcutWizardActivity : Activity() {

    private val REQ_PICK_GAME = 1
    private val REQ_PICK_ICON = 2

    private var pickedGameUri: Uri? = null
    private var pendingLabel: String? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        // Portrait for the entire wizard, until shortcut pinning is finished
        requestedOrientation = ActivityInfo.SCREEN_ORIENTATION_PORTRAIT

        // Immediately upon startup: Select a game
        requestGameFile()
    }

    private fun requestGameFile() {
        val pick = Intent(Intent.ACTION_OPEN_DOCUMENT).apply {
            addCategory(Intent.CATEGORY_OPENABLE)
            type = "*/*"
            addFlags(
                Intent.FLAG_GRANT_PERSISTABLE_URI_PERMISSION or
                    Intent.FLAG_GRANT_READ_URI_PERMISSION or
                    Intent.FLAG_GRANT_WRITE_URI_PERMISSION
            )
        }
        startActivityForResult(pick, REQ_PICK_GAME)
    }

    @Deprecated("simple flow")
    override fun onActivityResult(requestCode: Int, resultCode: Int, data: Intent?) {
        super.onActivityResult(requestCode, resultCode, data)

        if (requestCode == REQ_PICK_GAME) {
            if (resultCode != RESULT_OK) { finish(); return }
            val uri = data?.data ?: run { finish(); return }
            pickedGameUri = uri
            ShortcutUtils.persistReadWrite(this, uri)

            val suggested = ShortcutUtils.suggestLabelFromUri(uri)
            val input = EditText(this).apply { setText(suggested) }

            AlertDialog.Builder(this)
                .setTitle("Shortcut name")
                .setView(input)
                .setPositiveButton("Next (Icon)") { _, _ ->
                    val label = input.text?.toString()?.takeIf { it.isNotBlank() } ?: suggested
                    pendingLabel = label
                    requestIconImage()
                }
                .setNeutralButton("Use app icon") { _, _ ->
                    val label = input.text?.toString()?.takeIf { it.isNotBlank() } ?: suggested
                    // Use the grid icon (GameInfo.Icon Base64) as the shortcut icon
                    val bmp = pickedGameUri?.let { loadGridIconBitmap(it) }
                    createShortcut(label, bmp) // Fallback auf App-Icon passiert intern in ShortcutUtils, wenn bmp=null ist
                }
                .setNegativeButton("Cancel") { _, _ -> finish() }
                .setCancelable(false)
                .show()
            return
        }

        if (requestCode == REQ_PICK_ICON) {
            if (resultCode != RESULT_OK) {
                // Cancellation → Fallback app icon (or grid icon would be optional here)
                val label = pendingLabel ?: "Start Game"
                createShortcut(label, null)
                return
            }
            val imageUri = data?.data
            val bmp = imageUri?.let { loadBitmap(it) }
            val label = pendingLabel ?: "Start Game"
            createShortcut(label, bmp)
            return
        }

        finish()
    }

    private fun requestIconImage() {
        val pickIcon = Intent(Intent.ACTION_OPEN_DOCUMENT).apply {
            addCategory(Intent.CATEGORY_OPENABLE)
            type = "image/*"
            addFlags(
                Intent.FLAG_GRANT_PERSISTABLE_URI_PERMISSION or
                    Intent.FLAG_GRANT_READ_URI_PERMISSION
            )
        }
        startActivityForResult(Intent.createChooser(pickIcon, "Select icon"), REQ_PICK_ICON)
    }

    private fun loadBitmap(uri: Uri): Bitmap? =
        try {
            contentResolver.takePersistableUriPermission(uri, Intent.FLAG_GRANT_READ_URI_PERMISSION)
            contentResolver.openInputStream(uri).use { BitmapFactory.decodeStream(it) }
        } catch (_: Exception) {
            null
        }

    /**
     * Retrieves the same icon that is displayed in the game grid:
     * - Opens the FileDescriptor
     * - Retrieves native GameInfo (including Base64 icon)
     * - Decodes to Bitmap
     */
    private fun loadGridIconBitmap(gameUri: Uri): Bitmap? {
        return try {
            val doc = DocumentFile.fromSingleUri(this, gameUri)
            val name = doc?.name ?: return null
            val ext = name.substringAfterLast('.', "").lowercase()

            val pfd = contentResolver.openFileDescriptor(gameUri, "r") ?: return null
            pfd.use {
                val info = GameInfo()
                // Native call: deviceGetGameInfo(fd, extension, info)
                KenjinxNative.deviceGetGameInfo(it.fd, ext, info)
                val b64 = info.Icon ?: return null
                val bytes = Base64.decode(b64, Base64.DEFAULT)
                BitmapFactory.decodeByteArray(bytes, 0, bytes.size)
            }
        } catch (_: Exception) {
            null
        }
    }

    private fun createShortcut(label: String, bmp: Bitmap?) {
        val gameUri = pickedGameUri
        if (gameUri == null) { finish(); return }

        val ok = ShortcutUtils.pinShortcutForGame(
            activity = this,
            gameUri = gameUri,
            label = label,
            iconBitmap = bmp
        ) {
            // will only be called when Portrait mode has been re-enabled
            finish()
        }

        // No finish() here! — we're waiting for the callback
        Toast.makeText(
            this,
            if (ok) "Shortcut “$label” created." else "Shortcut failed.",
            Toast.LENGTH_SHORT
        ).show()
    }

    override fun onResume() {
        super.onResume()
        requestedOrientation = ActivityInfo.SCREEN_ORIENTATION_PORTRAIT
    }
}
