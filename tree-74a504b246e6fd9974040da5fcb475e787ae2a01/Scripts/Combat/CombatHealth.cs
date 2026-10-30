using Godot;

public partial class CombatHealth : Node
{
    [Signal]
    public delegate void HealthChangedEventHandler(float currentHealth, float maxHealth);

    [Signal]
    public delegate void DiedEventHandler();

    [Export] public float MaxHealth = 3.0f;
    [Export] public bool ResetToFullOnReady = true;
    [Export] public float CurrentHealth = -1.0f;

    public bool IsAlive => CurrentHealth > 0.0f;

    public override void _Ready()
    {
        if (ResetToFullOnReady || CurrentHealth <= 0.0f || CurrentHealth > MaxHealth)
        {
            CurrentHealth = MaxHealth;
        }

        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }

    public bool ApplyDamage(float amount)
    {
        if (amount <= 0.0f || !IsAlive)
        {
            return false;
        }

        CurrentHealth = Mathf.Max(0.0f, CurrentHealth - amount);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

        if (CurrentHealth <= 0.0f)
        {
            EmitSignal(SignalName.Died);
        }

        return true;
    }

    public void RestoreFull()
    {
        CurrentHealth = MaxHealth;
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }

    public float GetNormalizedHealth()
    {
        if (MaxHealth <= 0.0f)
        {
            return 0.0f;
        }

        return Mathf.Clamp(CurrentHealth / MaxHealth, 0.0f, 1.0f);
    }
}
