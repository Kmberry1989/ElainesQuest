using Godot;

[Tool]
public partial class RuntimeGltfModel : Node3D
{
    [Export(PropertyHint.File, "*.glb,*.gltf")] public string ModelPath = string.Empty;
    [Export] public Node3D PlaceholderRoot;
    [Export] public Vector3 LoadedOffset = Vector3.Zero;
    [Export] public Vector3 LoadedRotationDegrees = Vector3.Zero;
    [Export] public Vector3 LoadedScale = Vector3.One;
    [Export] public bool LoadInEditor = true;
    [Export] public AnimationTree LinkedAnimationTree;
    [Export] public string LibraryToLoad = "res://Assets/Animations/MixamoLibrary.res";

    private const string GeneratedModelName = "__RuntimeModel";

    public override void _EnterTree()
    {
        CallDeferred(nameof(RefreshModel));
    }

    public override void _Ready()
    {
        CallDeferred(nameof(RefreshModel));
    }

    public void RefreshModel()
    {
        if (Engine.IsEditorHint() && !LoadInEditor)
        {
            return;
        }

        ClearGeneratedModel();

        if (PlaceholderRoot != null)
        {
            PlaceholderRoot.Visible = true;
        }

        if (string.IsNullOrWhiteSpace(ModelPath) || !FileAccess.FileExists(ModelPath))
        {
            return;
        }

        var scene = ResourceLoader.Load<PackedScene>(ModelPath);
        if (scene == null)
        {
            GD.PushWarning($"Failed to load scene from '{ModelPath}'.");
            return;
        }

        Node generatedNode = scene.Instantiate();

        generatedNode.Name = GeneratedModelName;
        AddChild(generatedNode);
        generatedNode.Owner = null;

        var animPlayer = generatedNode.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        
        if (LinkedAnimationTree == null)
        {
            LinkedAnimationTree = GetNodeOrNull<AnimationTree>("../AnimationTree");
        }

        if (animPlayer != null && LinkedAnimationTree != null)
        {
            if (!string.IsNullOrEmpty(LibraryToLoad) && FileAccess.FileExists(LibraryToLoad))
            {
                var mixamoLib = ResourceLoader.Load<AnimationLibrary>(LibraryToLoad);
                if (mixamoLib != null)
                {
                    animPlayer.AddAnimationLibrary("Mixamo", mixamoLib);
                }
            }

            LinkedAnimationTree.AnimPlayer = LinkedAnimationTree.GetPathTo(animPlayer);
            LinkedAnimationTree.RootNode = LinkedAnimationTree.GetPathTo(generatedNode);
            
            // Toggle active to force initialization!
            LinkedAnimationTree.Active = false;
            LinkedAnimationTree.Active = true;
        }

        if (generatedNode is Node3D generatedNode3D)
        {
            generatedNode3D.Position = LoadedOffset;
            generatedNode3D.RotationDegrees = LoadedRotationDegrees;
            generatedNode3D.Scale = LoadedScale;
        }

        if (PlaceholderRoot != null)
        {
            PlaceholderRoot.Visible = false;
        }
    }

    private void ClearGeneratedModel()
    {
        Node existing = GetNodeOrNull<Node>(GeneratedModelName);
        existing?.QueueFree();
    }
}
