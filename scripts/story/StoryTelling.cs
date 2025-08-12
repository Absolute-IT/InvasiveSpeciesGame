using Godot;
using InvasiveSpeciesAustralia.UI;
using InvasiveSpeciesAustralia.Systems;
using System.Collections.Generic;

namespace InvasiveSpeciesAustralia.Story;

public partial class StoryTelling : BaseUIControl
{
    // UI Elements
    private Control _slideContainer;
    private TextureRect _currentSlide;
    private TextureRect _nextSlide;
    private Button _homeButton;
    private Control _navigationOverlay;
    private Label _tapToContinueLabel;
    
    // Audio elements
    private AudioStreamPlayer _bgMusicPlayer;
    private AudioStreamPlayer _voiceOverPlayer;
    
    // Story data
    private StoryInfo _currentStory;
    private int _currentSlideIndex = 0;
    private bool _isTransitioning = false;
    
    // Timer for auto-advance
    private Timer _autoAdvanceTimer;
    
    // Scene paths
    private const string StorySelectionScenePath = "res://scenes/story/StorySelection.tscn";
    
    // Transition settings
    private const float TransitionDuration = 0.6f;
    private const float FadeOutDuration = 0.25f;
    
    protected override void OnReady()
    {
        // Get the selected story
        _currentStory = StorySelection.SelectedStory;
        
        if (_currentStory == null)
        {
            GD.PrintErr("StoryTelling: No story selected or story has no slides");
            ReturnToSelection();
            return;
        }
        
        // Create UI
        CreateUI();
        
        // Connect input
        SetProcessUnhandledInput(true);
        
        // Load first slide
        var initialSlides = GetGeneratedSlides();
        if (initialSlides.Count == 0)
        {
            if (Systems.StorySlideGenerator.IsStoryGenerating(_currentStory.Id))
            {
                // Show a lightweight loading overlay until slides are ready
                var loading = new Label();
                loading.Text = "Preparing slides...";
                loading.AddThemeFontSizeOverride("font_size", 32);
                AddChild(loading);

                // Poll for readiness with a short timer
                var pollTimer = new Timer();
                pollTimer.WaitTime = 0.5f;
                pollTimer.OneShot = false;
                pollTimer.Timeout += () =>
                {
                    if (Systems.StorySlideGenerator.IsStoryReady(_currentStory.Id))
                    {
                        pollTimer.Stop();
                        pollTimer.QueueFree();
                        loading.QueueFree();
                        LoadSlide(0);
                    }
                };
                AddChild(pollTimer);
                pollTimer.Start();
            }
            else
            {
                GD.PrintErr($"StoryTelling: No generated slides found for story '{_currentStory.Id}'. Returning to selection.");
                ReturnToSelection();
                return;
            }
        }
        else
        {
            LoadSlide(0);
        }
        
        // Start background music if available
        PlayBackgroundMusic();
    }
    
    private void CreateUI()
    {
        // Main slide container
        _slideContainer = new Control();
        _slideContainer.AnchorRight = 1.0f;
        _slideContainer.AnchorBottom = 1.0f;
        _slideContainer.MouseFilter = Control.MouseFilterEnum.Stop; // capture clicks/taps anywhere
        _slideContainer.GuiInput += OnSlideGuiInput;
        AddChild(_slideContainer);
        
        // Current slide display
        _currentSlide = new TextureRect();
        _currentSlide.AnchorRight = 1.0f;
        _currentSlide.AnchorBottom = 1.0f;
        _currentSlide.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _currentSlide.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        _currentSlide.MouseFilter = Control.MouseFilterEnum.Pass;
        _slideContainer.AddChild(_currentSlide);
        
        // Next slide (for transitions)
        _nextSlide = new TextureRect();
        _nextSlide.AnchorRight = 1.0f;
        _nextSlide.AnchorBottom = 1.0f;
        _nextSlide.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _nextSlide.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        _nextSlide.Modulate = new Color(1, 1, 1, 0);
        _nextSlide.Visible = false;
        _nextSlide.MouseFilter = Control.MouseFilterEnum.Pass;
        _slideContainer.AddChild(_nextSlide);
        
        // Navigation overlay (transparent, for small corner elements)
        _navigationOverlay = new Control();
        _navigationOverlay.AnchorRight = 1.0f;
        _navigationOverlay.AnchorBottom = 1.0f;
        _navigationOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_navigationOverlay);

