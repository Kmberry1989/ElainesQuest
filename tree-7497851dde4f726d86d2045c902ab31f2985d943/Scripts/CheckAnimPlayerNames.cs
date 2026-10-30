using Godot;

public partial class CheckAnimPlayerNames : SceneTree
{
    public override void _Initialize()
    {
        var scene = ResourceLoader.Load<PackedScene>("res://Scenes/Characters/Player/Elaine.tscn");
        var node = scene.Instantiate();
        var player = node.GetNode<AnimationPlayer>("GlobalAnimationPlayer");
        foreach(var name in player.GetAnimationList())
        {
            GD.Print($"Found: {name}");
        }
        Quit();
    }
}
