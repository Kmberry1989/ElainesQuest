using Godot;
using System.Threading.Tasks;

public partial class FinalChapterDirector : Node3D
{
    [Export] public string RealmId = "StarfallSanctum";
    [Export] public Node Guardian;
    [Export] public Node ReturnPortal;
    [Export] public Node3D ArrivalMarker;

    private int _stage;

    public int CurrentStage => _stage;

    public override void _Ready()
    {
        Guardian ??= GetNodeOrNull("StarfallGuardian");
        ReturnPortal ??= GetNodeOrNull("ReturnPortal");
        ArrivalMarker ??= GetNodeOrNull<Node3D>("StarfallArrival");

        bool alreadyComplete = GameManager.Instance?.HasCompletedFantasyRealm(RealmId) ?? false;
        _stage = alreadyComplete ? 1 : 0;
        SetActive(Guardian, !alreadyComplete);
        SetActive(ReturnPortal, alreadyComplete);

        GameManager.Instance?.SetObjective(alreadyComplete
            ? "The Starfall Sanctum is quiet. Return to the Hub and share what you found."
            : "Follow Matthew's final lead into the Starfall Sanctum and defeat the starfall guardian.");

        if (HasVerificationArgument())
        {
            CallDeferred(nameof(RunVerification));
        }
    }

    public override void _Process(double delta)
    {
        if (_stage == 0 && IsDefeated(Guardian))
        {
            _stage = 1;
            GameManager.Instance?.CompleteFantasyRealm(RealmId);
            SetActive(ReturnPortal, true);
            GameManager.Instance?.SetObjective("The starfall seal is restored. Return to the Hub and share the final lead.");
        }
    }

    private async void RunVerification()
    {
        await WaitSeconds(0.15f);
        bool arrivalReady = ArrivalMarker != null && IsInstanceValid(ArrivalMarker) &&
            string.Equals(ArrivalMarker.Name, "StarfallArrival", System.StringComparison.Ordinal);
        bool openingReady = CurrentStage == 0 && IsActive(Guardian) && !IsActive(ReturnPortal);
        bool unlockReady = VerifyHubUnlockContract();

        Defeat(Guardian);
        await WaitSeconds(1.2f);

        bool complete = CurrentStage == 1 && IsActive(ReturnPortal) &&
            (GameManager.Instance?.HasCompletedFantasyRealm(RealmId) ?? false);
        bool returnReady = ReturnPortal is LevelPortal portal &&
            string.Equals(portal.TargetScene, "res://Scenes/Hub/HubWorld.tscn", System.StringComparison.Ordinal) &&
            string.Equals(portal.ArrivalMarker, "StarfallReturnArrival", System.StringComparison.Ordinal);
        bool passed = arrivalReady && openingReady && unlockReady && complete && returnReady;
        GD.Print($"STARFALL_VERIFY: {(passed ? "PASS" : "FAIL")} (arrival {arrivalReady}, opening {openingReady}, unlock {unlockReady}, complete {complete}, return {returnReady})");
        GetTree().Quit(passed ? 0 : 1);
    }

    private static bool VerifyHubUnlockContract()
    {
        PackedScene hubScene = GD.Load<PackedScene>("res://Scenes/Hub/HubWorld.tscn");
        Node hub = hubScene?.Instantiate();
        LevelPortal portal = hub?.GetNodeOrNull<LevelPortal>("StarfallSanctumPortal");
        bool valid = portal != null &&
            string.Equals(portal.RequiredRealmId, "EchoingCaves", System.StringComparison.Ordinal) &&
            string.Equals(portal.TargetScene, "res://Scenes/Levels/StarfallSanctum.tscn", System.StringComparison.Ordinal) &&
            string.Equals(portal.ArrivalMarker, "StarfallArrival", System.StringComparison.Ordinal) &&
            hub.GetNodeOrNull<Node3D>("StarfallReturnArrival") != null;
        hub?.Free();
        return valid;
    }

    private static bool IsDefeated(Node node)
    {
        return node == null || !IsInstanceValid(node) || node.IsQueuedForDeletion();
    }

    private static bool IsActive(Node node)
    {
        return node is not Node3D visual || visual.Visible;
    }

    private static void Defeat(Node node)
    {
        node?.GetNodeOrNull<CombatHealth>("Health")?.ApplyDamage(999.0f);
    }

    private static bool HasVerificationArgument()
    {
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (argument == "--verify-starfall")
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
        if (node == null)
        {
            return;
        }

        node.ProcessMode = active ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
        if (node is Node3D visual)
        {
            visual.Visible = active;
        }

        if (node is CollisionShape3D collision)
        {
            collision.SetDeferred("disabled", !active);
        }

        if (node is Area3D area)
        {
            area.SetDeferred("monitoring", active);
            area.SetDeferred("monitorable", active);
        }

        foreach (Node child in node.GetChildren())
        {
            SetActive(child, active);
        }
    }
}
