using Godot;

public partial class FindBestFlip : SceneTree
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
        
        var flips = new[] {
            new { name = "flipX", q = new Quaternion(Vector3.Right, Mathf.Pi) },
            new { name = "flipY", q = new Quaternion(Vector3.Up, Mathf.Pi) },
            new { name = "flipZ", q = new Quaternion(Vector3.Forward, Mathf.Pi) },
        };
        
        GD.Print($"Idle: {qIdle}");
        GD.Print($"Run: {qRun}");
        
        foreach (var flip in flips) {
            var right = qRun * flip.q;
            var left = flip.q * qRun;
            
            GD.Print($"{flip.name} Right: {right} (Dot: {Mathf.Abs(qIdle.Dot(right))})");
            GD.Print($"{flip.name} Left:  {left} (Dot: {Mathf.Abs(qIdle.Dot(left))})");
        }
        
        Quit();
    }
}
