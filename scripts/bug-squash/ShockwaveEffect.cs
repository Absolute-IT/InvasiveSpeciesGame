using Godot;
using System;

namespace InvasiveSpeciesAustralia
{
    public partial class ShockwaveEffect : Control
    {
        private ShaderMaterial _shaderMaterial;
        private TextureRect _textureRect;
        private float _currentRadius = 0f;
        private float _maxRadius = 480f; // Increased from 240f for larger effect
        private float _duration = 0.8f; // Decreased from 2.0f for faster animation
        private float _elapsedTime = 0f;
        private bool _isActive = false;
        private Vector2 _center;

        public override void _Ready()
        {
            // Set this control to fill the screen
            SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            MouseFilter = Control.MouseFilterEnum.Ignore; // Don't block input
            
            // Create a BackBufferCopy to capture the screen properly
            var backBufferCopy = new BackBufferCopy();
            backBufferCopy.CopyMode = BackBufferCopy.CopyModeEnum.Viewport;
            AddChild(backBufferCopy);
            
            // Create a full-screen TextureRect to apply the shader
            _textureRect = new TextureRect();
            _textureRect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            _textureRect.MouseFilter = Control.MouseFilterEnum.Ignore; // Don't block input
            
            // Create a transparent texture
            var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
            image.Fill(new Color(1, 1, 1, 0)); // Transparent white
            var texture = ImageTexture.CreateFromImage(image);
            _textureRect.Texture = texture;
            _textureRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            
            AddChild(_textureRect);

            // Load and apply the shockwave shader
            var shaderPath = "res://shaders/shockwave_ripple.gdshader";
            if (ResourceLoader.Exists(shaderPath))
            {
                var shader = GD.Load<Shader>(shaderPath);
                _shaderMaterial = new ShaderMaterial();
                _shaderMaterial.Shader = shader;
                _textureRect.Material = _shaderMaterial;
                
                // Set initial shader parameters
                _shaderMaterial.SetShaderParameter("radius", 0f);
                _shaderMaterial.SetShaderParameter("intensity", 0f);
                _shaderMaterial.SetShaderParameter("max_radius", _maxRadius);
                _shaderMaterial.SetShaderParameter("center", new Vector2(1920f, 1080f)); // Default center
            }
            else
            {
                GD.PrintErr("Shockwave shader not found!");
            }

            // Start hidden
            Visible = false;
        }

        public void TriggerShockwave(Vector2 position)
        {
            if (_shaderMaterial == null) return;

            // Convert world position to UV coordinates (0-1 range)
            var screenSize = new Vector2(3840f, 2160f); // 4K resolution
            _center = new Vector2(position.X / screenSize.X, position.Y / screenSize.Y);
            
            _currentRadius = 0f;
            _elapsedTime = 0f;
            _isActive = true;
            
            // Set the center position in the shader (in UV coordinates)
            _shaderMaterial.SetShaderParameter("center", _center);
            
            // Show the effect
            Visible = true;
            
            GD.Print($"Shockwave triggered at position: {position}, UV: {_center}");
        }

        public override void _Process(double delta)
        {
            if (!_isActive || _shaderMaterial == null) return;

            _elapsedTime += (float)delta;
            float progress = _elapsedTime / _duration;

            if (progress >= 1.0f)
            {
                // Effect complete
                _isActive = false;
                Visible = false;
                _shaderMaterial.SetShaderParameter("intensity", 0f);
                return;
            }

            // Calculate expanding radius
            _currentRadius = _maxRadius * progress;
            
            // Calculate intensity - start at full intensity for debugging
            float intensity = 1.0f - progress; // Simple linear fade out

            // Update shader parameters
            _shaderMaterial.SetShaderParameter("radius", _currentRadius);
            _shaderMaterial.SetShaderParameter("intensity", intensity);
            
            // Debug output
            if (_elapsedTime < 0.1f || Mathf.Abs(_elapsedTime - 0.5f) < 0.05f || Mathf.Abs(_elapsedTime - 0.9f) < 0.05f)
            {
                GD.Print($"Shockwave update - Progress: {progress:F2}, Radius: {_currentRadius:F1}, Intensity: {intensity:F2}");
            }
        }
    }
} 