using Godot;
using System;

public partial class BonusControl : Control
{
    private float _baseSpeed = 350.0f; // Fast but controlled
    private Vector2 _currentVelocity;
    private float _timeElapsed = 0.0f;
    private GpuParticles2D _particles;
    private GpuParticles2D _explosionParticles; // New explosion particles
    private ImageTexture _explosionTexture; // Pre-generated texture
    
    // Fairy-like movement parameters
    private Vector2 _targetPosition;
    private float _loopRadius = 80.0f;
    private float _loopSpeed = 2.5f;
    private float _accelerationStrength = 250.0f;
    private float _nextTargetTime = 0.0f;
    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    private bool _isLooping = false;
    private float _loopAngle = 0.0f;
    private Vector2 _loopCenter;
    private float _velocitySmoothing = 0.92f; // Smooth velocity changes
    
    // Debug flag to show/hide circle drawing
    private bool _debug = false;
    
    [Signal]
    public delegate void BonusCollectedEventHandler();
    
    public override void _Ready()
    {
        // Set the size - larger to match the bigger particle effect
        CustomMinimumSize = new Vector2(150, 150);
        Size = new Vector2(150, 150);
        
        // Center the pivot
        PivotOffset = Size / 2;
        
        // Connect GUI input
        GuiInput += OnGuiInput;
        
        // Enable mouse input
        MouseFilter = MouseFilterEnum.Stop;
        
        // Pre-generate explosion texture to avoid freeze on click
        _explosionTexture = CreateExplosionSparkleTexture();
        
        // Create particle system
        CreateParticleSystem();
        
        // Initialize movement after a frame to ensure proper viewport setup
        CallDeferred(nameof(InitializeMovement));
    }
    
    private void CreateParticleSystem()
    {
        _particles = new GpuParticles2D();
        AddChild(_particles);
        
        // Position at center of control
        _particles.Position = Size / 2;
        
        // Configure particle properties - optimized for performance
        _particles.Amount = 30;
        _particles.Lifetime = 1.2f; // 800ms average lifetime
        _particles.Preprocess = 0.0f;
        _particles.SpeedScale = 1.0f;
        _particles.Emitting = true;
        _particles.LocalCoords = false; // Important for trail effect
        _particles.DrawOrder = GpuParticles2D.DrawOrderEnum.Lifetime; // Optimize draw order
        
        // Create process material
        var processMaterial = new ParticleProcessMaterial();
        
        // Emission shape - emit from a smaller area for concentrated bright core
        processMaterial.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere;
        processMaterial.EmissionSphereRadius = 15.0f;
        
        // Initial velocity - varied for depth
        processMaterial.InitialVelocityMin = 30.0f;
        processMaterial.InitialVelocityMax = 100.0f;
        processMaterial.Spread = 50.0f;
        
        // Add subtle turbulence for sparkle effect (reduced for performance)
        processMaterial.TurbulenceEnabled = true;
        processMaterial.TurbulenceNoiseStrength = 8.0f;
        processMaterial.TurbulenceNoiseScale = 1.5f;
        processMaterial.TurbulenceNoiseSpeed = new Vector3(1.0f, 1.0f, 0.0f);
        
        // Gravity - slight upward drift
        processMaterial.Gravity = new Vector3(0, -30.0f, 0);
        
        // Scale variation for twinkling effect - optimized size
        processMaterial.ScaleMin = 1.5f;
        processMaterial.ScaleMax = 3.5f;
        
        // Scale curve - particles start larger for bright core, then shrink
        var scaleCurve = new CurveTexture();
        var curve = new Curve();
        curve.AddPoint(new Vector2(0.0f, 1.2f)); // Start larger
        curve.AddPoint(new Vector2(0.2f, 1.3f)); // Peak brightness
        curve.AddPoint(new Vector2(0.5f, 0.8f)); // Medium
        curve.AddPoint(new Vector2(1.0f, 0.0f)); // Fade out
        scaleCurve.Curve = curve;
        processMaterial.ScaleCurve = scaleCurve;
        
        // Angular velocity for spinning particles
        processMaterial.AngularVelocityMin = -180.0f;
        processMaterial.AngularVelocityMax = 180.0f;
        
        // Damping to slow particles over time
        processMaterial.LinearAccelMin = -10.0f;
        processMaterial.LinearAccelMax = -5.0f;
        
        _particles.ProcessMaterial = processMaterial;
        
        // Create gradient for color variation (bright white core to gold edges)
        var gradient = new Gradient();
        gradient.SetColor(0, new Color(1.0f, 1.0f, 1.0f, 1.0f)); // Pure white
        gradient.AddPoint(0.3f, new Color(1.0f, 1.0f, 0.85f, 1.0f)); // Bright yellow-white
        gradient.SetColor(1, new Color(1.0f, 0.8f, 0.0f, 0.9f)); // Rich gold
        
        var gradientTexture = new GradientTexture1D();
        gradientTexture.Gradient = gradient;
        gradientTexture.Width = 64; // Smaller gradient texture for performance
        
        // Create a lightweight additive material for glow
        var particleMaterial = new CanvasItemMaterial();
        particleMaterial.BlendMode = CanvasItemMaterial.BlendModeEnum.Add;
        particleMaterial.LightMode = CanvasItemMaterial.LightModeEnum.Unshaded;
        
        _particles.Texture = CreateSparkleTexture();
        _particles.Material = particleMaterial;
        
        // Brighter base color
        processMaterial.Color = new Color(1.2f, 1.1f, 0.9f, 1.0f);
        processMaterial.ColorRamp = gradientTexture;
        
        // Lifetime randomness for varied trail
        processMaterial.LifetimeRandomness = 1f;
    }
    
