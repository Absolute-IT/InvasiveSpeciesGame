using Godot;

namespace InvasiveSpeciesAustralia.UI;

/// <summary>
/// Adds a parallax tilt effect to UI elements based on mouse position with 3D depth illusion
/// </summary>
public partial class ParallaxTiltEffect : Control
{
    [Export] public float TiltIntensity { get; set; } = 10.0f;
    [Export] public float SmoothingSpeed { get; set; } = 8.0f;
    [Export] public bool EnableScale { get; set; } = true;
    [Export] public float ScaleAmount { get; set; } = 0.02f;
    [Export] public float PerspectiveStrength { get; set; } = 0.3f;
    [Export] public float DepthScale { get; set; } = 0.05f;
    [Export] public bool DebugMode { get; set; } = false;
    
    private Vector2 _originalPosition;
    private Vector2 _originalScale;
    private float _originalRotation;
    
    private Vector2 _targetOffset = Vector2.Zero;
    private float _targetRotation = 0.0f;
    private Vector2 _targetScale = Vector2.One;
    private Vector2 _targetPerspectiveScale = Vector2.One;
    
    private bool _isMouseInside = false;
    private bool _initialized = false;
    private Vector2 _lastMousePosition = Vector2.Zero;
    
    public override void _Ready()
    {
        // Store original transform values
        _originalPosition = Position;
        _originalScale = Scale;
        _originalRotation = Rotation;
        
        // Ensure we can receive mouse events
        MouseFilter = MouseFilterEnum.Pass;
        
        if (DebugMode)
        {
            GD.Print($"ParallaxTiltEffect Ready on {GetPath()}: Pos={Position}, Scale={Scale}, Size={Size}");
        }
    }
    
    public override void _Input(InputEvent @event)
    {
        if (!_initialized) return;
        
        // Handle both mouse and touch input
        if (@event is InputEventMouseMotion mouseMotion)
        {
            _lastMousePosition = mouseMotion.GlobalPosition;
            CheckMouseInside(_lastMousePosition);
        }
        else if (@event is InputEventScreenTouch touchEvent)
        {
            _lastMousePosition = touchEvent.Position;
            if (touchEvent.Pressed)
            {
                CheckMouseInside(_lastMousePosition);
            }
            else
            {
                _isMouseInside = false;
            }
        }
        else if (@event is InputEventScreenDrag dragEvent)
        {
            _lastMousePosition = dragEvent.Position;
            CheckMouseInside(_lastMousePosition);
        }
    }
    
    private void CheckMouseInside(Vector2 globalPos)
    {
        var rect = GetGlobalRect();
        bool wasInside = _isMouseInside;
        _isMouseInside = rect.HasPoint(globalPos);
        
        if (DebugMode && wasInside != _isMouseInside)
        {
            GD.Print($"{GetPath()}: Mouse {(_isMouseInside ? "entered" : "exited")} at {globalPos}, Rect: {rect}");
        }
    }
    
    public override void _Draw()
    {
        if (DebugMode && _initialized)
        {
            // Draw container bounds
            DrawRect(new Rect2(Vector2.Zero, Size), Colors.Red, false, 2.0f);
            
            // Draw pivot point
            DrawCircle(PivotOffset, 5.0f, Colors.Yellow);
            
            // Draw center point
            DrawCircle(Size / 2.0f, 3.0f, Colors.Green);
            
            // Draw detection area with semi-transparent fill
            var detectionColor = _isMouseInside ? new Color(0, 1, 0, 0.1f) : new Color(1, 0, 0, 0.1f);
            DrawRect(new Rect2(Vector2.Zero, Size), detectionColor, true);
            
            // Draw size text
            var font = ThemeDB.FallbackFont;
            var fontSize = 16;
            var sizeText = $"Size: {Size.X}x{Size.Y}";
            DrawString(font, new Vector2(5, 20), sizeText, HorizontalAlignment.Left, -1, fontSize, Colors.Yellow);
            
            // Draw mouse indicator if inside
            if (_isMouseInside)
            {
                var localMouse = GetLocalMousePosition();
                DrawCircle(localMouse, 8.0f, Colors.Blue);
                
                // Draw mouse position text
                var mousePosText = $"Mouse: {localMouse.X:F0},{localMouse.Y:F0}";
                DrawString(font, new Vector2(5, 40), mousePosText, HorizontalAlignment.Left, -1, fontSize, Colors.Blue);
            }
        }
    }
    
