using Godot;
using InvasiveSpeciesAustralia.UI;
using InvasiveSpeciesAustralia.Systems;
using System.Collections.Generic;

namespace InvasiveSpeciesAustralia.Story;

public partial class StorySelection : BaseUIControl
{
    // UI Elements
    private ScrollContainer _scrollContainer;
    private GridContainer _storyGrid;
    private Button _homeButton;
    
    // Story data
    private List<StoryInfo> _stories;
    
    // Scene paths
    private const string StoryTellingScenePath = "res://scenes/story/StoryTelling.tscn";
    private const string MainMenuScenePath = "res://scenes/MainMenu.tscn";
    
    // Static property to pass selected story between scenes
    public static StoryInfo SelectedStory { get; internal set; }
    
    // Grid settings (base values for 3840x2160; scaled at runtime)
    private const int BaseColumns = 4;
    private const float BaseCardWidth = 820f;
    private const float BaseCardHeight = 680f;
    private const float BaseCardSpacing = 40f;
    
    protected override void OnReady()
    {
        // Get UI references
        _scrollContainer = GetNode<ScrollContainer>("ScrollContainer");
        _storyGrid = GetNode<GridContainer>("ScrollContainer/StoryGrid");
        // Header was wrapped in a MarginContainer; support both old and new paths
        _homeButton = GetNodeOrNull<Button>("HeaderContainer/HomeButton")
                      ?? GetNodeOrNull<Button>("HeaderMargin/HeaderContainer/HomeButton");
        if (_homeButton == null)
        {
            GD.PrintErr("StorySelection: HomeButton not found in scene tree");
        }
        
        // Configure grid (dynamic based on viewport width)
        ApplyResponsiveGrid();
        
        // Connect signals
        if (_homeButton != null)
        {
            _homeButton.Pressed += OnHomePressed;
        }
        
        // Load and display stories
        LoadStories();
    }
    
    private void LoadStories()
    {
        // Get stories from config loader
        _stories = ConfigLoader.Instance.GetStories();
        
        if (_stories == null || _stories.Count == 0)
        {
            GD.PrintErr("StorySelection: No stories loaded from config");
            ShowNoStoriesMessage();
            return;
        }
        
        // Create a card for each story
        foreach (var story in _stories)
        {
            if (story.Visible)
            {
                CreateStoryCard(story);
            }
        }
        // Recalculate layout on resize
        GetViewport().SizeChanged += OnViewportSizeChanged;
    }

    private void OnViewportSizeChanged()
    {
        ApplyResponsiveGrid();
        // Also rescale existing cards
        foreach (var child in _storyGrid.GetChildren())
        {
            if (child is Control c)
            {
                var scale = GetUIScale();
                c.CustomMinimumSize = new Vector2(BaseCardWidth * scale, BaseCardHeight * scale);
            }
        }
    }

    private float GetUIScale()
    {
        var viewport = GetViewport().GetVisibleRect().Size;
        var design = new Vector2(3840, 2160);
        return Mathf.Min(viewport.X / design.X, viewport.Y / design.Y);
    }

    private void ApplyResponsiveGrid()
    {
        var width = GetViewport().GetVisibleRect().Size.X;
        // Determine columns: 4 on wide screens, 3 for mid, 2 for narrow
        int columns = width >= 3600 ? 4 : width >= 2600 ? 3 : 2;
        _storyGrid.Columns = columns;

        var scale = GetUIScale();
        _storyGrid.AddThemeConstantOverride("h_separation", (int)(BaseCardSpacing * scale));
        _storyGrid.AddThemeConstantOverride("v_separation", (int)(BaseCardSpacing * scale));
    }
    
    private void CreateStoryCard(StoryInfo story)
    {
        // Create card container
        var scale = GetUIScale();

        var card = new Control();
        card.CustomMinimumSize = new Vector2(BaseCardWidth * scale, BaseCardHeight * scale);
        
        // Create panel background
        var panel = new Panel();
        panel.AnchorRight = 1.0f;
        panel.AnchorBottom = 1.0f;
        
        // Create custom style for the panel
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0.12f, 0.16f, 0.2f, 0.88f);
        styleBox.BorderColor = new Color(0.9f, 0.9f, 0.9f, 0.55f);
        styleBox.BorderWidthLeft = Mathf.RoundToInt(3 * scale);
        styleBox.BorderWidthTop = Mathf.RoundToInt(3 * scale);
        styleBox.BorderWidthRight = Mathf.RoundToInt(3 * scale);
        styleBox.BorderWidthBottom = Mathf.RoundToInt(3 * scale);
        styleBox.CornerRadiusTopLeft = Mathf.RoundToInt(30 * scale);
        styleBox.CornerRadiusTopRight = Mathf.RoundToInt(30 * scale);
        styleBox.CornerRadiusBottomLeft = Mathf.RoundToInt(30 * scale);
        styleBox.CornerRadiusBottomRight = Mathf.RoundToInt(30 * scale);
        panel.AddThemeStyleboxOverride("panel", styleBox);
        
        card.AddChild(panel);
        
        // Create vertical container for content
        var vbox = new VBoxContainer();
        vbox.AnchorRight = 1.0f;
        vbox.AnchorBottom = 1.0f;
        vbox.AddThemeConstantOverride("separation", Mathf.RoundToInt(16 * scale));
        
