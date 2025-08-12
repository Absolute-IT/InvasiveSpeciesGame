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
   - Provides `GetSpeciesById` method for entity spawn mechanics

2. **BugSquashEntity.cs** - Entity behavior system
   - Handles AI behavior for different entity types
   - Manages click interactions and health system
   - Controls visual feedback (stunning, blinking)
   - Implements goal-based AI system
   - Handles entity size scaling

3. **BugSquashData.cs** - Data structures
   - Defines stage and species configuration classes
   - Maps to bug-squash.json structure
   - Supports goals array for complex behaviors

## Game Mechanics

### Entity Behaviors

1. **Predator**
   - Can move towards its targets, but never flees
   - Default behavior when no goals are active: wander

2. **Prey**
   - Can move towards its targets
   - Will flee from nearby predators (within 400 pixels)
   - Default behavior when no goals are active: wander

3. **Food**
   - Static entities that don't move
   - When eaten by another entity, creates a new entity based on `creates_on_eaten` property
   - If `creates_on_eaten` is not specified, the eating entity reproduces itself
   - Can respawn based on `spawn_rate` property

4. **Nest**
   - Static entities that don't move
   - Cannot be eaten by other entities
   - Can spawn other entities based on goals

5. **Weed**
   - Static entities that don't move
   - Spawns more entities nearby at rate set by `spawn_rate`

### Goals System

Goals define specific behaviors for entities. Each entity can have multiple goals that are prioritized:

1. **Goal Types**:
   - **Eat**: Move towards target to consume it and reproduce
   - **Breed**: Move towards target to reproduce after cooldown
   - **Spawn**: Produce new entities at regular intervals
   - **Kill**: Move towards target and deal damage

2. **Goal Prioritization**:
   - Breed has highest priority (when cooldown has elapsed)
   - Kill and Eat are chosen based on nearest available target
   - Spawn runs independently on a timer

3. **Goal Configuration**:
   ```json
   {
     "type": "Kill|Eat|Breed|Spawn",
     "target": "entity-id",
     "value": 5.0  // Used for Breed cooldown and Spawn interval
   }
   ```

### Health & Damage System

1. **Health Points**
   - Entities have configurable health (default: 1)
   - Player clicks deal 1 damage
   - Kill goals deal 1 damage per contact

2. **Stun Mechanics**
   - When damaged but not killed, entity is stunned for 3 seconds
   - During stun: entity blinks, turns gray, and flees from damage source
   - Cannot take additional damage while stunned

3. **Death**
   - Entity dies when health reaches 0
   - Triggers paint splatter effect (if enabled)
   - Updates game score

### Size Scaling

- Entities can be scaled using the `size` property (percentage)
- Default size: 100 (100%)
- Affects both collision radius and visual appearance
- Examples: 70 = 70% size, 120 = 120% size

### Spawning Mechanics

1. **Food Spawning**
   - Controlled by game manager
   - Respawns at rate defined by `spawn_rate` when below `starting_number`

2. **Weed Spawning**
   - Spawns nearby entities at `spawn_rate` intervals
   - New weeds appear within 2.5x entity radius

3. **Goal-based Spawning**
   - Entities with Spawn goal create new entities
   - Target and interval defined in goal configuration

### Visual Design

Each entity is represented by:
- A circular colored ring (color from config)
- The species image centered inside, clipped to a circle
- Collision radius scales with size property
- Base radius: 80 pixels (scaled by size percentage)
- Visual elements scaled to fit within the radius

### Stage Progression

1. **Instruction Screen**
   - Shows before each stage
   - Displays interaction description
   - Shows entity icons with names and behaviors (scaled 2x)
   - "Start Level" button to begin

