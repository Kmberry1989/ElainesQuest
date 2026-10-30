using Godot;

public partial class PlatformerCamera : Camera3D
{
    [Export] public Node3D Target;
    [Export] public Vector3 Offset = new(0.0f, 6.0f, 8.0f);
    [Export] public float FollowSpeed = 5.0f;
    [Export] public float LookAheadDistance = 1.35f;
    [Export] public float ShakeDamping = 10.0f;

    private ElaineController _signalPlayer;
    private float _shakeRemaining;
    private float _shakeStrength;
    private Vector3 _lookAhead;

    public override void _Ready()
    {
        Current = true;
        PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;
        Target ??= GetTree().GetFirstNodeInGroup("player") as Node3D;
        ConnectPlayerSignals();
        if (Target != null)
        {
            GlobalPosition = Target.GlobalPosition + Offset;
            LookAt(Target.GlobalPosition + Vector3.Up, Vector3.Up);
        }
    }

    public override void _Process(double delta)
    {
        if (!IsInstanceValid(Target))
        {
            Target = GetTree().GetFirstNodeInGroup("player") as Node3D;
            ConnectPlayerSignals();
            return;
        }
        Vector3 targetPosition = Target.GetGlobalTransformInterpolated().Origin;

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

        _lookAhead = _lookAhead.Lerp(lookAhead, 1.0f - Mathf.Exp(-3.5f * (float)delta));

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

        Vector3 desiredPosition = targetPosition + Offset + _lookAhead + shake;
        GlobalPosition = GlobalPosition.DistanceTo(desiredPosition) > 25.0f
            ? desiredPosition : GlobalPosition.Lerp(desiredPosition, weight);
        LookAt(targetPosition + Vector3.Up + _lookAhead * 0.35f, Vector3.Up);
    }

    public void ApplyShake(float strength, float duration = 0.2f)
    {
        _shakeStrength = Mathf.Max(_shakeStrength, strength);
        _shakeRemaining = Mathf.Max(_shakeRemaining, duration);
    }

    public override void _ExitTree() => DisconnectPlayerSignals();

    private void ConnectPlayerSignals()
    {
        if (Target is not ElaineController player || _signalPlayer == player) return;
        DisconnectPlayerSignals();
        _signalPlayer = player;
        player.DamageTaken += OnPlayerDamageTaken;
        player.SpellHit += OnPlayerSpellHit;
        player.MeleeHit += OnPlayerMeleeHit;
    }

    private void DisconnectPlayerSignals()
    {
        if (IsInstanceValid(_signalPlayer))
        {
            _signalPlayer.DamageTaken -= OnPlayerDamageTaken;
            _signalPlayer.SpellHit -= OnPlayerSpellHit;
            _signalPlayer.MeleeHit -= OnPlayerMeleeHit;
        }
        _signalPlayer = null;
    }

    private void OnPlayerMeleeHit(int targetsHit)
    {
        if (targetsHit > 0) ApplyShake(0.12f, 0.16f);
    }

    private void OnPlayerDamageTaken(float _amount, float _currentHealth, float _maxHealth) => ApplyShake(0.24f, 0.24f);
    private void OnPlayerSpellHit(int _spellIndex, int _hitNumber, int _maxTargetHits, string _displayName) => ApplyShake(0.08f, 0.12f);
}
