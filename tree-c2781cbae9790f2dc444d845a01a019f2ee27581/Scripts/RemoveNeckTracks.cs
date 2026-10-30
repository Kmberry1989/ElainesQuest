using Godot;

public partial class RemoveNeckTracks : SceneTree
{
    public override void _Initialize()
    {
        var lib = ResourceLoader.Load<AnimationLibrary>("res://Assets/Animations/MixamoLibrary.res");
        
        foreach (var animName in lib.GetAnimationList())
        {
            if (animName == "idle" || animName == "bored") continue;
            
            var anim = lib.GetAnimation(animName);
            
            // Iterate backwards when deleting
            for (int i = anim.GetTrackCount() - 1; i >= 0; i--)
            {
                string path = anim.TrackGetPath(i).ToString();
                if (path.Contains("mixamorig_Neck") || path.Contains("mixamorig_Head"))
                {
                    anim.RemoveTrack(i);
                }
            }
        }
        
        ResourceSaver.Save(lib, "res://Assets/Animations/MixamoLibrary.res");
        GD.Print("Removed Neck and Head tracks from all action animations!");
        Quit();
    }
}
