package org.kenjinx.android.cheats

import android.app.Activity
import android.util.Log
import java.io.File
import java.nio.charset.Charset
import android.net.Uri
import android.provider.OpenableColumns
import android.content.Intent

data class CheatItem(val buildId: String, val name: String) {
    val key get() = "$buildId-$name"
}

/* -------- Paths -------- */

private fun cheatsDirExternal(activity: Activity, titleId: String): File {
    val base = activity.getExternalFilesDir(null) // /storage/emulated/0/Android/data/<pkg>/files
    return File(base, "mods/contents/$titleId/cheats")
}

private fun allCheatDirs(activity: Activity, titleId: String): List<File> {
    return listOf(cheatsDirExternal(activity, titleId))
        .distinct()
        .filter { it.exists() && it.isDirectory }
}

/* -------- Parser -------- */

private fun parseCheatNames(text: String): List<String> {
    // Trim BOM, CRLF tolerant
    val clean = text.replace("\uFEFF", "")
    val rx = Regex("""(?m)^\s*\[(.+?)]\s*$""")
    return rx.findAll(clean)
        .map { it.groupValues[1].trim() }
        .filter { it.isNotEmpty() }
        .toList()
}

/* -------- Public: Loading Cheats -------- */

fun loadCheatsFromDisk(activity: Activity, titleId: String): List<CheatItem> {
    val dirs = allCheatDirs(activity, titleId)
    if (dirs.isEmpty()) {
        Log.d("CheatFs", "No cheat dirs for $titleId (checked internal+external).")
        return emptyList()
    }

    val out = mutableListOf<CheatItem>()
    for (dir in dirs) {
        dir.listFiles { f -> f.isFile && f.name.endsWith(".txt", ignoreCase = true) }?.forEach { file ->
            val buildId = file.nameWithoutExtension
            val text = runCatching { file.readText(Charset.forName("UTF-8")) }.getOrElse { "" }
            parseCheatNames(text).forEach { nm ->
                out += CheatItem(buildId, nm)
            }
        }
    }

    return out
        .distinctBy { it.key.lowercase() }
        .sortedWith(compareBy({ it.buildId.lowercase() }, { it.name.lowercase() }))
}

/* -------- Public: Apply selection to disk IMMEDIATELY -------- */

fun applyCheatSelectionOnDisk(activity: Activity, titleId: String, enabledKeys: Set<String>) {
    // We select exactly ONE BUILDID file (the "best" one), and enable sections within it.
    val dirs = allCheatDirs(activity, titleId)
    val allTxt = dirs.flatMap { d ->
        d.listFiles { f -> f.isFile && f.name.endsWith(".txt", ignoreCase = true) }?.toList() ?: emptyList()
    }
    if (allTxt.isEmpty()) {
        Log.d("CheatFs", "applyCheatSelectionOnDisk: no *.txt found for $titleId")
        return
    }

    val buildFile = pickBestBuildFile(allTxt)
    val text = runCatching { buildFile.readText(Charset.forName("UTF-8")) }.getOrElse { "" }
    if (text.isEmpty()) return

    // Normalize the enabled set: Keys are "<BUILDID>-<SectionName>"
    val enabledSections = enabledKeys.asSequence()
        .mapNotNull { key ->
            val dash = key.indexOf('-')
            if (dash <= 0) null else key.substring(dash + 1).trim()
        }
        .map { it.lowercase() }
        .toSet()

    val rewritten = rewriteCheatFile(text, enabledSections)

    runCatching {
        buildFile.writeText(rewritten, Charset.forName("UTF-8"))
    }.onFailure {
        Log.w("CheatFs", "Failed to write ${buildFile.absolutePath}: ${it.message}")
    }
}

/* -------- Implementation: Apply selection (only ';' is treated as a comment) -------- */

private fun pickBestBuildFile(files: List<File>): File {
    fun looksHexName(p: File): Boolean {
        val n = p.nameWithoutExtension
        return n.length >= 16 && n.all { it.isDigit() || it.lowercaseChar() in 'a'..'f' }
    }
    return files.firstOrNull(::looksHexName)
        ?: files.maxByOrNull { runCatching { it.lastModified() }.getOrDefault(0L) }
        ?: files.first()
}

private fun isSectionHeader(line: String): Boolean {
    val t = line.trim()
    return t.length > 2 && t.first() == '[' && t.contains(']')
}

private fun sectionNameFromHeader(line: String): String {
    val t = line.trim()
    val close = t.indexOf(']')
    return if (t.startsWith("[") && close > 1) t.substring(1, close).trim() else ""
}

/**
 * Removes a leading comment character (';') + optional whitespace.
 * Only at the very beginning of the line (no leading spaces allowed).
 */
private fun uncommentOnce(raw: String): String {
    if (raw.isEmpty()) return raw
    return if (raw.startsWith(";")) {
        raw.drop(1).let { if (it.startsWith(" ")) it.drop(1) else it }
    } else raw
}

/**
 * Comment out the line if it doesn't already start with ';'.
 * Atmosphere uses ';', which is what we use exclusively.
 */
private fun commentOut(raw: String): String {
    val t = raw.trimStart()
    if (t.isEmpty()) return raw
    if (t.startsWith(";")) return raw
    return "; $raw"
}

/**
 * Rewrites the file:
 * - Does not insert any markers
 * - Comments out/uncomments the body of each section according to its enabled/disabled status (enabledSections)
 * - Pure comment/empty lines (containing only ';') are preserved
 */
