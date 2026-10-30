using Godot;

public partial class LevelPortal : Area3D
{
    [Export] public string TargetScene = "res://Scenes/Levels/World_01_Forest.tscn";

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

        if (!string.IsNullOrWhiteSpace(TargetScene) && ResourceLoader.Exists(TargetScene))
        {
            GetTree().ChangeSceneToFile(TargetScene);
        }
    }
}
