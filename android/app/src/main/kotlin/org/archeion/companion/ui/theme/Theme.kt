package org.archeion.companion.ui.theme

import android.os.Build
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.platform.LocalContext

private val DarkColorScheme = darkColorScheme(
    primary = ArcheionDarkPrimary,
    onPrimary = ArcheionDarkOnPrimary,
    primaryContainer = ArcheionDarkPrimaryContainer,
    background = ArcheionDarkBackground,
    surface = ArcheionDarkSurface,
    onSurface = ArcheionDarkOnSurface
)

private val LightColorScheme = lightColorScheme(
    primary = ArcheionPrimary,
    onPrimary = ArcheionOnPrimary,
    primaryContainer = ArcheionPrimaryContainer,
    onPrimaryContainer = ArcheionOnPrimaryContainer,
    secondary = ArcheionSecondary,
    onSecondary = ArcheionOnSecondary,
    secondaryContainer = ArcheionSecondaryContainer,
    onSecondaryContainer = ArcheionOnSecondaryContainer,
    background = ArcheionBackground,
    onBackground = ArcheionOnBackground,
    surface = ArcheionSurface,
    onSurface = ArcheionOnSurface
)

@Composable
fun ArcheionCompanionTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    dynamicColor: Boolean = true,
    content: @Composable () -> Unit
) {
    val colorScheme = when {
        dynamicColor && Build.VERSION.SDK_INT >= Build.VERSION_CODES.S -> {
            val context = LocalContext.current
            if (darkTheme) dynamicDarkColorScheme(context) else dynamicLightColorScheme(context)
        }
        darkTheme -> DarkColorScheme
        else -> LightColorScheme
    }

    MaterialTheme(
        colorScheme = colorScheme,
        typography = Typography(),
        content = content
    )
}
