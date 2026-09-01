package org.kenjinx.android

import android.app.ActivityManager
import android.content.Context.ACTIVITY_SERVICE
import android.util.Log
import androidx.compose.runtime.MutableState
import java.io.RandomAccessFile

object PerformanceMonitor {
    fun getFrequencies(frequencies: MutableList<Double>){
        frequencies.clear()
        for (i in 0 until Runtime.getRuntime().availableProcessors()) {
            runCatching {
                val raf = RandomAccessFile("/sys/devices/system/cpu/cpu$i/cpufreq/scaling_cur_freq", "r")

                frequencies.add(raf.use { it.readLine().toDouble() / 1000.0 })
            }.onFailure {
                Log.e("Performance Monitor", "Failed to read frequency of CPU core $i", it)
            }
        }
    }

    fun getMemoryUsage(
        usedMem: MutableState<Int>,
        totalMem: MutableState<Int>) {
        MainActivity.mainViewModel?.activity?.apply {
            val actManager = getSystemService(ACTIVITY_SERVICE) as ActivityManager
            val memInfo = ActivityManager.MemoryInfo()
            actManager.getMemoryInfo(memInfo)
            val availMemory = memInfo.availMem.toDouble() / (1024 * 1024)
            val totalMemory = memInfo.totalMem.toDouble() / (1024 * 1024)

            usedMem.value = (totalMemory - availMemory).toInt()
            totalMem.value = totalMemory.toInt()
        }
    }
}
