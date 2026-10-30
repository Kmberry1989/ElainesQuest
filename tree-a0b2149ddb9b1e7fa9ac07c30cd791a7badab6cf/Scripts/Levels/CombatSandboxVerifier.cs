using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class CombatSandboxVerifier : Node3D
{
    [Export] public ElaineController Player;
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

        if (HasVerificationArgument("--verify-spells") || HasVerificationArgument("--verify-enemies"))
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
        await WaitSeconds(0.4f);
        Player.GlobalPosition = new Vector3(0.0f, 1.2f, 4.0f);
        Player.Velocity = Vector3.Zero;
        if (Player.Visuals != null)
        {
            Player.Visuals.GlobalRotation = Vector3.Zero;
        }

        await WaitSeconds(0.4f);

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

    private async Task<bool> VerifyEnemies()
    {
        bool corkrotPassed = await VerifyEnemy("Corkrot", CorkrotScene, new[] { "idle", "walk", "run", "attack", "hit", "death" }, false);
        bool fungalPassed = await VerifyEnemy("Fungal Blight", FungalBlightScene, new[] { "idle", "walk", "run", "attack", "hit", "death" }, false);
        bool bossPassed = await VerifyEnemy("RustBucketBob", RustBucketBobScene, new[] { "idle", "walk", "run", "attack", "swipe", "hit", "death" }, true);
        bool wardenPassed = await VerifyEnemy("Briar Warden", BriarWardenScene, new[] { "idle", "walk", "run", "attack", "swipe", "hit", "death" }, true);
        return corkrotPassed && fungalPassed && bossPassed && wardenPassed;
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
