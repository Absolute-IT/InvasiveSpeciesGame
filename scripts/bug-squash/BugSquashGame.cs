using Godot;
using System.Collections.Generic;
using System.Linq;

namespace InvasiveSpeciesAustralia
{
    public partial class BugSquashGame : Node2D
    {
        private List<BugSquashStage> _stages;
        private int _currentStageIndex = 0;
        private BugSquashStage _currentStage;
        private int _preyCount = 0;
        private int _predatorCount = 0;
        private int _foodCount = 0;
        private int _weedCount = 0;
        private int _maxFoodCount = 0;
        private float _foodRespawnTimer = 0f;
        private List<BugSquashSpecies> _foodSpecies = new();
        // Per-species food respawn state
        private Dictionary<string, float> _foodRespawnTimers = new();
        private Dictionary<string, int> _foodLiveCounts = new();
        private Dictionary<string, BugSquashSpecies> _foodSpeciesById = new();
        // Per-species live counts (all species)
        private readonly Dictionary<string, int> _liveCountsBySpeciesId = new();
        private bool _isGameOver = false; // Add flag to prevent multiple game over screens
        
        // UI Elements
        private CanvasLayer _uiLayer;
        private Sprite2D _backgroundSprite;
        private Control _instructionScreen;
        private Control _gameScreen;
        private Control _scorePanel;
        private Label _invasiveLabel;
        private Label _invasiveCountLabel;
        private Label _nativeLabel;
        private Label _nativeCountLabel;
        private Button _homeButton;
        private RichTextLabel _interactionDescLabel;
        private HFlowContainer _speciesContainer;
        
        // Effects
        private ShockwaveEffect _shockwaveEffect;
        
        // Camera and screen shake
        private Camera2D _camera;
        private float _shakeTimer = 0f;
        private float _shakeIntensity = 0f;
        private Vector2 _originalCameraPosition;
        
        // Sound system
        private List<AudioStream> _boomSounds = new();
        private int _lastSoundIndex = -1;
        
        // Ambience audio players for crossfading
        private AudioStreamPlayer _audioPlayer1;
        private AudioStreamPlayer _audioPlayer2;
        private AudioStreamPlayer _currentAudioPlayer;
        private AudioStreamPlayer _previousAudioPlayer;
        private Tween _audioFadeTween;
        private bool _isInitializingStage = false;

        private const float FOOD_RESPAWN_DELAY = 3f;
        private const float SCREEN_SHAKE_DURATION = 0.3f;
        private const float SCREEN_SHAKE_INTENSITY = 30f;

        public override void _Ready()
        {
            LoadStages();
            LoadSounds();
            SetupAudioPlayers();
            CreateUI();
            ShowInstructionScreen();
        }
        
        private void LoadSounds()
        {
            // Load all boom sounds from the boom folder
            var boomPath = "res://assets/sounds/bug-squash/boom/";
            var soundFiles = new List<string>
            {
                "boom-1.wav",
                "boom-2.wav",
                "boom-3.wav",
                "boom-4.wav",
                "boom-5.wav"
            };
            
            foreach (var soundFile in soundFiles)
            {
                var fullPath = boomPath + soundFile;
                if (ResourceLoader.Exists(fullPath))
                {
                    var sound = GD.Load<AudioStream>(fullPath);
                    if (sound != null)
                    {
                        _boomSounds.Add(sound);
                        GD.Print($"Loaded sound: {soundFile}");
                    }
                }
            }
            GD.Print($"Total boom sounds loaded: {_boomSounds.Count}");
        }
        
        private void SetupAudioPlayers()
        {
            // Create two audio players for crossfading
            _audioPlayer1 = new AudioStreamPlayer();
            _audioPlayer1.Name = "AudioPlayer1";
            _audioPlayer1.Bus = "Master";
            _audioPlayer1.VolumeDb = -80.0f; // Start silent
            AddChild(_audioPlayer1);
            
            _audioPlayer2 = new AudioStreamPlayer();
            _audioPlayer2.Name = "AudioPlayer2";
            _audioPlayer2.Bus = "Master";
            _audioPlayer2.VolumeDb = -80.0f; // Start silent
            AddChild(_audioPlayer2);
        }

        private void LoadStages()
        {
            _stages = ConfigLoader.LoadBugSquashStages();
            if (_stages == null || _stages.Count == 0)
            {
                GD.PrintErr("No bug squash stages found!");
                return;
            }
            
            // Shuffle stages into random order
            ShuffleStages();
        }
        
        private void ShuffleStages()
        {
            if (_stages == null || _stages.Count <= 1) return;
            
            // Fisher-Yates shuffle algorithm using Godot's random functions
            for (int i = _stages.Count - 1; i > 0; i--)
            {
                int randomIndex = GD.RandRange(0, i);
                var temp = _stages[i];
                _stages[i] = _stages[randomIndex];
                _stages[randomIndex] = temp;
            }
            
            GD.Print($"Shuffled {_stages.Count} stages into random order");
        }

