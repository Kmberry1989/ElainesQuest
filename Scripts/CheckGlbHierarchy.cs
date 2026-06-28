using Godot;

public partial class CheckGlbHierarchy : SceneTree
{
    public override void _Initialize()
    {
        var scene = ResourceLoader.Load<PackedScene>("res://Assets/Animations/Mixamo/run.glb");
        var node = scene.Instantiate();
        DumpNode(node, 0);
        Quit();
    }
    
    private void DumpNode(Node node, int depth)
    {
        string indent = new string(' ', depth * 2);
        GD.Print($"{indent}- {node.Name} ({node.GetType().Name})");
        foreach(var child in node.GetChildren())
        {
            DumpNode(child, depth + 1);
        }
    }
}
