# ParallaxTiltEffect

## Overview
The `ParallaxTiltEffect` is a UI component that adds an interactive parallax tilt effect with 3D depth illusion to UI elements based on mouse position. When the user hovers over an element with this effect, it will tilt, rotate, and scale with perspective transformations to create a convincing 3D card-like effect.

## Features
- **Dynamic Tilt**: Elements tilt based on mouse position for a 3D-like effect
- **Perspective Scaling**: Non-uniform scaling creates depth illusion
- **Smooth Animation**: Smooth interpolation between states for fluid motion
- **Touch Support**: Works with both mouse and touch input
- **Customizable Parameters**: All effect properties can be adjusted via exports

## Usage

### Adding to a Scene
1. Add the `ParallaxTiltEffect.cs` script to any Control node
2. The effect will automatically apply to the node and all its children
3. Configure the effect parameters in the Inspector

### Parameters
- **TiltIntensity** (float, default: 10.0): How much the element moves when tilted
- **SmoothingSpeed** (float, default: 8.0): How quickly the element responds to mouse movement
- **EnableScale** (bool, default: true): Whether to apply scaling effect
- **ScaleAmount** (float, default: 0.02): How much to scale when hovered
- **PerspectiveStrength** (float, default: 0.3): Strength of perspective distortion (0-1)
- **DepthScale** (float, default: 0.05): Additional scaling based on vertical position
- **DebugMode** (bool, default: false): Shows visual debugging information

### Example Implementation
```gdscript
[node name="TitleContainer" type="Control"]
script = ExtResource("ParallaxTiltEffect.cs")
TiltIntensity = 12.0
SmoothingSpeed = 10.0
EnableScale = true
ScaleAmount = 0.02
PerspectiveStrength = 0.25
DepthScale = 0.04
```

## Implementation Details

### 3D Depth Illusion
The effect creates depth through several techniques:
1. **Perspective Scaling**: X and Y scales change differently based on vertical mouse position
2. **Depth-based Scaling**: Elements appear larger when mouse is below center (closer)
3. **Enhanced Rotation**: Increased rotation creates stronger tilt effect
4. **Movement**: Enhanced vertical movement (70% of horizontal) for better depth perception

### Main Menu Usage
In the main menu, the effect is applied to three elements with different settings:
1. **Logo Container**: 
   - Highest tilt intensity (20.0) with strong perspective (0.4)
   - Creates dramatic 3D card effect
2. **Title Container**: 
   - Medium tilt intensity (12.0) with moderate perspective (0.25)
   - Balanced between readability and effect
3. **Subtitle Container**: 
   - Lower tilt intensity (8.0) with subtle perspective (0.2)
   - Minimal effect to maintain readability

### Technical Notes
- The effect uses non-uniform scaling to simulate perspective
- Mouse position is normalized to a -1 to 1 range based on the element's bounds
- Vertical movement is enhanced (70% of horizontal) for better depth perception
- Works with both mouse and touch input for kiosk compatibility

## Best Practices
- Use higher tilt intensity and perspective for decorative elements
- Use lower intensity for text to maintain readability
- Adjust smoothing speed based on element size
- Test with both mouse and touch input for kiosk applications 