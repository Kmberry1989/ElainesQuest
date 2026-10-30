using Godot;

public partial class CheckGlbAnimNames : SceneTree
{
    public override void _Initialize()
    {
        var scene = ResourceLoader.Load<PackedScene>("res://Assets/Animations/Mixamo/run.glb");
        var node = scene.Instantiate();
        var animPlayer = node.GetNode<AnimationPlayer>("AnimationPlayer");
        foreach(var animName in animPlayer.GetAnimationList())
        {
            GD.Print($"Anim: {animName}");
        }
        Quit();
    }
}
