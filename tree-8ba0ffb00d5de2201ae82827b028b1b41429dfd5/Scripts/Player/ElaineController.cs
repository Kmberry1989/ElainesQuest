using Godot;

public partial class ElaineController : CharacterBody3D
{
    [ExportCategory("Movement Settings")]
    [Export] public float Speed = 5.0f;
    [Export] public float JumpVelocity = 4.5f;
    [Export] public float Gravity = 9.8f;
    [Export] public float RotationSpeed = 10.0f;

    [ExportCategory("Magic Ring Settings")]
    [Export] public float HoverDuration = 2.0f;
    [Export] public float HoverGravityModifier = 0.2f; 
    private bool _isHovering = false;
    private float _hoverTimer = 0.0f;

    [ExportCategory("Nodes")]
    [Export] public Node3D Visuals; // Drag the 3D mesh node here
    [Export] public AnimationTree AnimTree; // Drag the AnimationTree here
    [Export] public float InteractionRadius = 4.0f;

    private AnimationNodeStateMachinePlayback _animPlayback;
    private Vector3 _respawnPoint;

    public override void _Ready()
    {
        AddToGroup("player");
        Visuals ??= GetNodeOrNull<Node3D>("Visuals");
        AnimTree ??= GetNodeOrNull<AnimationTree>("AnimationTree");
        _respawnPoint = GlobalPosition;

        if (AnimTree != null)
        {
            // Get the state machine playback to trigger Mixamo animations
            Variant playback = AnimTree.Get("parameters/playback");
            if (playback.VariantType == Variant.Type.Object)
            {
                _animPlayback = playback.As<AnimationNodeStateMachinePlayback>();
            }
        }
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

        // 1. Handle Gravity & Magic Ring Hover
        if (!IsOnFloor())
        {
            if (Input.IsActionPressed("jump") && Velocity.Y < 0 && _hoverTimer > 0)
            {
                // Magic Ring Hover logic
                _isHovering = true;
                velocity.Y -= (Gravity * HoverGravityModifier) * d;
                _hoverTimer -= d;
                _animPlayback?.Travel("Hover"); 
            }
            else
            {
                _isHovering = false;
                velocity.Y -= Gravity * d;
                _animPlayback?.Travel("Jump"); 
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
            _animPlayback?.Travel("Jump");
        }

        // 3. Handle Movement & Mixamo Animation Blending
        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
        Vector3 direction = new Vector3(inputDir.X, 0, inputDir.Y).Normalized();

        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;

            // Rotate Elaine's mesh to face the movement direction smoothly
            if (Visuals != null)
            {
                float targetAngle = Mathf.Atan2(direction.X, direction.Z);
                float currentRotation = Visuals.Rotation.Y;
                Visuals.Rotation = new Vector3(0, Mathf.LerpAngle(currentRotation, targetAngle, RotationSpeed * d), 0);
            }

            if (IsOnFloor()) 
                _animPlayback?.Travel("Run");
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
            
            if (IsOnFloor()) 
                _animPlayback?.Travel("Idle");
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
