using Godot;

public partial class ElaineController : CharacterBody3D
{
    private readonly struct SpellConfig
    {
        public SpellConfig(
            string displayName,
            string stateName,
            string animationName,
            float damage,
            float cooldown,
            float releaseTime,
            float projectileSpeed,
            float projectileRange,
            int projectileCount,
            float projectileSpreadDegrees,
            int maxTargetHits,
            PackedScene projectileScene,
            PackedScene impactScene)
        {
            DisplayName = displayName;
            StateName = stateName;
            AnimationName = animationName;
            Damage = damage;
            Cooldown = cooldown;
            ReleaseTime = releaseTime;
            ProjectileSpeed = projectileSpeed;
            ProjectileRange = projectileRange;
            ProjectileCount = projectileCount;
            ProjectileSpreadDegrees = projectileSpreadDegrees;
            MaxTargetHits = maxTargetHits;
            ProjectileScene = projectileScene;
            ImpactScene = impactScene;
        }

        public string DisplayName { get; }
        public string StateName { get; }
        public string AnimationName { get; }
        public float Damage { get; }
        public float Cooldown { get; }
        public float ReleaseTime { get; }
        public float ProjectileSpeed { get; }
        public float ProjectileRange { get; }
        public int ProjectileCount { get; }
        public float ProjectileSpreadDegrees { get; }
        public int MaxTargetHits { get; }
        public PackedScene ProjectileScene { get; }
        public PackedScene ImpactScene { get; }
    }

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
    [Export] public float CastBufferDuration = 0.14f;
    [Export] public float RespawnInvulnerabilityDuration = 1.15f;

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
    [Export] public string BurstSpellDisplayName = "Star Volley";
    [Export] public float BurstSpellDamage = 0.75f;
    [Export] public float BurstSpellCooldown = 0.75f;
    [Export] public float BurstSpellReleaseTime = 0.16f;
    [Export] public float BurstSpellProjectileSpeed = 31.0f;
    [Export] public float BurstSpellProjectileRange = 25.0f;
    [Export(PropertyHint.Range, "1,7,1")] public int BurstSpellProjectileCount = 3;
    [Export(PropertyHint.Range, "0,45,1")] public float BurstSpellSpreadDegrees = 18.0f;
    [Export] public PackedScene BurstSpellProjectileScene;
    [Export] public PackedScene BurstSpellImpactScene;
    [Export] public string LanceSpellDisplayName = "Sky Lance";
    [Export] public float LanceSpellDamage = 1.8f;
    [Export] public float LanceSpellCooldown = 1.2f;
    [Export] public float LanceSpellReleaseTime = 0.3f;
    [Export] public float LanceSpellProjectileSpeed = 36.0f;
    [Export] public float LanceSpellProjectileRange = 34.0f;
    [Export(PropertyHint.Range, "1,5,1")] public int LanceSpellMaxTargetHits = 2;
    [Export] public PackedScene LanceSpellProjectileScene;
    [Export] public PackedScene LanceSpellImpactScene;

    [ExportCategory("Nodes")]
    [Export] public Node3D Visuals;
    [Export] public AnimationPlayer AnimPlayer;
    [Export] public AnimationTree AnimTree;
    [Export] public Marker3D SpellOrigin;
    [Export] public float InteractionRadius = 4.0f;

    [Signal]
    public delegate void HealthChangedEventHandler(float currentHealth, float maxHealth);

    [Signal]
    public delegate void SpellSelectedEventHandler(int spellIndex, string displayName);

    [Signal]
    public delegate void SpellCastStartedEventHandler(int spellIndex, string displayName);

    [Signal]
    public delegate void SpellProjectilesReleasedEventHandler(int spellIndex, int projectileCount, string displayName);

    [Signal]
    public delegate void SpellHitEventHandler(int spellIndex, int hitNumber, int maxTargetHits, string displayName);

    [Signal]
    public delegate void InteractionPromptChangedEventHandler(string prompt);

    [Signal]
    public delegate void DamageTakenEventHandler(float amount, float currentHealth, float maxHealth);

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
    private float _spellCooldownDuration = 0.0f;
    private float _pendingSpellTimer = -1.0f;
    private int _selectedSpellIndex = 0;
    private int _pendingSpellIndex = -1;
    private float _castBufferTimer = -1.0f;
    private IPlayerInteractable _currentInteractable;
    private Node _currentInteractableNode;
    private bool _sceneArrivalApplied;