        private void CreateUI()
        {
            // Add Camera2D for screen shake effect
            _camera = new Camera2D();
            _camera.Enabled = true;
            _camera.MakeCurrent();
            _camera.Position = new Vector2(1920, 1080); // Center of screen
            AddChild(_camera);
            _originalCameraPosition = _camera.Position;
            
            // Background - add as a child of the game scene with negative Z-index
            var bgSprite = new Sprite2D();
            bgSprite.Centered = false;
            bgSprite.ZIndex = -100; // Behind everything
            AddChild(bgSprite);
            
            // Store reference for texture updates
            _backgroundSprite = bgSprite;
            
            // Create CanvasLayer for UI elements (renders on top but doesn't block input)
            _uiLayer = new CanvasLayer();
            AddChild(_uiLayer);

            // Game screen (hidden initially) - add to UI layer
            _gameScreen = new Control();
            _gameScreen.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            _gameScreen.Visible = false;
            _gameScreen.MouseFilter = Control.MouseFilterEnum.Pass; // Allow buttons to work
            _uiLayer.AddChild(_gameScreen);

            // Score/status panel
            CreateScorePanel();

            // Home button
            // _homeButton = new Button();
            // _homeButton.Text = "Home";
            // _homeButton.Position = new Vector2(3840 - 400, 2160 - 200);
            // _homeButton.Size = new Vector2(300, 160);
            // _homeButton.AddThemeFontSizeOverride("font_size", 48);
            // _homeButton.Pressed += OnHomePressed;
            // _gameScreen.AddChild(_homeButton);

            // Instruction screen
            CreateInstructionScreen();
            
            // Add multi-touch debugger
            AddMultiTouchDebugger();
            
            // Create shockwave effect on its own CanvasLayer to render after UI
            var effectLayer = new CanvasLayer();
            effectLayer.Layer = 2; // Higher than UI layer (which is default 1)
            AddChild(effectLayer);
            
            _shockwaveEffect = new ShockwaveEffect();
            effectLayer.AddChild(_shockwaveEffect);
        }

        private void CreateScorePanel()
        {
            // Main score container
            _scorePanel = new Control();
            _scorePanel.MouseFilter = Control.MouseFilterEnum.Ignore;
            _scorePanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
            _scorePanel.Position = new Vector2(0, 50);
            _scorePanel.Size = new Vector2(3840, 200);
            _gameScreen.AddChild(_scorePanel);

            // Create two score boxes - one for invasive, one for native
            var invasiveResult = CreateScoreBox(
                "Invasive Species", 
                new Color(0.9f, 0.2f, 0.2f), // Red color for invasive
                new Vector2(3840 - 700, 0),
                HorizontalAlignment.Center
            );
            _scorePanel.AddChild(invasiveResult.Item1);
            _invasiveCountLabel = invasiveResult.Item2;

            var nativeResult = CreateScoreBox(
                "Native Species", 
                new Color(0.2f, 0.8f, 0.3f), // Green color for native
                new Vector2(100, 0),
                HorizontalAlignment.Center
            );
            _scorePanel.AddChild(nativeResult.Item1);
            _nativeCountLabel = nativeResult.Item2;
        }

        private (Control, Label) CreateScoreBox(string title, Color accentColor, Vector2 position, HorizontalAlignment alignment)
        {
            var container = new Control();
            container.Position = position;
            container.Size = new Vector2(600, 160);
            container.MouseFilter = Control.MouseFilterEnum.Ignore;

            // Background with blur effect
            var background = new ColorRect();
            background.Color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            background.MouseFilter = Control.MouseFilterEnum.Ignore;
            
            // Apply backdrop blur shader
            var blurShader = GD.Load<Shader>("res://shaders/backdrop_blur.gdshader");
            var shaderMaterial = new ShaderMaterial();
            shaderMaterial.Shader = blurShader;
            shaderMaterial.SetShaderParameter("blur_amount", 5.0f);
            background.Material = shaderMaterial;
            
            container.AddChild(background);

            // Accent border on the bottom
            var accentBorder = new ColorRect();
            accentBorder.Color = accentColor;
            accentBorder.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
            accentBorder.Position = new Vector2(0, 156);
            accentBorder.Size = new Vector2(600, 4);
            accentBorder.MouseFilter = Control.MouseFilterEnum.Ignore;
            container.AddChild(accentBorder);

            // Content container
            var contentBox = new VBoxContainer();
            contentBox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            contentBox.AddThemeConstantOverride("separation", 10);
            contentBox.MouseFilter = Control.MouseFilterEnum.Ignore;
            container.AddChild(contentBox);

            // Title label
            var titleLabel = new Label();
            titleLabel.Text = title;
            titleLabel.AddThemeFontSizeOverride("font_size", 42);
            titleLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
            titleLabel.HorizontalAlignment = alignment;
            titleLabel.VerticalAlignment = VerticalAlignment.Center;
            titleLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            contentBox.AddChild(titleLabel);

            // Count label
            var countLabel = new Label();
            countLabel.Name = "Count";
            countLabel.Text = "0";
            countLabel.AddThemeFontSizeOverride("font_size", 72);
            countLabel.AddThemeColorOverride("font_color", accentColor);
            countLabel.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
            countLabel.HorizontalAlignment = alignment;
            countLabel.VerticalAlignment = VerticalAlignment.Center;
            countLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            contentBox.AddChild(countLabel);

            return (container, countLabel);
        }

