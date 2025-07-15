using Godot;

namespace InvasiveSpeciesAustralia;

public partial class RibbonButton : Control
{
    [Export] public string Text { get; set; } = "Button";
    [Export] public bool IsLeftSide { get; set; } = true;
    [Export] public Color BaseColor { get; set; } = new Color(0.9f, 0.95f, 0.92f, 0.5f);
    [Export] public Color HoverColor { get; set; } = new Color(0.95f, 0.98f, 0.96f, 0.85f);
    [Export] public Color PressedColor { get; set; } = new Color(0.85f, 0.9f, 0.88f, 0.95f);
    [Export] public float BlurAmount { get; set; } = 3.0f;
    
    [Signal]
    public delegate void PressedEventHandler();
    
    private Label _label;
    private float _baseOpacity = 0.5f;
    private float _hoverOpacity = 0.85f;
    private float _pressedOpacity = 0.95f;
    private bool _isHovered = false;
    private bool _isPressed = false;
    private Vector2 _originalLabelPosition;
    
    // Design resolution reference
    private readonly Vector2 _designResolution = new Vector2(3840, 2160);
    private float _currentScale = 1.0f;
    
    // Base values (designed for 3840x2160)
    private readonly float _baseFontSize = 110.0f;
    private readonly float _baseTextAnimationOffset = 20.0f;
    private readonly float _baseEdgeLineThickness = 4.0f;
    private readonly Vector2 _baseMinimumSize = new Vector2(1200, 216);
    private readonly float _baseLabelPadding = 60.0f;
    private readonly Vector2 _baseLabelSize = new Vector2(860, 216);
    private readonly int _baseShadowOffset = 3;
    
    private Font _nunitoFont;
    private ShaderMaterial _blurMaterial;
    

    
    public override void _Ready()
    {
        MouseDefaultCursorShape = CursorShape.PointingHand;
        
        // Load and apply backdrop blur shader
        var blurShader = GD.Load<Shader>("res://shaders/backdrop_blur.gdshader");
        _blurMaterial = new ShaderMaterial();
        _blurMaterial.Shader = blurShader;
        _blurMaterial.SetShaderParameter("blur_amount", BlurAmount);
        Material = _blurMaterial;
        
        // Load Nunito font
        _nunitoFont = GD.Load<Font>("res://assets/fonts/Nunito/Nunito-Bold.ttf");
        
        // Create label with enhanced visibility
        _label = new Label();
        _label.AddThemeColorOverride("font_color", new Color(0.95f, 0.95f, 0.95f, 1));
        _label.AddThemeColorOverride("font_shadow_color", new Color(0.02f, 0.02f, 0.02f, 0.9f));
        _label.AddThemeConstantOverride("shadow_blur_radius", 2);
        _label.AddThemeFontOverride("font", _nunitoFont);
        _label.Text = Text;
        _label.VerticalAlignment = VerticalAlignment.Center;
        _label.SizeFlagsVertical = Control.SizeFlags.Fill | Control.SizeFlags.ExpandFill;
        
        // Clear any potential margins that might affect positioning
        _label.AddThemeConstantOverride("margin_top", 0);
        _label.AddThemeConstantOverride("margin_bottom", 0);
        _label.AddThemeConstantOverride("margin_left", 0);
        _label.AddThemeConstantOverride("margin_right", 0);
        
        AddChild(_label);
        
        // Set label alignment without using anchors
        if (IsLeftSide)
        {
            _label.HorizontalAlignment = HorizontalAlignment.Left;
        }
        else
        {
            _label.HorizontalAlignment = HorizontalAlignment.Right;
        }
        
        // Connect input events
        GuiInput += OnGuiInput;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        
        // Connect to viewport size changes
        GetViewport().SizeChanged += OnViewportSizeChanged;
        
        // Initial scale calculation
        UpdateScale();
    }
    

    
    public override void _Draw()
    {
        var size = GetRect().Size;
        var currentColor = BaseColor;
        
        if (_isPressed)
            currentColor = PressedColor;
        else if (_isHovered)
            currentColor = HoverColor;
        
        // Draw the ribbon with gradient
        if (IsLeftSide)
        {
            // Left side ribbon - solid on left, fade on right
            var solidWidth = size.X * 0.55f; // Reduced from 0.7f to make gradient longer
            
            // Draw solid part
            DrawRect(new Rect2(0, 0, solidWidth, size.Y), currentColor);
            
            // Draw gradient part
            var gradientPoints = new Vector2[]
            {
                new Vector2(solidWidth, 0),
                new Vector2(size.X, 0),
                new Vector2(size.X, size.Y),
                new Vector2(solidWidth, size.Y)
            };
            
            var gradientColors = new Color[]
            {
                currentColor,
                new Color(currentColor.R, currentColor.G, currentColor.B, 0),
                new Color(currentColor.R, currentColor.G, currentColor.B, 0),
                currentColor
            };
            
            DrawPolygonWithGradient(gradientPoints, gradientColors);
        }
        else
        {
            // Right side ribbon - fade on left, solid on right
            var fadeWidth = size.X * 0.45f; // Increased from 0.3f to make gradient longer
            var solidStart = fadeWidth;
            
            // Draw gradient part
            var gradientPoints = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(fadeWidth, 0),
                new Vector2(fadeWidth, size.Y),
                new Vector2(0, size.Y)
            };
            
            var gradientColors = new Color[]
            {
                new Color(currentColor.R, currentColor.G, currentColor.B, 0),
                currentColor,
                currentColor,
                new Color(currentColor.R, currentColor.G, currentColor.B, 0)
            };
            
            DrawPolygonWithGradient(gradientPoints, gradientColors);
            
            // Draw solid part
            DrawRect(new Rect2(solidStart, 0, size.X - solidStart, size.Y), currentColor);
        }
        
