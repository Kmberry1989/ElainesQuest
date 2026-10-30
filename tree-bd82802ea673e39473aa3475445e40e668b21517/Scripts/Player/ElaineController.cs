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
    [Export] public AnimationPlayer AnimPlayer; // Drag the AnimationPlayer here
    [Export] public float InteractionRadius = 4.0f;

    private string _currentAnim = "";
    private Vector3 _respawnPoint;

    public override void _Ready()
    {
        AddToGroup("player");
        Visuals ??= GetNodeOrNull<Node3D>("Visuals");
        AnimPlayer ??= GetNodeOrNull<AnimationPlayer>("GlobalAnimationPlayer");
        _respawnPoint = GlobalPosition;
        
        PlayAnimation("Mixamo/idle");
    }

    private void PlayAnimation(string animName, float blendTime = 0.2f)
    {
        if (AnimPlayer == null)
        {
            GD.Print("AnimPlayer is NULL!");
            return;
        }

        if (_currentAnim != animName)
        {
            GD.Print($"Playing animation: {animName}");
            AnimPlayer.Play(animName, blendTime);
            _currentAnim = animName;
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
                PlayAnimation("Mixamo/fallingtoroll", 0.5f); 
            }
            else
            {
                _isHovering = false;
                velocity.Y -= Gravity * d;
                PlayAnimation("Mixamo/jump", 0.1f); 
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
            PlayAnimation("Mixamo/jump", 0.1f);
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
                PlayAnimation("Mixamo/run");
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
            
            if (IsOnFloor()) 
                PlayAnimation("Mixamo/idle");
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