        // Add margin
        var marginContainer = new MarginContainer();
        marginContainer.AnchorRight = 1.0f;
        marginContainer.AnchorBottom = 1.0f;
        marginContainer.AddThemeConstantOverride("margin_left", Mathf.RoundToInt(16 * scale));
        marginContainer.AddThemeConstantOverride("margin_top", Mathf.RoundToInt(16 * scale));
        marginContainer.AddThemeConstantOverride("margin_right", Mathf.RoundToInt(16 * scale));
        marginContainer.AddThemeConstantOverride("margin_bottom", Mathf.RoundToInt(14 * scale));
        marginContainer.AddChild(vbox);
        
        card.AddChild(marginContainer);
        
        // Create thumbnail
        // Prefer first generated slide as thumbnail if present
        var userSlide1Path = $"user://stories/{story.Id}/slide-1.png";
        if (FileAccess.FileExists(userSlide1Path))
        {
            var image = new Image();
            var loadErr = image.Load(userSlide1Path);
            Texture2D tex = null;
            if (loadErr == Error.Ok)
            {
                tex = ImageTexture.CreateFromImage(image);
            }
            var thumbnailRect = new TextureRect();
            thumbnailRect.Texture = tex;
            thumbnailRect.ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional;
            thumbnailRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            thumbnailRect.CustomMinimumSize = new Vector2(0, 420 * scale);
            thumbnailRect.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            vbox.AddChild(thumbnailRect);
        }
        // Otherwise try generated thumbnail in user:// if present
        else if (FileAccess.FileExists($"user://stories/{story.Id}/thumbnail.png"))
        {
            var image = new Image();
            var loadErr = image.Load($"user://stories/{story.Id}/thumbnail.png");
            Texture2D tex = null;
            if (loadErr == Error.Ok)
            {
                tex = ImageTexture.CreateFromImage(image);
            }
            var thumbnailRect = new TextureRect();
            thumbnailRect.Texture = tex;
            thumbnailRect.ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional;
            thumbnailRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            thumbnailRect.CustomMinimumSize = new Vector2(0, 420 * scale);
            thumbnailRect.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            vbox.AddChild(thumbnailRect);
        }
        else if (!string.IsNullOrEmpty(story.Thumbnail) && ResourceLoader.Exists(story.Thumbnail))
        {
            var thumbnailRect = new TextureRect();
            thumbnailRect.Texture = GD.Load<Texture2D>(story.Thumbnail);
            thumbnailRect.ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional;
            thumbnailRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            thumbnailRect.CustomMinimumSize = new Vector2(0, 420 * scale);
            thumbnailRect.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            vbox.AddChild(thumbnailRect);
        }
        else
        {
            // Placeholder for missing thumbnail
            var placeholder = new ColorRect();
            placeholder.Color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            placeholder.CustomMinimumSize = new Vector2(0, 420 * scale);
            vbox.AddChild(placeholder);
        }
        
        // Create title
        var titleLabel = new Label();
        titleLabel.Text = story.Title;
        titleLabel.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(54 * scale));
        titleLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        vbox.AddChild(titleLabel);
        
        // Create description
        if (!string.IsNullOrEmpty(story.Description))
        {
            var descLabel = new Label();
            descLabel.Text = story.Description;
            descLabel.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(42 * scale));
            descLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
            descLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            vbox.AddChild(descLabel);
        }
        
        // Create duration label if available
        // No duration with the simplified flow
        
        // Create button overlay
        var button = new Button();
        button.AnchorRight = 1.0f;
        button.AnchorBottom = 1.0f;
        button.Flat = true;
        button.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        button.FocusMode = Control.FocusModeEnum.None;
        card.AddChild(button);
        
        // Connect button press
        button.Pressed += () => OnStorySelected(story);
        
        // Add hover effect
        button.MouseEntered += () =>
        {
            var tween = CreateTween();
            tween.TweenProperty(card, "modulate", new Color(1.1f, 1.1f, 1.1f), 0.2f);
        };
        
        button.MouseExited += () =>
        {
            var tween = CreateTween();
            tween.TweenProperty(card, "modulate", Colors.White, 0.2f);
        };
        
        // Add to grid
        _storyGrid.AddChild(card);
    }
    
    private void ShowNoStoriesMessage()
    {
        var label = new Label();
        label.Text = "No stories available";
        label.AddThemeFontSizeOverride("font_size", 48);
        label.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
        _storyGrid.AddChild(label);
    }
    
    private void OnStorySelected(StoryInfo story)
    {
        GD.Print($"Selected story: {story.Title}");
        
        // Store selected story for the next scene
        SelectedStory = story;
        
        // Transition to story telling scene
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.3f);
        tween.TweenCallback(Callable.From(() => 
        {
            GetTree().ChangeSceneToFile(StoryTellingScenePath);
        }));
    }
    
    private void OnHomePressed()
    {
        GD.Print("Returning to main menu...");
        
        // Transition to main menu
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.3f);
        tween.TweenCallback(Callable.From(() => 
        {
            GetTree().ChangeSceneToFile(MainMenuScenePath);
        }));
    }
    
    protected override void OnExitTree()
    {
        // Clean up signal connections
        if (_homeButton != null)
        {
            _homeButton.Pressed -= OnHomePressed;
        }
    }
} 