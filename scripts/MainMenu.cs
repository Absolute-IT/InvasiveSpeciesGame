using Godot;
using InvasiveSpeciesAustralia.UI;
using System.Collections.Generic;

namespace InvasiveSpeciesAustralia;

public partial class MainMenu : BaseUIControl
{
    // Button references
    private RibbonButton _storiesButton;
    private RibbonButton _speciesGuideButton;
    private RibbonButton _bugSquashButton;
    private RibbonButton _memoryMatchButton;
    private RibbonButton _settingsRibbonButton;
    private Button _settingsButton;
    
    // Background rotation
    private TextureRect _backgroundTexture;
    private Timer _backgroundTimer;
    private List<string> _backgroundPaths;
    private int _currentBackgroundIndex = 0;
    private const float BackgroundRotationInterval = 30.0f; // 30 seconds
    
    // Scene paths
    private const string StoriesScenePath = "res://scenes/story/StorySelection.tscn";
    private const string SpeciesGuideScenePath = "res://scenes/gallery/Gallery.tscn";
    private const string BugSquashScenePath = "res://scenes/bug-squash/BugSquash.tscn";
    private const string MemoryMatchScenePath = "res://scenes/memory-match/MemoryMatch.tscn";
    private const string SettingsScenePath = "res://scenes/Settings.tscn";
    
    protected override void OnReady()
    {
        // Initialize configuration loader
        InitializeConfigLoader();
        
        // Add multi-touch debugger for testing (remove this after testing)
        AddMultiTouchDebugger();
        
        // Get button references
        _storiesButton = GetNode<RibbonButton>("RibbonContainer/StoriesButton");
        _speciesGuideButton = GetNode<RibbonButton>("RibbonContainer/SpeciesGuideButton");
        _bugSquashButton = GetNode<RibbonButton>("RibbonContainer/BugSquashButton");
        _memoryMatchButton = GetNode<RibbonButton>("RibbonContainer/MemoryMatchButton");
        _settingsRibbonButton = GetNode<RibbonButton>("RibbonContainer/SettingsRibbonButton");
        _settingsButton = GetNode<Button>("SettingsButton");
        
        // Get background texture reference
        _backgroundTexture = GetNode<TextureRect>("Background");
        
        // Connect button signals
        _storiesButton.Pressed += OnStoriesPressed;
        _speciesGuideButton.Pressed += OnSpeciesGuidePressed;
        _bugSquashButton.Pressed += OnBugSquashPressed;
        _memoryMatchButton.Pressed += OnMemoryMatchPressed;
        _settingsRibbonButton.Pressed += OnSettingsRibbonPressed;
        _settingsButton.Pressed += OnSettingsPressed;
        
        // Set up background rotation
        SetupBackgroundRotation();
    }
    
    private void AddMultiTouchDebugger()
    {
        var debugger = new Systems.MultiTouchDebugger();
        debugger.Name = "MultiTouchDebugger";
        AddChild(debugger);
        GD.Print("Multi-touch debugger added to scene");
    }
    
    private void InitializeConfigLoader()
    {
        // Add ConfigLoader to the scene tree so it persists
        var configLoader = ConfigLoader.Instance;
        if (!IsInstanceValid(configLoader) || !configLoader.IsInsideTree())
        {
            GetTree().Root.CallDeferred("add_child", configLoader);
            
            // Defer loading configs until after ConfigLoader is added to tree
            configLoader.CallDeferred("LoadAllConfigs");
        }
        else
        {
            // ConfigLoader is already in tree, load configs immediately
            configLoader.LoadAllConfigs();
        }
        
        // Optional: Copy default config to user directory for easy editing
        // This is useful for development/testing
        // configLoader.CopyDefaultConfigToUser("species.json");
    }
    
    private void SetupBackgroundRotation()
    {
        // Get background paths from config loader
        _backgroundPaths = ConfigLoader.Instance.GetMenuBackgrounds();
        
        if (_backgroundPaths == null || _backgroundPaths.Count == 0)
        {
            GD.PrintErr("MainMenu: No background paths loaded from config");
            return;
        }
        
        // Load the first background
        LoadBackgroundTexture(0);
        
        // Create and configure timer for background rotation
        _backgroundTimer = new Timer();
        _backgroundTimer.WaitTime = BackgroundRotationInterval;
        _backgroundTimer.Timeout += OnBackgroundTimerTimeout;
        _backgroundTimer.Autostart = true;
        AddChild(_backgroundTimer);
        _backgroundTimer.Start();
    }
    
