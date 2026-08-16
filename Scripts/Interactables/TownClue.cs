using Godot;

public partial class TownClue : Area3D, IPlayerInteractable
{
    [Export] public string ClueId = "";
    [Export] public string Speaker = "Elaine";
    [Export] public string DiscoveryText = "This must be connected to the strange portal.";
    [Export] public int GlimmerReward = 1;

    public string InteractionPrompt => "E  Investigate";

    public override void _Ready()
    {
        AddToGroup("player_interaction");
        if (GameManager.Instance?.HasTownClue(ClueId) ?? false)
        {
            QueueFree();
        }
    }

    public bool CanInteract(ElaineController player)
    {
        return IsInsideTree() && Visible && !(GameManager.Instance?.HasTownClue(ClueId) ?? false);
    }

    public void Interact(ElaineController player)
    {
        if (!CanInteract(player))
        {
            return;
        }

        if (GameManager.Instance?.FindTownClue(ClueId) ?? false)
        {
            GameManager.Instance.AddGlimmers(GlimmerReward);
            GameManager.Instance.RequestDialogue(Speaker, DiscoveryText);
        }

        QueueFree();
    }
}
