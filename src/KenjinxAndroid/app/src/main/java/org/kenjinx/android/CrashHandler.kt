package org.kenjinx.android

import android.os.Build
import java.io.File
import java.io.PrintWriter
import java.io.StringWriter
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import java.lang.Thread.UncaughtExceptionHandler

/**
 * Handles uncaught exceptions in the application and logs them to a file
 * Location: src/main/java/org/kenjinx/android/CrashHandler.kt
 */
class CrashHandler : UncaughtExceptionHandler {
    private var crashLog: StringBuilder = StringBuilder()
    private val dateFormat = SimpleDateFormat("yyyy-MM-dd HH:mm:ss", Locale.getDefault())
    
    companion object {
        private const val LOG_FOLDER = "Logs"
        private const val CRASH_LOG_FILE = "crash.log"
        private const val MAX_LOG_SIZE = 5 * 1024 * 1024 // 5MB
    }

    override fun uncaughtException(t: Thread, e: Throwable) {
        try {
            generateCrashLog(t, e)
            saveCrashLog()
        } catch (writeError: Exception) {
            // If we can't write to the crash log, print to system log as fallback
            writeError.printStackTrace()
        }
    }

    private fun generateCrashLog(thread: Thread, throwable: Throwable) {
        crashLog.apply {
            append("\n=== Crash Report ===\n")
            append("Date: ${dateFormat.format(Date())}\n")
            
            // Device Information
            append("\n=== Device Info ===\n")
            append("Device: ${Build.MANUFACTURER} ${Build.MODEL}\n")
            append("Android Version: ${Build.VERSION.RELEASE} (SDK ${Build.VERSION.SDK_INT})\n")
            append("Device Fingerprint: ${Build.FINGERPRINT}\n")
            
            // Thread Information
            append("\n=== Thread Info ===\n")
            append("Thread Name: ${thread.name}\n")
            append("Thread ID: ${thread.id}\n")
            append("Thread Priority: ${thread.priority}\n")
            append("Thread State: ${thread.state}\n")
            
            // Exception Details
            append("\n=== Exception Info ===\n")
            append("Exception Class: ${throwable.javaClass.name}\n")
            append("Exception Message: ${throwable.message}\n")
            
            // Stack Trace
            append("\n=== Stack Trace ===\n")
            val sw = StringWriter()
            throwable.printStackTrace(PrintWriter(sw))
            append(sw.toString())
            
            // Cause (if any)
            throwable.cause?.let { cause ->
                append("\n=== Cause ===\n")
                val causeSw = StringWriter()
                cause.printStackTrace(PrintWriter(causeSw))
                append(causeSw.toString())
            }
            
            append("\n=== End of Crash Report ===\n")
            append("----------------------------------------\n")
        }
    }

    private fun saveCrashLog() {
        val logDir = File(MainActivity.AppPath, LOG_FOLDER).apply {
            if (!exists()) mkdirs()
        }

        val crashLogFile = File(logDir, CRASH_LOG_FILE)
        
        // Rotate log if it's too large
        if (crashLogFile.exists() && crashLogFile.length() > MAX_LOG_SIZE) {
            val backupFile = File(logDir, "${CRASH_LOG_FILE}.old")
            if (backupFile.exists()) backupFile.delete()
            crashLogFile.renameTo(backupFile)
        }

        // Write the crash log
        crashLogFile.appendText(crashLog.toString())
    }
}