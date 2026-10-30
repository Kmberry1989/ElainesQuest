using Godot;

public partial class ElaineController : CharacterBody3D
{
    [ExportCategory("Movement Settings")]
    [Export] public float Speed = 7.0f;
    [Export] public float JumpVelocity = 4.5f;
    [Export] public float Gravity = 9.8f;
    [Export] public float RotationSpeed = 10.0f;
    [Export] public float Acceleration = 18.0f;
    [Export] public float GroundDeceleration = 18.0f;
    [Export] public float WalkAnimationThreshold = 0.05f;
    [Export] public float RunAnimationThreshold = 0.2f;
    [Export] public float RunInputThreshold = 0.65f;

    [ExportCategory("Magic Ring Settings")]
    [Export] public float HoverDuration = 2.0f;
    [Export] public float HoverGravityModifier = 0.2f;

    [ExportCategory("Action Settings")]
    [Export] public float OneShotFallbackDuration = 0.75f;

    [ExportCategory("Combat Settings")]
    [Export] public float MaxHealth = 5.0f;
    [Export] public float DamageInvulnerabilityDuration = 0.8f;
    [Export] public string SpellDisplayName = "Sun Ember";
    [Export] public float SpellDamage = 1.0f;
    [Export] public float SpellCooldown = 0.9f;
    [Export] public float SpellReleaseTime = 0.22f;
    [Export] public float SpellProjectileSpeed = 24.0f;
    [Export] public float SpellProjectileRange = 28.0f;
    [Export] public PackedScene SpellProjectileScene;
    [Export] public PackedScene SpellImpactScene;

    [ExportCategory("Nodes")]
    [Export] public Node3D Visuals;
    [Export] public AnimationPlayer AnimPlayer;
    [Export] public AnimationTree AnimTree;
    [Export] public Marker3D SpellOrigin;
    [Export] public float InteractionRadius = 4.0f;

    [Signal]
    public delegate void HealthChangedEventHandler(float currentHealth, float maxHealth);

    private bool _isHovering = false;
    private float _hoverTimer = 0.0f;
    private string _currentAnim = "";
    private Vector3 _respawnPoint;
    private AnimationNodeStateMachinePlayback _stateMachinePlayback;
    private string _oneShotState = "";
    private float _oneShotTimer = 0.0f;
    private float _currentHealth = 0.0f;
    private float _damageInvulnerabilityTimer = 0.0f;
    private float _spellCooldownTimer = 0.0f;
    private float _pendingSpellTimer = -1.0f;

