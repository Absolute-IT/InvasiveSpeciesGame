# Gallery Feature Documentation

## Overview

The Gallery is an interactive species guide that displays information about invasive animals and plants in Australia. It consists of two main views:

1. **Species Selection View** - A grid-based character selection screen
2. **Species Detail View** - Detailed information about a selected species with alternating layout

## Technical Implementation

### Files

- **Scene**: `scenes/gallery/Gallery.tscn`
- **Script**: `scripts/gallery/Gallery.cs`
- **Data**: `config/species.json` (with added `environment_image` property)

### Dependencies

- `BaseUIControl` - For responsive scaling and resolution handling
- `ConfigLoader` - For loading species data from JSON
- `Species` class - Data model for species information

## Features

### Species Selection View

#### Grid Layout
- **Structure**: 
  - Animals displayed in top section
  - Plants displayed in bottom section
  - Both sections auto-wrap to multiple rows as needed
- **Grid Configuration**:
  - 10 items per row
  - 320x320px item size
  - 40px spacing between items
  - Scrollable container for overflow

#### Species Cards
Each species is displayed as a square card with:
- **Border**: 4px border with rounded corners (15px radius)
- **Background**: Semi-transparent dark background
- **Image**: Species portrait centered in the card
- **Label**: Species name at the bottom with dark overlay for readability

#### Interactivity
- **Hover Effect**: 
  - Border color brightens
  - Background lightens slightly
  - Card scales up by 5%
- **Click**: Transitions to the Species Detail View

### Species Detail View

#### Layout Alternation
The detail view alternates between two layouts based on the species' position in its row:
- **Even index (0, 2, 4...)**: Species image on left, text panel on right
- **Odd index (1, 3, 5...)**: Species image on right, text panel on left

This creates visual variety and follows the concept art provided.

#### Background
Each species has an associated environment background image defined in the `environment_image` property in `species.json`. The backgrounds are semi-darkened with a 30% opacity overlay for better text readability.

#### Species Image Display
The species image is displayed in a container that includes:
- **Main Image**: Slightly reduced in size to accommodate navigation controls
- **Navigation Buttons**: Previous/Next buttons positioned directly below the image for easy access
- **Container Positioning**: 110px from top, 140px from bottom (provides more vertical space)
- **Increased dimensions**: Container is 20px larger in both width and height for better content display

#### Text Panel
The text panel includes:
- **Species Name**: Large bold title (72pt)
- **Scientific Name**: Subtitle in regular font (48pt)
- **Content Area**: Scrollable text area that displays different information based on selected tab
- **Tab Buttons**: Three buttons to switch between content types
- **Photo Placeholders**: Three boxes for future real photos

#### Content Types
1. **Overview**: Displays `history` and `diet` information
2. **Identification**: Shows bullet-pointed list of identification features
3. **Habitat**: Displays habitat information

#### Navigation
- **Back Button**: Returns to species selection view (top-left corner)
- **Next/Previous Buttons**: Navigate between species within the same category (below species image)
- **Home Button**: Returns to main menu (bottom-right corner, present in both views)

### Edge Fade Effect
The text panel background uses a custom shader (`edge_fade.gdshader`) to create smooth faded edges:
- **Fade Size**: 5% of panel dimensions
- **Fade Smoothness**: 0.8 for gradual transition
- **Background Color**: Black with 85% opacity
- Creates a softer, more visually appealing text container

## User Flow

1. User clicks "Species Guide" from Main Menu
2. Gallery loads showing grid of all species
3. User can scroll to see all animals and plants
4. Hovering over a species card provides visual feedback
5. Clicking a species card transitions to detail view
6. In detail view, user can:
   - Read different types of information using tab buttons
   - Navigate to next/previous species
   - Return to selection view
   - Go back to main menu

## Data Structure

### Species JSON Enhancement
Added `environment_image` property to each species:
```json
{
  "id": "red-fox",
  "name": "Red Fox",
  "scientific_name": "Vulpes vulpes",
  "type": "animals",
  "environment_image": "assets/art/environments/hilly-woods.png",
  // ... other properties
}
```

### Environment Mappings
- **Desert/Arid animals**: `desert.png` or `arid.png`
- **Wetland species**: `wetlands.png`
- **Forest dwellers**: `hilly-woods.png` or `jungle.png`
- **Coastal species**: `mangroves.png`
- **Agricultural pests**: `vineyards.png` or `grasslands.png`
- **Soil pathogens**: `barren-dirt.png` or `ant-dirt.png`

## Animations and Transitions

### View Transitions
- **Selection to Detail**: Fade out (0.3s) → switch view → fade in (0.3s)
- **Detail to Selection**: Same fade transition in reverse
- **To Main Menu**: Fade out current scene before changing

