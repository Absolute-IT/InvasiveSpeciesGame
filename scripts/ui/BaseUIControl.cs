using Godot;

namespace InvasiveSpeciesAustralia.UI;

/// <summary>
/// Base class for all UI controls that automatically handles resolution and scaling changes
/// </summary>
public partial class BaseUIControl : Control
{
    private Vector2 _designResolution = new Vector2(3840, 2160); // Design resolution
    
    public override void _Ready()
    {
        // Set anchors to full screen by default
        SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        
        // Connect to viewport size changed signal
        GetViewport().SizeChanged += OnViewportSizeChanged;
        
        // Initial setup
        HandleResolutionChange();
        
        // Call derived class ready
        OnReady();
    }
    
    /// <summary>
    /// Override this instead of _Ready in derived classes
    /// </summary>
    protected virtual void OnReady()
    {
        // To be overridden by derived classes
    }
    
    private void OnViewportSizeChanged()
    {
        HandleResolutionChange();
    }
    
    private void HandleResolutionChange()
    {
        var viewportSize = GetViewport().GetVisibleRect().Size;
        
        // Calculate scale factors
        var scaleX = viewportSize.X / _designResolution.X;
        var scaleY = viewportSize.Y / _designResolution.Y;
        
        // Use the smaller scale to maintain aspect ratio
        var scale = Mathf.Min(scaleX, scaleY);
        
        // Apply scale to this control
        Scale = Vector2.One * scale;
        
        // Center the UI if there's letterboxing
        var scaledSize = _designResolution * scale;
        var offset = (viewportSize - scaledSize) / 2;
        Position = offset;
        
        // Call derived class handler
        OnResolutionChanged(viewportSize, scale);
    }
    
    /// <summary>
    /// Override this to handle resolution changes in derived classes
    /// </summary>
    protected virtual void OnResolutionChanged(Vector2 newSize, float scale)
    {
        // To be overridden by derived classes if needed
    }
    
    public override void _ExitTree()
    {
        // Disconnect from viewport size changed signal
        if (GetViewport() != null)
        {
            GetViewport().SizeChanged -= OnViewportSizeChanged;
        }
        
        OnExitTree();
    }
    
    /// <summary>
    /// Override this instead of _ExitTree in derived classes
    /// </summary>
    protected virtual void OnExitTree()
    {
        // To be overridden by derived classes
    }
} 