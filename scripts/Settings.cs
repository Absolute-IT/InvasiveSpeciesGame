using Godot;
using InvasiveSpeciesAustralia.Systems;
using InvasiveSpeciesAustralia.UI;

namespace InvasiveSpeciesAustralia;

public partial class Settings : Control
{
    private Button _backButton;
    private Button _applyButton;
    
    // Display controls
    private OptionButton _resolutionOption;
    private OptionButton _scaleOption;
    private CustomCheckBox _fullscreenCheckBox;
    
    // Audio controls
    private HSlider _masterVolumeSlider;
    private Label _masterVolumeLabel;
    
    private HSlider _musicVolumeSlider;
    private Label _musicVolumeLabel;
    private CustomCheckBox _musicEnabledCheckBox;
    
    private HSlider _sfxVolumeSlider;
    private Label _sfxVolumeLabel;
    private CustomCheckBox _sfxEnabledCheckBox;
    
    // Pending settings (not applied until Apply button is pressed)
    private Vector2I _pendingResolution;
    private float _pendingScale;
    private bool _pendingFullscreen;
    
    // Track if any changes have been made
    private bool _hasChanges = false;
    
    public override void _Ready()
    {
        // Get references to controls
        _backButton = GetNode<Button>("Panel/ButtonContainer/BackButton");
        _applyButton = GetNode<Button>("Panel/ButtonContainer/ApplyButton");
        
        // Display controls
        _resolutionOption = GetNode<OptionButton>("Panel/ContentContainer/VBoxContainer/DisplaySection/SettingsGrid/ResolutionOption");
        _scaleOption = GetNode<OptionButton>("Panel/ContentContainer/VBoxContainer/DisplaySection/SettingsGrid/ScaleOption");
        _fullscreenCheckBox = GetNode<CustomCheckBox>("Panel/ContentContainer/VBoxContainer/DisplaySection/SettingsGrid/FullscreenContainer/FullscreenCheckBox");
        
        // Audio controls
        _masterVolumeSlider = GetNode<HSlider>("Panel/ContentContainer/VBoxContainer/AudioSection/MasterVolumeContainer/MasterVolumeRow/MasterVolumeSlider");
        _masterVolumeLabel = GetNode<Label>("Panel/ContentContainer/VBoxContainer/AudioSection/MasterVolumeContainer/MasterVolumeRow/MasterVolumeValue");
        
        _musicVolumeSlider = GetNode<HSlider>("Panel/ContentContainer/VBoxContainer/AudioSection/MusicContainer/MusicRow/MusicVolumeSlider");
        _musicVolumeLabel = GetNode<Label>("Panel/ContentContainer/VBoxContainer/AudioSection/MusicContainer/MusicRow/MusicVolumeValue");
        _musicEnabledCheckBox = GetNode<CustomCheckBox>("Panel/ContentContainer/VBoxContainer/AudioSection/MusicContainer/MusicRow/MusicEnabledCheckBox");
        
        _sfxVolumeSlider = GetNode<HSlider>("Panel/ContentContainer/VBoxContainer/AudioSection/SfxContainer/SfxRow/SfxVolumeSlider");
        _sfxVolumeLabel = GetNode<Label>("Panel/ContentContainer/VBoxContainer/AudioSection/SfxContainer/SfxRow/SfxVolumeValue");
        _sfxEnabledCheckBox = GetNode<CustomCheckBox>("Panel/ContentContainer/VBoxContainer/AudioSection/SfxContainer/SfxRow/SfxEnabledCheckBox");
        
        // Connect signals first
        ConnectSignals();
        
        // Defer population to ensure SettingsManager is ready
        CallDeferred(nameof(InitializeSettings));
    }
    
    private void InitializeSettings()
    {
        // Populate option buttons
        PopulateResolutionOptions();
        PopulateScaleOptions();
        
        // Load current settings
        LoadCurrentSettings();
    }
    
    private void PopulateResolutionOptions()
    {
        _resolutionOption.Clear();
        foreach (var resolution in SettingsManager.AvailableResolutions)
        {
            _resolutionOption.AddItem($"{resolution.X} × {resolution.Y}");
        }
    }
    
    private void PopulateScaleOptions()
    {
        _scaleOption.Clear();
        foreach (var scale in SettingsManager.ScaleFactors)
        {
            _scaleOption.AddItem($"{Mathf.RoundToInt(scale * 100)}%");
        }
    }
    
    private void LoadCurrentSettings()
    {
        var settings = SettingsManager.Instance;
        
        // Display settings
        _pendingResolution = settings.CurrentResolution;
        _pendingScale = settings.CurrentScale;
        _pendingFullscreen = settings.IsFullscreen;
        
        _resolutionOption.Selected = settings.GetResolutionIndex(settings.CurrentResolution);
        _scaleOption.Selected = settings.GetScaleIndex(settings.CurrentScale);
        _fullscreenCheckBox.ButtonPressed = settings.IsFullscreen;
        
        // Audio settings
        _masterVolumeSlider.Value = settings.MasterVolume;
        _masterVolumeLabel.Text = $"{Mathf.RoundToInt(settings.MasterVolume * 100)}%";
        
        _musicVolumeSlider.Value = settings.MusicVolume;
        _musicVolumeLabel.Text = $"{Mathf.RoundToInt(settings.MusicVolume * 100)}%";
        _musicEnabledCheckBox.ButtonPressed = settings.MusicEnabled;
        _musicVolumeSlider.Editable = settings.MusicEnabled;
        
        _sfxVolumeSlider.Value = settings.SfxVolume;
        _sfxVolumeLabel.Text = $"{Mathf.RoundToInt(settings.SfxVolume * 100)}%";
        _sfxEnabledCheckBox.ButtonPressed = settings.SfxEnabled;
        _sfxVolumeSlider.Editable = settings.SfxEnabled;
        
        // Disable apply button initially
        _hasChanges = false;
        _applyButton.Disabled = true;
    }
    
