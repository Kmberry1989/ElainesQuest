using Godot;

public partial class EnemyCombatant : CharacterBody3D
{
    [ExportCategory("Combat")]
    [Export] public float WalkSpeed = 2.4f;
    [Export] public float RunSpeed = 4.2f;
    [Export] public float Acceleration = 12.0f;
    [Export] public float DetectionRange = 18.0f;
    [Export] public float PreferredRange = 6.0f;
    [Export] public float AttackRange = 2.2f;
    [Export] public float AttackCooldown = 1.25f;
    [Export] public float AttackDamage = 1.0f;
    [Export] public float AttackWindup = 0.3f;
    [Export] public float AttackActiveDuration = 0.2f;
    [Export] public float AttackRecovery = 0.35f;
    [Export] public float HitStunDuration = 0.18f;
    [Export] public int DefeatGlimmers = 4;

    [ExportCategory("Presentation")]
    [Export] public float HoverAmplitude = 0.0f;
    [Export] public float HoverSpeed = 2.0f;
    [Export] public float RotationSpeed = 10.0f;
    [Export] public float DeathDelay = 0.9f;

    [ExportCategory("Animation")]
    [Export] public AnimationTree AnimationTree;
    [Export] public string IdleStateName = "Idle";
    [Export] public string WalkStateName = "Walk";
    [Export] public string RunStateName = "Run";
    [Export] public string AttackStateName = "Attack";
    [Export] public string SecondaryAttackStateName = "SecondaryAttack";
    [Export] public string HitStateName = "Hit";
    [Export] public string DeathStateName = "Death";
    [Export] public PackedScene IdleAnimationScene;
    [Export] public PackedScene WalkAnimationScene;
    [Export] public PackedScene RunAnimationScene;
    [Export] public PackedScene AttackAnimationScene;
    [Export] public PackedScene SecondaryAttackAnimationScene;
    [Export] public PackedScene HitAnimationScene;
    [Export] public PackedScene DeathAnimationScene;
    [Export] public bool AlternateAttacks = false;
    [Export] public float SecondaryAttackWindup = -1.0f;
    [Export] public float SecondaryAttackActiveDuration = -1.0f;
    [Export] public float SecondaryAttackRecovery = -1.0f;
    [Export] public float SecondaryAttackMoveSpeed = 0.0f;

    [ExportCategory("Nodes")]
    [Export] public Node3D Visuals;
    [Export] public CombatHealth Health;
    [Export] public CollisionShape3D CollisionShape;
    [Export] public CombatHurtbox Hurtbox;
    [Export] public CombatMeleeHitbox AttackHitbox;
    [Export] public PackedScene DefeatEffectScene;

    private ElaineController _player;
    private AnimationPlayer _animationPlayer;
    private AnimationNodeStateMachinePlayback _stateMachinePlayback;
    private string _currentAnimationState = string.Empty;
    private float _attackCooldownRemaining = 0.0f;
    private float _attackTimer = 0.0f;
    private float _hitStunRemaining = 0.0f;
    private bool _isDefeated = false;
    private bool _isAttacking = false;
    private bool _attackWindowOpened = false;
    private bool _usingSecondaryAttack = false;
    private int _attackSequence = 0;
    private Vector3 _attackDirection = Vector3.Zero;
    private float _baseVisualY = 0.0f;
    private double _hoverTime = 0.0f;

    public bool IsAnimationReady => _stateMachinePlayback != null;
    public bool IsDefeated => _isDefeated;
    public string CurrentAnimationState => _currentAnimationState;

    public bool HasAnimationClip(string clipName)
    {
        return _animationPlayer != null && _animationPlayer.HasAnimation($"Enemy/{clipName}");
    }

    public bool DebugTravelAnimationState(string stateName)
    {
        if (!IsAnimationReady || string.IsNullOrWhiteSpace(stateName))
        {
            return false;
        }

        TravelAnimationState(stateName);
        return CurrentAnimationState == stateName;
    }

