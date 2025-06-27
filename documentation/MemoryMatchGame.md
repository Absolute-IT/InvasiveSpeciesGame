# Memory Match Game System

## Overview

The Memory Match game is an educational card-matching game that teaches players to identify invasive species in Australia. Players match pairs of species cards while learning identification features.

## Game Flow

### 1. Initialization
- Loads all species from `species.json` configuration
- Separates species into animals and plants categories
- Shuffles both lists for random gameplay
- Animals are always used first, followed by plants

### 2. Stage Progression
- **Stage 1**: 2 species (4 cards)
- **Stage 2**: 3 species (6 cards)
- **Stage 3**: 4 species (8 cards)
- **Stage 4**: 5 species (10 cards) - switches to 3 rows
- **Stage 5**: 6 species (12 cards)
- Continues until all species are used
- **Species Order**: All animals are used first (in randomized order), then plants are added once all animals have been introduced

### 3. Identification Phase
Before each stage begins:
- New species are introduced with identification screens
- Each screen shows the species card image and key identification traits
- Display time: 5 seconds per species
- First stage shows all species, subsequent stages only show new additions

### 4. Memory Phase
1. All cards shown face-up for 5 seconds
2. Cards flip face-down
3. Player clicks cards to reveal and match pairs
4. Matched pairs remain face-up
5. Incorrect matches flip back after 0.5 seconds

### 5. Scoring & Timer
- Start time: 3 minutes (180 seconds)
- Penalty for incorrect match: -3 seconds
- Game ends when time runs out or all species are matched

## Key Components

### MemoryMatchGame.cs
Main game controller that manages:
- Stage progression
- Timer and scoring
- Species selection and ordering
- UI state management
- Card grid layout

### MemoryCard.cs
Individual card component handling:
- Card flipping animations
- Click interactions
- Visual states (face-up, face-down, matched)
- Species data display

## Card Layout System

The game dynamically adjusts card layout based on the number of cards, preferring wider grids:

| Cards | Layout | Columns | Rows |
|-------|--------|---------|------|
| 4     | 2×2    | 2       | 2    |
| 6     | 3×2    | 3       | 2    |
| 8     | 4×2    | 4       | 2    |
| 10    | 5×2    | 5       | 2    |
| 12    | 4×3    | 4       | 3    |
| 14    | 5×3    | 5       | 3    |
| 16    | 6×3    | 6       | 3    |
| 18    | 7×3    | 7       | 3    |
| 20    | 6×4    | 6       | 4    |

### Dynamic Card Sizing
- Cards maintain 800:1050 aspect ratio (portrait orientation matching card images)
- Cards are largest in early rounds and get smaller as more are added
- No maximum size for early rounds (cards fill available space)
- Minimum size: 80×105 pixels
- Spacing decreases as card count increases
- Game area uses 90% width, 80% height

## Assets Used

### Card Images
- **Face-up**: Species-specific cards from `assets/art/match-game/cards/`
- **Face-down**: Generic back design from `assets/art/match-game/card-base/back.png`
- **Background**: Neutral texture from `assets/art/match-game/neutral-background.png`

### Species Data
Loaded from `config/species.json`:
- `card_image`: Path to the card face image
- `identification`: Array of identification traits
- `name`: Species common name
- `type`: "animals" or "plants"

## Scene Structure

```
MemoryMatch (Control)
├── Background (TextureRect)
├── VBoxContainer
│   ├── Header (HBoxContainer)
│   │   ├── StageLabel
│   │   ├── TimerLabel
│   │   └── HomeButton
│   └── GameArea (MarginContainer)
│       └── CardGrid (GridContainer)
├── IdentificationPanel (Control)
│   ├── Background (ColorRect)
│   └── VBoxContainer
│       ├── SpeciesImage (TextureRect)
│       ├── SpeciesName (Label)
│       └── IdentificationText (RichTextLabel)
└── GameOverPanel (Control)
    ├── Background (ColorRect)
    └── VBoxContainer
        ├── GameOverText (Label)
        └── HomeButton (Button)
```

## Configuration

The game behavior can be modified through constants in `MemoryMatchGame.cs`:
- `CardRevealTime`: How long cards stay face-up initially (5 seconds)
- `IdentificationDisplayTime`: How long each identification screen shows (5 seconds)
- `MismatchPenalty`: Time penalty for incorrect matches (3 seconds)
- Initial game time: 180 seconds (3 minutes)

## Future Enhancements

Planned features not yet implemented:
- Bonus time pickups (glowing particles)
- Difficulty settings
- Score tracking and leaderboards
- Sound effects and background music
- Additional visual effects
- Progressive difficulty within stages 