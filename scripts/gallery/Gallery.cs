using Godot;
using InvasiveSpeciesAustralia.UI;
using InvasiveSpeciesAustralia.Systems;
using System.Collections.Generic;
using System.Linq;

namespace InvasiveSpeciesAustralia;

public partial class Gallery : BaseUIControl
{
    // Grid settings
    private const int ItemSize = 430;
    private const int ItemSpacing = 40;
    private const int ItemsPerRow = 7;
    
    // Containers
    private Control _selectionContainer;
    private Control _detailContainer;
    private GridContainer _animalsGrid;
    private GridContainer _plantsGrid;
    
    // Detail view elements
    private TextureRect _backgroundImage;
    private Control _speciesImageContainer;
    private TextureRect _speciesImage;
    private VBoxContainer _textPanel;
    private Label _speciesName;
    private Label _scientificName;
    private Label _contentText;
    private HBoxContainer _buttonContainer;
    private Button _overviewButton;
    private Button _identificationButton;
    private Button _habitatButton;
    private HBoxContainer _photoContainer;
    private Panel _photo1;
    private Panel _photo2;
    private Panel _photo3;
    private Button _backButton;
    private Button _nextAnimalButton;
    private Button _previousAnimalButton;
    
    // Panel with faded edges
    private PanelContainer _textPanelContainer;
    
    // Navigation
    private Button _homeButton;
    
    // Data
    private List<Species> _animals = new();
    private List<Species> _plants = new();
    private Species _currentSpecies;
    private string _currentView = "overview";
    private int _currentSpeciesIndex = 0;
    private bool _isShowingAnimals = true;
    
    protected override void OnReady()
    {
        // Get container references
        _selectionContainer = GetNode<Control>("SelectionContainer");
        _detailContainer = GetNode<Control>("DetailContainer");
        
        // Selection view
        _animalsGrid = GetNode<GridContainer>("SelectionContainer/ScrollContainer/VBoxContainer/AnimalsGrid");
        _plantsGrid = GetNode<GridContainer>("SelectionContainer/ScrollContainer/VBoxContainer/PlantsGrid");
        
        // Detail view
        _backgroundImage = GetNode<TextureRect>("DetailContainer/BackgroundImage");
        _speciesImageContainer = GetNode<Control>("DetailContainer/SpeciesImageContainer");
        _speciesImage = GetNode<TextureRect>("DetailContainer/SpeciesImageContainer/SpeciesImage");
        _textPanel = GetNode<VBoxContainer>("DetailContainer/TextPanel");
        _textPanelContainer = GetNode<PanelContainer>("DetailContainer/TextPanel/PanelContainer");
        _speciesName = GetNode<Label>("DetailContainer/TextPanel/PanelContainer/VBoxContainer/MarginContainer/InnerVBox/SpeciesName");
        _scientificName = GetNode<Label>("DetailContainer/TextPanel/PanelContainer/VBoxContainer/MarginContainer/InnerVBox/ScientificName");
        _contentText = GetNode<Label>("DetailContainer/TextPanel/PanelContainer/VBoxContainer/MarginContainer/InnerVBox/ScrollContainer/ContentText");
        _buttonContainer = GetNode<HBoxContainer>("DetailContainer/TextPanel/ButtonMarginContainer/ButtonContainer");
        _overviewButton = GetNode<Button>("DetailContainer/TextPanel/ButtonMarginContainer/ButtonContainer/OverviewButton");
        _identificationButton = GetNode<Button>("DetailContainer/TextPanel/ButtonMarginContainer/ButtonContainer/IdentificationButton");
        _habitatButton = GetNode<Button>("DetailContainer/TextPanel/ButtonMarginContainer/ButtonContainer/HabitatButton");
        _photoContainer = GetNode<HBoxContainer>("DetailContainer/TextPanel/PhotoMarginContainer/PhotoContainer");
        _photo1 = GetNode<Panel>("DetailContainer/TextPanel/PhotoMarginContainer/PhotoContainer/Photo1");
        _photo2 = GetNode<Panel>("DetailContainer/TextPanel/PhotoMarginContainer/PhotoContainer/Photo2");
        _photo3 = GetNode<Panel>("DetailContainer/TextPanel/PhotoMarginContainer/PhotoContainer/Photo3");
        _backButton = GetNode<Button>("DetailContainer/BackButton");
        _nextAnimalButton = GetNode<Button>("DetailContainer/SpeciesImageContainer/MarginContainer/NavigationContainer/NextAnimalButton");
        _previousAnimalButton = GetNode<Button>("DetailContainer/SpeciesImageContainer/MarginContainer/NavigationContainer/PreviousAnimalButton");
        
        // Navigation
        _homeButton = GetNode<Button>("HomeButton");
        
        // Connect signals
        _homeButton.Pressed += OnHomePressed;
        _backButton.Pressed += OnBackPressed;
        _overviewButton.Pressed += () => ShowContent("overview");
        _identificationButton.Pressed += () => ShowContent("identification");
        _habitatButton.Pressed += () => ShowContent("habitat");
        _nextAnimalButton.Pressed += OnNextPressed;
        _previousAnimalButton.Pressed += OnPreviousPressed;
        
        // Load species data
        LoadSpeciesData();
        
        // Setup grids
        SetupGrids();
        
        // Apply edge fade effect to text panel
        SetupTextPanelFadeEffect();
        
        // Style all buttons
        StyleButtons();
        
        // Show selection view initially
        ShowSelectionView();
        
        // Add multi-touch debugger
        AddMultiTouchDebugger();
    }
    