    public override void _Ready()
    {
        AddToGroup("player");
        Visuals ??= GetNodeOrNull<Node3D>("Visuals");
        AnimPlayer ??= GetNodeOrNull<AnimationPlayer>("GlobalAnimationPlayer");
        AnimTree ??= GetNodeOrNull<AnimationTree>("AnimationTree");
        SpellOrigin ??= GetNodeOrNull<Marker3D>("Visuals/SpellOrigin");
        _respawnPoint = GlobalPosition;
        _currentHealth = MaxHealth;

        EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);
        CallDeferred(nameof(InitializeAnimationStateMachine));
    }

    private void InitializeAnimationStateMachine()
    {
        if (AnimTree == null)
        {
            TravelAnimationState("Idle");
            return;
        }

        AnimTree.Active = true;
        _stateMachinePlayback = (AnimationNodeStateMachinePlayback)AnimTree.Get("parameters/playback");
        string initialState = string.IsNullOrEmpty(_currentAnim) ? "Idle" : _currentAnim;
        _currentAnim = "";
        TravelAnimationState(initialState);
    }

    private void TravelAnimationState(string stateName)
    {
        if (_currentAnim == stateName)
        {
            return;
        }

        if (_stateMachinePlayback != null)
        {
            _stateMachinePlayback.Travel(stateName);
            _currentAnim = stateName;
            return;
        }

        _currentAnim = stateName;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (IsDialogueOpen())
        {
            return;
        }

        if (@event.IsActionPressed("interact"))
        {
            if (TryInteract())
            {
                StartOneShot("Talk", "Mixamo/talk");
            }

            return;
        }

        if (@event.IsActionPressed("cast_magic"))
        {
            TryCastSpell();
            return;
        }

        if (@event.IsActionPressed("emote"))
        {
            StartOneShot("Wave", "Mixamo/wave");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;
        float d = (float)delta;
        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
        float inputStrength = Mathf.Clamp(inputDir.Length(), 0.0f, 1.0f);
        Vector3 direction = new Vector3(inputDir.X, 0.0f, inputDir.Y);

        _damageInvulnerabilityTimer = Mathf.Max(0.0f, _damageInvulnerabilityTimer - d);
        _spellCooldownTimer = Mathf.Max(0.0f, _spellCooldownTimer - d);

        if (direction.LengthSquared() > 0.0f)
        {
            direction = direction.Normalized();
        }

        if (_oneShotTimer > 0.0f && !IsDialogueOpen())
        {
            _oneShotTimer = Mathf.Max(0.0f, _oneShotTimer - d);
        }
        else if (_oneShotTimer <= 0.0f)
        {
            _oneShotState = string.Empty;
        }

        if (_pendingSpellTimer >= 0.0f)
        {
            _pendingSpellTimer -= d;
            if (_pendingSpellTimer <= 0.0f)
            {
                _pendingSpellTimer = -1.0f;
                SpawnSpellProjectile();
            }
        }

        if (!IsOnFloor())
        {
            _oneShotState = string.Empty;
            _oneShotTimer = 0.0f;
            CancelQueuedSpellCast();

            if (Input.IsActionPressed("jump") && Velocity.Y < 0.0f && _hoverTimer > 0.0f)
            {
                _isHovering = true;
                velocity.Y -= (Gravity * HoverGravityModifier) * d;
                _hoverTimer -= d;
                TravelAnimationState("Hover");
            }
            else
            {
                _isHovering = false;
                velocity.Y -= Gravity * d;
                TravelAnimationState("Jump");
            }
        }
        else
        {
            _hoverTimer = HoverDuration;
            _isHovering = false;
        }

        if (Input.IsActionJustPressed("jump") && IsOnFloor())
        {
            velocity.Y = JumpVelocity;
            TravelAnimationState("Jump");
        }

        bool oneShotActive = _oneShotTimer > 0.0f;
        float targetHorizontalSpeed = Speed * inputStrength;

        if (oneShotActive)
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0.0f, GroundDeceleration * d);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0.0f, GroundDeceleration * d);
            TravelAnimationState(_oneShotState);
        }
        else if (direction != Vector3.Zero)
        {
            velocity.X = Mathf.MoveToward(Velocity.X, direction.X * targetHorizontalSpeed, Acceleration * d);
            velocity.Z = Mathf.MoveToward(Velocity.Z, direction.Z * targetHorizontalSpeed, Acceleration * d);

            if (Visuals != null)
            {
                float targetAngle = Mathf.Atan2(direction.X, direction.Z);
                float currentRotation = Visuals.Rotation.Y;
                Visuals.Rotation = new Vector3(0.0f, Mathf.LerpAngle(currentRotation, targetAngle, RotationSpeed * d), 0.0f);
            }
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0.0f, GroundDeceleration * d);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0.0f, GroundDeceleration * d);
        }

        if (IsOnFloor() && !_isHovering && !oneShotActive)
        {
            float horizontalSpeed = new Vector2(velocity.X, velocity.Z).Length();

            if (horizontalSpeed < WalkAnimationThreshold)
            {
                TravelAnimationState("Idle");
            }
            else if (inputStrength < RunInputThreshold)
            {
                TravelAnimationState("Walk");
            }
            else if (horizontalSpeed >= RunAnimationThreshold)
            {
                TravelAnimationState("Run");
            }
            else
            {
                TravelAnimationState("Walk");
            }
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    public void SetRespawnPoint(Vector3 point)
    {
        _respawnPoint = point;
    }

    public void Respawn()
    {
        RespawnTo(_respawnPoint);
    }

    public void RespawnTo(Vector3 point)
    {
        GlobalPosition = point;
        Velocity = Vector3.Zero;
        _oneShotState = string.Empty;
        _oneShotTimer = 0.0f;
        _spellCooldownTimer = 0.0f;
        CancelQueuedSpellCast();
        RestoreFullHealth();
        TravelAnimationState("Idle");
    }

    public bool ReceiveDamage(float amount, Node source = null)
    {
        if (amount <= 0.0f || _damageInvulnerabilityTimer > 0.0f)
        {
            return false;
        }

        _currentHealth = Mathf.Max(0.0f, _currentHealth - amount);
        _damageInvulnerabilityTimer = DamageInvulnerabilityDuration;
        EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);

        if (_currentHealth <= 0.0f)
        {
            HandleDefeat();
        }

        return true;
    }

    public float GetCurrentHealth()
    {
        return _currentHealth;
    }

    public float GetMaxHealth()
    {
        return MaxHealth;
    }

    public float GetHealthRatio()
    {
        if (MaxHealth <= 0.0f)
        {
            return 0.0f;
        }

        return Mathf.Clamp(_currentHealth / MaxHealth, 0.0f, 1.0f);
    }

    public string GetSpellDisplayName()
    {
        return SpellDisplayName;
    }

    public float GetSpellCooldownRemaining()
    {
        return _spellCooldownTimer;
    }

    public float GetSpellCooldownRatio()
    {
        if (SpellCooldown <= 0.0f)
        {
            return 0.0f;
        }

        return Mathf.Clamp(_spellCooldownTimer / SpellCooldown, 0.0f, 1.0f);
    }

    public string GetSpellHudText()
    {
        if (_spellCooldownTimer <= 0.0f)
        {
            return $"{SpellDisplayName}: Ready  [F]";
        }

        return $"{SpellDisplayName}: {Mathf.Ceil(_spellCooldownTimer * 10.0f) / 10.0f:0.0}s";
    }

    private bool TryInteract()
    {
        if (IsDialogueOpen())
        {
            return false;
        }

        NPC closestNpc = null;
        float closestDistanceSquared = InteractionRadius * InteractionRadius;

        foreach (Node node in GetTree().GetNodesInGroup("npc_interaction"))
        {
            if (node is not NPC npc)
            {
                continue;
            }

            float distanceSquared = GlobalPosition.DistanceSquaredTo(npc.GlobalPosition);
            if (distanceSquared > closestDistanceSquared)
            {
                continue;
            }

            closestNpc = npc;
            closestDistanceSquared = distanceSquared;
        }

        if (closestNpc == null)
        {
            return false;
        }

        closestNpc.Interact();
        return true;
    }

    private bool TryCastSpell()
    {
        if (_spellCooldownTimer > 0.0f || SpellProjectileScene == null)
        {
            return false;
        }

        if (!StartOneShot("Cast", "Mixamo/cast"))
        {
            return false;
        }

        _spellCooldownTimer = SpellCooldown;
        _pendingSpellTimer = Mathf.Max(0.05f, Mathf.Min(SpellReleaseTime, _oneShotTimer));
        return true;
    }

    private bool StartOneShot(string stateName, string animationName)
    {
        if (!IsOnFloor() || IsDialogueOpen() || _oneShotTimer > 0.0f)
        {
            return false;
        }

        _oneShotState = stateName;
        _oneShotTimer = GetAnimationDuration(animationName);
        TravelAnimationState(stateName);
        return true;
    }

    private float GetAnimationDuration(string animationName)
    {
        AnimationPlayer activePlayer = ResolveActiveAnimationPlayer();
        Animation animation = activePlayer?.GetAnimation(animationName);
        if (animation == null)
        {
            return OneShotFallbackDuration;
        }

        return Mathf.Max(0.1f, (float)animation.Length - 0.05f);
    }

    private AnimationPlayer ResolveActiveAnimationPlayer()
    {
        if (AnimTree != null)
        {
            AnimationPlayer treePlayer = AnimTree.GetNodeOrNull<AnimationPlayer>(AnimTree.AnimPlayer);
            if (treePlayer != null)
            {
                return treePlayer;
            }
        }

        return AnimPlayer ?? GetNodeOrNull<AnimationPlayer>("GlobalAnimationPlayer");
    }

    private void HandleDefeat()
    {
        RespawnTo(_respawnPoint);
        _damageInvulnerabilityTimer = DamageInvulnerabilityDuration;
    }

    private void RestoreFullHealth()
    {
        _currentHealth = MaxHealth;
        EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);
    }

    private void CancelQueuedSpellCast()
    {
        _pendingSpellTimer = -1.0f;
    }

    private void SpawnSpellProjectile()
    {
        if (SpellProjectileScene == null)
        {
            return;
        }

        SpellProjectile projectile = SpellProjectileScene.Instantiate<SpellProjectile>();
        if (projectile == null)
        {
            GD.PushWarning("SpellProjectileScene does not instantiate a SpellProjectile.");
            return;
        }

        Vector3 direction = ResolveSpellDirection();
        Vector3 spawnPoint =
            SpellOrigin?.GlobalPosition ??
            (GlobalPosition + Vector3.Up * 1.2f + direction * 0.9f);

        GetTree().CurrentScene?.AddChild(projectile);
        projectile.GlobalPosition = spawnPoint;
        projectile.Launch(this, direction, SpellDamage, SpellProjectileSpeed, SpellProjectileRange, SpellImpactScene);
    }

    private Vector3 ResolveSpellDirection()
    {
        Vector3 direction = -(Visuals?.GlobalBasis.Z ?? GlobalBasis.Z);
        direction.Y = 0.0f;

        if (direction.LengthSquared() <= 0.001f)
        {
            direction = -Vector3.Forward;
        }

        return direction.Normalized();
    }

    private static bool IsDialogueOpen()
    {
        return DialogueUI.Instance?.Panel?.Visible ?? false;
    }
}
