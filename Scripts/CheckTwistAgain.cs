using Godot;

public partial class CheckTwistAgain : SceneTree
{
    public override void _Initialize()
    {
        var lib = ResourceLoader.Load<AnimationLibrary>("res://Assets/Animations/MixamoLibrary.res");
        
        var idle = lib.GetAnimation("idle");
        var run = lib.GetAnimation("run");
        
        int GetHipsTrack(Animation anim) {
            for (int i = 0; i < anim.GetTrackCount(); i++)
                if (anim.TrackGetPath(i).ToString().Contains("mixamorig_Hips") && anim.TrackGetType(i) == Animation.TrackType.Rotation3D)
                    return i;
            return -1;
        }
        
        var idleHips = GetHipsTrack(idle);
        var runHips = GetHipsTrack(run);
        
        GD.Print($"Idle Hips: {idle.TrackGetKeyValue(idleHips, 0)}");
        GD.Print($"Run Hips: {run.TrackGetKeyValue(runHips, 0)}");
        
        Quit();
    }
}
