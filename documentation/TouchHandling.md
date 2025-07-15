# Touch Handling System

This document describes the touch handling implementation for the Invasive Species Australia educational game, designed for a 10-point multi-touch tabletop display.

## Overview

The game supports multi-touch interactions across all modules, with special attention to simultaneous touches for enhanced gameplay experience.

## Multi-Touch Debugger

The `MultiTouchDebugger` (located at `scripts/systems/MultiTouchDebugger.cs`) is a debugging tool that visualizes all active touch points on screen.

### Features
- Shows all active touch points with colored circles
- Displays touch index and coordinates for each point
- Can be toggled on/off via Settings menu
- Renders on top of all game content (ZIndex = 1000)
- Does not block or consume input events

### Usage
The debugger is automatically added to scenes and controlled via `SettingsManager.ShowTouchDebugger`.

## Memory Match Game Touch Handling

The Memory Match game supports simultaneous multi-touch, allowing players to tap two cards at once for faster gameplay.

### Implementation Details

#### MemoryCard.cs
- Uses direct `GuiInput` event handling instead of Button controls
- Processes both `InputEventScreenTouch` and `InputEventMouseButton` events
- Does not consume events, allowing multiple cards to receive touches simultaneously
- Only responds to touches when the card is face-down and not already matched

```csharp
private void OnGuiInput(InputEvent @event)
{
    if (@event is InputEventScreenTouch touchEvent)
    {
        if (touchEvent.Pressed && !IsMatched && !IsFaceUp)
        {
            EmitSignal(SignalName.CardClicked, this);
            // Event is not consumed to allow multi-touch
        }
    }
}
```

#### MemoryMatchGame.cs
- Tracks selected cards using a List instead of individual references
- Automatically triggers match checking when two cards are selected
- Handles rapid simultaneous selection properly

### Touch Event Flow
1. Player touches one or more cards simultaneously
2. Each card receives its own touch event
3. Cards are added to the selected cards list
4. When two cards are selected, match checking occurs automatically
5. No artificial delays or sequential requirements

## Bug Squash Game Touch Handling

The Bug Squash game uses Area2D nodes for entity interaction, supporting unlimited simultaneous touches for squashing multiple invasive species at once.

### Implementation Details

#### BugSquashEntity.cs
- Uses Area2D's `InputPickable = true` for touch detection
- Handles input via `InputEvent` signal
- ProcessMode set to `Always` to ensure all input events are received
- Does not consume events, allowing multiple entities to be touched simultaneously

```csharp
private void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx)
{
    if (@event is InputEventScreenTouch touchEvent && touchEvent.Pressed)
    {
        HandleClick();
        // Don't consume the event - allow other entities to receive the same touch
        return;
    }
}
```

### Features
- Players can tap multiple entities at once with different fingers
- Each entity tracks its own health independently
- Native species (prey/food) die on single tap
- Invasive species (predators) require multiple hits
- Visual feedback includes stun effects and paint splatter
- Shockwave effects for non-fatal hits on invasive species

### Entity Behavior
- **Predators (Invasive)**: Require 3 hits, show stun effect between hits
- **Prey (Native)**: Die immediately on touch
- **Food (Native)**: Die immediately on touch

### Touch Event Flow
1. Player touches multiple entities simultaneously
2. Each Area2D receives its own input event
3. Entities process touches independently
4. Visual and audio feedback occurs for each touched entity
5. No limit on simultaneous touches

## Bonus Objects Touch Handling

Bonus objects in Memory Match use similar touch handling:
- Rendered on a separate CanvasLayer (layer 10) above game content
- Handle both touch and mouse events
- Create visual feedback on collection (explosion effect)
- Do not interfere with card selection

## Best Practices

1. **Use GuiInput for UI Elements**: For UI controls that need touch support, use `_GuiInput()` instead of button signals
2. **Don't Consume Events Unnecessarily**: Only call `AcceptEvent()` when you need exclusive handling
3. **Support Both Touch and Mouse**: Always handle both `InputEventScreenTouch` and `InputEventMouseButton`
4. **Layer Management**: Use CanvasLayers to control touch priority for overlapping elements
5. **Visual Feedback**: Provide immediate visual feedback for all touch interactions
6. **Area2D Configuration**: For Area2D nodes, ensure `InputPickable = true` and don't consume events

## Performance Considerations

- Touch events are processed every frame when active
- Minimize complex calculations in touch handlers
- Use object pooling for frequently created touch effects
- The debugger has minimal performance impact when hidden
- Area2D input detection is efficient for handling many simultaneous touches 