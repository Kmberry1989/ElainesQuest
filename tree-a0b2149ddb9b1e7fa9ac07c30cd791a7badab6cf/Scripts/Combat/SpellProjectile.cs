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
    private int _remainingTargetHits = 1;
    private int _maxTargetHits = 1;
    private int _spellIndex = -1;
    private bool _isSpent = false;
    private readonly Godot.Collections.Array<CombatHurtbox> _hitHurtboxes = new();

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
        PackedScene impactScene = null,
        int maxTargetHits = 1,
        int spellIndex = -1)
    {
        _caster = caster;
        _travelDirection = direction.LengthSquared() > 0.0f ? direction.Normalized() : -Vector3.Forward;
        _damage = damage > 0.0f ? damage : DefaultDamage;
        _speed = speed > 0.0f ? speed : DefaultSpeed;
        _range = range > 0.0f ? range : DefaultRange;
        _maxTargetHits = Mathf.Max(1, maxTargetHits);
        _remainingTargetHits = _maxTargetHits;
        _spellIndex = spellIndex;
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

        if (_hitHurtboxes.Contains(hurtbox))
        {
            return;
        }

        if (hurtbox.ApplySpellHit(_damage))
        {
            _hitHurtboxes.Add(hurtbox);
            GlobalPosition = hurtbox.GetImpactPoint();
            _remainingTargetHits--;
            if (_caster is ElaineController elaine)
            {
                elaine.NotifySpellHit(_spellIndex, _hitHurtboxes.Count, _maxTargetHits);
            }
            GameManager.Instance?.RequestHitStop(_spellIndex == 2 ? 0.05f : 0.025f);
            SpawnImpact();

            if (_remainingTargetHits <= 0)
            {
                QueueFree();
                _isSpent = true;
            }
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

        SpawnImpact();

        QueueFree();
    }

    private void SpawnImpact()
    {
        if (ImpactScene == null)
        {
            return;
        }

        Node3D effect = ImpactScene.Instantiate<Node3D>();
        if (effect is OneShotEffect oneShot)
        {
            oneShot.SpellIndex = _spellIndex;
        }
        GetTree().CurrentScene?.AddChild(effect);
        effect.GlobalPosition = GlobalPosition;
    }
}
