using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using InvasiveSpeciesAustralia;

public partial class MemoryMatchGame : Control
{
    private static readonly float IdentificationDisplayTime = 5.0f;
    private static readonly float MismatchPenalty = 3.0f;
    
    private ConfigLoader _configLoader;
    private List<Species> _allSpecies;
    private List<Species> _animals;
    private List<Species> _plants;
    private List<Species> _currentStageSpecies = new();
    private Queue<Species> _identificationQueue = new();
    
    private int _currentStage = 0;
    private int _animalsUsedCount = 0; // Track how many animals have been used
    private int _plantsUsedCount = 0; // Track how many plants have been used
    private float _gameTimer = 180.0f; // 3 minutes starting time
    private bool _isTimerRunning = false;
    private int _matchesSinceLastBonus = 0; // Track matches for bonus spawning
    private const int MatchesRequiredForBonus = 6;
    
    // UI References
    private GridContainer _cardGrid;
    private Label _timerLabel;
    private Label _stageLabel;
    private Control _identificationPanel;
    private TextureRect _identificationImage;
    private Label _identificationName;
    private RichTextLabel _identificationText;
    private Control _gameOverPanel;
    private Label _gameOverText;
    private Button _homeButton;
    
    // Card Management
    private List<MemoryCard> _cards = new();
    private List<MemoryCard> _selectedCards = new();  // Changed from individual card references
    private bool _canSelectCard = false;
    private int _matchedPairs = 0;
    
    // Timers
    private Timer _cardRevealTimer;
    private Timer _mismatchTimer;
    private Timer _identificationTimer;
    
    // Bonus objects layer
    private CanvasLayer _bonusLayer;
    
    public override void _Ready()
    {
        _configLoader = new ConfigLoader();
        _configLoader.LoadAllConfigs();
        _allSpecies = _configLoader.GetAllSpecies();
        
        // Separate animals and plants
        _animals = _allSpecies.Where(s => s.Type == "animals").ToList();
        _plants = _allSpecies.Where(s => s.Type == "plants").ToList();
        
        // Shuffle the lists
        _animals = _animals.OrderBy(x => GD.Randf()).ToList();
        _plants = _plants.OrderBy(x => GD.Randf()).ToList();
        
        // Get UI references
        _cardGrid = GetNode<GridContainer>("VBoxContainer/GameArea/CardGrid");
        _timerLabel = GetNode<Label>("VBoxContainer/Header/TimerLabel");
        _stageLabel = GetNode<Label>("VBoxContainer/Header/StageLabel");
        _identificationPanel = GetNode<Control>("IdentificationPanel");
        _identificationImage = GetNode<TextureRect>("IdentificationPanel/HBoxContainer/SpeciesImage");
        _identificationName = GetNode<Label>("IdentificationPanel/HBoxContainer/VBoxContainer/SpeciesName");
        _identificationText = GetNode<RichTextLabel>("IdentificationPanel/HBoxContainer/VBoxContainer/IdentificationText");
        _gameOverPanel = GetNode<Control>("GameOverPanel");
        _gameOverText = GetNode<Label>("GameOverPanel/VBoxContainer/GameOverText");
        _homeButton = GetNode<Button>("VBoxContainer/Header/HomeButton");
        
        // Create timers
        _cardRevealTimer = new Timer();
        _cardRevealTimer.OneShot = true;
        _cardRevealTimer.Timeout += OnCardRevealTimeout;
        AddChild(_cardRevealTimer);
        
        _mismatchTimer = new Timer();
        _mismatchTimer.WaitTime = 0.5f; // Short delay before flipping back
        _mismatchTimer.OneShot = true;
        _mismatchTimer.Timeout += OnMismatchTimeout;
        AddChild(_mismatchTimer);
        
        _identificationTimer = new Timer();
        _identificationTimer.WaitTime = IdentificationDisplayTime;
        _identificationTimer.OneShot = true;
        AddChild(_identificationTimer);
        
        // Connect home button
        _homeButton.Pressed += OnHomeButtonPressed;
        
        // Connect game over home button
        var gameOverHomeButton = GetNode<Button>("GameOverPanel/VBoxContainer/HomeButton");
        gameOverHomeButton.Pressed += OnHomeButtonPressed;
        
        // Hide panels initially
        _identificationPanel.Visible = false;
        _gameOverPanel.Visible = false;
        
        // Create bonus layer
        _bonusLayer = new CanvasLayer();
        _bonusLayer.Name = "BonusLayer";
        _bonusLayer.Layer = 10; // Render on top
        AddChild(_bonusLayer);
        
        // Start the game
        StartGame();
        
        // Add multi-touch debugger
        AddMultiTouchDebugger();
    }
    
