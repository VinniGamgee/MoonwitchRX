package org.kenjinx.android

import android.view.InputDevice
import android.view.KeyEvent
import android.view.MotionEvent
import org.kenjinx.android.viewmodels.QuickSettings
import kotlin.math.abs

class PhysicalControllerManager(val activity: MainActivity) {
    private var controllerId: Int = -1

    // Trigger debounce (analog → digital)
    private var leftTriggerPressed = false
    private var rightTriggerPressed = false
    private val pressThreshold = 0.65f
    private val releaseThreshold = 0.45f

    fun onKeyEvent(event: KeyEvent): Boolean {
        // Make sure we are connected
        if (controllerId == -1) {
            controllerId = KenjinxNative.inputConnectGamepad(0)
        }

        val id = getGamePadButtonInputId(event.keyCode)
        if (id != GamePadButtonInputId.None) {
            val isNotFallback = (event.flags and KeyEvent.FLAG_FALLBACK) == 0
            if (isNotFallback) {
                when (event.action) {
                    KeyEvent.ACTION_UP   -> KenjinxNative.inputSetButtonReleased(id.ordinal, controllerId)
                    KeyEvent.ACTION_DOWN -> KenjinxNative.inputSetButtonPressed(id.ordinal, controllerId)
                }
            }
            return true
        }
        return false
    }

    fun onMotionEvent(ev: MotionEvent) {
        if (ev.action != MotionEvent.ACTION_MOVE) return

        if (controllerId == -1) {
            controllerId = KenjinxNative.inputConnectGamepad(0)
        }

        val device = ev.device
        val source = InputDevice.SOURCE_JOYSTICK

        fun hasAxis(axis: Int): Boolean =
            device?.getMotionRange(axis, source) != null

        fun axisValue(axis: Int): Float = ev.getAxisValue(axis)

        // --- Sticks (right side prefers RX/RY, fallback Z/RZ) ---
        val rightXaxis = if (hasAxis(MotionEvent.AXIS_RX)) MotionEvent.AXIS_RX else MotionEvent.AXIS_Z
        val rightYaxis = if (hasAxis(MotionEvent.AXIS_RY)) MotionEvent.AXIS_RY else MotionEvent.AXIS_RZ

        val leftStickX  = if (hasAxis(MotionEvent.AXIS_X)) axisValue(MotionEvent.AXIS_X) else 0f
        val leftStickY  = if (hasAxis(MotionEvent.AXIS_Y)) axisValue(MotionEvent.AXIS_Y) else 0f
        val rightStickX = if (hasAxis(rightXaxis)) axisValue(rightXaxis) else 0f
        val rightStickY = if (hasAxis(rightYaxis)) axisValue(rightYaxis) else 0f

        KenjinxNative.inputSetStickAxis(1, leftStickX,  -leftStickY,  controllerId)
        KenjinxNative.inputSetStickAxis(2, rightStickX, -rightStickY, controllerId)

        // --- Reading the triggers (with fallbacks) ---
        // Preferred: LTRIGGER/RTRIGGER, then BRAKE/GAS.
        // If the right stick uses RX/RY (standard on Xbox), Z/RZ are free -> use them as a further fallback.
        // If the stick uses Z/RZ, these are NOT used for triggers (to avoid conflicts).
        val rightStickUsesZ  = (rightXaxis == MotionEvent.AXIS_Z)
        val rightStickUsesRZ = (rightYaxis == MotionEvent.AXIS_RZ)

        val rawLT = when {
            hasAxis(MotionEvent.AXIS_LTRIGGER) -> axisValue(MotionEvent.AXIS_LTRIGGER)
            hasAxis(MotionEvent.AXIS_BRAKE)    -> axisValue(MotionEvent.AXIS_BRAKE)
            !rightStickUsesZ && hasAxis(MotionEvent.AXIS_Z) -> axisValue(MotionEvent.AXIS_Z)
            else -> 0f
        }
        val rawRT = when {
            hasAxis(MotionEvent.AXIS_RTRIGGER) -> axisValue(MotionEvent.AXIS_RTRIGGER)
            hasAxis(MotionEvent.AXIS_GAS)      -> axisValue(MotionEvent.AXIS_GAS)
            !rightStickUsesRZ && hasAxis(MotionEvent.AXIS_RZ) -> axisValue(MotionEvent.AXIS_RZ)
            else -> 0f
        }

        // Some pads produce slight offsets – normalize them
        val lt = if (abs(rawLT) < 0.02f) 0f else rawLT.coerceIn(0f, 1f)
        val rt = if (abs(rawRT) < 0.02f) 0f else rawRT.coerceIn(0f, 1f)

        // Analog to digital conversion with hysteresis
        if (!leftTriggerPressed && lt >= pressThreshold) {
            leftTriggerPressed = true
            KenjinxNative.inputSetButtonPressed(GamePadButtonInputId.LeftTrigger.ordinal, controllerId)
        } else if (leftTriggerPressed && lt <= releaseThreshold) {
            leftTriggerPressed = false
            KenjinxNative.inputSetButtonReleased(GamePadButtonInputId.LeftTrigger.ordinal, controllerId)
        }

        if (!rightTriggerPressed && rt >= pressThreshold) {
            rightTriggerPressed = true
            KenjinxNative.inputSetButtonPressed(GamePadButtonInputId.RightTrigger.ordinal, controllerId)
        } else if (rightTriggerPressed && rt <= releaseThreshold) {
            rightTriggerPressed = false
            KenjinxNative.inputSetButtonReleased(GamePadButtonInputId.RightTrigger.ordinal, controllerId)
        }

        // --- DPAD as HAT (as before) ---
        device?.apply {
            if (sources and InputDevice.SOURCE_DPAD != InputDevice.SOURCE_DPAD) {
                val dPadHor  = ev.getAxisValue(MotionEvent.AXIS_HAT_X)
                val dPadVert = ev.getAxisValue(MotionEvent.AXIS_HAT_Y)

                if (dPadVert == 0.0f) {
                    KenjinxNative.inputSetButtonReleased(GamePadButtonInputId.DpadUp.ordinal, controllerId)
                    KenjinxNative.inputSetButtonReleased(GamePadButtonInputId.DpadDown.ordinal, controllerId)
                }
                if (dPadHor == 0.0f) {
                    KenjinxNative.inputSetButtonReleased(GamePadButtonInputId.DpadLeft.ordinal, controllerId)
                    KenjinxNative.inputSetButtonReleased(GamePadButtonInputId.DpadRight.ordinal, controllerId)
                }

                if (dPadVert < 0.0f) {
                    KenjinxNative.inputSetButtonPressed(GamePadButtonInputId.DpadUp.ordinal, controllerId)
                    KenjinxNative.inputSetButtonReleased(GamePadButtonInputId.DpadDown.ordinal, controllerId)
                }
                if (dPadHor < 0.0f) {
                    KenjinxNative.inputSetButtonPressed(GamePadButtonInputId.DpadLeft.ordinal, controllerId)
                    KenjinxNative.inputSetButtonReleased(GamePadButtonInputId.DpadRight.ordinal, controllerId)
                }

                if (dPadVert > 0.0f) {
                    KenjinxNative.inputSetButtonReleased(GamePadButtonInputId.DpadUp.ordinal, controllerId)
                    KenjinxNative.inputSetButtonPressed(GamePadButtonInputId.DpadDown.ordinal, controllerId)
                }
                if (dPadHor > 0.0f) {
                    KenjinxNative.inputSetButtonReleased(GamePadButtonInputId.DpadLeft.ordinal, controllerId)
                    KenjinxNative.inputSetButtonPressed(GamePadButtonInputId.DpadRight.ordinal, controllerId)
                }
            }
        }
    }

