using Godot;
using System.Collections.Generic;

public partial class HubAnimalSpawner : Node3D
{
    [Export] public Godot.Collections.Dictionary<string, PackedScene> AnimalPrefabs = new();
    [Export] public Marker3D[] SpawnMarkers;

    public override void _Ready()
    {
        SpawnMarkers ??= FindSpawnMarkers();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AnimalBefriended += OnAnimalBefriended;
        }

        SpawnAvailableAnimals();
    }

    public override void _ExitTree()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AnimalBefriended -= OnAnimalBefriended;
        }
    }

    private void OnAnimalBefriended(string _animalId)
    {
        SpawnAvailableAnimals();
    }

    private void SpawnAvailableAnimals()
    {
        foreach (Node child in GetChildren())
        {
            if (child.IsInGroup("hub_spawned_animal"))
            {
                child.QueueFree();
            }
        }

        if (GameManager.Instance == null || SpawnMarkers == null || SpawnMarkers.Length == 0)
        {
            return;
        }

        List<string> animalIds = new(GameManager.Instance.BefriendedAnimals);
        animalIds.Sort();

        int spawnIndex = 0;
        foreach (string animalId in animalIds)
        {
            if (spawnIndex >= SpawnMarkers.Length)
            {
                break;
            }

            if (!AnimalPrefabs.TryGetValue(animalId, out PackedScene packedScene) || packedScene == null)
            {
                continue;
            }

            Node3D animal = packedScene.Instantiate<Node3D>();
            AddChild(animal);
            animal.GlobalPosition = SpawnMarkers[spawnIndex].GlobalPosition;
            animal.GlobalRotation = SpawnMarkers[spawnIndex].GlobalRotation;
            animal.AddToGroup("hub_spawned_animal");
            spawnIndex++;
        }
    }

    private Marker3D[] FindSpawnMarkers()
    {
        List<Marker3D> markers = new();
        foreach (Node child in GetChildren())
        {
            if (child is Marker3D marker)
            {
                markers.Add(marker);
            }
        }

        return markers.ToArray();
    }
}
