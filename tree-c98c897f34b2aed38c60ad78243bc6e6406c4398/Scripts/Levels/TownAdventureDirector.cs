using Godot;

public partial class TownAdventureDirector : Node3D
{
    [Export] public string PortalClueId = "TownPortalAwakened";
    [Export] public int CluesRequired = 3;
    [Export] public Node3D FantasyPortal;
    [Export] public Node3D CompletionBeacon;

    public override void _Ready()
    {
        FantasyPortal ??= GetNodeOrNull<Node3D>("FantasyPortal");
        CompletionBeacon ??= GetNodeOrNull<Node3D>("CompletionBeacon");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TownClueFound += OnTownClueFound;
            GameManager.Instance.FantasyRealmCompleted += OnFantasyRealmCompleted;
        }
        Refresh();
    }

    public override void _ExitTree()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TownClueFound -= OnTownClueFound;
            GameManager.Instance.FantasyRealmCompleted -= OnFantasyRealmCompleted;
        }
    }

    private void OnTownClueFound(string _, int __) => Refresh();
    private void OnFantasyRealmCompleted(string _) => Refresh();

    private void Refresh()
    {
        int clues = GameManager.Instance?.TownClueIds.Count ?? 0;
        bool portalAwake = clues >= CluesRequired;
        bool realmComplete = GameManager.Instance?.HasCompletedFantasyRealm("MoonlitGarden") ?? false;
        if (portalAwake) GameManager.Instance?.FindTownClue(PortalClueId);
        SetActive(FantasyPortal, portalAwake && !realmComplete);
        SetActive(CompletionBeacon, realmComplete);
        GameManager.Instance?.SetObjective(realmComplete
            ? "The Moonlit Garden is calm. Speak with your family at home and follow the new lead toward the caves."
            : portalAwake
                ? "The schoolyard gate has awakened. Step through and restore the Moonlit Garden."
                : $"Investigate the town disturbance ({clues}/{CluesRequired} clues found)."
        );
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