    fun connect(): Int {
        controllerId = KenjinxNative.inputConnectGamepad(0)
        return controllerId
    }

    fun disconnect() {
        controllerId = -1
        // If a trigger got "stuck" during the disconnect, release it as a precaution
        if (leftTriggerPressed) {
            leftTriggerPressed = false
            KenjinxNative.inputSetButtonReleased(GamePadButtonInputId.LeftTrigger.ordinal, controllerId)
        }
        if (rightTriggerPressed) {
            rightTriggerPressed = false
            KenjinxNative.inputSetButtonReleased(GamePadButtonInputId.RightTrigger.ordinal, controllerId)
        }
    }

    private fun getGamePadButtonInputId(keycode: Int): GamePadButtonInputId {
        val quickSettings = QuickSettings(activity)
        return when (keycode) {
            // ABXY (Switch/Xbox Layout)
            KeyEvent.KEYCODE_BUTTON_A -> if (!quickSettings.useSwitchLayout) GamePadButtonInputId.A else GamePadButtonInputId.B
            KeyEvent.KEYCODE_BUTTON_B -> if (!quickSettings.useSwitchLayout) GamePadButtonInputId.B else GamePadButtonInputId.A
            KeyEvent.KEYCODE_BUTTON_X -> if (!quickSettings.useSwitchLayout) GamePadButtonInputId.X else GamePadButtonInputId.Y
            KeyEvent.KEYCODE_BUTTON_Y -> if (!quickSettings.useSwitchLayout) GamePadButtonInputId.Y else GamePadButtonInputId.X

            // Shoulder & Trigger (in case a pad sends them as keys)
            KeyEvent.KEYCODE_BUTTON_L1 -> GamePadButtonInputId.LeftShoulder
            KeyEvent.KEYCODE_BUTTON_L2 -> GamePadButtonInputId.LeftTrigger
            KeyEvent.KEYCODE_BUTTON_R1 -> GamePadButtonInputId.RightShoulder
            KeyEvent.KEYCODE_BUTTON_R2 -> GamePadButtonInputId.RightTrigger

            // L3 / R3
            KeyEvent.KEYCODE_BUTTON_THUMBL -> GamePadButtonInputId.LeftStickButton
            KeyEvent.KEYCODE_BUTTON_THUMBR -> GamePadButtonInputId.RightStickButton
            KeyEvent.KEYCODE_BUTTON_11     -> GamePadButtonInputId.LeftStickButton
            KeyEvent.KEYCODE_BUTTON_12     -> GamePadButtonInputId.RightStickButton

            // D-Pad
            KeyEvent.KEYCODE_DPAD_UP    -> GamePadButtonInputId.DpadUp
            KeyEvent.KEYCODE_DPAD_DOWN  -> GamePadButtonInputId.DpadDown
            KeyEvent.KEYCODE_DPAD_LEFT  -> GamePadButtonInputId.DpadLeft
            KeyEvent.KEYCODE_DPAD_RIGHT -> GamePadButtonInputId.DpadRight

            // Plus/Minus
            KeyEvent.KEYCODE_BUTTON_START  -> GamePadButtonInputId.Plus
            KeyEvent.KEYCODE_BUTTON_SELECT -> GamePadButtonInputId.Minus

            else -> GamePadButtonInputId.None
        }
    }
}