    public override void _Ready()
    {
        AddToGroup("player");
        Visuals ??= GetNodeOrNull<Node3D>("Visuals");
        AnimPlayer ??= GetNodeOrNull<AnimationPlayer>("GlobalAnimationPlayer");
        AnimTree ??= GetNodeOrNull<AnimationTree>("AnimationTree");
        SpellOrigin ??= GetNodeOrNull<Marker3D>("Visuals/SpellOrigin");
        SpellProjectileScene ??= ResourceLoader.Load<PackedScene>("res://Scenes/Combat/ElaineFirebolt.tscn");
        SpellImpactScene ??= ResourceLoader.Load<PackedScene>("res://Scenes/Combat/SpellImpact.tscn");
        BurstSpellProjectileScene ??= ResourceLoader.Load<PackedScene>("res://Scenes/Combat/ElaineStarVolley.tscn");
        BurstSpellImpactScene ??= SpellImpactScene;
        LanceSpellProjectileScene ??= ResourceLoader.Load<PackedScene>("res://Scenes/Combat/ElaineSkyLance.tscn");
        LanceSpellImpactScene ??= SpellImpactScene;
        _respawnPoint = GlobalPosition;
        _currentHealth = MaxHealth;
        _spellCooldownDuration = SpellCooldown;

        EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);
        CallDeferred(nameof(InitializeAnimationStateMachine));
        CallDeferred(nameof(ApplyPendingSceneArrival));
        CallDeferred(nameof(ApplyStoredCheckpoint));
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
            if (!TryCastSelectedSpell())
            {
                _castBufferTimer = CastBufferDuration;
            }
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
        bool spellWheelOpen = IsSpellWheelOpen();
        Vector2 inputDir = spellWheelOpen
            ? Vector2.Zero
            : Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
        float inputStrength = Mathf.Clamp(inputDir.Length(), 0.0f, 1.0f);
        Vector3 direction = new Vector3(inputDir.X, 0.0f, inputDir.Y);

        _damageInvulnerabilityTimer = Mathf.Max(0.0f, _damageInvulnerabilityTimer - d);
        _spellCooldownTimer = Mathf.Max(0.0f, _spellCooldownTimer - d);
        _castBufferTimer = Mathf.Max(-1.0f, _castBufferTimer - d);

