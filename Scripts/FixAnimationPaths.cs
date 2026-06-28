using Godot;

public partial class FixAnimationPaths : SceneTree
{
    public override void _Initialize()
    {
        var lib = ResourceLoader.Load<AnimationLibrary>("res://Assets/Animations/MixamoLibrary.res");
        
        foreach (var animName in lib.GetAnimationList())
        {
            var anim = lib.GetAnimation(animName);
            for (int i = 0; i < anim.GetTrackCount(); i++)
            {
                var path = anim.TrackGetPath(i);
                string newPath = path.ToString().Replace("Visuals/__RuntimeModel/RootNode/Skeleton3D", "RootNode/Skeleton3D");
                anim.TrackSetPath(i, newPath);
            }
        }
        
        ResourceSaver.Save(lib, "res://Assets/Animations/MixamoLibrary.res");
        GD.Print("Fixed animation paths!");
        Quit();
    }
}