        private void AddMultiTouchDebugger()
        {
            var debugger = new Systems.MultiTouchDebugger();
            debugger.Name = "MultiTouchDebugger";
            _uiLayer.AddChild(debugger);
        }
        
        private void CreateInstructionScreen()
        {
            _instructionScreen = new Control();
            _instructionScreen.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            _uiLayer.AddChild(_instructionScreen);

            // Semi-transparent background
            var bgRect = new ColorRect();
            bgRect.Color = new Color(0, 0, 0, 0.8f);
            bgRect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            _instructionScreen.AddChild(bgRect);

            // Calculate UI scale relative to a 4K reference
            var screenSize = GetViewportRect().Size;
            var uiScale = screenSize.Y / 2160f;

            // Outer margin container to adapt to any resolution
            var margin = new MarginContainer();
            margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            margin.AddThemeConstantOverride("margin_left", (int)(180 * uiScale));
            margin.AddThemeConstantOverride("margin_right", (int)(180 * uiScale));
            margin.AddThemeConstantOverride("margin_top", (int)(140 * uiScale));
            margin.AddThemeConstantOverride("margin_bottom", (int)(160 * uiScale));
            _instructionScreen.AddChild(margin);

            // Content container that fills the margin area
            var container = new VBoxContainer();
            container.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            container.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            container.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            container.AddThemeConstantOverride("separation", (int)(60 * uiScale));
            margin.AddChild(container);

            // Title
            var titleLabel = new Label();
            titleLabel.Text = "How to Play";
            titleLabel.AddThemeFontSizeOverride("font_size", (int)(96 * uiScale));
            titleLabel.AddThemeColorOverride("font_color", Colors.White);
            titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            container.AddChild(titleLabel);

            // Interaction description
            _interactionDescLabel = new RichTextLabel();
            _interactionDescLabel.Name = "InteractionDescription";
            _interactionDescLabel.CustomMinimumSize = new Vector2(0, (int)(400 * uiScale));
            _interactionDescLabel.BbcodeEnabled = true;
            _interactionDescLabel.AddThemeFontSizeOverride("normal_font_size", (int)(56 * uiScale));
            _interactionDescLabel.AddThemeColorOverride("font_color", Colors.White);
            _interactionDescLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _interactionDescLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            container.AddChild(_interactionDescLabel);

            // Species container (wraps items when too many for one row)
            _speciesContainer = new HFlowContainer();
            _speciesContainer.Name = "SpeciesContainer";
            _speciesContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            _speciesContainer.SizeFlagsVertical = Control.SizeFlags.ShrinkEnd;
            _speciesContainer.Alignment = FlowContainer.AlignmentMode.Center;
            // Increase separation for larger entities and scale with screen
            _speciesContainer.AddThemeConstantOverride("h_separation", (int)(220 * uiScale));
            _speciesContainer.AddThemeConstantOverride("v_separation", (int)(120 * uiScale));
            container.AddChild(_speciesContainer);

            // Start button
            var startButton = new Button();
            startButton.Text = "Start Level";
            startButton.CustomMinimumSize = new Vector2((int)(600 * uiScale), (int)(160 * uiScale));
            startButton.AddThemeFontSizeOverride("font_size", (int)(64 * uiScale));
            startButton.Pressed += OnStartLevel;
            container.AddChild(startButton);
        }

