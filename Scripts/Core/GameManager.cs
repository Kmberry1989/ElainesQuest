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

    [Signal]
    public delegate void ObjectiveChangedEventHandler(string objectiveText);

    [Signal]
    public delegate void TownClueFoundEventHandler(string clueId, int foundCount);

    [Signal]
    public delegate void FantasyRealmCompletedEventHandler(string realmId);

    [Signal]
    public delegate void CheckpointChangedEventHandler(string checkpointId, string scenePath);

    public int Glimmers { get; private set; }
    public int FamilyRescuedCount { get; private set; }

    public bool HasMagicRing = true;
    public bool CanTranslateAnimals = true;
    public string CurrentObjective { get; private set; } = "Find Toby near the forest trailhead and learn where Matthew was taken.";
    public string PendingArrivalMarker { get; private set; } = string.Empty;
    public string CheckpointId { get; private set; } = string.Empty;
    public string CheckpointScenePath { get; private set; } = string.Empty;
    public Vector3 CheckpointPosition { get; private set; } = Vector3.Zero;

    private ulong _hitStopUntilMs;
    private float _hitStopPreviousScale = 1.0f;

    public HashSet<string> RescuedFamilyIds { get; } = new();
    public HashSet<string> BefriendedAnimals { get; } = new();
    public HashSet<string> TownClueIds { get; } = new();
    public HashSet<string> CompletedFantasyRealmIds { get; } = new();

    public override void _Process(double delta)
    {
        if (_hitStopUntilMs != 0 && Time.GetTicksMsec() >= _hitStopUntilMs)
        {
            Engine.TimeScale = _hitStopPreviousScale;
            _hitStopUntilMs = 0;
        }
    }

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

    public bool FindTownClue(string clueId)
    {
        if (string.IsNullOrWhiteSpace(clueId) || !TownClueIds.Add(clueId))
        {
            return false;
        }

        EmitSignal(SignalName.TownClueFound, clueId, TownClueIds.Count);
        return true;
    }

    public bool HasTownClue(string clueId)
    {
        return !string.IsNullOrWhiteSpace(clueId) && TownClueIds.Contains(clueId);
    }

    public bool CompleteFantasyRealm(string realmId)
    {
        if (string.IsNullOrWhiteSpace(realmId) || !CompletedFantasyRealmIds.Add(realmId))
        {
            return false;
        }

        EmitSignal(SignalName.FantasyRealmCompleted, realmId);
        return true;
    }

    public bool HasCompletedFantasyRealm(string realmId)
    {
        return !string.IsNullOrWhiteSpace(realmId) && CompletedFantasyRealmIds.Contains(realmId);
    }

    public int GetTownClueCount()
    {
        int count = TownClueIds.Count;
        if (TownClueIds.Contains("TownPortalAwakened"))
        {
            count--;
        }

        return Mathf.Max(0, count);
    }

    public void QueueSceneArrival(string arrivalMarker)
    {
        PendingArrivalMarker = arrivalMarker?.Trim() ?? string.Empty;
    }

    public string ConsumeSceneArrival()
    {
        string marker = PendingArrivalMarker;
        PendingArrivalMarker = string.Empty;
        return marker;
    }

    public void SetCheckpoint(string checkpointId, string scenePath, Vector3 position)
    {
        if (string.IsNullOrWhiteSpace(checkpointId) || string.IsNullOrWhiteSpace(scenePath))
        {
            return;
        }

        CheckpointId = checkpointId.Trim();
        CheckpointScenePath = scenePath.Trim();
        CheckpointPosition = position;
        EmitSignal(SignalName.CheckpointChanged, CheckpointId, CheckpointScenePath);
    }

    public bool TryGetCheckpoint(string scenePath, out Vector3 position)
    {
        position = CheckpointPosition;
        return !string.IsNullOrWhiteSpace(scenePath) &&
            string.Equals(CheckpointScenePath, scenePath, System.StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(CheckpointId);
    }

    public void RequestHitStop(float durationSeconds)
    {
        if (durationSeconds <= 0.0f || Engine.TimeScale < 0.2f)
        {
            return;
        }

        _hitStopPreviousScale = (float)Engine.TimeScale;
        Engine.TimeScale = 0.08f;
        _hitStopUntilMs = Time.GetTicksMsec() + (ulong)Mathf.RoundToInt(durationSeconds * 1000.0f);
    }

    public void RequestDialogue(string speaker, string text)
    {
        EmitSignal(SignalName.DialogueRequested, speaker, text);
    }

    public void SetObjective(string objectiveText)
    {
        string resolvedObjective = string.IsNullOrWhiteSpace(objectiveText)
            ? string.Empty
            : objectiveText.Trim();

        if (CurrentObjective == resolvedObjective)
        {
            return;
        }

        CurrentObjective = resolvedObjective;
        EmitSignal(SignalName.ObjectiveChanged, CurrentObjective);
    }
}
