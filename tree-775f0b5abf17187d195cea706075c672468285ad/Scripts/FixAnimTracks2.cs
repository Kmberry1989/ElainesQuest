using Godot;

[Tool]
public partial class FixAnimTracks2 : SceneTree
{
    public override void _Initialize()
    {
        string libPath = "res://Assets/Animations/MixamoLibrary.res";
        var lib = ResourceLoader.Load<AnimationLibrary>(libPath);
        
        if (lib != null)
        {
            var anims = lib.GetAnimationList();
            foreach (var animName in anims)
            {
                var anim = lib.GetAnimation(animName);
                for (int i = 0; i < anim.GetTrackCount(); i++)
                {
                    string oldPath = anim.TrackGetPath(i).ToString();
                    if (oldPath.StartsWith("RootNode/Skeleton3D:"))
                    {
                        string newPath = "Visuals/__RuntimeModel/" + oldPath;
                        anim.TrackSetPath(i, newPath);
                    }
                }
            }
            
            ResourceSaver.Save(lib, libPath);
            GD.Print("Successfully updated tracks in MixamoLibrary.res to use Visuals/__RuntimeModel/RootNode/Skeleton3D");
        }
        Quit();
    }
}
