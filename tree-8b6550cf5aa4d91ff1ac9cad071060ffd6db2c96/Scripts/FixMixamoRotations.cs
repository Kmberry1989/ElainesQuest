using Godot;

public partial class FixMixamoRotations : SceneTree
{
    public override void _Initialize()
    {
        var lib = ResourceLoader.Load<AnimationLibrary>("res://Assets/Animations/MixamoLibrary.res");
        
        var flip180 = new Quaternion(Vector3.Up, Mathf.Pi);
        
        foreach (var animName in lib.GetAnimationList())
        {
            if (animName == "idle") continue;
            
            var anim = lib.GetAnimation(animName);
            int hipsTrack = -1;
            
            for (int i = 0; i < anim.GetTrackCount(); i++)
            {
                if (anim.TrackGetPath(i).ToString().Contains("mixamorig_Hips") && anim.TrackGetType(i) == Animation.TrackType.Rotation3D)
                {
                    hipsTrack = i;
                    break;
                }
            }
            
            if (hipsTrack != -1 && anim.TrackGetKeyCount(hipsTrack) > 0)
            {
                var firstKey = (Quaternion)anim.TrackGetKeyValue(hipsTrack, 0);
                if (Mathf.Abs(firstKey.Z) > 0.5f)
                {
                    GD.Print($"Fixing {animName} (Z was {firstKey.Z})");
                    for (int j = 0; j < anim.TrackGetKeyCount(hipsTrack); j++)
                    {
                        var rot = (Quaternion)anim.TrackGetKeyValue(hipsTrack, j);
                        anim.TrackSetKeyValue(hipsTrack, j, rot * flip180);
                    }
                }
            }
        }
        
        ResourceSaver.Save(lib, "res://Assets/Animations/MixamoLibrary.res");
        GD.Print("Fixed Hips rotations!");
        Quit();
    }
}
