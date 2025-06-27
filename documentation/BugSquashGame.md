# Bug Squash Game Documentation

## Overview
The Bug Squash game is an arcade-style game where players must eradicate invasive species before native species go extinct. Players click on invasive species to eliminate them while avoiding accidentally clicking on native species.

## Game Structure

### Core Components

1. **BugSquashGame.cs** - Main game manager (extends Node2D)
   - Handles stage loading and progression
   - Manages entity spawning and game state
   - Controls UI through CanvasLayer for proper input handling
   - Separates UI elements from game entities to prevent input blocking

2. **BugSquashEntity.cs** - Entity behavior system
   - Handles AI behavior for different entity types
   - Manages click interactions and health
   - Controls visual feedback (stunning, blinking)

3. **BugSquashData.cs** - Data structures
   - Defines stage and species configuration classes
   - Maps to bug-squash.json structure

## Game Mechanics

### Entity Types

1. **Predator (Invasive Species)**
   - Requires 3 clicks to eliminate
   - When clicked: turns gray, blinks, and is stunned for 3 seconds
   - Hunts prey entities
   - Reproduces with other predators after 10 seconds

2. **Prey (Native Species)**
   - Dies immediately when clicked (player must avoid)
   - Feeds on food sources to reproduce (doubles)
   - Flees from predators when nearby
   - Cannot move while feeding (2 seconds)

3. **Food**
   - Static entities that don't move
   - Consumed by prey for reproduction
   - Respawns after 3 seconds in random positions

### Visual Design

Each entity is represented by:
- A circular colored ring (color from config)
- The species image centered inside, clipped to a circle
- Collision radius of 80 pixels (doubled for 4K display)
- Visual elements scaled to fit within the radius
- Quick movement around the screen

### Stage Progression

1. **Instruction Screen**
   - Shows before each stage
   - Displays interaction description
   - Shows entity icons with names and behaviors
   - "Start Level" button to begin

2. **Game Screen**
   - Background image from config
   - Score display: "Invasive Species: X | Native Species: Y"
   - Home button in bottom-right

3. **Win/Loss Conditions**
   - **Victory**: All invasive species eliminated
   - **Game Over**: All native species extinct
   - Shows appropriate message and options

## Configuration (bug-squash.json)

```json
{
  "id": "stage-1",
  "background_image": "path/to/background.png",
  "interaction_description": "Description of ecological interaction",
  "species": [
    {
      "id": "species-id",
      "name": "Species Name",
      "image": "path/to/sprite.png",
      "color": "#hexcolor",
      "behavior": "Predator|Prey|Food",
      "speed": 500,
      "description": "Species description",
      "starting_number": 5
    }
  ]
}
```

### Configuration Fields

- **id**: Unique stage identifier
- **background_image**: Path to stage background
- **interaction_description**: Educational text shown on instruction screen
- **species**: Array of entities in the stage
  - **behavior**: Determines AI and interaction type
  - **color**: Hex color for the entity ring
  - **speed**: Movement speed (ignored for Food)
  - **starting_number**: Initial spawn count (max for Food)

## AI Behaviors

### Predator AI
1. Default: Hunt nearest prey
2. After 10 seconds: Seek another predator
3. On contact with prey: Prey dies
4. On contact with predator: Both reproduce

### Prey AI
1. If predator within 400 pixels: Flee
2. Otherwise: Seek nearest food
3. On reaching food: Feed for 2 seconds
4. After feeding: Spawn duplicate

### Food Spawning
- Maintains count specified in starting_number
- Respawns 3 seconds after consumption
- Random position within screen bounds

## Technical Details

### Entity Collision
- Uses Area2D with CircleShape2D
- Radius: 80 pixels
- Detection via position distance checks

### Visual Effects
- **Stun Effect**: Gray tint (0.5, 0.5, 0.5, 0.7) + blink animation
- **Blink Animation**: Alpha oscillates between 1.0 and 0.3

### Performance Considerations
- Entities update behavior every 0.5 seconds
- Position bounds checking keeps entities on screen
- Entity limit recommendations: ~150 max

## UI Sizing for 4K Display

All UI elements have been sized appropriately for a 4K (3840x2160) display:
- Entity radius: 80 pixels
- Main font sizes: 48-128px
- Button sizes: 300x160px to 600x160px
- Instruction screen: 2400x1600px container
- Species icons: 240x240px

## Technical Architecture Changes

### Input Handling Fix
The game was refactored to properly handle Area2D input:
- Changed `BugSquashGame` from extending `Control` to `Node2D`
- Created a `CanvasLayer` to hold all UI elements
- Entities are now direct children of the game scene, not nested under Controls
- This prevents Control nodes from blocking input to Area2D entities

## Known Issues

All major issues have been resolved. Click detection should work properly.

## Future Enhancements

1. **Additional Mechanics**
   - Different click requirements per species
   - Power-ups or special abilities
   - Time limits or score targets

2. **Visual Improvements**
   - Particle effects on entity death
   - Trail effects for movement
   - More sophisticated animations

3. **Audio**
   - Click feedback sounds
   - Background music per stage
   - Victory/defeat sounds

4. **Educational Features**
   - Post-stage fact screens
   - Links to species gallery
   - Score tracking and achievements 