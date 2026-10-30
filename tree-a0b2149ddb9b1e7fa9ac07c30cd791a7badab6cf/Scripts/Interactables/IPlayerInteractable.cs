public interface IPlayerInteractable
{
    string InteractionPrompt { get; }
    bool CanInteract(ElaineController player);
    void Interact(ElaineController player);
}
