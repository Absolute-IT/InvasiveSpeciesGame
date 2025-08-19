# ConfigLoader System

## Overview

The ConfigLoader system provides a centralized way to load and manage configuration files for the Invasive Species Australia game. It supports loading JSON configuration files from both internal (`res://config/`) and user-accessible (`user://config/`) directories, with user configurations overriding internal ones.

## Architecture

### Key Components

1. **ConfigLoader** (`scripts/systems/ConfigLoader.cs`)
   - Singleton pattern for global access
   - Loads configuration files from internal and user directories
   - Merges/overrides data based on unique IDs
   - Provides access methods for configuration data
   - Integrates with Story slide generation system

2. **Species** (`scripts/systems/Species.cs`)
   - Data model for species information
   - Contains all fields from the species.json configuration
   - Includes clone functionality for data manipulation
   - Supports enabled/disabled states for content management

3. **SpeciesReference** (`scripts/systems/Species.cs`)
   - Sub-model for reference information attached to species

4. **StoryInfo** (`scripts/systems/StoryModels.cs`)
   - Data model for story configurations
   - Links to PowerPoint files and manages generated slides
   - See [PowerPointStoryConverter.md](PowerPointStoryConverter.md) for conversion details

5. **BugSquashStage** (`scripts/bug-squash/BugSquashData.cs`)
   - Data model for Bug Squash game configurations
   - See [BugSquashGame.md](BugSquashGame.md) for gameplay details

## Usage

### Initialization

The ConfigLoader is automatically initialized when the MainMenu loads:

```csharp
// In MainMenu.cs OnReady()
var configLoader = ConfigLoader.Instance;
configLoader.LoadAllConfigs();
```

### Accessing Configuration Data

```csharp
// === Species Data ===
// Get all species (including disabled ones)
var allSpecies = ConfigLoader.Instance.GetSpeciesData();

// Get only enabled species
var enabledSpecies = ConfigLoader.Instance.GetAllEnabledSpecies();

// Get a specific species by ID
var caneToad = ConfigLoader.Instance.GetSpecies("cane-toad");

// Get species by type (all or enabled only)
var animals = ConfigLoader.Instance.GetSpeciesByType("animals");
var enabledAnimals = ConfigLoader.Instance.GetEnabledSpeciesByType("animals");
var plants = ConfigLoader.Instance.GetSpeciesByType("plants");
var enabledPlants = ConfigLoader.Instance.GetEnabledSpeciesByType("plants");

// === Story Data ===
// Get all stories
var stories = ConfigLoader.Instance.GetStories();

// Get a specific story by ID
var story = ConfigLoader.Instance.GetStoryById("the-great-escape");

// === Bug Squash Data ===
// Load bug squash stages (static method)
var stages = ConfigLoader.LoadBugSquashStages();

// === Menu Backgrounds ===
// Get menu background paths
var backgrounds = ConfigLoader.Instance.GetMenuBackgrounds();
```

### Species Data Structure

```csharp
public class Species
{
    public string Id { get; set; }                       // Unique identifier
    public bool Enabled { get; set; } = true;            // Whether species is active
    public string Name { get; set; }                     // Common name
    public string ScientificName { get; set; }           // Scientific name
    public string Type { get; set; }                     // "animals" or "plants"
    public string History { get; set; }                  // Historical background
    public string Habitat { get; set; }                  // Habitat description
    public string Diet { get; set; }                     // Diet (empty for plants)
    public List<string> Identification { get; set; }     // ID features
    public List<string> IdentificationImages { get; set; } // Paths to ID images
    public string Image { get; set; }                    // Path to main image
    public float ImageScale { get; set; } = 1.0f;        // Scale factor for display
    public string EnvironmentImage { get; set; }         // Background environment
    public string CardImage { get; set; }                // Memory game card image
    public string AmbienceSound { get; set; }            // Background audio
    public string Wikipedia { get; set; }                // Wikipedia URL
    public string AustralianMuseum { get; set; }         // Museum URL
    public List<SpeciesReference> References { get; set; } // Citations
}
```

## Configuration Files

The ConfigLoader manages four types of configuration:

1. **Species Configuration** (`species.json`) - Species database
2. **Story Configuration** (`stories.json`) - Story/presentation data  
3. **Bug Squash Configuration** (`bug-squash.json`) - Game level data
4. **Menu Backgrounds** (`menu-backgrounds.json`) - UI background images

### File Locations

Most configurations support both internal and user overrides:

- **Internal**: `res://config/<filename>.json` (ships with game)
- **User**: `user://config/<filename>.json` (for user modifications)

### Loading Order