    private void OnBackgroundTimerTimeout()
    {
        // Move to next background
        _currentBackgroundIndex = (_currentBackgroundIndex + 1) % _backgroundPaths.Count;
        
        // Animate the transition
        AnimateBackgroundTransition(_currentBackgroundIndex);
    }
    
    private void LoadBackgroundTexture(int index)
    {
        if (index < 0 || index >= _backgroundPaths.Count)
            return;
            
        string path = _backgroundPaths[index];
        
        // Check if the texture exists
        if (!ResourceLoader.Exists(path))
        {
            GD.PrintErr($"MainMenu: Background texture not found: {path}");
            return;
        }
        
        // Load the texture
        var texture = GD.Load<Texture2D>(path);
        if (texture != null)
        {
            _backgroundTexture.Texture = texture;
        }
        else
        {
            GD.PrintErr($"MainMenu: Failed to load background texture: {path}");
        }
    }
    
    private void AnimateBackgroundTransition(int newIndex)
    {
        // Create a duplicate TextureRect for the transition
        var newBackground = new TextureRect();
        newBackground.StretchMode = _backgroundTexture.StretchMode;
        newBackground.AnchorRight = 1.0f;
        newBackground.AnchorBottom = 1.0f;
        newBackground.Modulate = new Color(1, 1, 1, 0); // Start transparent
        
        // Load the new texture
        if (newIndex < _backgroundPaths.Count)
        {
            string path = _backgroundPaths[newIndex];
            if (ResourceLoader.Exists(path))
            {
                var texture = GD.Load<Texture2D>(path);
                if (texture != null)
                {
                    newBackground.Texture = texture;
                }
            }
        }
        
        // Add the new background behind the current one
        GetParent().AddChild(newBackground);
        GetParent().MoveChild(newBackground, 0);
        
        // Animate the transition
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(newBackground, "modulate:a", 1.0f, 1.5f);
        tween.TweenProperty(_backgroundTexture, "modulate:a", 0.0f, 1.5f);
        
        tween.Chain().TweenCallback(Callable.From(() => 
        {
            // Update the main background texture
            _backgroundTexture.Texture = newBackground.Texture;
            _backgroundTexture.Modulate = new Color(1, 1, 1, 1);
            
            // Remove the temporary background
            newBackground.QueueFree();
        }));
    }
    
    private void OnStoriesPressed()
    {
        GD.Print("Loading Stories scene...");
        LoadScene(StoriesScenePath);
    }
    
    private void OnSpeciesGuidePressed()
    {
        GD.Print("Loading Species Guide scene...");
        LoadScene(SpeciesGuideScenePath);
    }
    
    private void OnBugSquashPressed()
    {
        GD.Print("Loading Bug Squash game...");
        LoadScene(BugSquashScenePath);
    }
    
    private void OnMemoryMatchPressed()
    {
        GD.Print("Loading Memory Match game...");
        LoadScene(MemoryMatchScenePath);
    }
    
    private void OnSettingsPressed()
    {
        // Toggle visibility of the settings ribbon button
        _settingsRibbonButton.Visible = !_settingsRibbonButton.Visible;
        GD.Print($"Settings button visibility toggled to: {_settingsRibbonButton.Visible}");
    }
    
    private void OnSettingsRibbonPressed()
    {
        GD.Print("Loading Settings scene...");
        LoadScene(SettingsScenePath);
    }
    
    private void LoadScene(string scenePath)
    {
        // Check if scene file exists
        if (!ResourceLoader.Exists(scenePath))
        {
            GD.PrintErr($"Scene not found: {scenePath}");
            return;
        }
        
        // Add fade transition
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.3f);
        tween.TweenCallback(Callable.From(() => 
        {
            GetTree().ChangeSceneToFile(scenePath);
        }));
    }
    
    protected override void OnExitTree()
    {
        // Clean up signal connections
        if (_storiesButton != null) _storiesButton.Pressed -= OnStoriesPressed;
        if (_speciesGuideButton != null) _speciesGuideButton.Pressed -= OnSpeciesGuidePressed;
        if (_bugSquashButton != null) _bugSquashButton.Pressed -= OnBugSquashPressed;
        if (_memoryMatchButton != null) _memoryMatchButton.Pressed -= OnMemoryMatchPressed;
        if (_settingsRibbonButton != null) _settingsRibbonButton.Pressed -= OnSettingsRibbonPressed;
        if (_settingsButton != null) _settingsButton.Pressed -= OnSettingsPressed;
        
        // Clean up background timer
        if (_backgroundTimer != null)
        {
            _backgroundTimer.Stop();
            _backgroundTimer.Timeout -= OnBackgroundTimerTimeout;
            _backgroundTimer.QueueFree();
        }
    }
} 