### Content Switching
When switching between Overview/Identification/Habitat:
- Current text fades out (0.2s)
- New text content is loaded
- New text fades in (0.2s)

### Interactive Feedback
- **Card hover**: Scale animation (0.1s) and style changes
- **Button states**: Active button shown in white, inactive in 70% gray

## Resolution Support

The Gallery uses `BaseUIControl` for automatic scaling:
- Designed for 3840x2160 (4K)
- Automatically scales to maintain aspect ratio
- Centers content with letterboxing if needed

## Font Usage

- **Titles**: Nunito Bold
- **Body Text**: Nunito Regular
- **Consistent sizing across resolution changes**

## Future Enhancements

1. **Real Photos**: The three photo placeholders in the detail view are ready for real photographs when available
2. **Search/Filter**: Could add species filtering by biome or characteristics
3. **Audio**: Could add species sounds or narration
4. **Additional Info Tabs**: More detailed information categories could be added
5. **Favoriting**: Allow users to mark species for quick access

## Code Architecture

### Gallery.cs Structure

```csharp
public partial class Gallery : BaseUIControl
{
    // Grid configuration constants
    // UI element references
    // Data storage for species lists
    // View state management
    
    protected override void OnReady()
    {
        // Initialize UI references
        // Load species data
        // Setup grids
        // Connect signals
    }
    
    private Control CreateSpeciesItem(Species species, int index, bool isAnimal)
    {
        // Dynamically create species cards
        // Setup hover/click interactions
    }
    
    private void ShowSpeciesDetail(Species species, int index, bool isAnimal)
    {
        // Configure detail view
        // Handle layout alternation
        // Load appropriate content
    }
}
```

### Key Design Decisions

1. **Dynamic Grid Creation**: Species cards are created programmatically rather than in the scene file for flexibility
2. **Separate Containers**: Selection and Detail views are separate containers for clean transitions
3. **Content Type State**: Current content type (overview/identification/habitat) is tracked for button state management
4. **Index-based Layout**: Using array index for layout alternation ensures consistent patterns
5. **Navigation Placement**: Next/Previous buttons placed directly below species image for intuitive navigation

## Usage Tips

1. **Adding New Species**: Simply add to `species.json` with appropriate `environment_image`
2. **Modifying Grid Layout**: Change `ItemsPerRow`, `ItemSize`, or `ItemSpacing` constants
3. **Styling Changes**: Modify StyleBoxFlat resources in the scene file
4. **Content Formatting**: The identification list automatically formats with bullet points
5. **Performance**: The grid uses a ScrollContainer, so only visible items impact performance 

## UI Components

### Selection View
- **Grid Layout**: Two grids (animals and plants) with 10 items per row
- **Grid Items**: 320x320px cards with species images and names
- **Hover Effects**: Scale animation and border highlight on mouse hover

### Detail View
- **Background**: Full-screen environment image specific to each species
- **Species Image**: Large display of the selected species
- **Text Panel**: Information panel with tabbed content (Overview, Identification, Habitat)
- **Navigation**: Previous/Next buttons for browsing within the same category

### Button Styling
All buttons in the Gallery use a consistent design with variations in size:

**Content Buttons (Overview, Identification, Habitat):**
- **Background**: Semi-transparent black (70% opacity) with rounded corners
- **Text**: White color, 48px font size
- **Padding**: 60px horizontal, 40px vertical
- **Border**: Subtle white border (30% opacity)

**Navigation Buttons (Previous/Next):**
- **Background**: Same semi-transparent black style
- **Text**: White color, 42px font size
- **Padding**: 100px horizontal, 40px vertical (extra wide for easy tapping)
- **Border**: Same subtle white border

**Utility Buttons (Back, Home):**
- **Background**: Same semi-transparent black style
- **Text**: White color, 48px font size (52px for Home button)
- **Padding**: 60px horizontal, 20px vertical (reduced height)
- **Border**: Same subtle white border

**Button States:**
- Normal: Black background with 70% opacity
- Hover: Slightly lighter background (80% opacity) with brighter border
- Active tab: More opaque background (80%) with brighter border
- Inactive tab: Less opaque background (40%) with dimmer border

## Visual Effects

### View Transitions
- **Selection to Detail**: Fade out (0.3s) → switch view → fade in (0.3s)
- **Detail to Selection**: Same fade transition in reverse
- **To Main Menu**: Fade out current scene before changing

### Content Switching
When switching between Overview/Identification/Habitat:
- Current text fades out (0.2s)
- New text content is loaded
- New text fades in (0.2s)

### Interactive Feedback
- **Card hover**: Scale animation (0.1s) and style changes
- **Button states**: Active button shown in white, inactive in 70% gray

### Identification Images

