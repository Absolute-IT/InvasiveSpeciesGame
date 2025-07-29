# Gallery System Documentation

## Overview

The Gallery system provides an interactive species browser with detailed information about invasive animals and plants. It features a grid-based selection view and an immersive detail view with multiple content tabs.

## Features

### Selection View
- **Grid Layout**: 7 items per row with 40px spacing
- **Species Cards**: 430x430px cards with:
  - Environment background image (30% opacity)
  - Species image (centered)
  - Name label (60px height, positioned at bottom)
  - Hover effects (scale to 105%, border highlight)
- **Touch Support**: Optimized for multi-touch tabletop displays
- **Categorization**: Separate sections for Animals and Plants

### Detail View
- **Dynamic Layout**: Alternates between left/right positioning based on species index
- **Environment Background**: Full-screen environment image for immersion
- **Species Image**: Displayed with configurable scale factor
- **Content Tabs**:
  - Overview: History and diet information
  - Identification: Bullet-point list of identifying features
  - Habitat: Habitat description
- **Identification Photos**: Up to 3 reference images displayed below content
- **Navigation**: Previous/Next buttons for browsing within category
- **Smooth Transitions**: Fade animations between species (0.2s out, 0.3s in)

## Key Components

### Gallery.cs
Main controller handling:
- Species data loading from ConfigLoader
- Grid creation and population
- View transitions and animations
- Touch/mouse input handling
- Button styling and state management

### Gallery.tscn
Scene structure with:
- SelectionContainer: Grid view with scrolling
- DetailContainer: Species detail view
- Custom styled buttons with semi-transparent backgrounds
- Edge-faded text panel using shader effect

## Visual Design

### Color Scheme
- Cards: Dark background (0.1, 0.1, 0.1, 0.8) with subtle borders
- Hover: Lighter background (0.2, 0.2, 0.2, 0.9) with bright borders
- Text Panel: Edge-faded dark background (0.05, 0.05, 0.05, 0.85)
- Buttons: Semi-transparent black (0, 0, 0, 0.7) with white text

### Typography
- Species names: Bold, 96px (detail) / 32px (cards)
- Scientific names: Regular, 64px
- Content text: Regular, 56px
- Buttons: Various sizes (36-60px)

## Animations

### Selection to Detail
- Selection fade out: 0.3s
- Detail fade in: 0.3s

### Species Navigation
- Content fade out: 0.2s (parallel)
- Background partial fade: to 0.5 alpha
- Content update
- Content fade in: 0.3s (parallel)

### Hover Effects
- Scale: 1.0 → 1.05 (0.1s)
- Border highlight

## Configuration

Species data loaded from `config/species.json`:
- Name, scientific name
- Image path and scale
- Environment image path
- History, diet, habitat text
- Identification points and images
- Enabled/disabled state

## Technical Notes

### Performance
- Images loaded on-demand
- Smooth tweening for all animations
- Efficient grid rendering with reusable components

### Accessibility
- Large touch targets (430x430px minimum)
- High contrast text on backgrounds
- Clear visual feedback for interactions

### Multi-touch Support
- MultiTouchDebugger integrated for testing
- Touch events prioritized over mouse events
- Proper event handling for tabletop displays 