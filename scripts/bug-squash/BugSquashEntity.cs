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
        private int _fullHealth; // Stores species-defined full health for restoration after growth
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
        private Vector2 _eatenEntityPosition = Vector2.Zero; // Position where the eaten entity was located
        private BugSquashEntity _foodBeingEaten = null; // Reference to the food currently being eaten
        private BugSquashEntity _beingConsumedBy = null; // If this entity is food, tracks the eater that reserved it
        private bool _isFrozen = false; // When true, entity stops moving/processing on stage end

        private Sprite2D _sprite;
        private Sprite2D _ring;
        private AnimationPlayer _animationPlayer;

        // Status text effect during actions
        private ActionStatusText _actionStatusText;
        private enum EntityAction { None, Eating, Breeding, Growing }
        private EntityAction _currentAction = EntityAction.None;
        private float _currentActionDuration = 0f;

        private const float STUN_DURATION = 3f;
        private const float FEEDING_DURATION = 2f;
        private const float BREEDING_DURATION = 2f;
        private const float BEHAVIOR_UPDATE_INTERVAL = 0.5f;

        // Weed growth lifecycle
        private bool _isGrowing = false;
        private float _growthTimer = 0f;
        private float _growthDuration = 0f;
        private BugSquashEntity _weedParent = null;
        private BugSquashEntity _currentGrowingChild = null;

        public BugSquashSpecies Species => _species;
        public EntityBehavior Behavior => _behavior;
        public bool IsAlive { get; private set; } = true;
        public bool IsStunned => _isStunned;
        public float EntityRadius => _baseRadius * (_species?.Size ?? 100f) / 100f;
        public bool IsBeingConsumed => _beingConsumedBy != null;
        public bool IsGrowing => _isGrowing;
        public bool CountsAsNativeForScore => _species != null && _behavior == EntityBehavior.Food && _species.ConsiderNativeSpecies;
        public bool IsFrozen => _isFrozen;

        public void SetFrozen(bool frozen)
        {
            _isFrozen = frozen;
            if (_isFrozen)
            {
                _velocity = Vector2.Zero;
            }
        }

        private bool TryReserveForConsumption(BugSquashEntity eater)
        {
            if (_beingConsumedBy == null)
            {
                _beingConsumedBy = eater;
                return true;
            }
            return _beingConsumedBy == eater;
        }

        private void ClearConsumptionReservation(BugSquashEntity eater)
        {
            if (_beingConsumedBy == eater)
            {
                _beingConsumedBy = null;
            }
        }

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

        private void SetCurrentTarget(BugSquashEntity newTarget)
        {
            if (_currentTarget == newTarget) return;
            if (_currentTarget != null)
            {
                _currentTarget.EntityDied -= OnCurrentTargetDied;
            }
            _currentTarget = newTarget;
            if (_currentTarget != null)
            {
                _currentTarget.EntityDied += OnCurrentTargetDied;
            }
        }

        private void OnCurrentTargetDied(BugSquashEntity deadTarget)
        {
            if (deadTarget != _currentTarget) return;

            // Cancel any ongoing actions tied to this target so movement doesn't stall
            if (_isFeeding)
            {
                EndActionStatus();
                _isFeeding = false;
                _feedingTimer = 0f;
                if (_foodBeingEaten != null)
                {
                    _foodBeingEaten.ClearConsumptionReservation(this);
                    _foodBeingEaten = null;
                }
                // Clear any pending spawn data captured at feeding start
                _foodCreatesOnEaten = null;
                _eatenEntityPosition = Vector2.Zero;
            }
            if (_isBreeding)
            {
                EndActionStatus();
                _isBreeding = false;
                _feedingTimer = 0f;
                _shouldSpawnOffspring = false;
            }

            // Detach from old target
            SetCurrentTarget(null);

            // Immediately select and pursue a new goal/target so the entity keeps moving
            var nextGoal = GetActiveGoal();
            if (nextGoal != null)
            {
                ExecuteGoal(nextGoal);
            }
            else
            {
                // Fall back to wandering to avoid stopping
                Wander();
            }

            _behaviorUpdateTimer = 0f;
        }

        public override void _Draw()
        {
            // Draw a health bar below the entity when damaged
            if (!IsAlive || _maxHealth <= 0) return;

            if (_health < _maxHealth)
            {
                float healthRatio = Mathf.Clamp((float)_health / (float)_maxHealth, 0f, 1f);

                float barWidth = EntityRadius * 1.6f;
                float barHeight = Mathf.Max(6f, EntityRadius * 0.15f);
                float verticalOffset = EntityRadius * 0.25f; // gap below the ring

                float left = -barWidth / 2f;
                float top = EntityRadius + verticalOffset;

                // Background (missing health) in red
                DrawRect(new Rect2(left, top, barWidth, barHeight), new Color(0.65f, 0.1f, 0.1f, 0.9f), true);

                // Foreground (current health) in green
                float greenWidth = barWidth * healthRatio;
                if (greenWidth > 0)
                {
                    DrawRect(new Rect2(left, top, greenWidth, barHeight), new Color(0.15f, 0.8f, 0.2f, 0.95f), true);
                }

                // Optional subtle border for readability
                DrawRect(new Rect2(left, top, barWidth, barHeight), new Color(0f, 0f, 0f, 0.7f), false, 2f);
            }
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
            _fullHealth = species.Health;
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
            // Ensure visuals (including health bar) are drawn once initialized
            QueueRedraw();
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
            if (_isFrozen) return;

            var deltaF = (float)delta;

            // Handle growth fade-in for weeds
            if (_isGrowing)
            {
                _growthTimer -= deltaF;
                var remaining = Mathf.Max(_growthTimer, 0f);
                var progress = _growthDuration > 0f ? 1f - (remaining / _growthDuration) : 1f;
                SetVisualAlpha(progress);
                if (_growthTimer <= 0f)
                {
                    CompleteGrowth();
                }
            }

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
                else
                {
                    UpdateActionStatusProgress();
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
                else
                {
                    UpdateActionStatusProgress();
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

            // Handle Food spawn mechanics based on behavior
            // Weeds are now handled by explicit growth cycle and should not auto-spawn here
            if (_behavior == EntityBehavior.Food && _species.SpawnRate > 0)
            {
                // Food spawn is managed by the game; keep entity-side logic off for Food too
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

            // Find nearest target across all Kill and Eat goals independently
            var (nearestKillTarget, nearestKillDist) = FindNearestTargetAcrossGoals(GoalType.Kill);
            // When considering Eat goals, ignore weeds that are stunned or still growing
            var (nearestEatTarget, nearestEatDist) = FindNearestTargetAcrossGoals(
                GoalType.Eat,
                candidate => !(candidate.Behavior == EntityBehavior.Weed && (candidate.IsStunned || candidate.IsGrowing))
            );

            // Choose to execute the goal TYPE whose nearest target is closer
            if (nearestKillTarget != null && nearestEatTarget != null)
            {
                // Return any goal instance of that type; execution will re-evaluate nearest target across goals
                return nearestKillDist < nearestEatDist ? GetGoals(GoalType.Kill).FirstOrDefault() : GetGoals(GoalType.Eat).FirstOrDefault();
            }
            else if (nearestKillTarget != null)
            {
                return GetGoals(GoalType.Kill).FirstOrDefault();
            }
            else if (nearestEatTarget != null)
            {
                return GetGoals(GoalType.Eat).FirstOrDefault();
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
            // Select nearest target across all Kill goals to avoid bias toward a single target type
            var (target, distance) = FindNearestTargetAcrossGoals(GoalType.Kill);
            if (target == null) return;

            SetCurrentTarget(target);
            SeekTarget(target);

            // Check if close enough to attack
            if (distance < EntityRadius + target.EntityRadius)
            {
                // Deal damage
                target.TakeDamage(Position);
                
                // If target died, clear it
                if (!target.IsAlive)
                {
                    SetCurrentTarget(null);
                }
            }
        }

        private void ExecuteEatGoal(BugSquashGoal goal)
        {
            // Select nearest target across all Eat goals, ignoring stunned or growing weeds so we don't stall
            var (target, distance) = FindNearestTargetAcrossGoals(
                GoalType.Eat,
                candidate => !(candidate.Behavior == EntityBehavior.Weed && (candidate.IsStunned || candidate.IsGrowing))
            );
            if (target == null) return;

            SetCurrentTarget(target);
            
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

            // If the chosen target is a stunned or growing weed, reselect or keep moving
            if (target.Behavior == EntityBehavior.Weed && (target.IsStunned || target.IsGrowing))
            {
                var (altTarget, altDist) = FindNearestTargetAcrossGoals(
                    GoalType.Eat,
                    candidate => !(candidate.Behavior == EntityBehavior.Weed && (candidate.IsStunned || candidate.IsGrowing))
                );
                if (altTarget != null)
                {
                    SetCurrentTarget(altTarget);
                    SeekTarget(altTarget);
                    return;
                }
                // No alternative targets; wander to avoid stopping
                Wander();
                return;
            }

            // Check if close enough to eat
            if (distance < EntityRadius + target.EntityRadius)
            {
                // Start feeding (for weeds, prey will die after feeding completes)
                StartFeeding(target);
            }
        }

        private void ExecuteBreedGoal(BugSquashGoal goal)
        {
            var (target, distance) = FindNearestBreedingPartner(goal.Target);
            if (target == null) return;

            SetCurrentTarget(target);
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
                        // Skip this target if it is already being consumed by another eater
                        if (entity.IsBeingConsumed && entity._beingConsumedBy != this)
                        {
                            continue;
                        }
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

        private (BugSquashEntity, float) FindNearestTarget(string targetId, Func<BugSquashEntity, bool> candidateFilter)
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
                        // Skip this target if it is already being consumed by another eater
                        if (entity.IsBeingConsumed && entity._beingConsumedBy != this)
                        {
                            continue;
                        }
                        if (candidateFilter != null && !candidateFilter(entity))
                        {
                            continue;
                        }
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

        private List<BugSquashGoal> GetGoals(GoalType type)
        {
            return _goals.Where(g => g.Type == type.ToString()).ToList();
        }

        private (BugSquashEntity, float) FindNearestTargetAcrossGoals(GoalType type, Func<BugSquashEntity, bool> candidateFilter = null)
        {
            var goals = GetGoals(type);
            BugSquashEntity nearest = null;
            float nearestDist = float.MaxValue;
            foreach (var goal in goals)
            {
                var (candidate, dist) = candidateFilter == null
                    ? FindNearestTarget(goal.Target)
                    : FindNearestTarget(goal.Target, candidateFilter);
                if (candidate != null && dist < nearestDist)
                {
                    nearest = candidate;
                    nearestDist = dist;
                }
            }
            return (nearest, nearestDist);
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
            if (food == null || !food.IsAlive)
            {
                return;
            }
            // Do not allow feeding on weeds that are stunned or still growing
            if (food.Behavior == EntityBehavior.Weed && (food.IsStunned || food.IsGrowing))
            {
                return;
            }
            // Ensure only one eater can consume this food at a time
            if (!food.TryReserveForConsumption(this))
            {
                return;
            }

            _isFeeding = true;
            _feedingTimer = FEEDING_DURATION;
            _velocity = Vector2.Zero;
            _foodBeingEaten = food;

            // Setup action status text (Eating) - only if this entity is the initiator
            // For eating, the eater is the initiator; show one shared status centered between entities
            BeginActionStatus(EntityAction.Eating, FEEDING_DURATION, "Eating…", partner: food);

            // Store the food's creates_on_eaten property before it is removed
            _foodCreatesOnEaten = food.Species?.CreatesOnEaten;
            // Remember the precise position of the eaten entity so spawns occur exactly there
            _eatenEntityPosition = food.Position;
        }

        private void CompleteFeedingAndReproduce()
        {
            // If the food is a weed, the eater (prey) dies after finishing eating and the weed remains
            if (_foodBeingEaten != null && _foodBeingEaten.Behavior == EntityBehavior.Weed)
            {
                // Finish UI/status
                EndActionStatus();
                // Release the reservation so others can interact with the weed
                _foodBeingEaten.ClearConsumptionReservation(this);
                _foodBeingEaten = null;
                // Kill the eater
                Die();
                return;
            }

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

            // Finish action status text
            EndActionStatus();

            // After spawning, remove the eaten food and release the reservation
            if (_foodBeingEaten != null)
            {
                CallDeferred(nameof(FinalizeFoodConsumption));
            }
        }

        private void FinalizeFoodConsumption()
        {
            if (_foodBeingEaten != null)
            {
                _foodBeingEaten.ClearConsumptionReservation(this);
                if (_foodBeingEaten.IsAlive)
                {
                    _foodBeingEaten.Die();
                }
                _foodBeingEaten = null;
            }
        }

        private void StartBreeding(bool shouldSpawnOffspring = false)
        {
            _isBreeding = true;
            _feedingTimer = BREEDING_DURATION; // Reuse feeding timer
            _velocity = Vector2.Zero;
            _breedCooldownTimer = 0; // Reset cooldown
            _shouldSpawnOffspring = shouldSpawnOffspring;

            // Setup action status text (Breeding) only for the initiator
            if (shouldSpawnOffspring)
            {
                // The target was set as _currentTarget by ExecuteBreedGoal; use it as partner
                BeginActionStatus(EntityAction.Breeding, BREEDING_DURATION, "Breeding…", partner: _currentTarget);
            }
        }

        private void CompleteBreeding()
        {
            // Only spawn offspring if this entity is the initiator
            if (_shouldSpawnOffspring)
            {
                CallDeferred(nameof(SpawnOffspring));
            }
            _shouldSpawnOffspring = false; // Reset flag

            // Finish action status text
            EndActionStatus();
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
                    // Spawn exactly where the eaten entity was located
                    var spawnPos = _eatenEntityPosition;
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

        // ============ Weed Growth System ============
        public void StartWeedGrowthCycle()
        {
            if (!IsAlive) return;
            if (_behavior != EntityBehavior.Weed) return;
            // Only one growing child at a time
            if (_currentGrowingChild != null && IsInstanceValid(_currentGrowingChild) && _currentGrowingChild.IsAlive && _currentGrowingChild._isGrowing)
            {
                return;
            }
            SpawnWeedChildNearSelf();
        }

        private void SpawnWeedChildNearSelf()
        {
            var parent = GetParent();
            if (parent is not BugSquashGame game) return;

            // Compute a nearby spawn position
            var angle = GD.Randf() * Mathf.Pi * 2;
            var distance = EntityRadius * 2.5f + GD.Randf() * EntityRadius;
            var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            var spawnPos = Position + offset;

            // Keep within bounds
            var viewportSize = GetViewportRect().Size;
            spawnPos.X = Mathf.Clamp(spawnPos.X, EntityRadius, viewportSize.X - EntityRadius);
            spawnPos.Y = Mathf.Clamp(spawnPos.Y, EntityRadius, viewportSize.Y - EntityRadius);

            // Spawn as growth child so it begins fading in over spawn_rate
            var child = game.SpawnEntity(_species, spawnPos, isGrowthSpawn: true);
            if (child != null)
            {
                _currentGrowingChild = child;
                child.SetWeedParent(this);
                child.EntityDied += OnWeedChildDied;
            }
        }

        public void BeginGrowth(float growthDuration)
        {
            // Called by the game for newly spawned growth children
            _isGrowing = true;
            _growthDuration = growthDuration > 0f ? growthDuration : 3f;
            _growthTimer = _growthDuration;

            // Fade in visuals from 0 to 1 alpha over the growth duration
            SetVisualAlpha(0f);

            // While growing, make the weed fragile: 1 hp, kills in one hit
            _health = 1;
            _maxHealth = 1;
            QueueRedraw();
        }

        public void SetWeedParent(BugSquashEntity parentEntity)
        {
            _weedParent = parentEntity;
        }

        private void CompleteGrowth()
        {
            _isGrowing = false;
            SetVisualAlpha(1f);

            // Restore full health now that the weed is fully grown
            _maxHealth = _fullHealth;
            _health = _fullHealth;
            QueueRedraw();

            // Start this weed's own growth cycle immediately
            StartWeedGrowthCycle();

            // Notify parent (if alive) that growth completed so it can start the next child
            if (_weedParent != null && _weedParent.IsAlive)
            {
                _weedParent.NotifyWeedChildGrowthCompleted(this);
            }
            _weedParent = null;
        }

        private void NotifyWeedChildGrowthCompleted(BugSquashEntity child)
        {
            if (_currentGrowingChild == child)
            {
                _currentGrowingChild = null;
            }
            if (IsAlive)
            {
                StartWeedGrowthCycle();
            }
        }

        private void OnWeedChildDied(BugSquashEntity child)
        {
            if (_currentGrowingChild == child)
            {
                _currentGrowingChild = null;
                if (IsAlive)
                {
                    // Immediately start a new growth attempt
                    StartWeedGrowthCycle();
                }
            }
        }

        private void SetVisualAlpha(float alpha)
        {
            alpha = Mathf.Clamp(alpha, 0f, 1f);
            if (_sprite != null)
            {
                var c = _sprite.Modulate;
                _sprite.Modulate = new Color(c.R, c.G, c.B, alpha);
            }
            if (_ring != null)
            {
                var rc = _ring.Modulate;
                _ring.Modulate = new Color(rc.R, rc.G, rc.B, alpha);
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

            // Update health bar rendering
            QueueRedraw();
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
            // Unsubscribe from target to avoid dangling handlers
            if (_currentTarget != null)
            {
                _currentTarget.EntityDied -= OnCurrentTargetDied;
                _currentTarget = null;
            }

            // If this entity dies while consuming food, release the reservation so others can eat it
            if (_isFeeding && _foodBeingEaten != null)
            {
                _foodBeingEaten.ClearConsumptionReservation(this);
                _foodBeingEaten = null;
            }

            IsAlive = false;
            EmitSignal(SignalName.EntityDied, this);
            
            // Finish action status text immediately
            if (_actionStatusText != null && IsInstanceValid(_actionStatusText))
            {
                _actionStatusText.Finish();
                _actionStatusText = null;
            }
            
            // Create paint splatter effect before removing entity
            // var splatterEffect = new PaintSplatterEffect();
            // GetParent().AddChild(splatterEffect);
            // splatterEffect.TriggerSplatter(this);
            
            // Hide the entity immediately (splatter effect will show instead)
            Visible = false;
            
            // Queue free after a short delay to ensure splatter effect has captured the entity
            GetTree().CreateTimer(0.1).Timeout += () => QueueFree();
        }

        private void BeginActionStatus(EntityAction action, float duration, string text, BugSquashEntity partner = null)
        {
            // End any existing status first
            if (_actionStatusText != null && IsInstanceValid(_actionStatusText))
            {
                _actionStatusText.Finish();
                _actionStatusText = null;
            }

            _currentAction = action;
            _currentActionDuration = duration;

            var status = new ActionStatusText();
            AddChild(status);
            status.Initialize(text);
            // Follow both entities' midpoint; if partner is null, follow this only
            status.SetTargets(this, partner, verticalOffset: EntityRadius + 40f);
            _actionStatusText = status;
        }

        private void UpdateActionStatusProgress()
        {
            if (_actionStatusText == null || !IsInstanceValid(_actionStatusText)) return;

            float remaining = Mathf.Max(_feedingTimer, 0f);
            float progress = 1f;
            if (_currentActionDuration > 0f)
            {
                progress = 1f - (remaining / _currentActionDuration);
            }
            _actionStatusText.SetProgress(progress);
        }

        private void EndActionStatus()
        {
            if (_actionStatusText != null && IsInstanceValid(_actionStatusText))
            {
                _actionStatusText.Finish();
            }
            _actionStatusText = null;
            _currentAction = EntityAction.None;
            _currentActionDuration = 0f;
        }
    }
} 