    private ImageTexture CreateSparkleTexture()
    {
        // Create a smaller, optimized sparkle texture
        var size = 64;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        
        var center = new Vector2(size / 2, size / 2);
        var radius = size / 2.0f;
        
        // Pre-calculate for optimization
        var radiusSquared = radius * radius;
        var innerRadiusSquared = (radius * 0.3f) * (radius * 0.3f);
        var midRadiusSquared = (radius * 0.6f) * (radius * 0.6f);
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                var dx = x - center.X;
                var dy = y - center.Y;
                var distanceSquared = dx * dx + dy * dy;
                
                if (distanceSquared > radiusSquared)
                {
                    image.SetPixel(x, y, Colors.Transparent);
                    continue;
                }
                
                var distance = Mathf.Sqrt(distanceSquared);
                
                // Simplified star pattern
                var angle = Mathf.Atan2(dy, dx);
                var starFactor = Mathf.Abs(Mathf.Sin(angle * 5)) * 0.5f + 0.5f;
                
                float alpha;
                float brightness = 1.0f;
                
                if (distanceSquared < innerRadiusSquared)
                {
                    alpha = 1.0f; // Bright core
                    brightness = 1.2f; // Extra bright center
                }
                else if (distanceSquared < midRadiusSquared)
                {
                    var t = (distance - radius * 0.3f) / (radius * 0.3f);
                    alpha = (1.0f - t) * starFactor;
                    brightness = 1.0f + (0.2f * (1.0f - t)); // Gradual brightness falloff
                }
                else
                {
                    alpha = (1.0f - (distance - radius * 0.6f) / (radius * 0.4f)) * starFactor * 0.6f;
                    brightness = 1.0f;
                }
                
                // Apply HDR-like brightness to make core glow
                var color = new Color(brightness, brightness, brightness * 0.95f, alpha);
                image.SetPixel(x, y, color);
            }
        }
        
        return ImageTexture.CreateFromImage(image);
    }
    
    public override void _Draw()
    {
        // Only draw circles in debug mode
        if (!_debug)
            return;
            
        var center = Size / 2;
        
        // Create a pulsing effect
        var pulseScale = 1.0f + Mathf.Sin(_timeElapsed * 3.0f) * 0.1f;
        
        // Outer glow with watercolor-like transparency
        DrawCircle(center, 90.0f * pulseScale, new Color(1.0f, 0.9f, 0.4f, 0.2f));
        DrawCircle(center, 75.0f * pulseScale, new Color(1.0f, 0.85f, 0.3f, 0.3f));
        
        // Main golden circle with soft edges
        DrawCircle(center, 60.0f, new Color(1.0f, 0.8f, 0.2f, 0.9f));
        DrawCircle(center, 50.0f, new Color(1.0f, 0.75f, 0.15f));
        
        // Inner highlight
        DrawCircle(center + new Vector2(-12, -12), 25.0f, new Color(1.0f, 0.95f, 0.7f, 0.8f));
        
        // Draw a simple "+" symbol in the center
        var crossSize = 30.0f;
        var crossThickness = 10.0f;
        DrawRect(new Rect2(center.X - crossSize/2, center.Y - crossThickness/2, crossSize, crossThickness), Colors.White);
        DrawRect(new Rect2(center.X - crossThickness/2, center.Y - crossSize/2, crossThickness, crossSize), Colors.White);
        
        // Add text hint
        var font = ThemeDB.FallbackFont;
        var fontSize = 24;
        var text = "+5s";
        var textSize = font.GetStringSize(text, HorizontalAlignment.Center, -1, fontSize);
        DrawString(font, center - textSize / 2 + new Vector2(0, 50), text, HorizontalAlignment.Center, -1, fontSize, Colors.White);
    }
    
    public override void _Process(double delta)
    {
        _timeElapsed += (float)delta;
        var deltaF = (float)delta;
        
        // Check if we need a new target
        if (_timeElapsed > _nextTargetTime)
        {
            DecideNextMovement();
        }
        
        if (_isLooping)
        {
            // Perform smooth loop movement
            _loopAngle += _loopSpeed * deltaF;
            
            // Calculate position on the loop with smooth transitions
            var loopX = Mathf.Cos(_loopAngle) * _loopRadius;
            var loopY = Mathf.Sin(_loopAngle) * _loopRadius;
            var targetLoopPos = _loopCenter + new Vector2(loopX, loopY);
            
            // Smoothly move to loop position
            var toLoopPos = targetLoopPos - Position;
            if (toLoopPos.Length() > 1.0f)
            {
                _currentVelocity = toLoopPos.Normalized() * _baseSpeed * 0.8f;
            }
            
            Position += _currentVelocity * deltaF;
            
            // Add subtle drift to the loop center for organic movement
            _loopCenter += new Vector2(
                Mathf.Sin(_timeElapsed * 0.5f) * 20.0f * deltaF,
                Mathf.Cos(_timeElapsed * 0.7f) * 15.0f * deltaF
            );
            
            // Check if loop is complete
            if (_loopAngle > Mathf.Tau)
            {
                _isLooping = false;
                // Keep momentum from the loop
                _currentVelocity = new Vector2(
                    Mathf.Sin(_loopAngle) * _loopRadius * _loopSpeed,
                    -Mathf.Cos(_loopAngle) * _loopRadius * _loopSpeed
                );
                DecideNextMovement();
            }
        }
        else
        {
            // Fairy-like wandering movement
            var toTarget = (_targetPosition - Position);
            var distance = toTarget.Length();
            
            if (distance > 0.1f)
            {
                toTarget = toTarget.Normalized();
                
                // Add gentle perpendicular oscillation for graceful flight
                var perpendicular = new Vector2(-toTarget.Y, toTarget.X);
                var wave = perpendicular * Mathf.Sin(_timeElapsed * 3.0f) * 30.0f;
                
                // Calculate smooth acceleration towards target
                var desiredVelocity = toTarget * _baseSpeed;
                
                // Add wave component to desired velocity
                desiredVelocity += wave;
                
                // Smooth velocity changes for more graceful movement
                _currentVelocity = _currentVelocity.Lerp(desiredVelocity, (1.0f - _velocitySmoothing));
                
                // Add small random drift for organic feel (much reduced)
                var drift = new Vector2(
                    Mathf.Sin(_timeElapsed * 1.5f) * 20.0f,
                    Mathf.Cos(_timeElapsed * 1.7f) * 20.0f
                );
                
                // Apply movement
                Position += (_currentVelocity + drift) * deltaF;
            }
            
            // Check if we're close to target or if it's time for a new behavior
            if (distance < 100.0f || _timeElapsed > _nextTargetTime)
            {
                DecideNextMovement();
            }
        }
        
        // Force redraw
        QueueRedraw();
        
        // Remove if moved off screen
        var viewportSize = GetViewportRect().Size;
        if (Position.X < -300 || Position.X > viewportSize.X + 300 ||
            Position.Y < -300 || Position.Y > viewportSize.Y + 300)
        {
            QueueFree();
        }
    }
    
    private void InitializeMovement()
    {
        var viewportSize = GetViewportRect().Size;
        _rng.Randomize();
        
        // Start from a random edge
        var edge = _rng.RandiRange(0, 3);
        switch (edge)
        {
            case 0: // Left edge
                Position = new Vector2(-150, _rng.RandfRange(100, viewportSize.Y - 100));
                _currentVelocity = new Vector2(_baseSpeed * 0.3f, (_rng.Randf() - 0.5f) * 50);
                break;
            case 1: // Right edge
                Position = new Vector2(viewportSize.X + 150, _rng.RandfRange(100, viewportSize.Y - 100));
                _currentVelocity = new Vector2(-_baseSpeed * 0.3f, (_rng.Randf() - 0.5f) * 50);
                break;
            case 2: // Top edge
                Position = new Vector2(_rng.RandfRange(100, viewportSize.X - 100), -150);
                _currentVelocity = new Vector2((_rng.Randf() - 0.5f) * 50, _baseSpeed * 0.3f);
                break;
            case 3: // Bottom edge
                Position = new Vector2(_rng.RandfRange(100, viewportSize.X - 100), viewportSize.Y + 150);
                _currentVelocity = new Vector2((_rng.Randf() - 0.5f) * 50, -_baseSpeed * 0.3f);
                break;
        }
        
        // Set initial target
        _targetPosition = GetRandomTargetPosition();
        _nextTargetTime = _timeElapsed + _rng.RandfRange(1.0f, 2.0f);
        
        // Set consistent movement parameters for more predictable behavior
        _loopRadius = _rng.RandfRange(70.0f, 90.0f);
        _loopSpeed = _rng.RandfRange(2.0f, 2.5f);
        _velocitySmoothing = 0.92f;
    }
    
    private void DecideNextMovement()
    {
        // 25% chance to start a loop, but only if we have some velocity
        if (_rng.Randf() < 0.25f && !_isLooping && _currentVelocity.Length() > 50.0f)
        {
            _isLooping = true;
            _loopAngle = 0.0f;
            _loopCenter = Position;
            _loopRadius = _rng.RandfRange(60.0f, 100.0f);
            
            // Loop direction based on current velocity for smooth transition
            var cross = _currentVelocity.X * Vector2.Up.Y - _currentVelocity.Y * Vector2.Up.X;
            _loopSpeed = _rng.RandfRange(1.8f, 2.5f) * (cross > 0 ? 1 : -1);
        }
        else
        {
            _isLooping = false;
            _targetPosition = GetRandomTargetPosition();
            _nextTargetTime = _timeElapsed + _rng.RandfRange(2.0f, 4.0f); // Longer between decisions
        }
    }
    
    private Vector2 GetRandomTargetPosition()
    {
        var viewportSize = GetViewportRect().Size;
        var currentPos = Position;
        
        // Choose targets that create interesting paths
        var minDistance = 200.0f;
        var maxDistance = 600.0f;
        
        Vector2 newTarget;
        int attempts = 0;
        
        do
        {
            // Prefer targets within the viewport
            var margin = 50.0f;
            newTarget = new Vector2(
                _rng.RandfRange(margin, viewportSize.X - margin),
                _rng.RandfRange(margin, viewportSize.Y - margin)
            );
            
            attempts++;
        }
        while (currentPos.DistanceTo(newTarget) < minDistance && attempts < 5);
        
        // If target is too far, bring it closer
        if (currentPos.DistanceTo(newTarget) > maxDistance)
        {
            var direction = (newTarget - currentPos).Normalized();
            newTarget = currentPos + direction * maxDistance;
        }
        
        return newTarget;
    }
    
    private void OnGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                EmitSignal(SignalName.BonusCollected);
                
                // Disable further input
                MouseFilter = MouseFilterEnum.Ignore;
                
                // Stop emitting new particles but let existing ones finish
                if (_particles != null)
                {
                    _particles.Emitting = false;
                }
                
                // Create explosion effect
                CreateExplosionEffect();
                
                // Add a simple disappear effect
                var tween = CreateTween();
                tween.TweenProperty(this, "scale", Vector2.Zero, 0.2f);
                tween.TweenProperty(this, "modulate:a", 0.0f, 0.2f);
                tween.TweenCallback(Callable.From(() => QueueFree()));
                
                // Mark event as handled
                AcceptEvent();
            }
        }
    }
    
    private void CreateExplosionEffect()
    {
        _explosionParticles = new GpuParticles2D();
        AddChild(_explosionParticles);
        
        // Position at center of control
        _explosionParticles.Position = Size / 2;
        
        // Configure explosion properties - MUCH BIGGER
        _explosionParticles.Amount = 200; // Double the particles
        _explosionParticles.Lifetime = 2.0f; // Longer lifetime
        _explosionParticles.OneShot = true; // Single burst
        _explosionParticles.Emitting = true;
        _explosionParticles.LocalCoords = false;
        _explosionParticles.DrawOrder = GpuParticles2D.DrawOrderEnum.Index;
        
        // Create process material for explosion
        var processMaterial = new ParticleProcessMaterial();
        
        // Emission shape - sphere for more volume
        processMaterial.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere;
        processMaterial.EmissionSphereRadius = 20.0f; // Start from a bigger area
        
        // Initial velocity - MUCH higher speed burst
        processMaterial.InitialVelocityMin = 400.0f; // Double the speed
        processMaterial.InitialVelocityMax = 1000.0f; // Double the speed
        processMaterial.Spread = 45.0f; // Full circular spread
        
        // Direction - radial burst
        processMaterial.Direction = new Vector3(1, 0, 0);
        processMaterial.RadialAccelMin = -400.0f; // Slightly more slowdown
        processMaterial.RadialAccelMax = -300.0f;
        
        // Add stronger turbulence for more dynamic movement
        processMaterial.TurbulenceEnabled = true;
        processMaterial.TurbulenceNoiseStrength = 25.0f; // More turbulence
        processMaterial.TurbulenceNoiseScale = 2.0f;
        processMaterial.TurbulenceNoiseSpeed = new Vector3(4.0f, 4.0f, 0.0f);
        
        // Gravity - slight upward float
        processMaterial.Gravity = new Vector3(0, -80.0f, 0);
        
        // Scale variation - MUCH BIGGER particles
        processMaterial.ScaleMin = 4.0f; // Double size
        processMaterial.ScaleMax = 8.0f; // Double size
        
        // Scale curve - shrink over lifetime
        var scaleCurve = new CurveTexture();
        var curve = new Curve();
        curve.AddPoint(new Vector2(0.0f, 2.0f)); // Start even bigger
        curve.AddPoint(new Vector2(0.05f, 1.5f)); // Quick initial shrink
        curve.AddPoint(new Vector2(0.2f, 1.0f)); // Settle to base size
        curve.AddPoint(new Vector2(0.6f, 0.5f)); // Medium
        curve.AddPoint(new Vector2(1.0f, 0.0f)); // Fade out
        scaleCurve.Curve = curve;
        processMaterial.ScaleCurve = scaleCurve;
        
        // Angular velocity for spinning sparkles
        processMaterial.AngularVelocityMin = -720.0f; // Faster spin
        processMaterial.AngularVelocityMax = 720.0f;
        
        // Less damping for longer trails
        processMaterial.DampingMin = 1.0f;
        processMaterial.DampingMax = 3.0f;
        
        _explosionParticles.ProcessMaterial = processMaterial;
        
        // Create even more vibrant color gradient
        var gradient = new Gradient();
        gradient.SetColor(0, new Color(2.0f, 2.0f, 2.0f, 1.0f)); // Super bright white flash
        gradient.AddPoint(0.15f, new Color(1.5f, 1.5f, 0.5f, 1.0f)); // Bright yellow
        gradient.AddPoint(0.3f, new Color(1.0f, 0.8f, 0.0f, 1.0f)); // Gold
        gradient.AddPoint(0.6f, new Color(1.0f, 0.4f, 0.0f, 0.8f)); // Orange
        gradient.SetColor(1, new Color(1.0f, 0.1f, 0.0f, 0.0f)); // Red fade out
        
        var gradientTexture = new GradientTexture1D();
        gradientTexture.Gradient = gradient;
        gradientTexture.Width = 256; // Higher quality gradient
        
        // Additive blend for bright explosion
        var particleMaterial = new CanvasItemMaterial();
        particleMaterial.BlendMode = CanvasItemMaterial.BlendModeEnum.Add;
        particleMaterial.LightMode = CanvasItemMaterial.LightModeEnum.Unshaded;
        
        _explosionParticles.Texture = _explosionTexture; // Use pre-generated texture
        _explosionParticles.Material = particleMaterial;
        
        // Extra bright base color
        processMaterial.Color = new Color(2.0f, 1.8f, 1.2f, 1.0f);
        processMaterial.ColorRamp = gradientTexture;
        
        // Lifetime randomness for varied effect
        processMaterial.LifetimeRandomness = 0.3f;
        
        // Add a screen flash effect
        CreateScreenFlash();
        
        // Clean up after explosion
        var cleanupTimer = GetTree().CreateTimer(3.0f);
        cleanupTimer.Timeout += () => _explosionParticles?.QueueFree();
    }
    
    private void CreateScreenFlash()
    {
        // Create a full-screen flash effect
        var flash = new ColorRect();
        GetViewport().AddChild(flash);
        flash.Color = new Color(1.0f, 0.9f, 0.6f, 0.3f); // Golden flash
        flash.Size = GetViewport().GetVisibleRect().Size;
        flash.Position = Vector2.Zero;
        flash.MouseFilter = MouseFilterEnum.Ignore;
        
        // Fade out the flash
        var tween = flash.CreateTween();
        tween.TweenProperty(flash, "modulate:a", 0.0f, 0.3f);
        tween.TweenCallback(Callable.From(() => flash.QueueFree()));
        
        // Add shockwave ring effect
        CreateShockwaveRing();
    }
    
    private void CreateShockwaveRing()
    {
        // Create a custom control for the shockwave
        var shockwave = new Control();
        GetParent().AddChild(shockwave);
        shockwave.Position = GlobalPosition + Size / 2;
        shockwave.Size = Vector2.One * 100; // Start size
        shockwave.PivotOffset = shockwave.Size / 2;
        shockwave.MouseFilter = MouseFilterEnum.Ignore;
        
        // Custom draw for the ring
        shockwave.Draw += () =>
        {
            var center = shockwave.Size / 2;
            var radius = shockwave.Size.X / 2;
            
            // Draw multiple rings for depth
            for (int i = 0; i < 3; i++)
            {
                var ringRadius = radius - (i * 15);
                if (ringRadius > 0)
                {
                    var alpha = shockwave.Modulate.A * (1.0f - (i * 0.3f));
                    var color = new Color(1.0f, 0.9f, 0.4f, alpha);
                    
                    // Draw ring outline
                    shockwave.DrawArc(center, ringRadius, 0, Mathf.Tau, 64, color, 8.0f - (i * 2));
                }
            }
        };
        
        // Animate the shockwave
        var tween = shockwave.CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(shockwave, "scale", Vector2.One * 8, 0.6f).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(shockwave, "modulate:a", 0.0f, 0.6f).SetEase(Tween.EaseType.In);
        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(() => shockwave.QueueFree()));
    }
    
    private ImageTexture CreateExplosionSparkleTexture()
    {
        // Create a larger star-shaped sparkle for the explosion
        var size = 128; // Bigger texture
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        
        var center = new Vector2(size / 2, size / 2);
        var radius = size / 2.0f;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                var dx = x - center.X;
                var dy = y - center.Y;
                var distance = Mathf.Sqrt(dx * dx + dy * dy);
                
                if (distance > radius)
                {
                    image.SetPixel(x, y, Colors.Transparent);
                    continue;
                }
                
                var angle = Mathf.Atan2(dy, dx);
                
                // Create a 6-pointed star pattern
                var starPoints = 6;
                var starFactor = 0.5f + 0.5f * Mathf.Cos(angle * starPoints);
                
                // Add secondary star pattern
                var secondaryFactor = 0.5f + 0.5f * Mathf.Cos(angle * starPoints * 2 + Mathf.Pi / starPoints);
                var combinedFactor = Mathf.Max(starFactor, secondaryFactor * 0.7f);
                
                // Calculate brightness based on distance and star pattern
                var normalizedDistance = distance / radius;
                var coreBrightness = 1.0f - normalizedDistance;
                var brightness = coreBrightness * combinedFactor;
                
                // Extra bright core
                if (normalizedDistance < 0.2f)
                {
                    brightness = 1.0f;
                }
                
                var alpha = brightness * brightness; // Quadratic falloff
                
                // HDR-like color for extra sparkle
                var color = new Color(
                    brightness * 1.2f,
                    brightness * 1.1f,
                    brightness,
                    alpha
                );
                
                image.SetPixel(x, y, color);
            }
        }
        
        return ImageTexture.CreateFromImage(image);
    }
} 