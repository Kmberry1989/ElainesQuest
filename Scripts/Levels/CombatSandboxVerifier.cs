using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class CombatSandboxVerifier : Node3D
{
    [Export] public ElaineController Player;
    [Export] public PackedScene JackoScene;
    [Export] public PackedScene CorkrotScene;
    [Export] public PackedScene FungalBlightScene;
    [Export] public PackedScene RustBucketBobScene;
    [Export] public PackedScene BriarWardenScene;

    private readonly List<Node3D> _stageTargets = new();
    private int _expectedSpellIndex = -1;
    private int _releasedProjectileCount = -1;
    private int _hitCount;

    public override void _Ready()
    {
        Player ??= GetNodeOrNull<ElaineController>("Elaine");
        if (Player == null)
        {
            GD.PushError("Combat sandbox verifier could not find Elaine.");
            return;
        }

        if (HasVerificationArgument("--verify-spells") || HasVerificationArgument("--verify-enemies") || HasVerificationArgument("--verify-controls") || HasVerificationArgument("--verify-attacks"))
        {
            Player.SpellProjectilesReleased += OnSpellProjectilesReleased;
            Player.SpellHit += OnSpellHit;
            CallDeferred(nameof(RunVerification));
            return;
        }

        CreateManualRangeTargets();
    }

    public override void _ExitTree()
    {
        if (Player != null && IsInstanceValid(Player))
        {
            Player.SpellProjectilesReleased -= OnSpellProjectilesReleased;
            Player.SpellHit -= OnSpellHit;
        }
    }

    private async void RunVerification()
    {
        // Keep the manual-range enemy from intercepting deterministic target tests.
        GetParent().GetNodeOrNull("Jacko")?.QueueFree();
        await WaitSeconds(0.4f);
        Player.GlobalPosition = new Vector3(0.0f, 1.2f, 4.0f);
        Player.Velocity = Vector3.Zero;
        if (Player.Visuals != null)
        {
            Player.Visuals.GlobalRotation = Vector3.Zero;
        }

        await WaitSeconds(0.4f);

        if (HasVerificationArgument("--verify-attacks"))
        {
            await VerifyAttacks();
            return;
        }
        if (HasVerificationArgument("--verify-controls"))
        {
            await VerifyControls();
            return;
        }

        bool passed;
        if (HasVerificationArgument("--verify-enemies"))
        {
            passed = await VerifyEnemies();
        }
        else
        {
            bool sunEmberPassed = await VerifySunEmber();
            bool starVolleyPassed = await VerifyStarVolley();
            bool skyLancePassed = await VerifySkyLance();
            passed = sunEmberPassed && starVolleyPassed && skyLancePassed;
        }

        if (passed)
        {
            GD.Print(HasVerificationArgument("--verify-enemies")
                ? "ENEMY_VERIFY: PASS — animation contracts, state travel, spell damage, and defeat cleanup succeeded."
                : "SPELL_VERIFY: PASS — Sun Ember, Star Volley, and Sky Lance contracts succeeded.");
        }
        else
        {
            GD.PushError(HasVerificationArgument("--verify-enemies")
                ? "ENEMY_VERIFY: FAIL — see individual enemy results above."
                : "SPELL_VERIFY: FAIL — see individual spell results above.");
        }

        GetTree().Quit(passed ? 0 : 1);
    }

    private async Task VerifyAttacks()
    {
        bool passed = true;
        void Check(bool condition, string description)
        {
            passed &= condition;
            GD.Print($"ATTACK_VERIFY: {(condition ? "PASS" : "FAIL")} {description}");
        }
        GetTree().CurrentScene.GetNodeOrNull("Jacko")?.QueueFree();
        await WaitSeconds(0.1f);
        Vector3 home = new(0, 0.1f, 4);
        Player.AutoAimEnabled = true;
        for (int spell = 0; spell < 3; spell++)
        {
            ClearStageTargets();
            Player.RespawnTo(home);
            Player.Visuals.GlobalRotation = Vector3.Zero;
            Player.SelectSpell(spell);
            Vector3 position = spell switch {
                1 => new Vector3(0, 1.25f, 10), // Behind Elaine.
                2 => new Vector3(5, 3.5f, 4), // Above the casting plane.
                _ => new Vector3(6, 1.25f, 4), // Ninety degrees to the side.
            };
            CombatHealth target = CreateTarget("Autoaim", position, 10, false);
            await WaitSeconds(0.2f);
            bool accepted = Player.TryCastSelectedSpell();
            await WaitSeconds(1.0f);
            Check(accepted && target.CurrentHealth < 10, $"spell {spell} autoaim hits side/behind/elevated target");
        }

        ClearStageTargets();
        Player.RespawnTo(home);
        Player.SelectSpell(0);
        Player.Visuals.GlobalRotation = Vector3.Zero;
        CombatHealth hidden = CreateTarget("Hidden", new Vector3(0, 1.25f, 2), 10, false);
        ((Node3D)hidden.GetParent()).Visible = false;
        hidden.GetParent().ProcessMode = ProcessModeEnum.Disabled;
        CombatHealth blocked = CreateTarget("BehindWall", new Vector3(5, 1.25f, 4), 10, false);
        CombatHealth clear = CreateTarget("Clear", new Vector3(-5, 1.25f, 4), 10, false);
        StaticBody3D wall = new();
        wall.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(0.2f, 4, 4) } });
        GetTree().CurrentScene.AddChild(wall);
        wall.GlobalPosition = new Vector3(2.5f, 2, 4);
        await WaitSeconds(0.2f);
        Player.TryCastSelectedSpell();
        await WaitSeconds(1);
        Check(clear.CurrentHealth < 10 && blocked.CurrentHealth == 10 && hidden.CurrentHealth == 10,
            "autoaim ignores inactive targets and walls");
        wall.QueueFree();

        ClearStageTargets();
        Player.RespawnTo(home);
        CombatHealth doomed = CreateTarget("Doomed", new Vector3(3, 1.25f, 4), 10, false);
        CombatHealth replacement = CreateTarget("Replacement", new Vector3(6, 1.25f, 4), 10, false);
        await WaitSeconds(0.2f);
        Player.TryCastSelectedSpell();
        doomed.ApplyDamage(999);
        await WaitSeconds(1);
        Check(replacement.CurrentHealth < 10, "dead target is reacquired before release");

        ClearStageTargets();
        Player.RespawnTo(home);
        Player.Visuals.GlobalRotation = Vector3.Zero;
        CombatHealth near = CreateTarget("Near", new Vector3(2, 1.1f, 4), 10, false);
        CombatHealth alongside = CreateTarget("Alongside", new Vector3(2, 1.1f, 4.5f), 10, false);
        CombatHealth rear = CreateTarget("Rear", new Vector3(-2, 1.1f, 4), 10, false);
        CombatHealth far = CreateTarget("Far", new Vector3(5, 1.1f, 4), 10, false);
        var duplicate = new CombatHurtbox { Health = near };
        near.GetParent().AddChild(duplicate);
        duplicate.AddChild(new CollisionShape3D { Shape = new SphereShape3D { Radius = 0.4f } });
        await WaitSeconds(0.2f);
        AnimationPlayer animation = Player.AnimTree.GetNode<AnimationPlayer>(Player.AnimTree.AnimPlayer);
        Check(animation.HasAnimation("Melee/strike"), "supplied melee animation installed");
        Skeleton3D skeleton = Player.Visuals.FindChild("Skeleton3D", true, false) as Skeleton3D;
        Transform3D[] beforePose = CapturePose(skeleton);
        bool meleeAccepted = Player.TryMeleeAttack();
        Check(meleeAccepted && !Player.TryMeleeAttack() && !Player.TryCastSelectedSpell(), "melee cooldown and action exclusivity");
        await WaitSeconds(0.1f);
        Check(Player.AnimTree.Get("parameters/playback").As<AnimationNodeStateMachinePlayback>().GetCurrentNode() == "Melee", "melee animation state plays");
        Check(EnemyCombatant.MeasureAnimationPoseDelta(beforePose, CapturePose(skeleton)) > 0.001f, "melee animates the actual character bones");
        await WaitSeconds(0.17f);
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (!argument.StartsWith("--attack-capture=", System.StringComparison.Ordinal)) continue;
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            GetViewport().GetTexture().GetImage().SavePng(argument.Substring("--attack-capture=".Length));
        }
        await WaitSeconds(0.63f);
        Check(Mathf.IsEqualApprox(near.CurrentHealth, 10 - Player.MeleeDamage) &&
            Mathf.IsEqualApprox(alongside.CurrentHealth, 10 - Player.MeleeDamage) &&
            rear.CurrentHealth == 10 && far.CurrentHealth == 10, "melee arc, autoaim, range and one hit per health component");

        Player.RespawnTo(home);
        await WaitSeconds(0.1f);
        float before = near.CurrentHealth;
        Player.TryMeleeAttack();
        Player.RespawnTo(home);
        await WaitSeconds(0.4f);
        Check(near.CurrentHealth == before, "respawn cancels pending melee damage");
        wall = new StaticBody3D();
        wall.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(0.1f, 4, 6) } });
        GetTree().CurrentScene.AddChild(wall);
        wall.GlobalPosition = new Vector3(1, 2, 4);
        Player.Visuals.GlobalRotation = new Vector3(0, -Mathf.Pi / 2, 0);
        Player.AutoAimEnabled = false;
        await WaitSeconds(0.2f);
        Player.TryMeleeAttack();
        await WaitSeconds(0.8f);
        Check(near.CurrentHealth == before, "melee cannot hit through walls");
        wall.QueueFree();
        ClearStageTargets();
        Player.RespawnTo(home);
        await WaitSeconds(0.2f);
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.R, Pressed = true });
        await WaitSeconds(0.1f);
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.R, Pressed = false });
        Check(Player.GetMeleeCooldownRemaining() > 0, "R key starts melee through real input dispatch");
        Player.RespawnTo(home);
        await WaitSeconds(0.2f);
        Input.ParseInputEvent(new InputEventJoypadMotion { Device = 3, Axis = JoyAxis.TriggerRight, AxisValue = 1 });
        await WaitSeconds(0.1f);
        Input.ParseInputEvent(new InputEventJoypadMotion { Device = 3, Axis = JoyAxis.TriggerRight, AxisValue = 0 });
        Check(Player.GetMeleeCooldownRemaining() > 0, "right trigger works on a nonzero controller slot");
        await WaitSeconds(1.0f);
        GD.Print($"ATTACK_VERIFY: {(passed ? "PASS" : "FAIL")} — autoaim and melee contracts");
        GetTree().Quit(passed ? 0 : 1);
    }

    private static Transform3D[] CapturePose(Skeleton3D skeleton)
    {
        if (skeleton == null) return System.Array.Empty<Transform3D>();
        var result = new Transform3D[skeleton.GetBoneCount()];
        for (int i = 0; i < result.Length; i++) result[i] = skeleton.GetBonePose(i);
        return result;
    }

    private async Task VerifyControls()
    {
        bool passed = true;
        void Check(bool condition, string description)
        {
            passed &= condition;
            GD.Print($"CONTROLS_VERIFY: {(condition ? "PASS" : "FAIL")} {description}");
        }

        // Test real input and physics instead of directly setting the facing angle.
        foreach (var move in new[] {
            ("move_forward", Vector3.Forward), ("move_backward", Vector3.Back),
            ("move_left", Vector3.Left), ("move_right", Vector3.Right) })
        {
            Player.RespawnTo(new Vector3(0, 0.1f, 4));
            await WaitSeconds(0.2f);
            Vector3 start = Player.GlobalPosition;
            Input.ActionPress(move.Item1);
            await WaitSeconds(0.45f);
            Input.ActionRelease(move.Item1);
            Check((Player.GlobalPosition - start).Dot(move.Item2) > 0.5f, move.Item1 + " moves correctly");
            Check((-Player.Visuals.GlobalBasis.Z).Dot(move.Item2) > 0.98f, move.Item1 + " faces and casts along movement");
            Check((Player.SpellOrigin.GlobalPosition - Player.GlobalPosition).Dot(move.Item2) > 0.8f, "spell origin stays ahead");
            await WaitSeconds(0.4f);
        }

        Player.RespawnTo(new Vector3(0, 0.1f, 4));
        await WaitSeconds(0.2f);
        Input.ActionPress("spell_wheel");
        Input.ActionPress("jump");
        Input.ActionPress("move_right");
        bool wheelCastRejected = !Player.TryCastSelectedSpell() && !Player.TryMeleeAttack();
        await WaitSeconds(0.15f);
        Check(wheelCastRejected && Player.IsOnFloor() && Player.Velocity.Length() < 0.1f, "wheel blocks casting, jumping and walking");
        Input.ActionRelease("spell_wheel");
        Input.ActionRelease("jump");
        Input.ActionRelease("move_right");

        DialogueUI dialogue = GD.Load<PackedScene>("res://Scenes/UI/DialogueUI.tscn").Instantiate<DialogueUI>();
        GetTree().CurrentScene.AddChild(dialogue);
        dialogue.ShowDialogue("Audit", "Control isolation");
        Input.ActionPress("jump");
        Input.ActionPress("move_right");
        bool dialogueCastRejected = !Player.TryCastSelectedSpell() && !Player.TryMeleeAttack();
        await WaitSeconds(0.15f);
        Check(dialogueCastRejected && Player.IsOnFloor() && Player.Velocity.Length() < 0.1f, "dialogue blocks casting, jumping and walking");
        Input.ActionRelease("jump");
        Input.ActionRelease("move_right");
        dialogue.HideDialogue();
        dialogue.QueueFree();

        // A buffered input from an interrupted cast must not fire after respawn.
        Check(Player.TryCastSelectedSpell(), "grounded cast accepted");
        Player._UnhandledInput(new InputEventAction { Action = "cast_magic", Pressed = true });
        Player.RespawnTo(new Vector3(0, 0.1f, 4));
        await WaitSeconds(0.1f);
        Check(Player.GetSpellCooldownRemaining() == 0, "respawn clears buffered casting");
        Check(!Player.SelectSpell(-1) && !Player.SelectSpell(Player.GetSpellCount()), "invalid spell selection rejected");
        StaticBody3D wall = new();
        wall.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(0.02f, 4, 4) } });
        GetTree().CurrentScene.AddChild(wall);
        wall.GlobalPosition = new Vector3(0, 2, -4);
        await WaitSeconds(0.1f);
        SpellProjectile fastSpell = new();
        fastSpell.AddChild(new CollisionShape3D { Shape = new SphereShape3D { Radius = 0.05f } });
        GetTree().CurrentScene.AddChild(fastSpell);
        fastSpell.GlobalPosition = new Vector3(-1.1f, 2, -4);
        fastSpell.Launch(Player, Vector3.Right, 1, 120, 100);
        await WaitSeconds(0.1f);
        Check(!IsInstanceValid(fastSpell) || fastSpell.IsQueuedForDeletion(), "fast projectile cannot tunnel through thin wall");
        if (IsInstanceValid(fastSpell)) fastSpell.QueueFree();
        wall.QueueFree();
        GD.Print($"CONTROLS_VERIFY: {(passed ? "PASS" : "FAIL")} — movement, facing, modal controls, respawn buffer, projectile sweep");
        GetTree().Quit(passed ? 0 : 1);
    }

    private async Task<bool> VerifyEnemies()
    {
        bool jackoPassed = await VerifyEnemy("Jacko", JackoScene, new[] { "idle", "walk", "run", "attack", "swipe", "hit", "death" }, true);
        bool corkrotPassed = await VerifyEnemy("Corkrot", CorkrotScene, new[] { "idle", "walk", "run", "attack", "hit", "death" }, false);
        bool fungalPassed = await VerifyEnemy("Fungal Blight", FungalBlightScene, new[] { "idle", "walk", "run", "attack", "hit", "death" }, false);
        bool bossPassed = await VerifyEnemy("RustBucketBob", RustBucketBobScene, new[] { "idle", "walk", "run", "attack", "swipe", "hit", "death" }, true);
        bool wardenPassed = await VerifyEnemy("Briar Warden", BriarWardenScene, new[] { "idle", "walk", "run", "attack", "swipe", "hit", "death" }, true);
        return jackoPassed && corkrotPassed && fungalPassed && bossPassed && wardenPassed;
    }

    private async Task<bool> VerifyEnemy(string displayName, PackedScene scene, IReadOnlyList<string> requiredClips, bool expectsSecondaryAttack)
    {
        if (scene == null)
        {
            GD.PushError($"ENEMY_VERIFY: {displayName} FAIL (scene is not assigned)");
            return false;
        }

        EnemyCombatant enemy = scene.Instantiate<EnemyCombatant>();
        AddChild(enemy);
        if (Player.Visuals != null)
        {
            Player.Visuals.GlobalRotation = Vector3.Zero;
        }

        Vector3 spellDirection = -(Player.Visuals?.GlobalBasis.Z ?? Player.GlobalBasis.Z);
        spellDirection.Y = 0.0f;
        spellDirection = spellDirection.Normalized();
        Vector3 spellOrigin = Player.SpellOrigin?.GlobalPosition ?? Player.GlobalPosition + Vector3.Up * 1.2f;
        enemy.GlobalPosition = spellOrigin - Vector3.Up + spellDirection * 6.0f;
        await WaitSeconds(0.25f);

        bool clipsPresent = enemy.IsAnimationReady;
        bool clipsResolved = enemy.IsAnimationReady;
        foreach (string clip in requiredClips)
        {
            clipsPresent &= enemy.HasAnimationClip(clip);
            clipsResolved &= enemy.HasResolvedAnimationClip(clip);
        }
        if (!clipsResolved)
        {
            GD.Print($"ENEMY_VERIFY: {displayName} binding {enemy.GetAnimationBindingSummary()}");
        }

        bool statesTravel = enemy.DebugTravelAnimationState(enemy.IdleStateName) &&
            enemy.DebugTravelAnimationState(enemy.WalkStateName) &&
            enemy.DebugTravelAnimationState(enemy.RunStateName) &&
            enemy.DebugTravelAnimationState(enemy.AttackStateName) &&
            enemy.DebugTravelAnimationState(enemy.HitStateName) &&
            enemy.DebugTravelAnimationState(enemy.DeathStateName);
        if (expectsSecondaryAttack)
        {
            statesTravel &= enemy.DebugTravelAnimationState(enemy.SecondaryAttackStateName);
        }

        bool poseMotion = await VerifyPoseMotion(enemy, new[]
        {
            enemy.IdleStateName,
            enemy.WalkStateName,
            enemy.AttackStateName,
            enemy.HitStateName,
            enemy.DeathStateName
        });

        float healthBefore = enemy.Health?.CurrentHealth ?? 0.0f;
        bool spellDamage = enemy.Hurtbox?.ApplySpellHit(1.0f) ?? false;
        await WaitSeconds(0.1f);
        spellDamage &= enemy.Health != null && enemy.Health.CurrentHealth < healthBefore;

        enemy.Health?.ApplyDamage(999.0f);
        await WaitSeconds(enemy.DeathDelay + 0.2f);
        bool cleanedUp = !IsInstanceValid(enemy) || enemy.IsQueuedForDeletion();
        bool passed = clipsPresent && clipsResolved && statesTravel && poseMotion && spellDamage && cleanedUp;
        GD.Print($"ENEMY_VERIFY: {displayName} {(passed ? "PASS" : "FAIL")} (clips {clipsPresent}, resolved {clipsResolved}, states {statesTravel}, pose {poseMotion}, spell {spellDamage}, cleanup {cleanedUp})");
        if (!cleanedUp && IsInstanceValid(enemy))
        {
            enemy.QueueFree();
        }

        await WaitSeconds(0.1f);
        return passed;
    }

    private async Task<bool> VerifyPoseMotion(EnemyCombatant enemy, IReadOnlyList<string> states)
    {
        bool passed = true;
        foreach (string state in states)
        {
            bool stateReady = enemy.DebugTravelAnimationState(state);
            await WaitSeconds(0.08f);
            Transform3D[] before = enemy.CaptureAnimationPose();
            await WaitSeconds(0.16f);
            Transform3D[] after = enemy.CaptureAnimationPose();
            float delta = EnemyCombatant.MeasureAnimationPoseDelta(before, after);
            bool moved = stateReady && before.Length > 0 && delta > 0.0005f;
            passed &= moved;
            GD.Print($"ENEMY_VERIFY: {enemy.Name} pose {state} {(moved ? "PASS" : "FAIL")} (delta {delta:0.0000})");
        }

        return passed;
    }

    private async Task<bool> VerifySunEmber()
    {
        ClearStageTargets();
        await WaitSeconds(0.1f);

        Vector3 origin = GetSpellOrigin();
        Vector3 direction = GetFacingDirection();
        CombatHealth target = CreateTarget("Sun Ember Target", origin + direction * 6.0f, 3.0f, false);
        return await CastAndAssert("Sun Ember", 0, 1, new[] { target }, 1, 1);
    }

    private async Task<bool> VerifyStarVolley()
    {
        ClearStageTargets();
        await WaitSeconds(0.1f);

        Vector3 origin = GetSpellOrigin();
        Vector3 direction = GetFacingDirection();
        CombatHealth left = CreateTarget("Star Volley Left", origin + direction.Rotated(Vector3.Up, Mathf.DegToRad(-18.0f)) * 6.0f, 3.0f, false);
        CombatHealth center = CreateTarget("Star Volley Center", origin + direction * 6.0f, 3.0f, false);
        CombatHealth right = CreateTarget("Star Volley Right", origin + direction.Rotated(Vector3.Up, Mathf.DegToRad(18.0f)) * 6.0f, 3.0f, false);
        return await CastAndAssert("Star Volley", 1, 3, new[] { left, center, right }, 3, 3);
    }

    private async Task<bool> VerifySkyLance()
    {
        ClearStageTargets();
        await WaitSeconds(0.1f);

        Vector3 origin = GetSpellOrigin();
        Vector3 direction = GetFacingDirection();
        CombatHealth first = CreateTarget("Sky Lance First", origin + direction * 5.0f, 3.0f, false);
        var duplicateHurtbox = new CombatHurtbox { Health = first };
        first.GetParent().AddChild(duplicateHurtbox);
        duplicateHurtbox.AddChild(new CollisionShape3D { Shape = new SphereShape3D { Radius = 0.5f } });
        CombatHealth second = CreateTarget("Sky Lance Second", origin + direction * 9.0f, 3.0f, false);
        return await CastAndAssert("Sky Lance", 2, 1, new[] { first, second }, 2, 2);
    }

    private async Task<bool> CastAndAssert(
        string spellName,
        int spellIndex,
        int expectedProjectiles,
        IReadOnlyList<CombatHealth> targets,
        int expectedDamagedTargets,
        int expectedHits)
    {
        _expectedSpellIndex = spellIndex;
        _releasedProjectileCount = -1;
        _hitCount = 0;
        Player.SelectSpell(spellIndex);

        bool castAccepted = await WaitForCastAcceptance();
        bool released = castAccepted && await WaitForProjectileRelease();
        await WaitSeconds(0.75f);

        int damagedTargets = 0;
        foreach (CombatHealth target in targets)
        {
            if (target != null && target.CurrentHealth < target.MaxHealth)
            {
                damagedTargets++;
            }
        }

        bool passed = released &&
            _releasedProjectileCount == expectedProjectiles &&
            damagedTargets == expectedDamagedTargets &&
            _hitCount == expectedHits;

        string result = passed ? "PASS" : "FAIL";
        GD.Print($"SPELL_VERIFY: {spellName} {result} (released {_releasedProjectileCount}/{expectedProjectiles}, damaged {damagedTargets}/{expectedDamagedTargets}, hits {_hitCount}/{expectedHits})");
        return passed;
    }

    private async Task<bool> WaitForCastAcceptance()
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            if (Player.TryCastSelectedSpell())
            {
                return true;
            }

            await WaitSeconds(0.05f);
        }

        return false;
    }

    private async Task<bool> WaitForProjectileRelease()
    {
        for (int attempt = 0; attempt < 120; attempt++)
        {
            if (_releasedProjectileCount >= 0)
            {
                return true;
            }

            await WaitSeconds(0.05f);
        }

        return false;
    }

    private void OnSpellProjectilesReleased(int spellIndex, int projectileCount, string _displayName)
    {
        if (spellIndex == _expectedSpellIndex)
        {
            _releasedProjectileCount = projectileCount;
        }
    }

    private void OnSpellHit(int spellIndex, int _hitNumber, int _maxTargetHits, string _displayName)
    {
        if (spellIndex == _expectedSpellIndex)
        {
            _hitCount++;
        }
    }

    private void CreateManualRangeTargets()
    {
        Vector3 origin = GetSpellOrigin();
        Vector3 direction = GetFacingDirection();
        CreateTarget("Sun Ember", origin + direction * 5.5f, 6.0f, true);
        CreateTarget("Volley Left", origin + direction.Rotated(Vector3.Up, Mathf.DegToRad(-18.0f)) * 7.0f, 6.0f, true);
        CreateTarget("Volley Right", origin + direction.Rotated(Vector3.Up, Mathf.DegToRad(18.0f)) * 7.0f, 6.0f, true);
        CreateTarget("Lance Back", origin + direction * 10.0f, 6.0f, true);
    }

    private CombatHealth CreateTarget(string targetName, Vector3 position, float maxHealth, bool showLabel)
    {
        Node3D target = new()
        {
            Name = targetName,
            Position = position
        };
        AddChild(target);
        _stageTargets.Add(target);

        StandardMaterial3D material = new()
        {
            AlbedoColor = new Color(0.28f, 0.88f, 1.0f, 1.0f),
            EmissionEnabled = true,
            Emission = new Color(0.18f, 0.72f, 1.0f, 1.0f),
            EmissionEnergyMultiplier = 1.2f
        };
        target.AddChild(new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.6f, Height = 1.2f },
            MaterialOverride = material
        });

        if (showLabel)
        {
            target.AddChild(new Label3D
            {
                Text = targetName,
                Position = Vector3.Up * 1.0f,
                FontSize = 40,
                OutlineSize = 6
            });
        }

        CombatHealth health = new()
        {
            Name = "Health",
            MaxHealth = maxHealth
        };
        target.AddChild(health);

        CombatHurtbox hurtbox = new()
        {
            Name = "Hurtbox",
            Health = health,
            ImpactAnchor = target
        };
        target.AddChild(hurtbox);
        hurtbox.AddChild(new CollisionShape3D
        {
            Shape = new SphereShape3D { Radius = 0.78f }
        });

        return health;
    }

    private void ClearStageTargets()
    {
        foreach (Node3D target in _stageTargets)
        {
            if (target != null && !target.IsQueuedForDeletion())
            {
                target.QueueFree();
            }
        }

        _stageTargets.Clear();
    }

    private Vector3 GetSpellOrigin()
    {
        return Player.SpellOrigin?.GlobalPosition ?? Player.GlobalPosition + Vector3.Up * 1.2f + GetFacingDirection() * 0.9f;
    }

    private Vector3 GetFacingDirection()
    {
        Vector3 direction = -(Player.Visuals?.GlobalBasis.Z ?? Player.GlobalBasis.Z);
        direction.Y = 0.0f;
        return direction.LengthSquared() > 0.001f ? direction.Normalized() : -Vector3.Forward;
    }

    private bool HasVerificationArgument(string expectedArgument)
    {
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (argument == expectedArgument)
            {
                return true;
            }
        }

        return false;
    }

    private async Task WaitSeconds(float seconds)
    {
        await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }
}
