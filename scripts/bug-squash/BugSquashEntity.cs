using Godot;
using System;

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
        private Vector2 _velocity;
        private float _speed;
        private int _health = 3;
        private bool _isStunned = false;
        private float _stunTimer = 0f;
        private float _reproduceTimer = 0f;
        private float _feedingTimer = 0f;
        private bool _isFeeding = false;
        private BugSquashEntity _currentTarget;
        private float _behaviorUpdateTimer = 0f;

        private Sprite2D _sprite;
        private Sprite2D _ring;
        private AnimationPlayer _animationPlayer;

        private const float STUN_DURATION = 3f;
        private const float REPRODUCE_COOLDOWN = 10f;
        private const float FEEDING_DURATION = 2f;
        private const float BEHAVIOR_UPDATE_INTERVAL = 0.5f;
        private const float ENTITY_RADIUS = 80f;

        public BugSquashSpecies Species => _species;
        public EntityBehavior Behavior => _behavior;
        public bool IsAlive { get; private set; } = true;

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
            
            // Set up collision FIRST (Area2D requires this for input)
            var collisionShape = new CollisionShape2D();
            var circle = new CircleShape2D();
            circle.Radius = ENTITY_RADIUS;
            collisionShape.Shape = circle;
            collisionShape.Position = Vector2.Zero;
            collisionShape.DebugColor = new Color(1, 0, 0, 0.3f); // Red debug color
            AddChild(collisionShape);
            
            // Create the visual structure after collision
            CreateVisuals();

            // Connect input - use regular input event for Area2D
            InputEvent += OnInputEvent;
            MouseEntered += OnMouseEntered;
            MouseExited += OnMouseExited;
            
            // Verify collision setup
            var hasCollision = false;
            foreach (Node child in GetChildren())
            {
                if (child is CollisionShape2D)
                {
                    hasCollision = true;
                    break;
                }
            }
            
            GD.Print($"Entity ready: InputPickable={InputPickable}, HasCollision={hasCollision}, ZIndex={ZIndex}, Visible={Visible}");
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
            
            // Create a ring texture programmatically
            var ringTexture = CreateRingTexture();
            _ring.Texture = ringTexture;
            _ring.Modulate = Colors.White; // Will be modulated with species color
            _ring.Scale = Vector2.One; // Ring texture is already the correct size
            
            AddChild(_ring);

            // Create animation player
            _animationPlayer = new AnimationPlayer();
            AddChild(_animationPlayer);
            CreateBlinkAnimation();
            
            GD.Print($"Visuals created - Sprite texture: {_sprite.Texture}, Ring texture: {_ring.Texture}");
        }
        
        private ImageTexture CreateRingTexture()
        {
            int size = (int)(ENTITY_RADIUS * 2);
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
            Position = startPosition;
            
            // Parse behavior
            if (!Enum.TryParse<EntityBehavior>(species.Behavior, out _behavior))
            {
                GD.PrintErr($"Unknown behavior: {species.Behavior}");
                _behavior = EntityBehavior.Food;
            }

            // Set visuals
            if (!string.IsNullOrEmpty(species.Color) && _ring != null)
            {
                var color = new Color(species.Color);
                _ring.Modulate = color;
                GD.Print($"Set ring color to {species.Color} for {species.Name}");
            }

            if (!string.IsNullOrEmpty(species.Image) && _sprite != null)
            {
                var texture = GD.Load<Texture2D>(species.Image);
                _sprite.Texture = texture;
                
                // Scale the sprite to fit within the entity radius
                if (texture != null)
                {
                    var textureSize = texture.GetSize();
                    var targetSize = ENTITY_RADIUS * 1.8f; // Slightly smaller than full radius
                    var scale = targetSize / Mathf.Max(textureSize.X, textureSize.Y);
                    _sprite.Scale = new Vector2(scale, scale);
                    GD.Print($"Set sprite scale to {scale} for {species.Name} (texture size: {textureSize})");
                }
            }

            // Food doesn't move
            if (_behavior == EntityBehavior.Food)
            {
                _speed = 0;
            }
            
            GD.Print($"Entity {species.Name} initialized at {Position}, Visible: {Visible}, Sprite: {_sprite != null}, Ring: {_ring != null}");
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
                    // Move away from last click position during stun
                    var awayDirection = (Position - GetGlobalMousePosition()).Normalized();
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

            // Update behavior timers
            if (_behavior == EntityBehavior.Predator)
            {
                _reproduceTimer += deltaF;
            }

            // Update AI behavior
            if (!_isStunned && !_isFeeding)
            {
                _behaviorUpdateTimer += deltaF;
                if (_behaviorUpdateTimer >= BEHAVIOR_UPDATE_INTERVAL)
                {
                    _behaviorUpdateTimer = 0;
                    UpdateBehavior();
                }
            }

            // Move
            if (!_isFeeding)
            {
                Position += _velocity * deltaF;
                KeepInBounds();
            }
        }

        private void UpdateBehavior()
        {
            switch (_behavior)
            {
                case EntityBehavior.Predator:
                    UpdatePredatorBehavior();
                    break;
                case EntityBehavior.Prey:
                    UpdatePreyBehavior();
                    break;
                case EntityBehavior.Food:
                    // Food doesn't move
                    break;
            }
        }

        private void UpdatePredatorBehavior()
        {
            var gameNode = GetParent();
            BugSquashEntity nearestPrey = null;
            BugSquashEntity nearestPredator = null;
            float nearestPreyDist = float.MaxValue;
            float nearestPredatorDist = float.MaxValue;

            // Find nearest prey and predator
            foreach (Node child in gameNode.GetChildren())
            {
                if (child is BugSquashEntity entity && entity != this && entity.IsAlive)
                {
                    float dist = Position.DistanceTo(entity.Position);
                    
                    if (entity.Behavior == EntityBehavior.Prey && dist < nearestPreyDist)
                    {
                        nearestPrey = entity;
                        nearestPreyDist = dist;
                    }
                    else if (entity.Behavior == EntityBehavior.Predator && dist < nearestPredatorDist)
                    {
                        nearestPredator = entity;
                        nearestPredatorDist = dist;
                    }
                }
            }

            // Decide behavior based on reproduce timer
            if (_reproduceTimer >= REPRODUCE_COOLDOWN && nearestPredator != null)
            {
                // Seek another predator to reproduce
                _currentTarget = nearestPredator;
                SeekTarget(nearestPredator);
                
                // Check if close enough to reproduce
                if (nearestPredatorDist < ENTITY_RADIUS * 2)
                {
                    _reproduceTimer = 0;
                    nearestPredator._reproduceTimer = 0;
                    CallDeferred(nameof(SpawnOffspring));
                }
            }
            else if (nearestPrey != null)
            {
                // Hunt prey
                _currentTarget = nearestPrey;
                SeekTarget(nearestPrey);
                
                // Check if caught prey
                if (nearestPreyDist < ENTITY_RADIUS * 1.5f)
                {
                    nearestPrey.Die();
                }
            }
            else
            {
                // Wander
                Wander();
            }
        }

        private void UpdatePreyBehavior()
        {
            var gameNode = GetParent();
            BugSquashEntity nearestPredator = null;
            BugSquashEntity nearestFood = null;
            float nearestPredatorDist = float.MaxValue;
            float nearestFoodDist = float.MaxValue;

            // Find nearest predator and food
            foreach (Node child in gameNode.GetChildren())
            {
                if (child is BugSquashEntity entity && entity != this && entity.IsAlive)
                {
                    float dist = Position.DistanceTo(entity.Position);
                    
                    if (entity.Behavior == EntityBehavior.Predator && dist < nearestPredatorDist)
                    {
                        nearestPredator = entity;
                        nearestPredatorDist = dist;
                    }
                    else if (entity.Behavior == EntityBehavior.Food && dist < nearestFoodDist)
                    {
                        nearestFood = entity;
                        nearestFoodDist = dist;
                    }
                }
            }

            // Prioritize escaping from predators
            if (nearestPredator != null && nearestPredatorDist < 400f)
            {
                // Flee from predator
                var fleeDirection = (Position - nearestPredator.Position).Normalized();
                _velocity = fleeDirection * _speed;
            }
            else if (nearestFood != null)
            {
                // Seek food
                _currentTarget = nearestFood;
                SeekTarget(nearestFood);
                
                // Check if reached food
                if (nearestFoodDist < ENTITY_RADIUS * 1.5f)
                {
                    StartFeeding(nearestFood);
                }
            }
            else
            {
                // Wander
                Wander();
            }
        }

        private void SeekTarget(BugSquashEntity target)
        {
            var direction = (target.Position - Position).Normalized();
            _velocity = direction * _speed;
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
            var margin = ENTITY_RADIUS;

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
            food.Die();
        }

        private void CompleteFeedingAndReproduce()
        {
            CallDeferred(nameof(SpawnOffspring));
        }

        private void SpawnOffspring()
        {
            var parent = GetParent();
            if (parent is BugSquashGame game)
            {
                game.SpawnEntity(_species, Position + new Vector2(ENTITY_RADIUS * 2, 0));
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

            if (_behavior == EntityBehavior.Prey || _behavior == EntityBehavior.Food)
            {
                // Native species die immediately when clicked
                GD.Print($"Native species {_species?.Name} dying from click");
                Die();
            }
            else if (_behavior == EntityBehavior.Predator)
            {
                // Invasive species take damage
                _health--;
                GD.Print($"Invasive species {_species?.Name} hit. Health: {_health}");
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