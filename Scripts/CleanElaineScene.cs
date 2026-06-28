using Godot;

public partial class CleanElaineScene : SceneTree
{
    public override void _Initialize()
    {
        var path = "res://Scenes/Characters/Player/Elaine.tscn";
        var scene = ResourceLoader.Load<PackedScene>(path);
        var root = scene.Instantiate<CharacterBody3D>();
        
        // Find and delete the corrupted visuals node(s)
        foreach (var child in root.GetChildren())
        {
            if (child.Name.ToString().StartsWith("@Node3D") || child.Name == "Visuals" || child.Name == "GlobalAnimationPlayer")
            {
                child.QueueFree();
                root.RemoveChild(child);
            }
        }
        
        // Recreate cleanly
        var visuals = new Node3D();
        visuals.Name = "Visuals";
        root.AddChild(visuals);
        visuals.Owner = root;
        
        var modelScene = ResourceLoader.Load<PackedScene>("res://Assets/Models/Elaine/Elaine-model.glb");
        var modelNode = modelScene.Instantiate();
        modelNode.Name = "Elaine-model";
        visuals.AddChild(modelNode);
        
        // ONLY set owner on the root of the instantiated GLB! Do not set recursively!
        modelNode.Owner = root;
        
        var animPlayer = new AnimationPlayer();
        animPlayer.Name = "GlobalAnimationPlayer";
        root.AddChild(animPlayer);
        animPlayer.Owner = root;
        
        animPlayer.RootNode = animPlayer.GetPathTo(modelNode);
        
        var lib = ResourceLoader.Load<AnimationLibrary>("res://Assets/Animations/MixamoLibrary.res");
        animPlayer.AddAnimationLibrary("Mixamo", lib);
        
        // Save
        var newScene = new PackedScene();
        newScene.Pack(root);
        ResourceSaver.Save(newScene, path);
        
        GD.Print("Cleaned up Elaine.tscn!");
        Quit();
    }
}
