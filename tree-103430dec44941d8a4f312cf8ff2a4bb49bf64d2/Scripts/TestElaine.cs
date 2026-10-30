using Godot;

public partial class TestElaine : SceneTree
{
    public override void _Initialize()
    {
        var scene = ResourceLoader.Load<PackedScene>("res://Scenes/Characters/Player/Elaine.tscn");
        var node = scene.Instantiate();
        var player = node.GetNode<AnimationPlayer>("GlobalAnimationPlayer");
        player.Play("Mixamo/run");
        player.Advance(0.1f);
        Quit();
    }
}
