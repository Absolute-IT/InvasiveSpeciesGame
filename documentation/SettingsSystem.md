# Settings System Documentation

## Overview

The settings system in Invasive Species Australia provides comprehensive display and audio configuration options, including support for multiple resolutions, UI scaling for high-DPI displays (like macOS Retina), fullscreen mode, and audio controls.

## Components

### 1. SettingsManager (Singleton)

**Location**: `scripts/systems/SettingsManager.cs`

The SettingsManager is a global singleton that manages all game settings and persists them to disk.

#### Key Features:
- **Display Settings**: Resolution, UI scale, fullscreen mode
- **Audio Settings**: Master volume, music volume/enable, SFX volume/enable
- **Persistence**: Saves settings to `user://settings.cfg`
- **High-DPI Support**: Proper scaling for retina displays

#### Available Resolutions:
- 1920×1080 (Full HD)
- 2560×1440 (2K)
- 3840×2160 (4K) - Default
- 5120×2880 (5K)

#### Scale Factors:
- 50%, 75%, 100% (default), 125%, 150%, 200%

### 2. Settings Scene

**Location**: `scenes/Settings.tscn`
**Script**: `scripts/Settings.cs`

The settings menu UI that allows players to configure game settings.

#### UI Components:
- **Resolution Dropdown**: Select from available resolutions
- **UI Scale Dropdown**: Adjust UI scaling for high-DPI displays
- **Fullscreen Checkbox**: Toggle fullscreen mode
- **Master Volume Slider**: Control overall game volume
- **Music Volume Slider**: Control music volume with enable/disable checkbox
- **SFX Volume Slider**: Control sound effects volume with enable/disable checkbox
- **Apply Button**: Apply display settings changes
- **Back Button**: Return to main menu

## Implementation Details

### Display Settings

Display settings use a pending/apply pattern to prevent jarring changes:
1. User changes resolution/scale/fullscreen
2. Changes are stored as pending
3. Apply button becomes enabled
4. Clicking Apply applies all pending display changes at once

### Audio Settings

Audio settings apply immediately for better user experience:
- Volume changes are heard in real-time
- Enable/disable toggles take effect immediately
- Visual feedback shows disabled state (dimmed sliders)

### High-DPI/Retina Support

The system properly handles high-DPI displays:
- **ContentScaleSize**: Sets the internal rendering resolution
- **Window Size**: Actual window size is `resolution × scale`
- **ContentScaleMode**: Set to Viewport for proper scaling
- **ContentScaleAspect**: Set to Keep to maintain aspect ratio

Example: On a Retina display with 4K resolution at 200% scale:
- Internal render: 3840×2160
- Window size: 7680×4320
- Result: Crisp UI on high-DPI displays

### Audio Bus Configuration

The system creates audio buses if they don't exist:
- **Master**: Main audio bus
- **Music**: Background music bus (routes to Master)
- **SFX**: Sound effects bus (routes to Master)

## Usage

### From Code

```csharp
// Access the singleton
var settings = SettingsManager.Instance;

// Change settings
settings.SetResolution(new Vector2I(1920, 1080));
settings.SetScale(1.5f);
settings.SetFullscreen(true);
settings.SetMasterVolume(0.8f);
settings.SetMusicEnabled(false);

// Read current settings
var currentRes = settings.CurrentResolution;
var isFullscreen = settings.IsFullscreen;
```

### Playing Audio

When playing audio, assign the appropriate bus:

```csharp
// For music
musicPlayer.Bus = "Music";

// For sound effects
sfxPlayer.Bus = "SFX";
```

## UI Adaptation

The game uses Godot's built-in content scaling system:
- **Stretch Mode**: `canvas_items` - UI scales with resolution
- **Stretch Aspect**: `keep` - Maintains aspect ratio
- **Design Resolution**: 3840×2160 (4K)

All UI should be designed at 4K resolution. The content scaling system automatically handles:
- Downscaling for lower resolutions
- Maintaining crisp visuals at all scales
- Proper aspect ratio with letterboxing if needed

## File Storage

Settings are saved to:
- **Windows**: `%APPDATA%\Godot\app_userdata\InvasiveSpeciesAustralia\settings.cfg`
- **macOS**: `~/Library/Application Support/Godot/app_userdata/InvasiveSpeciesAustralia/settings.cfg`
- **Linux**: `~/.local/share/godot/app_userdata/InvasiveSpeciesAustralia/settings.cfg`

## Best Practices

1. **UI Design**: Always design UI at 3840×2160 resolution
2. **Audio Buses**: Always assign audio players to appropriate buses
3. **Testing**: Test UI at different resolutions and scales
4. **Performance**: Higher resolutions impact performance - test on target hardware

## Troubleshooting

### UI appears too small/large
- Adjust the UI Scale setting in the settings menu
- Ensure your display DPI settings are configured correctly in your OS

### Audio not affected by volume settings
- Ensure audio players have the correct bus assigned
- Check that audio buses exist (SettingsManager creates them automatically)

### Settings not persisting
- Check file permissions in the user data directory
- Look for error messages in the console about save failures 