// Utility functions: trim trailing blank lines / normalize headers
private fun trimTrailingBlankLines(lines: MutableList<String>) {
    while (lines.isNotEmpty() && lines.last().trim().isEmpty()) {
        lines.removeAt(lines.lastIndex)
    }
}

private fun joinHeaderBufferOnce(header: List<String>): String {
    // Header lines remain unchanged, but remove trailing blanks and add exactly one blank line afterwards
    val buf = header.toMutableList()
    trimTrailingBlankLines(buf)
    return if (buf.isEmpty()) "" else buf.joinToString("\n") + "\n\n"
}

/**
 * Rewrites the file:
 * - No markers are inserted.
 * - For each section, the body is commented out/uncommented according to its enabled/disabled status (enabledSections)
 * - Pure comment lines and empty lines are preserved.
 * - Exactly ONE blank line between sections, and exactly ONE newline at the end.
 */
private fun rewriteCheatFile(original: String, enabledSections: Set<String>): String {
    val lines = original.replace("\uFEFF", "").lines()

    val out = StringBuilder(original.length + 1024)

    var currentSection: String? = null
    val currentBlock = ArrayList<String>()
    val headerBuffer = ArrayList<String>()
    var sawAnySection = false
    var wroteAnySection = false

    fun flushCurrent() {
        val sec = currentSection ?: return

        // Remove trailing blank lines in the block to prevent double spacing from occurring
        trimTrailingBlankLines(currentBlock)

        val enabled = enabledSections.contains(sec.lowercase())

        // Insert exactly one blank line between sections (but not before the first one)
        if (wroteAnySection) out.append('\n')

        out.append('[').append(sec).append(']').append('\n')

        if (enabled) {
            // Uncomment (only a leading ';' in column 0)
            for (l in currentBlock) {
                val trimmed = l.trim()
                if (trimmed.isEmpty() || (trimmed.startsWith(";") && trimmed.length <= 1)) {
                    out.append(l).append('\n')
                } else {
                    if (l.startsWith(";")) {
                        out.append(
                            l.drop(1).let { if (it.startsWith(" ")) it.drop(1) else it }
                        ).append('\n')
                    } else {
                        out.append(l).append('\n')
                    }
                }
            }
        } else {
            // Disable: comment out everything that doesn't already start with ';' and is not empty
            for (l in currentBlock) {
                val t = l.trim()
                if (t.isEmpty() || t.startsWith(";")) {
                    out.append(l).append('\n')
                } else {
                    out.append("; ").append(l).append('\n')
                }
            }
        }

        wroteAnySection = true
        currentSection = null
        currentBlock.clear()
    }

    for (raw in lines) {
        if (isSectionHeader(raw)) {
            flushCurrent()
            currentSection = sectionNameFromHeader(raw)
            sawAnySection = true
            continue
        }

        if (!sawAnySection) {
            headerBuffer.add(raw)
        } else {
            currentBlock.add(raw)
        }
    }
    flushCurrent()

    // Insert the header at the beginning (with exactly one blank line after it, if present)
    val headerText = joinHeaderBufferOnce(headerBuffer)
    if (headerText.isNotEmpty()) {
        out.insert(0, headerText)
    }

    // Global normalization: 3+ newlines -> 2, and exactly ONE '\n' at the end
    var result = out.toString()
        .replace(Regex("\n{3,}"), "\n\n") // no more than 1 blank line between sections
        .trimEnd() + "\n"                 // exactly one newline at the end

    return result
}
private fun cheatsDirPreferredForWrite(activity: Activity, titleId: String): File {
    val dir = cheatsDirExternal(activity, titleId)
    if (!dir.exists()) dir.mkdirs()
    return dir
}

private fun getDisplayName(activity: Activity, uri: Uri): String? {
    return runCatching {
        val cr = activity.contentResolver
        cr.query(uri, arrayOf(OpenableColumns.DISPLAY_NAME), null, null, null)?.use { c ->
            if (c.moveToFirst()) c.getString(0) else null
        }
    }.getOrNull()
}

private fun uniqueFile(targetDir: File, baseName: String): File {
    var name = baseName
    if (!name.lowercase().endsWith(".txt")) name += ".txt"
    var out = File(targetDir, name)
    var idx = 1
    val stem = name.substringBeforeLast(".")
    val ext = ".txt"
    while (out.exists()) {
        out = File(targetDir, "$stem ($idx)$ext")
        idx++
    }
    return out
}

/**
 * Imports a .txt file from a SAF URI into the title's cheats folder.
 * Returns the target file object if successful.
 */
fun importCheatTxt(activity: Activity, titleId: String, source: Uri): Result<File> {
    return runCatching {
        // Ensure read access is permanently secured, if necessary
        try {
            activity.contentResolver.takePersistableUriPermission(
                source,
                Intent.FLAG_GRANT_READ_URI_PERMISSION
            )
        } catch (_: Throwable) {}

        val targetDir = cheatsDirPreferredForWrite(activity, titleId)

        val display = getDisplayName(activity, source) ?: "cheats.txt"
        val target = uniqueFile(targetDir, display)

        activity.contentResolver.openInputStream(source).use { ins ->
            requireNotNull(ins) { "InputStream null" }
            target.outputStream().use { outs ->
                ins.copyTo(outs)
            }
        }

        // After import: immediate re-reading/normalization would be possible,
        // but we leave the file as delivered.
        target
    }
}
