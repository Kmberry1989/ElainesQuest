using Godot;

public partial class FixLibPaths : SceneTree
{
    public override void _Initialize()
    {
        var lib = ResourceLoader.Load<AnimationLibrary>("res://Assets/Animations/MixamoLibrary.res");
        
        foreach (var animName in lib.GetAnimationList())
        {
            var anim = lib.GetAnimation(animName);
            for (int i = 0; i < anim.GetTrackCount(); i++)
            {
                var path = anim.TrackGetPath(i).ToString();
                
                // If it looks like RootNode/mixamorig_Hips
                if (path.StartsWith("RootNode/mixamorig_"))
                {
                    var boneName = path.Substring(9); // remove RootNode/
                    anim.TrackSetPath(i, $"RootNode/Skeleton3D:{boneName}");
                }
                // If it looks like GeneralSkeleton:mixamorig_Hips
                else if (path.Contains("GeneralSkeleton:"))
                {
                    var boneName = path.Substring(path.IndexOf(":") + 1);
                    anim.TrackSetPath(i, $"RootNode/Skeleton3D:{boneName}");
                }
            }
        }
        
        ResourceSaver.Save(lib, "res://Assets/Animations/MixamoLibrary.res");
        GD.Print("Fixed paths in MixamoLibrary.res to use Skeleton3D!");
        Quit();
    }
}
