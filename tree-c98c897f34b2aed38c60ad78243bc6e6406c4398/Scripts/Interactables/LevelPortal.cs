using Godot;

public partial class LevelPortal : Area3D
{
    [Export] public string TargetScene = "res://Scenes/Levels/World_1_Forest.tscn";
    [Export] public string RequiredClueId = "";
    [Export] public string RequiredRealmId = "";
    [Export] public string LockedMessage = "This portal is dormant. Find the clues that can awaken it.";

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not ElaineController && !body.IsInGroup("player"))
        {
            return;
        }

        bool hasClue = string.IsNullOrWhiteSpace(RequiredClueId) || (GameManager.Instance?.HasTownClue(RequiredClueId) ?? false);
        bool hasRealm = string.IsNullOrWhiteSpace(RequiredRealmId) || (GameManager.Instance?.HasCompletedFantasyRealm(RequiredRealmId) ?? false);
        if (!hasClue || !hasRealm)
        {
            GameManager.Instance?.RequestDialogue("Moonlit Gate", LockedMessage);
            return;
        }

        if (!string.IsNullOrWhiteSpace(TargetScene) && ResourceLoader.Exists(TargetScene))
        {
            GetTree().ChangeSceneToFile(TargetScene);
        }
    }
}