    private void AddMultiTouchDebugger()
    {
        var debugger = new InvasiveSpeciesAustralia.Systems.MultiTouchDebugger();
        debugger.Name = "MultiTouchDebugger";
        AddChild(debugger);
    }
    
    public override void _Process(double delta)
    {
        if (_isTimerRunning)
        {
            _gameTimer -= (float)delta;
            if (_gameTimer <= 0)
            {
                _gameTimer = 0;
                GameOver(false);
            }
            UpdateTimerDisplay();
        }
    }
    
    private void StartGame()
    {
        _currentStage = 1; // Start at stage 1
        _animalsUsedCount = 0;
        _plantsUsedCount = 0;
        _gameTimer = 180.0f;
        _matchesSinceLastBonus = 0;
        ShowNextIdentification();
    }
    
    private void ShowNextIdentification()
    {
        // Calculate how many species we need for this stage
        // No maximum limit - let it grow naturally until all species are used
        int targetSpeciesCount = _currentStage + 1; // Stage 1 = 2 species, Stage 2 = 3 species, etc.
        
        // Build the species list for this stage by accumulating all species used so far
        _currentStageSpecies.Clear();
        
        // First, add all previously used species
        for (int i = 0; i < _animalsUsedCount && i < _animals.Count; i++)
        {
            _currentStageSpecies.Add(_animals[i]);
        }
        for (int i = 0; i < _plantsUsedCount && i < _plants.Count; i++)
        {
            _currentStageSpecies.Add(_plants[i]);
        }
        
        // Determine how many new species we need
        int currentSpeciesCount = _currentStageSpecies.Count;
        int newSpeciesNeeded = targetSpeciesCount - currentSpeciesCount;
        List<Species> newSpecies = new();
        
        GD.Print($"  Target species: {targetSpeciesCount}, Current: {currentSpeciesCount}, Need to add: {newSpeciesNeeded}");
        
        // Add new species (animals first, then plants)
        while (newSpeciesNeeded > 0 && _animalsUsedCount < _animals.Count)
        {
            var newAnimal = _animals[_animalsUsedCount];
            _currentStageSpecies.Add(newAnimal);
            newSpecies.Add(newAnimal);
            _animalsUsedCount++;
            newSpeciesNeeded--;
            GD.Print($"  Added animal: {newAnimal.Name}");
        }
        
        // If we've used all animals, start adding plants
        while (newSpeciesNeeded > 0 && _plantsUsedCount < _plants.Count)
        {
            var newPlant = _plants[_plantsUsedCount];
            _currentStageSpecies.Add(newPlant);
            newSpecies.Add(newPlant);
            _plantsUsedCount++;
            newSpeciesNeeded--;
        }
        
        // Debug log to track progression
        GD.Print($"Stage {_currentStage}: Total species = {_currentStageSpecies.Count}, Animals used = {_animalsUsedCount}/{_animals.Count}, Plants used = {_plantsUsedCount}/{_plants.Count}");
        
        // Show identification for new species
        if (newSpecies.Count > 0)
        {
            _identificationQueue = new Queue<Species>(newSpecies);
            ShowNextIdentificationFromQueue();
        }
        else
        {
            StartStage();
        }
    }
    
    private void ShowNextIdentificationFromQueue()
    {
        if (_identificationQueue.Count > 0)
        {
            var species = _identificationQueue.Dequeue();
            ShowIdentification(species);
        }
        else
        {
            StartStage();
        }
    }
    
    private void ShowIdentification(Species species)
    {
        _identificationPanel.Visible = true;
        _isTimerRunning = false;
        
        // Load species image
        var texture = GD.Load<Texture2D>(species.CardImage);
        _identificationImage.Texture = texture;
        
        // Set species name
        _identificationName.Text = species.Name;
        
        // Build identification text
        string identText = "[b]How to identify:[/b]\n";
        foreach (var trait in species.Identification)
        {
            identText += $"• {trait}\n";
        }
        _identificationText.Text = identText;
        
        // Clear previous timeout connections
        foreach (var connection in _identificationTimer.GetSignalConnectionList("timeout"))
        {
            _identificationTimer.Disconnect("timeout", connection["callable"].AsCallable());
        }
        
        // Start timer to hide panel
        _identificationTimer.Start();
        _identificationTimer.Timeout += OnIdentificationTimeout;
    }
    
    private void OnIdentificationTimeout()
    {
        _identificationPanel.Visible = false;
        ShowNextIdentificationFromQueue();
    }
    
