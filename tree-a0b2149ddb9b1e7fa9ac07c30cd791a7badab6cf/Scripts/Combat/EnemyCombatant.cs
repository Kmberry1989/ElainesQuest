using Godot;
using System;

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
    [Export] public Vector3 VisualOffset = Vector3.Zero;
    [Export] public Vector3 VisualRotationDegrees = Vector3.Zero;
    [Export] public Vector3 VisualScale = Vector3.One;

    [ExportCategory("Animation")]
    [Export] public EnemyAnimationProfile AnimationProfile;
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
    [Export] public bool AnimationPreviewMode = false;
    [Export] public float AttackImpactNormalized = -1.0f;
    [Export] public float AttackActiveNormalized = -1.0f;
    [Export] public float AttackRecoveryNormalized = -1.0f;
    [Export] public float SecondaryAttackImpactNormalized = -1.0f;
    [Export] public float SecondaryAttackActiveNormalized = -1.0f;
    [Export] public float SecondaryAttackRecoveryNormalized = -1.0f;

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
    private float _hitAnimationRemaining = 0.0f;
    private bool _isDefeated = false;
    private bool _isAttacking = false;
    private bool _attackWindowOpened = false;
    private bool _usingSecondaryAttack = false;
    private int _attackSequence = 0;
    private Vector3 _attackDirection = Vector3.Zero;
    private float _baseVisualY = 0.0f;
    private double _hoverTime = 0.0f;
    private OmniLight3D _attackTelegraph;

    public bool IsAnimationReady => _stateMachinePlayback != null;
    public bool IsDefeated => _isDefeated;
    public string CurrentAnimationState => _currentAnimationState;

    public bool HasAnimationClip(string clipName)
    {
        return _animationPlayer != null && _animationPlayer.HasAnimation($"Enemy/{clipName}");
    }

    public bool HasResolvedAnimationClip(string clipName)
    {
        if (_animationPlayer == null || AnimationTree == null || !AnimationTree.Active ||
            !_animationPlayer.HasAnimation($"Enemy/{clipName}"))
        {
            return false;
        }

        Node animationRoot = AnimationTree.GetNodeOrNull<Node>(AnimationTree.RootNode);
        Node playerRoot = _animationPlayer.GetNodeOrNull<Node>(_animationPlayer.RootNode);
        Animation animation = _animationPlayer.GetAnimation($"Enemy/{clipName}");
        if (animationRoot == null || animationRoot != playerRoot || animation == null || animation.GetTrackCount() == 0)
        {
            return false;
        }

        bool hasSkeletonTrack = false;
        for (int i = 0; i < animation.GetTrackCount(); i++)
        {
            string trackPath = animation.TrackGetPath(i).ToString();
            int propertySeparator = trackPath.IndexOf(':');
            string nodePath = propertySeparator >= 0 ? trackPath.Substring(0, propertySeparator) : trackPath;
            if (nodePath.Contains("Skeleton3D", System.StringComparison.Ordinal))
            {
                hasSkeletonTrack = true;
            }

            if (string.IsNullOrWhiteSpace(nodePath) || animationRoot.GetNodeOrNull<Node>(nodePath) == null)
            {
                return false;
            }
        }

        return hasSkeletonTrack;
    }

    public string GetAnimationBindingSummary()
    {
        Node treeRoot = AnimationTree == null ? null : AnimationTree.GetNodeOrNull<Node>(AnimationTree.RootNode);
        Node playerRoot = _animationPlayer == null ? null : _animationPlayer.GetNodeOrNull<Node>(_animationPlayer.RootNode);
        return $"treeActive={AnimationTree?.Active ?? false}, treeRoot={AnimationTree?.RootNode}, playerRoot={_animationPlayer?.RootNode}, treeResolved={treeRoot?.GetPath()}, playerResolved={playerRoot?.GetPath()}, sameRoot={treeRoot != null && treeRoot == playerRoot}";
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

    public Transform3D[] CaptureAnimationPose()
    {
        Skeleton3D skeleton = Visuals?.FindChild("Skeleton3D", true, false) as Skeleton3D;
        if (skeleton == null)
        {
            return Array.Empty<Transform3D>();
        }

        Transform3D[] pose = new Transform3D[skeleton.GetBoneCount()];
        for (int i = 0; i < pose.Length; i++)
        {
            pose[i] = skeleton.GetBonePose(i);
        }

        return pose;
    }

    public static float MeasureAnimationPoseDelta(Transform3D[] first, Transform3D[] second)
    {
        int count = Math.Min(first?.Length ?? 0, second?.Length ?? 0);
        float delta = 0.0f;
        for (int i = 0; i < count; i++)
        {
            delta += first[i].Origin.DistanceTo(second[i].Origin);
            delta += first[i].Basis.X.DistanceTo(second[i].Basis.X);
            delta += first[i].Basis.Y.DistanceTo(second[i].Basis.Y);
            delta += first[i].Basis.Z.DistanceTo(second[i].Basis.Z);
        }

        return delta;
    }

    public override void _Ready()
    {
        AddToGroup("enemy");

        ApplyAnimationProfile();

        Visuals ??= GetNodeOrNull<Node3D>("Visuals");
        Health ??= GetNodeOrNull<CombatHealth>("Health");
        CollisionShape ??= GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
        Hurtbox ??= GetNodeOrNull<CombatHurtbox>("Hurtbox");
        AttackHitbox ??= GetNodeOrNull<CombatMeleeHitbox>("AttackHitbox");
        AnimationTree ??= GetNodeOrNull<AnimationTree>("AnimationTree");

        if (Visuals != null)
        {
            Visuals.Position += VisualOffset;
            Visuals.RotationDegrees = VisualRotationDegrees;
            Visuals.Scale = VisualScale;
            _baseVisualY = Visuals.Position.Y;
        }

        if (Health != null)
        {
            Health.Damaged += OnDamaged;
            Health.Died += OnDefeated;
        }

        _attackTelegraph = new OmniLight3D
        {
            LightColor = new Color(1.0f, 0.48f, 0.18f),
            LightEnergy = 0.0f,
            OmniRange = 2.8f,
            ShadowEnabled = false,
            Name = "AttackTelegraph"
        };
        AddChild(_attackTelegraph);

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

        if (animationPlayer != null && animationRoot != null)
        {
            animationPlayer.RootNode = animationPlayer.GetPathTo(animationRoot);
        }

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

    private void ApplyAnimationProfile()
    {
        if (AnimationProfile == null)
        {
            return;
        }

        IdleAnimationScene = AnimationProfile.IdleAnimationScene ?? IdleAnimationScene;
        WalkAnimationScene = AnimationProfile.WalkAnimationScene ?? WalkAnimationScene;
        RunAnimationScene = AnimationProfile.RunAnimationScene ?? RunAnimationScene;
        AttackAnimationScene = AnimationProfile.AttackAnimationScene ?? AttackAnimationScene;
        SecondaryAttackAnimationScene = AnimationProfile.SecondaryAttackAnimationScene ?? SecondaryAttackAnimationScene;
        HitAnimationScene = AnimationProfile.HitAnimationScene ?? HitAnimationScene;
        DeathAnimationScene = AnimationProfile.DeathAnimationScene ?? DeathAnimationScene;
        AttackImpactNormalized = AnimationProfile.AttackImpactNormalized;
        AttackActiveNormalized = AnimationProfile.AttackActiveNormalized;
        AttackRecoveryNormalized = AnimationProfile.AttackRecoveryNormalized;
        SecondaryAttackImpactNormalized = AnimationProfile.SecondaryAttackImpactNormalized;
        SecondaryAttackActiveNormalized = AnimationProfile.SecondaryAttackActiveNormalized;
        SecondaryAttackRecoveryNormalized = AnimationProfile.SecondaryAttackRecoveryNormalized;
        VisualOffset = AnimationProfile.VisualOffset;
        VisualRotationDegrees = AnimationProfile.VisualRotationDegrees;
        VisualScale = AnimationProfile.VisualScale;
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
        AnimationNodeStateMachineTransition transition = new()
        {
            XfadeTime = 0.1f
        };
        stateMachine.AddTransition(from, to, transition);
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

        if (AnimationPreviewMode)
        {
            Velocity = Vector3.Zero;
            return;
        }

        float d = (float)delta;
        _attackCooldownRemaining = Mathf.Max(0.0f, _attackCooldownRemaining - d);
        _hitStunRemaining = Mathf.Max(0.0f, _hitStunRemaining - d);
        _hitAnimationRemaining = Mathf.Max(0.0f, _hitAnimationRemaining - d);
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

        if (_hitStunRemaining > 0.0f || _hitAnimationRemaining > 0.0f)
        {
            DecelerateHorizontal(d);
            TravelAnimationState(_hitAnimationRemaining > 0.0f && HitAnimationScene != null ? HitStateName : IdleStateName);
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
        if (_attackTelegraph != null)
        {
            _attackTelegraph.LightColor = _usingSecondaryAttack
                ? new Color(1.0f, 0.12f, 0.12f)
                : new Color(1.0f, 0.48f, 0.18f);
            _attackTelegraph.LightEnergy = 0.65f;
        }
        TravelAnimationState(_usingSecondaryAttack ? SecondaryAttackStateName : AttackStateName);
    }

    private void UpdateAttack(float delta)
    {
        _attackTimer += delta;

        if (_attackTelegraph != null)
        {
            float windup = Mathf.Max(0.05f, CurrentAttackWindup);
            float telegraphProgress = Mathf.Clamp(_attackTimer / windup, 0.0f, 1.0f);
            _attackTelegraph.LightEnergy = _attackTimer < windup
                ? Mathf.Lerp(0.65f, 2.4f, telegraphProgress)
                : 0.0f;
        }

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
        if (_attackTelegraph != null)
        {
            _attackTelegraph.LightEnergy = 0.0f;
        }
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
        _hitAnimationRemaining = Mathf.Max(_hitAnimationRemaining, ResolveAnimationDuration("hit", 0.35f));
        GameManager.Instance?.RequestHitStop(0.025f);

        if (_isAttacking)
        {
            FinishAttack();
        }

        if (HitAnimationScene != null)
        {
            TravelAnimationState(HitStateName);
        }
    }

    private float CurrentAttackWindup => ResolveAttackTiming(
        _usingSecondaryAttack ? SecondaryAttackWindup : AttackWindup,
        _usingSecondaryAttack ? SecondaryAttackImpactNormalized : AttackImpactNormalized,
        _usingSecondaryAttack ? "swipe" : "attack");

    private float CurrentAttackActiveDuration => ResolveAttackTiming(
        _usingSecondaryAttack ? SecondaryAttackActiveDuration : AttackActiveDuration,
        _usingSecondaryAttack ? SecondaryAttackActiveNormalized : AttackActiveNormalized,
        _usingSecondaryAttack ? "swipe" : "attack");

    private float CurrentAttackRecovery => ResolveAttackTiming(
        _usingSecondaryAttack ? SecondaryAttackRecovery : AttackRecovery,
        _usingSecondaryAttack ? SecondaryAttackRecoveryNormalized : AttackRecoveryNormalized,
        _usingSecondaryAttack ? "swipe" : "attack");

    private float ResolveAttackTiming(float fallback, float normalized, string clipName)
    {
        float clipDuration = ResolveAnimationDuration(clipName, 0.0f);
        return normalized >= 0.0f && clipDuration > 0.0f ? clipDuration * normalized : fallback;
    }

    private float ResolveAnimationDuration(string clipName, float fallback)
    {
        Animation animation = _animationPlayer?.GetAnimation($"Enemy/{clipName}");
        return animation == null ? fallback : (float)animation.Length;
    }

    private async void OnDefeated()
    {
        if (_isDefeated)
        {
            return;
        }

        _isDefeated = true;
        _hitStunRemaining = 0.0f;
        _hitAnimationRemaining = 0.0f;
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