    public override void _Ready()
    {
        AddToGroup("enemy");

        Visuals ??= GetNodeOrNull<Node3D>("Visuals");
        Health ??= GetNodeOrNull<CombatHealth>("Health");
        CollisionShape ??= GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
        Hurtbox ??= GetNodeOrNull<CombatHurtbox>("Hurtbox");
        AttackHitbox ??= GetNodeOrNull<CombatMeleeHitbox>("AttackHitbox");
        AnimationTree ??= GetNodeOrNull<AnimationTree>("AnimationTree");

        if (Visuals != null)
        {
            _baseVisualY = Visuals.Position.Y;
        }

        if (Health != null)
        {
            Health.Damaged += OnDamaged;
            Health.Died += OnDefeated;
        }

        AttackHitbox?.EndAttack();
        CallDeferred(nameof(InitializeAnimation));
    }

    public override void _ExitTree()
    {
        if (Health != null)
        {
            Health.Damaged -= OnDamaged;
            Health.Died -= OnDefeated;
        }
    }

    private void InitializeAnimation()
    {
        if (Visuals == null || AnimationTree == null)
        {
            TravelAnimationState(IdleStateName);
            return;
        }

        AnimationPlayer animationPlayer = FindOrCreateAnimationPlayer();
        _animationPlayer = animationPlayer;
        Node animationRoot =
            Visuals.FindChild("RootNode", true, false) ??
            Visuals.FindChild("Skeleton3D", true, false)?.GetParent() ??
            Visuals;

        if (animationPlayer != null)
        {
            AnimationLibrary library = BuildAnimationLibrary();
            if (library != null)
            {
                animationPlayer.AddAnimationLibrary("Enemy", library);
            }
        }

        if (AnimationTree.TreeRoot == null)
        {
            AnimationTree.TreeRoot = BuildStateMachine();
        }

        if (animationPlayer != null)
        {
            AnimationTree.AnimPlayer = AnimationTree.GetPathTo(animationPlayer);
        }

        if (animationRoot != null)
        {
            AnimationTree.RootNode = AnimationTree.GetPathTo(animationRoot);
        }

        AnimationTree.Active = false;
        AnimationTree.Active = true;
        _stateMachinePlayback = AnimationTree.Get("parameters/playback").As<AnimationNodeStateMachinePlayback>();
        TravelAnimationState(IdleStateName);
    }

    private AnimationPlayer FindOrCreateAnimationPlayer()
    {
        AnimationPlayer animationPlayer = Visuals?.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
        if (animationPlayer != null || Visuals == null)
        {
            return animationPlayer;
        }

        animationPlayer = new AnimationPlayer
        {
            Name = "AnimationPlayer"
        };
        Visuals.AddChild(animationPlayer);
        animationPlayer.Owner = GetTree().EditedSceneRoot == this ? this : null;
        return animationPlayer;
    }

    private AnimationLibrary BuildAnimationLibrary()
    {
        AnimationLibrary library = new();
        bool hasAnimations = false;

        AddAnimationToLibrary(library, "idle", IdleAnimationScene, true, ref hasAnimations);
        AddAnimationToLibrary(library, "walk", WalkAnimationScene, true, ref hasAnimations);
        AddAnimationToLibrary(library, "run", RunAnimationScene, true, ref hasAnimations);
        AddAnimationToLibrary(library, "attack", AttackAnimationScene, false, ref hasAnimations);
        AddAnimationToLibrary(library, "swipe", SecondaryAttackAnimationScene, false, ref hasAnimations);
        AddAnimationToLibrary(library, "hit", HitAnimationScene, false, ref hasAnimations);
        AddAnimationToLibrary(library, "death", DeathAnimationScene, false, ref hasAnimations);

        return hasAnimations ? library : null;
    }