        private void ShowInstructionScreen()
        {
            if (_currentStageIndex >= _stages.Count)
            {
                // Game complete
                StopAllAmbience();
                GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
                return;
            }

            _currentStage = _stages[_currentStageIndex];
            _instructionScreen.Visible = true;
            _gameScreen.Visible = false;

            // Play ambience sound for this stage
            PlayAmbienceSound(_currentStage);

            // Update background
            if (!string.IsNullOrEmpty(_currentStage.BackgroundImage) && _backgroundSprite != null)
            {
                var bgTexture = GD.Load<Texture2D>(_currentStage.BackgroundImage);
                _backgroundSprite.Texture = bgTexture;
                if (bgTexture != null)
                {
                    // Scale to cover the screen
                    var textureSize = bgTexture.GetSize();
                    var screenSize = new Vector2(3840, 2160);
                    var scale = Mathf.Max(screenSize.X / textureSize.X, screenSize.Y / textureSize.Y);
                    _backgroundSprite.Scale = new Vector2(scale, scale);
                }
            }

            // Update interaction description
            if (_interactionDescLabel != null)
            {
                // Center the paragraph using BBCode since RichTextLabel lacks a direct alignment property
                _interactionDescLabel.Text = "[center]" + _currentStage.InteractionDescription + "[/center]";
            }

            // Clear and populate species display
            if (_speciesContainer != null)
            {
                foreach (Node child in _speciesContainer.GetChildren())
                {
                    child.QueueFree();
                }

                foreach (var species in _currentStage.Species)
                {
                    var speciesDisplay = CreateSpeciesDisplay(species);
                    _speciesContainer.AddChild(speciesDisplay);
                }
            }
        }

        private Control CreateSpeciesDisplay(BugSquashSpecies species)
        {
            var container = new VBoxContainer();
            container.AddThemeConstantOverride("separation", 0); // No global separation

            // Entity visual
            var entityContainer = new Control();
            var screenSize = GetViewportRect().Size;
            var uiScale = screenSize.Y / 2160f;
            // Scale down for multi-row layouts, but clamp to keep readability
            var ringSize = Mathf.Clamp((int)(320 * uiScale), 220, 360);
            var imageSize = ringSize - (int)(ringSize * 0.08f);
            var halfRing = ringSize / 2f;
            entityContainer.CustomMinimumSize = new Vector2(ringSize, ringSize);
            container.AddChild(entityContainer);

            // Colored ring
            var ring = new TextureRect();
            ring.ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional;
            ring.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            ring.Modulate = new Color(species.Color);
            ring.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
            ring.Size = new Vector2(ringSize, ringSize);
            ring.Position = new Vector2(-halfRing, -halfRing);
            
            // Create a white circle texture for the ring
            var image = Image.CreateEmpty((int)ringSize, (int)ringSize, false, Image.Format.Rgba8);
            for (int x = 0; x < ringSize; x++)
            {
                for (int y = 0; y < ringSize; y++)
                {
                    float dx = x - halfRing;
                    float dy = y - halfRing;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    // Outline thickness ~20px at 4K, scaled with uiScale
                    var radius = halfRing;
                    var inner = halfRing - Mathf.Max(18f * uiScale, 12f);
                    if (dist < radius && dist > inner)
                    {
                        image.SetPixel(x, y, Colors.White);
                    }
                }
            }
            var ringTexture = ImageTexture.CreateFromImage(image);
            ring.Texture = ringTexture;
            entityContainer.AddChild(ring);

            // Entity sprite
            if (!string.IsNullOrEmpty(species.Image))
            {
                var sprite = new TextureRect();
                sprite.Texture = GD.Load<Texture2D>(species.Image);
                sprite.ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional;
                sprite.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                sprite.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
                // Image size scaled to fit the ring (leave ~10px border at 4K)
                sprite.Size = new Vector2(imageSize, imageSize);
                sprite.Position = new Vector2(-imageSize / 2f, -imageSize / 2f);
                
                // Apply circular clipping shader to match the in-game entities
                var clipShaderPath = "res://shaders/circular_clip.gdshader";
                if (ResourceLoader.Exists(clipShaderPath))
                {
                    var shader = GD.Load<Shader>(clipShaderPath);
                    var shaderMaterial = new ShaderMaterial();
                    shaderMaterial.Shader = shader;
                    sprite.Material = shaderMaterial;
                }
                
                entityContainer.AddChild(sprite);
            }

            // Add a fixed-size spacer for image-to-label gap
            var imageLabelSpacer = new Control();
            imageLabelSpacer.CustomMinimumSize = new Vector2(0, (int)(24 * uiScale));
            container.AddChild(imageLabelSpacer);

            // Group the name and behaviour labels in their own VBox with small separation
            var labelBox = new VBoxContainer();
            labelBox.AddThemeConstantOverride("separation", (int)(6 * uiScale)); // Small gap between labels

            var nameLabel = new Label();
            nameLabel.Text = species.Name;
            nameLabel.AddThemeFontSizeOverride("font_size", Mathf.Clamp((int)(52 * uiScale), 36, 60));
            nameLabel.AddThemeColorOverride("font_color", Colors.White);
            nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
            labelBox.AddChild(nameLabel);

            var behaviorLabel = new Label();
            behaviorLabel.Text = $"({species.Behavior})";
            behaviorLabel.AddThemeFontSizeOverride("font_size", Mathf.Clamp((int)(36 * uiScale), 28, 44));
            behaviorLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
            behaviorLabel.HorizontalAlignment = HorizontalAlignment.Center;
            labelBox.AddChild(behaviorLabel);

            container.AddChild(labelBox);

            return container;
        }