1. Internal configuration is loaded first
2. User configuration is loaded second (if exists)
3. Entries with matching IDs in user config override internal ones
4. New entries in user config are added to the collection

## Species Configuration

### Example species.json Entry

```json
{
    "id": "cane-toad",
    "enabled": true,
    "name": "Cane Toad",
    "scientific_name": "Rhinella marina",
    "type": "animals",
    "history": "Introduced in 1935 from Hawaii...",
    "habitat": "Found across Queensland, Northern Territory...",
    "diet": "Opportunistic feeder consuming insects...",
    "identification": [
        "Large, warty skin ranging from grey to yellow-brown",
        "Distinctive large parotoid glands behind eyes"
    ],
    "identification_images": [
        "assets/identification/cane-toad-1.jpg",
        "assets/identification/cane-toad-2.jpg",
        "assets/identification/cane-toad-3.jpg"
    ],
    "image": "assets/art/species/animals/cane-toad.png",
    "image_scale": 1.2,
    "environment_image": "assets/art/environments/wetlands.png",
    "card_image": "assets/art/match-game/cards/cane-toad-card.png",
    "ambience_sound": "assets/sounds/ambience-normalised/wetland.wav",
    "wikipedia": "https://en.wikipedia.org/wiki/Cane_toad",
    "australian_museum": "https://australian.museum/learn/animals/amphibians/cane-toad/",
    "references": [
        {
            "field": "history",
            "reference": "Australian Museum, 2023"
        }
    ]
}
```

### Species Fields

- **id**: Unique identifier for the species (required)
- **enabled**: Whether the species appears in the game (default: true)
- **name**: Common name displayed to users
- **scientific_name**: Scientific/Latin name
- **type**: Either "animals" or "plants"
- **history**: Historical information about the species introduction
- **habitat**: Description of where the species is found
- **diet**: What the species eats (mainly for animals)
- **identification**: Array of identification features as strings
- **identification_images**: Array of up to 3 image paths for visual identification
- **image**: Main species image path
- **image_scale**: Display scale factor (default: 1.0)
- **environment_image**: Background image for the detail view
- **card_image**: Image used in the memory match game
- **ambience_sound**: Audio file for background ambience
- **wikipedia**: Wikipedia URL for more information
- **australian_museum**: Australian Museum URL if available
- **references**: Array of citation objects with "field" and "reference" properties

## Story Configuration

Stories are educational presentations converted from PowerPoint files. The ConfigLoader loads story metadata from `stories.json` and integrates with the Story slide generation system.

### File Location

- **Internal**: `res://config/stories.json` (only internal, no user override)

### Structure

```json
[
    {
        "id": "the-great-escape",
        "title": "The Great Escape",
        "description": "The real reason rabbits don't make great pets.",
        "file": "assets/stories/the-great-escape.pptx",
        "visible": true,
        "thumbnail": "res://optional/fallback.png"
    }
]
```

### Story Fields

- **id**: Unique identifier (auto-generated from title if not provided)
- **title**: Display name for the story
- **description**: Brief description shown to users
- **file**: Path to PowerPoint presentation file
- **visible**: Whether story appears in the selection screen (default: true)
- **thumbnail**: Optional fallback thumbnail (generated thumbnails take priority)

For detailed information about slide generation, see [PowerPointStoryConverter.md](PowerPointStoryConverter.md).

## Bug Squash Configuration  

Bug Squash stages define ecological scenarios with interacting species. Each stage represents a different biome with its own species and behaviors.

### File Location

- **Internal**: `res://config/bug-squash.json` (only internal, no user override)

### Structure

See [BugSquashGame.md](BugSquashGame.md) for complete configuration documentation. The basic structure includes:

```json
[
    {
        "id": "stage-1",
        "background_image": "assets/art/bug-squash/backgrounds/woodland.png",
        "ambience_sound": "assets/sounds/ambience-normalised/woodland.wav",
        "interaction_description": "Educational text explaining the ecological interaction...",
        "species": [
            {
                "id": "feral-cat",
                "name": "Feral Cat",
                "behavior": "Predator",
                "goals": [...],
                "health": 3,
                "starting_number": 7
            }
        ]
    }
]
```

## Menu Backgrounds Configuration

Menu background images rotate in the main menu to provide visual variety.

### File Locations

- **Internal**: `res://config/menu-backgrounds.json`
- **User**: `user://config/menu-backgrounds.json`

### Structure

Simple array of texture paths:

```json
[
    "assets/art/environments/arid.png",
    "assets/art/environments/desert.png", 
    "assets/art/environments/grasslands.png",
    "assets/art/environments/jungle.png",
    "assets/art/environments/mangroves.png",
    "assets/art/environments/wetlands.png"
]
```

