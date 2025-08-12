using Godot;
using System;

namespace InvasiveSpeciesAustralia
{
    public partial class PopTextEffect : Node2D
    {
        private Label _label;
        private float _lifetime = 1.5f;
        private float _elapsed = 0f;
        private Vector2 _velocity;
        private float _fadeStartTime = 0.5f;
        
        // Text variations for invasive species (predators)
        private static readonly string[] PredatorTexts = {
            "SQUASH!", "BAM!", "POW!", "ZAP!", "WHAM!", 
            "BOOM!", "SNAP!", "POP!", "CRASH!", "SMASH!"
        };
        
        // Text variations for native species (prey and food) - mistake feedback
        private static readonly string[] NativeTexts = {
            "NOPE!", "OH NO!", "OOPS!", "WAIT!", "NO NO!",
            "STOP!", "WRONG!", "MISTAKE!", "SORRY!", "ACK!"
        };
        
        // Bright colors for predator hits
        private static readonly Color[] PredatorColors = {
            new Color(1f, 0.2f, 0.2f),    // Bright Red
            new Color(1f, 0.5f, 0f),      // Orange
            new Color(1f, 1f, 0f),        // Yellow
            new Color(0.2f, 1f, 0.2f),    // Bright Green
            new Color(0f, 0.5f, 1f),      // Blue
            new Color(1f, 0f, 1f),        // Magenta
            new Color(0f, 1f, 1f),        // Cyan
        };
        
        // Darker/warning colors for native species mistakes
        private static readonly Color[] NativeColors = {
            new Color(0.8f, 0f, 0f),      // Dark Red
            new Color(0.5f, 0f, 0.5f),    // Purple
            new Color(0.6f, 0.2f, 0f),    // Brown
            new Color(0.4f, 0f, 0.4f),    // Dark Purple
        };

        public override void _Ready()
        {
            // Create the label
            _label = new Label();
            _label.Position = Vector2.Zero;
            _label.AddThemeFontSizeOverride("font_size", 72);
            
            // Use a bold font if available
            var boldFont = ThemeDB.FallbackFont;
            if (boldFont != null)
            {
                _label.AddThemeFontOverride("font", boldFont);
            }
            
            // Center the text
            _label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
            
            // Enable shadow for better visibility
            _label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
            _label.AddThemeConstantOverride("shadow_offset_x", 4);
            _label.AddThemeConstantOverride("shadow_offset_y", 4);
            
            AddChild(_label);
            
            // Ensure we're above game entities
            ZIndex = 100;
        }

        public override void _Process(double delta)
        {
            var deltaF = (float)delta;
            _elapsed += deltaF;
            
            // Move the text
            Position += _velocity * deltaF;
            
            // Slow down over time
            _velocity *= 1f - (2f * deltaF);
            
            // Scale effect - grow then shrink
            var scaleProgress = _elapsed / _lifetime;
            float scale;
            if (scaleProgress < 0.2f)
            {
                // Quick grow
                scale = 1f + (scaleProgress / 0.2f) * 0.5f;
            }
            else
            {
                // Slow shrink
                scale = 1.5f - ((scaleProgress - 0.2f) / 0.8f) * 0.5f;
            }
            Scale = Vector2.One * scale;
            
            // Fade out
            if (_elapsed > _fadeStartTime)
            {
                var fadeProgress = (_elapsed - _fadeStartTime) / (_lifetime - _fadeStartTime);
                Modulate = new Color(1, 1, 1, 1f - fadeProgress);
            }
            
            // Remove when done
            if (_elapsed >= _lifetime)
            {
                QueueFree();
            }
        }
        
        public void Initialize(Vector2 position, EntityBehavior behavior)
        {
            Position = position;
            
            var random = new Random();
            
            // Choose text and color based on behavior
            if (behavior == EntityBehavior.Predator || behavior == EntityBehavior.Weed)
            {
                // Aggressive text for successful hits on invasive species
                _label.Text = PredatorTexts[random.Next(PredatorTexts.Length)];
                _label.AddThemeColorOverride("font_color", PredatorColors[random.Next(PredatorColors.Length)]);
                
                // Normal upward movement
                var angle = -Mathf.Pi / 2 + (GD.Randf() - 0.5f) * Mathf.Pi / 2; // -90° ± 45°
                var speed = 300f + GD.Randf() * 200f; // 300-500 pixels/second
                _velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                
                // Add some random rotation
                _label.RotationDegrees = (GD.Randf() - 0.5f) * 30f; // -15° to +15°
            }
            else
            {
                // Regretful text for mistaken hits on native species
                _label.Text = NativeTexts[random.Next(NativeTexts.Length)];
                _label.AddThemeColorOverride("font_color", NativeColors[random.Next(NativeColors.Length)]);
                
                // More erratic movement for mistakes
                var angle = -Mathf.Pi / 2 + (GD.Randf() - 0.5f) * Mathf.Pi; // Wider angle variance
                var speed = 400f + GD.Randf() * 200f; // Faster movement
                _velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                
                // More dramatic rotation for mistakes
                _label.RotationDegrees = (GD.Randf() - 0.5f) * 45f; // -22.5° to +22.5°
                
                // Slightly larger font for mistakes
                _label.AddThemeFontSizeOverride("font_size", 84);
            }
        }
    }
} 