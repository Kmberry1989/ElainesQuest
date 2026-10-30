using Godot;

public partial class CombatHurtbox : Area3D
{
    [Export] public CombatHealth Health;
    [Export] public Node3D ImpactAnchor;

    public override void _Ready()
    {
        AddToGroup("combat_hurtbox");
        Monitoring = true;
        Monitorable = true;

        Health ??= ResolveHealth();
        ImpactAnchor ??= this;
    }

    public bool ApplySpellHit(float damage)
    {
        return ApplyHit(damage);
    }

    public bool ApplyHit(float damage)
    {
        Health ??= ResolveHealth();
        return Health?.ApplyDamage(damage) ?? false;
    }

    public Vector3 GetImpactPoint()
    {
        return ImpactAnchor?.GlobalPosition ?? GlobalPosition;
    }

    private CombatHealth ResolveHealth()
    {
        Node current = this;
        while (current != null)
        {
            if (current is CombatHealth health)
            {
                return health;
            }

            CombatHealth childHealth = current.GetNodeOrNull<CombatHealth>("Health");
            if (childHealth != null)
            {
                return childHealth;
            }

            current = current.GetParent();
        }

        return null;
    }
}
