using Godot;

public partial class Checkpoint : Area3D
{
    [Export] public Marker3D RespawnMarker;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node3D body)
    {
        ElaineController player = body as ElaineController;
        if (player == null && !body.IsInGroup("player"))
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        player.SetRespawnPoint(RespawnMarker?.GlobalPosition ?? GlobalPosition);
    }
}
