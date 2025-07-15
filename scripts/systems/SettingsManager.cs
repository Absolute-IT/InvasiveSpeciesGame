using Godot;
using System.Collections.Generic;
using System.Linq;

namespace InvasiveSpeciesAustralia.Systems;

public partial class SettingsManager : Node
{
    private static SettingsManager _instance;
    public static SettingsManager Instance => _instance;
    
    // Available resolutions
    public static readonly List<Vector2I> AvailableResolutions = new()
    {
        new Vector2I(1920, 1080),   // Full HD
        new Vector2I(2560, 1440),   // 2K
        new Vector2I(3840, 2160),   // 4K (default)
        new Vector2I(5120, 2880),   // 5K
    };
    
    // Scale factors for high-DPI displays
    public static readonly List<float> ScaleFactors = new()
    {
        0.5f,   // 50%
        0.75f,  // 75%
        1.0f,   // 100% (default)
        1.25f,  // 125%
        1.5f,   // 150%
        2.0f    // 200%
    };
    
    // Settings properties
    public Vector2I CurrentResolution { get; private set; } = new Vector2I(3840, 2160);
    public float CurrentScale { get; private set; } = 1.0f;
    public bool IsFullscreen { get; private set; } = false;
    public float MasterVolume { get; private set; } = 1.0f;
    public float MusicVolume { get; private set; } = 0.8f;
    public float SfxVolume { get; private set; } = 1.0f;
    public bool MusicEnabled { get; private set; } = true;
    public bool SfxEnabled { get; private set; } = true;
    public bool ShowTouchDebugger { get; private set; } = false;
    
    // Audio bus indices
    private int _masterBusIndex;
    private int _musicBusIndex;
    private int _sfxBusIndex;
    
    // Config file for persistence
    private const string ConfigPath = "user://settings.cfg";
    private ConfigFile _config = new();
    
    public override void _Ready()
    {
        _instance = this;
        
        // Check current window mode
        var window = GetWindow();
        IsFullscreen = window.Mode == Window.ModeEnum.ExclusiveFullscreen || window.Mode == Window.ModeEnum.Fullscreen;
        
        // Get audio bus indices
        _masterBusIndex = AudioServer.GetBusIndex("Master");
        _musicBusIndex = AudioServer.GetBusIndex("Music");
        _sfxBusIndex = AudioServer.GetBusIndex("SFX");
        
        // Create audio buses if they don't exist
        if (_musicBusIndex == -1)
        {
            AudioServer.AddBus();
            _musicBusIndex = AudioServer.BusCount - 1;
            AudioServer.SetBusName(_musicBusIndex, "Music");
            AudioServer.SetBusSend(_musicBusIndex, "Master");
        }
        
        if (_sfxBusIndex == -1)
        {
            AudioServer.AddBus();
            _sfxBusIndex = AudioServer.BusCount - 1;
            AudioServer.SetBusName(_sfxBusIndex, "SFX");
            AudioServer.SetBusSend(_sfxBusIndex, "Master");
        }
        
        LoadSettings();
    }
    
    private void LoadSettings()
    {
        var error = _config.Load(ConfigPath);
        if (error != Error.Ok)
        {
            GD.Print($"No settings file found, using defaults");
            SaveSettings(); // Create default settings file
            return;
        }
        
        // Load display settings
        var resolutionX = (int)_config.GetValue("display", "resolution_x", CurrentResolution.X);
        var resolutionY = (int)_config.GetValue("display", "resolution_y", CurrentResolution.Y);
        CurrentResolution = new Vector2I(resolutionX, resolutionY);
        
        CurrentScale = (float)_config.GetValue("display", "scale", CurrentScale);
        IsFullscreen = (bool)_config.GetValue("display", "fullscreen", IsFullscreen);
        
        // Load audio settings
        MasterVolume = (float)_config.GetValue("audio", "master_volume", MasterVolume);
        MusicVolume = (float)_config.GetValue("audio", "music_volume", MusicVolume);
        SfxVolume = (float)_config.GetValue("audio", "sfx_volume", SfxVolume);
        MusicEnabled = (bool)_config.GetValue("audio", "music_enabled", MusicEnabled);
        SfxEnabled = (bool)_config.GetValue("audio", "sfx_enabled", SfxEnabled);
        
        // Load debug settings
        ShowTouchDebugger = (bool)_config.GetValue("debug", "show_touch_debugger", ShowTouchDebugger);
        
        // Apply loaded settings
        ApplyAllSettings();
    }
    
    public void SaveSettings()
    {
        // Save display settings
        _config.SetValue("display", "resolution_x", CurrentResolution.X);
        _config.SetValue("display", "resolution_y", CurrentResolution.Y);
        _config.SetValue("display", "scale", CurrentScale);
        _config.SetValue("display", "fullscreen", IsFullscreen);
        
        // Save audio settings
        _config.SetValue("audio", "master_volume", MasterVolume);
        _config.SetValue("audio", "music_volume", MusicVolume);
        _config.SetValue("audio", "sfx_volume", SfxVolume);
        _config.SetValue("audio", "music_enabled", MusicEnabled);
        _config.SetValue("audio", "sfx_enabled", SfxEnabled);
        
        // Save debug settings
        _config.SetValue("debug", "show_touch_debugger", ShowTouchDebugger);
        
        var error = _config.Save(ConfigPath);
        if (error != Error.Ok)
        {
            GD.PrintErr($"Failed to save settings: {error}");
        }
    }
    