The gallery now supports displaying up to 3 identification images for each species:

- Images are loaded from paths specified in the `identification_images` field in `species.json`
- Images are displayed in three preview boxes below the content tabs
- Images use `KeepAspectCovered` stretch mode to fill the boxes while maintaining aspect ratio
- Images are automatically cleared and reloaded when switching between species

## Resolution Support

The Gallery uses `BaseUIControl` for automatic scaling:

- Designed for 3840x2160 (4K)
- Automatically scales to maintain aspect ratio
- Centers content with letterboxing if needed

## Font Usage

- **Titles**: Nunito Bold
- **Body Text**: Nunito Regular
- **Consistent sizing across resolution changes**

## Future Enhancements

1. **Real Photos**: The three photo placeholders in the detail view are ready for real photographs when available
2. **Search/Filter**: Could add species filtering by biome or characteristics
3. **Audio**: Could add species sounds or narration
4. **Additional Info Tabs**: More detailed information categories could be added
5. **Favoriting**: Allow users to mark species for quick access

## Code Architecture

### Gallery.cs Structure

```csharp
public partial class Gallery : BaseUIControl
{
    // Grid configuration constants
    // UI element references
    // Data storage for species lists
    // View state management
    
    protected override void OnReady()
    {
        // Initialize UI references
        // Load species data
        // Setup grids
        // Connect signals
    }
    
    private Control CreateSpeciesItem(Species species, int index, bool isAnimal)
    {
        // Dynamically create species cards
        // Setup hover/click interactions
    }
    
    private void ShowSpeciesDetail(Species species, int index, bool isAnimal)
    {
        // Configure detail view
        // Handle layout alternation
        // Load appropriate content
    }
}
```

### Key Design Decisions

1. **Dynamic Grid Creation**: Species cards are created programmatically rather than in the scene file for flexibility
2. **Separate Containers**: Selection and Detail views are separate containers for clean transitions
3. **Content Type State**: Current content type (overview/identification/habitat) is tracked for button state management
4. **Index-based Layout**: Using array index for layout alternation ensures consistent patterns
5. **Navigation Placement**: Next/Previous buttons placed directly below species image for intuitive navigation

## Usage Tips

1. **Adding New Species**: Simply add to `species.json` with appropriate `environment_image`
2. **Modifying Grid Layout**: Change `ItemsPerRow`, `ItemSize`, or `ItemSpacing` constants
3. **Styling Changes**: Modify StyleBoxFlat resources in the scene file
4. **Content Formatting**: The identification list automatically formats with bullet points
5. **Performance**: The grid uses a ScrollContainer, so only visible items impact performance 

## UI Components

### Selection View
- **Grid Layout**: Two grids (animals and plants) with 10 items per row
- **Grid Items**: 320x320px cards with species images and names
- **Hover Effects**: Scale animation and border highlight on mouse hover

### Detail View
- **Background**: Full-screen environment image specific to each species
- **Species Image**: Large display of the selected species
- **Text Panel**: Information panel with tabbed content (Overview, Identification, Habitat)
- **Navigation**: Previous/Next buttons for browsing within the same category

### Button Styling
All buttons in the Gallery use a consistent design with variations in size:

**Content Buttons (Overview, Identification, Habitat):**
- **Background**: Semi-transparent black (70% opacity) with rounded corners
- **Text**: White color, 48px font size
- **Padding**: 60px horizontal, 40px vertical
- **Border**: Subtle white border (30% opacity)

**Navigation Buttons (Previous/Next):**
- **Background**: Same semi-transparent black style
- **Text**: White color, 42px font size
- **Padding**: 100px horizontal, 40px vertical (extra wide for easy tapping)
- **Border**: Same subtle white border

**Utility Buttons (Back, Home):**
- **Background**: Same semi-transparent black style
- **Text**: White color, 48px font size (52px for Home button)
- **Padding**: 60px horizontal, 20px vertical (reduced height)
- **Border**: Same subtle white border

**Button States:**
- Normal: Black background with 70% opacity
- Hover: Slightly lighter background (80% opacity) with brighter border
- Active tab: More opaque background (80%) with brighter border
- Inactive tab: Less opaque background (40%) with dimmer border

## Visual Effects

### View Transitions
- **Selection to Detail**: Fade out (0.3s) → switch view → fade in (0.3s)
- **Detail to Selection**: Same fade transition in reverse
- **To Main Menu**: Fade out current scene before changing

### Content Switching
When switching between Overview/Identification/Habitat:
- Current text fades out (0.2s)
- New text content is loaded
- New text fades in (0.2s)

### Interactive Feedback
- **Card hover**: Scale animation (0.1s) and style changes
- **Button states**: Active button shown in white, inactive in 70% gray 