    private AnimationNodeStateMachine BuildStateMachine()
    {
        AnimationNodeStateMachine stateMachine = new();

        stateMachine.AddNode(IdleStateName, new AnimationNodeAnimation { Animation = "Enemy/idle" }, new Vector2(80, 60));
        stateMachine.AddNode(WalkStateName, new AnimationNodeAnimation { Animation = "Enemy/walk" }, new Vector2(320, 60));
        stateMachine.AddNode(RunStateName, new AnimationNodeAnimation { Animation = "Enemy/run" }, new Vector2(560, 60));
        stateMachine.AddNode(AttackStateName, new AnimationNodeAnimation { Animation = "Enemy/attack" }, new Vector2(320, -140));
        stateMachine.AddNode(SecondaryAttackStateName, new AnimationNodeAnimation { Animation = "Enemy/swipe" }, new Vector2(560, -140));
        stateMachine.AddNode(HitStateName, new AnimationNodeAnimation { Animation = "Enemy/hit" }, new Vector2(800, -140));
        stateMachine.AddNode(DeathStateName, new AnimationNodeAnimation { Animation = "Enemy/death" }, new Vector2(1040, -140));

        AnimationNodeStateMachineTransition startToIdle = new();
        startToIdle.AdvanceMode = AnimationNodeStateMachineTransition.AdvanceModeEnum.Auto;
        stateMachine.AddTransition("Start", IdleStateName, startToIdle);

        string[] states = { IdleStateName, WalkStateName, RunStateName, AttackStateName, SecondaryAttackStateName, HitStateName, DeathStateName };
        foreach (string from in states)
        {
            foreach (string to in states)
            {
                if (from != to)
                {
                    AddTransition(stateMachine, from, to);
                }
            }
        }

        return stateMachine;
    }

    private void AddAnimationToLibrary(AnimationLibrary library, string libraryName, PackedScene scene, bool shouldLoop, ref bool hasAnimations)
    {
        if (scene == null)
        {
            return;
        }

        Node node = scene.Instantiate();
        AnimationPlayer animationPlayer = node.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        if (animationPlayer == null)
        {
            node.QueueFree();
            return;
        }

        string animationName = ResolveAnimationName(animationPlayer);
        Animation animation = animationPlayer.GetAnimation(animationName);
        if (animation == null)
        {
            node.QueueFree();
            return;
        }

        Animation rebuilt = (Animation)animation.Duplicate();
        NormalizeTrackPaths(rebuilt);
        rebuilt.LoopMode = shouldLoop ? Animation.LoopModeEnum.Linear : Animation.LoopModeEnum.None;
        library.AddAnimation(libraryName, rebuilt);
        hasAnimations = true;
        node.QueueFree();
    }

    private static string ResolveAnimationName(AnimationPlayer animationPlayer)
    {
        if (animationPlayer.HasAnimation("mixamo_com"))
        {
            return "mixamo_com";
        }

        if (animationPlayer.HasAnimation("mixamo.com"))
        {
            return "mixamo.com";
        }

        return animationPlayer.GetAnimationList()[0];
    }

    private static void NormalizeTrackPaths(Animation animation)
    {
        for (int i = 0; i < animation.GetTrackCount(); i++)
        {
            string path = animation.TrackGetPath(i).ToString();
            int skeletonIndex = path.IndexOf("Skeleton3D", System.StringComparison.Ordinal);
            if (skeletonIndex == -1)
            {
                continue;
            }

            string trimmed = path.Substring(skeletonIndex);
            animation.TrackSetPath(i, new NodePath(trimmed));
        }
    }

    private static void AddTransition(AnimationNodeStateMachine stateMachine, string from, string to)
    {
        stateMachine.AddTransition(from, to, new AnimationNodeStateMachineTransition());
    }

    public override void _Process(double delta)
    {
        if (_isDefeated || Visuals == null || HoverAmplitude <= 0.0f)
        {
            return;
        }

        _hoverTime += delta * HoverSpeed;
        Vector3 visualPosition = Visuals.Position;
        visualPosition.Y = _baseVisualY + Mathf.Sin((float)_hoverTime) * HoverAmplitude;
        Visuals.Position = visualPosition;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDefeated)
        {
            Velocity = Vector3.Zero;
            return;
        }

