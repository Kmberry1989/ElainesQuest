using Godot;
using System;
using System.Collections.Generic;

public partial class RebuildLibrary : SceneTree
{
    private readonly record struct ClipSource(string LibraryName, string ResourcePath);

    public override void _Initialize()
    {
        var lib = new AnimationLibrary();

        foreach (var clip in GetClipSources())
        {
            var scene = ResourceLoader.Load<PackedScene>(clip.ResourcePath);
            if (scene == null)
            {
                GD.PushError($"Failed to load clip scene: {clip.ResourcePath}");
                continue;
            }

            var node = scene.Instantiate();
            var animPlayer = node.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
            if (animPlayer == null)
            {
                GD.PushError($"Missing AnimationPlayer in {clip.ResourcePath}");
                node.QueueFree();
                continue;
            }

            string animationName = ResolveAnimationName(animPlayer);
            var anim = animPlayer.GetAnimation(animationName);
            if (anim == null)
            {
                GD.PushError($"Missing animation {animationName} in {clip.ResourcePath}");
                node.QueueFree();
                continue;
            }

            var rebuilt = (Animation)anim.Duplicate();
            NormalizeTrackPaths(rebuilt);
            ApplyLoopMode(clip.LibraryName, rebuilt);
            lib.AddAnimation(clip.LibraryName, rebuilt);
            GD.Print($"Added {clip.LibraryName} from {clip.ResourcePath}");
            node.QueueFree();
        }

        ResourceSaver.Save(lib, "res://Assets/Animations/MixamoLibrary.res");
        GD.Print("Rebuilt MixamoLibrary.res!");
        Quit();
    }

    private static IEnumerable<ClipSource> GetClipSources()
    {
        return new[]
        {
            new ClipSource("idle", "res://Assets/Animations/Mixamo/Idle.fbx"),
            new ClipSource("run", "res://Assets/Animations/Mixamo/Running.fbx"),
            new ClipSource("walk", "res://Assets/Animations/Mixamo/Walk.fbx"),
            new ClipSource("jump", "res://Assets/Animations/Mixamo/Jump.fbx"),
            new ClipSource("flying", "res://Assets/Animations/Mixamo/Floating (1).fbx"),
            new ClipSource("talk", "res://Assets/Animations/Mixamo/Talking.fbx"),
            new ClipSource("cast", "res://Assets/Animations/Mixamo/Magic Spell Casting.fbx"),
            new ClipSource("cast_burst", "res://Assets/Animations/Mixamo/Magic From Hands.fbx"),
            new ClipSource("cast_lance", "res://Assets/Animations/Mixamo/Standing 2H Cast Spell 01.fbx"),
            new ClipSource("wave", "res://Assets/Animations/Mixamo/Waving.fbx"),
        };
    }

    private static string ResolveAnimationName(AnimationPlayer player)
    {
        if (player.HasAnimation("mixamo_com"))
        {
            return "mixamo_com";
        }

        if (player.HasAnimation("mixamo.com"))
        {
            return "mixamo.com";
        }

        return player.GetAnimationList()[0];
    }

    private static void NormalizeTrackPaths(Animation animation)
    {
        for (int i = 0; i < animation.GetTrackCount(); i++)
        {
            string path = animation.TrackGetPath(i).ToString();

            int skeletonIndex = path.IndexOf("Skeleton3D", StringComparison.Ordinal);
            if (skeletonIndex == -1)
            {
                continue;
            }

            string trimmed = path.Substring(skeletonIndex);
            if (!trimmed.StartsWith("RootNode/", StringComparison.Ordinal))
            {
                trimmed = $"RootNode/{trimmed}";
            }

            animation.TrackSetPath(i, new NodePath(trimmed));
        }
    }

    private static void ApplyLoopMode(string clipName, Animation animation)
    {
        switch (clipName)
        {
            case "idle":
            case "walk":
            case "run":
            case "flying":
                animation.LoopMode = Animation.LoopModeEnum.Linear;
                break;
            default:
                animation.LoopMode = Animation.LoopModeEnum.None;
                break;
        }
    }
}