    private void AddMultiTouchDebugger()
    {
        var debugger = new Systems.MultiTouchDebugger();
        debugger.Name = "MultiTouchDebugger";
        AddChild(debugger);
    }
    
    private void LoadSpeciesData()
    {
        var allSpecies = ConfigLoader.Instance.GetAllSpecies();
        
        _animals = allSpecies.Where(s => s.Type == "animals").ToList();
        _plants = allSpecies.Where(s => s.Type == "plants").ToList();
    }
    
    private void SetupGrids()
    {
        // Configure grid columns
        _animalsGrid.Columns = ItemsPerRow;
        _plantsGrid.Columns = ItemsPerRow;
        
        // Add spacing
        _animalsGrid.AddThemeConstantOverride("h_separation", ItemSpacing);
        _animalsGrid.AddThemeConstantOverride("v_separation", ItemSpacing);
        _plantsGrid.AddThemeConstantOverride("h_separation", ItemSpacing);
        _plantsGrid.AddThemeConstantOverride("v_separation", ItemSpacing);
        
        // Create animal items
        for (int i = 0; i < _animals.Count; i++)
        {
            var item = CreateSpeciesItem(_animals[i], i, true);
            _animalsGrid.AddChild(item);
        }
        
        // Create plant items
        for (int i = 0; i < _plants.Count; i++)
        {
            var item = CreateSpeciesItem(_plants[i], i, false);
            _plantsGrid.AddChild(item);
        }
    }
    
