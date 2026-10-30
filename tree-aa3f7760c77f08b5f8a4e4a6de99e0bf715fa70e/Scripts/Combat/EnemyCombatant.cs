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
    [Export] public string DeathStateName = "Death";
    [Export] public PackedScene IdleAnimationScene;
    [Export] public PackedScene WalkAnimationScene;
    [Export] public PackedScene RunAnimationScene;
    [Export] public PackedScene AttackAnimationScene;
    [Export] public PackedScene SecondaryAttackAnimationScene;
    [Export] public PackedScene DeathAnimationScene;

    [ExportCategory("Nodes")]
    [Export] public Node3D Visuals;
    [Export] public CombatHealth Health;
    [Export] public CollisionShape3D CollisionShape;
    [Export] public CombatHurtbox Hurtbox;
    [Export] public CombatMeleeHitbox AttackHitbox;
    [Export] public PackedScene DefeatEffectScene;

    private ElaineController _player;
    private AnimationNodeStateMachinePlayback _stateMachinePlayback;
    private string _currentAnimationState = string.Empty;
    private float _attackCooldownRemaining = 0.0f;
    private float _attackTimer = 0.0f;
    private float _hitStunRemaining = 0.0f;
    private bool _isDefeated = false;
    private bool _isAttacking = false;
    private bool _attackWindowOpened = false;
    private float _baseVisualY = 0.0f;
    private double _hoverTime = 0.0f;

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
        stateMachine.AddNode(DeathStateName, new AnimationNodeAnimation { Animation = "Enemy/death" }, new Vector2(560, -140));

        AnimationNodeStateMachineTransition startToIdle = new();
        startToIdle.AdvanceMode = AnimationNodeStateMachineTransition.AdvanceModeEnum.Auto;
        stateMachine.AddTransition("Start", IdleStateName, startToIdle);

        AddTransition(stateMachine, IdleStateName, WalkStateName);
        AddTransition(stateMachine, IdleStateName, RunStateName);
        AddTransition(stateMachine, IdleStateName, AttackStateName);
        AddTransition(stateMachine, IdleStateName, DeathStateName);
        AddTransition(stateMachine, WalkStateName, IdleStateName);
        AddTransition(stateMachine, WalkStateName, RunStateName);
        AddTransition(stateMachine, WalkStateName, AttackStateName);
        AddTransition(stateMachine, WalkStateName, DeathStateName);
        AddTransition(stateMachine, RunStateName, IdleStateName);
        AddTransition(stateMachine, RunStateName, WalkStateName);
        AddTransition(stateMachine, RunStateName, AttackStateName);
        AddTransition(stateMachine, RunStateName, DeathStateName);
        AddTransition(stateMachine, AttackStateName, IdleStateName);
        AddTransition(stateMachine, AttackStateName, WalkStateName);
        AddTransition(stateMachine, AttackStateName, RunStateName);
        AddTransition(stateMachine, AttackStateName, DeathStateName);

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
        DecelerateHorizontal(AttackWindup);
        TravelAnimationState(AttackStateName);
    }

    private void UpdateAttack(float delta)
    {
        DecelerateHorizontal(delta);
        _attackTimer += delta;

        if (!_attackWindowOpened && _attackTimer >= AttackWindup)
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

        if (_attackWindowOpened && _attackTimer >= AttackWindup + AttackActiveDuration)
        {
            AttackHitbox?.EndAttack();
        }

        if (_attackTimer >= AttackWindup + AttackActiveDuration + AttackRecovery)
        {
            FinishAttack();
        }
    }

    private void FinishAttack()
    {
        AttackHitbox?.EndAttack();
        _isAttacking = false;
        _attackWindowOpened = false;
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
    }

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
            CollisionShape.Disabled = true;
        }

        if (Hurtbox != null)
        {
            Hurtbox.Monitorable = false;
            Hurtbox.Monitoring = false;
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
