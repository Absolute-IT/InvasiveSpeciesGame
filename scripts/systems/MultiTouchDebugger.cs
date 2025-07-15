using Godot;
using System.Collections.Generic;

namespace InvasiveSpeciesAustralia.Systems;

/// <summary>
/// Debug tool to visualize multi-touch points on screen
/// Add this to your scene to see all touch points
/// </summary>
public partial class MultiTouchDebugger : Control
{
    private Dictionary<int, TouchDebugInfo> _activeTouches = new();
    private Font _debugFont;
    
    private class TouchDebugInfo
    {
        public Vector2 Position { get; set; }
        public Color Color { get; set; }
    }
    
    // Colors for different touch indices
    private readonly Color[] _touchColors = new[]
    {
        Colors.Red,
        Colors.Green,
        Colors.Blue,
        Colors.Yellow,
        Colors.Magenta,
        Colors.Cyan,
        Colors.Orange,
        Colors.Purple,
        Colors.LightGreen,
        Colors.Pink
    };
    
    public override void _Ready()
    {
        // Cover the entire screen
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore; // Don't block input
        
        // Ensure we're drawn on top of everything else
        ZIndex = 1000;
        TopLevel = true; // Make this a top-level control
        
        _debugFont = ThemeDB.FallbackFont;
        
        // Add to group for easy access
        AddToGroup("touch_debuggers");
        
        // Check if debugging is enabled
        var settingsManager = SettingsManager.Instance;
        Visible = settingsManager.ShowTouchDebugger;
        
        // Use _Input instead of _UnhandledInput to ensure we see events first
        SetProcessInput(true);
        SetProcess(false); // No need for process since we don't fade
    }
    
    public override void _Input(InputEvent @event)
    {
        // Only process if visible
        if (!Visible) return;
        
        // Handle touch events
        if (@event is InputEventScreenTouch touchEvent)
        {
            if (touchEvent.Pressed)
            {
                // Add or update touch point
                _activeTouches[touchEvent.Index] = new TouchDebugInfo
                {
                    Position = touchEvent.Position,
                    Color = _touchColors[touchEvent.Index % _touchColors.Length]
                };
                GD.Print($"Touch {touchEvent.Index} started at {touchEvent.Position}");
            }
            else
            {
                // Remove touch point
                if (_activeTouches.ContainsKey(touchEvent.Index))
                {
                    GD.Print($"Touch {touchEvent.Index} ended");
                    _activeTouches.Remove(touchEvent.Index);
                }
            }
            QueueRedraw();
        }
        else if (@event is InputEventScreenDrag dragEvent)
        {
            // Update touch position during drag
            if (_activeTouches.ContainsKey(dragEvent.Index))
            {
                _activeTouches[dragEvent.Index].Position = dragEvent.Position;
                QueueRedraw();
            }
        }
        
        // IMPORTANT: Don't consume the event - let it pass through to other nodes
        // By not calling event.Handled = true or GetViewport().SetInputAsHandled(),
        // the event will continue to propagate to other nodes
    }
    
    public void SetEnabled(bool enabled)
    {
        Visible = enabled;
        if (!enabled)
        {
            _activeTouches.Clear();
            QueueRedraw();
        }
    }
    
    public override void _Draw()
    {
        foreach (var kvp in _activeTouches)
        {
            var index = kvp.Key;
            var info = kvp.Value;
            var color = info.Color;
            
            // Outer circle
            DrawCircle(info.Position, 60, new Color(color.R, color.G, color.B, 0.3f));
            
            // Inner circle
            DrawCircle(info.Position, 40, new Color(color.R, color.G, color.B, 0.5f));
            
            // Center dot
            DrawCircle(info.Position, 10, color);
            
            // Draw touch index
            var text = $"Touch {index}";
            var textSize = _debugFont.GetStringSize(text, HorizontalAlignment.Center, -1, 20);
            DrawString(_debugFont, info.Position - textSize / 2 + new Vector2(0, -70), text, 
                HorizontalAlignment.Center, -1, 20, color);
            
            // Draw position
            var posText = $"({info.Position.X:F0}, {info.Position.Y:F0})";
            var posTextSize = _debugFont.GetStringSize(posText, HorizontalAlignment.Center, -1, 16);
            DrawString(_debugFont, info.Position - posTextSize / 2 + new Vector2(0, 50), posText, 
                HorizontalAlignment.Center, -1, 16, color);
        }
        
        // Draw active touch count
        if (_activeTouches.Count > 0)
        {
            var countText = $"Active Touches: {_activeTouches.Count}";
            DrawString(_debugFont, new Vector2(20, 40), countText, 
                HorizontalAlignment.Left, -1, 24, Colors.White);
        }
    }
} 