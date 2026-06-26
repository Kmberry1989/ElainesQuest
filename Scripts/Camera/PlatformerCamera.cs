using Godot;

public partial class PlatformerCamera : Camera3D
{
    [Export] public Node3D Target;
    [Export] public Vector3 Offset = new(0.0f, 6.0f, 8.0f);
    [Export] public float FollowSpeed = 5.0f;

    public override void _Ready()
    {
        Current = true;
        Target ??= GetTree().GetFirstNodeInGroup("player") as Node3D;
    }

    public override void _Process(double delta)
    {
        if (Target == null)
        {
            Target = GetTree().GetFirstNodeInGroup("player") as Node3D;
            return;
        }

        float weight = 1.0f - Mathf.Exp(-FollowSpeed * (float)delta);
        Vector3 desiredPosition = Target.GlobalPosition + Offset;
        GlobalPosition = GlobalPosition.Lerp(desiredPosition, weight);
        LookAt(Target.GlobalPosition + Vector3.Up * 1.5f, Vector3.Up);
    }
}
