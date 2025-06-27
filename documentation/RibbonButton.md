# RibbonButton Documentation

## Overview

The RibbonButton is a custom UI control that creates stylized ribbon-shaped buttons with gradient effects, blur backgrounds, and smooth animations. It's designed to be resolution-aware and scales properly with different screen sizes.

## Features

- **Gradient Ribbon Design**: Buttons appear as ribbons with gradient fade effects
- **Directional Support**: Can be configured for left-side or right-side ribbons
- **Backdrop Blur**: Uses a custom shader for backdrop blur effects
- **Hover Animations**: Text slides and scales on hover
- **Resolution Scaling**: Automatically adapts to different screen sizes
- **Edge Line Effects**: Decorative edge lines with gradient fading

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| Text | string | "Button" | The text displayed on the ribbon |
| IsLeftSide | bool | true | Whether this is a left-side ribbon (true) or right-side ribbon (false) |
| BaseColor | Color | (0.9, 0.95, 0.92, 0.5) | The base color of the ribbon |
| HoverColor | Color | (0.95, 0.98, 0.96, 0.85) | The color when hovered |
| PressedColor | Color | (0.85, 0.9, 0.88, 0.95) | The color when pressed |
| BlurAmount | float | 3.0 | The amount of backdrop blur to apply |

## Resolution Scaling

The RibbonButton automatically scales all its visual elements based on the current viewport size compared to the design resolution (3840x2160). This includes:

- **Font Size**: Scales from base size of 85px
- **Minimum Size**: Scales from base size of 600x180
- **Edge Lines**: Scales from base thickness of 4px
- **Text Animation**: Scales from base offset of 20px
- **Shadows**: Scale from base offset of 3px
- **Label Padding**: Scales from base padding of 60px

### How Scaling Works

1. The button calculates a scale factor based on: `min(viewport.width / 3840, viewport.height / 2160)`
2. All visual properties are multiplied by this scale factor
3. The button updates automatically when the viewport size changes

## Usage

### In Scene Files

```
[node name="MyRibbonButton" type="Control" parent="."]
script = ExtResource("res://scripts/ui/RibbonButton.cs")
Text = "My Button"
IsLeftSide = true
BaseColor = Color(0.9, 0.95, 0.92, 0.5)
```

### From Code

```csharp
var ribbonButton = new RibbonButton();
ribbonButton.Text = "Click Me";
ribbonButton.IsLeftSide = false;
ribbonButton.BaseColor = new Color(0.8f, 0.9f, 0.85f, 0.6f);
AddChild(ribbonButton);

// Connect to the pressed signal
ribbonButton.Pressed += OnRibbonButtonPressed;
```

## Visual Design

### Left-Side Ribbons
- Solid color on the left (55% of width)
- Gradient fade to transparent on the right (45% of width)
- Text aligned to the left within the solid area
- Animation slides text to the right on hover

### Right-Side Ribbons
- Gradient fade from transparent on the left (45% of width)
- Solid color on the right (55% of width)
- Text aligned to the right within the solid area
- Animation slides text to the left on hover

## Dependencies

- **Backdrop Blur Shader**: `res://shaders/backdrop_blur.gdshader`
- **Nunito Font**: `res://assets/fonts/Nunito/Nunito-Bold.ttf`

## Best Practices

1. **Positioning**: Use anchor-based positioning in scene files to ensure proper layout at different resolutions
2. **Color Contrast**: Ensure sufficient contrast between the ribbon color and background
3. **Text Length**: Keep button text concise to fit within the solid area of the ribbon
4. **Parent Container**: Works best when placed in a Control node that manages overall layout

## Troubleshooting

### Button appears too small/large
- Check that the parent MainMenu is using BaseUIControl for proper scaling
- Verify the viewport size matches expected dimensions

### Text is cut off
- Reduce text length or adjust the base label size values
- Consider using a smaller base font size

### Blur effect not working
- Ensure the backdrop_blur.gdshader file exists and is properly loaded
- Check that the Material property is set correctly

## Example Setup

A typical main menu setup with ribbon buttons:

```
MainMenu (BaseUIControl)
└── RibbonContainer (Control with full rect anchors)
    ├── StoriesButton (RibbonButton, left side)
    ├── SpeciesGuideButton (RibbonButton, left side)
    ├── BugSquashButton (RibbonButton, right side)
    └── MemoryMatchButton (RibbonButton, right side)
``` 