using Godot;

public partial class AnimalBehavior : CharacterBody3D
{
    [Export] public float WalkSpeed = 1.5f;
    [Export] public float Gravity = 9.8f;
    [Export] public float MoveDuration = 2.0f;
    [Export] public float PauseDuration = 1.5f;

    private float _stateTimer;
    private bool _isPaused = true;
    private Vector3 _moveDirection = Vector3.Zero;

    public override void _Ready()
    {
        AddToGroup("wildlife");
        StartPause();
    }

    public override void _PhysicsProcess(double delta)
    {
        float d = (float)delta;
        _stateTimer -= d;

        if (_stateTimer <= 0.0f)
        {
            if (_isPaused)
            {
                StartMove();
            }
            else
            {
                StartPause();
            }
        }

        Vector3 velocity = Velocity;
        if (!IsOnFloor())
        {
            velocity.Y -= Gravity * d;
        }
        else if (_isPaused)
        {
            velocity.Y = 0.0f;
        }

        if (_isPaused)
        {
            velocity.X = Mathf.MoveToward(velocity.X, 0.0f, WalkSpeed * d);
            velocity.Z = Mathf.MoveToward(velocity.Z, 0.0f, WalkSpeed * d);
        }
        else
        {
            velocity.X = _moveDirection.X * WalkSpeed;
            velocity.Z = _moveDirection.Z * WalkSpeed;

            if (_moveDirection != Vector3.Zero)
            {
                float targetAngle = Mathf.Atan2(_moveDirection.X, _moveDirection.Z);
                Rotation = new Vector3(0, Mathf.LerpAngle(Rotation.Y, targetAngle, 6.0f * d), 0);
            }
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    private void StartPause()
    {
        _isPaused = true;
        _stateTimer = PauseDuration;
        _moveDirection = Vector3.Zero;
    }

    private void StartMove()
    {
        _isPaused = false;
        _stateTimer = MoveDuration;
        Vector2 randomDirection = Vector2.FromAngle((float)GD.RandRange(0.0, Mathf.Tau));
        _moveDirection = new Vector3(randomDirection.X, 0.0f, randomDirection.Y).Normalized();
    }
}
