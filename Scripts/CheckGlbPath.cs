using Godot;

public partial class CheckGlbPath : SceneTree
{
    public override void _Initialize()
    {
        var scene = ResourceLoader.Load<PackedScene>("res://Assets/Animations/Mixamo/idle.glb");
        var node = scene.Instantiate();
        var animPlayer = node.GetNode<AnimationPlayer>("AnimationPlayer");
        var anim = animPlayer.GetAnimation("mixamo_com");
        
        GD.Print($"First track path: {anim.TrackGetPath(0)}");
        Quit();
    }
}
