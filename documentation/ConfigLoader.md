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

2. **Species** (`scripts/systems/Species.cs`)
   - Data model for species information
   - Contains all fields from the species.json configuration
   - Includes clone functionality for data manipulation

3. **SpeciesReference** (`scripts/systems/Species.cs`)
   - Sub-model for reference information attached to species

## Usage

### Initialization

The ConfigLoader is automatically initialized when the MainMenu loads:

```csharp
// In MainMenu.cs OnReady()
var configLoader = ConfigLoader.Instance;
configLoader.LoadAllConfigs();
```

### Accessing Species Data

```csharp
// Get all species
var allSpecies = ConfigLoader.Instance.GetSpeciesData();

// Get a specific species by ID
var caneToad = ConfigLoader.Instance.GetSpecies("cane-toad");

// Get all species of a specific type
var animals = ConfigLoader.Instance.GetSpeciesByType("animals");
var plants = ConfigLoader.Instance.GetSpeciesByType("plants");

// Get menu background paths
var backgrounds = ConfigLoader.Instance.GetMenuBackgrounds();
```

### Species Data Structure

```csharp
public class Species
{
    public string Id { get; set; }              // Unique identifier
    public string Name { get; set; }            // Common name
    public string ScientificName { get; set; }  // Scientific name
    public string Type { get; set; }            // "animals" or "plants"
    public string History { get; set; }         // Historical background
    public string Habitat { get; set; }         // Habitat description
    public string Diet { get; set; }            // Diet (empty for plants)
    public List<string> Identification { get; set; }  // ID features
    public string Image { get; set; }           // Path to image asset
    public string Wikipedia { get; set; }       // Wikipedia URL
    public string AustralianMuseum { get; set; } // Museum URL
    public List<SpeciesReference> References { get; set; } // Citations
}
```

## Configuration Files

### File Locations

- **Internal**: `res://config/species.json` (ships with game)
- **User**: `user://config/species.json` (for user modifications)

### Loading Order

1. Internal configuration is loaded first
2. User configuration is loaded second
3. Entries with matching IDs in user config override internal ones
4. New entries in user config are added to the collection

### Example species.json Entry

```json
{
    "id": "cane-toad",
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
    "image": "assets/art/species/animals/cane-toad.png",
    "wikipedia": "https://en.wikipedia.org/wiki/Cane_toad",
    "australian_museum": "https://australian.museum/learn/animals/amphibians/cane-toad/",
    "references": []
}
```

### Menu Backgrounds Configuration

The ConfigLoader also manages menu background images that rotate in the main menu.

#### File Locations

- **Internal**: `res://config/menu-backgrounds.json`
- **User**: `user://config/menu-backgrounds.json`

#### Structure

The menu-backgrounds.json file contains a simple array of texture paths:

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

#### Usage in Main Menu

The MainMenu scene automatically loads these backgrounds and rotates through them every 30 seconds with a smooth fade transition.

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

## Extending the System

### Adding New Configuration Types

1. Create a data model class (similar to Species)
2. Add a storage dictionary in ConfigLoader
3. Implement a `LoadXConfig()` method
4. Add parsing logic in a `ParseXFromJson()` method
5. Call the new load method from `LoadAllConfigs()`

Example for levels configuration:

```csharp
// In ConfigLoader.cs
private Dictionary<string, Level> _levelData = new Dictionary<string, Level>();

private void LoadLevelsConfig()
{
    const string fileName = "levels.json";
    // ... similar loading pattern as LoadSpeciesConfig
}
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

## Future Enhancements

1. **Hot Reloading**: Watch for file changes and reload automatically
2. **Validation**: Schema validation for configuration files
3. **Export**: Export merged configuration for debugging
4. **Localization**: Support for localized species names and descriptions

## Species Configuration

The `species.json` file contains an array of species objects with the following structure:

```json
{
    "id": "black-rat",
    "name": "Black Rat",
    "scientific_name": "Rattus rattus",
    "type": "animals",
    "history": "...",
    "habitat": "...",
    "diet": "...",
    "identification": ["Feature 1", "Feature 2"],
    "identification_images": [
        "assets/identification/image1.jpg",
        "assets/identification/image2.jpg",
        "assets/identification/image3.jpg"
    ],
    "image": "assets/art/species/animals/black-rat.png",
    "environment_image": "assets/art/environments/hilly-woods.png",
    "card_image": "assets/art/match-game/cards/black-rat-card.png",
    "wikipedia": "https://...",
    "australian_museum": "https://...",
    "references": [
        {
            "field": "history",
            "reference": "Citation text"
        }
    ]
}
```

### Species Fields

- **id**: Unique identifier for the species (required)
- **name**: Common name displayed to users
- **scientific_name**: Scientific/Latin name
- **type**: Either "animals" or "plants"
- **history**: Historical information about the species introduction
- **habitat**: Description of where the species is found
- **diet**: What the species eats (mainly for animals)
- **identification**: Array of identification features as strings
- **identification_images**: Array of up to 3 image paths for visual identification (e.g., showing key features)
- **image**: Main species image path
- **environment_image**: Background image for the detail view
- **card_image**: Image used in the memory match game
- **wikipedia**: Wikipedia URL for more information
- **australian_museum**: Australian Museum URL if available
- **references**: Array of citation objects with "field" and "reference" properties 