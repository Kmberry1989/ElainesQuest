using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Opt-in integration check run against the actual packaged executable by CI.
public partial class DeploymentVerifier : Node
{
    public override void _Ready()
    {
        if (OS.GetCmdlineUserArgs().Contains("--verify-export"))
        {
            CallDeferred(nameof(VerifyExport));
        }
    }

    private async void VerifyExport()
    {
        try
        {
            await WaitFor(() => GetTree().Root.FindChild("HubWorld", true, false) != null, "hub startup");
            await WaitSeconds(1.0);
            Node hub = GetTree().Root.FindChild("HubWorld", true, false);
            ElaineController player = FindPlayer();
            Require(player != null && player.GetCurrentHealth() > 0, "healthy playable Elaine");
            await VerifyModels(hub);
            GD.Print("EXPORT_VERIFY: hub models loaded");
            Require(GetViewport().GetCamera3D() != null, "active camera");
            Require(Descendants(hub).OfType<WorldEnvironment>().Count() == 1, "one environment controller");
            Require(Descendants(hub).OfType<DirectionalLight3D>().Count() == 1, "one daylight sun");
            Rect2 viewport = GetViewport().GetVisibleRect();
            foreach (string name in new[] { "FamilyLabel", "ObjectiveLabel", "InteractionPromptLabel" })
            {
                Label label = hub.FindChild(name, true, false) as Label;
                Require(label != null && viewport.Encloses(label.GetGlobalRect()), "HUD fits viewport: " + name);
            }
            DialogueUI.Instance?.HideDialogue();
            await WaitFor(() => player.IsOnFloor(), "grounded Hub spawn");
            float groundY = player.GlobalPosition.Y;
            Input.ActionPress("jump");
            await WaitSeconds(0.2);
            Input.ActionRelease("jump");
            Require(player.GlobalPosition.Y > groundY + 0.5f, "visible jump clearance");
            await WaitFor(() => player.IsOnFloor(), "jump returns to visible ground");
            Require(Mathf.Abs(player.GlobalPosition.Y - groundY) < 0.1f, "stable ground height");
            Require(InputMap.HasAction("melee_attack"), "melee input available");
            Require(player.AutoAimEnabled, "autoaim enabled by default");
            AnimationPlayer animation = player.AnimTree.GetNode<AnimationPlayer>(player.AnimTree.AnimPlayer);
            Require(animation.HasAnimation("Melee/strike"), "supplied melee animation available");
            Require(player.TryMeleeAttack(), "grounded melee attack");
            await WaitSeconds(0.85);
            Vector3 start = player.GlobalPosition;
            Input.ActionPress("move_forward");
            await WaitSeconds(0.35);
            Input.ActionRelease("move_forward");
            Input.ActionRelease("jump");
            Vector3 displacement = player.GlobalPosition - start;
            displacement.Y = 0;
            Require(displacement.Length() > 0.1f, "movement input");

            LevelPortal portal = Descendants(hub).OfType<LevelPortal>()
                .FirstOrDefault(p => p.TargetScene.EndsWith("World_1_Forest.tscn", StringComparison.Ordinal));
            Require(portal != null && portal.CanInteract(player), "forest portal available");
            portal.Interact(player);
            await WaitFor(() => GetTree().CurrentScene?.SceneFilePath == "res://Scenes/Levels/World_1_Forest.tscn", "forest portal transition");
            await VerifyModels(GetTree().CurrentScene);
            GD.Print("EXPORT_VERIFY: forest models loaded");
            Require(FindPlayer() != null, "forest player");

            // Exercise the same scene-transition/arrival service used by the return portal.
            GameManager.Instance.QueueSceneArrival("HubArrival");
            await WaitSeconds(0.5);
            ElainesSceneTransition.Instance.TransitionTo("res://Scenes/Hub/HubWorld.tscn");
            await WaitFor(() => GetTree().CurrentScene?.SceneFilePath == "res://Scenes/Hub/HubWorld.tscn", "hub return transition");
            await VerifyModels(GetTree().CurrentScene);
            Require(FindPlayer() != null, "returned hub player");
            GD.Print("EXPORT_VERIFY: PASS (models, movement, jump and landing, single daylight rig, camera, autoaim default, melee input and animation, forest portal, hub return)");
            GetTree().Quit(0);
        }
        catch (Exception error)
        {
            Input.ActionRelease("move_forward");
            Input.ActionRelease("jump");
            GD.PushError("EXPORT_VERIFY: FAIL " + error.Message);
            GetTree().Quit(1);
        }
    }

    private async Task VerifyModels(Node scene)
    {
        RuntimeGltfModel[] models = Descendants(scene).OfType<RuntimeGltfModel>()
            .Where(model => !string.IsNullOrWhiteSpace(model.ModelPath)).ToArray();
        Require(models.Length >= 2, "character model loaders present");
        foreach (RuntimeGltfModel model in models)
        {
            await WaitFor(() => model.GetChildren().Any(child =>
                child.Name == "__RuntimeModel" &&
                ((child is MeshInstance3D rootMesh && rootMesh.Mesh != null) ||
                 Descendants(child).OfType<MeshInstance3D>().Any(mesh => mesh.Mesh != null))),
                "packaged model " + model.ModelPath);
        }
    }

    private ElaineController FindPlayer() => GetTree().GetFirstNodeInGroup("player") as ElaineController;

    private async Task WaitFor(Func<bool> condition, string description)
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            if (condition()) return;
            await WaitSeconds(0.1);
        }
        throw new InvalidOperationException("Timed out: " + description);
    }

    private async Task WaitSeconds(double duration) =>
        await ToSignal(GetTree().CreateTimer(duration), SceneTreeTimer.SignalName.Timeout);

    private static IEnumerable<Node> Descendants(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            yield return child;
            foreach (Node nested in Descendants(child)) yield return nested;
        }
    }

    private static void Require(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
    }
}
