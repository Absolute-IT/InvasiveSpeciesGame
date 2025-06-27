# Main Menu System Documentation

## Overview
The Main Menu is the entry point for the Invasive Species Australia kiosk application. It provides navigation to all main sections of the application.

## Features
- Animated ribbon-style navigation buttons
- Background image with color overlay
- Title and subtitle display
- Settings access control
- **Interactive parallax tilt effect on logo and text elements**

## Layout
- **Background**: Full-screen environment image with darkening overlay
- **Logo**: Positioned in the bottom-left corner with parallax tilt effect
- **Title**: Large centered title "Invasive Species Australia" with parallax tilt
- **Subtitle**: "Learn • Identify • Protect" tagline with subtle parallax tilt
- **Navigation Buttons**: All four game buttons on the right side
- **Settings Button**: Top-right gear icon that toggles visibility of settings ribbon
- **Settings Ribbon**: Hidden by default on the left side, shown when gear icon is pressed

## Parallax Tilt Effect
The main menu includes an interactive parallax tilt effect with 3D depth illusion on key visual elements. The effect creates a convincing 3D card-like appearance that responds to mouse/touch position.

### Logo Container
- **Tilt Intensity**: 20.0 (highest for dramatic effect)
- **Perspective Strength**: 0.4 (strong perspective distortion)
- **Depth Scale**: 0.08 (significant depth-based scaling)
- **Effect**: Creates a prominent floating card effect

### Title Container
- **Tilt Intensity**: 12.0 (balanced for impact and readability)
- **Perspective Strength**: 0.25 (moderate perspective)
- **Depth Scale**: 0.04 (subtle depth scaling)
- **Effect**: Adds dimension while maintaining text clarity

### Subtitle Container
- **Tilt Intensity**: 8.0 (subtle enhancement)
- **Perspective Strength**: 0.2 (minimal perspective)
- **Depth Scale**: 0.03 (slight depth effect)
- **Effect**: Gentle movement that complements the title

The 3D depth illusion is achieved through:
- Non-uniform scaling that simulates perspective
- Enhanced rotation and vertical movement
- Depth-based scaling where elements appear larger when "closer" to viewer

## Button Configuration

### Right Side Buttons (Always Visible)
1. **Stories** (55% from top)
2. **Species Guide** (65% from top)
3. **Bug Squash!** (75% from top)
4. **Memory Match** (85% from top)

### Left Side Button (Hidden by Default)
1. **Settings** (75% from top) - Only visible when toggled by the gear icon

### Settings Access
The gear icon (⚙) in the top-right corner toggles the visibility of the Settings ribbon button. This prevents ordinary users from accidentally accessing settings while still keeping them accessible for administrators.

## Scene Paths
- Stories: `res://scenes/story/Stories.tscn`
- Species Guide: `res://scenes/gallery/Gallery.tscn`
- Bug Squash: `res://scenes/bug-squash/BugSquash.tscn`
- Memory Match: `res://scenes/memory-match/MemoryMatch.tscn`
- Settings: `res://scenes/Settings.tscn`

## Implementation Details
- **Script**: `MainMenu.cs`
- **Scene**: `MainMenu.tscn`
- **Theme**: Uses project default theme from `assets/MainTheme.tres`
- **Fonts**: Nunito Bold for title, Nunito Regular for subtitle

## Configuration Loading
The Main Menu initializes the ConfigLoader singleton on startup, ensuring all game configuration files are loaded before navigating to other scenes.

## Transitions
All scene transitions include a fade-out effect (0.3 seconds) for smooth navigation. 