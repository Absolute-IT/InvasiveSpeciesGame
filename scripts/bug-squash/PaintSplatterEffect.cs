using Godot;
using System;

namespace InvasiveSpeciesAustralia
{
    public partial class PaintSplatterEffect : Sprite2D
    {
        private ShaderMaterial _shaderMaterial;
        private float _duration = 2.0f;
        private float _elapsedTime = 0f;
        private bool _isActive = false;
        private Vector2 _worldPosition;
        
        public override void _Ready()
        {
            // Start completely invisible via modulate
            Modulate = new Color(1, 1, 1, 0); // Alpha = 0
            Visible = true; // Keep visible but transparent
            
            // Don't set any texture initially - wait for the viewport
            Texture = null;
            
            // Set the sprite to be centered
            Centered = true;
            
            // Load the paint splatter shader
            var shaderPath = "res://shaders/paint_splatter.gdshader";
            if (ResourceLoader.Exists(shaderPath))
            {
                var shader = GD.Load<Shader>(shaderPath);
                _shaderMaterial = new ShaderMaterial();
                _shaderMaterial.Shader = shader;
                Material = _shaderMaterial;
                
                // Create a tiny transparent texture specifically for entity_texture
                var transparentImage = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
                transparentImage.Fill(new Color(0, 0, 0, 0)); // Fully transparent
                var transparentTexture = ImageTexture.CreateFromImage(transparentImage);
                
                // Set initial shader parameters
                _shaderMaterial.SetShaderParameter("progress", 0f);
                _shaderMaterial.SetShaderParameter("splatter_scale", 1.5f); // Reduced scale
                _shaderMaterial.SetShaderParameter("entity_scale", 0.08f); // Smaller entity relative to texture
                
                // Set a default transparent texture for entity_texture to prevent white flash
                _shaderMaterial.SetShaderParameter("entity_texture", transparentTexture);
                _shaderMaterial.SetShaderParameter("ring_color", new Color(0, 0, 0, 0)); // Completely transparent
                _shaderMaterial.SetShaderParameter("entity_center", new Vector2(0.5f, 0.5f));
            }
            else
            {
                GD.PrintErr("Paint splatter shader not found!");
            }
        }
        
        public void TriggerSplatter(BugSquashEntity entity)
        {
            if (_shaderMaterial == null || entity == null) return;
            
            _worldPosition = entity.GlobalPosition;
            _elapsedTime = 0f;
            _isActive = false; // Don't start processing until ready
            
            // Ensure effect is invisible until ready
            Modulate = new Color(1, 1, 1, 0); // Keep transparent
            
            // Create a viewport to render the entity
            var viewport = new SubViewport();
            viewport.Size = new Vector2I(512, 512); // Size for the splatter texture
            viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
            viewport.TransparentBg = true;
            AddChild(viewport);
            
            // Create a copy of the entity's visuals in the viewport
            var entityCopy = new Node2D();
            entityCopy.Position = viewport.Size / 2; // Center in viewport
            viewport.AddChild(entityCopy);
            
            // Find the sprite and ring nodes by checking their properties
            Sprite2D entitySprite = null;
            Sprite2D entityRing = null;
            
            foreach (Node child in entity.GetChildren())
            {
                if (child is Sprite2D sprite)
                {
                    // The entity sprite has ShowBehindParent = true
                    if (sprite.ShowBehindParent)
                    {
                        entitySprite = sprite;
                    }
                    // The ring doesn't have ShowBehindParent
                    else
                    {
                        entityRing = sprite;
                    }
                }
            }
            
            if (entitySprite != null)
            {
                var spriteCopy = new Sprite2D();
                spriteCopy.Texture = entitySprite.Texture;
                spriteCopy.Scale = entitySprite.Scale;
                spriteCopy.Modulate = entitySprite.Modulate;
                spriteCopy.Material = entitySprite.Material; // Include the circular clip shader
                spriteCopy.ShowBehindParent = true;
                entityCopy.AddChild(spriteCopy);
            }
            
            Color ringColor = Colors.White;
            if (entityRing != null)
            {
                var ringCopy = new Sprite2D();
                ringCopy.Texture = entityRing.Texture;
                ringCopy.Scale = entityRing.Scale;
                ringCopy.Modulate = entityRing.Modulate;
                ringColor = entityRing.Modulate;
                entityCopy.AddChild(ringCopy);
            }
            
            // Wait for viewport to render, then use its texture
            CallDeferred(nameof(WaitForViewportReady), viewport, ringColor);
            
            // Position the sprite at the entity location (it's centered)
            GlobalPosition = _worldPosition;
            
            // Set z-index to render behind entities but above background
            ZIndex = -1;
            
            // Don't show yet - wait until viewport texture is ready
            
            GD.Print($"Paint splatter triggered for {entity.Species?.Name} at {_worldPosition}");
        }
        
        private void WaitForViewportReady(SubViewport viewport, Color ringColor)
        {
            // Wait a frame for the viewport to render, then apply the texture
            CallDeferred(nameof(ApplyViewportTexture), viewport, ringColor);
        }
        
        private void ApplyViewportTexture(SubViewport viewport, Color ringColor)
        {
            // Wait for viewport to render by checking if it has a valid texture
            if (viewport.GetTexture() == null)
            {
                // Viewport hasn't rendered yet, wait another frame
                GD.Print("Viewport texture is null, waiting...");
                CallDeferred(nameof(ApplyViewportTexture), viewport, ringColor);
                return;
            }
            
            // Get the rendered texture from viewport
            var viewportTexture = viewport.GetTexture();
            
            // Validate that the texture is actually rendered and not empty
            if (viewportTexture == null || viewportTexture.GetSize() == Vector2I.Zero)
            {
                GD.PrintErr("Viewport texture is invalid, retrying...");
                CallDeferred(nameof(ApplyViewportTexture), viewport, ringColor);
                return;
            }
            
            GD.Print($"Viewport texture ready: {viewportTexture.GetSize()}, Ring color: {ringColor}");
            
            // Create the base texture now that we're ready
            if (Texture == null)
            {
                var image = Image.CreateEmpty(2048, 2048, false, Image.Format.Rgba8);
                image.Fill(new Color(0, 0, 0, 0)); // Fully transparent
                Texture = ImageTexture.CreateFromImage(image);
            }
            
            // Set shader parameters
            _shaderMaterial.SetShaderParameter("entity_texture", viewportTexture);
            _shaderMaterial.SetShaderParameter("ring_color", ringColor);
            _shaderMaterial.SetShaderParameter("entity_center", new Vector2(0.5f, 0.5f));
            
            // Now that everything is set up, fade in the effect and start processing
            Modulate = new Color(1, 1, 1, 1); // Fade in to full opacity
            _isActive = true; // Start processing now that texture is ready
            GD.Print("Paint splatter effect now fading in and active");
            
            // Clean up viewport after a frame
            CallDeferred(nameof(CleanupViewport), viewport);
        }
        
        private void CleanupViewport(SubViewport viewport)
        {
            viewport.QueueFree();
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
                QueueFree();
                return;
            }
            
            // Update shader progress
            _shaderMaterial.SetShaderParameter("progress", progress);
        }
    }
} 