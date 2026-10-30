using Godot;

public partial class ElaineController : CharacterBody3D
{
    [ExportCategory("Movement Settings")]
    [Export] public float Speed = 5.0f;
    [Export] public float JumpVelocity = 4.5f;
    [Export] public float Gravity = 9.8f;
    [Export] public float RotationSpeed = 10.0f;
    [Export] public float Acceleration = 14.0f;
    [Export] public float GroundDeceleration = 18.0f;
    [Export] public float WalkAnimationThreshold = 0.2f;
    [Export] public float RunAnimationThreshold = 3.25f;

    [ExportCategory("Magic Ring Settings")]
    [Export] public float HoverDuration = 2.0f;
    [Export] public float HoverGravityModifier = 0.2f; 
    private bool _isHovering = false;
    private float _hoverTimer = 0.0f;

    [ExportCategory("Nodes")]
    [Export] public Node3D Visuals; // Drag the 3D mesh node here
    [Export] public AnimationPlayer AnimPlayer; // Drag the AnimationPlayer here
    [Export] public AnimationTree AnimTree;
    [Export] public float InteractionRadius = 4.0f;

    private string _currentAnim = "";
    private Vector3 _respawnPoint;
    private AnimationNodeStateMachinePlayback _stateMachinePlayback;

    public override void _Ready()
    {
        AddToGroup("player");
        Visuals ??= GetNodeOrNull<Node3D>("Visuals");
        AnimPlayer ??= GetNodeOrNull<AnimationPlayer>("GlobalAnimationPlayer");
        AnimTree ??= GetNodeOrNull<AnimationTree>("AnimationTree");
        _respawnPoint = GlobalPosition;

        if (AnimTree != null)
        {
            AnimTree.Active = true;
            _stateMachinePlayback = (AnimationNodeStateMachinePlayback)AnimTree.Get("parameters/playback");
        }

        TravelAnimationState("Idle");
    }

    private void TravelAnimationState(string stateName)
    {
        if (_currentAnim == stateName)
        {
            return;
        }

        if (_stateMachinePlayback != null)
        {
            _stateMachinePlayback.Travel(stateName);
            _currentAnim = stateName;
            return;
        }

        if (AnimPlayer == null)
        {
            GD.Print("AnimPlayer is NULL!");
            return;
        }

        string clipName = stateName switch
        {
            "Walk" => "Mixamo/walk",
            "Run" => "Mixamo/run",
            "Jump" => "Mixamo/jump",
            "Hover" => "Mixamo/flying",
            _ => "Mixamo/idle",
        };

        GD.Print($"Playing animation: {clipName}");
        AnimPlayer.Play(clipName, 0.2f);
        _currentAnim = stateName;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("interact"))
        {
            TryInteract();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;
        float d = (float)delta;
        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
        float inputStrength = Mathf.Clamp(inputDir.Length(), 0.0f, 1.0f);
        Vector3 direction = new Vector3(inputDir.X, 0, inputDir.Y);

        if (direction.LengthSquared() > 0.0f)
        {
            direction = direction.Normalized();
        }

        // 1. Handle Gravity & Magic Ring Hover
        if (!IsOnFloor())
        {
            if (Input.IsActionPressed("jump") && Velocity.Y < 0 && _hoverTimer > 0)
            {
                // Magic Ring Hover logic
                _isHovering = true;
                velocity.Y -= (Gravity * HoverGravityModifier) * d;
                _hoverTimer -= d;
                TravelAnimationState("Hover");
            }
            else
            {
                _isHovering = false;
                velocity.Y -= Gravity * d;
                TravelAnimationState("Jump");
            }
        }
        else
        {
            // Reset Hover when on the ground
            _hoverTimer = HoverDuration;
            _isHovering = false;
        }

        // 2. Handle Jump
        if (Input.IsActionJustPressed("jump") && IsOnFloor())
        {
            velocity.Y = JumpVelocity;
            TravelAnimationState("Jump");
        }

        // 3. Handle Movement & Animation State
        float targetHorizontalSpeed = Speed * inputStrength;

        if (direction != Vector3.Zero)
        {
            velocity.X = Mathf.MoveToward(Velocity.X, direction.X * targetHorizontalSpeed, Acceleration * d);
            velocity.Z = Mathf.MoveToward(Velocity.Z, direction.Z * targetHorizontalSpeed, Acceleration * d);

            // Rotate Elaine's mesh to face the movement direction smoothly
            if (Visuals != null)
            {
                float targetAngle = Mathf.Atan2(direction.X, direction.Z);
                float currentRotation = Visuals.Rotation.Y;
                Visuals.Rotation = new Vector3(0, Mathf.LerpAngle(currentRotation, targetAngle, RotationSpeed * d), 0);
            }
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, GroundDeceleration * d);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, GroundDeceleration * d);
        }

        if (IsOnFloor() && !_isHovering)
        {
            float horizontalSpeed = new Vector2(velocity.X, velocity.Z).Length();

            if (horizontalSpeed >= RunAnimationThreshold)
            {
                TravelAnimationState("Run");
            }
            else if (horizontalSpeed >= WalkAnimationThreshold)
            {
                TravelAnimationState("Walk");
            }
            else
            {
                TravelAnimationState("Idle");
            }
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    public void SetRespawnPoint(Vector3 point)
    {
        _respawnPoint = point;
    }

    public void Respawn()
    {
        RespawnTo(_respawnPoint);
    }

    public void RespawnTo(Vector3 point)
    {
        GlobalPosition = point;
        Velocity = Vector3.Zero;
    }

    private void TryInteract()
    {
        NPC closestNpc = null;
        float closestDistanceSquared = InteractionRadius * InteractionRadius;

        foreach (Node node in GetTree().GetNodesInGroup("npc_interaction"))
        {
            if (node is not NPC npc)
            {
                continue;
            }

            float distanceSquared = GlobalPosition.DistanceSquaredTo(npc.GlobalPosition);
            if (distanceSquared > closestDistanceSquared)
            {
                continue;
            }

            closestNpc = npc;
            closestDistanceSquared = distanceSquared;
        }

        closestNpc?.Interact();
    }
}
