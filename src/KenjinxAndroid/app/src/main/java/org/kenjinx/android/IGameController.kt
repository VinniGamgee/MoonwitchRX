package org.kenjinx.android

interface IGameController {
    val isVisible: Boolean
    fun setVisible(isVisible: Boolean)
    fun connect()
}
