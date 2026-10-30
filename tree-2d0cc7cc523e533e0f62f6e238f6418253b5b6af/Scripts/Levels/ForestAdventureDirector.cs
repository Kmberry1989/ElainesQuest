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
            GameManager.Instance?.RequestDialogue("Forest Trail", "The lower clearing is calm again. The ridge seal has fallen. Push across the bridge and break the ambush above.");
            UpdateWorldState();
        }

        if (_firstEncounterComplete && !_secondEncounterComplete && AreEncounterEnemiesDefeated(SecondEncounterEnemyPaths))
        {
            _secondEncounterComplete = true;
            GameManager.Instance?.RequestDialogue("Forest Trail", "The upper ambush is broken. Matthew is waiting at the lantern overlook just ahead.");
            UpdateWorldState();
        }
    }

    private void SyncProgressFromGameState()
    {
        _guideMet = GameManager.Instance?.HasBefriendedAnimal(GuideAnimalId) ?? false;
        _familyRescued = GameManager.Instance?.HasRescuedFamilyMember(RescuedFamilyId) ?? false;

        if (_familyRescued)
        {
            _guideMet = true;
            _firstEncounterComplete = true;
            _secondEncounterComplete = true;
            ClearEncounterEnemies(FirstEncounterEnemyPaths);
            ClearEncounterEnemies(SecondEncounterEnemyPaths);
            return;
        }

        _firstEncounterComplete = AreEncounterEnemiesDefeated(FirstEncounterEnemyPaths);
        _secondEncounterComplete = AreEncounterEnemiesDefeated(SecondEncounterEnemyPaths);

        if (_secondEncounterComplete)
        {
            _guideMet = true;
            _firstEncounterComplete = true;
        }
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
        _guideMet = true;
        _firstEncounterComplete = true;
        _secondEncounterComplete = true;
        ClearEncounterEnemies(FirstEncounterEnemyPaths);
        ClearEncounterEnemies(SecondEncounterEnemyPaths);
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
        bool firstEncounterActive = _guideMet && !_firstEncounterComplete && !_familyRescued;
        bool secondEncounterActive = _firstEncounterComplete && !_secondEncounterComplete && !_familyRescued;

        SetEncounterEnemiesActive(FirstEncounterEnemyPaths, firstEncounterActive);
        SetEncounterEnemiesActive(SecondEncounterEnemyPaths, secondEncounterActive);
        SetNodeActive(FirstGate, !_firstEncounterComplete);
        SetNodeActive(SecondGate, !_secondEncounterComplete);
        SetNodeActive(RescueNode, _secondEncounterComplete && !_familyRescued);
        SetNodeActive(ReturnPortal, _familyRescued);
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
            GameManager.Instance.SetObjective("Find Toby near the forest trailhead and learn where Matthew was taken.");
            return;
        }

        if (!_firstEncounterComplete)
        {
            GameManager.Instance.SetObjective("Hold Tab to choose Star Volley, then clear the lower clearing and break the ridge seal.");
            return;
        }

        if (!_secondEncounterComplete)
        {
            GameManager.Instance.SetObjective("Choose Sky Lance from the spell wheel; pierce the upper ambush guarding Matthew.");
            return;
        }

        if (!_familyRescued)
        {
            GameManager.Instance.SetObjective("Rescue Matthew at the lantern overlook.");
            return;
        }

        GameManager.Instance.SetObjective("Matthew is safe. Return to the Hub and help the Family Tree bloom.");
    }

    private void SetEncounterEnemiesActive(Godot.Collections.Array<NodePath> enemyPaths, bool isActive)
    {
        if (enemyPaths == null || enemyPaths.Count == 0)
        {
            return;
        }

        foreach (NodePath enemyPath in enemyPaths)
        {
            if (enemyPath.IsEmpty)
            {
                continue;
            }

            SetNodeActive(GetNodeOrNull(enemyPath), isActive);
        }
    }

    private void ClearEncounterEnemies(Godot.Collections.Array<NodePath> enemyPaths)
    {
        if (enemyPaths == null || enemyPaths.Count == 0)
        {
            return;
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
                enemy.QueueFree();
            }
        }
    }

    private static void SetNodeActive(Node node, bool isActive)
    {
        if (node == null)
        {
            return;
        }

        node.ProcessMode = isActive ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;

        foreach (Node child in node.GetChildren())
        {
            SetNodeActive(child, isActive);
        }

        if (node is Node3D node3D)
        {
            node3D.Visible = isActive;
        }

        if (node is CollisionShape3D collisionShape)
        {
            collisionShape.SetDeferred("disabled", !isActive);
        }

        if (node is CombatMeleeHitbox meleeHitbox)
        {
            meleeHitbox.EndAttack();
            return;
        }

        if (node is Area3D area)
        {
            area.SetDeferred("monitoring", isActive);
            area.SetDeferred("monitorable", isActive);
        }
    }
}
