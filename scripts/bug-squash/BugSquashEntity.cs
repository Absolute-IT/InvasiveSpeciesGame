using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InvasiveSpeciesAustralia
{
    public partial class BugSquashEntity : Area2D
    {
        [Signal]
        public delegate void EntityDiedEventHandler(BugSquashEntity entity);

        [Signal]
        public delegate void EntityClickedEventHandler(BugSquashEntity entity);

        private BugSquashSpecies _species;
        private EntityBehavior _behavior;
        private List<BugSquashGoal> _goals;
        private Vector2 _velocity;
        private float _speed;
        private int _health;
        private int _maxHealth;
        private bool _isStunned = false;
        private float _stunTimer = 0f;
        private float _breedCooldownTimer = 0f;
        private float _spawnTimer = 0f;
        private float _feedingTimer = 0f;
        private bool _isFeeding = false;
        private bool _isBreeding = false;
        private bool _shouldSpawnOffspring = false;
        private BugSquashEntity _currentTarget;
        private Vector2 _damageSourcePosition;
        private float _behaviorUpdateTimer = 0f;
        private float _baseRadius = 80f; // Base radius, will be scaled by size
        private string _foodCreatesOnEaten = null; // Stores what entity to create after eating food

        private Sprite2D _sprite;
        private Sprite2D _ring;
        private AnimationPlayer _animationPlayer;

        private const float STUN_DURATION = 3f;
        private const float FEEDING_DURATION = 2f;
        private const float BREEDING_DURATION = 2f;
        private const float BEHAVIOR_UPDATE_INTERVAL = 0.5f;

        public BugSquashSpecies Species => _species;
        public EntityBehavior Behavior => _behavior;
        public bool IsAlive { get; private set; } = true;
        public float EntityRadius => _baseRadius * (_species?.Size ?? 100f) / 100f;

        public override void _Ready()
        {
            // Enable input detection for this Area2D
            InputPickable = true;
            Monitoring = true;
            Monitorable = true;
            
            // IMPORTANT: Set process mode to ensure we receive all input events
            ProcessMode = ProcessModeEnum.Always;
            
            // Ensure proper Z ordering - above background
            ZIndex = 0; // Default Z-index, will be above background which is -100
            
            // Create the visual structure first (collision will be set after initialization)
            CreateVisuals();

            // Connect input - use regular input event for Area2D
            InputEvent += OnInputEvent;
            MouseEntered += OnMouseEntered;
            MouseExited += OnMouseExited;
        }

        private void CreateVisuals()
        {
            // Create the sprite for the entity image (bottom layer)
            _sprite = new Sprite2D();
            _sprite.Position = Vector2.Zero;
            _sprite.ShowBehindParent = true; // Behind the ring
            
            // Apply circular clipping shader to the sprite
            var clipShaderPath = "res://shaders/circular_clip.gdshader";
            if (ResourceLoader.Exists(clipShaderPath))
            {
                var shader = GD.Load<Shader>(clipShaderPath);
                var shaderMaterial = new ShaderMaterial();
                shaderMaterial.Shader = shader;
                _sprite.Material = shaderMaterial;
            }
            AddChild(_sprite);

            // Create the colored ring on top using a sprite with a ring texture
            _ring = new Sprite2D();
            _ring.Position = Vector2.Zero;
            _ring.Modulate = Colors.White; // Will be modulated with species color
            
            AddChild(_ring);

            // Create animation player
            _animationPlayer = new AnimationPlayer();
            AddChild(_animationPlayer);
            CreateBlinkAnimation();
        }
        
        private ImageTexture CreateRingTexture(float radius)
        {
            int size = (int)(radius * 2);
            var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
            
            float center = size / 2f;
            float outerRadius = size / 2f;
            float innerRadius = outerRadius * 0.85f;
            
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (dist <= outerRadius && dist >= innerRadius)
                    {
                        image.SetPixel(x, y, Colors.White);
                    }
                    else
                    {
                        image.SetPixel(x, y, Colors.Transparent);
                    }
                }
            }
            
            return ImageTexture.CreateFromImage(image);
        }

        private void CreateBlinkAnimation()
        {
            var animation = new Animation();
            animation.Length = 0.3f;
            animation.LoopMode = Animation.LoopModeEnum.Pingpong;

            var track = animation.AddTrack(Animation.TrackType.Value);
            animation.TrackSetPath(track, new NodePath(".:modulate:a"));
            animation.TrackInsertKey(track, 0f, 1f);
            animation.TrackInsertKey(track, 0.15f, 0.3f);

            var animLib = new AnimationLibrary();
            animLib.AddAnimation("blink", animation);
            _animationPlayer.AddAnimationLibrary("default", animLib);
        }

        public void Initialize(BugSquashSpecies species, Vector2 startPosition)
        {
            if (species == null)
            {
                GD.PrintErr("BugSquashEntity.Initialize: species is null");
                return;
            }
            
            _species = species;
            _speed = species.Speed;
            _health = species.Health;
            _maxHealth = species.Health;
            _goals = species.Goals ?? new List<BugSquashGoal>();
            Position = startPosition;
            
            // Parse behavior
            if (!Enum.TryParse<EntityBehavior>(species.Behavior, out _behavior))
            {
                GD.PrintErr($"Unknown behavior: {species.Behavior}");
                _behavior = EntityBehavior.Food;
            }

            // Set up collision based on size
            var collisionShape = new CollisionShape2D();
            var circle = new CircleShape2D();
            circle.Radius = EntityRadius;
            collisionShape.Shape = circle;
            collisionShape.Position = Vector2.Zero;
            collisionShape.DebugColor = new Color(1, 0, 0, 0.3f); // Red debug color
            AddChild(collisionShape);

            // Create and set ring texture based on size
            if (_ring != null)
            {
                var ringTexture = CreateRingTexture(EntityRadius);
                _ring.Texture = ringTexture;
                _ring.Scale = Vector2.One; // Ring texture is already the correct size
                
                // Set ring color
                if (!string.IsNullOrEmpty(species.Color))
                {
                    var color = new Color(species.Color);
                    _ring.Modulate = color;
                    GD.Print($"Set ring color to {species.Color} for {species.Name}");
                }
            }

            // Set sprite texture and scale
            if (!string.IsNullOrEmpty(species.Image) && _sprite != null)
            {
                var texture = GD.Load<Texture2D>(species.Image);
                _sprite.Texture = texture;
                
                // Scale the sprite to fit within the entity radius
                if (texture != null)
                {
                    var textureSize = texture.GetSize();
                    var targetSize = EntityRadius * 1.8f; // Slightly smaller than full radius
                    var scale = targetSize / Mathf.Max(textureSize.X, textureSize.Y);
                    _sprite.Scale = new Vector2(scale, scale);
                    GD.Print($"Set sprite scale to {scale} for {species.Name} (texture size: {textureSize})");
                }
            }

            // Static entities don't move
            if (_behavior == EntityBehavior.Food || _behavior == EntityBehavior.Nest || _behavior == EntityBehavior.Weed)
            {
                _speed = 0;
            }
            
            // Initialize goal-specific timers
            InitializeGoalTimers();
            
            GD.Print($"Entity {species.Name} initialized at {Position}, Visible: {Visible}, Size: {species.Size}%, Health: {_health}");
        }

        private void InitializeGoalTimers()
        {
            foreach (var goal in _goals)
            {
                if (Enum.TryParse<GoalType>(goal.Type, out var goalType))
                {
                    switch (goalType)
                    {
                        case GoalType.Breed:
                            // Start with cooldown at 0 so entities must wait before breeding
                            _breedCooldownTimer = 0f;
                            break;
                        case GoalType.Spawn:
                            _spawnTimer = 0f;
                            break;
                    }
                }
            }
        }

        public override void _Process(double delta)
        {
            if (!IsAlive) return;

            var deltaF = (float)delta;

            // Update stun
            if (_isStunned)
            {
                _stunTimer -= deltaF;
                if (_stunTimer <= 0)
                {
                    _isStunned = false;
                    _animationPlayer.Stop();
                    Modulate = Colors.White;
                }
                else
                {
                    // Move away from damage source during stun
                    var awayDirection = (Position - _damageSourcePosition).Normalized();
                    _velocity = awayDirection * _speed * 0.5f;
                }
            }

            // Update feeding
            if (_isFeeding)
            {
                _feedingTimer -= deltaF;
                if (_feedingTimer <= 0)
                {
                    _isFeeding = false;
                    CompleteFeedingAndReproduce();
                }
            }

            // Update breeding
            if (_isBreeding)
            {
                _feedingTimer -= deltaF; // Reuse feeding timer for breeding
                if (_feedingTimer <= 0)
                {
                    _isBreeding = false;
                    CompleteBreeding();
                }
            }

            // Update goal timers
            UpdateGoalTimers(deltaF);

            // Update AI behavior
            if (!_isStunned && !_isFeeding && !_isBreeding)
            {
                _behaviorUpdateTimer += deltaF;
                if (_behaviorUpdateTimer >= BEHAVIOR_UPDATE_INTERVAL)
                {
                    _behaviorUpdateTimer = 0;
                    UpdateBehavior();
                }
            }

            // Move
            if (!_isFeeding && !_isBreeding && _speed > 0)
            {
                Position += _velocity * deltaF;
                KeepInBounds();
            }
        }

        private void UpdateGoalTimers(float delta)
        {
            // Update breed cooldown
            if (_breedCooldownTimer < GetBreedCooldown())
            {
                _breedCooldownTimer += delta;
            }

            // Update spawn timer for Spawn goals
            var spawnGoal = GetGoal(GoalType.Spawn);
            if (spawnGoal != null)
            {
                _spawnTimer += delta;
                if (_spawnTimer >= spawnGoal.Value)
                {
                    _spawnTimer = 0;
                    SpawnEntity(spawnGoal.Target);
                }
            }

            // Handle Food and Weed spawn mechanics based on behavior
            if ((_behavior == EntityBehavior.Food || _behavior == EntityBehavior.Weed) && _species.SpawnRate > 0)
            {
                // This is handled by the game manager for Food entities
                // Weed entities spawn nearby entities
                if (_behavior == EntityBehavior.Weed)
                {
                    _spawnTimer += delta;
                    if (_spawnTimer >= _species.SpawnRate)
                    {
                        _spawnTimer = 0;
                        SpawnNearbyWeed();
                    }
                }
            }
        }

        private void UpdateBehavior()
        {
            // Get current active goal
            var activeGoal = GetActiveGoal();
            
            if (activeGoal != null)
            {
                ExecuteGoal(activeGoal);
            }
            else
            {
                // Default behavior based on entity type
                switch (_behavior)
                {
                    case EntityBehavior.Predator:
                    case EntityBehavior.Prey:
                        Wander();
                        break;
                    // Static entities don't have default behavior
                }
            }
        }

        private BugSquashGoal GetActiveGoal()
        {
            // Priority order:
            // 1. Breed (if cooldown has passed AND there's an available partner)
            // 2. Kill or Eat (whichever target is nearest)
            // 3. Spawn (handled separately in timers)

            var breedGoal = GetGoal(GoalType.Breed);
            if (breedGoal != null && _breedCooldownTimer >= breedGoal.Value)
            {
                // Check if there's actually a breeding partner available
                var (partner, _) = FindNearestBreedingPartner(breedGoal.Target);
                if (partner != null)
                {
                    return breedGoal;
                }
            }

            // Get Kill and Eat goals
            var killGoal = GetGoal(GoalType.Kill);
            var eatGoal = GetGoal(GoalType.Eat);

            // Find nearest targets for each
            BugSquashEntity nearestKillTarget = null;
            BugSquashEntity nearestEatTarget = null;
            float nearestKillDist = float.MaxValue;
            float nearestEatDist = float.MaxValue;

            if (killGoal != null)
            {
                (nearestKillTarget, nearestKillDist) = FindNearestTarget(killGoal.Target);
            }

            if (eatGoal != null)
            {
                (nearestEatTarget, nearestEatDist) = FindNearestTarget(eatGoal.Target);
            }

            // Choose the goal with the nearest target
            if (nearestKillTarget != null && nearestEatTarget != null)
            {
                return nearestKillDist < nearestEatDist ? killGoal : eatGoal;
            }
            else if (nearestKillTarget != null)
            {
                return killGoal;
            }
            else if (nearestEatTarget != null)
            {
                return eatGoal;
            }

            return null;
        }

        private void ExecuteGoal(BugSquashGoal goal)
        {
            if (!Enum.TryParse<GoalType>(goal.Type, out var goalType))
            {
                return;
            }

            switch (goalType)
            {
                case GoalType.Kill:
                    ExecuteKillGoal(goal);
                    break;
                case GoalType.Eat:
                    ExecuteEatGoal(goal);
                    break;
                case GoalType.Breed:
                    ExecuteBreedGoal(goal);
                    break;
                // Spawn is handled in timer updates
            }
        }

        private void ExecuteKillGoal(BugSquashGoal goal)
        {
            var (target, distance) = FindNearestTarget(goal.Target);
            if (target == null) return;

            _currentTarget = target;
            SeekTarget(target);

            // Check if close enough to attack
            if (distance < EntityRadius + target.EntityRadius)
            {
                // Deal damage
                target.TakeDamage(Position);
                
                // If target died, clear it
                if (!target.IsAlive)
                {
                    _currentTarget = null;
                }
            }
        }

        private void ExecuteEatGoal(BugSquashGoal goal)
        {
            var (target, distance) = FindNearestTarget(goal.Target);
            if (target == null) return;

            _currentTarget = target;
            
            // Prey should flee from nearby predators
            if (_behavior == EntityBehavior.Prey)
            {
                var (nearestPredator, predatorDist) = FindNearestPredator();
                if (nearestPredator != null && predatorDist < 400f)
                {
                    // Flee from predator instead
                    var fleeDirection = (Position - nearestPredator.Position).Normalized();
                    _velocity = fleeDirection * _speed;
                    return;
                }
            }

            SeekTarget(target);

            // Check if close enough to eat
            if (distance < EntityRadius + target.EntityRadius)
            {
                StartFeeding(target);
            }
        }

        private void ExecuteBreedGoal(BugSquashGoal goal)
        {
            var (target, distance) = FindNearestBreedingPartner(goal.Target);
            if (target == null) return;

            _currentTarget = target;
            SeekTarget(target);

            // Check if close enough to breed
            if (distance < EntityRadius + target.EntityRadius)
            {
                // Both entities should already be ready (checked in FindNearestBreedingPartner)
                // This entity is the initiator and will spawn the offspring
                StartBreeding(true);
                // The target just participates but doesn't spawn
                target.StartBreeding(false);
            }
        }

        private (BugSquashEntity, float) FindNearestBreedingPartner(string targetId)
        {
            var gameNode = GetParent();
            BugSquashEntity nearest = null;
            float nearestDist = float.MaxValue;

            foreach (Node child in gameNode.GetChildren())
            {
                if (child is BugSquashEntity entity && entity != this && entity.IsAlive)
                {
                    if (entity.Species.Id == targetId)
                    {
                        // Check if this entity also has a breed goal and is ready to breed
                        var targetBreedGoal = entity.GetGoal(GoalType.Breed);
                        if (targetBreedGoal != null && entity._breedCooldownTimer >= targetBreedGoal.Value)
                        {
                            // Also check if they're not already breeding
                            if (!entity._isBreeding)
                            {
                                float dist = Position.DistanceTo(entity.Position);
                                if (dist < nearestDist)
                                {
                                    nearest = entity;
                                    nearestDist = dist;
                                }
                            }
                        }
                    }
                }
            }

            return (nearest, nearestDist);
        }

        private (BugSquashEntity, float) FindNearestTarget(string targetId)
        {
            var gameNode = GetParent();
            BugSquashEntity nearest = null;
            float nearestDist = float.MaxValue;

            foreach (Node child in gameNode.GetChildren())
            {
                if (child is BugSquashEntity entity && entity != this && entity.IsAlive)
                {
                    if (entity.Species.Id == targetId)
                    {
                        float dist = Position.DistanceTo(entity.Position);
                        if (dist < nearestDist)
                        {
                            nearest = entity;
                            nearestDist = dist;
                        }
                    }
                }
            }

            return (nearest, nearestDist);
        }

        private (BugSquashEntity, float) FindNearestPredator()
        {
            var gameNode = GetParent();
            BugSquashEntity nearest = null;
            float nearestDist = float.MaxValue;

            foreach (Node child in gameNode.GetChildren())
            {
                if (child is BugSquashEntity entity && entity != this && entity.IsAlive)
                {
                    if (entity.Behavior == EntityBehavior.Predator)
                    {
                        float dist = Position.DistanceTo(entity.Position);
                        if (dist < nearestDist)
                        {
                            nearest = entity;
                            nearestDist = dist;
                        }
                    }
                }
            }

            return (nearest, nearestDist);
        }

        private BugSquashGoal GetGoal(GoalType type)
        {
            return _goals.FirstOrDefault(g => g.Type == type.ToString());
        }

        private float GetBreedCooldown()
        {
            var breedGoal = GetGoal(GoalType.Breed);
            return breedGoal?.Value ?? float.MaxValue;
        }

        private void SeekTarget(BugSquashEntity target)
        {
            var direction = (target.Position - Position).Normalized();
            var distance = Position.DistanceTo(target.Position);
            
            // Slow down when getting close to avoid overshooting
            var arrivalRadius = EntityRadius * 3;
            if (distance < arrivalRadius)
            {
                // Linear slowdown as we approach the target
                var slowFactor = distance / arrivalRadius;
                _velocity = direction * _speed * slowFactor;
            }
            else
            {
                _velocity = direction * _speed;
            }
        }

        private void Wander()
        {
            // Add some random movement
            var randomAngle = GD.Randf() * Mathf.Pi * 2;
            var randomDirection = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
            _velocity = (_velocity * 0.9f + randomDirection * _speed * 0.1f).Normalized() * _speed;
        }

        private void KeepInBounds()
        {
            var viewportSize = GetViewportRect().Size;
            var margin = EntityRadius;

            if (Position.X < margin)
            {
                Position = new Vector2(margin, Position.Y);
                _velocity.X = Mathf.Abs(_velocity.X);
            }
            else if (Position.X > viewportSize.X - margin)
            {
                Position = new Vector2(viewportSize.X - margin, Position.Y);
                _velocity.X = -Mathf.Abs(_velocity.X);
            }

            if (Position.Y < margin)
            {
                Position = new Vector2(Position.X, margin);
                _velocity.Y = Mathf.Abs(_velocity.Y);
            }
            else if (Position.Y > viewportSize.Y - margin)
            {
                Position = new Vector2(Position.X, viewportSize.Y - margin);
                _velocity.Y = -Mathf.Abs(_velocity.Y);
            }
        }

        private void StartFeeding(BugSquashEntity food)
        {
            _isFeeding = true;
            _feedingTimer = FEEDING_DURATION;
            _velocity = Vector2.Zero;
            
            // Store the food's creates_on_eaten property before it dies
            _foodCreatesOnEaten = food.Species?.CreatesOnEaten;
            
            food.Die();
        }

        private void CompleteFeedingAndReproduce()
        {
            // If the food specified what to create, spawn that entity
            if (!string.IsNullOrEmpty(_foodCreatesOnEaten))
            {
                CallDeferred(nameof(SpawnSpecificEntity), _foodCreatesOnEaten);
            }
            else
            {
                // Default behavior: spawn offspring of the same type as the eater
                CallDeferred(nameof(SpawnOffspring));
            }
            
            // Clear the stored value
            _foodCreatesOnEaten = null;
        }

        private void StartBreeding(bool shouldSpawnOffspring = false)
        {
            _isBreeding = true;
            _feedingTimer = BREEDING_DURATION; // Reuse feeding timer
            _velocity = Vector2.Zero;
            _breedCooldownTimer = 0; // Reset cooldown
            _shouldSpawnOffspring = shouldSpawnOffspring;
        }

        private void CompleteBreeding()
        {
            // Only spawn offspring if this entity is the initiator
            if (_shouldSpawnOffspring)
            {
                CallDeferred(nameof(SpawnOffspring));
            }
            _shouldSpawnOffspring = false; // Reset flag
        }

        private void SpawnOffspring()
        {
            var parent = GetParent();
            if (parent is BugSquashGame game)
            {
                game.SpawnEntity(_species, Position + new Vector2(EntityRadius * 2, 0));
            }
        }

        private void SpawnSpecificEntity(string entityId)
        {
            var parent = GetParent();
            if (parent is BugSquashGame game)
            {
                var targetSpecies = game.GetSpeciesById(entityId);
                if (targetSpecies != null)
                {
                    var spawnPos = Position + new Vector2(EntityRadius * 2, 0);
                    game.SpawnEntity(targetSpecies, spawnPos);
                }
                else
                {
                    GD.PrintErr($"Could not find species with ID: {entityId}");
                }
            }
        }

        private void SpawnEntity(string entityId)
        {
            var parent = GetParent();
            if (parent is BugSquashGame game)
            {
                // Find the species data for the target entity
                var targetSpecies = game.GetSpeciesById(entityId);
                if (targetSpecies != null)
                {
                    game.SpawnEntity(targetSpecies, Position);
                }
            }
        }

        private void SpawnNearbyWeed()
        {
            var parent = GetParent();
            if (parent is BugSquashGame game)
            {
                // Spawn another weed nearby
                var angle = GD.Randf() * Mathf.Pi * 2;
                var distance = EntityRadius * 2.5f + GD.Randf() * EntityRadius;
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                var spawnPos = Position + offset;
                
                // Keep within bounds
                var viewportSize = GetViewportRect().Size;
                spawnPos.X = Mathf.Clamp(spawnPos.X, EntityRadius, viewportSize.X - EntityRadius);
                spawnPos.Y = Mathf.Clamp(spawnPos.Y, EntityRadius, viewportSize.Y - EntityRadius);
                
                game.SpawnEntity(_species, spawnPos);
            }
        }

        public void TakeDamage(Vector2 damageSource)
        {
            if (_isStunned) return; // Can't take damage while stunned

            _health--;
            _damageSourcePosition = damageSource;
            
            GD.Print($"{_species?.Name} took damage. Health: {_health}/{_maxHealth}");
            
            if (_health <= 0)
            {
                Die();
            }
            else
            {
                // Start stun effect
                _isStunned = true;
                _stunTimer = STUN_DURATION;
                Modulate = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                _animationPlayer.Play("default/blink");
            }
        }

        private void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx)
        {
            // Handle touch input first for better responsiveness on multi-touch screens
            if (@event is InputEventScreenTouch touchEvent && touchEvent.Pressed)
            {
                GD.Print($"Touch {touchEvent.Index} detected on entity: {_species?.Name}");
                HandleClick();
                // Don't consume the event - allow other entities to receive the same touch
                return;
            }
            // Handle mouse input (including synthetic mouse events from touch)
            else if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                GD.Print($"Click detected on entity: {_species?.Name}");
                HandleClick();
                // Don't consume the event - allow other entities to receive the same click
                return;
            }
        }
        
        private void OnMouseEntered()
        {
            GD.Print($"Mouse entered entity: {_species?.Name}");
        }
        
        private void OnMouseExited()
        {
            GD.Print($"Mouse exited entity: {_species?.Name}");
        }

        private void HandleClick()
        {
            // Don't process clicks while stunned
            if (_isStunned)
            {
                GD.Print($"Entity {_species?.Name} is stunned, ignoring click");
                return;
            }
            
            GD.Print($"Entity clicked: {_species?.Name} ({_behavior})");
            EmitSignal(SignalName.EntityClicked, this);
            
            // Take damage from player click
            TakeDamage(GetGlobalMousePosition());
        }

        public void Die()
        {
            if (!IsAlive) return;

            IsAlive = false;
            EmitSignal(SignalName.EntityDied, this);
            
            // Create paint splatter effect before removing entity
            // var splatterEffect = new PaintSplatterEffect();
            // GetParent().AddChild(splatterEffect);
            // splatterEffect.TriggerSplatter(this);
            
            // Hide the entity immediately (splatter effect will show instead)
            Visible = false;
            
            // Queue free after a short delay to ensure splatter effect has captured the entity
            GetTree().CreateTimer(0.1).Timeout += () => QueueFree();
        }
    }
} 