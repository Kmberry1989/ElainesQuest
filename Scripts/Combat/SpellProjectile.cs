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
    private readonly System.Collections.Generic.HashSet<ulong> _hitTargets = new();
    private readonly Godot.Collections.Array<Rid> _sweepExcluded = new();

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
        _sweepExcluded.Clear();
        _sweepExcluded.Add(GetRid());
        ExcludeCasterColliders(caster);
        _travelDirection = direction.LengthSquared() > 0.0f ? direction.Normalized() : -Vector3.Forward;
        _damage = damage > 0.0f ? damage : DefaultDamage;
        _speed = speed > 0.0f ? speed : DefaultSpeed;
        _range = range > 0.0f ? range : DefaultRange;
        _maxTargetHits = Mathf.Max(1, maxTargetHits);
        _remainingTargetHits = _maxTargetHits;
        _spellIndex = spellIndex;
        ImpactScene = impactScene ?? ImpactScene;

        Vector3 up = Mathf.Abs(_travelDirection.Dot(Vector3.Up)) > 0.99f ? Vector3.Forward : Vector3.Up;
        LookAt(GlobalPosition + _travelDirection, up, true);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isSpent)
        {
            return;
        }

        float step = Mathf.Min(_speed * (float)delta, _range - _distanceTravelled);
        Vector3 start = GlobalPosition;
        Vector3 end = start + _travelDirection * step;
        // Area overlap signals alone can skip walls or targets between physics ticks.
        var excluded = new Godot.Collections.Array<Rid>(_sweepExcluded);
        while (!_isSpent)
        {
            var query = PhysicsRayQueryParameters3D.Create(start, end, CollisionMask, excluded);
            query.CollideWithAreas = true;
            query.CollideWithBodies = true;
            var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
            if (hit.Count == 0) break;
            excluded.Add(hit["rid"].AsRid());
            GlobalPosition = hit["position"].AsVector3();
            Node3D collider = hit["collider"].AsGodotObject() as Node3D;
            if (collider is Area3D area) OnAreaEntered(area);
            else if (collider != null) OnBodyEntered(collider);
        }
        if (_isSpent) return;
        GlobalPosition = end;
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

        ulong targetId = IsInstanceValid(hurtbox.Health) ? hurtbox.Health.GetInstanceId() : hurtbox.GetInstanceId();
        if (_hitTargets.Contains(targetId))
        {
            return;
        }

        if (hurtbox.ApplySpellHit(_damage))
        {
            _hitTargets.Add(targetId);
            Vector3 travelPosition = GlobalPosition;
            GlobalPosition = hurtbox.GetImpactPoint();
            _remainingTargetHits--;
            if (IsInstanceValid(_caster) && _caster is ElaineController elaine)
            {
                elaine.NotifySpellHit(_spellIndex, _hitTargets.Count, _maxTargetHits);
            }
            GameManager.Instance?.RequestHitStop(_spellIndex == 2 ? 0.05f : 0.025f);
            SpawnImpact();
            GlobalPosition = travelPosition;

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

        CombatHurtbox hurtbox = body.GetNodeOrNull<CombatHurtbox>("Hurtbox");
        if (hurtbox != null)
        {
            OnAreaEntered(hurtbox);
            return;
        }
        ImpactAndFree();
    }

    private void ExcludeCasterColliders(Node node)
    {
        if (!IsInstanceValid(node)) return;
        if (node is CollisionObject3D collider) _sweepExcluded.Add(collider.GetRid());
        foreach (Node child in node.GetChildren()) ExcludeCasterColliders(child);
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