        // Scale helper
        float uiScale = GetUIScale();

        // Top-left cluster: Back button + Title (no bar background)
        var topLeft = new HBoxContainer();
        topLeft.LayoutMode = 0;
        topLeft.AnchorRight = 0;
        topLeft.AnchorBottom = 0;
        topLeft.OffsetLeft = Mathf.RoundToInt(40 * uiScale);
        topLeft.OffsetTop = Mathf.RoundToInt(30 * uiScale);
        _navigationOverlay.AddChild(topLeft);

        _homeButton = new Button();
        _homeButton.Text = "Back";
        _homeButton.CustomMinimumSize = new Vector2(260 * uiScale, 84 * uiScale);
        _homeButton.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(40 * uiScale));
        _homeButton.Pressed += OnHomePressed;
        topLeft.AddChild(_homeButton);

        var titleLabel = new Label();
        titleLabel.Text = _currentStory.Title;
        titleLabel.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(68 * uiScale));
        titleLabel.AddThemeColorOverride("font_color", new Color(0.95f,0.95f,0.95f));
        titleLabel.AddThemeColorOverride("font_shadow_color", new Color(0,0,0,0.7f));
        titleLabel.AddThemeConstantOverride("shadow_offset_x", Mathf.RoundToInt(2 * uiScale));
        titleLabel.AddThemeConstantOverride("shadow_offset_y", Mathf.RoundToInt(2 * uiScale));
        titleLabel.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        titleLabel.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        titleLabel.HorizontalAlignment = HorizontalAlignment.Left;
        titleLabel.VerticalAlignment = VerticalAlignment.Center;
        var titleMargin = new MarginContainer();
        titleMargin.AddThemeConstantOverride("margin_left", Mathf.RoundToInt(20 * uiScale));
        titleMargin.AddChild(titleLabel);
        topLeft.AddChild(titleMargin);

        // Bottom-right: "Tap to continue →" with subtle parallax
        var parallax = new InvasiveSpeciesAustralia.UI.ParallaxTiltEffect
        {
            TiltIntensity = 12.0f,
            SmoothingSpeed = 10.0f,
            ScaleAmount = 0.015f,
            PerspectiveStrength = 0.25f,
            DepthScale = 0.03f
        };
        parallax.AnchorLeft = 1.0f;
        parallax.AnchorTop = 1.0f;
        parallax.AnchorRight = 1.0f;
        parallax.AnchorBottom = 1.0f;
        parallax.OffsetLeft = -Mathf.RoundToInt(700 * uiScale);
        parallax.OffsetTop = -Mathf.RoundToInt(160 * uiScale);
        parallax.OffsetRight = -Mathf.RoundToInt(40 * uiScale);
        parallax.OffsetBottom = -Mathf.RoundToInt(30 * uiScale);
        parallax.MouseFilter = Control.MouseFilterEnum.Stop;
        parallax.GuiInput += (InputEvent e) =>
        {
            if (_isTransitioning) return;
            if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
            {
                NextSlide();
            }
            else if (e is InputEventScreenTouch st && st.Pressed)
            {
                NextSlide();
            }
        };
        _navigationOverlay.AddChild(parallax);

        _tapToContinueLabel = new Label();
        _tapToContinueLabel.Text = "Tap to continue  →";
        _tapToContinueLabel.HorizontalAlignment = HorizontalAlignment.Right;
        _tapToContinueLabel.VerticalAlignment = VerticalAlignment.Center;
        _tapToContinueLabel.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(56 * uiScale));
        _tapToContinueLabel.AddThemeColorOverride("font_color", new Color(0.96f, 0.96f, 0.96f, 0.95f));
        _tapToContinueLabel.AddThemeColorOverride("font_shadow_color", new Color(0,0,0,0.65f));
        _tapToContinueLabel.AddThemeConstantOverride("shadow_offset_x", Mathf.RoundToInt(2 * uiScale));
        _tapToContinueLabel.AddThemeConstantOverride("shadow_offset_y", Mathf.RoundToInt(2 * uiScale));
        parallax.AddChild(_tapToContinueLabel);
        
        // Audio players
        _bgMusicPlayer = new AudioStreamPlayer();
        _bgMusicPlayer.Bus = "Music";
        AddChild(_bgMusicPlayer);
        
        _voiceOverPlayer = new AudioStreamPlayer();
        _voiceOverPlayer.Bus = "SFX";
        AddChild(_voiceOverPlayer);
        
        // Auto-advance timer
        _autoAdvanceTimer = new Timer();
        _autoAdvanceTimer.OneShot = true;
        _autoAdvanceTimer.Timeout += OnAutoAdvanceTimeout;
        AddChild(_autoAdvanceTimer);
    }
    
    public override void _UnhandledInput(InputEvent @event)
    {
        if (_isTransitioning) return;
        
        // Fallback: also handle inputs at the scene level
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left) NextSlide();
        if (@event is InputEventScreenTouch st && st.Pressed) NextSlide();
        
        // Handle keyboard shortcuts
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            switch (keyEvent.Keycode)
            {
                case Key.Right:
                case Key.Space:
                case Key.Enter:
                    NextSlide();
                    break;
                case Key.Escape:
                    OnHomePressed();
                    break;
            }
        }
    }

    private void OnSlideGuiInput(InputEvent @event)
    {
        if (_isTransitioning) return;
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            NextSlide();
        }
        else if (@event is InputEventScreenTouch st && st.Pressed)
        {
            NextSlide();
        }
    }
    
    private void LoadSlide(int index)
    {
        var slides = GetGeneratedSlides();
        if (index < 0 || index >= slides.Count)
            return;
            
        string slidePath = slides[index];
        if (FileAccess.FileExists(slidePath))
        {
            var img = new Image();
            if (img.Load(slidePath) == Error.Ok)
            {
                _currentSlide.Texture = ImageTexture.CreateFromImage(img);
            }
        }
        else
        {
            GD.PrintErr($"StoryTelling: Slide image not found: {slidePath}");
        }
        
        // Update hint visibility on last slide
        bool isLast = index == slides.Count - 1;
        _tapToContinueLabel.Text = isLast ? "Tap to finish  →" : "Tap to continue  →";
        
        // Play voice over if available
        // Voice over not used in simplified flow
        
        // Set up auto-advance if duration is specified
        _autoAdvanceTimer.Stop();
    }
    
    private void TransitionToSlide(int newIndex)
    {
        var slides = GetGeneratedSlides();
        if (_isTransitioning || newIndex < 0 || newIndex >= slides.Count)
            return;
            
        _isTransitioning = true;
        
        var transitionType = "fade";
        
        // Load next slide texture
        var slides2 = slides;
        if (newIndex >= 0 && newIndex < slides2.Count && FileAccess.FileExists(slides2[newIndex]))
        {
            var img = new Image();
            if (img.Load(slides2[newIndex]) == Error.Ok)
            {
                _nextSlide.Texture = ImageTexture.CreateFromImage(img);
            }
        }
        
        // Prepare next slide
        _nextSlide.Visible = true;
        _nextSlide.Modulate = new Color(1, 1, 1, 0);
        
        // Create transition
        var tween = CreateTween();
        tween.SetParallel(true);
        
        switch (transitionType.ToLower())
        {
            case "slide":
                // Slide transition from right
                _nextSlide.Position = new Vector2(GetViewportRect().Size.X, 0);
                _nextSlide.Modulate = Colors.White;
                tween.TweenProperty(_currentSlide, "position:x", -GetViewportRect().Size.X, TransitionDuration);
                tween.TweenProperty(_nextSlide, "position:x", 0, TransitionDuration);
                break;
                
            case "zoom":
                // Zoom transition
                _nextSlide.Scale = new Vector2(0.8f, 0.8f);
                _nextSlide.Position = GetViewportRect().Size * 0.1f;
                tween.TweenProperty(_currentSlide, "modulate:a", 0.0f, TransitionDuration * 0.5f);
                tween.TweenProperty(_nextSlide, "scale", Vector2.One, TransitionDuration);
                tween.TweenProperty(_nextSlide, "position", Vector2.Zero, TransitionDuration);
                tween.TweenProperty(_nextSlide, "modulate:a", 1.0f, TransitionDuration);
                break;
                
            default: // fade
                tween.TweenProperty(_currentSlide, "modulate:a", 0.0f, TransitionDuration);
                tween.TweenProperty(_nextSlide, "modulate:a", 1.0f, TransitionDuration);
                break;
        }
        
        tween.Chain().TweenCallback(Callable.From(() =>
        {
            // Swap slides
            var tempTexture = _currentSlide.Texture;
            _currentSlide.Texture = _nextSlide.Texture;
            _currentSlide.Modulate = Colors.White;
            _currentSlide.Position = Vector2.Zero;
            _currentSlide.Scale = Vector2.One;
            
            _nextSlide.Visible = false;
            _nextSlide.Texture = tempTexture;
            
            _currentSlideIndex = newIndex;
            _isTransitioning = false;
            
            // Update UI for new slide
            LoadSlide(_currentSlideIndex);
        }));
    }
    
    private void NextSlide()
    {
        var slides = GetGeneratedSlides();
        if (_currentSlideIndex < slides.Count - 1)
        {
            TransitionToSlide(_currentSlideIndex + 1);
        }
        else
        {
            // Story finished -> return to selection immediately
            ReturnToSelection();
        }
    }
    
    // Backwards navigation removed per new UX
    
    private void OnAutoAdvanceTimeout()
    {
        NextSlide();
    }
    
    private void PlayBackgroundMusic() { }
    
    private void PlayVoiceOver() { }
    
    private void OnStoryComplete() { }
    
    private void OnHomePressed()
    {
        ReturnToSelection();
    }
    
    private void ReturnToSelection()
    {
        // Stop audio
        _bgMusicPlayer.Stop();
        _voiceOverPlayer.Stop();
        
        // Clear selected story
        StorySelection.SelectedStory = null;
        
        // Transition back to selection
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, FadeOutDuration);
        tween.TweenCallback(Callable.From(() =>
        {
            GetTree().ChangeSceneToFile(StorySelectionScenePath);
        }));
    }

    private List<string> GetGeneratedSlides()
    {
        var list = new List<string>();
        if (_currentStory == null || string.IsNullOrEmpty(_currentStory.Id)) return list;

        var dirPath = $"user://stories/{_currentStory.Id}";
        var dir = DirAccess.Open(dirPath);
        if (dir == null) return list;

        var files = dir.GetFiles();
        foreach (var file in files)
        {
            var name = file.ToString();
            if (name.ToLower().EndsWith(".png") && name.StartsWith("slide-"))
            {
                list.Add($"{dirPath}/{name}");
            }
        }

        list.Sort((a, b) =>
        {
            int GetIndex(string p)
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(p);
                var parts = name.Split('-');
                if (parts.Length >= 2 && int.TryParse(parts[^1], out var n)) return n;
                return 0;
            }
            return GetIndex(a).CompareTo(GetIndex(b));
        });

        return list;
    }
    
    protected override void OnExitTree()
    {
        // Clean up
        if (_homeButton != null) _homeButton.Pressed -= OnHomePressed;
        if (_autoAdvanceTimer != null) _autoAdvanceTimer.Timeout -= OnAutoAdvanceTimeout;
        
        // Stop audio
        _bgMusicPlayer?.Stop();
        _voiceOverPlayer?.Stop();
    }

    private float GetUIScale()
    {
        var viewport = GetViewport().GetVisibleRect().Size;
        var design = new Vector2(3840, 2160);
        return Mathf.Min(viewport.X / design.X, viewport.Y / design.Y);
    }
} 