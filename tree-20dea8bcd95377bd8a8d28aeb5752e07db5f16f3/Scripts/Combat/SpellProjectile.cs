using Godot;

public partial class SpellProjectile : Area3D
{
    [Export] public float DefaultDamage = 1.0f;
    [Export] public float DefaultSpeed = 24.0f;
    [Export] public float DefaultRange = 28.0f;
    [Export] public PackedScene ImpactScene;

    private Node _caster;
    private Vector3 _travelDirection = -Vector3.Forward;
    private float _damage = 1.0f;
    private float _speed = 24.0f;
    private float _range = 28.0f;
    private float _distanceTravelled = 0.0f;
    private bool _isSpent = false;

    public override void _Ready()
    {
        AreaEntered += OnAreaEntered;
        BodyEntered += OnBodyEntered;
    }

    public void Launch(
        Node caster,
        Vector3 direction,
        float damage,
        float speed,
        float range,
        PackedScene impactScene = null)
    {
        _caster = caster;
        _travelDirection = direction.LengthSquared() > 0.0f ? direction.Normalized() : -Vector3.Forward;
        _damage = damage > 0.0f ? damage : DefaultDamage;
        _speed = speed > 0.0f ? speed : DefaultSpeed;
        _range = range > 0.0f ? range : DefaultRange;
        ImpactScene = impactScene ?? ImpactScene;

        LookAt(GlobalPosition + _travelDirection, Vector3.Up, true);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isSpent)
        {
            return;
        }

        float step = _speed * (float)delta;
        GlobalPosition += _travelDirection * step;
        _distanceTravelled += step;

        if (_distanceTravelled >= _range)
        {
            ImpactAndFree();
        }
    }

    private void OnAreaEntered(Area3D area)
    {
        if (_isSpent || IsCasterNode(area))
        {
            return;
        }

        CombatHurtbox hurtbox = ResolveHurtbox(area);
        if (hurtbox == null)
        {
            return;
        }

        if (hurtbox.ApplySpellHit(_damage))
        {
            GlobalPosition = hurtbox.GetImpactPoint();
            ImpactAndFree();
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        if (_isSpent || IsCasterNode(body))
        {
            return;
        }

        ImpactAndFree();
    }

    private CombatHurtbox ResolveHurtbox(Node node)
    {
        Node current = node;
        while (current != null)
        {
            if (current is CombatHurtbox hurtbox)
            {
                return hurtbox;
            }

            current = current.GetParent();
        }

        return null;
    }

    private bool IsCasterNode(Node node)
    {
        Node current = node;
        while (current != null)
        {
            if (current == _caster)
            {
                return true;
            }

            current = current.GetParent();
        }

        return false;
    }

    private void ImpactAndFree()
    {
        if (_isSpent)
        {
            return;
        }

        _isSpent = true;

        if (ImpactScene != null)
        {
            Node3D effect = ImpactScene.Instantiate<Node3D>();
            GetTree().CurrentScene?.AddChild(effect);
            effect.GlobalPosition = GlobalPosition;
        }

        QueueFree();
    }
}
