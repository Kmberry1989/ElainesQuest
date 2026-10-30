using Godot;

[Tool]
public partial class FixAnimTracks : SceneTree
{
    public override void _Initialize()
    {
        var lib = ResourceLoader.Load<AnimationLibrary>("res://Assets/Animations/MixamoLibrary.res");
        if (lib != null)
        {
            var animList = lib.GetAnimationList();
            foreach (var animName in animList)
            {
                var anim = lib.GetAnimation(animName);
                if (anim != null)
                {
                    for (int i = 0; i < anim.GetTrackCount(); i++)
                    {
                        string oldPath = anim.TrackGetPath(i);
                        // Convert "RootNode/mixamorig_Hips..." to "RootNode/Skeleton3D:mixamorig_Hips..."
                        if (oldPath.StartsWith("RootNode/mixamorig_"))
                        {
                            // We need to extract just the bone name, which is the last part of the path, or is it the whole path?
                            // Wait, if it's "RootNode/mixamorig_Hips/mixamorig_Spine", the bone name is "mixamorig_Spine".
                            // In Godot, a bone track is "Path/To/Skeleton:BoneName".
                            string[] parts = oldPath.Split('/');
                            string boneName = parts[parts.Length - 1];
                            
                            // Check if there is a property suffix like ":position" or ":rotation"
                            string property = "";
                            if (boneName.Contains(":"))
                            {
                                int colonIdx = boneName.IndexOf(':');
                                property = boneName.Substring(colonIdx);
                                boneName = boneName.Substring(0, colonIdx);
                            }
                            
                            string newPath = "RootNode/Skeleton3D:" + boneName + property;
                            anim.TrackSetPath(i, newPath);
                        }
                    }
                }
            }
            
            Error err = ResourceSaver.Save(lib, "res://Assets/Animations/MixamoLibrary.res");
            if (err == Error.Ok)
            {
                GD.Print("Successfully fixed and saved MixamoLibrary.res!");
            }
            else
            {
                GD.PrintErr("Failed to save: " + err);
            }
        }
        else
        {
            GD.PrintErr("Could not load MixamoLibrary.res");
        }
        
        Quit();
    }
}
