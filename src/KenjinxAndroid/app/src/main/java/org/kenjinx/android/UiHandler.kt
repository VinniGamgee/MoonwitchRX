package org.kenjinx.android

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Text
import androidx.compose.material3.TextField
import androidx.compose.runtime.Composable
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import androidx.compose.ui.platform.LocalSoftwareKeyboardController
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.DialogProperties
import com.halilibo.richtext.markdown.Markdown
import com.halilibo.richtext.ui.material3.RichText
import org.kenjinx.android.widgets.SimpleAlertDialog
import kotlinx.coroutines.delay

enum class KeyboardMode {
    Default, Numeric, ASCII, FullLatin, Alphabet, SimplifiedChinese, TraditionalChinese, Korean, LanguageSet2, LanguageSet2Latin
}

class UiHandler {
    private var initialText: String = ""
    private var subtitle: String = ""
    private var maxLength: Int = 0
    private var minLength: Int = 0
    private var watermark: String = ""
    private var type: Int = -1
    private var mode: KeyboardMode = KeyboardMode.Default
    val showMessage = mutableStateOf(false)
    val inputText = mutableStateOf("")
    var title: String = ""
    var message: String = ""

    init {
        KenjinxNative.uiHandlerSetup()
    }

    fun update(
        newTitle: String,
        newMessage: String,
        newWatermark: String,
        newType: Int,
        min: Int,
        max: Int,
        newMode: KeyboardMode,
        newSubtitle: String,
        newInitialText: String
    ) {
        title = newTitle
        message = newMessage
        watermark = newWatermark
        type = newType
        minLength = min
        maxLength = max
        mode = newMode
        subtitle = newSubtitle
        initialText = newInitialText
        inputText.value = initialText
        showMessage.value = type > 0
    }

    @OptIn(ExperimentalMaterial3Api::class)
    @Composable
    fun Compose() {
        val showMessageListener = remember { showMessage }
        val inputListener = remember { inputText }
        val validation = remember { mutableStateOf("") }

        val focusRequester = remember { FocusRequester() }
        val keyboard = LocalSoftwareKeyboardController.current

        LaunchedEffect(showMessageListener.value, type) {
            if (showMessageListener.value && type == 2) {
                delay(100)
                focusRequester.requestFocus()
                keyboard?.show()
            }
        }

        fun validate(): Boolean {
            if (inputText.value.isEmpty()) {
                validation.value = "Must be between $minLength and $maxLength characters"
            } else {
                return inputText.value.length !in minLength..maxLength
            }
            return false
        }

        fun getInputType(): KeyboardType {
            return when (mode) {
                KeyboardMode.Default -> KeyboardType.Text
                KeyboardMode.Numeric -> KeyboardType.Decimal
                KeyboardMode.ASCII -> KeyboardType.Ascii
                else -> KeyboardType.Text
            }
        }

        fun submit() {
            if (type == 2) {
                if (inputListener.value.length !in minLength..maxLength) return
            }
            KenjinxNative.uiHandlerSetResponse(true, if (type == 2) inputListener.value else "")
            showMessageListener.value = false
        }

        SimpleAlertDialog.Custom(
            showDialog = showMessageListener,
            onDismissRequest = { },
            properties = DialogProperties(dismissOnBackPress = false, dismissOnClickOutside = false)
        ) {
            Column(
                modifier = Modifier
                    .padding(16.dp)
                    .fillMaxWidth(),
                verticalArrangement = Arrangement.SpaceBetween,
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Text(
                    text = title,
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(bottom = 16.dp),
                    textAlign = TextAlign.Center
                )

                Column(
                    modifier = Modifier
                        .height(128.dp)
                        .verticalScroll(rememberScrollState())
                        .padding(8.dp),
                    verticalArrangement = Arrangement.Center
                ) {
                    RichText {
                        Markdown(content = message)
                    }

                    if (type == 2) {
                        validate()
                        if (watermark.isNotEmpty()) {
                            TextField(
                                value = inputListener.value,
                                onValueChange = { inputListener.value = it },
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(4.dp)
                                    .focusRequester(focusRequester),
                                label = { Text(text = watermark) },
                                keyboardOptions = KeyboardOptions(keyboardType = getInputType()),
                                isError = validate()
                            )
                        } else {
                            TextField(
                                value = inputListener.value,
                                onValueChange = { inputListener.value = it },
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(4.dp)
                                    .focusRequester(focusRequester),
                                keyboardOptions = KeyboardOptions(
                                    keyboardType = getInputType(),
                                    imeAction = ImeAction.Done
                                ),
                                isError = validate(),
                                singleLine = true,
                                keyboardActions = KeyboardActions(onDone = { submit() })
                            )
                        }

                        if (subtitle.isNotEmpty()) {
                            Text(text = subtitle)
                        }

                        Text(text = validation.value)
                    }
                }

                Row(
                    horizontalArrangement = Arrangement.SpaceEvenly,
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(top = 16.dp)
                ) {
                    Button(
                        onClick = { submit() },
                        modifier = Modifier
                            .weight(1f)
                            .padding(horizontal = 8.dp)
                    ) {
                        Text(text = "Ok")
                    }
                }
            }
        }
    }
}