    private void StartStage()
    {
        _stageLabel.Text = $"Stage {_currentStage}";
        _matchedPairs = 0;
        _isTimerRunning = true;
        
        // Clear any remaining bonus objects from previous stage
        ClearBonusObjects();
        
        // Clear existing cards
        foreach (var card in _cards)
        {
            card.QueueFree();
        }
        _cards.Clear();
        
        // Create card pairs
        CreateCards();
        
        // Show all cards face up
        foreach (var card in _cards)
        {
            card.ShowFace();
        }
        
        // Set reveal time based on number of species (1 second per species)
        _cardRevealTimer.WaitTime = _currentStageSpecies.Count;
        
        // Start timer to flip cards
        _canSelectCard = false;
        _cardRevealTimer.Start();
    }
    
    private void CreateCards()
    {
        // Determine grid layout based on number of cards
        int cardCount = _currentStageSpecies.Count * 2;
        int columns;
        
        // Layout logic - prefer wider grids (more columns than rows)
        // This will naturally make cards larger in early rounds and smaller in later rounds
        
        switch (cardCount)
        {
            case 4:  // 2x2
                columns = 2;
                break;
            case 6:  // 3x2
                columns = 3;
                break;
            case 8:  // 4x2
                columns = 4;
                break;
            case 10: // 5x2
                columns = 5;
                break;
            case 12: // 4x3
                columns = 4;
                break;
            case 14: // 7x2 - might be too wide for portrait cards
                columns = 5; // 5x3 is more balanced
                break;
            case 16: // 4x4
                columns = 6;
                break;
            case 18: // 6x3
                columns = 7;
                break;
            case 20: // 5x4
                columns = 6;
                break;
            default:
                // For larger counts, aim for roughly square but slightly wider
                columns = Mathf.CeilToInt(Mathf.Sqrt(cardCount * 1.1f));
                break;
        }
        
        _cardGrid.Columns = columns;
        
        // Create two cards for each species
        var cardSpecies = new List<Species>();
        foreach (var species in _currentStageSpecies)
        {
            cardSpecies.Add(species);
            cardSpecies.Add(species);
        }
        
        // Shuffle the cards
        cardSpecies = cardSpecies.OrderBy(x => GD.Randf()).ToList();
        
        // Create card instances
        foreach (var species in cardSpecies)
        {
            var card = new MemoryCard();
            card.SetSpecies(species);
            card.CardClicked += OnCardClicked;
            _cardGrid.AddChild(card);
            _cards.Add(card);
        }
        
        // Adjust card size based on grid
        AdjustCardSizes();
    }
    
    private void AdjustCardSizes()
    {
        // Get viewport size
        var viewportSize = GetViewportRect().Size;
        var gameAreaHeight = viewportSize.Y * 0.8f; // 80% of screen for game area
        var gameAreaWidth = viewportSize.X * 0.9f; // 90% of screen width
        
        int columns = _cardGrid.Columns;
        int rows = Mathf.CeilToInt(_cards.Count / (float)columns);
        
        // Calculate spacing based on card count - less spacing for more cards
        float baseSpacing = 30f;
        float spacingMultiplier = Math.Max(0.3f, 1.0f - (_cards.Count * 0.03f));
        float spacing = baseSpacing * spacingMultiplier;
        _cardGrid.AddThemeConstantOverride("h_separation", (int)spacing);
        _cardGrid.AddThemeConstantOverride("v_separation", (int)spacing);
        
        // Calculate available space for cards
        float availableWidth = gameAreaWidth - (columns - 1) * spacing;
        float availableHeight = gameAreaHeight - (rows - 1) * spacing;
        
        // Calculate card size based on available space
        float cardWidth = availableWidth / columns;
        float cardHeight = availableHeight / rows;
        
        // Maintain aspect ratio (800:1050 - actual card image dimensions)
        float targetAspect = 800.0f / 1050.0f; // ~0.762
        
        // Adjust to maintain aspect ratio
        if (cardWidth / cardHeight > targetAspect)
        {
            // Too wide, adjust width
            cardWidth = cardHeight * targetAspect;
        }
        else
        {
            // Too tall, adjust height
            cardHeight = cardWidth / targetAspect;
        }
        
        // Apply minimum size constraints (but no maximum - let early rounds have large cards)
        float minCardWidth = 80f;
        float minCardHeight = 105f; // Maintain aspect ratio for minimum size
        
        cardWidth = Math.Max(cardWidth, minCardWidth);
        cardHeight = Math.Max(cardHeight, minCardHeight);
        
        // For very early rounds with few cards, apply a reasonable maximum
        if (_cards.Count <= 6)
        {
            cardWidth = Math.Min(cardWidth, viewportSize.X * 0.25f); // Max 25% of screen width
            cardHeight = cardWidth / targetAspect;
        }
        
        // Apply size to all cards
        foreach (var card in _cards)
        {
            card.SetCardSize(new Vector2(cardWidth, cardHeight));
        }
    }
    
