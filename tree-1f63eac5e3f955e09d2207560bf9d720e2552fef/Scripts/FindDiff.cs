using Godot;

public partial class FindDiff : SceneTree
{
    public override void _Initialize()
    {
        var lib = ResourceLoader.Load<AnimationLibrary>("res://Assets/Animations/MixamoLibrary.res");
        var idle = lib.GetAnimation("idle");
        var run = lib.GetAnimation("run");
        
        int GetHips(Animation anim) {
            for (int i = 0; i < anim.GetTrackCount(); i++)
                if (anim.TrackGetPath(i).ToString().Contains("mixamorig_Hips") && anim.TrackGetType(i) == Animation.TrackType.Rotation3D)
                    return i;
            return -1;
        }
        
        var qIdle = (Quaternion)idle.TrackGetKeyValue(GetHips(idle), 0);
        var qRun = (Quaternion)run.TrackGetKeyValue(GetHips(run), 0);
        
        var qDiffRight = qRun.Inverse() * qIdle;
        var qDiffLeft = qIdle * qRun.Inverse();
        
        GD.Print($"qDiffRight (Local correction): {qDiffRight}");
        GD.Print($"qDiffLeft (Parent correction): {qDiffLeft}");
        
        // Let's get the Euler angles of qDiffRight to see what rotation it is
        var eulerRight = qDiffRight.GetEuler();
        GD.Print($"Euler Right: X:{Mathf.RadToDeg(eulerRight.X)} Y:{Mathf.RadToDeg(eulerRight.Y)} Z:{Mathf.RadToDeg(eulerRight.Z)}");
        
        var eulerLeft = qDiffLeft.GetEuler();
        GD.Print($"Euler Left: X:{Mathf.RadToDeg(eulerLeft.X)} Y:{Mathf.RadToDeg(eulerLeft.Y)} Z:{Mathf.RadToDeg(eulerLeft.Z)}");
        
        Quit();
    }
}
