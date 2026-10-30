using Godot;

public partial class ForestAdventureDirector : Node3D
{
    [ExportCategory("Progression")]
    [Export] public string GuideAnimalId = "Toby";
    [Export] public string RescuedFamilyId = "Matthew";

    [ExportCategory("Encounter Enemies")]
    [Export] public Godot.Collections.Array<NodePath> FirstEncounterEnemyPaths = new();
    [Export] public Godot.Collections.Array<NodePath> SecondEncounterEnemyPaths = new();

    [ExportCategory("Scene Nodes")]
    [Export] public Node3D FirstGate;
    [Export] public Node3D SecondGate;
    [Export] public Node RescueNode;
    [Export] public Node ReturnPortal;

    private bool _guideMet;
    private bool _firstEncounterComplete;
    private bool _secondEncounterComplete;
    private bool _familyRescued;

    public override void _Ready()
    {
        FirstGate ??= GetNodeOrNull<Node3D>("FirstEncounterGate");
        SecondGate ??= GetNodeOrNull<Node3D>("SecondEncounterGate");
        RescueNode ??= GetNodeOrNull("MatthewRescue");
        ReturnPortal ??= GetNodeOrNull("ReturnPortal");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AnimalBefriended += OnAnimalBefriended;
            GameManager.Instance.FamilyRescuedChanged += OnFamilyRescuedChanged;
        }

        SyncProgressFromGameState();
        UpdateWorldState();
    }

    public override void _ExitTree()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AnimalBefriended -= OnAnimalBefriended;
            GameManager.Instance.FamilyRescuedChanged -= OnFamilyRescuedChanged;
        }
    }

    public override void _Process(double delta)
    {
        if (_guideMet && !_firstEncounterComplete && AreEncounterEnemiesDefeated(FirstEncounterEnemyPaths))
        {
            _firstEncounterComplete = true;
            GameManager.Instance?.RequestDialogue("Forest Trail", "The first corrupted clearing calms down. The ridge path has opened.");
            UpdateWorldState();
        }

        if (_firstEncounterComplete && !_secondEncounterComplete && AreEncounterEnemiesDefeated(SecondEncounterEnemyPaths))
        {
            _secondEncounterComplete = true;
            GameManager.Instance?.RequestDialogue("Forest Trail", "The upper ridge is safe now. Matthew is just ahead.");
            UpdateWorldState();
        }
    }

    private void SyncProgressFromGameState()
    {
        _guideMet = GameManager.Instance?.HasBefriendedAnimal(GuideAnimalId) ?? false;
        _familyRescued = GameManager.Instance?.HasRescuedFamilyMember(RescuedFamilyId) ?? false;
        _firstEncounterComplete = AreEncounterEnemiesDefeated(FirstEncounterEnemyPaths);
        _secondEncounterComplete = AreEncounterEnemiesDefeated(SecondEncounterEnemyPaths);
    }

    private void OnAnimalBefriended(string animalId)
    {
        if (!string.Equals(animalId, GuideAnimalId, System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _guideMet = true;
        UpdateWorldState();
    }

    private void OnFamilyRescuedChanged(int _rescuedCount)
    {
        if (!(GameManager.Instance?.HasRescuedFamilyMember(RescuedFamilyId) ?? false))
        {
            return;
        }

        _familyRescued = true;
        UpdateWorldState();
    }

    private bool AreEncounterEnemiesDefeated(Godot.Collections.Array<NodePath> enemyPaths)
    {
        if (enemyPaths == null || enemyPaths.Count == 0)
        {
            return true;
        }

        foreach (NodePath enemyPath in enemyPaths)
        {
            if (enemyPath.IsEmpty)
            {
                continue;
            }

            Node enemy = GetNodeOrNull(enemyPath);
            if (enemy != null && !enemy.IsQueuedForDeletion())
            {
                return false;
            }
        }

        return true;
    }

    private void UpdateWorldState()
    {
        SetBarrierActive(FirstGate, !_firstEncounterComplete);
        SetBarrierActive(SecondGate, !_secondEncounterComplete);
        SetInteractableActive(RescueNode, _secondEncounterComplete && !_familyRescued);
        SetInteractableActive(ReturnPortal, _familyRescued);
        UpdateObjective();
    }

    private void UpdateObjective()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (!_guideMet)
        {
            GameManager.Instance.SetObjective("Reach Toby and learn what happened in the forest.");
            return;
        }

        if (!_firstEncounterComplete)
        {
            GameManager.Instance.SetObjective("Clear the lower clearing with Elaine's spells.");
            return;
        }

        if (!_secondEncounterComplete)
        {
            GameManager.Instance.SetObjective("Cross the ridge and defeat the ambush ahead.");
            return;
        }

        if (!_familyRescued)
        {
            GameManager.Instance.SetObjective("Rescue Matthew at the lantern overlook.");
            return;
        }

        GameManager.Instance.SetObjective("Return to the Hub and grow the Family Tree.");
    }

    private static void SetBarrierActive(Node node, bool isActive)
    {
        if (node == null)
        {
            return;
        }

        node.ProcessMode = isActive ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;

        foreach (Node child in node.GetChildren())
        {
            SetBarrierActive(child, isActive);
        }

        if (node is CollisionShape3D collisionShape)
        {
            collisionShape.SetDeferred("disabled", !isActive);
        }

        if (node is Area3D area)
        {
            area.SetDeferred("monitoring", isActive);
            area.SetDeferred("monitorable", isActive);
        }

        if (node is VisualInstance3D visual)
        {
            visual.Visible = isActive;
        }
    }

    private static void SetInteractableActive(Node node, bool isActive)
    {
        if (node == null)
        {
            return;
        }

        node.ProcessMode = isActive ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;

        foreach (Node child in node.GetChildren())
        {
            SetInteractableActive(child, isActive);
        }

        if (node is CollisionShape3D collisionShape)
        {
            collisionShape.SetDeferred("disabled", !isActive);
        }

        if (node is Area3D area)
        {
            area.SetDeferred("monitoring", isActive);
            area.SetDeferred("monitorable", isActive);
        }

        if (node is VisualInstance3D visual)
        {
            visual.Visible = isActive;
        }
    }
}