        // Draw edge lines
        DrawEdgeLines(size, currentColor);
    }
    
    private void DrawPolygonWithGradient(Vector2[] points, Color[] colors)
    {
        // Draw gradient as multiple thin rectangles
        int steps = 50;
        for (int i = 0; i < steps; i++)
        {
            float t = (float)i / steps;
            float tNext = (float)(i + 1) / steps;
            
            var leftX = Mathf.Lerp(points[0].X, points[1].X, t);
            var rightX = Mathf.Lerp(points[0].X, points[1].X, tNext);
            
            var leftColor = colors[0].Lerp(colors[1], t);
            var rightColor = colors[0].Lerp(colors[1], tNext);
            
            // Average the two colors for this strip
            var stripColor = leftColor.Lerp(rightColor, 0.5f);
            
            DrawRect(new Rect2(leftX, 0, rightX - leftX, Size.Y), stripColor);
        }
    }
    
    private void DrawEdgeLines(Vector2 size, Color baseColor)
    {
        // Calculate edge line color - brighter than the base
        var brightEdgeColor = new Color(
            Mathf.Min(baseColor.R + 0.2f, 1.0f),
            Mathf.Min(baseColor.G + 0.2f, 1.0f),
            Mathf.Min(baseColor.B + 0.2f, 1.0f),
            Mathf.Min(baseColor.A + 0.3f, 1.0f)
        );
        
        var scaledEdgeLineThickness = _baseEdgeLineThickness * _currentScale;
        
        if (IsLeftSide)
        {
            // Left side ribbon - full line on top/bottom, gradient on right
            var solidWidth = size.X * 0.55f;
            
            // Top line - solid part
            DrawRect(new Rect2(0, 0, solidWidth, scaledEdgeLineThickness), brightEdgeColor);
            
            // Top line - gradient part
            for (int i = 0; i < 50; i++)
            {
                float t = (float)i / 50;
                var x = solidWidth + (size.X - solidWidth) * t;
                var alpha = 1.0f - t;
                var gradColor = new Color(brightEdgeColor.R, brightEdgeColor.G, brightEdgeColor.B, brightEdgeColor.A * alpha);
                DrawRect(new Rect2(x, 0, (size.X - solidWidth) / 50, scaledEdgeLineThickness), gradColor);
            }
            
            // Bottom line - solid part
            DrawRect(new Rect2(0, size.Y - scaledEdgeLineThickness, solidWidth, scaledEdgeLineThickness), brightEdgeColor);
            
            // Bottom line - gradient part
            for (int i = 0; i < 50; i++)
            {
                float t = (float)i / 50;
                var x = solidWidth + (size.X - solidWidth) * t;
                var alpha = 1.0f - t;
                var gradColor = new Color(brightEdgeColor.R, brightEdgeColor.G, brightEdgeColor.B, brightEdgeColor.A * alpha);
                DrawRect(new Rect2(x, size.Y - scaledEdgeLineThickness, (size.X - solidWidth) / 50, scaledEdgeLineThickness), gradColor);
            }
        }
        else
        {
            // Right side ribbon - gradient on left, full line on right
            var fadeWidth = size.X * 0.45f;
            
            // Top line - gradient part
            for (int i = 0; i < 50; i++)
            {
                float t = (float)i / 50;
                var x = fadeWidth * t;
                var alpha = t;
                var gradColor = new Color(brightEdgeColor.R, brightEdgeColor.G, brightEdgeColor.B, brightEdgeColor.A * alpha);
                DrawRect(new Rect2(x, 0, fadeWidth / 50, scaledEdgeLineThickness), gradColor);
            }
            
            // Top line - solid part
            DrawRect(new Rect2(fadeWidth, 0, size.X - fadeWidth, scaledEdgeLineThickness), brightEdgeColor);
            
            // Bottom line - gradient part
            for (int i = 0; i < 50; i++)
            {
                float t = (float)i / 50;
                var x = fadeWidth * t;
                var alpha = t;
                var gradColor = new Color(brightEdgeColor.R, brightEdgeColor.G, brightEdgeColor.B, brightEdgeColor.A * alpha);
                DrawRect(new Rect2(x, size.Y - scaledEdgeLineThickness, fadeWidth / 50, scaledEdgeLineThickness), gradColor);
            }
            
            // Bottom line - solid part
            DrawRect(new Rect2(fadeWidth, size.Y - scaledEdgeLineThickness, size.X - fadeWidth, scaledEdgeLineThickness), brightEdgeColor);
        }
    }
    
    private void OnGuiInput(InputEvent @event)
    {
        // Handle touch input first for better responsiveness on multi-touch screens
        if (@event is InputEventScreenTouch touchEvent)
        {
            if (touchEvent.Pressed)
            {
                _isPressed = true;
                QueueRedraw();
            }
            else if (_isPressed)
            {
                _isPressed = false;
                QueueRedraw();
                
                // Check if touch is still over the button
                var touchPos = GetLocalMousePosition();
                if (touchPos.X >= 0 && touchPos.X <= Size.X &&
                    touchPos.Y >= 0 && touchPos.Y <= Size.Y)
                {
                    EmitSignal(SignalName.Pressed);
                }
            }
        }
        // Handle mouse input (including synthetic mouse events from touch)
        else if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed)
                {
                    _isPressed = true;
                    QueueRedraw();
                }
                else if (_isPressed)
                {
                    _isPressed = false;
                    QueueRedraw();
                    
                    // Check if mouse is still over the button
                    var mousePos = GetLocalMousePosition();
                    if (mousePos.X >= 0 && mousePos.X <= Size.X &&
                        mousePos.Y >= 0 && mousePos.Y <= Size.Y)
                    {
                        EmitSignal(SignalName.Pressed);
                    }
                }
            }
        }
    }
    
    private void OnMouseEntered()
    {
        _isHovered = true;
        QueueRedraw();
        AnimateText(true);
    }
    
    private void OnMouseExited()
    {
        _isHovered = false;
        _isPressed = false;
        QueueRedraw();
        AnimateText(false);
    }
    
    private void AnimateText(bool towards)
    {
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);
        
        var scaledAnimationOffset = _baseTextAnimationOffset * _currentScale;
        
        Vector2 targetPosition;
        if (towards)
        {
            if (IsLeftSide)
                targetPosition = _originalLabelPosition + new Vector2(scaledAnimationOffset, 0);
            else
                targetPosition = _originalLabelPosition - new Vector2(scaledAnimationOffset, 0);
        }
        else
        {
            targetPosition = _originalLabelPosition;
        }
        
        tween.TweenProperty(_label, "position", targetPosition, 0.2f);
        
        // Also animate text scale slightly
        if (towards)
        {
            tween.Parallel().TweenProperty(_label, "scale", Vector2.One, 0.2f);
        }
        else
        {
            tween.Parallel().TweenProperty(_label, "scale", Vector2.One, 0.2f);
        }
    }
    
    private void UpdateScale()
    {
        var viewportSize = GetViewport().GetVisibleRect().Size;
        
        // Calculate scale based on viewport size vs design resolution
        var scaleX = viewportSize.X / _designResolution.X;
        var scaleY = viewportSize.Y / _designResolution.Y;
        
        // Use the smaller scale to maintain aspect ratio
        _currentScale = Mathf.Min(scaleX, scaleY);
        
        // Update all scaled properties
        UpdateScaledProperties();
    }
    
    private void UpdateScaledProperties()
    {
        // Update minimum size
        CustomMinimumSize = _baseMinimumSize * _currentScale;
        
        // Update label properties
        if (_label != null)
        {
            _label.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(_baseFontSize * _currentScale));
            _label.AddThemeConstantOverride("shadow_offset_x", Mathf.RoundToInt(_baseShadowOffset * _currentScale));
            _label.AddThemeConstantOverride("shadow_offset_y", Mathf.RoundToInt(_baseShadowOffset * _currentScale));
            
            // Update label positioning
            UpdateLabelPosition();
        }
        
        // Force redraw to update edge lines
        QueueRedraw();
    }
    
    private void UpdateLabelPosition()
    {
        if (_label == null) return;
        
        var scaledPadding = _baseLabelPadding * _currentScale;
        
        // Debug output
        GD.Print($"Button Size: {Size}");
        GD.Print($"Label Text: {_label.Text}");
        GD.Print($"Font Size: {_baseFontSize * _currentScale}");
        
        if (IsLeftSide)
        {
            _label.Position = new Vector2(scaledPadding, 0);
            _label.Size = new Vector2(_baseLabelSize.X * _currentScale, Size.Y);
        }
        else
        {
            // For right-aligned text, we need to position it from the right edge
            var labelWidth = _baseLabelSize.X * _currentScale;
            var xPosition = Size.X - labelWidth - scaledPadding;
            _label.Position = new Vector2(xPosition, 0);
            _label.Size = new Vector2(labelWidth, Size.Y);
        }
        
        GD.Print($"Label Position: {_label.Position}");
        GD.Print($"Label Size: {_label.Size}");
        
        _originalLabelPosition = _label.Position;
    }
    
    private void OnViewportSizeChanged()
    {
        UpdateScale();
    }
    
    public override void _ExitTree()
    {
        // Disconnect from viewport size changed signal
        if (GetViewport() != null)
        {
            GetViewport().SizeChanged -= OnViewportSizeChanged;
        }
    }
    
    public override void _Notification(int what)
    {
        if (what == NotificationResized)
        {
            // Update label position when the button is resized
            UpdateLabelPosition();
        }
    }
} 