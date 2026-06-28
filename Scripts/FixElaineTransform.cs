using Godot;
using System.Linq;

public partial class FixElaineTransform : SceneTree
{
    public override void _Initialize()
    {
        var path = "res://Scenes/Characters/Player/Elaine.tscn";
        var scene = ResourceLoader.Load<PackedScene>(path);
        var root = scene.Instantiate<CharacterBody3D>();
        
        var collision = root.GetNode<CollisionShape3D>("CollisionShape3D");
        collision.Position = new Vector3(0, 0.55f, 0);
        
        var visuals = root.GetNode<Node3D>("Visuals");
        visuals.Position = new Vector3(0, 0, 0);
        
        var newScene = new PackedScene();
        newScene.Pack(root);
        ResourceSaver.Save(newScene, path);
        
        GD.Print("Fixed Transforms in Elaine.tscn!");
        Quit();
    }
}