        private void OnStartLevel()
        {
            _instructionScreen.Visible = false;
            _gameScreen.Visible = true;
            
            // Ensure score panel is visible
            if (_scorePanel != null)
                _scorePanel.Visible = true;
            
            GD.Print($"Starting level");
            
            StartStage();
        }

        private void StartStage()
        {
            // Clear existing entities
            foreach (Node child in GetChildren())
            {
                if (child is BugSquashEntity entity)
                {
                    entity.QueueFree();
                }
            }

            // Reset game state
            _isGameOver = false;
            _isInitializingStage = true;
            _preyCount = 0;
            _predatorCount = 0;
            _foodCount = 0;
            _weedCount = 0;
            _maxFoodCount = 0;
            _foodRespawnTimer = 0f;
            _foodSpecies.Clear();
            _foodRespawnTimers.Clear();
            _foodLiveCounts.Clear();
            _foodSpeciesById.Clear();
            _liveCountsBySpeciesId.Clear();

            // Spawn initial entities
            foreach (var species in _currentStage.Species)
            {
                var behavior = System.Enum.Parse<EntityBehavior>(species.Behavior);
                
                if (behavior == EntityBehavior.Food)
                {
                    // Track all food species for counts
                    _foodSpeciesById[species.Id] = species;
                    _foodLiveCounts[species.Id] = 0; // Will be incremented on spawn

                    // Only food with spawn rate participates in timed respawn
                    if (species.SpawnRate > 0)
                    {
                        _foodSpecies.Add(species);
                        _foodRespawnTimers[species.Id] = 0f;
                    }
                }

                for (int i = 0; i < species.StartingNumber; i++)
                {
                    var position = GetRandomPosition();
                    SpawnEntity(species, position);
                }
            }

            _isInitializingStage = false;
            UpdateScore();
            CheckWinCondition();
        }

        public BugSquashSpecies GetSpeciesById(string speciesId)
        {
            if (_currentStage == null || _currentStage.Species == null)
                return null;
            
            return _currentStage.Species.FirstOrDefault(s => s.Id == speciesId);
        }

        public BugSquashEntity SpawnEntity(BugSquashSpecies species, Vector2 position, bool isGrowthSpawn = false)
        {
            var entity = new BugSquashEntity();
            
            // Add to scene tree first so _Ready is called
            AddChild(entity); // Add directly to game scene instead of container
            
            // Then initialize with species data
            entity.Initialize(species, position);
            
            // Connect signals
            entity.EntityDied += OnEntityDied;
            entity.EntityClicked += OnEntityClicked;
            
            GD.Print($"Spawned {species.Name} at {position}, Entity Global Position: {entity.GlobalPosition}");

            // Update counts
            var behavior = System.Enum.Parse<EntityBehavior>(species.Behavior);
            switch (behavior)
            {
                case EntityBehavior.Predator:
                    _predatorCount++;
                    break;
                case EntityBehavior.Prey:
                    _preyCount++;
                    break;
                case EntityBehavior.Food:
                    _foodCount++;
                    if (species != null && _foodLiveCounts.ContainsKey(species.Id))
                    {
                        _foodLiveCounts[species.Id] = _foodLiveCounts[species.Id] + 1;
                    }
                    break;
                case EntityBehavior.Nest:
                    _predatorCount++;
                    break;
                case EntityBehavior.Weed:
                    _weedCount++;
                    break;
            }

            // Track per-species live counts
            if (species != null)
            {
                if (!_liveCountsBySpeciesId.ContainsKey(species.Id))
                {
                    _liveCountsBySpeciesId[species.Id] = 0;
                }
                _liveCountsBySpeciesId[species.Id] = _liveCountsBySpeciesId[species.Id] + 1;
            }

            // Handle weed growth behavior: either begin growing (for growth spawns) or start its own growth cycle immediately
            if (behavior == EntityBehavior.Weed)
            {
                if (isGrowthSpawn)
                {
                    // Growing child fades in over spawn_rate and only then starts its own cycle
                    entity.BeginGrowth(species.SpawnRate);
                }
                else
                {
                    // Mature weed starts producing a new growing weed immediately
                    entity.StartWeedGrowthCycle();
                }
            }

            UpdateScore();
            if (!_isInitializingStage)
            {
                CheckWinCondition();
            }

            return entity;
        }

