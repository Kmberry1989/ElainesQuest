using Godot;

public partial class CheckGlbTracks : SceneTree
{
    public override void _Initialize()
    {
        var scene = ResourceLoader.Load<PackedScene>("res://Assets/Animations/Mixamo/run.glb");
        var node = scene.Instantiate();
        var player = node.GetNode<AnimationPlayer>("AnimationPlayer");
        var anim = player.GetAnimation("mixamo.com");
        
        GD.Print($"Animation track count: {anim.GetTrackCount()}");
        for(int i = 0; i < Mathf.Min(3, anim.GetTrackCount()); i++) {
            GD.Print($"Track {i} path: {anim.TrackGetPath(i)}");
        }
        Quit();
    }
}
