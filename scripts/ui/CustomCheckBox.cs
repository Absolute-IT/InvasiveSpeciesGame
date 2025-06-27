using Godot;

namespace InvasiveSpeciesAustralia.UI;

/// <summary>
/// Custom checkbox with a large, visible checkmark
/// </summary>
[Tool]
public partial class CustomCheckBox : Control
{
    [Signal]
    public delegate void ToggledEventHandler(bool pressed);
    
    [Export]
    public bool ButtonPressed { get; set; } = false;
    
    [Export]
    public string Text { get; set; } = "";
    
    [Export]
    public int FontSize { get; set; } = 28;
    
    private bool _isHovered = false;
    private StyleBoxFlat _normalStyle;
    private StyleBoxFlat _checkedStyle;
    private StyleBoxFlat _hoverStyle;
    
    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(64, 64);
        MouseFilter = MouseFilterEnum.Stop;
        
        // Create styles
        CreateStyles();
        
        // Connect mouse signals
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }
    
    private void CreateStyles()
    {
        // Normal unchecked style
        _normalStyle = new StyleBoxFlat();
        _normalStyle.BgColor = new Color(0.15f, 0.2f, 0.25f, 0.8f);
        _normalStyle.SetBorderWidthAll(3);
        _normalStyle.BorderColor = new Color(0.3f, 0.4f, 0.5f, 1);
        _normalStyle.SetCornerRadiusAll(8);
        
        // Checked style
        _checkedStyle = new StyleBoxFlat();
        _checkedStyle.BgColor = new Color(0.2f, 0.3f, 0.4f, 1);
        _checkedStyle.SetBorderWidthAll(3);
        _checkedStyle.BorderColor = new Color(0.4f, 0.6f, 0.8f, 1);
        _checkedStyle.SetCornerRadiusAll(8);
        
        // Hover style
        _hoverStyle = new StyleBoxFlat();
        _hoverStyle.BgColor = new Color(0.18f, 0.25f, 0.32f, 0.9f);
        _hoverStyle.SetBorderWidthAll(3);
        _hoverStyle.BorderColor = new Color(0.5f, 0.7f, 0.9f, 1);
        _hoverStyle.SetCornerRadiusAll(8);
    }
    
    public override void _Draw()
    {
        var checkBoxSize = new Vector2(48, 48);
        var checkBoxRect = new Rect2(Vector2.Zero, checkBoxSize);
        
        // Draw checkbox background
        StyleBoxFlat currentStyle;
        if (_isHovered && !ButtonPressed)
            currentStyle = _hoverStyle;
        else if (ButtonPressed)
            currentStyle = _checkedStyle;
        else
            currentStyle = _normalStyle;
            
        DrawStyleBox(currentStyle, checkBoxRect);
        
        // Draw checkmark if checked
        if (ButtonPressed)
        {
            var checkColor = new Color(0.9f, 0.95f, 1, 1);
            var checkThickness = 6.0f;
            
            // Draw checkmark as two lines forming a check
            var padding = 10.0f;
            var p1 = new Vector2(padding, checkBoxSize.Y * 0.5f);
            var p2 = new Vector2(checkBoxSize.X * 0.4f, checkBoxSize.Y - padding);
            var p3 = new Vector2(checkBoxSize.X - padding, padding);
            
            // Draw the check mark
            DrawLine(p1, p2, checkColor, checkThickness);
            DrawLine(p2, p3, checkColor, checkThickness);
            
            // Draw a circle at the joint for smoother appearance
            DrawCircle(p2, checkThickness * 0.5f, checkColor);
        }
        
        // Draw text if present
        if (!string.IsNullOrEmpty(Text))
        {
            var font = ThemeDB.FallbackFont;
            var textPos = new Vector2(checkBoxSize.X + 12, checkBoxSize.Y * 0.5f + FontSize * 0.35f);
            var textColor = _isHovered ? new Color(0.85f, 0.9f, 0.95f, 1) : Colors.White;
            
            DrawString(font, textPos, Text, HorizontalAlignment.Left, -1, FontSize, textColor);
        }
    }
    
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            ButtonPressed = !ButtonPressed;
            EmitSignal(SignalName.Toggled, ButtonPressed);
            QueueRedraw();
        }
    }
    
    private void OnMouseEntered()
    {
        _isHovered = true;
        QueueRedraw();
    }
    
    private void OnMouseExited()
    {
        _isHovered = false;
        QueueRedraw();
    }
    
    public override Vector2 _GetMinimumSize()
    {
        var minSize = new Vector2(48, 48);
        
        if (!string.IsNullOrEmpty(Text))
        {
            var font = ThemeDB.FallbackFont;
            var textSize = font.GetStringSize(Text, HorizontalAlignment.Left, -1, FontSize);
            minSize.X += 12 + textSize.X; // 12px gap between checkbox and text
        }
        
        return minSize;
    }
    
    public override void _ExitTree()
    {
        MouseEntered -= OnMouseEntered;
        MouseExited -= OnMouseExited;
    }
} 