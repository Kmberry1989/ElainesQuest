using Godot;

public partial class EnemyCombatant : CharacterBody3D
{
    [ExportCategory("Combat")]
    [Export] public float MovementSpeed = 3.6f;
    [Export] public float Acceleration = 10.0f;
    [Export] public float DetectionRange = 18.0f;
    [Export] public float PreferredRange = 5.5f;
    [Export] public float AttackRange = 2.2f;
    [Export] public float AttackCooldown = 1.25f;
    [Export] public float ContactDamage = 1.0f;
    [Export] public int DefeatGlimmers = 4;

    [ExportCategory("Presentation")]
    [Export] public float HoverAmplitude = 0.18f;
    [Export] public float HoverSpeed = 2.4f;
    [Export] public float RotationSpeed = 8.0f;
    [Export] public float DeathDelay = 0.35f;

    [ExportCategory("Nodes")]
    [Export] public Node3D Visuals;
    [Export] public CombatHealth Health;
    [Export] public CollisionShape3D CollisionShape;
    [Export] public CombatHurtbox Hurtbox;
    [Export] public PackedScene DefeatEffectScene;

    private ElaineController _player;
    private float _attackCooldownRemaining = 0.0f;
    private bool _isDefeated = false;
    private float _baseVisualY = 0.0f;
    private double _hoverTime = 0.0f;

    public override void _Ready()
    {
        AddToGroup("enemy");

        Visuals ??= GetNodeOrNull<Node3D>("Visuals");
        Health ??= GetNodeOrNull<CombatHealth>("Health");
        CollisionShape ??= GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
        Hurtbox ??= GetNodeOrNull<CombatHurtbox>("Hurtbox");

        if (Visuals != null)
        {
            _baseVisualY = Visuals.Position.Y;
        }

        if (Health != null)
        {
            Health.Died += OnDefeated;
        }
    }

    public override void _ExitTree()
    {
        if (Health != null)
        {
            Health.Died -= OnDefeated;
        }
    }

    public override void _Process(double delta)
    {
        if (_isDefeated || Visuals == null)
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

        _attackCooldownRemaining = Mathf.Max(0.0f, _attackCooldownRemaining - (float)delta);
        _player ??= GetTree().GetFirstNodeInGroup("player") as ElaineController;

        if (_player == null)
        {
            Velocity = Vector3.Zero;
            return;
        }

        Vector3 toPlayer = _player.GlobalPosition - GlobalPosition;
        Vector3 flatToPlayer = new Vector3(toPlayer.X, 0.0f, toPlayer.Z);
        float distanceToPlayer = flatToPlayer.Length();

        if (distanceToPlayer > DetectionRange)
        {
            Velocity = Vector3.Zero;
            return;
        }

        Vector3 desiredVelocity = Vector3.Zero;
        if (distanceToPlayer > PreferredRange)
        {
            Vector3 moveDirection = flatToPlayer.Normalized();
            desiredVelocity = moveDirection * MovementSpeed;
        }

        Velocity = new Vector3(
            Mathf.MoveToward(Velocity.X, desiredVelocity.X, Acceleration * (float)delta),
            0.0f,
            Mathf.MoveToward(Velocity.Z, desiredVelocity.Z, Acceleration * (float)delta)
        );

        if (flatToPlayer.LengthSquared() > 0.001f)
        {
            float targetAngle = Mathf.Atan2(flatToPlayer.X, flatToPlayer.Z);
            float currentAngle = Rotation.Y;
            Rotation = new Vector3(0.0f, Mathf.LerpAngle(currentAngle, targetAngle, RotationSpeed * (float)delta), 0.0f);
        }

        if (distanceToPlayer <= AttackRange && _attackCooldownRemaining <= 0.0f)
        {
            if (_player.ReceiveDamage(ContactDamage, this))
            {
                _attackCooldownRemaining = AttackCooldown;
            }
        }

        MoveAndSlide();
    }

    private async void OnDefeated()
    {
        if (_isDefeated)
        {
            return;
        }

        _isDefeated = true;
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
        }

        if (DefeatGlimmers > 0)
        {
            GameManager.Instance?.AddGlimmers(DefeatGlimmers);
        }

        if (DefeatEffectScene != null)
        {
            Node3D effect = DefeatEffectScene.Instantiate<Node3D>();
            GetTree().CurrentScene?.AddChild(effect);
            effect.GlobalPosition = GlobalPosition + Vector3.Up * 1.1f;
        }

        if (Visuals != null)
        {
            Visuals.Visible = false;
        }

        await ToSignal(GetTree().CreateTimer(DeathDelay), SceneTreeTimer.SignalName.Timeout);
        QueueFree();
    }
}