2. **Game Screen**
   - Background image from config
   - Score display: "Invasive Species: X | Native Species: Y"
   - Home button in bottom-right
   - Note: Nest and Weed entities are not counted in score

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
      "behavior": "Predator|Prey|Food|Nest|Weed",
      "goals": [
        {
          "type": "Kill|Eat|Breed|Spawn",
          "target": "target-species-id",
          "value": 5.0
        }
      ],
      "speed": 500,
      "size": 100,
      "description": "Species description",
      "starting_number": 5,
      "health": 3,
      "spawn_rate": 5.0,
      "creates_on_eaten": "entity-id"
    }
  ]
}
```

### Configuration Fields

- **id**: Unique stage identifier
- **background_image**: Path to stage background
- **interaction_description**: Educational text shown on instruction screen
- **species**: Array of entities in the stage
  - **id**: Unique identifier for the species
  - **behavior**: Base AI behavior type
  - **goals**: Array of goal objects defining specific behaviors
  - **color**: Hex color for the entity ring
  - **speed**: Movement speed (ignored for static entities)
  - **size**: Entity scale as percentage (default: 100)
  - **starting_number**: Initial spawn count
  - **health**: Hit points before death (default: 1)
  - **spawn_rate**: Seconds between spawns for Food/Weed (default: 0)
  - **creates_on_eaten**: Entity ID to spawn when this Food is consumed (Food behavior only)

## AI Behavior Details

### Goal Execution

1. **Eat Goal**
   - Seeks nearest target entity
   - On contact: target dies, creates entity based on target's `creates_on_eaten` property
   - If no `creates_on_eaten` is specified, the eating entity reproduces itself
   - Prey will flee if predator is within 400 pixels

2. **Kill Goal**
   - Seeks nearest target entity
   - On contact: deals 1 damage to target
   - Immediately seeks new target if available

3. **Breed Goal**
   - Cooldown period starts when entity spawns (value property in seconds)
   - Seeks nearest entity of target type that also has breed goal AND is ready to breed
   - Both entities must have their breed cooldown elapsed
   - Will not pursue entities that are already breeding or not ready
   - Both entities stop for 2 seconds then spawn offspring
   - Only the initiating entity spawns one offspring (not both)
   - Cooldown resets to 0 after breeding

4. **Spawn Goal**
   - Creates new entity every X seconds (value property)
   - Spawns entity type specified in target property
   - Runs independently of movement

### Movement Patterns

- **Seeking**: Smooth approach with arrival slowdown
- **Fleeing**: Direct movement away from threat
- **Wandering**: Random movement when no goals active
- **Static**: No movement (Food, Nest, Weed)

## Technical Details

### Entity Collision
- Uses Area2D with CircleShape2D
- Base radius: 80 pixels (scaled by size property)
- Detection via position distance checks

### Visual Effects

The bug squash game includes several visual effects to enhance player feedback:

1. **Shockwave Effect** (`ShockwaveEffect.cs`)
   - Triggered when entities are hit but not killed
   - Creates a circular ripple effect emanating from the hit entity
   - Uses a custom shader (`shockwave_ripple.gdshader`) for the visual effect
   - Duration: 0.5 seconds with fade-out

2. **Paint Splatter Effect** (`PaintSplatterEffect.cs`)
   - *(Currently disabled but available for future use)*
   - Creates a paint splatter when entities die
   - Uses the entity's color for the splatter
   - Fades out over 2 seconds

3. **Screen Shake Effect** 
   - Triggered on every entity click
   - Duration: 0.3 seconds
   - Intensity: 30 pixel displacement
   - Implemented using Camera2D offset manipulation
   - Adds visceral feedback to player interactions

4. **Pop Text Effect** (`PopTextEffect.cs`)
   - Spawns animated text when any entity is clicked
   - Text varies based on entity behavior:
     - **Invasive species**: Action words like "SQUASH!", "BAM!", "POW!", "ZAP!", etc.
     - **Native species**: Mistake feedback like "NOPE!", "OH NO!", "OOPS!", etc.
   - Color schemes match entity type
   - Animation includes upward movement, rotation, scale, and fade
   - Total lifetime: 1.5 seconds

### Performance Considerations
- Entities update behavior every 0.5 seconds
- Position bounds checking keeps entities on screen
- Entity limit recommendations: ~150 max
- Size scaling affects collision detection

## UI Sizing for 4K Display

All UI elements have been sized appropriately for a 4K (3840x2160) display:
- Base entity radius: 80 pixels (scaled by size property)
- Main font sizes: 48-128px
- Button sizes: 300x160px to 600x160px
- Instruction screen: 2400x1600px container
- Species icons: 400x400px (2x scaled)

## Technical Architecture

### Input Handling
The game uses proper scene structure for Area2D input:
- `BugSquashGame` extends `Node2D` (not Control)
- UI elements are on a separate `CanvasLayer`
- Entities are direct children of the game scene
- This prevents Control nodes from blocking Area2D input

### Entity Management
- Entities register themselves with the game manager
- Spawn requests go through the game manager
- Species lookup via `GetSpeciesById` method

## Audio System

### Sound Effects
The game includes an audio system that plays sound effects when entities are interacted with:

- **Boom Sounds**: Collection of 5 boom effects in `assets/sounds/bug-squash/boom/`
- **Random Selection**: Prevents same sound playing twice in a row
- **Spatial Audio**: Uses AudioStreamPlayer2D for positional effects
- **Pitch Variation**: 0.9 to 1.1 for variety
- **Concurrent Playback**: Multiple sounds can play simultaneously

### Sound Triggers
- Entity clicks/taps (all types)
- Damage events
- Entity elimination

## Future Enhancements

1. **Additional Mechanics**
   - Environmental hazards
   - Power-ups or special abilities
   - Time limits or score targets

2. **Visual Improvements**
   - Particle effects on entity death
   - Trail effects for movement
   - More sophisticated animations

3. **Educational Features**
   - Post-stage fact screens
   - Links to species gallery
   - Score tracking and achievements 