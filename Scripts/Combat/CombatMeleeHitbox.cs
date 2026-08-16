using Godot;
using System.Collections.Generic;

public partial class CombatMeleeHitbox : Area3D
{
    [Export] public string TargetGroup = "player";

    private readonly HashSet<ulong> _hitTargets = new();
    private Node _source;
    private float _damage = 0.0f;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        SetActive(false);
    }

    public void BeginAttack(Node source, float damage)
    {
        _source = source;
        _damage = Mathf.Max(0.0f, damage);
        _hitTargets.Clear();
        SetActive(true);
        CallDeferred(nameof(ApplyCurrentOverlaps));
    }

    public void EndAttack()
    {
        SetActive(false);
        _hitTargets.Clear();
        _source = null;
        _damage = 0.0f;
    }

    private void ApplyCurrentOverlaps()
    {
        if (!Monitoring)
        {
            return;
        }

        foreach (Node3D body in GetOverlappingBodies())
        {
            ApplyToBody(body);
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        ApplyToBody(body);
    }

    private void ApplyToBody(Node body)
    {
        if (!Monitoring || body == null || _damage <= 0.0f)
        {
            return;
        }

        Node target = ResolveTarget(body);
        if (target == null)
        {
            return;
        }

        ulong instanceId = target.GetInstanceId();
        if (!_hitTargets.Add(instanceId))
        {
            return;
        }

        if (target is ElaineController player && !player.ReceiveDamage(_damage, _source ?? this))
        {
            _hitTargets.Remove(instanceId);
        }
    }

    private Node ResolveTarget(Node node)
    {
        Node current = node;
        while (current != null)
        {
            if (current.IsInGroup(TargetGroup))
            {
                return current;
            }

            current = current.GetParent();
        }

        return null;
    }

    private void SetActive(bool enabled)
    {
        SetDeferred("monitoring", enabled);
        SetDeferred("monitorable", enabled);
    }
}
