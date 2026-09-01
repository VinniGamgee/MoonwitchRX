package org.kenjinx.android.widgets

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.MutableState
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp

/**
 * A reusable switch component with consistent styling across the app.
 *
 * @param text Main text label for the switch
 * @param checked Current state of the switch
 * @param onCheckedChange Callback for when the switch is toggled
 * @param description Optional descriptive text to display below the main label
 * @param secondaryDescription Optional additional description text (displays as third line)
 * @param modifier Modifier for customizing the layout
 */
@Composable
fun SwitchSelector(
    label: String,
    checked: Boolean,
    onCheckedChange: (Boolean) -> Unit
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .height(56.dp)
            .padding(horizontal = 8.dp, vertical = 8.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(
            text = label,
            modifier = Modifier.weight(1f)
        )
        Switch(
            checked = checked,
            onCheckedChange = onCheckedChange,
        )
    }
}

@Composable
fun MutableState<Boolean>.SwitchSelector(
    label: String
) {
    SwitchSelector(
        label = label,
        checked = this.value,
        onCheckedChange = { this.value = it }
    )
}
