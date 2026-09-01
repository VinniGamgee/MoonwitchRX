package org.kenjinx.android.widgets

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.wrapContentHeight
import androidx.compose.material3.AlertDialogDefaults
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.MutableState
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalConfiguration
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import androidx.compose.ui.window.DialogProperties

@OptIn(ExperimentalMaterial3Api::class)
object SimpleAlertDialog {

    @Composable
    fun Loading(
        showDialog: MutableState<Boolean>,
        title: String = "Loading",
        isDeterminate: Boolean = false,
        progress: Float = 0f,
        widthFraction: Float = 0.6f,
        onDismissRequest: (() -> Unit)? = null
    ) {
        val screenWidth = LocalConfiguration.current.screenWidthDp
        if (showDialog.value) {
            Dialog(
                onDismissRequest = {
                    onDismissRequest?.invoke() ?: run { showDialog.value = false }
                },
                properties = DialogProperties(usePlatformDefaultWidth = false)
            ) {
                Box(
                    modifier = Modifier.fillMaxSize(),
                    contentAlignment = Alignment.Center
                ) {
                    Card(
                        modifier = Modifier
                            .width((screenWidth * widthFraction).dp)
                            .wrapContentHeight()
                            .padding(8.dp),
                        shape = MaterialTheme.shapes.medium
                    ) {
                        Column(
                            modifier = Modifier
                                .padding(16.dp)
                                .fillMaxWidth()
                        ) {
                            Text(
                                text = title,
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(bottom = 16.dp),
                                textAlign = TextAlign.Center
                            )

                            if (isDeterminate) {
                                LinearProgressIndicator(
                                    progress = { progress },
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .padding(top = 16.dp)
                                )
                            } else {
                                LinearProgressIndicator(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .padding(top = 16.dp)
                                )
                            }
                        }
                    }
                }
            }
        }
    }

    @Composable
    fun Confirmation(
        showDialog: MutableState<Boolean>,
        title: String,
        message: String,
        confirmText: String = "Yes",
        dismissText: String = "No",
        widthFraction: Float = 0.6f,
        onConfirm: () -> Unit,
        onDismiss: () -> Unit = { showDialog.value = false }
    ) {
        val screenWidth = LocalConfiguration.current.screenWidthDp
        if (showDialog.value) {
            Dialog(
                onDismissRequest = { showDialog.value = false },
                properties = DialogProperties(usePlatformDefaultWidth = false)
            ) {
                Box(
                    modifier = Modifier.fillMaxSize(),
                    contentAlignment = Alignment.Center
                ) {
                    Card(
                        modifier = Modifier
                            .width((screenWidth * widthFraction).dp)
                            .wrapContentHeight()
                            .padding(8.dp),
                        shape = MaterialTheme.shapes.medium
                    ) {
                        Column(
                            modifier = Modifier
                                .padding(16.dp)
                                .fillMaxWidth()
                        ) {
                            Text(
                                text = title,
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(bottom = 16.dp),
                                textAlign = TextAlign.Center
                            )
                            Text(
                                text = message,
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(start = 8.dp, bottom = 8.dp),
                                textAlign = TextAlign.Start
                            )

                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(horizontal = 8.dp, vertical = 8.dp),
                                horizontalArrangement = Arrangement.SpaceEvenly,
                            ) {
                                Button(
                                    onClick = {
                                        showDialog.value = false
                                        onDismiss()
                                    },
                                    modifier = Modifier
                                        .weight(1f)
                                        .padding(end = 8.dp)
                                ) {
                                    Text(text = dismissText)
                                }
                                Button(
                                    onClick = {
                                        showDialog.value = false
                                        onConfirm()
                                    },
                                    modifier = Modifier
                                        .weight(1f)
                                        .padding(start = 8.dp)
                                ) {
                                    Text(text = confirmText)
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    @Composable
    fun Custom(
        showDialog: MutableState<Boolean>,
        onDismissRequest: () -> Unit = { showDialog.value = false },
        properties: DialogProperties = DialogProperties(usePlatformDefaultWidth = false),
        widthFraction: Float = 0.6f,
        content: @Composable () -> Unit
    ) {
        val screenWidth = LocalConfiguration.current.screenWidthDp
        if (showDialog.value) {
            Dialog(
                onDismissRequest = onDismissRequest,
                properties = properties
            ) {
                Box(
                    modifier = Modifier.fillMaxSize(),
                    contentAlignment = Alignment.Center
                ) {
                    Surface(
                        modifier = Modifier
                            .width((screenWidth * widthFraction).dp)
                            .wrapContentHeight(),
                        shape = MaterialTheme.shapes.large,
                        tonalElevation = AlertDialogDefaults.TonalElevation
                    ) {
                        content()
                    }
                }
            }
        }
    }

    @Composable
    fun Progress(
        showDialog: MutableState<Boolean>,
        progressText: String,
        progressValue: Float
    ) {
        val isDeterminate = progressValue > -1

        Loading(
            showDialog = showDialog,
            title = progressText,
            isDeterminate = isDeterminate,
            progress = if (isDeterminate) progressValue else 0f,
            onDismissRequest = null
        )
    }
}