    public override void _Process(double delta)
    {
        // Initialize pivot offset on first frame when Size is properly calculated
        if (!_initialized && Size.X > 0 && Size.Y > 0)
        {
            PivotOffset = Size / 2.0f;
            _initialized = true;
            
            if (DebugMode)
            {
                GD.Print($"ParallaxTiltEffect Initialized on {GetPath()}: Size={Size}, PivotOffset={PivotOffset}");
            }
        }
        
        if (!_initialized) return;
        
        if (!_isMouseInside)
        {
            // Smoothly return to original position when mouse is outside
            _targetOffset = Vector2.Zero;
            _targetRotation = 0.0f;
            _targetScale = Vector2.One;
            _targetPerspectiveScale = Vector2.One;
        }
        else
        {
            // Calculate tilt based on mouse position
            UpdateTiltFromMouse();
        }
        
        // Smoothly interpolate to target values
        float smoothingFactor = (float)(SmoothingSpeed * delta);
        
        // Position
        Vector2 currentOffset = Position - _originalPosition;
        Vector2 newOffset = currentOffset.Lerp(_targetOffset, smoothingFactor);
        Position = _originalPosition + newOffset;
        
        // Rotation
        Rotation = Mathf.Lerp(Rotation, _originalRotation + _targetRotation, smoothingFactor);
        
        // Scale with perspective
        if (EnableScale)
        {
            Vector2 combinedScale = _originalScale * _targetScale * _targetPerspectiveScale;
            Scale = Scale.Lerp(combinedScale, smoothingFactor);
        }
        
        // Trigger redraw in debug mode
        if (DebugMode)
        {
            QueueRedraw();
        }
    }
    
    private void UpdateTiltFromMouse()
    {
        if (!_initialized) return;
        
        Vector2 mousePos = _lastMousePosition;
        Vector2 centerPos = GlobalPosition + PivotOffset;
        
        // Calculate normalized offset from center (-1 to 1)
        Vector2 offset = (mousePos - centerPos) / (Size / 2.0f);
        offset = offset.Clamp(new Vector2(-1, -1), new Vector2(1, 1));
        
        // Apply tilt with enhanced vertical movement for depth
        _targetOffset = new Vector2(
            offset.X * TiltIntensity,
            offset.Y * TiltIntensity * 0.7f // Increased vertical movement
        );
        
        // Apply rotation for tilt effect
        _targetRotation = -offset.X * 0.08f; // Increased rotation
        
        // Apply perspective scaling - different X and Y scales for perspective effect
        float perspectiveX = 1.0f - (Mathf.Abs(offset.Y) * PerspectiveStrength * 0.3f);
        float perspectiveY = 1.0f + (offset.Y * PerspectiveStrength * 0.2f);
        _targetPerspectiveScale = new Vector2(perspectiveX, perspectiveY);
        
        // Apply scale based on Y position and proximity to center
        if (EnableScale)
        {
            float distanceFromCenter = offset.Length();
            float depthFactor = 1.0f + (offset.Y * DepthScale); // Scale based on Y position
            float hoverScale = 1.0f + (1.0f - distanceFromCenter) * ScaleAmount;
            
            _targetScale = Vector2.One * depthFactor * hoverScale;
        }
        
        if (DebugMode && GetTree().GetFrame() % 60 == 0) // Log every second
        {
            GD.Print($"Tilt Update on {GetPath()}: MousePos={mousePos}, Offset={offset}, TargetOffset={_targetOffset}");
        }
    }
} 