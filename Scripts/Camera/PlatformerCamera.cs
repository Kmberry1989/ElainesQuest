using Godot;

public partial class PlatformerCamera : Camera3D
{
    [Export] public Node3D Target;
    [Export] public Vector3 Offset = new(0.0f, 6.0f, 8.0f);
    [Export] public float FollowSpeed = 5.0f;
    [Export] public float LookAheadDistance = 1.35f;
    [Export] public float ShakeDamping = 10.0f;

    private float _shakeRemaining;
    private float _shakeStrength;

    public override void _Ready()
    {
        Current = true;
        Target ??= GetTree().GetFirstNodeInGroup("player") as Node3D;
        ConnectPlayerSignals();
    }

    public override void _Process(double delta)
    {
        if (Target == null)
        {
            Target = GetTree().GetFirstNodeInGroup("player") as Node3D;
            ConnectPlayerSignals();
            return;
        }

        float weight = 1.0f - Mathf.Exp(-FollowSpeed * (float)delta);
        Vector3 lookAhead = Vector3.Zero;
        if (Target is ElaineController player)
        {
            Vector3 velocity = player.Velocity;
            velocity.Y = 0.0f;
            if (velocity.LengthSquared() > 0.05f)
            {
                lookAhead = velocity.Normalized() * LookAheadDistance;
            }
        }

        Vector3 shake = Vector3.Zero;
        if (_shakeRemaining > 0.0f)
        {
            _shakeRemaining = Mathf.Max(0.0f, _shakeRemaining - (float)delta);
            shake = new Vector3(
                Mathf.Sin(Time.GetTicksMsec() * 0.041f),
                Mathf.Cos(Time.GetTicksMsec() * 0.053f),
                0.0f) * _shakeStrength * (_shakeRemaining / 0.2f);
            _shakeStrength = Mathf.MoveToward(_shakeStrength, 0.0f, ShakeDamping * (float)delta);
        }

        Vector3 desiredPosition = Target.GlobalPosition + Offset + lookAhead + shake;
        GlobalPosition = GlobalPosition.Lerp(desiredPosition, weight);
        LookAt(Target.GlobalPosition + Vector3.Up * 1.5f + lookAhead * 0.35f, Vector3.Up);
    }

    public void ApplyShake(float strength, float duration = 0.2f)
    {
        _shakeStrength = Mathf.Max(_shakeStrength, strength);
        _shakeRemaining = Mathf.Max(_shakeRemaining, duration);
    }

    private void ConnectPlayerSignals()
    {
        if (Target is not ElaineController player) return;
        player.DamageTaken -= OnPlayerDamageTaken;
        player.SpellHit -= OnPlayerSpellHit;
        player.DamageTaken += OnPlayerDamageTaken;
        player.SpellHit += OnPlayerSpellHit;
    }

    private void OnPlayerDamageTaken(float _amount, float _currentHealth, float _maxHealth) => ApplyShake(0.24f, 0.24f);
    private void OnPlayerSpellHit(int _spellIndex, int _hitNumber, int _maxTargetHits, string _displayName) => ApplyShake(0.08f, 0.12f);
}
