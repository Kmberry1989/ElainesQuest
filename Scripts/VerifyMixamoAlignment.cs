using Godot;
using System;
using System.Collections.Generic;

public partial class VerifyMixamoAlignment : SceneTree
{
    private const float MinAcceptableDot = 0.90f;

    public override void _Initialize()
    {
        var lib = ResourceLoader.Load<AnimationLibrary>("res://Assets/Animations/MixamoLibrary.res");
        if (lib == null)
        {
            GD.PushError("Failed to load MixamoLibrary.res");
            Quit(1);
            return;
        }

        var idle = lib.GetAnimation("idle");
        if (idle == null)
        {
            GD.PushError("Missing idle animation in MixamoLibrary.res");
            Quit(1);
            return;
        }

        int idleHipsTrack = FindRotationTrack(idle, "mixamorig_Hips");
        if (idleHipsTrack == -1)
        {
            GD.PushError("Idle animation is missing a hips rotation track.");
            Quit(1);
            return;
        }

        var idleHips = (Quaternion)idle.TrackGetKeyValue(idleHipsTrack, 0);
        var failures = new List<string>();

        foreach (string name in new[] { "run", "walk", "jump", "flying" })
        {
            var anim = lib.GetAnimation(name);
            if (anim == null)
            {
                failures.Add($"{name}: missing from library");
                continue;
            }

            int hipsTrack = FindRotationTrack(anim, "mixamorig_Hips");
            if (hipsTrack == -1)
            {
                failures.Add($"{name}: missing hips rotation track");
                continue;
            }

            var hips = (Quaternion)anim.TrackGetKeyValue(hipsTrack, 0);
            float similarity = Mathf.Abs(idleHips.Dot(hips));
            GD.Print($"{name}: hips similarity to idle = {similarity:0.000000}");

            if (similarity < MinAcceptableDot)
            {
                failures.Add($"{name}: hips similarity {similarity:0.000000} < {MinAcceptableDot:0.00}");
            }
        }

        foreach (string loopingName in new[] { "idle", "walk", "run", "flying" })
        {
            var anim = lib.GetAnimation(loopingName);
            if (anim == null)
            {
                failures.Add($"{loopingName}: missing from library");
                continue;
            }

            if (anim.LoopMode == Animation.LoopModeEnum.None)
            {
                failures.Add($"{loopingName}: loop mode is not enabled");
            }
        }

        if (failures.Count > 0)
        {
            foreach (var failure in failures)
            {
                GD.PushError(failure);
            }

            Quit(1);
            return;
        }

        GD.Print("MixamoLibrary alignment verification passed.");
        Quit();
    }

    private static int FindRotationTrack(Animation animation, string boneName)
    {
        for (int i = 0; i < animation.GetTrackCount(); i++)
        {
            if (animation.TrackGetType(i) != Animation.TrackType.Rotation3D)
            {
                continue;
            }

            if (animation.TrackGetPath(i).ToString().Contains(boneName, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }
}
