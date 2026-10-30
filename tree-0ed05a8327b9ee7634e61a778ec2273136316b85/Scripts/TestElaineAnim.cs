using Godot;

public partial class TestElaineAnim : SceneTree
{
    public override void _Initialize()
    {
        var scene = ResourceLoader.Load<PackedScene>("res://Scenes/Characters/Player/Elaine.tscn");
        var node = scene.Instantiate();
        var player = node.GetNode<AnimationPlayer>("GlobalAnimationPlayer");
        var skeleton =
            node.GetNodeOrNull<Skeleton3D>("Visuals/Elaine-model/Skeleton3D") ??
            node.GetNode<Skeleton3D>("Visuals/Elaine-model/RootNode/Skeleton3D");
        
        GD.Print($"Bone 0 (Hips) pose before: {skeleton.GetBonePosePosition(0)}");
        
        player.Play("Mixamo/run");
        player.Advance(0.1f);
        
        GD.Print($"Bone 0 (Hips) pose after: {skeleton.GetBonePosePosition(0)}");
        node.Free();
        Quit();
    }
}
