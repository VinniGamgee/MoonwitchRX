package org.kenjinx.android.viewmodels

/**
 * Single source of truth for Android settings defaults.
 *
 * The upstream UI and runtime loader previously disagreed on several defaults,
 * so the value shown in Settings could differ from the value used to boot a game.
 */
object SettingsDefaults {
    val memoryManagerMode = MemoryManagerMode.HostMappedUnsafe
    val memoryConfiguration = MemoryConfiguration.MemoryConfiguration4GiB
    val vSyncMode = VSyncMode.Switch

    // ARM64 Android devices can use NCE. It is the performance-oriented default
    // for MoonwitchRX and can still be disabled for compatibility.
    const val useNce = true
    const val enablePptc = true
    const val enableLowPowerPptc = false
    const val enableJitCacheEviction = true

    const val enableDocked = false
    const val enableShaderCache = true
    const val enableTextureRecompression = false
    const val enableMacroHLE = true
    const val enablePerformanceMode = true
    const val disableThreadedRendering = false
}
