using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class FamilyTree : Node3D
{
    [Export] public Node3D[] TreeStages;
    [Export] public Node3D[] FamilySpawnPoints;
    [Export] public PackedScene[] FamilyMemberScenes;
    [Export] public CpuParticles3D GrowthParticles;

    public override void _Ready()
    {
        if (TreeStages == null || TreeStages.Length == 0)
        {
            TreeStages = FindChildrenByPrefix<Node3D>("Stage");
        }

        if (FamilySpawnPoints == null || FamilySpawnPoints.Length == 0)
        {
            FamilySpawnPoints = FindChildrenByPrefix<Node3D>("FamilySpawn");
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.FamilyRescuedChanged += OnFamilyRescuedChanged;
        }

        if (FamilyMemberScenes == null || FamilyMemberScenes.Length == 0)
        {
            FamilyMemberScenes = LoadDefaultFamilyScenes();
        }

        UpdateTreeVisuals();
        SpawnRescuedFamily();
    }

    public override void _ExitTree()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.FamilyRescuedChanged -= OnFamilyRescuedChanged;
        }
    }

    public void UpdateTreeVisuals()
    {
        if (GameManager.Instance == null) return;
        int rescued = GameManager.Instance.FamilyRescuedCount;

        foreach (var stage in TreeStages) { if (stage != null) stage.Hide(); }

        if (TreeStages.Length == 0)
        {
            return;
        }

        int stageIndex = Mathf.Clamp(rescued, 0, TreeStages.Length - 1);
        if (TreeStages[stageIndex] != null)
        {
            TreeStages[stageIndex].Show();
        }

        if (GrowthParticles != null)
        {
            GrowthParticles.Emitting = rescued > 0;
        }
    }

    private void SpawnRescuedFamily()
    {
        if (GameManager.Instance == null || FamilySpawnPoints.Length == 0 || FamilyMemberScenes.Length == 0) return;

        foreach (Node child in GetChildren()) { if (child.IsInGroup("SpawnedFamily")) child.QueueFree(); }

        int familyCount = Mathf.Min(GameManager.Instance.FamilyRescuedCount, Mathf.Min(FamilyMemberScenes.Length, FamilySpawnPoints.Length));
        for (int i = 0; i < familyCount; i++)
        {
            if (FamilyMemberScenes[i] != null)
            {
                Node3D familyMember = FamilyMemberScenes[i].Instantiate<Node3D>();
                AddChild(familyMember);
                familyMember.GlobalPosition = FamilySpawnPoints[i].GlobalPosition;
                familyMember.GlobalRotation = FamilySpawnPoints[i].GlobalRotation;
                familyMember.AddToGroup("SpawnedFamily");
            }
        }
    }

    private void OnFamilyRescuedChanged(int _rescuedCount)
    {
        UpdateTreeVisuals();
        SpawnRescuedFamily();
    }

    private T[] FindChildrenByPrefix<T>(string prefix) where T : Node
    {
        List<T> matches = new();
        foreach (Node child in GetChildren())
        {
            T typedChild = child as T;
            if (typedChild != null && child.Name.ToString().StartsWith(prefix))
            {
                matches.Add(typedChild);
            }
        }

        return matches.ToArray();
    }

    private PackedScene[] LoadDefaultFamilyScenes()
    {
        string[] familyScenePaths =
        {
            "res://Scenes/Characters/Family/Matthew.tscn",
            "res://Scenes/Characters/Family/Ann.tscn",
            "res://Scenes/Characters/Family/Antonio.tscn",
            "res://Scenes/Characters/Family/Armando.tscn",
            "res://Scenes/Characters/Family/GrandmaJudy.tscn"
        };

        return familyScenePaths
            .Where(path => ResourceLoader.Exists(path))
            .Select(path => GD.Load<PackedScene>(path))
            .Where(scene => scene != null)
            .ToArray();
    }
}
