using Godot;
using System;

namespace InvasiveSpeciesAustralia
{
    // Displays animated status text above an entity while it performs an action
    // Shakes slowly at first and speeds up as progress approaches completion.
    // On finish, it expands and fades out.
    public partial class ActionStatusText : Node2D
    {
        private Label _label;
        private float _progress; // 0..1
        private float _time;
        private bool _isFinishing;
        private float _finishElapsed;
        private const float FINISH_DURATION = 0.6f;
        private Vector2 _cachedTextSize = Vector2.Zero;
        private Node2D _targetA;
        private Node2D _targetB;
        private float _verticalOffset = 30f;

        // Visual tuning
        private float _baseVerticalOffset = 0f; // Provided by caller via Position
        private const float BASE_AMPLITUDE_PX = 10f;
        private const float MIN_FREQUENCY = 1.25f;  // Hz
        private const float MAX_FREQUENCY = 5.0f;  // Hz

        // Vibrant color palette similar to PopTextEffect's predator colors
        private static readonly Color[] VibrantColors =
        {
            new Color(1f, 0.2f, 0.2f),    // Bright Red
            new Color(1f, 0.5f, 0f),      // Orange
            new Color(1f, 1f, 0f),        // Yellow
            new Color(0.2f, 1f, 0.2f),    // Bright Green
            new Color(0f, 0.5f, 1f),      // Blue
            new Color(1f, 0f, 1f),        // Magenta
            new Color(0f, 1f, 1f),        // Cyan
        };

        public override void _Ready()
        {
            _label = new Label();
            _label.MouseFilter = Control.MouseFilterEnum.Ignore;
            _label.HorizontalAlignment = HorizontalAlignment.Center;
            _label.VerticalAlignment = VerticalAlignment.Center;
            _label.AddThemeFontSizeOverride("font_size", 64);

            // Use fallback font (will be replaced by project theme if present)
            var font = ThemeDB.FallbackFont;
            if (font != null)
            {
                _label.AddThemeFontOverride("font", font);
            }

            // Text shadow for legibility
            _label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.85f));
            _label.AddThemeConstantOverride("shadow_offset_x", 3);
            _label.AddThemeConstantOverride("shadow_offset_y", 3);

            AddChild(_label);

            ZIndex = 50; // Above entities
            Modulate = Colors.White;
        }

        // Initialize with text and random vibrant color
        public void Initialize(string text)
        {
            _label.Text = text;
            var rand = GD.RandRange(0, VibrantColors.Length - 1);
            _label.AddThemeColorOverride("font_color", VibrantColors[rand]);

            // Cache text size for centering
            var font = _label.GetThemeFont("font");
            var fontSize = _label.GetThemeFontSize("font_size");
            if (font != null)
            {
                _cachedTextSize = font.GetStringSize(_label.Text, HorizontalAlignment.Left, -1, fontSize);
            }
            else
            {
                _cachedTextSize = _label.GetMinimumSize();
            }
        }

        // Set targets to follow; if b is null, follows only a
        public void SetTargets(Node2D a, Node2D b = null, float verticalOffset = 30f)
        {
            _targetA = a;
            _targetB = b;
            _verticalOffset = verticalOffset;
        }

        // Update progress externally (0..1)
        public void SetProgress(float progress)
        {
            _progress = Mathf.Clamp(progress, 0f, 1f);
        }

        // Trigger finishing animation: stop shaking, expand and fade
        public void Finish()
        {
            _isFinishing = true;
            _finishElapsed = 0f;
        }

        public override void _Process(double delta)
        {
            var dt = (float)delta;
            _time += dt;

            if (_isFinishing)
            {
                _finishElapsed += dt;
                var t = Mathf.Clamp(_finishElapsed / FINISH_DURATION, 0f, 1f);

                // Ease out expansion and fade
                var eased = 1f - Mathf.Pow(1f - t, 2f);
                Scale = Vector2.One * (1.0f + 0.6f * eased);
                Modulate = new Color(1, 1, 1, 1f - eased);

                // Keep label centered at local origin while finishing
                CenterLabelAtOrigin(Vector2.Zero);

                if (_finishElapsed >= FINISH_DURATION)
                {
                    QueueFree();
                }
                return;
            }

            // Follow targets by updating global position to midpoint (+ upward offset)
            UpdateFollowPosition();

            // Active shaking state
            var amplitude = BASE_AMPLITUDE_PX; // Keep amplitude steady
            // Frequency ramps up with progress; slight easing for nicer feel
            var freq = Mathf.Lerp(MIN_FREQUENCY, MAX_FREQUENCY, Mathf.Pow(_progress, 1.2f));

            // Compute jitter using sin/cos to create a small 2D vibration
            var jitterX = Mathf.Sin(_time * Mathf.Tau * freq) * amplitude;
            var jitterY = Mathf.Cos(_time * Mathf.Tau * (freq * 1.13f)) * amplitude * 0.6f;

            // A tiny rotation wobble for life
            Rotation = Mathf.Sin(_time * Mathf.Tau * (freq * 0.37f)) * 0.03f;

            // Place label around the local origin with jitter
            CenterLabelAtOrigin(new Vector2(jitterX, jitterY));
        }

        private void UpdateFollowPosition()
        {
            if (_targetA != null && IsInstanceValid(_targetA))
            {
                var posA = _targetA.GlobalPosition;
                Vector2 center = posA;
                if (_targetB != null && IsInstanceValid(_targetB))
                {
                    var posB = _targetB.GlobalPosition;
                    center = (posA + posB) * 0.5f;
                }
                GlobalPosition = center + new Vector2(0, -_verticalOffset);
            }
        }

        private void CenterLabelAtOrigin(Vector2 offset)
        {
            // Position so that the label is horizontally centered at local origin, above (negative y)
            var pos = new Vector2(-_cachedTextSize.X / 2f, -_cachedTextSize.Y / 2f);
            _label.Position = pos + offset;
        }
    }
}


