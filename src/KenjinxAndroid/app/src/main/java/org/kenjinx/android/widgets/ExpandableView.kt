package org.kenjinx.android.widgets

import android.annotation.SuppressLint
import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.core.MutableTransitionState
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.rememberTransition
import androidx.compose.animation.core.tween
import androidx.compose.animation.expandVertically
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.shrinkVertically
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.KeyboardArrowUp
import androidx.compose.material3.Card
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.rotate
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.unit.dp
import org.kenjinx.android.views.SettingViews.Companion.EXPANSTION_TRANSITION_DURATION

@OptIn(ExperimentalMaterial3Api::class)
@Composable
@SuppressLint("UnusedTransitionTargetStateParameter")
fun ExpandableView(
    onCardArrowClick: () -> Unit,
    title: String,
    icon: ImageVector,
    isFirst: Boolean = false,
    content: @Composable () -> Unit
) {
    val expanded = false
    val mutableExpanded = remember { mutableStateOf(expanded) }
    val transitionState = remember {
        MutableTransitionState(expanded).apply {
            targetState = !mutableExpanded.value
        }
    }
    val transition = rememberTransition(transitionState, label = "transition")
    val arrowRotationDegree = transition.animateFloat(
        transitionSpec = {
            tween(durationMillis = EXPANSTION_TRANSITION_DURATION)
        },
        label = "rotationDegreeTransition"
    ) {
        if (mutableExpanded.value) 0f else 180f
    }.value

    Card(
        shape = MaterialTheme.shapes.medium,
        modifier = Modifier
            .fillMaxWidth()
            .padding(
                top = if (isFirst) 8.dp else 4.dp,
                bottom = 4.dp,
                start = 12.dp,
                end = 12.dp
            )
    ) {
        Column {
            Card(
                onClick = {
                    mutableExpanded.value = !mutableExpanded.value
                    onCardArrowClick()
                },
                modifier = Modifier.fillMaxWidth()
            ) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 12.dp, vertical = 8.dp),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.Start
                    ) {
                        Icon(
                            imageVector = icon,
                            contentDescription = null,
                            modifier = Modifier.size(16.dp)
                        )
                        androidx.compose.foundation.layout.Spacer(
                            modifier = Modifier.padding(
                                end = 8.dp
                            )
                        )

                        Text(
                            text = title,
                            style = MaterialTheme.typography.titleMedium
                        )
                    }

                    Icon(
                        Icons.Filled.KeyboardArrowUp,
                        contentDescription = "Expandable Arrow",
                        modifier = Modifier.rotate(arrowRotationDegree)
                    )
                }
            }

            AnimatedVisibility(
                visible = mutableExpanded.value,
                enter = expandVertically(
                    expandFrom = Alignment.Top,
                    animationSpec = tween(EXPANSTION_TRANSITION_DURATION)
                ) + fadeIn(
                    initialAlpha = 0.3f,
                    animationSpec = tween(EXPANSTION_TRANSITION_DURATION)
                ),
                exit = shrinkVertically(
                    shrinkTowards = Alignment.Top,
                    animationSpec = tween(EXPANSTION_TRANSITION_DURATION)
                ) + fadeOut(
                    animationSpec = tween(EXPANSTION_TRANSITION_DURATION)
                )
            ) {
                Column(
                    modifier = Modifier.padding(
                        start = 8.dp,
                        end = 8.dp,
                        top = 0.dp,
                        bottom = 8.dp
                    )
                ) {
                    content()
                }
            }
        }
    }
}

@Composable
fun ExpandableContent(
    visible: Boolean = true,
    content: @Composable () -> Unit
) {
    val enterTransition = remember {
        expandVertically(
            expandFrom = Alignment.Top,
            animationSpec = tween(EXPANSTION_TRANSITION_DURATION)
        ) + fadeIn(
            initialAlpha = 0.3f,
            animationSpec = tween(EXPANSTION_TRANSITION_DURATION)
        )
    }
    val exitTransition = remember {
        shrinkVertically(
            // Expand from the top.
            shrinkTowards = Alignment.Top,
            animationSpec = tween(EXPANSTION_TRANSITION_DURATION)
        ) + fadeOut(
            // Fade in with the initial alpha of 0.3f.
            animationSpec = tween(EXPANSTION_TRANSITION_DURATION)
        )
    }

    AnimatedVisibility(
        visible = visible,
        enter = enterTransition,
        exit = exitTransition
    ) {
        Column(modifier = Modifier.padding(8.dp)) {
            content()
        }
    }
}
