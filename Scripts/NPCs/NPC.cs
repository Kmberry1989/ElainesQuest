using Godot;

public partial class NPC : Node3D
{
    [Export] public string CharacterName = "Unknown Animal";
    [Export] public string BaseDialogue = "*Squeak squeak!*";
    [Export] public string TranslatedDialogue = "Hello Elaine! Thank goodness you have that ring!";
    [Export] public string AnimalId = "";

    public override void _Ready()
    {
        AddToGroup("npc_interaction");
    }

    // Called when Elaine presses the 'interact' button inside this NPC's Area3D
    public void Interact()
    {
        string resolvedAnimalId = string.IsNullOrWhiteSpace(AnimalId) ? CharacterName : AnimalId;

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
}
