using Godot;

public partial class FixMixamoRotationsAgain : SceneTree
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
                // We know which ones we fixed previously because they now have Z < 0.5 (since we inverted it)
                // Wait, no. We fixed ones where Z > 0.5 originally.
                // Let's just fix the ones that don't match idle.
                // Idle Z is ~0.
                
                var firstKey = (Quaternion)anim.TrackGetKeyValue(hipsTrack, 0);
                // If it was already modified by rot * flip180, let's reverse it and apply flip180 * rot
                
                // Let's just see what Z is now.
                GD.Print($"{animName} current Z is {firstKey.Z}, W is {firstKey.W}");
            }
        }
        
        Quit();
    }
}
