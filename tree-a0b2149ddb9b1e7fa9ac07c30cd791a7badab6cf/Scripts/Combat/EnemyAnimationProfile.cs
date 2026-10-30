using Godot;

[GlobalClass]
public partial class EnemyAnimationProfile : Resource
{
    [ExportCategory("Clips")]
    [Export] public PackedScene IdleAnimationScene;
    [Export] public PackedScene WalkAnimationScene;
    [Export] public PackedScene RunAnimationScene;
    [Export] public PackedScene AttackAnimationScene;
    [Export] public PackedScene SecondaryAttackAnimationScene;
    [Export] public PackedScene HitAnimationScene;
    [Export] public PackedScene DeathAnimationScene;

    [ExportCategory("Attack Timing")]
    [Export] public float AttackImpactNormalized = -1.0f;
    [Export] public float AttackActiveNormalized = -1.0f;
    [Export] public float AttackRecoveryNormalized = -1.0f;
    [Export] public float SecondaryAttackImpactNormalized = -1.0f;
    [Export] public float SecondaryAttackActiveNormalized = -1.0f;
    [Export] public float SecondaryAttackRecoveryNormalized = -1.0f;

    [ExportCategory("Presentation")]
    [Export] public Vector3 VisualOffset = Vector3.Zero;
    [Export] public Vector3 VisualRotationDegrees = Vector3.Zero;
    [Export] public Vector3 VisualScale = Vector3.One;
}
