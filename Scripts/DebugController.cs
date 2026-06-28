using Godot;

public partial class DebugController : SceneTree
{
    public override async void _Initialize()
    {
        var scene = ResourceLoader.Load<PackedScene>("res://Scenes/Characters/Player/Elaine.tscn");
        var node = scene.Instantiate();
        Root.AddChild(node);
        
        var visuals = node.GetNode<Node3D>("Visuals");
        visuals.Call("RefreshModel");
        
        await ToSignal(this, "process_frame");
        
        Quit();
    }
}
