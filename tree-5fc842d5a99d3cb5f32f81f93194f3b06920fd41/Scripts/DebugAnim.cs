using Godot;

[Tool]
public partial class DebugAnim : SceneTree
{
    public override void _Initialize()
    {
        var lib = ResourceLoader.Load<AnimationLibrary>("res://Assets/Animations/MixamoLibrary.res");
        if (lib != null)
        {
            var anim = lib.GetAnimation("idle");
            if (anim != null)
            {
                GD.Print("Idle tracks:");
                for (int i = 0; i < anim.GetTrackCount(); i++)
                {
                    GD.Print("- " + anim.TrackGetPath(i));
                }
            }
        }
        
        var scene = ResourceLoader.Load<PackedScene>("res://Assets/Models/Elaine/Elaine-model.glb");
        if (scene != null)
        {
            var node = scene.Instantiate();
            GD.Print("Elaine model hierarchy:");
            PrintHierarchy(node, "");
        }
        
        Quit();
    }
    
    private void PrintHierarchy(Node node, string indent)
    {
        GD.Print(indent + node.Name + " (" + node.GetType().Name + ")");
        foreach (Node child in node.GetChildren())
        {
            PrintHierarchy(child, indent + "  ");
        }
    }
}
