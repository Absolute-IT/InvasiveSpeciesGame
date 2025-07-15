using Godot;
using System;
using InvasiveSpeciesAustralia;

public partial class MemoryCard : Control
{
    [Signal]
    public delegate void CardClickedEventHandler(MemoryCard card);
    
    private static readonly string BackImagePath = "res://assets/art/match-game/card-base/back.png";
    
    private Species _species;
    private TextureRect _cardImage;
    
    public bool IsFaceUp { get; private set; }
    public bool IsMatched { get; private set; }
    public string SpeciesId => _species?.Id;
    
    public override void _Ready()
    {
        // Create the card structure
        CustomMinimumSize = new Vector2(200, 263); // Default size (800:1050 ratio)
        
        // Enable GUI input for touch handling
        GuiInput += OnGuiInput;
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        
        // Create texture rect for the card image
        _cardImage = new TextureRect();
        _cardImage.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _cardImage.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        _cardImage.AnchorLeft = 0;
        _cardImage.AnchorTop = 0;
        _cardImage.AnchorRight = 1;
        _cardImage.AnchorBottom = 1;
        _cardImage.OffsetLeft = 0;
        _cardImage.OffsetTop = 0;
        _cardImage.OffsetRight = 0;
        _cardImage.OffsetBottom = 0;
        _cardImage.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_cardImage);
        
        // Load back image by default
        ShowBack();
    }
    
    public void SetSpecies(Species species)
    {
        _species = species;
    }
    
    public void SetCardSize(Vector2 size)
    {
        CustomMinimumSize = size;
        Size = size;
    }
    
    public void ShowFace()
    {
        if (_species == null || IsMatched) return;
        
        IsFaceUp = true;
        var texture = GD.Load<Texture2D>(_species.CardImage);
        _cardImage.Texture = texture;
        
        // Add a subtle scale animation
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Back);
        tween.SetEase(Tween.EaseType.Out);
        Scale = new Vector2(0.9f, 0.9f);
        tween.TweenProperty(this, "scale", Vector2.One, 0.2f);
    }
    
    public void ShowBack()
    {
        if (IsMatched) return;
        
        IsFaceUp = false;
        var texture = GD.Load<Texture2D>(BackImagePath);
        _cardImage.Texture = texture;
        
        // Add a subtle scale animation
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Back);
        tween.SetEase(Tween.EaseType.Out);
        Scale = new Vector2(0.9f, 0.9f);
        tween.TweenProperty(this, "scale", Vector2.One, 0.2f);
    }
    
    public void SetMatched()
    {
        IsMatched = true;
        IsFaceUp = true;
        
        // Add a visual effect for matched cards
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Elastic);
        tween.SetEase(Tween.EaseType.Out);
        Scale = new Vector2(1.1f, 1.1f);
        tween.TweenProperty(this, "scale", Vector2.One, 0.3f);
        
        // Slightly fade the card to indicate it's matched
        tween.Parallel().TweenProperty(this, "modulate:a", 0.8f, 0.3f);
    }
    
    private void OnGuiInput(InputEvent @event)
    {
        // Handle touch input first for better responsiveness on multi-touch screens
        if (@event is InputEventScreenTouch touchEvent)
        {
            if (touchEvent.Pressed && !IsMatched && !IsFaceUp)
            {
                EmitSignal(SignalName.CardClicked, this);
                // Don't consume the event to allow multiple simultaneous touches
            }
        }
        // Handle mouse input (including synthetic mouse events from touch)
        else if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left && !IsMatched && !IsFaceUp)
            {
                EmitSignal(SignalName.CardClicked, this);
                // Don't consume the event to allow multiple simultaneous touches
            }
        }
    }
} 