    private void OnCardRevealTimeout()
    {
        // Flip all cards to back
        foreach (var card in _cards)
        {
            card.ShowBack();
        }
        _canSelectCard = true;
    }
    
    private void OnCardClicked(MemoryCard card)
    {
        if (!_canSelectCard || card.IsMatched || card.IsFaceUp)
            return;
        
        // Add card to selected list if not already there
        if (!_selectedCards.Contains(card))
        {
            _selectedCards.Add(card);
            card.ShowFace();
            
            // Check if we have two cards selected
            if (_selectedCards.Count == 2)
            {
                _canSelectCard = false;
                CheckMatch();
            }
        }
    }
    
    private void CheckMatch()
    {
        if (_selectedCards.Count != 2)
            return;
            
        var firstCard = _selectedCards[0];
        var secondCard = _selectedCards[1];
        
        if (firstCard.SpeciesId == secondCard.SpeciesId)
        {
            // Match!
            firstCard.SetMatched();
            secondCard.SetMatched();
            _matchedPairs++;
            _matchesSinceLastBonus++;
            
            // Check if we should spawn a bonus object
            if (_matchesSinceLastBonus >= MatchesRequiredForBonus)
            {
                SpawnBonusObject();
                _matchesSinceLastBonus = 0;
            }
            
            ResetSelection();
            
            // Check if stage complete
            if (_matchedPairs == _currentStageSpecies.Count)
            {
                StageComplete();
            }
        }
        else
        {
            // No match - apply penalty
            _gameTimer -= MismatchPenalty;
            _mismatchTimer.Start();
        }
    }
    
    private void OnMismatchTimeout()
    {
        if (_selectedCards.Count >= 2)
        {
            _selectedCards[0].ShowBack();
            _selectedCards[1].ShowBack();
        }
        ResetSelection();
    }
    
    private void ResetSelection()
    {
        _selectedCards.Clear();
        _canSelectCard = true;
    }
    
    private void StageComplete()
    {
        _isTimerRunning = false;
        
        // Clean up any remaining bonus objects
        ClearBonusObjects();
        
        // Check if there are more species to play
        int totalSpeciesUsed = _animalsUsedCount + _plantsUsedCount;
        int totalSpeciesAvailable = _animals.Count + _plants.Count;
        
        if (totalSpeciesUsed < totalSpeciesAvailable)
        {
            // Increment stage BEFORE showing next identification
            _currentStage++;
            ShowNextIdentification();
        }
        else
        {
            GameOver(true);
        }
    }
    
    private void GameOver(bool won)
    {
        _isTimerRunning = false;
        
        // Clean up any remaining bonus objects
        ClearBonusObjects();
        
        _gameOverPanel.Visible = true;
        _gameOverText.Text = won ? "Congratulations!\nYou matched all species!" : "Time's Up!\nBetter luck next time!";
    }
    
    private void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(_gameTimer / 60);
        int seconds = Mathf.FloorToInt(_gameTimer % 60);
        _timerLabel.Text = $"Time: {minutes:00}:{seconds:00}";
        
        // Change color when time is low
        if (_gameTimer < 30)
        {
            _timerLabel.Modulate = Colors.Red;
        }
        else if (_gameTimer < 60)
        {
            _timerLabel.Modulate = Colors.Yellow;
        }
        else
        {
            _timerLabel.Modulate = Colors.White;
        }
    }
    
    private void OnHomeButtonPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }
    
    private void SpawnBonusObject()
    {
        var bonusObject = new BonusControl();
        bonusObject.BonusCollected += OnBonusCollected;
        
        // Add to the canvas layer
        _bonusLayer.AddChild(bonusObject);
    }
    
    private void OnBonusCollected()
    {
        // Add 5 seconds to the timer
        _gameTimer += 5.0f;
        
        // Play a positive sound effect here if desired
        GD.Print("Bonus collected! +5 seconds");
    }
    
    private void ClearBonusObjects()
    {
        // Remove all bonus objects from the bonus layer
        foreach (Node child in _bonusLayer.GetChildren())
        {
            if (child is BonusControl bonus)
            {
                bonus.QueueFree();
            }
        }
    }
} 