    private void ConnectSignals()
    {
        _backButton.Pressed += OnBackPressed;
        _applyButton.Pressed += OnApplyPressed;
        
        // Display signals
        _resolutionOption.ItemSelected += OnResolutionChanged;
        _scaleOption.ItemSelected += OnScaleChanged;
        _fullscreenCheckBox.Toggled += OnFullscreenToggled;
        
        // Audio signals - these apply immediately
        _masterVolumeSlider.ValueChanged += OnMasterVolumeChanged;
        _musicVolumeSlider.ValueChanged += OnMusicVolumeChanged;
        _musicEnabledCheckBox.Toggled += OnMusicEnabledToggled;
        _sfxVolumeSlider.ValueChanged += OnSfxVolumeChanged;
        _sfxEnabledCheckBox.Toggled += OnSfxEnabledToggled;
    }
    
    private void OnBackPressed()
    {
        // Return to main menu
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }
    
    private void OnApplyPressed()
    {
        var settings = SettingsManager.Instance;
        
        // Apply pending display settings
        settings.SetResolution(_pendingResolution);
        settings.SetScale(_pendingScale);
        settings.SetFullscreen(_pendingFullscreen);
        
        // Reset changes flag and disable apply button
        _hasChanges = false;
        _applyButton.Disabled = true;
    }
    
    private void OnResolutionChanged(long index)
    {
        _pendingResolution = SettingsManager.AvailableResolutions[(int)index];
        UpdateApplyButton();
    }
    
    private void OnScaleChanged(long index)
    {
        _pendingScale = SettingsManager.ScaleFactors[(int)index];
        UpdateApplyButton();
    }
    
    private void OnFullscreenToggled(bool pressed)
    {
        _pendingFullscreen = pressed;
        UpdateApplyButton();
    }
    
    private void UpdateApplyButton()
    {
        // Enable the apply button when any setting is changed
        // Even though audio settings apply immediately, users expect an Apply button
        _hasChanges = true;
        _applyButton.Disabled = false;
    }
    
    // Audio settings apply immediately
    private void OnMasterVolumeChanged(double value)
    {
        var volume = (float)value;
        SettingsManager.Instance.SetMasterVolume(volume);
        _masterVolumeLabel.Text = $"{Mathf.RoundToInt(volume * 100)}%";
        UpdateApplyButton();
    }
    
    private void OnMusicVolumeChanged(double value)
    {
        var volume = (float)value;
        SettingsManager.Instance.SetMusicVolume(volume);
        _musicVolumeLabel.Text = $"{Mathf.RoundToInt(volume * 100)}%";
        UpdateApplyButton();
    }
    
    private void OnMusicEnabledToggled(bool pressed)
    {
        SettingsManager.Instance.SetMusicEnabled(pressed);
        _musicVolumeSlider.Editable = pressed;
        
        // Update visual state
        _musicVolumeSlider.Modulate = pressed ? Colors.White : new Color(1, 1, 1, 0.5f);
        UpdateApplyButton();
    }
    
    private void OnSfxVolumeChanged(double value)
    {
        var volume = (float)value;
        SettingsManager.Instance.SetSfxVolume(volume);
        _sfxVolumeLabel.Text = $"{Mathf.RoundToInt(volume * 100)}%";
        UpdateApplyButton();
    }
    
    private void OnSfxEnabledToggled(bool pressed)
    {
        SettingsManager.Instance.SetSfxEnabled(pressed);
        _sfxVolumeSlider.Editable = pressed;
        
        // Update visual state
        _sfxVolumeSlider.Modulate = pressed ? Colors.White : new Color(1, 1, 1, 0.5f);
        UpdateApplyButton();
    }
    
    public override void _ExitTree()
    {
        // Disconnect signals
        if (_backButton != null) 
            _backButton.Pressed -= OnBackPressed;
        if (_applyButton != null)
            _applyButton.Pressed -= OnApplyPressed;
            
        if (_resolutionOption != null)
            _resolutionOption.ItemSelected -= OnResolutionChanged;
        if (_scaleOption != null)
            _scaleOption.ItemSelected -= OnScaleChanged;
        if (_fullscreenCheckBox != null)
            _fullscreenCheckBox.Toggled -= OnFullscreenToggled;
            
        if (_masterVolumeSlider != null)
            _masterVolumeSlider.ValueChanged -= OnMasterVolumeChanged;
        if (_musicVolumeSlider != null)
            _musicVolumeSlider.ValueChanged -= OnMusicVolumeChanged;
        if (_musicEnabledCheckBox != null)
            _musicEnabledCheckBox.Toggled -= OnMusicEnabledToggled;
        if (_sfxVolumeSlider != null)
            _sfxVolumeSlider.ValueChanged -= OnSfxVolumeChanged;
        if (_sfxEnabledCheckBox != null)
            _sfxEnabledCheckBox.Toggled -= OnSfxEnabledToggled;
    }
} 