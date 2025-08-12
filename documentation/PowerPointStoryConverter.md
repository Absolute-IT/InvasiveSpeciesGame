# Story Slide Conversion (LibreOffice + Poppler)

## Overview
The story system now converts PowerPoint presentations to PNG slides using LibreOffice and Poppler at runtime. Slides are written to the Godot user data directory (`user://stories/<id>/`) and then loaded directly by the story scenes.

See Godot’s docs on data paths for details about `res://` vs `user://` [link](https://docs.godotengine.org/en/latest/tutorials/io/data_paths.html).

## How It Works
1. At startup, `ConfigLoader` parses `config/stories.json` into `StoryInfo` items.
2. `StorySlideGenerator` detects the OS and calls LibreOffice to convert PPTX → PDF, then Poppler (`pdftoppm`) to convert PDF → PNG.
3. Output goes to `user://stories/<story-id>/slide-1.png`, `slide-2.png`, … and `thumbnail.png`.
4. `StorySelection` prefers the generated `user://stories/<story-id>/thumbnail.png` when showing cards.
5. `StoryTelling` loads `user://stories/<story-id>/slide-*.png` in numeric order and advances on tap/click; at the end it returns to selection.

## Requirements
Ensure these tools are installed and available on PATH:
- LibreOffice (provides `soffice` or `libreoffice`)
- Poppler (provides `pdftoppm`)

On macOS the generator also checks `/Applications/LibreOffice.app/Contents/MacOS/soffice`.

## Commands Used
Example commands run by the generator (platform-specific binary chosen automatically):

```bash
soffice --headless --convert-to pdf --outdir out "deck.pptx"
pdftoppm -png -rx 300 -ry 300 "out/deck.pdf" "slide"
```

Slides are then renamed to `slide-1.png`, `slide-2.png`, ... and a `thumbnail.png` is created.

## stories.json Format
Minimal configuration per story:

```json
{
  "id": "the-great-escape",
  "title": "The Great Escape",
  "description": "The real reason rabbits don't make great pets.",
  "file": "assets/stories/the-great-escape.pptx",
  "visible": true,
  "thumbnail": "res://optional/fallback.png"
}
```

Notes:
- `file` may be relative to the project root or an absolute path.
- Generated assets live in `user://stories/<id>/`.

## Updating Stories
Replace the PPTX and rerun the app. If the PPTX timestamp is newer than the generated slides, regeneration runs automatically.

## Troubleshooting
- If LibreOffice/Poppler isn’t found, ensure the executables are on PATH or LibreOffice is installed to the default location.
- If no slides appear, confirm PNGs exist in `user://stories/<id>/`.
- Logs in the Godot console include errors from subprocesses when conversions fail.

## Removed Legacy
The previous multi-method converter and slide metadata (`StoryData`, Python/PowerShell helpers) have been removed. The system no longer relies on embedding slide lists in JSON; it discovers generated PNGs in `user://` at runtime.