        float d = (float)delta;
        _attackCooldownRemaining = Mathf.Max(0.0f, _attackCooldownRemaining - d);
        _hitStunRemaining = Mathf.Max(0.0f, _hitStunRemaining - d);
        _player ??= GetTree().GetFirstNodeInGroup("player") as ElaineController;

        if (_player == null)
        {
            DecelerateHorizontal(d);
            TravelAnimationState(IdleStateName);
            MoveAndSlide();
            return;
        }

        Vector3 toPlayer = _player.GlobalPosition - GlobalPosition;
        Vector3 flatToPlayer = new Vector3(toPlayer.X, 0.0f, toPlayer.Z);
        float distanceToPlayer = flatToPlayer.Length();

        if (flatToPlayer.LengthSquared() > 0.001f)
        {
            FaceDirection(flatToPlayer, d);
        }

        if (_isAttacking)
        {
            UpdateAttack(d);
            MoveAndSlide();
            return;
        }

        if (_hitStunRemaining > 0.0f)
        {
            DecelerateHorizontal(d);
            TravelAnimationState(IdleStateName);
            MoveAndSlide();
            return;
        }

        if (distanceToPlayer > DetectionRange)
        {
            DecelerateHorizontal(d);
            TravelAnimationState(IdleStateName);
            MoveAndSlide();
            return;
        }

        if (distanceToPlayer <= AttackRange && _attackCooldownRemaining <= 0.0f)
        {
            StartAttack();
            MoveAndSlide();
            return;
        }

        if (distanceToPlayer > AttackRange)
        {
            Vector3 moveDirection = flatToPlayer.Normalized();
            float targetSpeed = distanceToPlayer > PreferredRange ? RunSpeed : WalkSpeed;
            ApplyDesiredVelocity(moveDirection * targetSpeed, d);
            TravelAnimationState(distanceToPlayer > PreferredRange ? RunStateName : WalkStateName);
        }
        else
        {
            DecelerateHorizontal(d);
            TravelAnimationState(IdleStateName);
        }