### Usage

The MainMenu scene automatically rotates through backgrounds every 30 seconds with fade transitions.

## User Configuration

### Enabling User Modifications

To allow users to modify species data:

```csharp
// Copy default config to user directory
ConfigLoader.Instance.CopyDefaultConfigToUser("species.json");
```

This creates a copy of the internal configuration in the user directory where it can be edited.

### User Directory Location

- **Windows**: `%APPDATA%/Godot/app_userdata/InvasiveSpeciesAustralia/config/`
- **macOS**: `~/Library/Application Support/Godot/app_userdata/InvasiveSpeciesAustralia/config/`
- **Linux**: `~/.local/share/godot/app_userdata/InvasiveSpeciesAustralia/config/`

## System Integration

### Initialization Flow

The ConfigLoader automatically initializes when the MainMenu loads:

1. **MainMenu.cs** calls `ConfigLoader.Instance.LoadAllConfigs()`
2. Species configuration loads (internal + user overrides)
3. Menu backgrounds configuration loads
4. Story configuration loads from `stories.json`
5. **StorySlideGenerator** starts PowerPoint-to-PNG conversion
6. Bug Squash configuration loads on-demand when needed

### Slide Generation Integration

The ConfigLoader integrates with the Story slide generation system:

- Parses story metadata from `stories.json`
- Starts **StorySlideGenerator** for PowerPoint conversion
- Generated slides are saved to `user://stories/<story-id>/`
- See [PowerPointStoryConverter.md](PowerPointStoryConverter.md) for details

## Extending the System

### Adding New Configuration Types

To add support for a new configuration file:

1. **Create Data Model**: Define classes for the configuration structure
2. **Add Storage**: Add a storage dictionary/list in ConfigLoader
3. **Implement Loading**: Create a `LoadXConfig()` method
4. **Add Access Methods**: Provide public methods to access the data
5. **Update LoadAllConfigs()**: Call the new load method

Example for a hypothetical achievements configuration:

```csharp
// In ConfigLoader.cs
private Dictionary<string, Achievement> _achievements = new Dictionary<string, Achievement>();

private void LoadAchievementsConfig()
{
    const string fileName = "achievements.json";
    // ... similar loading pattern as LoadSpeciesConfig
}

public List<Achievement> GetAchievements()
{
    return _achievements.Values.ToList();
}

// In LoadAllConfigs()
LoadAchievementsConfig();
```

## Error Handling

The ConfigLoader includes comprehensive error handling:

- File existence checks before attempting to load
- JSON parsing wrapped in try-catch blocks
- Detailed error logging with GD.PrintErr()
- Graceful handling of missing or malformed data

## Performance Considerations

- Configuration is loaded once at startup
- Data is stored in memory for fast access
- Dictionary lookups provide O(1) access by ID
- Clone methods create deep copies to prevent data mutation

## Technical Implementation

### JSON Serialization

The ConfigLoader uses System.Text.Json with custom options:

- **PropertyNamingPolicy**: `JsonNamingPolicy.SnakeCaseLower` (converts C# PascalCase to JSON snake_case)
- **JsonStringEnumConverter**: Handles enum serialization/deserialization
- **WriteIndented**: Produces readable JSON output

### Loading Strategy

Different configurations use different loading approaches:

- **Species & Menu Backgrounds**: User configs can override/extend internal configs
- **Stories**: Internal only, no user override (PPTX files managed separately)
- **Bug Squash**: Static loading method, called on-demand by the game

### Memory Management

- Configuration data is loaded once at startup
- Dictionary lookups provide O(1) access by ID
- Clone methods create deep copies to prevent data mutation
- Large configurations (Bug Squash) load only when needed

## Related Documentation

- [BugSquashGame.md](BugSquashGame.md) - Complete Bug Squash configuration reference
- [PowerPointStoryConverter.md](PowerPointStoryConverter.md) - Story slide generation system
- [Gallery.md](Gallery.md) - How species data is used in the gallery
- [MemoryMatchGame.md](MemoryMatchGame.md) - How species data is used in memory match

## Future Enhancements

### Planned Improvements

1. **Hot Reloading**: Watch for file changes and reload automatically during development
2. **Schema Validation**: JSON schema validation for configuration files to catch errors early
3. **Configuration Export**: Export merged configurations for debugging and validation
4. **Localization**: Support for localized species names and descriptions in multiple languages
5. **Version Management**: Configuration versioning to handle format changes gracefully
6. **Performance Profiling**: Metrics for configuration loading times and memory usage 