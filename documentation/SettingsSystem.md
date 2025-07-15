# Settings System

The settings system provides a centralized way to manage game settings including display, audio, and debug options.

## Components

### SettingsManager (Singleton)

Located at `scripts/systems/SettingsManager.cs`

This singleton manages all game settings and persists them to disk. It's configured as an autoload in `project.godot`.

**Key Features:**
- Display settings: Resolution, UI scale, fullscreen mode
- Audio settings: Master/Music/SFX volumes and enable states
- Debug settings: Touch debugger visibility
- Automatic persistence to `user://settings.cfg`

### Settings UI

Located at `scenes/Settings.tscn` with script `scripts/Settings.cs`

The UI provides controls for all settings:
- Display controls use the Apply button pattern (changes are pending until applied)
- Audio controls apply immediately
- Debug controls apply immediately

### CustomCheckBox

Located at `scripts/ui/CustomCheckBox.cs`

A custom checkbox implementation designed for touch interfaces:
- Large 48x48 pixel touch target
- Visual feedback for hover and pressed states
- Handles both mouse and touch input properly
- Uses `_Input` for global input handling to ensure touches are captured

## Recent Fixes

### Input Handling
- Fixed checkbox input by using `_Input` instead of `_GuiInput`
- Added proper press/release tracking to prevent accidental toggles
- Set `FocusMode` and `MouseFilter` for proper event handling

### Initialization Timing
- Added initialization check to prevent settings from being accessed before SettingsManager is ready
- Apply button now only enables for display settings changes (audio/debug apply immediately)

### Apply Button Logic
- Apply button correctly tracks only display setting changes
- Audio and debug settings no longer incorrectly enable the apply button

## Usage

The settings menu can be accessed from the main menu. Changes to display settings require clicking "Apply", while audio and debug settings take effect immediately.

## Testing

To verify the settings system is working:
1. Open the settings menu
2. Test each checkbox by clicking/touching - they should toggle properly
3. Change display settings - Apply button should enable
4. Change audio settings - they should apply immediately without enabling Apply
5. Click Apply - display settings should update
6. Click Back - should return to main menu

## Known Issues

None at this time. The recent fixes addressed:
- Checkbox click responsiveness
- Apply button enable/disable logic
- Initialization timing issues 