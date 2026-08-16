using Godot;

public partial class EnemyAnimationShowcase : Node3D
{
    private EnemyCombatant[] _enemies = System.Array.Empty<EnemyCombatant>();

    public override void _Ready()
    {
        _enemies = new[]
        {
            GetNodeOrNull<EnemyCombatant>("Corkrot"),
            GetNodeOrNull<EnemyCombatant>("FungalBlight"),
            GetNodeOrNull<EnemyCombatant>("RustBucketBob"),
            GetNodeOrNull<EnemyCombatant>("BriarWarden")
        };

        Camera3D camera = GetNodeOrNull<Camera3D>("Camera3D");
        camera?.LookAt(new Vector3(0.0f, 1.2f, 0.0f), Vector3.Up);
        BuildControls();
        CallDeferred(nameof(SelectState), "Idle");
        if (HasVerificationArgument())
        {
            CallDeferred(nameof(VerifyShowcase));
        }
    }

    private void BuildControls()
    {
        CanvasLayer layer = new() { Name = "Controls" };
        AddChild(layer);

        PanelContainer panel = new() { Name = "AnimationPanel" };
        panel.Position = new Vector2(24.0f, 24.0f);
        panel.Size = new Vector2(280.0f, 0.0f);
        layer.AddChild(panel);

        VBoxContainer content = new();
        content.AddThemeConstantOverride("separation", 8);
        panel.AddChild(content);
        content.AddChild(new Label { Text = "Enemy animation showcase" });
        content.AddChild(new Label { Text = "Drive all four enemies" });

        foreach (string state in new[] { "Idle", "Walk", "Run", "Attack", "Secondary", "Hit", "Death" })
        {
            Button button = new() { Text = state, CustomMinimumSize = new Vector2(240.0f, 34.0f) };
            button.Pressed += () => SelectState(state);
            content.AddChild(button);
        }
    }

    private void SelectState(string state)
    {
        foreach (EnemyCombatant enemy in _enemies)
        {
            if (!IsInstanceValid(enemy))
            {
                continue;
            }

            string stateName = state switch
            {
                "Walk" => enemy.WalkStateName,
                "Run" => enemy.RunStateName,
                "Attack" => enemy.AttackStateName,
                "Secondary" => enemy.SecondaryAttackStateName,
                "Hit" => enemy.HitStateName,
                "Death" => enemy.DeathStateName,
                _ => enemy.IdleStateName
            };
            enemy.DebugTravelAnimationState(stateName);
        }
    }

    private async void VerifyShowcase()
    {
        await ToSignal(GetTree().CreateTimer(0.35f), SceneTreeTimer.SignalName.Timeout);
        bool ready = true;
        foreach (EnemyCombatant enemy in _enemies)
        {
            ready &= IsInstanceValid(enemy) && enemy.IsAnimationReady;
        }

        GD.Print($"SHOWCASE_VERIFY: {(ready ? "PASS" : "FAIL")} (enemies {_enemies.Length}, animationReady {ready})");
        GetTree().Quit(ready ? 0 : 1);
    }

    private static bool HasVerificationArgument()
    {
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (argument == "--verify-enemy-showcase")
            {
                return true;
            }
        }

        return false;
    }
}
