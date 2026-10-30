using Godot;

public partial class FantasyRealmDirector : Node3D
{
    [Export] public string RealmId = "MoonlitGarden";
    [Export] public Godot.Collections.Array<NodePath> OpeningEnemyPaths = new();
    [Export] public Node3D BossGate;
    [Export] public Node Boss;
    [Export] public Node ReturnPortal;
    private bool _openingComplete;

    public override void _Ready()
    {
        BossGate ??= GetNodeOrNull<Node3D>("BossGate");
        Boss ??= GetNodeOrNull("RustBucketBob");
        ReturnPortal ??= GetNodeOrNull("ReturnPortal");
        SetActive(BossGate, true);
        SetActive(Boss, false);
        SetActive(ReturnPortal, false);
        GameManager.Instance?.SetObjective("Clear the Moonlit Garden's blight, then confront the creature behind the sealed arch.");
    }

    public override void _Process(double delta)
    {
        if (!_openingComplete && EnemiesDefeated())
        {
            _openingComplete = true;
            SetActive(BossGate, false);
            SetActive(Boss, true);
            GameManager.Instance?.SetObjective("RustBucketBob has emerged. Watch for his recovery after each heavy strike.");
        }
        if (_openingComplete && Boss != null && (Boss.IsQueuedForDeletion() || !IsInstanceValid(Boss)))
        {
            GameManager.Instance?.CompleteFantasyRealm(RealmId);
            SetActive(ReturnPortal, true);
            GameManager.Instance?.SetObjective("The garden is restored. Return through the arch to town.");
            Boss = null;
        }
    }

    private bool EnemiesDefeated()
    {
        foreach (NodePath path in OpeningEnemyPaths)
        {
            Node enemy = GetNodeOrNull(path);
            if (enemy != null && !enemy.IsQueuedForDeletion()) return false;
        }
        return true;
    }

    private static void SetActive(Node node, bool active)
    {
        if (node == null) return;
        node.ProcessMode = active ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
        if (node is Node3D visual) visual.Visible = active;
        if (node is CollisionShape3D collision) collision.SetDeferred("disabled", !active);
        if (node is Area3D area)
        {
            area.SetDeferred("monitoring", active);
            area.SetDeferred("monitorable", active);
        }
        foreach (Node child in node.GetChildren()) SetActive(child, active);
    }
}