        private void OnEntityDied(BugSquashEntity entity)
        {
            // Update counts
            switch (entity.Behavior)
            {
                case EntityBehavior.Predator:
                    _predatorCount--;
                    break;
                case EntityBehavior.Prey:
                    _preyCount--;
                    break;
                case EntityBehavior.Food:
                    _foodCount--;
                    if (entity.Species != null && _foodLiveCounts.ContainsKey(entity.Species.Id))
                    {
                        _foodLiveCounts[entity.Species.Id] = Mathf.Max(0, _foodLiveCounts[entity.Species.Id] - 1);
                    }
                    break;
				case EntityBehavior.Nest:
					_predatorCount--;
					break;
				case EntityBehavior.Weed:
					_weedCount--;
					break;
            }

            // Update per-species live counts
            if (entity.Species != null && _liveCountsBySpeciesId.ContainsKey(entity.Species.Id))
            {
                _liveCountsBySpeciesId[entity.Species.Id] = Mathf.Max(0, _liveCountsBySpeciesId[entity.Species.Id] - 1);
            }

            UpdateScore();
            CheckWinCondition();
        }

        private void OnEntityClicked(BugSquashEntity entity)
        {
            // Spawn pop text effect at entity position
            var popText = new PopTextEffect();
            AddChild(popText);
            popText.Initialize(entity.GlobalPosition, entity.Behavior);
            
            // Trigger screen shake
            StartScreenShake();
            
            // Only trigger shockwave for non-fatal hits on invasive species
            if (_shockwaveEffect != null && entity.Behavior == EntityBehavior.Predator && entity.IsAlive)
            {
                _shockwaveEffect.TriggerShockwave(entity.GlobalPosition);
            }
            
            // Play sound effect
            PlayRandomBoomSound(entity.GlobalPosition);
        }
        
        private void PlayRandomBoomSound(Vector2 position)
        {
            if (_boomSounds.Count == 0) return;
            
            int soundIndex;
            
            // If only one sound, just play it
            if (_boomSounds.Count == 1)
            {
                soundIndex = 0;
            }
            else
            {
                // Choose a random sound that's different from the last one
                do
                {
                    soundIndex = GD.RandRange(0, _boomSounds.Count - 1);
                } while (soundIndex == _lastSoundIndex);
            }
            
            _lastSoundIndex = soundIndex;
            
            // Create a new audio player for this sound
            var audioPlayer = new AudioStreamPlayer2D();
            audioPlayer.Stream = _boomSounds[soundIndex];
            audioPlayer.GlobalPosition = position;
            audioPlayer.Bus = "Master";
            
            // Randomize pitch slightly for variety (0.9 to 1.1)
            audioPlayer.PitchScale = 0.9f + (float)GD.Randf() * 0.2f;
            
            // Add to scene
            AddChild(audioPlayer);
            
            // Play the sound
            audioPlayer.Play();
            
            // Clean up the audio player when finished
            audioPlayer.Finished += () => audioPlayer.QueueFree();
            
            GD.Print($"Playing boom sound {soundIndex} at position {position}");
        }
        
        private void StartScreenShake()
        {
            _shakeTimer = SCREEN_SHAKE_DURATION;
            _shakeIntensity = SCREEN_SHAKE_INTENSITY;
        }

        private void UpdateScore()
        {
            GD.Print($"UpdateScore called - Predator: {_predatorCount}, Prey: {_preyCount}");
            GD.Print($"Labels - Invasive: {_invasiveCountLabel}, Native: {_nativeCountLabel}");
            
            var invasiveTotal = _predatorCount + _weedCount;
            if (_invasiveCountLabel != null)
                _invasiveCountLabel.Text = invasiveTotal.ToString();

            // Native total includes Prey plus any Food species flagged as consider_native_species
            int nativeFoodCount = 0;
            if (_foodLiveCounts != null && _foodSpeciesById != null)
            {
                foreach (var kvp in _foodLiveCounts)
                {
                    if (_foodSpeciesById.TryGetValue(kvp.Key, out var species) && species != null && species.ConsiderNativeSpecies)
                    {
                        nativeFoodCount += kvp.Value;
                    }
                }
            }

            var nativeTotal = _preyCount + nativeFoodCount;
            if (_nativeCountLabel != null)
                _nativeCountLabel.Text = nativeTotal.ToString();
        }

