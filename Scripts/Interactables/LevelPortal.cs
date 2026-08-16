using Godot;

public partial class LevelPortal : Area3D, IPlayerInteractable
{
    [Export] public string TargetScene = "res://Scenes/Levels/World_1_Forest.tscn";
    [Export] public string PortalName = "Portal";
    [Export] public string RequiredClueId = "";
    [Export] public string RequiredRealmId = "";
    [Export] public string LockedMessage = "This portal is dormant. Find the clues that can awaken it.";
    [Export] public string ArrivalMarker = "";

    public string InteractionPrompt => $"E  Enter {PortalName}";

    public override void _Ready()
    {
        AddToGroup("player_interaction");
    }

    public bool CanInteract(ElaineController player)
    {
        return IsInsideTree() && Visible && ProcessMode != ProcessModeEnum.Disabled;
    }

    public void Interact(ElaineController player)
    {
        if (!CanInteract(player)) return;

        bool hasClue = string.IsNullOrWhiteSpace(RequiredClueId) || (GameManager.Instance?.HasTownClue(RequiredClueId) ?? false);
        bool hasRealm = string.IsNullOrWhiteSpace(RequiredRealmId) || (GameManager.Instance?.HasCompletedFantasyRealm(RequiredRealmId) ?? false);
        if (!hasClue || !hasRealm)
        {
            GameManager.Instance?.RequestDialogue(PortalName, LockedMessage);
            return;
        }

        if (!string.IsNullOrWhiteSpace(TargetScene) && ResourceLoader.Exists(TargetScene))
        {
            GameManager.Instance?.QueueSceneArrival(ArrivalMarker);
            if (ElainesSceneTransition.Instance != null)
            {
                ElainesSceneTransition.Instance.TransitionTo(TargetScene);
            }
            else
            {
                GetTree().ChangeSceneToFile(TargetScene);
            }
        }
    }
}