        MoveAndSlide();
    }

    private void StartAttack()
    {
        _isAttacking = true;
        _attackTimer = 0.0f;
        _attackWindowOpened = false;
        _usingSecondaryAttack = AlternateAttacks && SecondaryAttackAnimationScene != null && _attackSequence++ % 2 == 1;
        Vector3 toPlayer = _player?.GlobalPosition - GlobalPosition ?? Vector3.Zero;
        _attackDirection = new Vector3(toPlayer.X, 0.0f, toPlayer.Z).Normalized();
        DecelerateHorizontal(CurrentAttackWindup);
        TravelAnimationState(_usingSecondaryAttack ? SecondaryAttackStateName : AttackStateName);
    }

    private void UpdateAttack(float delta)
    {
        _attackTimer += delta;

        bool isCharging = _usingSecondaryAttack && SecondaryAttackMoveSpeed > 0.0f &&
            _attackTimer >= CurrentAttackWindup &&
            _attackTimer < CurrentAttackWindup + CurrentAttackActiveDuration;
        if (isCharging && _attackDirection.LengthSquared() > 0.001f)
        {
            ApplyDesiredVelocity(_attackDirection * SecondaryAttackMoveSpeed, delta);
        }
        else
        {
            DecelerateHorizontal(delta);
        }

        if (!_attackWindowOpened && _attackTimer >= CurrentAttackWindup)
        {
            _attackWindowOpened = true;

            if (AttackHitbox != null)
            {
                AttackHitbox.BeginAttack(this, AttackDamage);
            }
            else if (_player != null && GlobalPosition.DistanceTo(_player.GlobalPosition) <= AttackRange + 0.35f)
            {
                _player.ReceiveDamage(AttackDamage, this);
            }
        }

        if (_attackWindowOpened && _attackTimer >= CurrentAttackWindup + CurrentAttackActiveDuration)
        {
            AttackHitbox?.EndAttack();
        }

        if (_attackTimer >= CurrentAttackWindup + CurrentAttackActiveDuration + CurrentAttackRecovery)
        {
            FinishAttack();
        }
    }

    private void FinishAttack()
    {
        AttackHitbox?.EndAttack();
        _isAttacking = false;
        _attackWindowOpened = false;
        _usingSecondaryAttack = false;
        _attackTimer = 0.0f;
        _attackCooldownRemaining = AttackCooldown;
    }

    private void ApplyDesiredVelocity(Vector3 desiredVelocity, float delta)
    {
        Velocity = new Vector3(
            Mathf.MoveToward(Velocity.X, desiredVelocity.X, Acceleration * delta),
            0.0f,
            Mathf.MoveToward(Velocity.Z, desiredVelocity.Z, Acceleration * delta)
        );
    }

    private void DecelerateHorizontal(float delta)
    {
        Velocity = new Vector3(
            Mathf.MoveToward(Velocity.X, 0.0f, Acceleration * delta),
            0.0f,
            Mathf.MoveToward(Velocity.Z, 0.0f, Acceleration * delta)
        );
    }

    private void FaceDirection(Vector3 flatDirection, float delta)
    {
        float targetAngle = Mathf.Atan2(flatDirection.X, flatDirection.Z);
        float currentAngle = Rotation.Y;
        Rotation = new Vector3(0.0f, Mathf.LerpAngle(currentAngle, targetAngle, RotationSpeed * delta), 0.0f);
    }

    private void TravelAnimationState(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName) || _currentAnimationState == stateName)
        {
            return;
        }

        if (_stateMachinePlayback != null)
        {
            _stateMachinePlayback.Travel(stateName);
        }

        _currentAnimationState = stateName;
    }

    private void OnDamaged(float _amount, float currentHealth, float _maxHealth)
    {
        if (_isDefeated || currentHealth <= 0.0f)
        {
            return;
        }

        _hitStunRemaining = Mathf.Max(_hitStunRemaining, HitStunDuration);

        if (_isAttacking)
        {
            FinishAttack();
        }

        if (HitAnimationScene != null)
        {
            TravelAnimationState(HitStateName);
        }
    }

    private float CurrentAttackWindup => _usingSecondaryAttack && SecondaryAttackWindup >= 0.0f ? SecondaryAttackWindup : AttackWindup;
    private float CurrentAttackActiveDuration => _usingSecondaryAttack && SecondaryAttackActiveDuration >= 0.0f ? SecondaryAttackActiveDuration : AttackActiveDuration;
    private float CurrentAttackRecovery => _usingSecondaryAttack && SecondaryAttackRecovery >= 0.0f ? SecondaryAttackRecovery : AttackRecovery;

    private async void OnDefeated()
    {
        if (_isDefeated)
        {
            return;
        }

        _isDefeated = true;
        _hitStunRemaining = 0.0f;
        FinishAttack();
        Velocity = Vector3.Zero;

        if (CollisionShape != null)
        {
            CollisionShape.SetDeferred("disabled", true);
        }

        if (Hurtbox != null)
        {
            Hurtbox.SetDeferred("monitorable", false);
            Hurtbox.SetDeferred("monitoring", false);
        }

        AttackHitbox?.EndAttack();

        if (DefeatGlimmers > 0)
        {
            GameManager.Instance?.AddGlimmers(DefeatGlimmers);
        }

        TravelAnimationState(DeathStateName);

        if (DefeatEffectScene != null)
        {
            Node3D effect = DefeatEffectScene.Instantiate<Node3D>();
            GetTree().CurrentScene?.AddChild(effect);
            effect.GlobalPosition = GlobalPosition + Vector3.Up * 1.1f;
        }

        await ToSignal(GetTree().CreateTimer(DeathDelay), SceneTreeTimer.SignalName.Timeout);
        QueueFree();
    }
}
