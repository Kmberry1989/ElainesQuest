using Godot;
using System;

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

        Node generatedNode = TryInstantiateImportedScene() ?? TryLoadRuntimeGltfScene();
        if (generatedNode == null)
        {
            GD.PushWarning($"Failed to load scene from '{ModelPath}'.");
            return;
        }

        generatedNode.Name = GeneratedModelName;
        AddChild(generatedNode);
        generatedNode.Owner = null;

        var animPlayer = generatedNode.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
        var skeleton = generatedNode.FindChild("Skeleton3D", true, false) as Node;
        Node animationRoot =
            skeleton?.GetParent() ??
            generatedNode.FindChild("RootNode", true, false) ??
            generatedNode;

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
            LinkedAnimationTree.RootNode = LinkedAnimationTree.GetPathTo(animationRoot);
            
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

    private Node TryInstantiateImportedScene()
    {
        PackedScene scene = ResourceLoader.Load<PackedScene>(ModelPath);
        return scene?.Instantiate();
    }

    private Node TryLoadRuntimeGltfScene()
    {
        GltfDocument document = new();
        GltfState state = new
        {
            BasePath = ResolveBasePath(ModelPath)
        };

        Error result = document.AppendFromFile(ModelPath, state, 0, state.BasePath);
        if (result != Error.Ok)
        {
            return null;
        }

        return document.GenerateScene(state, 30.0f, false, true);
    }

    private void ClearGeneratedModel()
    {
        Node existing = GetNodeOrNull<Node>(GeneratedModelName);
        existing?.QueueFree();
    }

    private static string ResolveBasePath(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return string.Empty;
        }

        int slashIndex = resourcePath.LastIndexOf('/');
        if (slashIndex <= 0)
        {
            return string.Empty;
        }

        return resourcePath[..slashIndex];
    }
}
