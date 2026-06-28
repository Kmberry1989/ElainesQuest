using Godot;

public partial class CleanTwistLogs : SceneTree
{
    public override void _Initialize()
    {
        GD.Print("All clean!");
        Quit();
    }
}
