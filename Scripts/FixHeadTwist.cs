using Godot;

public partial class FixHeadTwist : SceneTree
{
    public override void _Initialize()
    {
        var lib = ResourceLoader.Load<AnimationLibrary>("res://Assets/Animations/MixamoLibrary.res");
        
        foreach (var animName in lib.GetAnimationList())
        {
            if (animName == "idle") continue; // Keep idle intact
            
            var anim = lib.GetAnimation(animName);
            
            // We iterate backwards because removing tracks shifts the indices!
            for (int i = anim.GetTrackCount() - 1; i >= 0; i--)
            {
                var path = anim.TrackGetPath(i).ToString();
                if (path.Contains("Neck") || path.Contains("Head"))
                {
                    anim.RemoveTrack(i);
                    GD.Print($"Removed {path} from {animName}");
                }
            }
        }
        
        ResourceSaver.Save(lib, "res://Assets/Animations/MixamoLibrary.res");
        GD.Print("Fixed Head/Neck tracks!");
        Quit();
    }
}
