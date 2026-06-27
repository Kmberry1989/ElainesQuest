using Godot;

public partial class SceneBootstrap : Node
{
    [Export] public string DefaultWorldScene = "res://Scenes/Hub/HubWorld.tscn";

    public override void _Ready()
    {
        Node worldRoot = GetNodeOrNull<Node>("WorldRoot");
        if (worldRoot == null)
        {
            worldRoot = new Node { Name = "WorldRoot" };
            AddChild(worldRoot);
        }

        EnsureInstancedChild(worldRoot, DefaultWorldScene);
    }

    private void EnsureInstancedChild(Node parent, string scenePath)
    {
        if (string.IsNullOrWhiteSpace(scenePath) || !ResourceLoader.Exists(scenePath))
        {
            GD.PushWarning($"Bootstrap scene missing: {scenePath}");
            return;
        }

        PackedScene packedScene = GD.Load<PackedScene>(scenePath);
        if (packedScene == null)
        {
            GD.PushWarning($"Unable to load scene: {scenePath}");
            return;
        }

        string sceneName = scenePath.GetFile().GetBaseName();
        if (parent.HasNode(sceneName))
        {
            return;
        }

        Node instance = packedScene.Instantiate();
        parent.AddChild(instance);
    }
}