    public void SetResolution(Vector2I resolution)
    {
        CurrentResolution = resolution;
        ApplyDisplaySettings();
        SaveSettings();
    }
    
    public void SetScale(float scale)
    {
        CurrentScale = scale;
        ApplyDisplaySettings();
        SaveSettings();
    }
    
    public void SetFullscreen(bool fullscreen)
    {
        IsFullscreen = fullscreen;
        ApplyDisplaySettings();
        SaveSettings();
    }
    
    public void SetMasterVolume(float volume)
    {
        MasterVolume = Mathf.Clamp(volume, 0.0f, 1.0f);
        ApplyAudioSettings();
        SaveSettings();
    }
    
    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp(volume, 0.0f, 1.0f);
        ApplyAudioSettings();
        SaveSettings();
    }
    
    public void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp(volume, 0.0f, 1.0f);
        ApplyAudioSettings();
        SaveSettings();
    }
    
    public void SetMusicEnabled(bool enabled)
    {
        MusicEnabled = enabled;
        ApplyAudioSettings();
        SaveSettings();
    }
    
    public void SetSfxEnabled(bool enabled)
    {
        SfxEnabled = enabled;
        ApplyAudioSettings();
        SaveSettings();
    }
    
    public void SetShowTouchDebugger(bool show)
    {
        ShowTouchDebugger = show;
        SaveSettings();
        
        // Update all active debuggers
        var tree = GetTree();
        if (tree != null)
        {
            var debuggers = tree.GetNodesInGroup("touch_debuggers");
            foreach (Node node in debuggers)
            {
                if (node is MultiTouchDebugger debugger)
                {
                    debugger.SetEnabled(show);
                }
            }
        }
    }
    
    private void ApplyAllSettings()
    {
        ApplyDisplaySettings();
        ApplyAudioSettings();
    }
    
    private void ApplyDisplaySettings()
    {
        var window = GetWindow();
        
        if (IsFullscreen)
        {
            window.Mode = Window.ModeEnum.ExclusiveFullscreen;
        }
        else
        {
            window.Mode = Window.ModeEnum.Windowed;
            
            // Apply resolution with scaling
            var scaledSize = new Vector2I(
                Mathf.RoundToInt(CurrentResolution.X * CurrentScale),
                Mathf.RoundToInt(CurrentResolution.Y * CurrentScale)
            );
            
            window.Size = scaledSize;
            
            // Set content scale for proper rendering
            window.ContentScaleSize = CurrentResolution;
            window.ContentScaleMode = Window.ContentScaleModeEnum.Viewport;
            window.ContentScaleAspect = Window.ContentScaleAspectEnum.Keep;
            
            // Center window
            var screenSize = DisplayServer.ScreenGetSize();
            var position = (screenSize - scaledSize) / 2;
            window.Position = position;
        }
    }
    
    private void ApplyAudioSettings()
    {
        // Apply master volume
        AudioServer.SetBusVolumeDb(_masterBusIndex, LinearToDb(MasterVolume));
        
        // Apply music volume and mute state
        var musicVolume = MusicEnabled ? MusicVolume : 0.0f;
        AudioServer.SetBusVolumeDb(_musicBusIndex, LinearToDb(musicVolume));
        AudioServer.SetBusMute(_musicBusIndex, !MusicEnabled);
        
        // Apply SFX volume and mute state
        var sfxVolume = SfxEnabled ? SfxVolume : 0.0f;
        AudioServer.SetBusVolumeDb(_sfxBusIndex, LinearToDb(sfxVolume));
        AudioServer.SetBusMute(_sfxBusIndex, !SfxEnabled);
    }
    
    private float LinearToDb(float linear)
    {
        // Convert linear volume (0-1) to decibels
        if (linear <= 0.0f)
            return -80.0f; // Effectively silent
        
        return 20.0f * Mathf.Log(linear) / Mathf.Log(10.0f);
    }
    
    public int GetResolutionIndex(Vector2I resolution)
    {
        for (int i = 0; i < AvailableResolutions.Count; i++)
        {
            if (AvailableResolutions[i] == resolution)
                return i;
        }
        
        // Return default (4K) if not found
        return AvailableResolutions.FindIndex(r => r == new Vector2I(3840, 2160));
    }
    
    public int GetScaleIndex(float scale)
    {
        for (int i = 0; i < ScaleFactors.Count; i++)
        {
            if (Mathf.IsEqualApprox(ScaleFactors[i], scale))
                return i;
        }
        
        // Return default (100%) if not found
        return ScaleFactors.FindIndex(s => Mathf.IsEqualApprox(s, 1.0f));
    }
} 