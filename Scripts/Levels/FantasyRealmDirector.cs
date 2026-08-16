using Godot;
using System.Threading.Tasks;

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

        if (HasVerificationArgument())
        {
            CallDeferred(nameof(RunVerification));
        }
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
        if (_openingComplete && Boss != null && (!IsInstanceValid(Boss) || Boss.IsQueuedForDeletion()))
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

    private async void RunVerification()
    {
        await WaitSeconds(0.15f);
        bool openingReady = IsActive(BossGate) && !IsActive(Boss) && !IsActive(ReturnPortal);
        foreach (NodePath path in OpeningEnemyPaths)
        {
            GetNodeOrNull(path)?.GetNodeOrNull<CombatHealth>("Health")?.ApplyDamage(999.0f);
        }

        await WaitSeconds(1.2f);
        bool bossReady = !IsActive(BossGate) && IsActive(Boss);
        Boss?.GetNodeOrNull<CombatHealth>("Health")?.ApplyDamage(999.0f);
        await WaitSeconds(1.8f);
        bool complete = IsActive(ReturnPortal) && (GameManager.Instance?.HasCompletedFantasyRealm(RealmId) ?? false);
        bool passed = openingReady && bossReady && complete;
        GD.Print($"MOONLIT_VERIFY: {(passed ? "PASS" : "FAIL")} (opening {openingReady}, boss {bossReady}, complete {complete})");
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
            if (argument == "--verify-moonlit")
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
