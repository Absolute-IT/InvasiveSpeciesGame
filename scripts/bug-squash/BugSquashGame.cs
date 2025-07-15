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
        private int _maxFoodCount = 0;
        private float _foodRespawnTimer = 0f;
        private List<BugSquashSpecies> _foodSpecies = new();
        
        // UI Elements
        private CanvasLayer _uiLayer;
        private Sprite2D _backgroundSprite;
        private Control _instructionScreen;
        private Control _gameScreen;
        private Label _scoreLabel;
        private Button _homeButton;
        private RichTextLabel _interactionDescLabel;
        private HBoxContainer _speciesContainer;
        
        // Effects
        private ShockwaveEffect _shockwaveEffect;
        
        // Camera and screen shake
        private Camera2D _camera;
        private float _shakeTimer = 0f;
        private float _shakeIntensity = 0f;
        private Vector2 _originalCameraPosition;

        private const float FOOD_RESPAWN_DELAY = 3f;
        private const float SCREEN_SHAKE_DURATION = 0.3f;
        private const float SCREEN_SHAKE_INTENSITY = 30f;

        public override void _Ready()
        {
            LoadStages();
            CreateUI();
            ShowInstructionScreen();
        }

        private void LoadStages()
        {
            _stages = ConfigLoader.LoadBugSquashStages();
            if (_stages == null || _stages.Count == 0)
            {
                GD.PrintErr("No bug squash stages found!");
                return;
            }
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

            // Score/status label
            _scoreLabel = new Label();
            _scoreLabel.AddThemeStyleboxOverride("normal", new StyleBoxFlat());
            _scoreLabel.Position = new Vector2(100, 100);
            _scoreLabel.AddThemeFontSizeOverride("font_size", 64);
            _scoreLabel.MouseFilter = Control.MouseFilterEnum.Ignore; // Don't block mouse input
            _gameScreen.AddChild(_scoreLabel);

            // Home button
            _homeButton = new Button();
            _homeButton.Text = "Home";
            _homeButton.Position = new Vector2(3840 - 400, 2160 - 200);
            _homeButton.Size = new Vector2(300, 160);
            _homeButton.AddThemeFontSizeOverride("font_size", 48);
            _homeButton.Pressed += OnHomePressed;
            _gameScreen.AddChild(_homeButton);

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

            // Content container
            var container = new VBoxContainer();
            container.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
            container.Position = new Vector2(-1200, -800);
            container.Size = new Vector2(2400, 1600);
            container.AddThemeConstantOverride("separation", 60);
            _instructionScreen.AddChild(container);

            // Title
            var titleLabel = new Label();
            titleLabel.Text = "How to Play";
            titleLabel.AddThemeFontSizeOverride("font_size", 96);
            titleLabel.AddThemeColorOverride("font_color", Colors.White);
            container.AddChild(titleLabel);

            // Interaction description
            _interactionDescLabel = new RichTextLabel();
            _interactionDescLabel.Name = "InteractionDescription";
            _interactionDescLabel.CustomMinimumSize = new Vector2(2400, 400);
            _interactionDescLabel.BbcodeEnabled = true;
            _interactionDescLabel.AddThemeFontSizeOverride("normal_font_size", 56);
            _interactionDescLabel.AddThemeColorOverride("font_color", Colors.White);
            _interactionDescLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            container.AddChild(_interactionDescLabel);

            // Species container
            _speciesContainer = new HBoxContainer();
            _speciesContainer.Name = "SpeciesContainer";
            // Increase separation for larger entities
            _speciesContainer.AddThemeConstantOverride("separation", 220);
            container.AddChild(_speciesContainer);

            // Start button
            var startButton = new Button();
            startButton.Text = "Start Level";
            startButton.CustomMinimumSize = new Vector2(600, 160);
            startButton.AddThemeFontSizeOverride("font_size", 64);
            startButton.Pressed += OnStartLevel;
            container.AddChild(startButton);
        }

        private void ShowInstructionScreen()
        {
            if (_currentStageIndex >= _stages.Count)
            {
                // Game complete
                GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
                return;
            }

            _currentStage = _stages[_currentStageIndex];
            _instructionScreen.Visible = true;
            _gameScreen.Visible = false;

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
                _interactionDescLabel.Text = _currentStage.InteractionDescription;
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
            entityContainer.CustomMinimumSize = new Vector2(400, 400);
            container.AddChild(entityContainer);

            // Colored ring
            var ring = new TextureRect();
            ring.ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional;
            ring.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            ring.Modulate = new Color(species.Color);
            ring.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
            // Double the size: 400x400 ring
            ring.Size = new Vector2(400, 400);
            ring.Position = new Vector2(-200, -200);
            
            // Create a white circle texture for the ring
            var image = Image.CreateEmpty(400, 400, false, Image.Format.Rgba8);
            for (int x = 0; x < 400; x++)
            {
                for (int y = 0; y < 400; y++)
                {
                    float dx = x - 200;
                    float dy = y - 200;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    // Double the outline thickness: 20px
                    if (dist < 200 && dist > 180)
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
                // Double the image size to fit the new ring (364x364, 10px border)
                sprite.Size = new Vector2(370, 370);
                sprite.Position = new Vector2(-185, -185);
                
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
            imageLabelSpacer.CustomMinimumSize = new Vector2(0, 40); // 80px vertical space
            container.AddChild(imageLabelSpacer);

            // Group the name and behaviour labels in their own VBox with small separation
            var labelBox = new VBoxContainer();
            labelBox.AddThemeConstantOverride("separation", 10); // Small gap between labels

            var nameLabel = new Label();
            nameLabel.Text = species.Name;
            nameLabel.AddThemeFontSizeOverride("font_size", 64);
            nameLabel.AddThemeColorOverride("font_color", Colors.White);
            nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
            labelBox.AddChild(nameLabel);

            var behaviorLabel = new Label();
            behaviorLabel.Text = $"({species.Behavior})";
            behaviorLabel.AddThemeFontSizeOverride("font_size", 48);
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

            _preyCount = 0;
            _predatorCount = 0;
            _foodCount = 0;
            _maxFoodCount = 0;
            _foodSpecies.Clear();

            // Spawn initial entities
            foreach (var species in _currentStage.Species)
            {
                var behavior = System.Enum.Parse<EntityBehavior>(species.Behavior);
                
                if (behavior == EntityBehavior.Food)
                {
                    _foodSpecies.Add(species);
                    _maxFoodCount = species.StartingNumber;
                }

                for (int i = 0; i < species.StartingNumber; i++)
                {
                    var position = GetRandomPosition();
                    SpawnEntity(species, position);
                }
            }

            UpdateScore();
        }

        public void SpawnEntity(BugSquashSpecies species, Vector2 position)
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
                    break;
            }

            UpdateScore();
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
                    break;
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
            
            // Play click sound effect if available
        }
        
        private void StartScreenShake()
        {
            _shakeTimer = SCREEN_SHAKE_DURATION;
            _shakeIntensity = SCREEN_SHAKE_INTENSITY;
        }

        private void UpdateScore()
        {
            _scoreLabel.Text = $"Invasive Species: {_predatorCount} | Native Species: {_preyCount}";
        }

        private void CheckWinCondition()
        {
            if (_preyCount <= 0 && _predatorCount > 0)
            {
                // Game over - native species extinct
                ShowGameOver(false);
            }
            else if (_predatorCount <= 0 && _preyCount > 0)
            {
                // Victory - invasive species eradicated
                ShowGameOver(true);
            }
        }

        private void ShowGameOver(bool victory)
        {
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
                "The native species has gone extinct.";
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

            // Handle food respawning
            if (_foodCount < _maxFoodCount && _foodSpecies.Count > 0)
            {
                _foodRespawnTimer += deltaF;
                if (_foodRespawnTimer >= FOOD_RESPAWN_DELAY)
                {
                    _foodRespawnTimer = 0;
                    var foodSpecies = _foodSpecies[GD.RandRange(0, _foodSpecies.Count - 1)];
                    SpawnEntity(foodSpecies, GetRandomPosition());
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

        private void OnHomePressed()
        {
            GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
        }
    }
} 