        private void CheckWinCondition()
        {
            // Only check win conditions if game is not already over
            if (_isGameOver) return;
            
            var invasiveTotal = _predatorCount + _weedCount;

            // Native total includes Prey plus any Food species flagged to count as native
            int nativeFoodCount = 0;
            if (_foodLiveCounts != null && _foodSpeciesById != null)
            {
                foreach (var kvp in _foodLiveCounts)
                {
                    if (_foodSpeciesById.TryGetValue(kvp.Key, out var species) && species != null && species.ConsiderNativeSpecies)
                    {
                        nativeFoodCount += kvp.Value;
                    }
                }
            }
            var nativeTotal = _preyCount + nativeFoodCount;

            // Optional alternate lose condition
            if (_currentStage?.LoseCondition != null)
            {
                var lc = _currentStage.LoseCondition;
                int currentCount = 0;
                if (!string.IsNullOrEmpty(lc.Species))
                {
                    _liveCountsBySpeciesId.TryGetValue(lc.Species, out currentCount);
                }
                if (currentCount >= lc.Count)
                {
                    _isGameOver = true;
                    // Build reason text using species display name if available
                    var speciesName = lc.Species;
                    var spec = GetSpeciesById(lc.Species);
                    if (spec != null && !string.IsNullOrEmpty(spec.Name))
                    {
                        speciesName = spec.Name;
                    }
                    var reason = $"The {speciesName} population got out of control! It's all over.";
                    ShowGameOver(false, reason);
                    return;
                }
            }

            if (nativeTotal <= 0 && invasiveTotal > 0)
            {
                // Game over - native species extinct
                _isGameOver = true;
                ShowGameOver(false);
            }
            else if (invasiveTotal <= 0 && (_currentStage?.LoseCondition != null || nativeTotal > 0))
            {
                // Victory - invasive species eradicated
                _isGameOver = true;
                ShowGameOver(true);
            }
        }

        private void ShowGameOver(bool victory, string customLoseMessage = null)
        {
            // Freeze all entities so they stop moving/processing when the stage ends
            SetAllEntitiesFrozen(true);

            // Create game over overlay
            var overlay = new Control();
            overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            _uiLayer.AddChild(overlay);

            var bg = new ColorRect();
            bg.Color = new Color(0, 0, 0, 0.7f);
            bg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            overlay.AddChild(bg);

            var container = new VBoxContainer();
            container.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
            container.Position = new Vector2(-600, -400);
            container.Size = new Vector2(1200, 800);
            container.AddThemeConstantOverride("separation", 60);
            overlay.AddChild(container);

            var resultLabel = new Label();
            resultLabel.Text = victory ? "Victory!" : "Game Over";
            resultLabel.AddThemeFontSizeOverride("font_size", 128);
            resultLabel.AddThemeColorOverride("font_color", victory ? Colors.Green : Colors.Red);
            container.AddChild(resultLabel);

            var messageLabel = new Label();
            messageLabel.Text = victory ? 
                "You've successfully eradicated the invasive species!" : 
                (string.IsNullOrEmpty(customLoseMessage) ? "The native species has gone extinct." : customLoseMessage);
            messageLabel.AddThemeFontSizeOverride("font_size", 48);
            messageLabel.AddThemeColorOverride("font_color", Colors.White);
            messageLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            messageLabel.CustomMinimumSize = new Vector2(1200, 200);
            container.AddChild(messageLabel);

            var buttonContainer = new HBoxContainer();
            buttonContainer.AddThemeConstantOverride("separation", 40);
            container.AddChild(buttonContainer);

            if (victory && _currentStageIndex < _stages.Count - 1)
            {
                var nextButton = new Button();
                nextButton.Text = "Next Level";
                nextButton.CustomMinimumSize = new Vector2(400, 120);
                nextButton.AddThemeFontSizeOverride("font_size", 48);
                nextButton.Pressed += () => {
                    overlay.QueueFree();
                    _currentStageIndex++;
                    ShowInstructionScreen();
                };
                buttonContainer.AddChild(nextButton);
            }

            var homeButton = new Button();
            homeButton.Text = "Main Menu";
            homeButton.CustomMinimumSize = new Vector2(400, 120);
            homeButton.AddThemeFontSizeOverride("font_size", 48);
            homeButton.Pressed += OnHomePressed;
            buttonContainer.AddChild(homeButton);
        }

        private void SetAllEntitiesFrozen(bool frozen)
        {
            foreach (Node child in GetChildren())
            {
                if (child is BugSquashEntity entity)
                {
                    entity.SetFrozen(frozen);
                }
            }
        }

