using Godot;

public partial class SetupGlobalAnimation : SceneTree
{
    public override void _Initialize()
    {
        var path = "res://Scenes/Characters/Player/Elaine.tscn";
        var scene = ResourceLoader.Load<PackedScene>(path);
        var root = scene.Instantiate<CharacterBody3D>();
        
        var oldVisuals = root.GetNode<Node3D>("Visuals");
        var newVisuals = new Node3D();
        newVisuals.Name = "Visuals";
        newVisuals.Transform = oldVisuals.Transform;
        
        root.AddChild(newVisuals);
        newVisuals.Owner = root;
        
        oldVisuals.QueueFree();
        root.RemoveChild(oldVisuals);
        
        // Remove old AnimationTree
        var animTree = root.GetNodeOrNull<AnimationTree>("AnimationTree");
        if (animTree != null)
        {
            animTree.QueueFree();
            root.RemoveChild(animTree);
        }
        
        // Instantiate the model directly
        var modelScene = ResourceLoader.Load<PackedScene>("res://Assets/Models/Elaine/Elaine-model.glb");
        var modelNode = modelScene.Instantiate();
        modelNode.Name = "Elaine-model";
        newVisuals.AddChild(modelNode);
        
        // VERY IMPORTANT: Set owners recursively so it saves in the scene!
        SetOwnerRecursive(modelNode, root);
        
        // Create AnimationPlayer
        var animPlayer = new AnimationPlayer();
        animPlayer.Name = "GlobalAnimationPlayer";
        root.AddChild(animPlayer);
        animPlayer.Owner = root;
        
        // Set root node to the model
        animPlayer.RootNode = animPlayer.GetPathTo(modelNode);
        
        // Add the Mixamo library
        var lib = ResourceLoader.Load<AnimationLibrary>("res://Assets/Animations/MixamoLibrary.res");
        animPlayer.AddAnimationLibrary("Mixamo", lib);
        
        // Save the scene
        var newScene = new PackedScene();
        newScene.Pack(root);
        ResourceSaver.Save(newScene, path);
        
        GD.Print("Setup global AnimationPlayer in Elaine.tscn!");
        Quit();
    }
    
    private void SetOwnerRecursive(Node node, Node owner)
    {
        node.Owner = owner;
        foreach (var child in node.GetChildren())
        {
            SetOwnerRecursive(child, owner);
        }
    }
}