    private Control CreateSpeciesItem(Species species, int index, bool isAnimal)
    {
        var container = new PanelContainer();
        container.CustomMinimumSize = new Vector2(ItemSize, ItemSize);
        container.MouseFilter = Control.MouseFilterEnum.Pass;
        
        // Create style box with border
        var styleBox = new StyleBoxFlat();
        styleBox.BorderWidthTop = 4;
        styleBox.BorderWidthBottom = 4;
        styleBox.BorderWidthLeft = 4;
        styleBox.BorderWidthRight = 4;
        styleBox.BorderColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        styleBox.BgColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        styleBox.CornerRadiusTopLeft = 20;
        styleBox.CornerRadiusTopRight = 20;
        styleBox.CornerRadiusBottomLeft = 20;
        styleBox.CornerRadiusBottomRight = 20;
        container.AddThemeStyleboxOverride("panel", styleBox);
        
        // Create hover style
        var hoverStyle = (StyleBoxFlat)styleBox.Duplicate();
        hoverStyle.BorderColor = new Color(0.8f, 0.8f, 0.8f, 1.0f);
        hoverStyle.BgColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        // Add species image
        var imageRect = new TextureRect();
        imageRect.ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional;
        imageRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        imageRect.MouseFilter = Control.MouseFilterEnum.Ignore;
        
        // Load texture
        if (!string.IsNullOrEmpty(species.Image))
        {
            var texture = GD.Load<Texture2D>(species.Image);
            if (texture != null)
            {
                imageRect.Texture = texture;
            }
        }
        
        container.AddChild(imageRect);
        
        // Add name label at bottom
        var nameLabel = new Label();
        nameLabel.Text = species.Name;
        nameLabel.AddThemeStyleboxOverride("normal", new StyleBoxFlat() 
        { 
            BgColor = new Color(0, 0, 0, 0.7f),
            ContentMarginTop = 10,
            ContentMarginBottom = 10,
            ContentMarginLeft = 20,
            ContentMarginRight = 20
        });
        nameLabel.AddThemeFontSizeOverride("font_size", 32);
        nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        nameLabel.VerticalAlignment = VerticalAlignment.Bottom;
        nameLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        nameLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        container.AddChild(nameLabel);
        
        // Handle mouse events
        container.MouseEntered += () =>
        {
            container.AddThemeStyleboxOverride("panel", hoverStyle);
            var tween = CreateTween();
            tween.TweenProperty(container, "scale", Vector2.One * 1.05f, 0.1f);
        };
        
        container.MouseExited += () =>
        {
            container.AddThemeStyleboxOverride("panel", styleBox);
            var tween = CreateTween();
            tween.TweenProperty(container, "scale", Vector2.One, 0.1f);
        };
        
        container.GuiInput += (InputEvent @event) =>
        {
            // Handle touch input first for better responsiveness on multi-touch screens
            if (@event is InputEventScreenTouch touchEvent && touchEvent.Pressed)
            {
                ShowSpeciesDetail(species, index, isAnimal);
            }
            // Handle mouse input (including synthetic mouse events from touch)
            else if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                ShowSpeciesDetail(species, index, isAnimal);
            }
        };
        
