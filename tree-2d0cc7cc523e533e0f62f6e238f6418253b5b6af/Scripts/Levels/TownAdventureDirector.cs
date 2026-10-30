using Godot;
using System.Threading.Tasks;

public partial class TownAdventureDirector : Node3D
{
    private static readonly string[] RequiredTownClueIds =
    {
        "TownHomeNote",
        "TownStoreReceipt",
        "TownSchoolMark"
    };

    [Export] public string PortalClueId = "TownPortalAwakened";
    [Export] public int CluesRequired = 3;
    [Export] public Node3D FantasyPortal;
    [Export] public Node3D CompletionBeacon;
    [Export] public Node3D ReturnToHub;

    public override void _Ready()
    {
        FantasyPortal ??= GetNodeOrNull<Node3D>("FantasyPortal");
        CompletionBeacon ??= GetNodeOrNull<Node3D>("CompletionBeacon");
        ReturnToHub ??= GetNodeOrNull<Node3D>("ReturnToHub");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TownClueFound += OnTownClueFound;
            GameManager.Instance.FantasyRealmCompleted += OnFantasyRealmCompleted;
        }
        Refresh();

        if (HasVerificationArgument())
        {
            CallDeferred(nameof(RunVerification));
        }
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
        int clues = RequiredClueCount();
        bool portalAwake = RequiredCluesFound();
        bool realmComplete = GameManager.Instance?.HasCompletedFantasyRealm("MoonlitGarden") ?? false;
        if (portalAwake) GameManager.Instance?.FindTownClue(PortalClueId);
        SetActive(FantasyPortal, portalAwake && !realmComplete);
        SetActive(CompletionBeacon, realmComplete);
        SetActive(ReturnToHub, realmComplete);
        GameManager.Instance?.SetObjective(realmComplete
            ? "Moonlit Garden is restored. Return home, speak with Matthew, and follow his lead to the caves."
            : portalAwake
                ? "The schoolyard gate has awakened. Step through and restore the Moonlit Garden."
                : $"Investigate the town disturbance ({clues}/{CluesRequired} clues found)."
        );
    }

    private int RequiredClueCount()
    {
        if (GameManager.Instance == null)
        {
            return 0;
        }

        int found = 0;
        foreach (string clueId in RequiredTownClueIds)
        {
            if (GameManager.Instance.HasTownClue(clueId))
            {
                found++;
            }
        }

        return found;
    }

    private bool RequiredCluesFound()
    {
        return RequiredClueCount() >= CluesRequired && RequiredTownClueIds.Length == CluesRequired;
    }

    private async void RunVerification()
    {
        await WaitSeconds(0.15f);
        bool startsLocked = !IsActive(FantasyPortal) && !IsActive(ReturnToHub);

        GameManager.Instance?.FindTownClue("UnrelatedClue");
        await WaitSeconds(0.1f);
        bool unrelatedClueDoesNotUnlock = !IsActive(FantasyPortal);

        foreach (string clueId in RequiredTownClueIds)
        {
            GameManager.Instance?.FindTownClue(clueId);
        }

        await WaitSeconds(0.1f);
        bool threeCluesUnlockPortal = IsActive(FantasyPortal) &&
            (GameManager.Instance?.HasTownClue(PortalClueId) ?? false);

        GameManager.Instance?.CompleteFantasyRealm("MoonlitGarden");
        await WaitSeconds(0.1f);
        bool completedRouteShowsHandoff = !IsActive(FantasyPortal) && IsActive(CompletionBeacon) && IsActive(ReturnToHub);
        bool passed = startsLocked && unrelatedClueDoesNotUnlock && threeCluesUnlockPortal && completedRouteShowsHandoff;
        GD.Print($"TOWN_VERIFY: {(passed ? "PASS" : "FAIL")} (locked {startsLocked}, exact clues {unrelatedClueDoesNotUnlock}, portal {threeCluesUnlockPortal}, handoff {completedRouteShowsHandoff})");
        GetTree().Quit(passed ? 0 : 1);
    }

    private static bool IsActive(Node node)
    {
        return node is not Node3D visual || visual.Visible;
    }

    private static bool HasVerificationArgument()
    {
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (argument == "--verify-town")
            {
                return true;
            }
        }

        return false;
    }

    private async Task WaitSeconds(float seconds)
    {
        await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
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
