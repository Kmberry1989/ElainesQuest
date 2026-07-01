using Godot;

public partial class ElaineController : CharacterBody3D
{
    [ExportCategory("Movement Settings")]
    [Export] public float Speed = 7.0f;
    [Export] public float JumpVelocity = 4.5f;
    [Export] public float Gravity = 9.8f;
    [Export] public float RotationSpeed = 10.0f;
    [Export] public float Acceleration = 18.0f;
    [Export] public float GroundDeceleration = 18.0f;
    [Export] public float WalkAnimationThreshold = 0.05f;
    [Export] public float RunAnimationThreshold = 0.2f;
    [Export] public float RunInputThreshold = 0.65f;

    [ExportCategory("Magic Ring Settings")]
    [Export] public float HoverDuration = 2.0f;
    [Export] public float HoverGravityModifier = 0.2f; 
    private bool _isHovering = false;
    private float _hoverTimer = 0.0f;

    [ExportCategory("Action Settings")]
    [Export] public float OneShotFallbackDuration = 0.75f;

    [ExportCategory("Nodes")]
    [Export] public Node3D Visuals; // Drag the 3D mesh node here
    [Export] public AnimationPlayer AnimPlayer; // Drag the AnimationPlayer here
    [Export] public AnimationTree AnimTree;
    [Export] public float InteractionRadius = 4.0f;

    private string _currentAnim = "";
    private Vector3 _respawnPoint;
    private AnimationNodeStateMachinePlayback _stateMachinePlayback;
    private string _oneShotState = "";
    private float _oneShotTimer = 0.0f;

    public override void _Ready()
    {
        AddToGroup("player");
        Visuals ??= GetNodeOrNull<Node3D>("Visuals");
        AnimPlayer ??= GetNodeOrNull<AnimationPlayer>("GlobalAnimationPlayer");
        AnimTree ??= GetNodeOrNull<AnimationTree>("AnimationTree");
        _respawnPoint = GlobalPosition;

        CallDeferred(nameof(InitializeAnimationStateMachine));
    }

    private void InitializeAnimationStateMachine()
    {
        if (AnimTree == null)
        {
            TravelAnimationState("Idle");
            return;
        }

        AnimTree.Active = true;
        _stateMachinePlayback = (AnimationNodeStateMachinePlayback)AnimTree.Get("parameters/playback");
        string initialState = string.IsNullOrEmpty(_currentAnim) ? "Idle" : _currentAnim;
        _currentAnim = "";
        TravelAnimationState(initialState);
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

        _currentAnim = stateName;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (IsDialogueOpen())
        {
            return;
        }

        if (@event.IsActionPressed("interact"))
        {
            if (TryInteract())
            {
                StartOneShot("Talk", "Mixamo/talk");
            }
            return;
        }

        if (@event.IsActionPressed("cast_magic"))
        {
            StartOneShot("Cast", "Mixamo/cast");
            return;
        }

        if (@event.IsActionPressed("emote"))
        {
            StartOneShot("Wave", "Mixamo/wave");
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

        if (_oneShotTimer > 0.0f && !IsDialogueOpen())
        {
            _oneShotTimer = Mathf.Max(0.0f, _oneShotTimer - d);
        }
        else if (_oneShotTimer <= 0.0f)
        {
            _oneShotState = string.Empty;
        }

        // 1. Handle Gravity & Magic Ring Hover
        if (!IsOnFloor())
        {
            _oneShotState = string.Empty;
            _oneShotTimer = 0.0f;
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
        bool oneShotActive = _oneShotTimer > 0.0f;
        float targetHorizontalSpeed = Speed * inputStrength;

        if (oneShotActive)
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0.0f, GroundDeceleration * d);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0.0f, GroundDeceleration * d);
            TravelAnimationState(_oneShotState);
        }
        else if (direction != Vector3.Zero)
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

        if (IsOnFloor() && !_isHovering && !oneShotActive)
        {
            float horizontalSpeed = new Vector2(velocity.X, velocity.Z).Length();

            if (horizontalSpeed < WalkAnimationThreshold)
            {
                TravelAnimationState("Idle");
            }
            else if (inputStrength < RunInputThreshold)
            {
                TravelAnimationState("Walk");
            }
            else if (horizontalSpeed >= RunAnimationThreshold)
            {
                TravelAnimationState("Run");
            }
            else
            {
                TravelAnimationState("Walk");
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
        _oneShotState = string.Empty;
        _oneShotTimer = 0.0f;
    }

    private bool TryInteract()
    {
        if (IsDialogueOpen())
        {
            return false;
        }

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

        if (closestNpc == null)
        {
            return false;
        }

        closestNpc.Interact();
        return true;
    }

    private bool StartOneShot(string stateName, string animationName)
    {
        if (!IsOnFloor() || IsDialogueOpen() || _oneShotTimer > 0.0f)
        {
            return false;
        }

        _oneShotState = stateName;
        _oneShotTimer = GetAnimationDuration(animationName);
        TravelAnimationState(stateName);
        return true;
    }

    private float GetAnimationDuration(string animationName)
    {
        AnimationPlayer activePlayer = ResolveActiveAnimationPlayer();
        Animation animation = activePlayer?.GetAnimation(animationName);
        if (animation == null)
        {
            return OneShotFallbackDuration;
        }

        return Mathf.Max(0.1f, (float)animation.Length - 0.05f);
    }

    private AnimationPlayer ResolveActiveAnimationPlayer()
    {
        if (AnimTree != null)
        {
            AnimationPlayer treePlayer = AnimTree.GetNodeOrNull<AnimationPlayer>(AnimTree.AnimPlayer);
            if (treePlayer != null)
            {
                return treePlayer;
            }
        }

        return AnimPlayer ?? GetNodeOrNull<AnimationPlayer>("GlobalAnimationPlayer");
    }

    private static bool IsDialogueOpen()
    {
        return DialogueUI.Instance?.Panel?.Visible ?? false;
    }
}
