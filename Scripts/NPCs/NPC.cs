using Godot;

public partial class NPC : Node3D, IPlayerInteractable
{
    [Export] public string CharacterName = "Unknown Animal";
    [Export] public string BaseDialogue = "*Squeak squeak!*";
    [Export] public string TranslatedDialogue = "Hello Elaine! Thank goodness you have that ring!";
    [Export] public string AnimalId = "";
    [Export] public string RequiredRealmId = "";
    [Export(PropertyHint.MultilineText)] public string RequiredRealmDialogue = "";
    [Export] public string RequiredRealmObjective = "";
    [Export] public string CompletedRealmId = "";
    [Export(PropertyHint.MultilineText)] public string CompletedRealmDialogue = "";

    public string InteractionPrompt => "E  Talk";

    public override void _Ready()
    {
        AddToGroup("npc_interaction");
        AddToGroup("player_interaction");
    }

    // Called when Elaine presses the 'interact' button inside this NPC's Area3D
    public bool CanInteract(ElaineController player)
    {
        return IsInsideTree() && Visible && ProcessMode != ProcessModeEnum.Disabled;
    }

    public void Interact(ElaineController player)
    {
        if (!CanInteract(player)) return;
        string resolvedAnimalId = string.IsNullOrWhiteSpace(AnimalId) ? CharacterName : AnimalId;

        if (!string.IsNullOrWhiteSpace(CompletedRealmId) &&
            GameManager.Instance?.HasCompletedFantasyRealm(CompletedRealmId) == true &&
            !string.IsNullOrWhiteSpace(CompletedRealmDialogue))
        {
            GD.Print($"{CharacterName} says: {CompletedRealmDialogue}");
            GameManager.Instance.RequestDialogue(CharacterName, CompletedRealmDialogue);
            return;
        }

        if (!string.IsNullOrWhiteSpace(RequiredRealmId) &&
            GameManager.Instance?.HasCompletedFantasyRealm(RequiredRealmId) == true &&
            !string.IsNullOrWhiteSpace(RequiredRealmDialogue))
        {
            GD.Print($"{CharacterName} says: {RequiredRealmDialogue}");
            GameManager.Instance.RequestDialogue(CharacterName, RequiredRealmDialogue);
            if (!string.IsNullOrWhiteSpace(RequiredRealmObjective))
            {
                GameManager.Instance.SetObjective(RequiredRealmObjective);
            }

            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.CanTranslateAnimals)
        {
            GD.Print($"{CharacterName} says: {TranslatedDialogue}");
            GameManager.Instance.RequestDialogue(CharacterName, TranslatedDialogue);

            // If they haven't been sent to the Hub yet, send them!
            GameManager.Instance.BefriendAnimal(resolvedAnimalId);
        }
        else
        {
            GD.Print($"{CharacterName} says: {BaseDialogue}");
            GameManager.Instance?.RequestDialogue(CharacterName, BaseDialogue);
        }
    }

    public void Interact() => Interact(null);
}