        UpdateNearbyInteraction();
        if (_castBufferTimer >= 0.0f && _spellCooldownTimer <= 0.0f && TryCastSelectedSpell())
        {
            _castBufferTimer = -1.0f;
        }

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
        _spellCooldownDuration = GetCurrentSpell().Cooldown;
        _damageInvulnerabilityTimer = RespawnInvulnerabilityDuration;
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
        EmitSignal(SignalName.DamageTaken, amount, _currentHealth, MaxHealth);

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
        return GetCurrentSpell().DisplayName;
    }

    public string GetSpellDisplayName(int spellIndex)
    {
        return GetSpellConfig(spellIndex).DisplayName;
    }

    public string GetSpellRoleText(int spellIndex)
    {
        return Mathf.Clamp(spellIndex, 0, GetSpellCount() - 1) switch
        {
            1 => "Three close-range bolts",
            2 => "Pierces two targets",
            _ => "Precise single target",
        };
    }

    public int GetSpellCount()
    {
        return 3;
    }

    public int GetSelectedSpellIndex()
    {
        return _selectedSpellIndex;
    }

    public bool SelectSpell(int spellIndex)
    {
        if (spellIndex < 0 || spellIndex >= GetSpellCount())
        {
            return false;
        }

        if (_selectedSpellIndex != spellIndex)
        {
            _selectedSpellIndex = spellIndex;
            EmitSignal(SignalName.SpellSelected, spellIndex, GetCurrentSpell().DisplayName);
        }

        return true;
    }

    public float GetSpellCooldownRemaining()
    {
        return _spellCooldownTimer;
    }

    public float GetSpellCooldownRatio()
    {
        float resolvedDuration = _spellCooldownTimer > 0.0f
            ? _spellCooldownDuration
            : GetCurrentSpell().Cooldown;

        if (resolvedDuration <= 0.0f)
        {
            return 0.0f;
        }

        return Mathf.Clamp(_spellCooldownTimer / resolvedDuration, 0.0f, 1.0f);
    }

    public string GetSpellHudText()
    {
        SpellConfig spell = GetCurrentSpell();
        if (_spellCooldownTimer <= 0.0f)
        {
            return $"{spell.DisplayName} — {GetSpellRoleText(_selectedSpellIndex)}: Ready  [F]  Wheel [Tab]";
        }

        return $"{spell.DisplayName} — {GetSpellRoleText(_selectedSpellIndex)}: {Mathf.Ceil(_spellCooldownTimer * 10.0f) / 10.0f:0.0}s  Wheel [Tab]";
    }

    private bool TryInteract()
    {
        if (IsDialogueOpen())
        {
            return false;
        }

        if (_currentInteractable == null || _currentInteractableNode == null || !IsInstanceValid(_currentInteractableNode) || !_currentInteractable.CanInteract(this))
        {
            UpdateNearbyInteraction();
        }

        if (_currentInteractable == null)
        {
            return false;
        }

        _currentInteractable.Interact(this);
        return true;
    }

    private void UpdateNearbyInteraction()
    {
        if (IsDialogueOpen())
        {
            SetCurrentInteractable(null, null);
            return;
        }

        IPlayerInteractable nearest = null;
        Node nearestNode = null;
        float closestDistanceSquared = InteractionRadius * InteractionRadius;

        foreach (Node node in GetTree().GetNodesInGroup("player_interaction"))
        {
            if (node is not Node3D node3D || node is not IPlayerInteractable interactable || !interactable.CanInteract(this))
            {
                continue;
            }

            float distanceSquared = GlobalPosition.DistanceSquaredTo(node3D.GlobalPosition);
            if (distanceSquared > closestDistanceSquared)
            {
                continue;
            }

            nearest = interactable;
            nearestNode = node;
            closestDistanceSquared = distanceSquared;
        }

        SetCurrentInteractable(nearest, nearestNode);
    }

    private void SetCurrentInteractable(IPlayerInteractable interactable, Node node)
    {
        string previousPrompt = _currentInteractable?.InteractionPrompt ?? string.Empty;
        _currentInteractable = interactable;
        _currentInteractableNode = node;
        string nextPrompt = _currentInteractable?.InteractionPrompt ?? string.Empty;
        if (!string.Equals(previousPrompt, nextPrompt, System.StringComparison.Ordinal))
        {
            EmitSignal(SignalName.InteractionPromptChanged, nextPrompt);
        }
    }

    private void ApplyPendingSceneArrival()
    {
        string markerName = GameManager.Instance?.ConsumeSceneArrival() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(markerName))
        {
            return;
        }

        Node markerNode = GetTree().CurrentScene?.FindChild(markerName, true, false);
        if (markerNode is Node3D marker)
        {
            GlobalPosition = marker.GlobalPosition;
            Velocity = Vector3.Zero;
            SetRespawnPoint(marker.GlobalPosition);
            _sceneArrivalApplied = true;
        }
    }

    private void ApplyStoredCheckpoint()
    {
        if (_sceneArrivalApplied || GameManager.Instance == null)
        {
            return;
        }

        string scenePath = GetTree().CurrentScene?.SceneFilePath ?? string.Empty;
        if (GameManager.Instance.TryGetCheckpoint(scenePath, out Vector3 checkpointPosition))
        {
            GlobalPosition = checkpointPosition;
            Velocity = Vector3.Zero;
            SetRespawnPoint(checkpointPosition);
        }
    }

    public bool TryCastSelectedSpell()
    {
        SpellConfig spell = GetCurrentSpell();
        if (_spellCooldownTimer > 0.0f || spell.ProjectileScene == null)
        {
            return false;
        }

        if (!StartOneShot(spell.StateName, spell.AnimationName))
        {
            return false;
        }

        _spellCooldownDuration = spell.Cooldown;
        _spellCooldownTimer = spell.Cooldown;
        _pendingSpellIndex = _selectedSpellIndex;
        _pendingSpellTimer = Mathf.Max(0.05f, Mathf.Min(spell.ReleaseTime, _oneShotTimer));
        EmitSignal(SignalName.SpellCastStarted, _selectedSpellIndex, spell.DisplayName);
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
        _damageInvulnerabilityTimer = RespawnInvulnerabilityDuration;
    }

    private void RestoreFullHealth()
    {
        _currentHealth = MaxHealth;
        EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);
    }

    private void CancelQueuedSpellCast()
    {
        _pendingSpellTimer = -1.0f;
        _pendingSpellIndex = -1;
    }

    private void SpawnSpellProjectile()
    {
        int spellIndex = _pendingSpellIndex >= 0 ? _pendingSpellIndex : _selectedSpellIndex;
        SpellConfig spell = GetSpellConfig(spellIndex);
        _pendingSpellIndex = -1;

        if (spell.ProjectileScene == null)
        {
            return;
        }

        Vector3 direction = ResolveSpellDirection();
        Vector3 spawnPoint =
            SpellOrigin?.GlobalPosition ??
            (GlobalPosition + Vector3.Up * 1.2f + direction * 0.9f);

        int projectileCount = Mathf.Max(1, spell.ProjectileCount);
        for (int projectileIndex = 0; projectileIndex < projectileCount; projectileIndex++)
        {
            SpellProjectile projectile = spell.ProjectileScene.Instantiate<SpellProjectile>();
            if (projectile == null)
            {
                GD.PushWarning("SpellProjectileScene does not instantiate a SpellProjectile.");
                return;
            }

            float centeredIndex = projectileIndex - (projectileCount - 1) * 0.5f;
            float spreadRadians = Mathf.DegToRad(centeredIndex * spell.ProjectileSpreadDegrees);
            Vector3 shotDirection = direction.Rotated(Vector3.Up, spreadRadians).Normalized();
            Vector3 shotSpawnPoint = spawnPoint + shotDirection * 0.08f * projectileIndex;

            GetTree().CurrentScene?.AddChild(projectile);
            projectile.GlobalPosition = shotSpawnPoint;
            projectile.Launch(
                this,
                shotDirection,
                spell.Damage,
                spell.ProjectileSpeed,
                spell.ProjectileRange,
                spell.ImpactScene,
                spell.MaxTargetHits,
                spellIndex);
        }

        EmitSignal(SignalName.SpellProjectilesReleased, spellIndex, projectileCount, spell.DisplayName);
    }

    public void NotifySpellHit(int spellIndex, int hitNumber, int maxTargetHits)
    {
        SpellConfig spell = GetSpellConfig(spellIndex);
        EmitSignal(SignalName.SpellHit, spellIndex, hitNumber, maxTargetHits, spell.DisplayName);
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

    private SpellConfig GetCurrentSpell()
    {
        return GetSpellConfig(_selectedSpellIndex);
    }

    private SpellConfig GetSpellConfig(int spellIndex)
    {
        int resolvedIndex = Mathf.Clamp(spellIndex, 0, GetSpellCount() - 1);
        return resolvedIndex switch
        {
            1 => new SpellConfig(
                BurstSpellDisplayName,
                "CastBurst",
                "Mixamo/cast_burst",
                BurstSpellDamage,
                BurstSpellCooldown,
                BurstSpellReleaseTime,
                BurstSpellProjectileSpeed,
                BurstSpellProjectileRange,
                BurstSpellProjectileCount,
                BurstSpellSpreadDegrees,
                1,
                BurstSpellProjectileScene,
                BurstSpellImpactScene),
            2 => new SpellConfig(
                LanceSpellDisplayName,
                "CastLance",
                "Mixamo/cast_lance",
                LanceSpellDamage,
                LanceSpellCooldown,
                LanceSpellReleaseTime,
                LanceSpellProjectileSpeed,
                LanceSpellProjectileRange,
                1,
                0.0f,
                LanceSpellMaxTargetHits,
                LanceSpellProjectileScene,
                LanceSpellImpactScene),
            _ => new SpellConfig(
                SpellDisplayName,
                "Cast",
                "Mixamo/cast",
                SpellDamage,
                SpellCooldown,
                SpellReleaseTime,
                SpellProjectileSpeed,
                SpellProjectileRange,
                1,
                0.0f,
                1,
                SpellProjectileScene,
                SpellImpactScene),
        };
    }

    private static bool IsSpellWheelOpen()
    {
        return Input.IsActionPressed("spell_wheel") && !IsDialogueOpen();
    }
}
