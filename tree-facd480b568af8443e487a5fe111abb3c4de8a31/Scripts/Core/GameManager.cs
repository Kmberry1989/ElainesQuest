using Godot;
using System.Collections.Generic;

public partial class GameManager : Node
{
    public const int TotalFamilyMembers = 5;

    public static GameManager Instance { get; private set; }

    [Signal]
    public delegate void GlimmersChangedEventHandler(int glimmers);

    [Signal]
    public delegate void FamilyRescuedChangedEventHandler(int rescuedCount);

    [Signal]
    public delegate void AnimalBefriendedEventHandler(string animalId);

    [Signal]
    public delegate void DialogueRequestedEventHandler(string speaker, string text);

    public int Glimmers { get; private set; }
    public int FamilyRescuedCount { get; private set; }

    public bool HasMagicRing = true;
    public bool CanTranslateAnimals = true;

    public HashSet<string> RescuedFamilyIds { get; } = new();
    public HashSet<string> BefriendedAnimals { get; } = new();

    public override void _EnterTree()
    {
        if (Instance != null && Instance != this)
        {
            QueueFree();
            return;
        }

        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void AddGlimmers(int amount)
    {
        if (amount == 0)
        {
            return;
        }

        Glimmers = Mathf.Max(0, Glimmers + amount);
        EmitSignal(SignalName.GlimmersChanged, Glimmers);
        GD.Print($"Glimmers: {Glimmers}");
    }

    public void RescueFamilyMember(string familyId)
    {
        if (string.IsNullOrWhiteSpace(familyId) || !RescuedFamilyIds.Add(familyId))
        {
            return;
        }

        FamilyRescuedCount = RescuedFamilyIds.Count;
        EmitSignal(SignalName.FamilyRescuedChanged, FamilyRescuedCount);
        GD.Print($"Family Rescued: {FamilyRescuedCount}/{TotalFamilyMembers}");
    }

    public void BefriendAnimal(string animalId)
    {
        if (string.IsNullOrWhiteSpace(animalId) || !BefriendedAnimals.Add(animalId))
        {
            return;
        }

        EmitSignal(SignalName.AnimalBefriended, animalId);
        GD.Print($"{animalId} has moved to the Hub!");
    }

    public bool HasRescuedFamilyMember(string familyId)
    {
        return !string.IsNullOrWhiteSpace(familyId) && RescuedFamilyIds.Contains(familyId);
    }

    public bool HasBefriendedAnimal(string animalId)
    {
        return !string.IsNullOrWhiteSpace(animalId) && BefriendedAnimals.Contains(animalId);
    }

    public void RequestDialogue(string speaker, string text)
    {
        EmitSignal(SignalName.DialogueRequested, speaker, text);
    }
}
