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

        var gltfDocument = new GltfDocument();
        var gltfState = new GltfState();
        Error error = gltfDocument.AppendFromFile(ModelPath, gltfState);
        if (error != Error.Ok)
        {
            GD.PushWarning($"Failed to load glTF model '{ModelPath}' with error {error}.");
            return;
        }

        Node generatedNode = gltfDocument.GenerateScene(gltfState);
        if (generatedNode == null)
        {
            GD.PushWarning($"glTF scene generation returned null for '{ModelPath}'.");
            return;
        }

        generatedNode.Name = GeneratedModelName;
        AddChild(generatedNode);
        generatedNode.Owner = null;

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