        public override void _Process(double delta)
        {
            if (!_gameScreen.Visible) return;

            var deltaF = (float)delta;
            
            // Handle screen shake
            if (_shakeTimer > 0)
            {
                _shakeTimer -= deltaF;
                
                // Calculate shake offset
                var shakeAmount = _shakeIntensity * (_shakeTimer / SCREEN_SHAKE_DURATION);
                var shakeX = (GD.Randf() - 0.5f) * 2f * shakeAmount;
                var shakeY = (GD.Randf() - 0.5f) * 2f * shakeAmount;
                
                _camera.Position = _originalCameraPosition + new Vector2(shakeX, shakeY);
                
                if (_shakeTimer <= 0)
                {
                    // Reset camera position
                    _camera.Position = _originalCameraPosition;
                }
            }

            // Handle food respawning per species
            if (_foodSpecies.Count > 0)
            {
                foreach (var foodSpecies in _foodSpecies)
                {
                    // Ensure tracking exists
                    if (!_foodRespawnTimers.ContainsKey(foodSpecies.Id))
                    {
                        _foodRespawnTimers[foodSpecies.Id] = 0f;
                    }
                    if (!_foodLiveCounts.ContainsKey(foodSpecies.Id))
                    {
                        _foodLiveCounts[foodSpecies.Id] = 0;
                    }

                    // Respawn only if below per-species cap (starting_number)
                    var currentLive = _foodLiveCounts[foodSpecies.Id];
                    var maxForSpecies = foodSpecies.StartingNumber;
                    if (currentLive < maxForSpecies && foodSpecies.SpawnRate > 0)
                    {
                        _foodRespawnTimers[foodSpecies.Id] += deltaF;
                        if (_foodRespawnTimers[foodSpecies.Id] >= foodSpecies.SpawnRate)
                        {
                            _foodRespawnTimers[foodSpecies.Id] = 0f;
                            SpawnEntity(foodSpecies, GetRandomPosition());
                        }
                    }
                }
            }
        }

        private Vector2 GetRandomPosition()
        {
            var margin = 200f;
            var x = GD.RandRange(margin, 3840 - margin);
            var y = GD.RandRange(margin, 2160 - margin);
            return new Vector2((float)x, (float)y);
        }

        private void PlayAmbienceSound(BugSquashStage stage)
        {
            // Check if we have an ambience sound to play
            if (string.IsNullOrEmpty(stage.AmbienceSound))
            {
                // If no ambience sound, fade out any current audio
                if (_currentAudioPlayer != null && _currentAudioPlayer.Playing)
                {
                    FadeOutAudio(_currentAudioPlayer);
                    _currentAudioPlayer = null;
                }
                return;
            }
            
            // Load the audio stream
            var audioStream = GD.Load<AudioStream>(stage.AmbienceSound);
            if (audioStream == null)
            {
                GD.PrintErr($"Failed to load ambience sound: {stage.AmbienceSound}");
                return;
            }
            
            // Cancel any existing fade tween
            _audioFadeTween?.Kill();
            
            // Determine which player to use
            AudioStreamPlayer newPlayer = null;
            if (_currentAudioPlayer == null)
            {
                // First time playing audio
                newPlayer = _audioPlayer1;
            }
            else if (_currentAudioPlayer == _audioPlayer1)
            {
                // Switch to player 2
                newPlayer = _audioPlayer2;
            }
            else
            {
                // Switch to player 1
                newPlayer = _audioPlayer1;
            }
            
            // Set up the new player
            newPlayer.Stream = audioStream;
            newPlayer.VolumeDb = -80.0f; // Start silent
            newPlayer.Play();
            
            // Create crossfade tween
            _audioFadeTween = CreateTween();
            _audioFadeTween.SetParallel(true);
            
            // Fade in new audio
            _audioFadeTween.TweenProperty(newPlayer, "volume_db", 0.0f, 3.0f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Quad);
            
            // Fade out previous audio if playing
            if (_currentAudioPlayer != null && _currentAudioPlayer.Playing)
            {
                var previousPlayer = _currentAudioPlayer;
                _audioFadeTween.TweenProperty(previousPlayer, "volume_db", -80.0f, 3.0f)
                    .SetEase(Tween.EaseType.In)
                    .SetTrans(Tween.TransitionType.Quad);
                
                // Stop the previous player after fade out
                _audioFadeTween.Chain().TweenCallback(Callable.From(() =>
                {
                    previousPlayer.Stop();
                }));
            }
            
            // Update current player reference
            _previousAudioPlayer = _currentAudioPlayer;
            _currentAudioPlayer = newPlayer;
        }
        
        private void FadeOutAudio(AudioStreamPlayer player)
        {
            if (player == null || !player.Playing) return;
            
            var tween = CreateTween();
            tween.TweenProperty(player, "volume_db", -80.0f, 0.5f)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Quad);
            tween.TweenCallback(Callable.From(() => player.Stop()));
        }
        
        private void StopAllAmbience()
        {
            _audioFadeTween?.Kill();
            
            if (_audioPlayer1 != null && _audioPlayer1.Playing)
            {
                FadeOutAudio(_audioPlayer1);
            }
            
            if (_audioPlayer2 != null && _audioPlayer2.Playing)
            {
                FadeOutAudio(_audioPlayer2);
            }
            
            _currentAudioPlayer = null;
            _previousAudioPlayer = null;
        }

        private void OnHomePressed()
        {
            // Stop ambience when leaving the game
            StopAllAmbience();
            
            GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
        }
        
        public override void _ExitTree()
        {
            // Stop all audio when exiting
            StopAllAmbience();
        }
    }
}