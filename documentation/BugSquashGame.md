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

The bug squash game includes several visual effects to enhance player feedback:

1. **Shockwave Effect** (`ShockwaveEffect.cs`)
   - Triggered when invasive species (predators) are hit but not killed
   - Creates a circular ripple effect emanating from the hit entity
   - Uses a custom shader (`shockwave_ripple.gdshader`) for the visual effect
   - Duration: 0.5 seconds with fade-out

2. **Paint Splatter Effect** (`PaintSplatterEffect.cs`)
   - *(Currently disabled but available for future use)*
   - Creates a paint splatter when entities die
   - Uses the entity's color for the splatter
   - Fades out over 2 seconds

3. **Screen Shake Effect** 
   - Triggered on every entity click (native or invasive)
   - Duration: 0.3 seconds
   - Intensity: 30 pixel displacement
   - Implemented using Camera2D offset manipulation
   - Adds visceral feedback to player interactions

4. **Pop Text Effect** (`PopTextEffect.cs`)
   - Spawns animated text when any entity is clicked
   - Text varies based on entity behavior:
     - **Predators (invasive species)**: Action words like "SQUASH!", "BAM!", "POW!", "ZAP!", "WHAM!", "BOOM!", "SNAP!", "POP!", "CRASH!", "SMASH!"
     - **Prey/Food (native species)**: Mistake feedback like "NOPE!", "OH NO!", "OOPS!", "WAIT!", "NO NO!", "STOP!", "WRONG!", "MISTAKE!", "SORRY!", "ACK!"
   - Color schemes:
     - **Predators**: Bright colors (red, orange, yellow, green, blue, magenta, cyan)
     - **Native species**: Darker warning colors (dark red, purple, brown)
   - Animation includes:
     - Upward movement with angle variance (±45° for predators, ±90° for mistakes)
     - Scale animation (grow then shrink)
     - Fade out after 0.5 seconds
     - Total lifetime: 1.5 seconds
     - Mistakes have larger font size (84 vs 72) and more dramatic rotation
   - Text has drop shadow for better visibility

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

## Audio System

### Sound Effects
The game includes an audio system that plays sound effects when entities are interacted with:

- **Boom Sounds**: A collection of 5 boom sound effects (boom-1.wav through boom-5.wav) located in `assets/sounds/bug-squash/boom/`
- **Random Selection**: The system randomly selects a sound to play, ensuring the same sound doesn't play twice in a row
- **Spatial Audio**: Uses AudioStreamPlayer2D for positional audio effects
- **Pitch Variation**: Slightly randomizes pitch (0.9 to 1.1) for added variety
- **Concurrent Playback**: Each sound plays to completion without being cancelled by new sounds

### Sound Triggers
Sounds play in the following scenarios:
- When any entity is tapped/clicked
- When invasive species take damage (with shockwave effect)
- When native species are eliminated

### Implementation Details
- `LoadSounds()`: Loads all boom sounds from the assets folder at startup
- `PlayRandomBoomSound(Vector2 position)`: Creates a new AudioStreamPlayer2D for each sound, allowing multiple sounds to play simultaneously
- Prevents repetition by tracking the last played sound index
- Audio players are automatically cleaned up after sounds finish playing 