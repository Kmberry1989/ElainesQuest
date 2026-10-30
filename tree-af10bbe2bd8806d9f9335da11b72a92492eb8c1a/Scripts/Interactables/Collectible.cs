using Godot;

public partial class Collectible : Area3D
{
    [Export] public int GlimmerValue = 1;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not ElaineController && !body.IsInGroup("player"))
        {
            return;
        }

        GameManager.Instance?.AddGlimmers(GlimmerValue);
        QueueFree();
    }
}
