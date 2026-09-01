package org.kenjinx.android

import android.view.SurfaceView

class NativeWindow(val surface: SurfaceView) {
    var nativePointer: Long
    private val nativeHelpers: NativeHelpers = NativeHelpers.instance
    private var _swapInterval: Int = 0

    val maxSwapInterval: Int
        get() {
            return if (nativePointer == -1L) 0 else nativeHelpers.getMaxSwapInterval(nativePointer)
        }

    val minSwapInterval: Int
        get() {
            return if (nativePointer == -1L) 0 else nativeHelpers.getMinSwapInterval(nativePointer)
        }

    var swapInterval: Int
        get() {
            return _swapInterval
        }
        set(value) {
            if (nativePointer == -1L || nativeHelpers.setSwapInterval(nativePointer, value) == 0)
                _swapInterval = value
        }

    init {
        nativePointer = nativeHelpers.getNativeWindow(surface.holder.surface)

        swapInterval = maxOf(1, minSwapInterval)
    }

    fun requeryWindowHandle(): Long {
        nativePointer = nativeHelpers.getNativeWindow(surface.holder.surface)

        swapInterval = swapInterval

        return nativePointer
    }
}
