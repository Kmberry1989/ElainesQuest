using Godot;

public partial class CheckBones : SceneTree
{
    public override void _Initialize()
    {
        var scene = ResourceLoader.Load<PackedScene>("res://Assets/Models/Elaine/Elaine-model.glb");
        var node = scene.Instantiate();
        
        var skeleton = node.GetNodeOrNull<Skeleton3D>("RootNode/Skeleton3D");
        if (skeleton != null) {
            GD.Print($"Found skeleton with {skeleton.GetBoneCount()} bones!");
            for(int i = 0; i < Mathf.Min(5, skeleton.GetBoneCount()); i++) {
                GD.Print($"- Bone {i}: {skeleton.GetBoneName(i)}");
            }
        } else {
            GD.Print("Could not find Skeleton3D!");
        }
        Quit();
    }
}
