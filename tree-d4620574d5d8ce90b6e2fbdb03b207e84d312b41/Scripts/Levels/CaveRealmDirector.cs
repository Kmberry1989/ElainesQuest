using Godot;
using System.Threading.Tasks;

public partial class CaveRealmDirector : Node3D
{
    [Export] public string RealmId = "EchoingCaves";
    [Export] public Node AmbushEnemy;
    [Export] public Node PressureEnemy;
    [Export] public Node Boss;
    [Export] public Node3D FirstGate;
    [Export] public Node3D BossGate;
    [Export] public Node ReturnPortal;

    private int _stage;

    public int CurrentStage => _stage;

    public override void _Ready()
    {
        AmbushEnemy ??= GetNodeOrNull("CorkrotAmbush");
        PressureEnemy ??= GetNodeOrNull("FungalPressure");
        Boss ??= GetNodeOrNull("BriarWarden");
        FirstGate ??= GetNodeOrNull<Node3D>("FirstGate");
        BossGate ??= GetNodeOrNull<Node3D>("BossGate");
        ReturnPortal ??= GetNodeOrNull("ReturnPortal");

        if (GameManager.Instance?.HasCompletedFantasyRealm(RealmId) ?? false)
        {
            _stage = 3;
            SetActive(AmbushEnemy, false);
            SetActive(PressureEnemy, false);
            SetActive(Boss, false);
            SetActive(FirstGate, false);
            SetActive(BossGate, false);
            SetActive(ReturnPortal, true);
            GameManager.Instance?.SetObjective("The Echoing Caves are quiet. Return home and prepare for the final lead.");
            return;
        }

        _stage = 0;
        SetActive(AmbushEnemy, true);
        SetActive(PressureEnemy, false);
        SetActive(Boss, false);
        SetActive(FirstGate, true);
        SetActive(BossGate, true);
        SetActive(ReturnPortal, false);
        GameManager.Instance?.SetObjective("Follow the echoing trail and clear the cave mouth of the first corruption.");

        if (HasVerificationArgument())
        {
            CallDeferred(nameof(RunVerification));
        }
    }

    public override void _Process(double delta)
    {
        if (_stage == 0 && IsDefeated(AmbushEnemy))
        {
            _stage = 1;
            SetActive(FirstGate, false);
            SetActive(PressureEnemy, true);
            GameManager.Instance?.SetObjective("The tunnel opens, but the fungal blight is choking the crystal passage.");
        }
        else if (_stage == 1 && IsDefeated(PressureEnemy))
        {
            _stage = 2;
            SetActive(BossGate, false);
            SetActive(Boss, true);
            GameManager.Instance?.SetObjective("The Briar Warden is awake. Evade its charge after every heavy swipe.");
        }
        else if (_stage == 2 && IsDefeated(Boss))
        {
            _stage = 3;
            GameManager.Instance?.CompleteFantasyRealm(RealmId);
            SetActive(ReturnPortal, true);
            GameManager.Instance?.SetObjective("The caves are restored. Return to your family for the final lead.");
        }
    }

    private static bool IsDefeated(Node node)
    {
        return node == null || !IsInstanceValid(node) || node.IsQueuedForDeletion();
    }

    private async void RunVerification()
    {
        await WaitSeconds(0.15f);
        bool openingReady = CurrentStage == 0 && IsActive(AmbushEnemy) && !IsActive(PressureEnemy) && IsActive(FirstGate) && IsActive(BossGate) && !IsActive(ReturnPortal);
        Defeat(AmbushEnemy);
        await WaitSeconds(1.2f);
        bool pressureReady = CurrentStage == 1 && !IsActive(FirstGate) && IsActive(PressureEnemy) && !IsActive(Boss);
        Defeat(PressureEnemy);
        await WaitSeconds(1.2f);
        bool bossReady = CurrentStage == 2 && !IsActive(BossGate) && IsActive(Boss);
        Defeat(Boss);
        await WaitSeconds(1.8f);
        bool complete = CurrentStage == 3 && IsActive(ReturnPortal) && (GameManager.Instance?.HasCompletedFantasyRealm(RealmId) ?? false);
        bool passed = openingReady && pressureReady && bossReady && complete;
        GD.Print($"CAVE_VERIFY: {(passed ? "PASS" : "FAIL")} (opening {openingReady}, pressure {pressureReady}, boss {bossReady}, complete {complete})");
        GetTree().Quit(passed ? 0 : 1);
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
            if (argument == "--verify-caves")
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