        return container;
    }
    
    private void ShowSpeciesDetail(Species species, int index, bool isAnimal)
    {
        _currentSpecies = species;
        _currentSpeciesIndex = index;
        _isShowingAnimals = isAnimal;
        
        // Load background
        if (!string.IsNullOrEmpty(species.EnvironmentImage))
        {
            var bgTexture = GD.Load<Texture2D>(species.EnvironmentImage);
            if (bgTexture != null)
            {
                _backgroundImage.Texture = bgTexture;
            }
        }
        
        // Load species image
        if (!string.IsNullOrEmpty(species.Image))
        {
            var speciesTexture = GD.Load<Texture2D>(species.Image);
            if (speciesTexture != null)
            {
                _speciesImage.Texture = speciesTexture;
            }
        }
        
        // Set text
        _speciesName.Text = species.Name;
        _scientificName.Text = species.ScientificName;
        
        // Load identification images
        LoadIdentificationImages(species);
        
        // Position elements based on even/odd index
        bool isLeftLayout = index % 2 == 0;
        
        if (isLeftLayout)
        {
            // Species image on left, text on right
            _speciesImageContainer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.LeftWide);
            _speciesImageContainer.OffsetLeft = 80;
            _speciesImageContainer.OffsetRight = 1580; // Half screen minus gap - increased by 10px
            _speciesImageContainer.OffsetTop = 90; // Reduced by 40px
            _speciesImageContainer.OffsetBottom = -140; // Increased by 10px
            _textPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.RightWide);
            _textPanel.OffsetLeft = -2230; // From right edge - increased by 10px
            _textPanel.OffsetRight = -100;
            _textPanel.OffsetTop = 110; // Reduced by 40px
            _textPanel.OffsetBottom = -140; // Increased by 10px
        }
        else
        {
            // Species image on right, text on left
            _speciesImageContainer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.RightWide);
            _speciesImageContainer.OffsetLeft = -1600; // From right edge - increased by 10px
            _speciesImageContainer.OffsetRight = -80;
            _speciesImageContainer.OffsetTop = 90; // Reduced by 40px
            _speciesImageContainer.OffsetBottom = -140; // Increased by 10px
            _textPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.LeftWide);
            _textPanel.OffsetLeft = 100;
            _textPanel.OffsetRight = 2230; // Increased by 10px
            _textPanel.OffsetTop = 110; // Reduced by 40px
            _textPanel.OffsetBottom = -140; // Increased by 10px
        }
        
        // Update navigation buttons text
        _nextAnimalButton.Text = "Next →";
        _previousAnimalButton.Text = "← Previous";
        
        // Update navigation button visibility
        var list = isAnimal ? _animals : _plants;
        _previousAnimalButton.Visible = index > 0;
        _nextAnimalButton.Visible = index < list.Count - 1;
        
        // Show overview by default
        ShowContent("overview");
        
        // Transition to detail view
        ShowDetailView();
    }
    
    private void ShowContent(string contentType)
    {
        _currentView = contentType;
        
        // Update button states - adjust opacity instead of modulating
        var buttons = new[] 
        {
            (_overviewButton, "overview"),
            (_identificationButton, "identification"),
            (_habitatButton, "habitat")
        };
        
        foreach (var (button, type) in buttons)
        {
            if (button != null)
            {
                // Get the current style and create a copy
                var style = button.GetThemeStylebox("normal") as StyleBoxFlat;
                if (style != null)
                {
                    var activeStyle = (StyleBoxFlat)style.Duplicate();
                    
                    // Active button is more opaque, inactive is less
                    if (type == contentType)
                    {
                        activeStyle.BgColor = new Color(0, 0, 0, 0.8f); // More opaque for active
                        activeStyle.BorderColor = new Color(1, 1, 1, 0.6f); // Brighter border
                    }
                    else
                    {
                        activeStyle.BgColor = new Color(0, 0, 0, 0.4f); // Less opaque for inactive
                        activeStyle.BorderColor = new Color(1, 1, 1, 0.2f); // Dimmer border
                    }
                    
                    button.AddThemeStyleboxOverride("normal", activeStyle);
                }
            }
        }
        
        // Fade out current content
        var fadeOut = CreateTween();
        fadeOut.TweenProperty(_contentText, "modulate:a", 0.0f, 0.2f);
        fadeOut.TweenCallback(Callable.From(() =>
        {
            // Update content based on type
            switch (contentType)
            {
                case "overview":
                    var overviewText = _currentSpecies.History;
                    if (!string.IsNullOrEmpty(_currentSpecies.Diet))
                    {
                        overviewText += "\n\n" + _currentSpecies.Diet;
                    }
                    _contentText.Text = overviewText;
                    break;
                    
                case "identification":
                    if (_currentSpecies.Identification != null && _currentSpecies.Identification.Count > 0)
                    {
                        _contentText.Text = "• " + string.Join("\n• ", _currentSpecies.Identification);
                    }
                    else
                    {
                        _contentText.Text = "No identification information available.";
                    }
                    break;
                    
                case "habitat":
                    _contentText.Text = _currentSpecies.Habitat;
                    break;
            }
            
            // Fade in new content
            var fadeIn = CreateTween();
            fadeIn.TweenProperty(_contentText, "modulate:a", 1.0f, 0.2f);
        }));
    }
    
    private void LoadIdentificationImages(Species species)
    {
        GD.Print($"Loading identification images for {species.Name}");
        
        // Clear existing images from panels
        foreach (var child in _photo1.GetChildren())
        {
            child.QueueFree();
        }
        foreach (var child in _photo2.GetChildren())
        {
            child.QueueFree();
        }
        foreach (var child in _photo3.GetChildren())
        {
            child.QueueFree();
        }
        
        // Get the photo panels
        var panels = new[] { _photo1, _photo2, _photo3 };
        
        // Reset panel styles to default (with background)
        var defaultStyle = new StyleBoxFlat();
        defaultStyle.BgColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        defaultStyle.BorderWidthLeft = 2;
        defaultStyle.BorderWidthTop = 2;
        defaultStyle.BorderWidthRight = 2;
        defaultStyle.BorderWidthBottom = 2;
        defaultStyle.BorderColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        defaultStyle.CornerRadiusTopLeft = 10;
        defaultStyle.CornerRadiusTopRight = 10;
        defaultStyle.CornerRadiusBottomLeft = 10;
        defaultStyle.CornerRadiusBottomRight = 10;
        
        foreach (var panel in panels)
        {
            panel.AddThemeStyleboxOverride("panel", defaultStyle);
        }
        
        // Load and display identification images
        if (species.IdentificationImages != null)
        {
            GD.Print($"Found {species.IdentificationImages.Count} identification images");
            for (int i = 0; i < species.IdentificationImages.Count && i < 3; i++)
            {
                var imagePath = species.IdentificationImages[i];
                GD.Print($"Loading image {i}: {imagePath}");
                if (!string.IsNullOrEmpty(imagePath))
                {
                    var texture = GD.Load<Texture2D>(imagePath);
                    if (texture != null)
                    {
                        GD.Print($"Successfully loaded texture for image {i}");
                        
                        // Create transparent style for panels with images
                        var transparentStyle = new StyleBoxFlat();
                        transparentStyle.BgColor = new Color(0, 0, 0, 0);
                        transparentStyle.BorderWidthLeft = 2;
                        transparentStyle.BorderWidthTop = 2;
                        transparentStyle.BorderWidthRight = 2;
                        transparentStyle.BorderWidthBottom = 2;
                        transparentStyle.BorderColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
                        transparentStyle.CornerRadiusTopLeft = 10;
                        transparentStyle.CornerRadiusTopRight = 10;
                        transparentStyle.CornerRadiusBottomLeft = 10;
                        transparentStyle.CornerRadiusBottomRight = 10;
                        panels[i].AddThemeStyleboxOverride("panel", transparentStyle);
                        
                        // Create a TextureRect to display the image
                        var imageRect = new TextureRect();
                        imageRect.Texture = texture;
                        imageRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                        imageRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
                        imageRect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
                        
                        // Add to the corresponding panel
                        panels[i].AddChild(imageRect);
                        GD.Print($"Added image to panel {i}");
                    }
                    else
                    {
                        GD.PrintErr($"Failed to load texture from path: {imagePath}");
                    }
                }
            }
        }
        else
        {
            GD.Print("No identification images found for this species");
        }
    }
    
    private void ShowSelectionView()
    {
        _selectionContainer.Visible = true;
        _detailContainer.Visible = false;
    }
    
    private void ShowDetailView()
    {
        // Animate transition
        var tween = CreateTween();
        tween.TweenProperty(_selectionContainer, "modulate:a", 0.0f, 0.3f);
        tween.TweenCallback(Callable.From(() =>
        {
            _selectionContainer.Visible = false;
            _detailContainer.Visible = true;
            _detailContainer.Modulate = new Color(1, 1, 1, 0);
        }));
        tween.TweenProperty(_detailContainer, "modulate:a", 1.0f, 0.3f);
    }
    
    private void OnBackPressed()
    {
        // Animate transition back to selection
        var tween = CreateTween();
        tween.TweenProperty(_detailContainer, "modulate:a", 0.0f, 0.3f);
        tween.TweenCallback(Callable.From(() =>
        {
            _detailContainer.Visible = false;
            _selectionContainer.Visible = true;
            _selectionContainer.Modulate = new Color(1, 1, 1, 0);
        }));
        tween.TweenProperty(_selectionContainer, "modulate:a", 1.0f, 0.3f);
    }
    
    private void OnNextPressed()
    {
        var list = _isShowingAnimals ? _animals : _plants;
        if (_currentSpeciesIndex < list.Count - 1)
        {
            ShowSpeciesDetail(list[_currentSpeciesIndex + 1], _currentSpeciesIndex + 1, _isShowingAnimals);
        }
    }
    
    private void OnPreviousPressed()
    {
        if (_currentSpeciesIndex > 0)
        {
            var list = _isShowingAnimals ? _animals : _plants;
            ShowSpeciesDetail(list[_currentSpeciesIndex - 1], _currentSpeciesIndex - 1, _isShowingAnimals);
        }
    }
    
    private void OnHomePressed()
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.3f);
        tween.TweenCallback(Callable.From(() =>
        {
            GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
        }));
    }
    
    private void SetupTextPanelFadeEffect()
    {
        // Make the existing PanelContainer transparent
        var transparentStyle = new StyleBoxFlat();
        transparentStyle.BgColor = new Color(0, 0, 0, 0); // Fully transparent
        transparentStyle.BorderWidthTop = 0;
        transparentStyle.BorderWidthBottom = 0;
        transparentStyle.BorderWidthLeft = 0;
        transparentStyle.BorderWidthRight = 0;
        _textPanelContainer.AddThemeStyleboxOverride("panel", transparentStyle);
        
        // Increase padding on the text content
        var marginContainer = GetNode<MarginContainer>("DetailContainer/TextPanel/PanelContainer/VBoxContainer/MarginContainer");
        if (marginContainer != null)
        {
            marginContainer.AddThemeConstantOverride("margin_left", 130);
            marginContainer.AddThemeConstantOverride("margin_top", 80);
            marginContainer.AddThemeConstantOverride("margin_right", 130);
            marginContainer.AddThemeConstantOverride("margin_bottom", 80);
        }
        
        // Create a ColorRect to apply the shader effect
        var fadeBackground = new ColorRect();
        fadeBackground.Name = "FadeBackground";
        fadeBackground.MouseFilter = Control.MouseFilterEnum.Ignore;
        
        // Make it fill the panel container
        fadeBackground.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        
        // Load and apply the edge fade shader
        var shader = GD.Load<Shader>("res://shaders/edge_fade.gdshader");
        if (shader != null)
        {
            // Create shader material
            var shaderMaterial = new ShaderMaterial();
            shaderMaterial.Shader = shader;
            
            // Configure shader parameters
            shaderMaterial.SetShaderParameter("fade_size", 0.05f);
            shaderMaterial.SetShaderParameter("fade_power", 2.5f);
            shaderMaterial.SetShaderParameter("background_color", new Color(0.05f, 0.05f, 0.05f, 0.85f));
            
            // Apply to the ColorRect
            fadeBackground.Material = shaderMaterial;
            
            // Insert as first child of PanelContainer so it's behind the content
            _textPanelContainer.AddChild(fadeBackground);
            _textPanelContainer.MoveChild(fadeBackground, 0);
        }
    }
    
    private void StyleButtons()
    {
        // Create a semi-transparent black style for buttons
        var buttonStyle = new StyleBoxFlat();
        buttonStyle.BgColor = new Color(0, 0, 0, 0.7f); // Semi-transparent black
        buttonStyle.BorderWidthTop = 2;
        buttonStyle.BorderWidthBottom = 2;
        buttonStyle.BorderWidthLeft = 2;
        buttonStyle.BorderWidthRight = 2;
        buttonStyle.BorderColor = new Color(1, 1, 1, 0.3f); // Subtle white border
        buttonStyle.CornerRadiusTopLeft = 10;
        buttonStyle.CornerRadiusTopRight = 10;
        buttonStyle.CornerRadiusBottomLeft = 10;
        buttonStyle.CornerRadiusBottomRight = 10;
        buttonStyle.ContentMarginLeft = 60;
        buttonStyle.ContentMarginRight = 60;
        buttonStyle.ContentMarginTop = 40;
        buttonStyle.ContentMarginBottom = 40;
        
        // Create hover style
        var hoverStyle = (StyleBoxFlat)buttonStyle.Duplicate();
        hoverStyle.BgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Slightly lighter on hover
        hoverStyle.BorderColor = new Color(1, 1, 1, 0.5f);
        
        // Create pressed style
        var pressedStyle = (StyleBoxFlat)buttonStyle.Duplicate();
        pressedStyle.BgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        // Apply to content buttons (Overview, Identification, Habitat)
        var contentButtons = new Button[] 
        {
            _overviewButton,
            _identificationButton,
            _habitatButton
        };
        
        foreach (var button in contentButtons)
        {
            if (button != null)
            {
                // Apply styles
                button.AddThemeStyleboxOverride("normal", buttonStyle);
                button.AddThemeStyleboxOverride("hover", hoverStyle);
                button.AddThemeStyleboxOverride("pressed", pressedStyle);
                button.AddThemeStyleboxOverride("focus", buttonStyle);
                
                // Set text color to white
                button.AddThemeColorOverride("font_color", Colors.White);
                button.AddThemeColorOverride("font_hover_color", Colors.White);
                button.AddThemeColorOverride("font_pressed_color", Colors.White);
                button.AddThemeColorOverride("font_focus_color", Colors.White);
                
                // Increase font size significantly
                button.AddThemeFontSizeOverride("font_size", 48);
            }
        }
        
        // Style for navigation buttons (wider)
        var navStyle = (StyleBoxFlat)buttonStyle.Duplicate();
        navStyle.ContentMarginLeft = 100; // Much wider
        navStyle.ContentMarginRight = 100;
        
        var navHoverStyle = (StyleBoxFlat)navStyle.Duplicate();
        navHoverStyle.BgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        navHoverStyle.BorderColor = new Color(1, 1, 1, 0.5f);
        
        var navPressedStyle = (StyleBoxFlat)navStyle.Duplicate();
        navPressedStyle.BgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        // Apply to navigation buttons
        var navButtons = new Button[] { _nextAnimalButton, _previousAnimalButton };
        foreach (var button in navButtons)
        {
            if (button != null)
            {
                button.AddThemeStyleboxOverride("normal", navStyle);
                button.AddThemeStyleboxOverride("hover", navHoverStyle);
                button.AddThemeStyleboxOverride("pressed", navPressedStyle);
                button.AddThemeStyleboxOverride("focus", navStyle);
                
                button.AddThemeColorOverride("font_color", Colors.White);
                button.AddThemeColorOverride("font_hover_color", Colors.White);
                button.AddThemeColorOverride("font_pressed_color", Colors.White);
                button.AddThemeColorOverride("font_focus_color", Colors.White);
                
                button.AddThemeFontSizeOverride("font_size", 42);
            }
        }
        
        // Style for back and home buttons (shorter)
        var utilityStyle = (StyleBoxFlat)buttonStyle.Duplicate();
        utilityStyle.ContentMarginTop = 20; // Reduced height
        utilityStyle.ContentMarginBottom = 20;
        
        var utilityHoverStyle = (StyleBoxFlat)utilityStyle.Duplicate();
        utilityHoverStyle.BgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        utilityHoverStyle.BorderColor = new Color(1, 1, 1, 0.5f);
        
        var utilityPressedStyle = (StyleBoxFlat)utilityStyle.Duplicate();
        utilityPressedStyle.BgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        // Apply to back and home buttons
        var utilityButtons = new Button[] { _backButton, _homeButton };
        foreach (var button in utilityButtons)
        {
            if (button != null)
            {
                button.AddThemeStyleboxOverride("normal", utilityStyle);
                button.AddThemeStyleboxOverride("hover", utilityHoverStyle);
                button.AddThemeStyleboxOverride("pressed", utilityPressedStyle);
                button.AddThemeStyleboxOverride("focus", utilityStyle);
                
                button.AddThemeColorOverride("font_color", Colors.White);
                button.AddThemeColorOverride("font_hover_color", Colors.White);
                button.AddThemeColorOverride("font_pressed_color", Colors.White);
                button.AddThemeColorOverride("font_focus_color", Colors.White);
                
                button.AddThemeFontSizeOverride("font_size", 48);
            }
        }
        
        // Extra large font for the home button
        if (_homeButton != null)
        {
            _homeButton.AddThemeFontSizeOverride("font_size", 52);
        }
    }
    
    protected override void OnExitTree()
    {
        // Clean up signal connections
        if (_homeButton != null) _homeButton.Pressed -= OnHomePressed;
        if (_backButton != null) _backButton.Pressed -= OnBackPressed;
        if (_nextAnimalButton != null) _nextAnimalButton.Pressed -= OnNextPressed;
        if (_previousAnimalButton != null) _previousAnimalButton.Pressed -= OnPreviousPressed;
    }
} 