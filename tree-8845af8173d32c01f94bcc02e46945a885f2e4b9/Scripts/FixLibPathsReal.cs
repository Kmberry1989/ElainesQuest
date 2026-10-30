using Godot;

public partial class FixLibPathsReal : SceneTree
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
                
                // path is like "RootNode/Skeleton3D:mixamorig_Hips/mixamorig_Spine" because of our last script
                // We need to extract just the last part after the slash.
                
                if (path.Contains("Skeleton3D:"))
                {
                    var boneNameParts = path.Split(":");
                    var bonePath = boneNameParts[1];
                    
                    var actualBoneName = bonePath;
                    if (bonePath.Contains("/"))
                    {
                        var parts = bonePath.Split("/");
                        actualBoneName = parts[parts.Length - 1];
                    }
                    
                    anim.TrackSetPath(i, $"RootNode/Skeleton3D:{actualBoneName}");
                }
            }
        }
        
        ResourceSaver.Save(lib, "res://Assets/Animations/MixamoLibrary.res");
        GD.Print("Fixed paths in MixamoLibrary.res properly this time!");
        Quit();
    }
}
