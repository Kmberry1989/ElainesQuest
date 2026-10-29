using Godot;
using System;
using System.IO;

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
    [Export] public float DisplayHeight = 0.0f;
    [Export] public string DefaultAnimation = string.Empty;

    private const string GeneratedModelName = "__RuntimeModel";
    private bool _refreshQueued;

    public override void _EnterTree()
    {
        QueueModelRefresh();
    }

    public override void _Ready()
    {
        QueueModelRefresh();
    }

    private void QueueModelRefresh()
    {
        if (_refreshQueued) return;
        _refreshQueued = true;
        CallDeferred(nameof(FlushModelRefresh));
    }

    private void FlushModelRefresh()
    {
        _refreshQueued = false;
        RefreshModel();
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

        // Exported models may exist only as imported/remapped resources in the PCK.
        if (string.IsNullOrWhiteSpace(ModelPath) ||
            (!ResourceLoader.Exists(ModelPath) && !Godot.FileAccess.FileExists(ModelPath)))
        {
            return;
        }

        bool importRemapInvalid = IsImportRemapInvalid();
        Node generatedNode = importRemapInvalid
            ? TryInstantiateCachedImportedScene()
            : TryInstantiateImportedScene() ??
              TryInstantiateCachedImportedScene() ??
              TryLoadRuntimeGltfScene();
        if (generatedNode == null)
        {
            if (PlaceholderRoot == null)
            {
                GD.PushWarning($"Failed to load scene from '{ModelPath}'.");
            }

            return;
        }

        generatedNode.Name = GeneratedModelName;
        AddChild(generatedNode);
        generatedNode.Owner = null;

        var animPlayer = generatedNode.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
        if (animPlayer == null && !string.IsNullOrEmpty(DefaultAnimation))
        {
            animPlayer = new AnimationPlayer { Name = "IdleAnimationPlayer" };
            generatedNode.AddChild(animPlayer);
        }

        if (LinkedAnimationTree == null)
        {
            LinkedAnimationTree = GetNodeOrNull<AnimationTree>("../AnimationTree");
        }

        if (animPlayer != null && LinkedAnimationTree != null)
        {
            if (!string.IsNullOrEmpty(LibraryToLoad) && Godot.FileAccess.FileExists(LibraryToLoad))
            {
                var mixamoLib = ResourceLoader.Load<AnimationLibrary>(LibraryToLoad);
                if (mixamoLib != null)
                {
                    animPlayer.AddAnimationLibrary("Mixamo", mixamoLib);
                }
            }

            LinkedAnimationTree.AnimPlayer = LinkedAnimationTree.GetPathTo(animPlayer);
            // The shared clips already include RootNode/Skeleton3D in their paths.
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
            if (!Engine.IsEditorHint())
            {
                if (!string.IsNullOrEmpty(DefaultAnimation) && animPlayer != null)
                {
                    animPlayer.RootNode = animPlayer.GetPathTo(generatedNode);
                    if (!animPlayer.HasAnimationLibrary("Mixamo"))
                        animPlayer.AddAnimationLibrary("Mixamo", GD.Load<AnimationLibrary>(LibraryToLoad));
                    if (animPlayer.HasAnimation(DefaultAnimation))
                    {
                        animPlayer.Play(DefaultAnimation);
                        animPlayer.Advance(0);
                    }
                }
                if (DisplayHeight > 0) CalibrateDisplay(generatedNode3D);
            }
        }

        if (PlaceholderRoot != null)
        {
            PlaceholderRoot.Visible = false;
        }
    }

    private async void CalibrateDisplay(Node3D model)
    {
        // Evaluate the idle pose before fitting: bind-pose boxes miss the actual feet.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!IsInstanceValid(model) || !model.IsInsideTree()) return;
        Aabb? bounds = DisplayBounds(model);
        if (bounds == null || bounds.Value.Size.Y < 0.001f) return;
        float scale = DisplayHeight / bounds.Value.Size.Y;
        model.Scale *= scale;
        model.Position = new Vector3(model.Position.X, model.Position.Y * scale - bounds.Value.Position.Y * scale, model.Position.Z);
    }

    private Aabb? DisplayBounds(Node node)
    {
        Aabb? bounds = null;
        if (node is MeshInstance3D mesh && mesh.Mesh != null)
        {
            // The dummy renderer cannot bake a skin. Its invisible contract run
            // uses bind bounds; graphical builds fit the evaluated idle pose.
            Mesh posed = mesh.Skin != null && DisplayServer.GetName() != "headless"
                ? mesh.BakeMeshFromCurrentSkeletonPose() ?? mesh.Mesh : mesh.Mesh;
            Aabb box = GlobalTransform.AffineInverse() * mesh.GlobalTransform * posed.GetAabb();
            bounds = box;
        }
        foreach (Node child in node.GetChildren())
        {
            Aabb? childBounds = DisplayBounds(child);
            if (childBounds != null) bounds = bounds == null ? childBounds : bounds.Value.Merge(childBounds.Value);
        }
        return bounds;
    }

    private Node TryInstantiateImportedScene()
    {
        PackedScene scene = ResourceLoader.Load<PackedScene>(ModelPath);
        return scene?.Instantiate();
    }

    private Node TryLoadRuntimeGltfScene()
    {
        GltfDocument document = new();
        GltfState state = new();
        state.BasePath = ResolveBasePath(ModelPath);

        Error result = document.AppendFromFile(ModelPath, state, 0, state.BasePath);
        if (result != Error.Ok)
        {
            return null;
        }

        return document.GenerateScene(state, 30.0f, false, true);
    }

    private Node TryInstantiateCachedImportedScene()
    {
        string importedDirectory = ProjectSettings.GlobalizePath("res://.godot/imported");
        if (!Directory.Exists(importedDirectory))
        {
            return null;
        }

        string fileName = Path.GetFileName(ModelPath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        string[] matches = Directory.GetFiles(importedDirectory, $"{fileName}-*.scn");
        Array.Sort(matches, StringComparer.Ordinal);

        foreach (string match in matches)
        {
            string resourcePath = ProjectSettings.LocalizePath(match);
            PackedScene scene = ResourceLoader.Load<PackedScene>(resourcePath);
            if (scene != null)
            {
                return scene.Instantiate();
            }
        }

        return null;
    }

    private void ClearGeneratedModel()
    {
        Node existing = GetNodeOrNull<Node>(GeneratedModelName);
        if (existing != null)
        {
            RemoveChild(existing);
            existing.QueueFree();
        }
    }

    private bool IsImportRemapInvalid()
    {
        string importMetadataPath = $"{ModelPath}.import";
        if (!Godot.FileAccess.FileExists(importMetadataPath))
        {
            return false;
        }

        using Godot.FileAccess importFile = Godot.FileAccess.Open(importMetadataPath, Godot.FileAccess.ModeFlags.Read);
        if (importFile == null)
        {
            return false;
        }

        string importText = importFile.GetAsText();
        return importText.Contains("valid=false", StringComparison.Ordinal);
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
